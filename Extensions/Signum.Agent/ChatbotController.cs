using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Signum.Agent.Skills;
using Signum.API;
using Signum.API.Filters;
using Signum.Authorization;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace Signum.Agent;

public class ChatbotController : Controller
{
    [HttpGet("api/chatbot/provider/{providerKey}/models")]
    public async Task<List<string>> GetModels(string providerKey, CancellationToken token)
    {
        var symbol = SymbolLogic<LanguageModelProviderSymbol>.ToSymbol(providerKey);

        var list = await ChatbotLogic.GetModelNamesAsync(symbol, token);

        return list.Order().ToList();
    }

    [HttpGet("api/chatbot/provider/{providerKey}/embeddingModels")]
    public async Task<List<string>> GetEmbeddingModels(string providerKey, CancellationToken token)
    {
        var symbol = SymbolLogic<LanguageModelProviderSymbol>.ToSymbol(providerKey);

        var list = await ChatbotLogic.GetEmbeddingModelNamesAsync(symbol, token);
        
        return list.Order().ToList();
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
                await resp.WriteAsync(UINotification(ChatbotUICommand.MessageId, init.Id.ToString()), ct);
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
                if (history.ConversationSummary == null && history.Messages.Count > ChatbotLogic.MaxContextMessages)
                {
                    var keepCount = ChatbotLogic.MaxContextMessages / 2;
                    var systemMsg = history.Messages.FirstOrDefault(m => m.Role == ChatMessageRole.System);
                    var nonSystemMessages = history.Messages.Where(m => m.Role != ChatMessageRole.System).ToList();
                    var messagesToSummarize = nonSystemMessages.Take(nonSystemMessages.Count - keepCount).ToList();
                    var recentMessages = nonSystemMessages.Skip(nonSystemMessages.Count - keepCount).ToList();

                    history.ConversationSummary = await ChatbotLogic.SumarizeConversation(messagesToSummarize, history.LanguageModel, ct);

                    var keptMessages = new List<ChatMessageEntity>();
                    if (systemMsg != null)
                        keptMessages.Add(systemMsg);
                    keptMessages.AddRange(recentMessages);
                    history.Messages = keptMessages;
                }

                var tools = history.GetTools();
                var options = ChatbotLogic.ChatOptions(history.LanguageModel, tools);

                var messages = history.GetMessages();
                List<ChatResponseUpdate> updates = [];
                var sw = Stopwatch.StartNew();
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
                sw.Stop();

                var response = updates.ToChatResponse();
                var responseMsg = response.Messages.SingleEx();


                var notSupported = responseMsg.Contents.Where(a => !(a is FunctionCallContent or Microsoft.Extensions.AI.TextContent)).ToList();

                if (notSupported.Any())
                    throw new InvalidOperationException("Unexpected response" + notSupported.ToString(a => a.GetType().Name, ", "));

                var usage = response.Usage;
                var inputTokens = (int?)usage?.InputTokenCount;
                var outputTokens = (int?)usage?.OutputTokenCount;

                var toolCalls = responseMsg.Contents.OfType<FunctionCallContent>().ToList();
                var answer = new ChatMessageEntity
                {
                    ChatSession = history.Session.ToLite(),
                    Role = ChatMessageRole.Assistant,
                    Content = responseMsg.Text,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    Duration = sw.Elapsed,
                    ToolCalls = toolCalls.Select(fc => new ToolCallEmbedded
                    {
                        ToolId = fc.Name,
                        CallId = fc.CallId,
                        Arguments = JsonSerializer.Serialize(fc.Arguments),
                    }).ToMList()
                }.Save();

                history.Session.TotalInputTokens += inputTokens ?? 0;
                history.Session.TotalOutputTokens += outputTokens ?? 0;
                history.Session.TotalToolCalls += toolCalls.Count;

                foreach (var item in answer.ToolCalls)
                {
                    await resp.WriteAsync("\n");
                    await resp.WriteAsync(UINotification(ChatbotUICommand.AssistantTool, item.ToolId + "/" + item.CallId), ct);
                    await resp.WriteAsync(item.Arguments, ct);
                }

                await resp.WriteAsync("\n");
                await resp.WriteAsync(UINotification(ChatbotUICommand.MessageId, answer.Id.ToString()), ct);
                await resp.Body.FlushAsync();

                history.Messages.Add(answer);

                if (toolCalls.IsEmpty())
                    break;

                foreach (var funCall in toolCalls)
                {
                    await resp.WriteAsync(UINotification(ChatbotUICommand.Tool, funCall.Name + "/" + funCall.CallId), ct);

                    var toolSw = Stopwatch.StartNew();
                    try
                    {
                        AITool tool = ChatbotSkillLogic.AllTools.Value.GetOrThrow(funCall.Name);
                        var obj = await ((AIFunction)tool).InvokeAsync(new AIFunctionArguments(funCall.Arguments), ct);
                        string toolResponse = JsonSerializer.Serialize(obj);
                        toolSw.Stop();
                        var toolMsg = new ChatMessageEntity()
                        {
                            ChatSession = history.Session.ToLite(),
                            Role = ChatMessageRole.Tool,
                            ToolCallID = funCall.CallId,
                            ToolID = funCall.Name,
                            Content = toolResponse,
                            Duration = toolSw.Elapsed,
                        }.Save();

                        await resp.WriteAsync(toolResponse, ct);
                        await resp.WriteAsync("\n");
                        await resp.WriteAsync(UINotification(ChatbotUICommand.MessageId, toolMsg.Id.ToString()), ct);
                        await resp.Body.FlushAsync();
                        history.Messages.Add(toolMsg);
                    }
                    catch (Exception e)
                    {
                        toolSw.Stop();
                        var errorContent = FormatToolError(funCall.Name, e, funCall.Arguments);

                        ChatMessageEntity toolMsg;
                        using (AuthLogic.Disable())
                        {
                            toolMsg = new ChatMessageEntity()
                            {
                                ChatSession = history.Session.ToLite(),
                                Role = ChatMessageRole.Tool,
                                ToolCallID = funCall.CallId,
                                ToolID = funCall.Name,
                                Content = errorContent,
                                Exception = e.LogException().ToLiteFat(),
                                Duration = toolSw.Elapsed,
                            }.Save();
                        }

                        await resp.WriteAsync(UINotification(ChatbotUICommand.Exception, toolMsg.Exception!.Id.ToString()), ct);
                        await resp.WriteAsync(errorContent, ct);
                        await resp.WriteAsync("\n");
                        await resp.WriteAsync(UINotification(ChatbotUICommand.MessageId, toolMsg.Id.ToString()), ct);
                        await resp.Body.FlushAsync();
                        history.Messages.Add(toolMsg);
                    }
                }
            }

            history.Session.Save();

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


    static string FormatToolError(string toolName, Exception e, IDictionary<string, object?>? arguments)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Tool '{toolName}' failed.");
        sb.AppendLine($"Arguments: {JsonSerializer.Serialize(arguments).Etc(300)}");
        sb.AppendLine($"Error: {e.GetType().Name}: {e.Message}");
        if (arguments != null && arguments.Count > 0)

        if (e.Data["Hint"] is string s)
            sb.AppendLine($"Hint: {s}");

        sb.AppendLine("Please review the error and try again with corrected arguments.");
        return sb.ToString();
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
    MessageId,
    AssistantAnswer,
    AssistantTool,
    Tool,
    Exception,
}
