using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Signum.Authorization;
using Signum.Chatbot.Agents;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Security;
using static Signum.Utilities.StringDistance;

namespace Signum.Chatbot;

public class ChatbotController : Controller
{
    [HttpPost("api/session")]
    public ChatSessionEntity NewChatSession()
    {
        var configuration = Database.Query<ChatbotLanguageModelEntity>().Where( cb => cb.IsDefault).FirstEx().ToLite();

        var newChatSession = new ChatSessionEntity()
        {
            LanguageModel = configuration,
            StartDate = DateTime.Now,
            User = UserEntity.Current,
        }.Save();


        return newChatSession;
    }


    


    [HttpPost("api/askQuestionAsync")]
    public  async Task AskQuestionAsync(CancellationToken ct)
    {
        var resp = this.HttpContext.Response;
        var context = this.HttpContext;

        var sessionID = HttpContext.Request.Headers["X-Chatbot-Session-Id"].ToString();
        var message = Encoding.UTF8.GetString(HttpContext.Request.Body.ReadAllBytesAsync().Result);

        var session = sessionID.HasText() == false ||  sessionID == "undefined" ? new ChatSessionEntity
        {
            LanguageModel = ChatbotLogic.DefaultLanguageModel.Value ?? throw new InvalidOperationException($"No default {typeof(ChatbotLanguageModelEntity).Name}"),
            User = UserEntity.Current,
            StartDate = Clock.Now,
        }.Save() : Database.Query<ChatSessionEntity>().SingleEx(a => a.Id == PrimaryKey.Parse(sessionID, typeof(ChatSessionEntity)));

        ConversationHistory history;
        if (sessionID.HasText() == false || sessionID == "undefined") 
        {
            await resp.WriteAsync(UICommand("SessionId", session.Id.ToString()), ct);
            await resp.Body.FlushAsync();
            history = new ConversationHistory
            {
                Session = session,
                LanguageModel = session.LanguageModel.RetrieveFromCache(),
                Messages = new List<ChatMessageEntity>
                {
                    new ChatMessageEntity
                    {
                        Role = ChatMessageRole.System,
                        ChatSession = session.ToLite(),
                        Message = ChatbotAgentLogic.GetAgent(DefaultAgent.Introduction).GetPrompt(null)
                    }.Save()
                }
            };
        }
        else
        {
            history = new ConversationHistory
            {
                Session = session,
                LanguageModel = session.LanguageModel.RetrieveFromCache(),
                Messages = Database.Query<ChatMessageEntity>().Where(c => c.ChatSession.Is(session)).OrderBy(a=>a.CreationDate).ToList()
            };
        }

        var userQuestion = new ChatMessageEntity()
        {
            ChatSession = session.ToLite(),
            Role = ChatMessageRole.User,
            Message = message,
        }.Save();

        history.Messages.Add(userQuestion);

        await resp.WriteAsync(UICommand("QuestionId", userQuestion.Id.ToString()), ct);
        await resp.Body.FlushAsync();

        string answer = "";


        await foreach(var item in ChatbotLogic.AskQuestionAsync(history.GetMessages(), history.LanguageModel, ct))
        {
            await resp.WriteAsync(item);
            await resp.Body.FlushAsync();

            answer += item;
        }

        var answerChat = new ChatMessageEntity()
        {
            ChatSession = history.Session.ToLite(),
            Message = answer,
            Role = ChatMessageRole.Assistant,
        }.Save();

        history.Messages.Add(answerChat);

        if (history.Session.Title == null)
        {
            string title = await ChatbotLogic.SumarizeTitle(history, ct);
            if (title.HasText() && title.ToLower() != "pending")
            {
                history.Session.Title = title;
                history.Session.Save();
                await resp.WriteAsync(UICommand("SessionTitle", title), ct);
            }
        }
    }

   
    private string UICommand(string commandName, string payload)
    {
        if (payload.Contains("\n"))
            throw new InvalidOperationException("Payload has newlines!");

        return "§$%" + commandName + ":" + payload + "\n";
    }

    [HttpGet("api/session/{id}")]
    public Lite<ChatSessionEntity>? GetChatSessionById(int id)
    {
       return Database.Query<ChatSessionEntity>().Where(c => c.Id == id).Select( s => s.ToLite()).FirstOrDefault();
    }


    [HttpGet("api/messages/session/{id}")]
    public List<ChatMessageEntity> GetMessagesBySessionId(int id)
    {
        var messages = Database.Query<ChatMessageEntity>().Where(m => m.ChatSession.Id == id).OrderByDescending(cm => cm.CreationDate).ToList();

        return messages;
    }

    [HttpGet("api/userSessions")]
    public List<ChatSessionEntity>? GetUserSessions(int id)
    {
        var session = Database.Query<ChatSessionEntity>().Where(m => m.User.Is(UserEntity.Current)).OrderByDescending(s => s.StartDate).ToList();

        return session;
    }

    [HttpGet("api/lastSession")]
    public ChatSessionEntity? GetLastSession(int id)
    {
        var session = Database.Query<ChatSessionEntity>().Where(m => m.User.Is(UserEntity.Current)).OrderByDescending( s => s.StartDate).FirstOrDefault();

        return session;
    }
}
