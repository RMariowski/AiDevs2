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
    public static async Task StartAsync(AiDevsClient aiDevsClient, OpenAIClient openAiClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("scraper");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        var fileContent = await GetFileContentAsync(taskResponse);

        var answer = await GetAnswerAsync(openAiClient, fileContent, taskResponse);
        Console.WriteLine($"OpenAI Answer: {answer}");

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private static async Task<string> GetFileContentAsync(TaskResponse taskResponse)
    {
        var fileResponse = await HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            .ExecuteAsync(async () =>
            {
                using HttpClient client = new();
                var response = await client.GetAsync(taskResponse.Input);
                return response;
            });

        return await fileResponse.Content.ReadAsStringAsync();
    }

    private static async Task<string> GetAnswerAsync(OpenAIClient openAiClient, string fileContent,
        TaskResponse taskResponse)
    {
        var systemMessage =
            $$"""
              {{fileContent}}
              {{taskResponse.Msg}}
              Return result in JSON format for example: {"answer":"text"}.
              """;

        var userMessage = taskResponse.Question;

        var chatCompletionsResponse = await openAiClient.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = "gpt-3.5-turbo",
            Messages =
            {
                new ChatRequestSystemMessage(systemMessage),
                new ChatRequestUserMessage(userMessage),
            }
        });

        return chatCompletionsResponse.Value.Choices[0].Message.Content;
    }

    private record TaskResponse(string Msg, string Input, string Question);
}
