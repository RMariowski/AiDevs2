/*
 * Wykonaj zadanie o nazwie functions zgodnie ze standardem zgłaszania odpowiedzi opisanym na zadania.aidevs.pl.
 * Zadanie polega na zdefiniowaniu funkcji o nazwie addUser, która przyjmuje jako parametr obiekt z właściwościami:
 * imię (name, string), nazwisko (surname, string) oraz rok urodzenia osoby (year, integer).
 * Jako odpowiedź musisz wysłać jedynie ciało funkcji w postaci JSON-a. Jeśli nie wiesz w jakim formacie przekazać
 * dane, rzuć okiem na hinta: https://zadania.aidevs.pl/hint/functions
 */

namespace AiDevs2.Tasks;

internal sealed class Functions
{
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("functions");
        Console.WriteLine(tokenResponse);

        _ = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);

        const string answer =
            """
            {
                "answer": {
                    "name": "addUser",
                    "description": "adds user",
                    "parameters": {
                        "type": "object",
                        "properties": {
                            "name": {
                                "type": "string",
                                "description": "provide first name of the user"
                            },
                            "surname": {
                                "type": "string",
                                "description": "provide last name of the user"
                            },
                            "year": {
                                "type": "integer",
                                "description": "provide birth year of the user"
                            }
                        }
                    }
                }
            }
            """;

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private record TaskResponse;
}
