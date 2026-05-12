using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Signum.Agent;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;

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
        // DeepSeek requires reasoning_content to be passed back as a top-level field
        // on assistant messages when in thinking mode. We reconstruct the raw OpenAI
        // AssistantChatMessage so that SerializedAdditionalRawData carries it through.
        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            if (msg.Role != ChatRole.Assistant) continue;

            var reasoning = msg.Contents.OfType<TextReasoningContent>().FirstOrDefault();
            if (reasoning == null) continue;

            var text = string.Concat(msg.Contents.OfType<TextContent>().Select(c => c.Text)).DefaultToNull();
            var toolCalls = msg.Contents.OfType<FunctionCallContent>().ToList();

            var rawObj = new Dictionary<string, object?>
            {
                ["role"] = "assistant",
                ["reasoning_content"] = reasoning.Text,
                ["content"] = (object?)text,
            };

            if (toolCalls.Count > 0)
            {
                rawObj["tool_calls"] = toolCalls.Select(tc => new
                {
                    id = tc.CallId,
                    type = "function",
                    function = new
                    {
                        name = tc.Name,
                        arguments = JsonSerializer.Serialize(tc.Arguments),
                    }
                }).ToList();
            }

            var rawMsg = ModelReaderWriter.Read<AssistantChatMessage>(
                BinaryData.FromObjectAsJson(rawObj))!;

            var nonReasoning = msg.Contents.Where(c => c is not TextReasoningContent).ToList();
            messages[i] = new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, nonReasoning)
            {
                RawRepresentation = rawMsg,
            };
        }
    }

    static string GetApiKey()
    {
        var apiKey = LanguageModelLogic.GetConfig().DeepSeekAPIKey;

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("No API Key for DeepSeek configured!");
        return apiKey;
    }
}
