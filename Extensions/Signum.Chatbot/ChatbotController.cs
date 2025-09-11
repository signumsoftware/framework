using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Signum.API;
using Signum.API.Filters;
using Signum.Authorization;
using Signum.Chatbot.Agents;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace Signum.Chatbot;

public class ChatbotController : Controller
{
    [HttpGet("api/chatbot/provider/{providerKey}/models")]
    public Task<List<string>> GetModels(string providerKey, CancellationToken token)
    {
        var symbol = SymbolLogic<ChatbotProviderSymbol>.ToSymbol(providerKey);

        return ChatbotLogic.GetModelNamesAsync(symbol, token);
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
        try
        {
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
                Schema.Current.AssertAllowed(typeof(ChatMessageEntity), true);

                var messages = AuthLogic.Disable().Using(() => Database.Query<ChatMessageEntity>()
                    .Where(c => c.ChatSession.Is(session))
                    .ExpandLite(a => a.Exception, ExpandLite.EntityEager)
                    .OrderBy(a => a.CreationDate)
                    .ToList());

                history = new ConversationHistory
                {
                    Session = session,
                    LanguageModel = session.LanguageModel.RetrieveFromCache(),
                    Messages = messages,
                };
            }

            ChatMessageEntity userQuestion = NewChatMessage(session.ToLite(), question, ChatMessageRole.User).Save();

            history.Messages.Add(userQuestion);

            await resp.WriteAsync(UINotification(ChatbotUICommand.QuestionId, userQuestion.Id.ToString()), ct);
            await resp.Body.FlushAsync();

            var client = ChatbotLogic.GetChatClient(history.LanguageModel);
            while (true)
            {
                var tools = history.GetTools();
                var options = ChatbotLogic.ChatOptions(history.LanguageModel, tools);

                var messages = history.GetMessages();
                List<ChatResponseUpdate> updates = [];
                await foreach (var update in client.GetStreamingResponseAsync(messages, options, ct))
                {
                    if (updates.Count == 0)
                        await resp.WriteAsync(UINotification(ChatbotUICommand.AssistantAnswer), ct);

                    updates.Add(update);
                    var text = update.Text;
                    if (text.HasText())
                    {
                        await resp.WriteAsync(text);
                        await resp.Body.FlushAsync();
                    }
                }

                var response = updates.ToChatResponse();
                var responseMsg = response.Messages.SingleEx();


                var notSupported = responseMsg.Contents.Where(a => !(a is FunctionCallContent or Microsoft.Extensions.AI.TextContent)).ToList();

                if (notSupported.Any())
                    throw new InvalidOperationException("Unexpected response" + notSupported.ToString(a => a.GetType().Name, ", "));

                var toolCalls = responseMsg.Contents.OfType<FunctionCallContent>().ToList();
                var answer = new ChatMessageEntity
                {
                    ChatSession = history.Session.ToLite(),
                    Role = ChatMessageRole.Assistant,
                    Content = responseMsg.Text,
                    ToolCalls = toolCalls.Select(fc => new ToolCallEmbedded
                    {
                        ToolId = fc.Name,
                        CallId = fc.CallId,
                        Arguments = JsonSerializer.Serialize(fc.Arguments),
                    }).ToMList()
                }.Save();

                foreach (var item in answer.ToolCalls)
                {
                    await resp.WriteAsync("\n");
                    await resp.WriteAsync(UINotification(ChatbotUICommand.AssistantTool, item.ToolId + "/" + item.CallId), ct);
                    await resp.WriteAsync(item.Arguments, ct);
                }

                await resp.WriteAsync("\n");
                await resp.WriteAsync(UINotification(ChatbotUICommand.AnswerId, answer.Id.ToString()), ct);
                await resp.Body.FlushAsync();

                history.Messages.Add(answer);

                if (toolCalls.IsEmpty())
                    break;

                foreach (var funCall in toolCalls)
                {
                    await resp.WriteAsync(UINotification(ChatbotUICommand.Tool, funCall.Name + "/" + funCall.CallId), ct);

                    try
                    {
                        AITool tool = ChatbotSkillLogic.AllTools.Value.GetOrThrow(funCall.Name);
                        var obj = await ((AIFunction)tool).InvokeAsync(new AIFunctionArguments(funCall.Arguments), ct);
                        string toolResponse = JsonSerializer.Serialize(obj);
                        var toolMsg = new ChatMessageEntity()
                        {
                            ChatSession = history.Session.ToLite(),
                            Role = ChatMessageRole.Tool,
                            ToolCallID = funCall.CallId,
                            ToolID = funCall.Name,
                            Content = toolResponse,
                        }.Save();

                        await resp.WriteAsync(toolResponse, ct);
                        await resp.WriteAsync("\n");
                        await resp.WriteAsync(UINotification(ChatbotUICommand.AnswerId, toolMsg.Id.ToString()), ct);
                        await resp.Body.FlushAsync();
                        history.Messages.Add(toolMsg);
                    }
                    catch (Exception e)
                    {
                        ChatMessageEntity toolMsg;
                        using (AuthLogic.Disable())
                        {
                            toolMsg = new ChatMessageEntity()
                            {
                                ChatSession = history.Session.ToLite(),
                                Role = ChatMessageRole.Tool,
                                ToolCallID = funCall.CallId,
                                ToolID = funCall.Name,
                                Exception = e.LogException().ToLiteFat(),
                            }.Save();
                        }

                        await resp.WriteAsync(UINotification(ChatbotUICommand.Exception, toolMsg.Exception!.Id.ToString()), ct);
                        await resp.WriteAsync(toolMsg.Exception!.ToString()!, ct);
                        await resp.WriteAsync("\n");
                        await resp.WriteAsync(UINotification(ChatbotUICommand.AnswerId, toolMsg.Id.ToString()), ct);
                        await resp.Body.FlushAsync();
                        history.Messages.Add(toolMsg);
                    }
                }
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
        catch (Exception e)
        {
            var ex = e.LogException().ToLiteFat();

            await resp.WriteAsync(UINotification(ChatbotUICommand.Exception, ex!.Id.ToString()), ct);
            await resp.WriteAsync(ex!.ToString()!, ct);
            await resp.WriteAsync("\n");
            await resp.Body.FlushAsync();
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
            Title = null,
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

    ChatMessageEntity NewChatMessage(Lite<ChatSessionEntity> session, string message, ChatMessageRole role)
    {
        var command = new ChatMessageEntity()
        {
            ChatSession = session,
            Role = role,
            Content = message,
        };

        return command;
    }
}

[InTypeScript(true)]
public enum ChatbotUICommand
{
    System,
    SessionId,
    SessionTitle,
    QuestionId,
    AnswerId,
    AssistantAnswer,
    AssistantTool,
    Tool,
    Exception,
}
