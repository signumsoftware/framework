using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Signum.Chatbot.Agents;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace Signum.Chatbot.OpenAI;

public class MistralChatbotProvider : IChatbotProvider
{
    static string BaseUrl = "https://api.mistral.ai/v1/chat/completions";


    public async IAsyncEnumerable<string> AskQuestionAsync(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    {
        List<ChatMessageEntity> answers = new List<ChatMessageEntity>();

        var client = LeChatClient();

        var payload = new
        {
            model =  model.Model,
            messages = messages.Select(c => new { role = ToMistalRole(c.Role), content = c.Content }).ToArray(),
            stream = true,
            temperature = model.Temperature,
            max_tokens = 1024,
        };
        
        var responseClient = await client.PostAsJsonAsync(BaseUrl, payload);

      
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


    public async Task<string?> GetAgentAsync(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    {
        var client = LeChatClient();

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
            model = "mistral-medium-latest",
            messages = messages.Select(c => new { role = ToMistalRole(c.Role), content = c.Content }).ToArray(),
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


    public async Task<string?> GenerateSessionTitle(List<ChatMessage> configMessages, List<ChatMessage> chatMessages, CancellationToken ct)
    {
        var client = LeChatClient();

        var payload = new
        {
            model = "mistral-medium-latest",
            messages = new[]
                {
                    new {
                        role = "system", content = "Gib alle mathematischen Ausdrücke und Formeln ausschließlich in LaTeX-Schreibweise zurück, " +
                                                   "wobei jede Formel in $-Zeichen eingeschlossen sein muss. Verwende keine anderen Formatierungen oder Zeichen für die Darstellung von Formeln außer den $-Zeichen. " +
                                                   "Achte darauf, dass keine zusätzlichen Zeichen wie Backticks oder andere Formatierungen vor oder nach den $-Zeichen stehen."
                    },
                    new { role = "system", content = "Fasse das Thema dieses Gesprächs in maximal 6 Wörtern als Titel zusammen. Keine Zeilenumbrüche oder Absätze verwenden. Eine Formel gilt als ein Wort." },
                    new { role = "user", content = string.Join("\n", chatMessages.Select(m => m.Content)) }
                },
            stream = false,
            temperature = 0.5,
            max_tokens = 24
        };

        var responseClient = await client.PostAsJsonAsync(BaseUrl, payload);


        responseClient.EnsureSuccessStatusCode();

        responseClient = await client.PostAsJsonAsync(BaseUrl, payload);
        responseClient.EnsureSuccessStatusCode();

        var result = await responseClient.Content.ReadFromJsonAsync<ChatCompletionResponse>();

       var title = result != null ? result.choices[0].message.content : null;

        return title;
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
        return new string[] { "mistral-medium-latest" };
    }


    public string[] GetModelVersions(string name)
    {
        return new string[] { "1.0" };
    }


    private  HttpClient LeChatClient(bool stream = false)
    {
        var apiKey = ChatbotLogic.GetConfig().MistralAPIKey;

        if(apiKey.IsNullOrEmpty())
            throw new InvalidOperationException("No API Key for Mistral configured!");

        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        if (stream)
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        return client;

    }

    public class ChatCompletionResponse
    {
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public LeChatMessage message { get; set; }
    }

    public class LeChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}


[Serializable]
public class MistralException : Exception
{
    public MistralException(string message) : base(message) { }
}
