using Microsoft.Extensions.AI;
using OllamaSharp;

namespace Signum.Chatbot.Providers;

public class OllamaChatbotProvider : IChatbotProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        string url = GetOllamaUrl();
        var models = await new OllamaApiClient(url).ListLocalModelsAsync(ct);
        return models.Select(a => a.Name).ToList();
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string url = GetOllamaUrl();

        IChatClient client = new OllamaApiClient(url);

        return client;
    }

    private static string GetOllamaUrl()
    {
        var apiKey = ChatbotLogic.GetConfig().OllamaUrl;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No Ollama URL configured!");
        return apiKey;
    }


}
