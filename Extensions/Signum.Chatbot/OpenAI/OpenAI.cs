using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using static Signum.Chatbot.OpenAI.MistralChatbotProvider;

namespace Signum.Chatbot.OpenAI;

public class OpenAIChatbotProvider : IChatbotProvider
{
    string BaseUrl = "https://api.openai.com/v1/chat/completions";

    private static HttpClient OpenAIChatClient(bool stream = false)
    {
        var apiKey = ChatbotLogic.GetConfig().OpenAIAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Mistral configured!");

        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);


        return client;

    }

    public async IAsyncEnumerable<string> AskQuestionAsync(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    {
        var client = OpenAIChatClient();

        var payload = new
        {
            model = model.Model,
            messages = messages.Select(c => new { role = ToOpenAIRole(c.Role), content = c.Content }).ToArray(),
            stream = true,
            temperature = model.Temperature,
            max_tokens = 1024,
        };
    

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(BaseUrl, content, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var json = line.Substring("data: ".Length);
                if (json == "[DONE]") break;

               
                dynamic parsed = JsonConvert.DeserializeObject(json)!;

                string? token = parsed?.choices[0]?.delta?.content;

                if (!string.IsNullOrEmpty(token))
                {
                    yield return token;
                    await Task.Delay(50);
                }
            }
        }
    }

    public async Task<string?> GetAgentAsync(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    {
        var client = OpenAIChatClient();

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
            messages = messages.Select(c => new { role = ToOpenAIRole(c.Role), content = c.Content }).ToArray(),
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
        return new string[] { "" };
    }


    public string[] GetModelVersions(string name)
    {
        return new string[] { "" };
    }

    private static string ToOpenAIRole(ChatMessageRole role) => role switch
    {
        ChatMessageRole.System => "system",
        ChatMessageRole.User => "user",
        ChatMessageRole.Assistant => "assistant",
        ChatMessageRole.Tool => "function",
        _ => throw new UnexpectedValueException(role)
    };
}
