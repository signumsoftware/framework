using Signum.Utilities.Synchronization;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Signum.Chatbot.Providers;

public abstract class ChatbotProviderBase : IChatbotProvider
{
    protected virtual string GetMessagesUrl() => "v1/messages";

    public async IAsyncEnumerable<StreamingValue> AskStreaming(List<ChatMessage> messages, List<IChatbotTool> tools, ChatbotLanguageModelEntity model, [EnumeratorCancellation] CancellationToken ct)
    {
        var client = GetClient();

        var system = model.Provider.Is(ChatbotProviders.Anthropic) ?            
            messages.Where(a => a.Role == ChatMessageRole.System).FirstOrDefault() : null;

        var payload = new JsonObject
        {
            { "model", model.Model },
            { "messages",
                new JsonArray(
                    messages
                    .Where(s => s != system)
                    .Select(m => new JsonObject
                    {
                        { "role", ToRoleString(m.Role) },
                        { "content", m.Content }
                    }.AddIfNotNull("tool_call_id", m.ToolCallID))
                    .ToArray()
                )
            },
            { "stream", true },
            { "temperature", model.Temperature ?? 1.0 },
            { "max_tokens", model.MaxTokens ?? 1024 }
        }
        .AddIfNotNull("system", system?.Content)
        .AddIfNotNull("tools", tools.IsNullOrEmpty() ? null : new JsonArray(tools.Select(t => t.ParametersSchema(model.Provider)).ToArray()));


        var responseClient = await client.PostAsJsonAsync(GetMessagesUrl(), payload);


        if (!responseClient.IsSuccessStatusCode)
        {
            var errorResult = await responseClient.Content.ReadAsStringAsync(ct);
            throw new ChatbotProviderException(errorResult) { Data = { ["model"] = model } };
        }


        var stream = await responseClient.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);


        // delegate the parsing
        await foreach (var chunk in ParseStream(reader, ct))
            yield return chunk;
    }


    protected virtual async IAsyncEnumerable<StreamingValue> ParseStream(
        StreamReader reader,
        [EnumeratorCancellation] CancellationToken ct)
    {
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            Debug.WriteLine(line);
            if (line.HasText() && line.StartsWith("data: "))
            {
                var payload = line.After("data: ");

                if (payload == "[DONE]")
                    yield break;

                var eventData = JsonSerializer.Deserialize<JsonElement>(payload);
                var delta = eventData
                    .GetProperty("choices")[0]
                    .GetProperty("delta");

                //    if(delta.TryGetProperty("tool_calls", out var tool_calls))
                //    {
                //        var tool = tool_calls.EnumerateArray().SingleEx();
                //        if (mode == null)
                //        {
                //            mode = IChatbotProvider.ToolCall;
                //            var functionName = tool.GetProperty("function").GetProperty("name").GetString();
                //            if (functionName.IsNullOrEmpty())
                //                throw new ChatbotProviderException("Tool call without function name");

                //            var args = tool.GetProperty("function").GetProperty("arguments").GetString();
                //            yield return IChatbotProvider.ToolCall + functionName;
                //            if (args.HasText())
                //                yield return StreamingValue.ToolCal_Start();
                //        }
                //        else
                //        {
                //            if(mode != IChatbotProvider.ToolCall)
                //                throw new ChatbotProviderException("Mixed modes in the same response");
                //            var args = tool.GetProperty("function").GetProperty("arguments").GetString();
                //            if (args.HasText())
                //                yield return args;
                //        }                   
                //    }
                //    else if(delta.TryGetProperty("content", out var content))
                //    {
                //        if (mode == null)
                //        {
                //            mode = IChatbotProvider.Answer;
                //            var text = content.GetString()!;
                //            yield return new StreamingValue( IChatbotProvider.Answer + text;
                //        }
                //        else
                //        {
                //            if (mode != IChatbotProvider.Answer)
                //                throw new ChatbotProviderException("Mixed modes in the same response");
                //            var text = content.GetString()!;
                //            if (text.HasText())
                //                yield return text;
                //        }
                //    }
                //    else if (delta.TryGetProperty("role", out var role))
                //    {
                //        // Ignore
                //    }
                //    else
                //    {
                //        throw new ChatbotProviderException("Unknown delta format: " + delta.ToString());
                //    }
                //}
            }
        }
    }

    protected virtual string ToRoleString(ChatMessageRole role) => role switch
    {
        ChatMessageRole.System => "system",
        ChatMessageRole.User => "user",
        ChatMessageRole.Assistant => "assistant",
        ChatMessageRole.Tool => "tool",
        _ => throw new UnexpectedValueException(role)
    };

    public virtual string[] GetModelNames()
    {
        var client = GetClient();

        var response = client.GetFromJsonAsync<JsonDocument>("v1/models").ResultSafe();

        return response!.RootElement.GetProperty("data").EnumerateArray()
            .Select(a => a.GetProperty("id").GetString()!)
            .ToArray();
    }

    protected abstract HttpClient GetClient(bool stream = false);
}

[Serializable]
public class ChatbotProviderException : Exception
{
    public ChatbotProviderException(string message) : base(message) { }
}
