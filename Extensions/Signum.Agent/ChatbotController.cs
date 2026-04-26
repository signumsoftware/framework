using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Signum.Agent.Skills;
using Signum.API;
using Signum.API.Filters;
using Signum.Authorization;
using System.Data;
using System.Diagnostics;
using System.Reflection;
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

    [HttpPost("api/chatbot/feedback/{messageId}")]
    public void SetFeedback(int messageId, [FromBody] SetFeedbackRequest request)
    {
        var message = Database.Retrieve<ChatMessageEntity>(messageId);

        if (message.Role != ChatMessageRole.Assistant)
            throw new InvalidOperationException("Feedback can only be set on Assistant messages.");

        message.UserFeedback = request.Feedback;
        message.UserFeedbackMessage = request.Feedback == UserFeedback.Negative ? request.Message : null;
        message.Save();
    }

    [HttpGet("api/chatbot/messages/{sessionID}")]
    public List<ChatMessageEntity> GetMessagesBySessionId(int sessionID)
    {
        var messages = Database.Query<ChatMessageEntity>().Where(m => m.ChatSession.Id == sessionID).OrderBy(cm => cm.CreationDate).ToList();

        return messages;
    }

    [HttpPost("api/chatbot/ask")]
    public async Task AskQuestionAsync(CancellationToken ct)
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

                using (AuthLogic.Disable())
                {
                    var systemAndSummaries = Database.Query<ChatMessageEntity>()
                        .Where(c => c.ChatSession.Is(session))
                        .ExpandLite(a => a.Exception, ExpandLite.EntityEager)
                        .Where(a => a.Role == ChatMessageRole.System)
                        .OrderBy(a => a.CreationDate)
                        .ToList();

                    var lastSystemDate = systemAndSummaries.Max(a => a.CreationDate); //Inial or Summary

                    var remainingMessages = Database.Query<ChatMessageEntity>()
                        .Where(c => c.ChatSession.Is(session))
                        .ExpandLite(a => a.Exception, ExpandLite.EntityEager)
                        .Where(a => a.Role != ChatMessageRole.System && a.CreationDate > lastSystemDate)
                        .OrderBy(a => a.CreationDate)
                        .ToList();

                    history = new ConversationHistory
                    {
                        Session = session.ToLite(),
                        SessionTitle = session.Title,
                        LanguageModel = session.LanguageModel.RetrieveFromCache(),
                        Messages = systemAndSummaries.Concat(remainingMessages).ToList(),
                    };
                }
            }

            // If this request is a UI reply, inject the tool result into history and skip adding a new user message
            string? uiReplyCallId = HttpContext.Request.Headers["X-Chatbot-UIReply-CallId"].ToString().DefaultToNull();
            string? uiReplyToolId = HttpContext.Request.Headers["X-Chatbot-UIReply-ToolId"].ToString().DefaultToNull();
            bool isRecover = HttpContext.Request.Headers["X-Chatbot-Recover"].ToString() == "true";

            if (uiReplyCallId != null && uiReplyToolId != null)
            {
                // Create the Tool message now that we have the client's result
                var toolMsg = new ChatMessageEntity()
                {
                    ChatSession = session.ToLite(),
                    Role = ChatMessageRole.Tool,
                    ToolCallID = uiReplyCallId,
                    ToolID = uiReplyToolId,
                    Content = question, // request body carries the JSON result
                }.Save();

                await resp.WriteAsync(UINotification(ChatbotUICommand.Tool, uiReplyToolId + "/" + uiReplyCallId), ct);
                await resp.WriteAsync(question, ct);
                await resp.WriteAsync("\n");
                await resp.WriteAsync(UINotification(ChatbotUICommand.MessageId, toolMsg.Id.ToString()), ct);
                await resp.Body.FlushAsync();
                history.Messages.Add(toolMsg);
            }
            else if (isRecover)
            {
                // Recover mode: body must be empty — we resume from the last saved state
                if (question.HasText())
                    throw new InvalidOperationException("Recover requests must have an empty body.");

                // Case C: last message in history is an Assistant with an unresponded regular tool call.
                // Re-execute that tool call so the agent loop can continue.
                var lastAssistant = history.Messages.LastOrDefault(m => m.Role == ChatMessageRole.Assistant);
                if (lastAssistant != null)
                {
                    var pendingToolCall = lastAssistant.ToolCalls
                        .Select(tc => tc)
                        .FirstOrDefault(tc => !tc.IsUITool &&
                            !history.Messages.Any(m => m.Role == ChatMessageRole.Tool && m.ToolCallID == tc.CallId));

                    if (pendingToolCall != null)
                    {
                        var parsedArgs = JsonSerializer.Deserialize<Dictionary<string, object?>>(pendingToolCall.Arguments) ?? new();
                        await ExecuteToolAsync(history, pendingToolCall.ToolId, pendingToolCall.CallId, parsedArgs, ct);
                    }
                }
                // Case D (User or Tool as last message): history is already correct — fall through to the agent loop.
            }
            else
            {
                ChatMessageEntity userQuestion = NewChatMessage(session.ToLite(), question, ChatMessageRole.User).Save();

                history.Messages.Add(userQuestion);

                await resp.WriteAsync(UINotification(ChatbotUICommand.QuestionId, userQuestion.Id.ToString()), ct);
                await resp.Body.FlushAsync();
            }

            var client = ChatbotLogic.GetChatClient(history.LanguageModel);
            while (true)
            {
                if (history.LanguageModel.MaxTokens != null && history.Messages.Skip(1).LastOrDefault()?.InputTokens > history.LanguageModel.MaxTokens * 0.8)
                {
                    var systemMsg = history.Messages.FirstEx();
                    if (systemMsg.Role != ChatMessageRole.System)
                        throw new InvalidOperationException("First message is expected to be system");
                    var normalMessages = history.Messages.Skip(1).ToList();
                    var toKeepIndex = normalMessages.FindLastIndex(a => a.InputTokens < history.LanguageModel.MaxTokens * 0.5).NotFoundToNull() ?? (normalMessages.Count - 1);
                    var toSumarize = normalMessages.Take(toKeepIndex).ToList();
                    var toKeep = normalMessages.Skip(toKeepIndex).ToList();

                    var summaryContent = await ChatbotLogic.SumarizeConversation(toSumarize, history.LanguageModel, ct);

                    var summary = new ChatMessageEntity
                    {
                        ChatSession = history.Session,
                        Role = ChatMessageRole.System,
                        Content = $"## Summary of earlier conversation\n{summaryContent}\n\n---\nRecent messages follow:",
                    }.Save();

                    await resp.WriteAsync(UINotification(ChatbotUICommand.System), ct);
                    await resp.WriteAsync(summary.Content!, ct);
                    await resp.WriteAsync("\n", ct);
                    await resp.WriteAsync(UINotification(ChatbotUICommand.MessageId, summary.Id.ToString()), ct);

                    history.Messages = [systemMsg, summary, .. toKeep];
                }

                var tools = history.GetTools();
                var options = ChatbotLogic.ChatOptions(history.LanguageModel, tools);

                var messages = history.GetMessages();
                ChatbotLogic.GetProvider(history.LanguageModel).CustomizeMessagesAndOptions(messages, options);
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

                var toolCalls = responseMsg.Contents.OfType<FunctionCallContent>().ToList();

                // Detect UITool calls — the server never invokes their bodies
                var uiToolCalls = toolCalls.Where(fc =>
                {
                    var tool = AgentSkillLogic.IntroductionSkill?.FindTool(fc.Name)
                        ?? throw new InvalidOperationException($"Tool '{fc.Name}' not found");
                    return ((AIFunction)tool).UnderlyingMethod?.GetCustomAttribute<UIToolAttribute>() != null;
                }).ToList();

                if (uiToolCalls.Count > 1)
                    throw new InvalidOperationException($"The LLM invoked more than one UITool in a single response ({string.Join(", ", uiToolCalls.Select(t => t.Name))}). Only one UITool can be active at a time.");

                var answer = new ChatMessageEntity
                {
                    ChatSession = history.Session,
                    Role = ChatMessageRole.Assistant,
                    Content = responseMsg.Text,
                    LanguageModel = session.LanguageModel,
                    InputTokens = (int?)usage?.InputTokenCount,
                    CachedInputTokens = (int?)usage?.CachedInputTokenCount,
                    OutputTokens = (int?)usage?.OutputTokenCount,
                    ReasoningOutputTokens = (int?)usage?.ReasoningTokenCount,
                    Duration = sw.Elapsed,
                    ToolCalls = toolCalls.Select(fc => new ToolCallEmbedded
                    {
                        ToolId = fc.Name,
                        CallId = fc.CallId,
                        Arguments = JsonSerializer.Serialize(fc.Arguments),
                        IsUITool = uiToolCalls.Any(u => u.CallId == fc.CallId),
                    }).ToMList()
                }.Save();


                Expression<Func<int?, int?, int?>> NullableAdd = (a, b) => a == null && b == null ? null : (a ?? 0) + (b ?? 0);

                history.Session.InDB().UnsafeUpdate()
                    .Set(a => a.TotalInputTokens, a => NullableAdd.Evaluate(a.TotalInputTokens, answer.InputTokens))
                    .Set(a => a.TotalCachedInputTokens, a => NullableAdd.Evaluate(a.TotalCachedInputTokens, answer.CachedInputTokens))
                    .Set(a => a.TotalOutputTokens, a => NullableAdd.Evaluate(a.TotalOutputTokens, answer.OutputTokens))
                    .Set(a => a.TotalReasoningOutputTokens, a => NullableAdd.Evaluate(a.TotalReasoningOutputTokens, answer.ReasoningOutputTokens))
                    .Set(a => a.TotalToolCalls, a => a.TotalToolCalls + answer.ToolCalls.Count)
                    .Execute();

                foreach (var item in answer.ToolCalls)
                {
                    await resp.WriteAsync("\n");
                    var cmd = item.IsUITool ? ChatbotUICommand.AssistantUITool : ChatbotUICommand.AssistantTool;
                    await resp.WriteAsync(UINotification(cmd, item.ToolId + "/" + item.CallId), ct);
                    await resp.WriteAsync(item.Arguments, ct);
                }

                await resp.WriteAsync("\n");
                await resp.WriteAsync(UINotification(ChatbotUICommand.MessageId, answer.Id.ToString()), ct);
                await resp.Body.FlushAsync();

                history.Messages.Add(answer);

                if (toolCalls.IsEmpty())
                    break;

                // If a UITool was invoked, close the stream — the client will resume via a new ask request
                if (uiToolCalls.Any())
                    goto doneWithTools;

                foreach (var funCall in toolCalls)
                    await ExecuteToolAsync(history, funCall.Name, funCall.CallId, funCall.Arguments!, ct);
            }

            doneWithTools:

            if (history.SessionTitle == null || history.SessionTitle.StartsWith("!*$"))
            {
                history.SessionTitle = history.Session.InDB(a => a.Title);
                if (history.SessionTitle == null || history.SessionTitle.StartsWith("!*$"))
                {
                    string title = await ChatbotLogic.SumarizeTitle(history, ct);
                    if (title.HasText() && title.ToLower() != "pending")
                    {
                        history.Session.InDB().UnsafeUpdate(a => a.Title, a => title);
                        await resp.WriteAsync(UINotification(ChatbotUICommand.SessionTitle, title), ct);
                    }
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


    async Task ExecuteToolAsync(ConversationHistory history, string toolId, string callId, IDictionary<string, object?> arguments, CancellationToken ct)
    {
        var resp = this.HttpContext.Response;

        await resp.WriteAsync(UINotification(ChatbotUICommand.Tool, toolId + "/" + callId), ct);

        var toolSw = Stopwatch.StartNew();
        try
        {
            AITool tool = AgentSkillLogic.IntroductionSkill?.FindTool(toolId)
                ?? throw new InvalidOperationException($"Tool '{toolId}' not found");
            var obj = await ((AIFunction)tool).InvokeAsync(new AIFunctionArguments(arguments), ct);
            toolSw.Stop();

            string toolResponse = JsonSerializer.Serialize(obj);
            var toolMsg = new ChatMessageEntity()
            {
                ChatSession = history.Session,
                Role = ChatMessageRole.Tool,
                ToolCallID = callId,
                ToolID = toolId,
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
            var errorContent = FormatToolError(toolId, e, arguments);

            ChatMessageEntity toolMsg;
            using (AuthLogic.Disable())
            {
                toolMsg = new ChatMessageEntity()
                {
                    ChatSession = history.Session,
                    Role = ChatMessageRole.Tool,
                    ToolCallID = callId,
                    ToolID = toolId,
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

    static string FormatToolError(string toolName, Exception e, IDictionary<string, object?>? arguments)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Tool '{toolName}' failed.");
        if (arguments != null && arguments.Count > 0)
            sb.AppendLine($"Arguments: {JsonSerializer.Serialize(arguments).Etc(300)}");
        sb.AppendLine($"Error: {e.GetType().Name}: {e.Message}");

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
        var intro = AgentSkillLogic.IntroductionSkill
            ?? throw new InvalidOperationException("IntroductionSkill not configured");

        var history = new ConversationHistory
        {
            Session = session.ToLite(),
            SessionTitle = session.Title,
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

public class SetFeedbackRequest
{
    public UserFeedback? Feedback { get; set; }
    public string? Message { get; set; }
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
    AssistantUITool,
    Tool,
    Exception,
}
