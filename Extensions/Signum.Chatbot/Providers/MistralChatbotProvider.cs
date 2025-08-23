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

public class MistralChatbotProvider : IChatbotProvider
{
    static string BaseUrl = "https://api.mistral.ai/";

    public async IAsyncEnumerable<string> AskStreaming(List<ChatMessage> messages, ChatbotLanguageModelEntity model, [EnumeratorCancellation] CancellationToken ct)
    {
        var client = LeChatClient();

        var payload = new
        {
            model =  model.Model,
            messages = messages.Select(c => new { role = ToMistalRole(c.Role), content = c.Content }).ToArray(),
            stream = true,
            temperature = model.Temperature,
            max_tokens = model.MaxTokens,
        };
        
        var responseClient = await client.PostAsJsonAsync("v1/chat/completions", payload);

      
        if(!responseClient.IsSuccessStatusCode)
        {
            var errorResult = await responseClient.Content.ReadAsStringAsync();
            throw new MistralException(errorResult);
        }
      

        var stream = await responseClient.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string answer = "";

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6);
                if (json == "[DONE]") break;

                
                var doc = JsonDocument.Parse(json);
                var token = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta")
                    .GetProperty("content")
                    .GetString();

                if (!string.IsNullOrEmpty(token))
                {
                    yield return token;
                    await Task.Delay(50);

                    answer = answer + token; 
                }
            }
        }
    }



    private string ToMistalRole(ChatMessageRole role) => role switch
    {
        ChatMessageRole.System => "system",
        ChatMessageRole.User => "user",
        ChatMessageRole.Assistant => "assistant",
        ChatMessageRole.Tool => "function",
        _ => throw new UnexpectedValueException(role)
    };
        

    public string[] GetModelNames()
    {
        var client = LeChatClient();

        var response = client.GetFromJsonAsync<JsonDocument>("/v1/models").ResultSafe();

        return response!.RootElement.GetProperty("data").EnumerateArray().Select(a => a.GetProperty("id").GetString()!).ToArray();
    }


    public string[] GetModelVersions(string name)
    {
        return new string[] { "1.0" };
    }


    private HttpClient LeChatClient(bool stream = false)
    {
        var apiKey = ChatbotLogic.GetConfig().MistralAPIKey;

        if(apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Mistral configured!");

        var client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
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


[Serializable]
public class MistralException : Exception
{
    public MistralException(string message) : base(message) { }
}
