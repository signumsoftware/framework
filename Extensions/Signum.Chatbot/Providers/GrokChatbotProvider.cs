using System.Net.Http;
using System.Net.Http.Headers;

namespace Signum.Chatbot.Providers;

public class GrokChatbotProvider : ChatbotProviderBase
{
    protected override HttpClient GetClient(bool stream = false)
    {
        var apiKey = ChatbotLogic.GetConfig().GrokAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Grok configured!");

        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.x.ai"),
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        if (stream)
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        else
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
}
