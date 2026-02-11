using Microsoft.Extensions.AI;
using DeepSeek;

namespace Signum.Agent.Providers;

public class DeepSeekProvider : IChatbotModelProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        string? apiKey = GetApiKey();
        var client = new DeepSeekClient(apiKey);
        var modelsResponse = await client.ListModelsAsync(ct);
        
        if (modelsResponse?.Data == null)
            return new List<string>();
            
        return modelsResponse.Data.Select(a => a.Id).Where(id => id != null).ToList()!;
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string? apiKey = GetApiKey();
        var client = new DeepSeekClient(apiKey);
        return (IChatClient)client;
    }

    static string GetApiKey()
    {
        var apiKey = ChatbotLogic.GetConfig().DeepSeekAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for DeepSeek configured!");
        return apiKey;
    }
}
