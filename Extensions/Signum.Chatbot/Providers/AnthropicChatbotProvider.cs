using System.Net.Http;

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
}
