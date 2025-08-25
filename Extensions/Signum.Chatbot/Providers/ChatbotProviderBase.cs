using Signum.Utilities.Synchronization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Signum.Chatbot.Providers;

public abstract class ChatbotProviderBase : IChatbotProvider
{

    protected virtual string GetMessagesUrl() => "v1/messages";

    public async IAsyncEnumerable<string> AskStreaming(List<ChatMessage> messages, ChatbotLanguageModelEntity model, [EnumeratorCancellation] CancellationToken ct)
    {
        var client = GetClient();

        var payload = new
        {
            model = model.Model,
            messages = messages.Select(c => new { role = ToRoleString(c.Role), content = c.Content, tool_call_id = c.ToolID }).ToArray(),
            stream = true,
            temperature = model.Temperature ?? 1,
            max_tokens = model.MaxTokens ?? 1024,
        };

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


    protected async IAsyncEnumerable<string> ParseStream(
        StreamReader reader,
        [EnumeratorCancellation] CancellationToken ct)
    {
        string totalAnswer = "";
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            totalAnswer += line + "\n";
            if (line.StartsWith("data: ") && !line.Contains("[DONE]"))
            {
                var jsonData = line.Substring(6);

                var eventData = JsonSerializer.Deserialize<JsonElement>(jsonData);
                var text = eventData
                    .GetProperty("choices")[0]
                    .GetProperty("delta")
                    .GetProperty("content")
                    .GetString()!;

                yield return text;
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
