using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Signum.Authorization;
using Signum.Chatbot.Agents;
using Signum.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Text.Json;
using static Signum.Utilities.StringDistance;

namespace Signum.Chatbot;

public class ChatbotController : Controller
{
    [HttpGet("api/chatbot/provider/{providerKey}/models")]
    public string[] GetModels(string providerKey)
    {
        var messages = ChatbotLogic.GetModelNames(SymbolLogic<ChatbotProviderSymbol>.ToSymbol(providerKey));

        return messages;
    }


    [HttpGet("api/chatbot/messages/{sessionID}")]
    public List<ChatMessageEntity> GetMessagesBySessionId(int sessionID)
    {
        var messages = Database.Query<ChatMessageEntity>().Where(m => m.ChatSession.Id == sessionID).OrderBy(cm => cm.CreationDate).ToList();

        return messages;
    }

    [HttpPost("api/chatbot/ask")]
    public  async Task AskQuestionAsync(CancellationToken ct)
    {
        var resp = this.HttpContext.Response;
        var context = this.HttpContext;

        string sessionID = HttpContext.Request.Headers["X-Chatbot-Session-Id"].ToString();
        string question = Encoding.UTF8.GetString(HttpContext.Request.Body.ReadAllBytesAsync().Result);

        var session = GetOrCreateSession(sessionID);

        ConversationHistory history;

        if (sessionID.HasText() == false || sessionID == "undefined") 
        {
            await resp.WriteAsync(UINotification(ChatbotUICommand.SessionId, session.Id.ToString()), ct);
            await resp.Body.FlushAsync();

            history = CreateNewConversationHistory(session);

            var init = history.Messages.SingleEx();

            await resp.WriteAsync(UINotification(ChatbotUICommand.System), ct);
            await resp.WriteAsync(init.Content!, ct);
            await resp.WriteAsync("\n", ct);
            await resp.WriteAsync(UINotification(ChatbotUICommand.AnswerId, init.Id.ToString()), ct);
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

        ChatMessageEntity userQuestion = NewChatMessage(session.ToLite(), question, ChatMessageRole.User).Save();

        history.Messages.Add(userQuestion);

        await resp.WriteAsync(UINotification(ChatbotUICommand.QuestionId, userQuestion.Id.ToString()), ct);
        await resp.Body.FlushAsync();

        while (true)
        {
            string lastAnswer = "";
            string? mode = null;
            string? tool_id = null;
            await foreach (var item in ChatbotLogic.AskStreaming(history.GetMessages(), history.LanguageModel, ct))
            {
                Debug.WriteLine(">> " + item);
                if (item.HasText())
                {
                    if (lastAnswer.Length == 0 && mode == null)
                    {
                        if (item.StartsWith(IChatbotProvider.ToolCall))
                        {
                            mode = IChatbotProvider.ToolCall;
                            tool_id = item.After(IChatbotProvider.ToolCall);
                            if(tool_id.IsNullOrEmpty())
                                throw new InvalidOperationException("Tool id cannot be empty");
                            await resp.WriteAsync(UINotification(ChatbotUICommand.AssistantTool, tool_id), ct);
                        }
                        else if (item.StartsWith(IChatbotProvider.Answer))
                        {
                            mode = IChatbotProvider.Answer;
                            lastAnswer = item.After(IChatbotProvider.Answer);
                            await resp.WriteAsync(UINotification(ChatbotUICommand.AssistantFinalAnswer), ct);
                            await resp.WriteAsync(lastAnswer);
                        }
                        else 
                            throw new InvalidOperationException("Unexpected start of answer: " + item);
                    }
                    else
                    {
                        lastAnswer += item;
                        await resp.WriteAsync(item);
                        await resp.Body.FlushAsync();
                    }
                }

            }
            Debug.WriteLine(">>!!Finish!!");

            ChatMessageEntity answer = NewChatMessage(history.Session.ToLite(), lastAnswer, ChatMessageRole.Assistant, tool_id).Save();

            history.Messages.Add(answer);
            await resp.WriteAsync("\n");
            await resp.WriteAsync(UINotification(ChatbotUICommand.AnswerId, answer.Id.ToString()), ct);
            await resp.Body.FlushAsync();

            if (tool_id == null)
                break;

            await resp.WriteAsync(UINotification(ChatbotUICommand.Tool, tool_id), ct);
            IChatbotTool tool = ChatbotSkillLogic.AllTools.Value.GetOrThrow(tool_id);
            string toolResponse = await tool.ExecuteTool(lastAnswer, ct); ;
            ChatMessageEntity responseMsg = NewChatMessage(history.Session.ToLite(), toolResponse, ChatMessageRole.Tool, toolId: tool_id).Save();


            history.Messages.Add(responseMsg);
            await resp.WriteAsync(responseMsg.Content!, ct);

            await resp.WriteAsync(UINotification(ChatbotUICommand.AnswerId, responseMsg.Id.ToString()), ct);
            await resp.Body.FlushAsync();
        }

        if (history.Session.Title == null || history.Session.Title.StartsWith("!*$"))
        {
            string title = await ChatbotLogic.SumarizeTitle(history, ct);
            if (title.HasText() && title.ToLower() != "pending")
            {
                history.Session.Title = title;
                history.Session.Save();
                await resp.WriteAsync(UINotification(ChatbotUICommand.SessionTitle, title), ct);
            }
        }
    }


    string UINotification(ChatbotUICommand commandName, string? payload = null)
    {
        if (payload == null)
            return "$!" + commandName + "\n";

        if (payload.Contains("\n"))
            throw new InvalidOperationException("Payload has newlines!");

        return "$!" + commandName + ":" + payload + "\n";
    }

    ChatSessionEntity GetOrCreateSession(string? sessionID)
    {
        return sessionID.HasText() == false || sessionID == "undefined" ? new ChatSessionEntity
        {
            LanguageModel = ChatbotLogic.DefaultLanguageModel.Value ?? throw new InvalidOperationException($"No default {typeof(ChatbotLanguageModelEntity).Name}"),
            User = UserEntity.Current,
            StartDate = Clock.Now,
            Title = "!*$Neuer Chat" + DateTime.Now,
        }.Save() : Database.Query<ChatSessionEntity>().SingleEx(a => a.Id == PrimaryKey.Parse(sessionID, typeof(ChatSessionEntity)));
    }

    ConversationHistory CreateNewConversationHistory(ChatSessionEntity session)
    {
        var intro = ChatbotSkillLogic.GetSkill<IntroductionSkill>();

        var history = new ConversationHistory
        {
            Session = session,
            LanguageModel = session.LanguageModel.RetrieveFromCache(),
            Messages = new List<ChatMessageEntity>
            {
                new ChatMessageEntity
                {
                    Role = ChatMessageRole.System,
                    ChatSession = session.ToLite(),
                    Content = intro.GetInstruction(null),
                }.Save()
            }
        };

        return history;
    }

    ChatMessageEntity NewChatMessage(Lite<ChatSessionEntity> session, string message, ChatMessageRole role, string? toolId = null)
    {
        var command = new ChatMessageEntity()
        {
            ChatSession = session,
            Role = role,
            ToolID = toolId,
            Content = message,
        };

        return command;
    }
}

public enum ChatbotUICommand
{
    System,
    SessionId,
    SessionTitle,
    QuestionId,
    AnswerId,
    AssistantFinalAnswer,
    AssistantTool,
    Tool,
}
