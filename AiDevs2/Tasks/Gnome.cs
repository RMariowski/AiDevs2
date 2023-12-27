using Azure.AI.OpenAI;

/*
 * Rozwiąż zadanie API o nazwie ‘gnome’. Backend będzie zwracał Ci linka do obrazków przedstawiających gnomy/skrzaty.
 * Twoim zadaniem jest przygotowanie systemu, który będzie rozpoznawał, jakiego koloru czapkę ma wygenerowana postać.
 * Uwaga! Adres URL zmienia się po każdym pobraniu zadania i nie wszystkie podawane obrazki zawierają zdjęcie postaci
 * w czapce. Jeśli natkniesz się na coś, co nie jest skrzatem/gnomem, odpowiedz “error”.
 * Do tego zadania musisz użyć GPT-4V (Vision).
 */

namespace AiDevs2.Tasks;

internal sealed class Gnome
{
    public static async Task StartAsync(AiDevsClient aiDevsClient, OpenAIClient openAiClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("gnome");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        var answer = await GetAnswerAsync(openAiClient, taskResponse);
        Console.WriteLine($"OpenAI Answer: {answer}");

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, new AnswerRequest(answer));
    }

    private static async Task<string> GetAnswerAsync(OpenAIClient openAiClient, TaskResponse taskResponse)
    {
        var systemMessage =
            $"""
               {taskResponse.Msg}
               Hint: {taskResponse.Hint}
             """;

        var chatResponse = await openAiClient.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = "gpt-4-vision-preview",
            Messages =
            {
                new ChatRequestSystemMessage(systemMessage),
                new ChatRequestUserMessage(new ChatMessageImageContentItem(new Uri(taskResponse.Url))),
            },
        });

        return chatResponse.Value.Choices[0].Message.Content;
    }

    private record TaskResponse(string Msg, string Hint, string Url);
}
