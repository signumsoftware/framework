using Microsoft.Extensions.AI;
using OpenAI;

namespace Signum.Agent.Providers;

public class OpenAIProvider : IChatbotModelProvider, IEmbeddingsProvider
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
        var models = await new OpenAIClient(apiKey).GetOpenAIModelClient().GetModelsAsync(ct);
        return models.Value.Select(a => a.Id).ToList();
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string apiKey = GetApiKey();

        IChatClient client = new OpenAIClient(apiKey).GetChatClient(model.Model).AsIChatClient();

        return client;
    }

    public async Task<List<float[]>> GetEmbeddings(string[] inputs, EmbeddingsLanguageModelEntity model, CancellationToken ct)
    {
        var client = new OpenAIClient(GetApiKey()).GetEmbeddingClient(model.Model);

        var result = await client.GenerateEmbeddingsAsync(inputs, new OpenAI.Embeddings.EmbeddingGenerationOptions
        {
            Dimensions = model.Dimensions,
        }, ct);

        return result.Value.Select(a => a.ToFloats().ToArray()).ToList();
    }

    static string GetApiKey()
    {
        var apiKey = LanguageModelLogic.GetConfig().OpenAIAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for OpenAI configured!");
        return apiKey;
    }
}
