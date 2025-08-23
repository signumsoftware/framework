using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using static Signum.Chatbot.Providers.MistralChatbotProvider;

namespace Signum.Chatbot.Providers;

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

    public async IAsyncEnumerable<string> AskStreaming(List<ChatMessage> messages, ChatbotLanguageModelEntity model, [EnumeratorCancellation]CancellationToken ct)
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
