using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Models;
using Microsoft.Extensions.AI;
using OpenAI.Embeddings;
using Signum.Utilities.Synchronization;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Signum.Agent.Providers;

public class AnthropicProvider : ILanguageModelProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        string apiKey = GetApiKey();

        var models = await new AnthropicClient(apiKey).Models.ListModelsAsync(ctx: ct);
        return models.Models.Select(models => models.Id).ToList();
    }

    public Task<List<string>> GetEmbeddingModelNames(CancellationToken ct)
    {
        return Task.FromResult(new List<string>());
    }


    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string apiKey = GetApiKey();

        IChatClient client = new AnthropicClient(new APIAuthentication(apiKey)).Messages;

        return client;
    }

    private static string GetApiKey()
    {
        var apiKey = ChatbotLogic.GetConfig().AnthropicAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Claude configured!");
        return apiKey;
    }

    public List<float[]> GetEmbeddings(string[] embeddings, int? numParameters)
    {
        throw new NotSupportedException("Anthropic does not provide embedding models. Consider using Voyage AI or another embedding provider.");
    }
}
