/*
 * Dziś zadanie jest proste, ale nie łatwe — zaimportuj do swojej bazy wektorowej, spis wszystkich linków z newslettera
 * unknowNews z adresu: https://unknow.news/archiwum.json [jeśli zależy Ci na czasie, możesz zaindeksować np. tylko
 * rekordy z przedziału 500-1000]
 * Następnie wykonaj zadanie API o nazwie “search” — odpowiedz w nim na zwrócone przez API pytanie. Odpowiedź musi
 * być adresem URL kierującym do jednego z linków unknowNews. Powodzenia!
 */

using System.Net.Http.Json;
using Azure.AI.OpenAI;

namespace AiDevs2.Tasks;

internal sealed class Search
{
    private static readonly OpenAIClient AiClient = new(Envs.OpenAiApiKey, new OpenAIClientOptions());
    private static readonly VectorCollection<FileEntry> VectorDb = new();

    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        await SetupVectorDatabase();

        var tokenResponse = await aiDevsClient.GetTokenAsync("search");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        EmbeddingsOptions embeddingsOptions = new()
        {
            DeploymentName = "text-embedding-ada-002",
            Input = { taskResponse.Question }
        };
        var embeddingResponse = await AiClient.GetEmbeddingsAsync(embeddingsOptions);

        var entry = VectorDb.FindNearest(embeddingResponse.Value.Data[0].Embedding.ToArray());

        var answer = $$"""{"answer":"{{entry.Url}}"}""";
        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private static async Task SetupVectorDatabase()
    {
        using HttpClient client = new();
        var fileResponse = await client.GetAsync("https://unknow.news/archiwum.json");
        var fileContent = await fileResponse.Content.ReadFromJsonAsync<FileEntry[]>();
        var entries = fileContent!.Take(500).ToArray();
        VectorDb.AddRange(entries);

        EmbeddingsOptions embeddingsOptions = new() { DeploymentName = "text-embedding-ada-002" };
        foreach (var entry in entries)
            embeddingsOptions.Input.Add(entry.Info);
        var embeddingResponse = await AiClient.GetEmbeddingsAsync(embeddingsOptions);
        foreach (var embeddingItem in embeddingResponse.Value.Data)
        {
            entries[embeddingItem.Index].Vector = embeddingItem.Embedding.ToArray();
        }
    }

    private record TaskResponse(string Question);

    private record FileEntry(string Title, string Url, string Info, string Date) : IVectorObject
    {
        public float[] Vector { get; set; } = null!;

        public float[] GetVector() => Vector;
    }
}
