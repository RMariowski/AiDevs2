using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;

namespace AiDevs2.Tasks;

internal sealed class Tools
{
    public static async Task StartAsync(AiDevsClient aiDevsClient, OpenAIClient openAiClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("tools");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        var answer = await GetAnswerAsync(openAiClient, taskResponse);
        Console.WriteLine($"OpenAI Answer: {answer}");

        var answerObject = JsonSerializer.Deserialize<object>(answer)!;
        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, new AnswerRequest(answerObject));
    }

    private static async Task<string> GetAnswerAsync(OpenAIClient openAiClient, TaskResponse taskResponse)
    {
        var systemMessage =
            $"""
              Zadecyduj, czy zadanie powinno zostać dodane do listy ToDo, czy do kalendarza (jeśli podano datę/dzień/czas) i zwróć odpowiedni JSON.
              Zawsze używaj YYYY-MM-DD jako format dat.
              Dziś jest {DateTime.Now}
              Przykład zadania ToDo: {taskResponse.ExampleForToDo}
              Przykład zadania Calendar: {taskResponse.ExampleForCalendar}
              """;

        var userMessage = taskResponse.Question;

        var chatCompletionsResponse = await openAiClient.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = "gpt-4",
            Messages =
            {
                new ChatRequestSystemMessage(systemMessage),
                new ChatRequestUserMessage(userMessage),
            }
        });

        return chatCompletionsResponse.Value.Choices[0].Message.Content;
    }

    private record TaskResponse(
        [property: JsonPropertyName("example for ToDo")]
        string ExampleForToDo,
        [property: JsonPropertyName("example for Calendar")]
        string ExampleForCalendar,
        string Question);
}
