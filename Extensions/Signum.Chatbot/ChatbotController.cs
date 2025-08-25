using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Signum.Authorization;
using Signum.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Security;
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

    [HttpGet("api/chatbot/agent/{agentCodeKey}")]
    public AgentInfo GetAgentInfo(string agentCodeKey)
    { 
        var agent = ChatbotAgentLogic.GetAgent(SymbolLogic<ChatbotAgentCodeSymbol>.ToSymbol(agentCodeKey));

        return agent.ToInfo();
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

        var sessionID = HttpContext.Request.Headers["X-Chatbot-Session-Id"].ToString();
        var message = Encoding.UTF8.GetString(HttpContext.Request.Body.ReadAllBytesAsync().Result);

        var session = GetOrCreateSession(sessionID);

        ConversationHistory history;

        if (sessionID.HasText() == false || sessionID == "undefined") 
        {
            await resp.WriteAsync(UINotification("SessionId", session.Id.ToString()), ct);
            await resp.Body.FlushAsync();

            history = CreateNewConversationHistory(session);

            var init = history.Messages.SingleEx();

            await resp.WriteAsync(UINotification("System"), ct);
            await resp.WriteAsync(init.Message, ct);
            await resp.WriteAsync("\n", ct);
            await resp.WriteAsync(UINotification("AnswerId", init.Id.ToString()), ct);
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

        var userQuestion = NewChatMessage(session.ToLite(), message, ChatMessageRole.User).Save();

        history.Messages.Add(userQuestion);

        await resp.WriteAsync(UINotification("QuestionId", userQuestion.Id.ToString()), ct);
        await resp.Body.FlushAsync();

        string lastAnswer;

        while (true)
        {
            lastAnswer = "";
            bool isCommand = false;

            await foreach (var item in ChatbotLogic.AskStreaming(history.GetMessages(), history.LanguageModel, ct))
            {
                if (lastAnswer.Length == 0)
                {
                    if (item.StartsWith("$"))
                    {
                        isCommand = true;
                        await resp.WriteAsync(UINotification("AssistantTool"), ct);
                    }
                    else
                    {
                        await resp.WriteAsync(UINotification("AssistantFinalAnswer"), ct);
                    }
                }

                lastAnswer += item;

                await resp.WriteAsync(item);
                await resp.Body.FlushAsync();
            }

            var command = NewChatMessage(history.Session.ToLite(), lastAnswer, ChatMessageRole.Assistant, isCommand).Save();

            history.Messages.Add(command);
            await resp.WriteAsync("\n");
            await resp.WriteAsync(UINotification("AnswerId", command.Id.ToString()), ct);
            await resp.Body.FlushAsync();

            if (!isCommand)
                break;

            var parsedCommand = ChatbotAgentLogic.ParseCommand(lastAnswer);
            ChatMessageEntity responseMsg;
            if (parsedCommand.commandName == "Describe")
            {
                await resp.WriteAsync(UINotification("System"), ct);
                string describeResponse = await ChatbotAgentLogic.GetDescribe(parsedCommand.args);
                responseMsg = NewChatMessage(history.Session.ToLite(), describeResponse, ChatMessageRole.Tool, toolId: parsedCommand.commandName).Save();
            }
            else
            {
                await resp.WriteAsync(UINotification("Tool"), ct);
                string toolResponse = await ChatbotAgentLogic.EvaluateTool(parsedCommand.commandName, parsedCommand.args, ct);
                responseMsg = NewChatMessage(history.Session.ToLite(), toolResponse, ChatMessageRole.Tool, toolId: parsedCommand.commandName).Save();
            }   

            history.Messages.Add(responseMsg);
            await resp.WriteAsync(responseMsg.Message, ct);

            await resp.WriteAsync(UINotification("AnswerId", responseMsg.Id.ToString()), ct);
            await resp.Body.FlushAsync();
        }

        if (history.Session.Title == null || history.Session.Title.StartsWith("!*$"))
        {
            string title = await ChatbotLogic.SumarizeTitle(history, ct);
            if (title.HasText() && title.ToLower() != "pending")
            {
                history.Session.Title = title;
                history.Session.Save();
                await resp.WriteAsync(UINotification("SessionTitle", title), ct);
            }
        }
    }


    string UINotification(string commandName, string? payload = null)
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
                    Message = ChatbotAgentLogic.GetAgent(DefaultAgent.Introduction).LongDescriptionWithReplacements(null),
                }.Save()
            }
        };

        return history;
    }

    ChatMessageEntity NewChatMessage(Lite<ChatSessionEntity> session, string message, ChatMessageRole role, 
        bool isCommand = false, 
        string? toolId = null)
    {
        var command = new ChatMessageEntity()
        {
            ChatSession = session,
            Message = message,
            Role = role,
            IsToolCall = isCommand,
            ToolID = toolId,
        };

        return command;
    }



}
