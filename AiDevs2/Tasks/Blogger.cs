using Azure.AI.OpenAI;

/*
 * Napisz wpis na bloga (w języku polskim) na temat przyrządzania pizzy Margherity. Zadanie w API nazywa się
 * ”blogger”. Jako wejście otrzymasz spis 4 rozdziałów, które muszą pojawić
 * się we wpisie. Jako odpowiedź
 * musisz zwrócić tablicę (w formacie JSON) złożoną z 4 pól reprezentujących te cztery rozdziały, np.: {"answer":["tekst 1","tekst 2","tekst 3","tekst 4"]}
 */

namespace AiDevs2.Tasks;

internal sealed class Blogger
{
    public static async Task StartAsync(AiDevsClient aiDevsClient, OpenAIClient openAiClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("blogger");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);

        var answer = await GetTextForChaptersAsync(openAiClient, taskResponse);
        Console.WriteLine($"OpenAI Answer: {answer}");

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private static async Task<string> GetTextForChaptersAsync(OpenAIClient openAiClient, TaskResponse taskResponse)
    {
        const string systemMessage =
            """
            Dla każdego rozdziału podanego przez użytkownika przygotuj tekst o przyrządzania pizzy Margherita na bloga.
            Odpowiedź zwracaj w formacie JSON np. {"answer":["tekst dla rozdziału 1","tekst dla rozdziału 2","tekst dla rozdziału 3","tekst dla rozdziału 4"]}.
            W odpowiedzi nie podawaj tytułów rozdziałów, które podał użytkownik.
            """;

        var userMessage = string.Join("\n", taskResponse.Blog);

        var chatCompletionsResponse = await openAiClient.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = "gpt-3.5-turbo",
            Messages =
            {
                new ChatRequestSystemMessage(systemMessage),
                new ChatRequestUserMessage(userMessage)
            }
        });
        return chatCompletionsResponse.Value.Choices[0].Message.Content;
    }

    private record TaskResponse(string[] Blog);
}
