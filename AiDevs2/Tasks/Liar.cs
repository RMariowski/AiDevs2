using Azure.AI.OpenAI;

/*
 * Wykonaj zadanie o nazwie liar. Jest to mechanizm, który mówi nie na temat w 1/3 przypadków. Twoje zadanie polega
 * na tym, aby do endpointa /task/ wysłać swoje pytanie w języku angielskim (dowolne, np “What is capital of Poland?’)
 * w polu o nazwie ‘question’ (metoda POST, jako zwykłe pole formularza, NIE JSON). System API odpowie na to
 * pytanie (w polu ‘answer’) lub zacznie opowiadać o czymś zupełnie innym, zmieniając temat. Twoim zadaniem jest
 * napisanie systemu filtrującego (Guardrails), który określi (YES/NO), czy odpowiedź jest na temat. Następnie swój
 * werdykt zwróć do systemu sprawdzającego jako pojedyncze słowo YES/NO. Jeśli pobierzesz treść zadania przez API
 * bez wysyłania żadnych dodatkowych parametrów, otrzymasz komplet podpowiedzi. Skąd wiedzieć, czy odpowiedź jest
 * ‘na temat’? Jeśli Twoje pytanie dotyczyło stolicy Polski, a w odpowiedzi otrzymasz spis zabytków w Rzymie,
 * to odpowiedź, którą należy wysłać do API to NO.
 */

namespace AiDevs2.Tasks;

internal sealed class Liar
{
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("liar");
        Console.WriteLine(tokenResponse);

        const string question = "What is capital of Poland?";
        MultipartFormDataContent taskRequest = new() { { new StringContent(question), "question" } };
        var taskResponse = await aiDevsClient.PostTaskAsync<TaskResponse>(tokenResponse.Token, taskRequest);

        OpenAIClient client = new(Envs.OpenAiApiKey, new OpenAIClientOptions());
        const string systemMessage =
            """
            You are checking whatever answer matches to question.
            If answer matches to question then return only one word YES without any explanation, otherwise NO.
            Return answer in JSON format, for example {"answer":"YES"}.
            """;
        var userMessage =
            $"""
             Question: {question}
             Answer: {taskResponse.Answer}
             """;
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

    private record TaskResponse(string Answer);
}
