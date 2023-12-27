/*
 * Rozwiąż zadanie o nazwie “whoami”. Za każdym razem, gdy pobierzesz zadanie, system zwróci Ci jedną ciekawostkę na
 * temat pewnej osoby. Twoim zadaniem jest zbudowanie mechanizmu, który odgadnie, co to za osoba. W zadaniu chodzi o
 * utrzymanie wątku w konwersacji z backendem. Jest to dodatkowo utrudnione przez fakt, że token ważny jest tylko
 * 2 sekundy (trzeba go cyklicznie odświeżać!). Celem zadania jest napisania mechanizmu, który odpowiada, czy na
 * podstawie otrzymanych hintów jest w stanie powiedzieć, czy wie, kim jest tajemnicza postać. Jeśli odpowiedź
 * brzmi NIE, to pobierasz kolejną wskazówkę i doklejasz ją do bieżącego wątku. Jeśli odpowiedź brzmi TAK, to zgłaszasz
 * ją do /answer/. Wybraliśmy dość ‘ikoniczną’ postać, więc model powinien zgadnąć, o kogo chodzi, po maksymalnie
 * 5-6 podpowiedziach. Zaprogramuj mechanizm tak, aby wysyłał dane do /answer/ tylko, gdy jest absolutnie pewny
 * swojej odpowiedzi.
 */

using Azure.AI.OpenAI;

namespace AiDevs2.Tasks;

internal sealed class WhoAmI
{
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        const int maxRetries = 6;
        var tokenLifeTime = TimeSpan.FromSeconds(2);

        List<string> hints = [];
        OpenAIClient client = new(Envs.OpenAiApiKey, new OpenAIClientOptions());
        TokenResponse tokenResponse = null!;
        var tokenReceivedDateTime = DateTime.MinValue;
        for (var i = 0; i < maxRetries; ++i)
        {
            if (DateTime.UtcNow - tokenReceivedDateTime >= tokenLifeTime)
            {
                tokenResponse = await aiDevsClient.GetTokenAsync("whoami");
                tokenReceivedDateTime = DateTime.UtcNow;
                Console.WriteLine(tokenResponse);
            }

            var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
            Console.WriteLine(taskResponse);

            const string systemMessage =
                """
                Based on hints you need to guess person.
                If you are completely sure who the person is then return result in JSON format for example: {"answer":"FirstName LastName"}.
                If you are not sure then just return NO
                """;
            hints.Add(taskResponse.Hint);
            ChatCompletionsOptions chatCompletionsOptions = new()
            {
                DeploymentName = "gpt-3.5-turbo",
                Messages = { new ChatRequestSystemMessage(systemMessage) }
            };
            foreach (var hint in hints)
                chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(hint));
            var chatCompletionsResponse = await client.GetChatCompletionsAsync(chatCompletionsOptions);
            var chat = chatCompletionsResponse.Value;

            var answer = chat.Choices[0].Message.Content;
            Console.WriteLine($"OpenAI Answer: {answer}");

            if (answer.Equals("NO", StringComparison.InvariantCultureIgnoreCase))
                continue;

            var answerResponse = await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
            if (answerResponse.Code == -777 /* Not correct answer */)
                continue;
            break;
        }
    }

    private record TaskResponse(string Hint);
}
