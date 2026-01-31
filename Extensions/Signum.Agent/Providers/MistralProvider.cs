using Microsoft.Extensions.AI;
using Mistral.SDK;

namespace Signum.Agent.Providers;

public class MistralProvider : ILanguageModelProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        string? apiKey = GetApiKey();

        var models = await new MistralClient(new APIAuthentication(apiKey)).Models.GetModelsAsync(ct);

        return models.Data.Select(a => a.Id).ToList();    
    }

    public Task<List<string>> GetEmbeddingModelNames(CancellationToken ct)
    {
        return Task.FromResult(new List<string>
        {
            "mistral-embed"
        });
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

    public List<float[]> GetEmbeddings(string[] embeddings, int? numParameters)
    {
        throw new NotImplementedException("Mistral embedding API needs to be verified");
    }
}
