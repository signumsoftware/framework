using Microsoft.Extensions.AI;
using Mistral.SDK;

namespace Signum.Chatbot.Providers;

public class MistralChatbotProvider : IChatbotProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        string? apiKey = GetApiKey();

        var models = await new MistralClient(new APIAuthentication(apiKey)).Models.GetModelsAsync(ct);

        return models.Data.Select(a => a.Id).ToList();    
    }

    static string GetApiKey()
    {
        var apiKey = ChatbotLogic.GetConfig().MistralAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Mistral configured!");
        return apiKey;
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string? apiKey = GetApiKey();

        return new MistralClient(new APIAuthentication(apiKey)).Completions;
    }
}
