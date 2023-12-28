using System.Net.Http.Json;
using Azure.AI.OpenAI;

/*
 * Wykonaj zadanie API o nazwie “meme”. Celem zadania jest nauczenie Cię pracy z generatorami grafik i dokumentów.
 * Zadanie polega na wygenerowaniu mema z podanego obrazka i podanego tekstu. Mem ma być obrazkiem JPG o wymiarach
 * 1080x1080. Powinien posiadać czarne tło, dostarczoną grafikę na środku i podpis zawierający dostarczony tekst.
 * Grafikę z memem możesz wygenerować za pomocą darmowych tokenów dostępnych w usłudze RenderForm
 * (50 pierwszych grafik jest darmowych). URL do wygenerowanej grafiki spełniającej wymagania wyślij do
 * endpointa /answer/. W razie jakichkolwiek problemów możesz sprawdzić hinty https://zadania.aidevs.pl/hint/meme
 */

namespace AiDevs2.Tasks;

internal sealed class Meme
{
    public static async Task StartAsync(AiDevsClient aiDevsClient, OpenAIClient openAiClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("meme");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        var memeUrl = await GenerateMemeImageAsync(taskResponse);

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, new AnswerRequest(memeUrl));
    }

    private static async Task<string> GenerateMemeImageAsync(TaskResponse taskResponse)
    {
        using HttpClient memeClient = new();
        memeClient.DefaultRequestHeaders.Add("X-API-KEY", Envs.RenderFormApiKey);
        var memeResponse = await memeClient.PostAsJsonAsync("https://get.renderform.io/api/v2/render", new
        {
            template = Envs.RenderFormTemplate,
            data = new Dictionary<string, string>
            {
                { "title.text", taskResponse.Text },
                { "image.src", taskResponse.Image }
            }
        });
        var meme = await memeResponse.Content.ReadFromJsonAsync<MemeResponse>();
        Console.WriteLine(meme);
        return meme!.Href;
    }

    private record TaskResponse(string Image, string Text);

    private record MemeResponse(string RequestId, string Href);
}
