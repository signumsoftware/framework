using GenerativeAI;
using GenerativeAI.Microsoft;
using Microsoft.Extensions.AI;

namespace Signum.Chatbot.Providers;

public class GeminiChatbotProvider : IChatbotProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        string? apiKey = GetApiKey();
        var models = await new GoogleAi(apiKey).ModelClient.ListModelsAsync(cancellationToken: ct);
        return models.Models!.Select(a => a.Name).ToList();
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string apiKey = GetApiKey();

        IChatClient client = new GenerativeAIChatClient(apiKey, model.Model) { AutoCallFunction = false };

        return client;
    }

    static string GetApiKey()
    {
        var apiKey = ChatbotLogic.GetConfig().GeminiAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Gemini configured!");
        return apiKey;
    }


}
