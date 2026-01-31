using Microsoft.Extensions.AI;
using OpenAI;

namespace Signum.Chatbot.Providers;

public class OpenAIProvider : ILanguageModelProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        string? apiKey = GetApiKey();
        var models = await new OpenAIClient(apiKey).GetOpenAIModelClient().GetModelsAsync(ct);
        return models.Value.Select(a => a.Id).ToList();
    }

    public Task<List<string>> GetEmbeddingModelNames(CancellationToken ct)
    {
        return Task.FromResult(new List<string>
        {
            "text-embedding-3-small",
            "text-embedding-3-large",
            "text-embedding-ada-002"
        });
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

    public List<float[]> GetEmbeddings(string[] embeddings, int? numParameters)
    {
        throw new NotImplementedException("OpenAI embedding API needs to be verified");
    }
}
