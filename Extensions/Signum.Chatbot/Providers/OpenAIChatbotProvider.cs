using System.Net.Http;
using System.Net.Http.Headers;

namespace Signum.Chatbot.Providers;

public class OpenAIChatbotProvider : ChatbotProviderBase
{
    protected override HttpClient GetClient(bool stream = false)
    {
        var apiKey = ChatbotLogic.GetConfig().OpenAIAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for OpenAI configured!");

        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/"),
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        if (stream)
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        else
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
}
