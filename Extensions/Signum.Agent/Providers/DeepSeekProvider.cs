using Microsoft.Extensions.AI;
using OpenAI;
using Signum.Agent;
using System.ClientModel;

namespace Signum.Agent.Providers;

public class DeepSeekProvider : IChatbotModelProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        var allModels = await GetAllModelNames(ct);
        // DeepSeek doesn't have embedding models, so just return all
        return allModels;
    }

    async Task<List<string>> GetAllModelNames(CancellationToken ct)
    {
        string? apiKey = GetApiKey();

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com/v1")
        });

        var models = await client.GetOpenAIModelClient().GetModelsAsync(ct);
        return models.Value.Select(a => a.Id).ToList();
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        string apiKey = GetApiKey();

        var client = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.deepseek.com/v1")
        });

        var chatClient = client.GetChatClient(model.Model).AsIChatClient();

        return chatClient;
    }

    public void CustomizeMessagesAndOptions(List<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions options)
    {
        // DeepSeek doesn't need any special customization
        // The OpenAI-compatible API works with default parameters
    }

    static string GetApiKey()
    {
        var apiKey = ChatbotLogic.GetConfig().DeepSeekAPIKey;

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("No API Key for DeepSeek configured!");
        return apiKey;
    }
}
