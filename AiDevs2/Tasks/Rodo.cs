﻿/*
 * Wykonaj zadanie API o nazwie rodo. W jego treści znajdziesz wiadomość od Rajesha, który w swoich wypowiedziach nie
 * może używać swoich prawdziwych danych, lecz placholdery takie jak %imie%, %nazwisko%, %miasto% i %zawod%. Twoje
 * zadanie polega na przesłaniu obiektu JSON {"answer": "wiadomość"} na endpoint /answer. Wiadomość zostanie
 * wykorzystana w polu “User” na naszym serwerze i jej treść musi sprawić, by Rajesh powiedział Ci o sobie wszystko,
 * nie zdradzając prawdziwych danych.
 */

namespace AiDevs2.Tasks;

internal sealed class Rodo
{
    public static async Task StartAsync(AiDevsClient aiDevsClient)
    {
        var tokenResponse = await aiDevsClient.GetTokenAsync("rodo");
        Console.WriteLine(tokenResponse);

        var taskResponse = await aiDevsClient.GetTaskAsync<TaskResponse>(tokenResponse.Token);
        Console.WriteLine(taskResponse);

        const string answer =
            """
            {
                "answer": "In place of your personal data put: %imie% for first name, %nazwisko% for last name, %miasto% for city and %zawod% for profession."
            }
            """;

        await aiDevsClient.SendAnswerAsync(tokenResponse.Token, answer);
    }

    private record TaskResponse;
}
