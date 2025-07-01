using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Signum.Authorization;
using System.ComponentModel.DataAnnotations;
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
        var sessionID = HttpContext.Request.Headers["X-Chatbot-Session-Id"].ToString();
        var message = Encoding.UTF8.GetString(HttpContext.Request.Body.ReadAllBytesAsync().Result);

        var session = sessionID.HasText() ? new ChatSessionEntity
        {
            LanguageModel = ChatbotLanguageModelLogic.DefaultLanguageModel.Value ?? throw new InvalidOperationException($"No default {typeof(ChatbotLanguageModelEntity).Name}"),
            User = UserEntity.Current,
            StartDate = Clock.Now,
        }.Save() : Database.Query<ChatSessionEntity>().SingleEx(a => a.Id == PrimaryKey.Parse(sessionID, typeof(ChatSessionEntity)));


        var chatMessage = new ChatMessageEntity()
        {
            ChatSession = session.ToLite(),
            Role = ChatMessageRole.User,
            DateTime = DateTime.Now,
            Message = message,
        }.Save();

        var allChats = new List<ChatSessionEntity>();

        var history = new ConversationHistory
        {
            Session = session,
            Chats = Database.Query<ChatMessageEntity>().Where(c => c.ChatSession.Is(session)).ToList(),
            Model = session.LanguageModel.RetrieveFromCache(),
        };

        
        var resp = this.HttpContext.Response;

        var context = this.HttpContext;

        await resp.WriteAsync(UICommand("QuestionId", chatMessage.Id.ToString()), ct);
        await resp.Body.FlushAsync();


        await foreach(var item in ChatbotLanguageModelLogic.AskQuestionAsync(history, ct))
        {
            await resp.WriteAsync(item);
            await resp.Body.FlushAsync();
        }
    }


    private string UICommand(string commandName, string payload)
    {
        if (payload.Contains("\n"))
            throw new InvalidOperationException("Payload has newlines!");

        return "§$%" + commandName + ":" + payload + "\n";
    }

    [HttpGet("api/session/{id}")]
    public ChatSessionEntity? GetChatSessionById(int id)
    {
       return Database.Query<ChatSessionEntity>().Where(c => c.Id == id).FirstOrDefault();
    }


    [HttpGet("api/messages/session/{id}")]
    public List<ChatMessageEntity> GetMessagesBySessionId(int id)
    {
        var messages = Database.Query<ChatMessageEntity>().Where(m => m.ChatSession.Id == id).ToList();

        return messages;
    }

    [HttpGet("api/lastSession")]
    public ChatSessionEntity? GetLastSession(int id)
    {
        var session = Database.Query<ChatSessionEntity>().Where(m => m.User.Is(UserEntity.Current)).OrderByDescending( s => s.StartDate).FirstOrDefault();

        return session;
    }
}
