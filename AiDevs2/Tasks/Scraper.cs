/*
 * Rozwiąż zadanie z API o nazwie "scraper". Otrzymasz z API link do artykułu (format TXT), który zawiera pewną wiedzę,
 * oraz pytanie dotyczące otrzymanego tekstu. Twoim zadaniem jest udzielenie odpowiedzi na podstawie artykułu. Trudność
 * polega tutaj na tym, że serwer z artykułami działa naprawdę kiepsko — w losowych momentach zwraca błędy typu
 * "error 500", czasami odpowiada bardzo wolno na Twoje zapytania, a do tego serwer odcina dostęp nieznanym
 * przeglądarkom internetowym. Twoja aplikacja musi obsłużyć każdy z napotkanych błędów. Pamiętaj, że pytania,
 * jak i teksty źródłowe, są losowe, więc nie zakładaj, że uruchamiając aplikację kilka razy, za każdym razem zapytamy
 * Cię o to samo i będziemy pracować na tym samym artykule.
 */

using Azure.AI.OpenAI;
using Polly;
using Polly.Extensions.Http;

namespace AiDevs2.Tasks;

internal sealed class Scraper
{
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("scraper");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        var fileResponse = await HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            .ExecuteAsync(async () =>
            {
                using HttpClient client = new();
                var response = await client.GetAsync(taskResponse.Input);
                return response;
            });
        var fileContent = await fileResponse.Content.ReadAsStringAsync();

        OpenAIClient client = new(Envs.OpenAiApiKey, new OpenAIClientOptions());
        var systemMessage =
            $$"""
              {{fileContent}}
              {{taskResponse.Msg}}
              Return result in JSON format for example: {"answer":"text"}.
              """;
        var userMessage = taskResponse.Question;
        var chatCompletionsResponse = await client.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = "gpt-3.5-turbo",
            Messages =
            {
                new ChatRequestSystemMessage(systemMessage),
                new ChatRequestUserMessage(userMessage),
            }
        });
        var chat = chatCompletionsResponse.Value;

        var answer = chat.Choices[0].Message.Content;
        Console.WriteLine($"OpenAI Answer: {answer}");

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private record TaskResponse(string Msg, string Input, string Question);
}
