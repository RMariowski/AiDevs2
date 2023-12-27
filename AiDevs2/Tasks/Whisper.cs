using Azure.AI.OpenAI;

/*
 * Korzystając z modelu Whisper wykonaj zadanie API (zgodnie z opisem na zadania.aidevs.pl) o nazwie whisper.
 * W ramach zadania otrzymasz plik MP3 (15 sekund), który musisz wysłać do transkrypcji, a otrzymany z niej tekst
 * odeślij jako rozwiązanie zadania.
 */

namespace AiDevs2.Tasks;

internal sealed class Whisper
{
    public static async Task StartAsync(AiDevsClient aiDevsClient, OpenAIClient openAiClient)
    {
        var mp3Stream = await GetMp3FileAsync();
        var mp3Data = await BinaryData.FromStreamAsync(mp3Stream);

        var tokenResponse = await aiDevsClient.GetTokenAsync("whisper");
        Console.WriteLine(tokenResponse);

        _ = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);

        var answer = await GetTranscriptionAsync(openAiClient, mp3Data);
        Console.WriteLine($"OpenAI Answer: {answer}");

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, new AnswerRequest(answer));
    }

    private static async Task<Stream> GetMp3FileAsync()
    {
        using HttpClient fileClient = new();
        var fileResponse = await fileClient.GetAsync("https://zadania.aidevs.pl/data/mateusz.mp3");
        return await fileResponse.Content.ReadAsStreamAsync();
    }

    private static async Task<string> GetTranscriptionAsync(OpenAIClient openAiClient, BinaryData mp3Data)
    {
        var transcriptionResponse = await openAiClient.GetAudioTranscriptionAsync(new AudioTranscriptionOptions
        {
            DeploymentName = "whisper-1",
            AudioData = mp3Data,
            ResponseFormat = AudioTranscriptionFormat.Verbose,
            Filename = "mateusz.mp3"
        });
        return transcriptionResponse.Value.Text;
    }

    private record TaskResponse;
}
