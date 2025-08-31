using Microsoft.Extensions.AI;
using OpenAI;

namespace Signum.Chatbot.Providers;

public class OpenAIChatbotProvider : IChatbotProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        string? apiKey = GetApiKey();
        var models = await new OpenAIClient(apiKey).GetOpenAIModelClient().GetModelsAsync(ct);
        return models.Value.Select(a => a.Id).ToList();
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string apiKey = GetApiKey();

        IChatClient client = new OpenAIClient(apiKey).GetChatClient(model.Model).AsIChatClient();

        return client;
    }

    static string GetApiKey()
    {
        var apiKey = ChatbotLogic.GetConfig().OpenAIAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Claude configured!");
        return apiKey;
    }


}
