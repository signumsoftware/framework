using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Models;
using Microsoft.Extensions.AI;
using Signum.Utilities.Synchronization;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Signum.Chatbot.Providers;

public class AnthropicChatbotProvider : IChatbotProvider
{
    public string[] GetModelNames()
    {
        var models = new AnthropicClient(null).Models.ListModelsAsync().ResultSafe();
        return models.Models.Select(models => models.Id).ToArray();
    }

    public IChatClient CreateChatClient()
    {
        var apiKey = ChatbotLogic.GetConfig().AnthropicAPIKey;

        if (apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Claude configured!");

        IChatClient client = new AnthropicClient(new APIAuthentication(apiKey)).Messages;

        return client;
    }
}
