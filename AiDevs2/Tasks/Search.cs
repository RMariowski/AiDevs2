/*
 * Dziś zadanie jest proste, ale nie łatwe — zaimportuj do swojej bazy wektorowej, spis wszystkich linków z newslettera
 * unknowNews z adresu: https://unknow.news/archiwum.json [jeśli zależy Ci na czasie, możesz zaindeksować np. tylko
 * rekordy z przedziału 500-1000]
 * Następnie wykonaj zadanie API o nazwie “search” — odpowiedz w nim na zwrócone przez API pytanie. Odpowiedź musi
 * być adresem URL kierującym do jednego z linków unknowNews. Powodzenia!
 */

using System.Net.Http.Json;
using Azure;
using Azure.AI.OpenAI;

namespace AiDevs2.Tasks;

internal sealed class Search
{
    private static readonly VectorCollection<FileEntry> VectorDb = new();

    public static async Task StartAsync(AiDevsClient aiDevsClient, OpenAIClient openAiClient)
    {
        await SetupVectorDatabase(openAiClient);

        var tokenResponse = await aiDevsClient.GetTokenAsync("search");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        var questionEmbedding = await GetEmbeddingOfQuestion(openAiClient, taskResponse);

        var entry = VectorDb.FindNearest(questionEmbedding);

        AnswerRequest answer = new(entry.Url);
        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private static async Task SetupVectorDatabase(OpenAIClient openAiClient)
    {
        var entries = await GetFileEntriesAsync();
        VectorDb.AddRange(entries);

        var embeddingResponse = await GetEmbeddingsForEntries(openAiClient, entries);
        foreach (var embeddingItem in embeddingResponse.Value.Data)
        {
            entries[embeddingItem.Index].Vector = embeddingItem.Embedding.ToArray();
        }
    }

    private static async Task<FileEntry[]> GetFileEntriesAsync()
    {
        using HttpClient client = new();
        var fileResponse = await client.GetAsync("https://unknow.news/archiwum.json");
        var fileContent = await fileResponse.Content.ReadFromJsonAsync<FileEntry[]>();
        return fileContent!.Take(500).ToArray();
    }

    private static async Task<Response<Embeddings>> GetEmbeddingsForEntries(OpenAIClient openAiClient,
        FileEntry[] entries)
    {
        EmbeddingsOptions embeddingsOptions = new() { DeploymentName = "text-embedding-ada-002" };
        foreach (var entry in entries)
            embeddingsOptions.Input.Add(entry.Info);
        return await openAiClient.GetEmbeddingsAsync(embeddingsOptions);
    }

    private static async Task<float[]> GetEmbeddingOfQuestion(OpenAIClient openAiClient,
        TaskResponse taskResponse)
    {
        EmbeddingsOptions embeddingsOptions = new()
        {
            DeploymentName = "text-embedding-ada-002",
            Input = { taskResponse.Question }
        };
        var embeddingResponse = await openAiClient.GetEmbeddingsAsync(embeddingsOptions);
        return embeddingResponse.Value.Data[0].Embedding.ToArray();
    }

    private record TaskResponse(string Question);

    private record FileEntry(string Title, string Url, string Info, string Date) : IVectorObject
    {
        public float[] Vector { get; set; } = null!;

        public float[] GetVector() => Vector;
    }
}
