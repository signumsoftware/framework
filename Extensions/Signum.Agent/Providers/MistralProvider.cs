using Microsoft.Extensions.AI;
using Mistral.SDK;
using Mistral.SDK.DTOs;

namespace Signum.Agent.Providers;

public class MistralProvider : IChatbotModelProvider, IEmbeddingsProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        var allModels = await GetAllModelNames(ct);
        return allModels.Where(name => !name.Contains("embed", StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<string>> GetEmbeddingModelNames(CancellationToken ct)
    {
        var allModels = await GetAllModelNames(ct);
        return allModels.Where(name => name.Contains("embed", StringComparison.OrdinalIgnoreCase)).ToList();
    }

    async Task<List<string>> GetAllModelNames(CancellationToken ct)
    {
        string? apiKey = GetApiKey();

        var models = await new MistralClient(new APIAuthentication(apiKey)).Models.GetModelsAsync(ct);

        return models.Data.Select(a => a.Id).ToList();
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string? apiKey = GetApiKey();

        return new MistralClient(new APIAuthentication(apiKey)).Completions;
    }

    public async Task<List<float[]>> GetEmbeddings(string[] inputs, EmbeddingsLanguageModelEntity model, CancellationToken ct)
    {
        string? apiKey = GetApiKey();

        var client = new MistralClient(new APIAuthentication(apiKey));

        var request = new EmbeddingRequest
        {
            Model = model.Model,
            Input = inputs.ToList()
        };

        var response = await client.Embeddings.GetEmbeddingsAsync(request, ct);

        return response.Data.Select(d => d.Embedding.Select(e => (float)e).ToArray()).ToList();
    }

    static string GetApiKey()
    {
        var apiKey = ChatbotLogic.GetConfig().MistralAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Mistral configured!");
        return apiKey;
    }
}
