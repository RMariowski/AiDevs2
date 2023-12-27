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
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("moderation");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);

        OpenAIClient client = new(Envs.OpenAiApiKey, new OpenAIClientOptions());
        const string systemMessage =
            """
            Jesteś moderatorem tekstu.
            Użytkownik podaje treść, która wymaga moderacji. Jeśli zdanie nie przeszło moderacji to nadaj mu wartość 1, w przeciwnym wypadku 0.
            Odpowiedź zwracaj w formacie JSON np. {"answer":[1,0,0,1]}.
            """;
        var userMessage = string.Join("\n", taskResponse.Input);
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

    private record TaskResponse(string[] Input);
}
