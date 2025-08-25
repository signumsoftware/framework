using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Signum.Chatbot.Agents;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Synchronization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Signum.Chatbot.Providers;

public class MistralChatbotProvider : ChatbotProviderBase
{
    protected override HttpClient GetClient(bool stream = false)
    { 
        var apiKey = ChatbotLogic.GetConfig().MistralAPIKey;

        if(apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Mistral configured!");

        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.mistral.ai/"),
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", apiKey)
            }
        };

        if (stream)
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        return client;
    }
}
