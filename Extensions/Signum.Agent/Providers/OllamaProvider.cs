using Microsoft.Extensions.AI;
using OllamaSharp;

namespace Signum.Agent.Providers;

public class OllamaProvider : IChatbotModelProvider, IEmbeddingsProvider
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

    public async Task<List<float[]>> GetEmbeddings(string[] inputs, EmbeddingsLanguageModelEntity model, CancellationToken ct)
    {
        string url = GetOllamaUrl();
        var client = new OllamaApiClient(url);

        var response = await client.EmbedAsync(new OllamaSharp.Models.EmbedRequest
        {

            Input = inputs.ToList(),
            Model = model.Model,
            Dimensions = model.Dimensions,
        });

        return response.Embeddings;
    }

    private static string GetOllamaUrl()
    {
        var apiKey = ChatbotLogic.GetConfig().OllamaUrl;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No Ollama URL configured!");
        return apiKey;
    }
}
