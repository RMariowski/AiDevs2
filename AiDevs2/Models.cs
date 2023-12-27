namespace AiDevs2;

internal record TokenResponse(string Token);

internal record AnswerRequest(object Answer);

internal record AnswerResponse(string Msg, int Code);
