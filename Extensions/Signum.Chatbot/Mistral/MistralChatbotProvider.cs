using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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


    public async IAsyncEnumerable<string> AskQuestionAsync(ConversationHistory history, CancellationToken ct)
    {
        List<ChatMessageEntity> answers = new List<ChatMessageEntity>();

        Lite<ChatSessionEntity> session = history.Session.ToLite();

        var configuration = session.InDB(s => s.LanguageModel).RetrieveAndRemember();

        var client = LeChatClient();

        history.Chats.Insert(0, new ChatMessageEntity() 
        { 
            Role = ChatMessageRole.System, 
            ChatSession = session, 
            DateTime = DateTime.Now,  
            Message = "Bitte gib alle Formeln in LaTeX - Schreibweise zurück. Beispiel: $E_0=mc^2$ also nur mit $ Schreibweise. Keine anderen Formatierungen"
        });

        var payload = new
        {
            model =  "mistral-medium-latest",
            messages = history.Chats.Select(c => new { role = ToMistalRole(c.Role), content = c.Message }).ToArray(),
            stream = true,
            temperature =  0.8,
            max_tokens = 1024
        };
        
        var responseClient = await client.PostAsJsonAsync(BaseUrl, payload);
        responseClient.EnsureSuccessStatusCode();

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

        var answerChat = new ChatMessageEntity()
        {
            ChatSession = history.Session.ToLite(),
            Message = answer,
            DateTime = DateTime.Now,
            Role = ChatMessageRole.Assistant,
        }.Save();

        history.Chats.Add(answerChat);

        if (history.Session.Title == null)
        {
            payload = new
            {
                model = "mistral-medium-latest",
                messages = new[]
                {
                    new { role = "system", content = "Fasse das Thema dieses Gesprächs in maximal 6 Wörtern als Titel zusammen." },
                    new { role = "user", content = string.Join("\n", history.Chats.Skip(1).Select(m => m.Message)) }
                },
                stream = false,
                temperature = 0.5,
                max_tokens = 24
            };

            responseClient = await client.PostAsJsonAsync(BaseUrl, payload);
            responseClient.EnsureSuccessStatusCode();

            var result = await responseClient.Content.ReadFromJsonAsync<ChatCompletionResponse>();

            history.Session.Title = result != null ? result.choices[0].message.content : null;

            history.Session.Save();

        }

    }

    private string ToMistalRole(ChatMessageRole role) => role switch
    {
        ChatMessageRole.System => "system",
        ChatMessageRole.User => "user",
        ChatMessageRole.Assistant => "assistant",
        ChatMessageRole.Tool => "tool",
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
        var apiKey = ChatbotLanguageModelLogic.GetConfig().MistralAPIKey;

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
