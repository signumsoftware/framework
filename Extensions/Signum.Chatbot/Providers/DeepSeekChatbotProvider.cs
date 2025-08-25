using System.Net.Http;
using System.Net.Http.Headers;

namespace Signum.Chatbot.Providers;

public class DeepSeekChatbotProvider : ChatbotProviderBase
{
    protected override string GetMessagesUrl() => "chat/completions";

    protected override HttpClient GetClient(bool stream = false)
    {
        var apiKey = ChatbotLogic.GetConfig().DeepSeekAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for DeepSeek configured!");

        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.deepseek.com"),
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        if (stream)
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        else
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
}
