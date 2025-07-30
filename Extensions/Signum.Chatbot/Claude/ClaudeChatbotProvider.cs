using Signum.Chatbot.OpenAI;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using static Signum.Chatbot.OpenAI.MistralChatbotProvider;

namespace Signum.Chatbot.Claude;

public class ClaudeChatbotProvider : IChatbotProvider
{
    static string BaseUrl = "https://api.anthropic.com/v1/messages";

    public async IAsyncEnumerable<string> AskQuestionAsync(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    {
        List<ChatMessageEntity> answers = new List<ChatMessageEntity>();

        var client = ClaudeChatClient();

        var payload = new
        {
            model = model.Model,
            messages = messages.Select(c => new { role = ToClaudeRole(c.Role), content = c.Content }).ToArray(),
            stream = true,
            temperature = model.Temperature,
            max_tokens = 1024,
        };

        var responseClient = await client.PostAsJsonAsync(BaseUrl, payload);


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

    public async Task<string?> GetAgentAsync(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    {
        var client = ClaudeChatClient();

        var functionDefinition = new
        {
            name = "GetAgentPrompt",
            description = "Select the best Agent for the user question",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    agentName = new
                    {
                        type = "string",
                        description = "Name of the selected agent"
                    }
                },
                required = new[] { "agentName" }
            }
        };

        var payload = new
        {
            model = model.Model,
            messages = messages.Select(c => new { role = ToClaudeRole(c.Role), content = c.Content }).ToArray(),
            temperature = 0.4,
            max_tokens = 256,
            tools = new[]
            {
                new {
                    type = "function",
                    function = functionDefinition
                }
            },
            tool_choice = "auto"
        };

        var responseClient = await client.PostAsJsonAsync(BaseUrl, payload);

        responseClient.EnsureSuccessStatusCode();

        responseClient = await client.PostAsJsonAsync(BaseUrl, payload);
        responseClient.EnsureSuccessStatusCode();

        var result = await responseClient.Content.ReadFromJsonAsync<ChatCompletionResponse>();

        var agent = result != null ? result.choices[0].message.content : null;

        return agent;
    }


    public string[] GetModelNames()
    {
        return new string[] { "mistral-medium-latest" };
    }


    public string[] GetModelVersions(string name)
    {
        return new string[] { "1.0" };
    }


    private HttpClient ClaudeChatClient(bool stream = false)
    {
        var apiKey = ChatbotLogic.GetConfig().ClaudeAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Claude configured!");

        var client = new HttpClient();
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
