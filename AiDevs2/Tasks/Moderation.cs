using Azure.AI.OpenAI;

/*
 * Zastosuj wiedzę na temat działania modułu do moderacji treści i rozwiąż zadanie o nazwie “moderation” z użyciem
 * naszego API do sprawdzania rozwiązań. Zadanie polega na odebraniu tablicy zdań (4 sztuki), a następnie zwróceniu
 * tablicy z informacją, które zdania nie przeszły moderacji. Jeśli moderacji nie przeszło pierwsze i ostatnie
 * zdanie, to odpowiedź powinna brzmieć [1,0,0,1]. Pamiętaj, aby w polu ‘answer’ zwrócić tablicę w
 * JSON, a nie czystego stringa.
 */

namespace AiDevs2.Tasks;

internal sealed class Moderation
{
    public static async Task StartAsync(AiDevsClient aiDevsClient, OpenAIClient openAiClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("moderation");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);

        var answer = await GetAnswerAsync(openAiClient, taskResponse);
        Console.WriteLine($"OpenAI Answer: {answer}");

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private static async Task<string> GetAnswerAsync(OpenAIClient openAiClient, TaskResponse taskResponse)
    {
        const string systemMessage =
            """
            Jesteś moderatorem tekstu.
            Użytkownik podaje treść, która wymaga moderacji. Jeśli zdanie nie przeszło moderacji to nadaj mu wartość 1, w przeciwnym wypadku 0.
            Odpowiedź zwracaj w formacie JSON np. {"answer":[1,0,0,1]}.
            """;

        var userMessage = string.Join("\n", taskResponse.Input);

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

    private record TaskResponse(string[] Input);
}
