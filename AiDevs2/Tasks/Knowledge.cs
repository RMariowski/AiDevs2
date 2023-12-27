/*
 * Wykonaj zadanie API o nazwie ‘knowledge’. Automat zada Ci losowe pytanie na temat kursu walut, populacji wybranego
 * kraju lub wiedzy ogólnej. Twoim zadaniem jest wybór odpowiedniego narzędzia do udzielenia odpowiedzi
 * (API z wiedzą lub skorzystanie z wiedzy modelu). W treści zadania uzyskanego przez API, zawarte są dwa API,
 * które mogą być dla Ciebie użyteczne.
 */

using System.Net.Http.Json;
using System.Text.Json;
using Azure.AI.OpenAI;

namespace AiDevs2.Tasks;

internal sealed class Knowledge
{
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("knowledge");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        OpenAIClient client = new(Envs.OpenAiApiKey, new OpenAIClientOptions());
        const string systemMessage = 
            """
            Użytkownik zadaje pytanie.
            Odpowiedź na nie zwracaj w formacie JSON np. {"answer":"Odpowiedź"}.
            """;
        var chatCompletionsResponse = await client.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = "gpt-4",
            Functions =
            {
                GetPopulationFunctionDefinition(),
                GetExchangeRateFunctionDefinition()
            },
            Messages =
            {
                new ChatRequestSystemMessage(systemMessage),
                new ChatRequestUserMessage(taskResponse.Question)
            }
        });
        var chat = chatCompletionsResponse.Value;

        string answer;
        var message = chat.Choices[0].Message;
        if (message.FunctionCall is null)
        {
            answer = chat.Choices[0].Message.Content;
        }
        else if (message.FunctionCall.Name == "GetPopulation")
        {
            var country = JsonSerializer.Deserialize<FunctionParams>(message.FunctionCall.Arguments)!.value;
            var populationResponse = await new HttpClient().GetFromJsonAsync<PopulationResponse[]>(
                $"https://restcountries.com/v3.1/name/{country}");
            answer = $$"""{"answer":{{populationResponse![0].Population}}}""";
        }
        else if (message.FunctionCall.Name == "GetExchangeRate")
        {
            var currency = JsonSerializer.Deserialize<FunctionParams>(message.FunctionCall.Arguments)!.value;
            var exchangeResponse = await new HttpClient().GetFromJsonAsync<ExchangeRateResponse>(
                $"https://api.nbp.pl/api/exchangerates/rates/a/{currency}?format=json");
            answer = $$"""{"answer":{{exchangeResponse!.Rates[0].Ask}}}""";
        }
        else
        {
            answer = chat.Choices[0].Message.Content;
        }

        Console.WriteLine($"OpenAI Answer: {answer}");
        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private static FunctionDefinition GetPopulationFunctionDefinition()
    {
        return new FunctionDefinition
        {
            Name = "GetPopulation",
            Description = "Get population of given country",
            Parameters = BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        Value = new
                        {
                            Type = "string",
                            Description = "Name of country in English, for example: Poland, Germany, Spain",
                        }
                    },
                    Required = new[] { "value" },
                },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
        };
    }

    private static FunctionDefinition GetExchangeRateFunctionDefinition()
    {
        return new FunctionDefinition
        {
            Name = "GetExchangeRate",
            Description = "Get exchange rate for given currency",
            Parameters = BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        Value = new
                        {
                            Type = "string",
                            Description = "Code of currency, for example: PLN, EUR, USD",
                        }
                    },
                    Required = new[] { "value" },
                },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
        };
    }

    private record TaskResponse(string Question);

    public record FunctionParams(string value);

    private record PopulationResponse(int Population);

    private record ExchangeRateResponse(ExchangeRateItemResponse[] Rates);
    private record ExchangeRateItemResponse(decimal Ask);
}
