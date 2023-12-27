using System.Text.Json;
using Azure.AI.OpenAI;

/*
 * Rozwiąż zadanie API o nawie embedding.
 * Korzystając z modelu text-embedding-ada-002 wygeneruj embedding dla frazy Hawaiian pizza — upewnij się, że to
 * dokładnie to zdanie. Następnie prześlij wygenerowany embedding na endpoint /answer. Konkretnie musi być to
 * format {"answer": [0.003750941, 0.0038711438, 0.0082909055, -0.008753223, -0.02073651, -0.018862579, -0.010596331, -0.022425512, ..., -0.026950065]}.
 * Lista musi zawierać dokładnie 1536 elementów.
 */

namespace AiDevs2.Tasks;

internal sealed class Embedding
{
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("embedding");
        Console.WriteLine(tokenResponse);

        _ = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);

        OpenAIClient client = new(Envs.OpenAiApiKey, new OpenAIClientOptions());

        var embeddingResponse = await client.GetEmbeddingsAsync(new EmbeddingsOptions
        {
            DeploymentName = "text-embedding-ada-002",
            Input = { "Hawaiian pizza" }
        });

        var answer = JsonSerializer.Serialize(new
        {
            answer = embeddingResponse.Value.Data.SelectMany(emb => emb.Embedding.ToArray()).ToArray()
        });

        Console.WriteLine($"OpenAI Answer: {answer}");

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private record TaskResponse;
}
