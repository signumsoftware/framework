using Azure;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Signum.Chatbot.Providers;

public class GithubModelsChatbotProvider : IChatbotProvider
{
    public async Task<List<string>> GetModelNames(CancellationToken ct)
    {
        var url = "https://models.github.ai/catalog/models";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);

        var doc = JsonDocument.Parse(json);

        return doc.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()!).ToList();
    }

    public IChatClient CreateChatClient(ChatbotLanguageModelEntity model)
    {
        var openAIOptions = new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://models.github.ai/inference")
        };

        var token = GetToken();

        var client = new ChatClient(model.Model, new ApiKeyCredential(token), openAIOptions);

        return client.AsIChatClient();

    }

    static string GetToken()
    {
        var apiKey = ChatbotLogic.GetConfig().GithubModelsToken;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No Token for Github Models configured!");
        return apiKey;
    }
}
