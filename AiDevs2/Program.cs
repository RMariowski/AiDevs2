using AiDevs2;
using AiDevs2.Tasks;
using Azure.AI.OpenAI;

AiDevsClient aiDevsClient = new();
OpenAIClient openAiClient = new(Envs.OpenAiApiKey, new OpenAIClientOptions());
await Meme.StartAsync(aiDevsClient, openAiClient);
