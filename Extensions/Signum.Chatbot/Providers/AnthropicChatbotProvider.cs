using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Signum.Chatbot.Providers;

public class AnthropicChatbotProvider : IChatbotProvider
{
    public async IAsyncEnumerable<string> AskStreaming(List<ChatMessage> messages, ChatbotLanguageModelEntity model, [EnumeratorCancellation]CancellationToken ct)
    {
        List<ChatMessageEntity> answers = new List<ChatMessageEntity>();

        var client = AnthropicChatClient();

        var payload = new
        {
            model = model.Model,
            messages = messages.Select(c => new { role = ToClaudeRole(c.Role), content = c.Content }).ToArray(),
            stream = true,
            temperature = model.Temperature,
            max_tokens = 1024,
        };

        var responseClient = await client.PostAsJsonAsync("v1/messages", payload);


        if (!responseClient.IsSuccessStatusCode)
        {
            var errorResult = await responseClient.Content.ReadAsStringAsync();
            throw new MistralException(errorResult);
        }


        var stream = await responseClient.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);


        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data: ") && !line.Contains("[DONE]"))
            {
                var jsonData = line.Substring(6);
               
                var eventData = JsonSerializer.Deserialize<JsonElement>(jsonData);

                if (eventData.TryGetProperty("type", out var eventType) &&
                    eventType.GetString() == "content_block_delta")
                {
                    if (eventData.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("text", out var textElement))
                    {
                        yield return textElement.GetString() ?? "";
                    }
                }
            }
        }
    }

  


    public string[] GetModelNames()
    {
        var client = AnthropicChatClient();

        return new string[] { "mistral-medium-latest" };
    }


    public string[] GetModelVersions(string name)
    {
        return new string[] { "1.0" };
    }


    private HttpClient AnthropicChatClient(bool stream = false)
    {
        var apiKey = ChatbotLogic.GetConfig().ClaudeAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Claude configured!");

        var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.anthropic.com");
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        return client;

    }


    private string ToClaudeRole(ChatMessageRole role) => role switch
    {
        ChatMessageRole.System => "system",
        ChatMessageRole.User => "user",
        ChatMessageRole.Assistant => "assistant",
        ChatMessageRole.Tool => "function",
        _ => throw new UnexpectedValueException(role)
    };
}
