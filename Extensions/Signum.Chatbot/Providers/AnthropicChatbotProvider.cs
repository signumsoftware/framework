using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Signum.Chatbot.Providers;

public class AnthropicChatbotProvider : ChatbotProviderBase
{
    protected override HttpClient GetClient(bool stream = false)
    {
        var apiKey = ChatbotLogic.GetConfig().AnthropicAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Claude configured!");

        var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.anthropic.com");
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        return client;
    }

    protected override async IAsyncEnumerable<StreamingValue> ParseStream(StreamReader reader, [EnumeratorCancellation] CancellationToken ct)
    {
        string? toolCallId = null;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            Debug.WriteLine(line);

            if (line.HasText() && line.StartsWith("data: "))
            {
                var payload = line.After("data: ");

                if (string.IsNullOrWhiteSpace(payload))
                    continue;

                var eventData = JsonSerializer.Deserialize<JsonElement>(payload);
                var eventType = eventData.GetProperty("type").GetString();

                switch (eventType)
                {
                    case "content_block_start":
                        var contentBlock = eventData.GetProperty("content_block");
                        var blockType = contentBlock.GetProperty("type").GetString();

                        if (blockType == "tool_use")
                        {
                            yield return 
                            toolName = contentBlock.GetProperty("name").GetString();
                        }
                        break;

                    case "content_block_delta":
                        var delta = eventData.GetProperty("delta");
                        var deltaType = delta.GetProperty("type").GetString();

                        if (deltaType == "text_delta" && delta.TryGetProperty("text", out var textContent))
                        {
                            var text = textContent.GetString()!;
                            if (mode != IChatbotProvider.Answer)
                            {
                                mode = IChatbotProvider.Answer;
                                yield return IChatbotProvider.Answer + text;
                            }
                            else
                            {
                                if (text.HasText())
                                    yield return text;
                            }
                        }
                        else if (deltaType == "input_json_delta" && delta.TryGetProperty("partial_json", out var partialJson))
                        {
                            var args = partialJson.GetString()!;
                            if (mode != IChatbotProvider.ToolCall)
                            {
                                yield return IChatbotProvider.ToolCall + toolName!;
                                if (args.HasText())
                                    yield return args;
                            }
                            else
                            {
                                if (args.HasText())
                                    yield return args;
                            }
                        }
                        break;

                    case "message_stop":
                        yield break;
                }
            }
        }
    }
}
