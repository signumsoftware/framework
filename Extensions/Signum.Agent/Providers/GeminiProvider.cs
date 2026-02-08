using GenerativeAI;
using GenerativeAI.Microsoft;
using Microsoft.Extensions.AI;

namespace Signum.Agent.Providers;

public class GeminiProvider : IChatbotModelProvider, IEmbeddingsProvider
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
        var models = await new GoogleAi(apiKey).ModelClient.ListModelsAsync(cancellationToken: ct);
        return models.Models!.Select(a => a.Name).ToList();
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string apiKey = GetApiKey();
        IChatClient client = new GenerativeAIChatClient(apiKey, model.Model) { AutoCallFunction = false };

        return client;
    }

    public async Task<List<float[]>> GetEmbeddings(string[] inputs, EmbeddingsLanguageModelEntity model, CancellationToken ct)
    {
        string apiKey = GetApiKey();
        var embeddingModel = new GoogleAi(apiKey).CreateEmbeddingModel(model.Model);

        var contents = inputs.Select(text => new GenerativeAI.Types.Content(text, null)).ToList();

        var sizedResponse = await embeddingModel.BatchEmbedContentAsync(contents.Select(c => new GenerativeAI.Types.EmbedContentRequest
        {
            Content = c,
            OutputDimensionality = model.Dimensions
        }), cancellationToken: ct);
        return sizedResponse.Embeddings!.Select(e => e.Values!.ToArray()).ToList();
    }

    static string GetApiKey()
    {
        var apiKey = ChatbotLogic.GetConfig().GeminiAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Gemini configured!");
        return apiKey;
    }
}
