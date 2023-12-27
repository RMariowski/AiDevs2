/*
 * Rozwiąż zadanie o nazwie “people”. Pobierz, a następnie zoptymalizuj odpowiednio pod swoje potrzeby bazę danych
 * https://zadania.aidevs.pl/data/people.json [jeśli pobrałeś plik przed 11:30, to pobierz proszę poprawioną wersję].
 * Twoim zadaniem jest odpowiedź na pytanie zadane przez system. Uwaga! Pytanie losuje się za każdym razem na nowo,
 * gdy odwołujesz się do /task. Spraw, aby Twoje rozwiązanie działało za każdym razem, a także, aby zużywało możliwie
 * mało tokenów. Zastanów się, czy wszystkie operacje muszą być wykonywane przez LLM-a - może warto zachować jakiś
 * balans między światem kodu i AI?
 */

using System.Net.Http.Json;
using Azure;
using Azure.AI.OpenAI;

namespace AiDevs2.Tasks;

internal sealed class People
{
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        using HttpClient fileClient = new();
        var fileResponse = await fileClient.GetAsync("https://zadania.aidevs.pl/data/people.json");
        var fileContent = await fileResponse.Content.ReadFromJsonAsync<FileEntry[]>();
        var people = fileContent!;

        var tokenResponse = await aiDevsClient.GetTokenAsync("people");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        OpenAIClient client = new(Envs.OpenAiApiKey, new OpenAIClientOptions());

        string systemMessage;
        string userMessage;
        Response<ChatCompletions> chatCompletionsResponse;
        ChatCompletions chat;
        string answer;

        var person = people.FirstOrDefault(p => taskResponse.Question.Contains($"{p.Imie} {p.Nazwisko}"));
        if (person is null)
        {
            systemMessage = "Zwróć imię i nazwisko występujące w pytaniu. Odpowiedź podaj w mianowniku. Nie używaj zdrobnienia. Tylko dwa wyrazy - imię i nazwisko.";
            userMessage = taskResponse.Question;
            chatCompletionsResponse = await client.GetChatCompletionsAsync(new ChatCompletionsOptions
            {
                DeploymentName = "gpt-3.5-turbo",
                Messages =
                {
                    new ChatRequestSystemMessage(systemMessage),
                    new ChatRequestUserMessage(userMessage),
                }
            });
            chat = chatCompletionsResponse.Value;

            answer = chat.Choices[0].Message.Content;
            Console.WriteLine($"OpenAI Answer: {answer}");
            
            person = people.FirstOrDefault(p => answer.Contains(p.Imie) && answer.Contains(p.Nazwisko));
            if (person is null)
            {
                Console.WriteLine("AI sucks...");
                return;
            }
        }

        Console.WriteLine(person);

        systemMessage =
            $$"""
              Użytkownik zadaje pytanie dotyczące pewnej osoby.
              Osoba to: {{person.Imie}} {{person.Nazwisko}}
              Jej opis: {{person.o_mnie}}
              Jej ulubiony serial: {{person.ulubiony_serial}}
              Jej ulubiony film: {{person.ulubiony_film}}
              Jej ulubiony kolor: {{person.ulubiony_kolor}}
              Jej ulubiona postać z kapitana bomby: {{person.ulubiona_postac_z_kapitana_bomby}}
              Odpowiedź podawaj jak najkrótszą.
              Odpowiedź zawsze zwracaj w formacie JSON np. {"answer":"odpowiedź"}.
              """;
        userMessage = taskResponse.Question;
        chatCompletionsResponse = await client.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = "gpt-3.5-turbo",
            Messages =
            {
                new ChatRequestSystemMessage(systemMessage),
                new ChatRequestUserMessage(userMessage),
            }
        });
        chat = chatCompletionsResponse.Value;

        answer = chat.Choices[0].Message.Content;
        Console.WriteLine($"OpenAI Answer: {answer}");

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private record TaskResponse(string Question);

    private record FileEntry(
        string Imie,
        string Nazwisko,
        int Wiek,
        string o_mnie,
        string ulubiona_postac_z_kapitana_bomby,
        string ulubiony_serial,
        string ulubiony_film,
        string ulubiony_kolor);
}
