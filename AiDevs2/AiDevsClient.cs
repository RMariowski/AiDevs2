using System.Net.Http.Json;

namespace AiDevs2;

internal class AiDevsClient
{
    private readonly HttpClient _client = new() { BaseAddress = new Uri("https://zadania.aidevs.pl") };

    public async Task<TokenResponse> GetTokenAsync(string taskName)
    {
        var postTokenResponse =
            await _client.PostAsJsonAsync($"/token/{taskName}", new { apikey = Envs.AiDevsApiKey });
        postTokenResponse.EnsureSuccessStatusCode();
        var tokenResponse = await postTokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        return tokenResponse!;
    }

    public async Task<T> GetTaskAsync<T>(string token)
    {
        var getTaskResponse = await _client.GetAsync($"/task/{token}");
        Console.WriteLine(await getTaskResponse.Content.ReadAsStringAsync());
        getTaskResponse.EnsureSuccessStatusCode();
        var taskResponse = await getTaskResponse.Content.ReadFromJsonAsync<T>();
        return taskResponse!;
    }

    public async Task<TResponse> PostTaskAsync<TResponse>(string token, HttpContent data)
    {
        var postTaskResponse = await _client.PostAsync($"/task/{token}", data);
        Console.WriteLine(await postTaskResponse.Content.ReadAsStringAsync());
        postTaskResponse.EnsureSuccessStatusCode();
        var taskResponse = await postTaskResponse.Content.ReadFromJsonAsync<TResponse>();
        return taskResponse!;
    }

    public async Task<AnswerResponse> SendAnswerAsync(string token, string answer)
    {
        var postAnswerResponse = await _client.PostAsync($"/answer/{token}", new StringContent(answer));
        var answerResponse = await postAnswerResponse.Content.ReadFromJsonAsync<AnswerResponse>();
        Console.WriteLine(answerResponse);
        return answerResponse!;
    }
    
    public async Task<AnswerResponse> SendAnswerAsync(string token, AnswerRequest answer)
    {
        var postAnswerResponse = await _client.PostAsJsonAsync($"/answer/{token}", answer);
        var answerResponse = await postAnswerResponse.Content.ReadFromJsonAsync<AnswerResponse>();
        Console.WriteLine(answerResponse);
        return answerResponse!;
    }
}
