using Azure.AI.OpenAI;

/*
 * Skorzystaj z API zadania.aidevs.pl, aby pobrać dane zadania inprompt. Znajdziesz w niej dwie właściwości — input,
 * czyli tablicę / listę zdań na temat różnych osób (każde z nich zawiera imię jakiejś osoby) oraz question będące
 * pytaniem na temat jednej z tych osób. Lista jest zbyt duża, aby móc ją wykorzystać w jednym zapytaniu, więc dowolną
 * techniką odfiltruj te zdania, które zawierają wzmiankę na temat osoby wspomnianej w pytaniu. Ostatnim krokiem
 * jest wykorzystanie odfiltrowanych danych jako kontekst na podstawie którego model ma udzielić odpowiedzi na pytanie.
 * Zatem: pobierz listę zdań oraz pytanie, skorzystaj z LLM, aby odnaleźć w pytaniu imię, programistycznie lub
 * z pomocą no-code odfiltruj zdania zawierające to imię. Ostatecznie spraw by model odpowiedział na pytanie, a jego
 * odpowiedź prześlij do naszego API w obiekcie JSON zawierającym jedną właściwość “answer”.
 */

namespace AiDevs2.Tasks;

internal sealed class InPrompt
{
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("inprompt");
        Console.WriteLine(tokenResponse);

        var (input, question) = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);

        var lastSpaceIndex = question.LastIndexOf(' ', question.LastIndexOf('?') - 1);
        var name = question.Substring(lastSpaceIndex + 1, question.Length - lastSpaceIndex - 2);
        var filteredInput = string.Join("\n", input.Where(sentence => sentence.Contains(name)));

        OpenAIClient client = new(Envs.OpenAiApiKey, new OpenAIClientOptions());

        var systemMessage =
            $$"""
                Na pytanie użytkownika odpowiadaj na podstawie tej listy: {{filteredInput}}
                Odpowiedź zwracaj w formacie JSON np. {"answer":"odpowiedź"}.
              """;
        var chatCompletionsResponse = await client.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = "gpt-3.5-turbo",
            Messages =
            {
                new ChatRequestSystemMessage(systemMessage),
                new ChatRequestUserMessage(question),
            }
        });
        var chat = chatCompletionsResponse.Value;

        var answer = chat.Choices[0].Message.Content;
        Console.WriteLine($"OpenAI Answer: {answer}");

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private record TaskResponse(string[] Input, string Question);
}
