/*
 * Rozwiąż zadanie API o nazwie ‘optimaldb’. Masz dostarczoną bazę danych o rozmiarze ponad 30kb.
 * https://zadania.aidevs.pl/data/3friends.json Musisz zoptymalizować ją w taki sposób, aby automat korzystający z
 * niej, a mający pojemność pamięci ustawioną na 9kb był w stanie odpowiedzieć na 6 losowych pytań na temat trzech osób
 * znajdujących się w bazie. Zoptymalizowaną bazę wyślij do endpointa /answer/ jako zwykły string. Automat użyje jej
 * jako fragment swojego kontekstu i spróbuje odpowiedzieć na pytania testowe. Wyzwanie polega na tym, aby nie zgubić
 * żadnej informacji i nie zapomnieć kogo ona dotyczy oraz aby zmieścić się w wyznaczonym limicie rozmiarów bazy.
 */

using Azure.AI.OpenAI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiDevs2.Tasks;

internal sealed class OptimalDb
{
    public static async Task StartAsync(AiDevsClient aiDevsClient, OpenAIClient openAiClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("optimaldb");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        var database = await GetDatabaseAsync(taskResponse.Database);
        var deserializedDatabase = JsonConvert.DeserializeObject<Dictionary<string, JArray>>(database)!;
        var dbDict = new Dictionary<string, JArray>();
        foreach (var kvp in deserializedDatabase)
        {
            var length = (int)(0.39 * kvp.Value.Count);
            dbDict.Add(kvp.Key, new JArray(kvp.Value.Take(length)));
        }

        var optimizedDb = JsonConvert.SerializeObject(dbDict);

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, new AnswerRequest(optimizedDb));
    }

    private static async Task<string> GetDatabaseAsync(string url)
    {
        using HttpClient fileClient = new();
        var fileResponse = await fileClient.GetAsync(url);
        var fileContent = await fileResponse.Content.ReadAsStringAsync();
        return fileContent;
    }

    private record TaskResponse(string Database);
}
