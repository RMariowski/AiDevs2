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
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("blogger");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        OpenAIClient client = new(Envs.OpenAiApiKey, new OpenAIClientOptions());
        const string systemMessage =
            """
            Jesteś asystentem blogera, który kocha pizze.
            Użytkownik podaje rozdziały, a ty dla każdego z nich przygotowujesz tekst na bloga na temat przyrządzania pizzy Margherity.
            Odpowiedź zwracaj w formacie JSON np. {"answer":["tekst 1","tekst 2","tekst 3","tekst 4"]}.
            Do odpowiedzi nie dodawaj tytułów rozdziałów, które podał użytkownik.
            """;
        var userMessage = string.Join("\n", taskResponse.Blog);
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

    private record TaskResponse(string[] Blog);
}
