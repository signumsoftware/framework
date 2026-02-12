using DeepSeek.Core;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using DeepSeekModels = DeepSeek.Core.Models;

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
        return new DeepSeekChatClient(client);
    }

    static string GetApiKey()
    {
        var apiKey = ChatbotLogic.GetConfig().DeepSeekAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for DeepSeek configured!");
        return apiKey;
    }
}

public class DeepSeekChatClient : IChatClient
{
    private readonly DeepSeekClient _client;

    public DeepSeekChatClient(DeepSeekClient client)
    {
        _client = client;
    }

    public ChatClientMetadata Metadata => new ChatClientMetadata("DeepSeek");

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var request = CreateChatRequest(messages, options);
        var response = await _client.ChatAsync(request, cancellationToken);

        if (response == null)
            throw new InvalidOperationException($"DeepSeek API returned null response. Error: {_client.ErrorMsg}");

        return MapToChatResponse(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = CreateChatRequest(messages, options);
        var stream = _client.ChatStreamAsync(request, cancellationToken);

        if (stream == null)
            throw new InvalidOperationException($"DeepSeek API returned null stream. Error: {_client.ErrorMsg}");

        await foreach (var choice in stream.WithCancellation(cancellationToken))
        {
            yield return MapToStreamingUpdate(choice);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }

    DeepSeekModels.ChatRequest CreateChatRequest(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var request = new DeepSeekModels.ChatRequest
        {
            Model = options?.ModelId ?? "deepseek-chat",
            Messages = messages.Select(m => new DeepSeekModels.Message
            {
                Role = m.Role.Value,
                Content = m.Text
            }).ToList(),
            Stream = false
        };

        if (options?.Temperature.HasValue == true)
            request.Temperature = (float)options.Temperature.Value;

        if (options?.MaxOutputTokens.HasValue == true)
            request.MaxTokens = options.MaxOutputTokens.Value;

        if (options?.TopP.HasValue == true)
            request.TopP = (float)options.TopP.Value;

        if (options?.FrequencyPenalty.HasValue == true)
            request.FrequencyPenalty = (float)options.FrequencyPenalty.Value;

        if (options?.PresencePenalty.HasValue == true)
            request.PresencePenalty = (float)options.PresencePenalty.Value;

        if (options?.StopSequences?.Count > 0)
            request.Stop = options.StopSequences.ToList();

        if (options?.Tools?.Count > 0)
        {
            request.Tools = options.Tools.Select(t =>
            {
                JsonNode? parameters = new JsonObject();
                
                if (t.AdditionalProperties?.TryGetValue("schema", out var schemaObj) == true && schemaObj != null)
                {
                    parameters = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(schemaObj)) ?? parameters;
                }
                
                return new DeepSeekModels.Tool
                {
                    Type = "function",
                    Function = new DeepSeekModels.RequestFunction
                    {
                        Name = t.Name,
                        Description = t.Description ?? string.Empty,
                        Parameters = parameters
                    }
                };
            }).ToList();
        }

        return request;
    }

    private ChatResponse MapToChatResponse(DeepSeekModels.ChatResponse response)
    {
        var choice = response.Choices?.FirstOrDefault();
        
        var chatMessage = new ChatMessage(
            new ChatRole(choice?.Message?.Role ?? "assistant"),
            choice?.Message?.Content
        );

        ChatFinishReason? finishReason = choice?.FinishReason switch
        {
            "stop" => ChatFinishReason.Stop,
            "length" => ChatFinishReason.Length,
            "tool_calls" => ChatFinishReason.ToolCalls,
            "content_filter" => ChatFinishReason.ContentFilter,
            _ => null
        };

        var usage = response.Usage != null ? new UsageDetails
        {
            InputTokenCount = response.Usage.PromptTokens,
            OutputTokenCount = response.Usage.CompletionTokens,
            TotalTokenCount = response.Usage.TotalTokens
        } : null;

        return new ChatResponse([chatMessage])
        {
            FinishReason = finishReason,
            Usage = usage,
            ModelId = response.Model
        };
    }

    private ChatResponseUpdate MapToStreamingUpdate(DeepSeekModels.Choice choice)
    {
        ChatRole? role = !string.IsNullOrEmpty(choice.Delta?.Role) 
            ? new ChatRole(choice.Delta.Role) 
            : null;

        ChatFinishReason? finishReason = choice.FinishReason switch
        {
            "stop" => ChatFinishReason.Stop,
            "length" => ChatFinishReason.Length,
            "tool_calls" => ChatFinishReason.ToolCalls,
            "content_filter" => ChatFinishReason.ContentFilter,
            _ => null
        };

        return new ChatResponseUpdate
        {
            Role = role,
            Contents = [new TextContent(choice.Delta?.Content ?? string.Empty)],
            FinishReason = finishReason
        };
    }
}
