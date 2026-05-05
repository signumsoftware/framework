using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Signum.Agent.Skills;
using Signum.API;
using Signum.API.Filters;
using Signum.Authorization;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Signum.Agent;

public class ChatbotController : Controller
{
    [HttpGet("api/agentSkill/skillCodeInfo/{skillCodeName}")]
    public DefaultSkillCodeInfo GetSkillCodeInfo(string skillCodeName) =>
        SkillCodeLogic.GetDefaultSkillCodeInfo(skillCodeName);

    [HttpGet("api/agentSkill/defaultAgentSkillCodeInfo/{agentName}")]
    public DefaultSkillCodeInfo GetDefaultAgentSkillCodeInfo(string agentName)
    {
        var agent = AgentLogic.RegisteredAgents.Keys.SingleOrDefault(a => a.Key == agentName)
            ?? throw new KeyNotFoundException($"Agent '{agentName}' is not registered.");
        return SkillCodeLogic.GetDefaultSkillCodeInfo(AgentLogic.RegisteredAgents[agent]());
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
    public List<ChatMessageEntity> GetMessagesBySessionId(int sessionID) =>
        Database.Query<ChatMessageEntity>()
            .Where(m => m.ChatSession.Id == sessionID)
            .OrderBy(cm => cm.CreationDate)
            .ToList();

    [HttpPost("api/chatbot/ask")]
    public async Task AskQuestionAsync(CancellationToken ct)
    {
        var resp = this.HttpContext.Response;
        var output = new HttpAgentOutput(resp);
        try
        {
            string sessionID = HttpContext.Request.Headers["X-Chatbot-Session-Id"].ToString();
            string question = Encoding.UTF8.GetString(HttpContext.Request.Body.ReadAllBytesAsync().Result);

            var session = GetOrCreateSession(sessionID);

            ConversationHistory history;

            if (sessionID.HasText() == false || sessionID == "undefined")
            {
                await resp.WriteAsync(output.Notification(ChatbotUICommand.SessionId, session.Id.ToString()), ct);
                await resp.Body.FlushAsync();

                history = CreateNewConversationHistory(session);

                var init = history.Messages.SingleEx();
                await output.OnSystemMessageAsync(init, ct);
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

                    var lastSystemDate = systemAndSummaries.Max(a => a.CreationDate);

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
                        RootSkill = AgentLogic.GetEffectiveSkillCode(DefaultAgent.Chatbot),
                        Messages = systemAndSummaries.Concat(remainingMessages).ToList(),
                    };
                }
            }

            string? uiReplyCallId = HttpContext.Request.Headers["X-Chatbot-UIReply-CallId"].ToString().DefaultToNull();
            string? uiReplyToolId = HttpContext.Request.Headers["X-Chatbot-UIReply-ToolId"].ToString().DefaultToNull();
            bool isRecover = HttpContext.Request.Headers["X-Chatbot-Recover"].ToString() == "true";

            if (uiReplyCallId != null && uiReplyToolId != null)
            {
                var toolMsg = new ChatMessageEntity
                {
                    ChatSession = session.ToLite(),
                    Role = ChatMessageRole.Tool,
                    ToolCallID = uiReplyCallId,
                    ToolID = uiReplyToolId,
                    Content = question,
                }.Save();

                await resp.WriteAsync(output.Notification(ChatbotUICommand.Tool, uiReplyToolId + "/" + uiReplyCallId), ct);
                await resp.WriteAsync(question, ct);
                await resp.WriteAsync("\n");
                await resp.WriteAsync(output.Notification(ChatbotUICommand.MessageId, toolMsg.Id.ToString()), ct);
                await resp.Body.FlushAsync();
                history.Messages.Add(toolMsg);
            }
            else if (isRecover)
            {
                if (question.HasText())
                    throw new InvalidOperationException("Recover requests must have an empty body.");

                var lastAssistant = history.Messages.LastOrDefault(m => m.Role == ChatMessageRole.Assistant);
                if (lastAssistant != null)
                {
                    var pendingToolCall = lastAssistant.ToolCalls
                        .FirstOrDefault(tc => !tc.IsUITool &&
                            !history.Messages.Any(m => m.Role == ChatMessageRole.Tool && m.ToolCallID == tc.CallId));

                    if (pendingToolCall != null)
                    {
                        var parsedArgs = JsonSerializer.Deserialize<Dictionary<string, object?>>(pendingToolCall.Arguments) ?? new();
                        await ChatbotLogic.ExecuteToolAsync(history, pendingToolCall.ToolId, pendingToolCall.CallId, parsedArgs, output, ct);
                    }
                }
            }
            else
            {
                var userQuestion = new ChatMessageEntity
                {
                    ChatSession = session.ToLite(),
                    Role = ChatMessageRole.User,
                    Content = question,
                }.Save();
                history.Messages.Add(userQuestion);
                await output.OnUserQuestionAsync(userQuestion, ct);
            }

            await ChatbotLogic.RunAgentLoopAsync(history, output, ct);
        }
        catch (Exception e)
        {
            var ex = e.LogException().ToLiteFat();
            await resp.WriteAsync(output.Notification(ChatbotUICommand.Exception, ex!.Id.ToString()), ct);
            await resp.WriteAsync(ex!.ToString()!, ct);
            await resp.WriteAsync("\n");
            await resp.Body.FlushAsync();
        }
    }

    ChatSessionEntity GetOrCreateSession(string? sessionID)
    {
        return sessionID.HasText() == false || sessionID == "undefined"
            ? new ChatSessionEntity
            {
                LanguageModel = LanguageModelLogic.DefaultLanguageModel.Value
                    ?? throw new InvalidOperationException($"No default {typeof(ChatbotLanguageModelEntity).Name}"),
                User = UserEntity.Current,
                StartDate = Clock.Now,
                Title = null,
            }.Save()
            : Database.Query<ChatSessionEntity>().SingleEx(a => a.Id == PrimaryKey.Parse(sessionID, typeof(ChatSessionEntity)));
    }

    ConversationHistory CreateNewConversationHistory(ChatSessionEntity session)
    {
        var rootSkill = AgentLogic.GetEffectiveSkillCode(DefaultAgent.Chatbot)
            ?? throw new InvalidOperationException("No active AgentSkillEntity with UseCase = DefaultChatbot");

        return new ConversationHistory
        {
            Session = session.ToLite(),
            SessionTitle = session.Title,
            LanguageModel = session.LanguageModel.RetrieveFromCache(),
            RootSkill = rootSkill,
            Messages = new List<ChatMessageEntity>
            {
                new ChatMessageEntity
                {
                    Role = ChatMessageRole.System,
                    ChatSession = session.ToLite(),
                    Content = rootSkill.GetInstruction(null),
                }.Save()
            }
        };
    }
}

public class HttpAgentOutput : IAgentOutput
{
    readonly HttpResponse _resp;

    public HttpAgentOutput(HttpResponse resp) => _resp = resp;

    public string Notification(ChatbotUICommand cmd, string? payload = null)
    {
        if (payload == null)
            return "$!" + cmd + "\n";

        if (payload.Contains('\n'))
            throw new InvalidOperationException("Payload has newlines!");

        return "$!" + cmd + ":" + payload + "\n";
    }

    public async Task OnSystemMessageAsync(ChatMessageEntity msg, CancellationToken ct)
    {
        await _resp.WriteAsync(Notification(ChatbotUICommand.System), ct);
        await _resp.WriteAsync(msg.Content!, ct);
        await _resp.WriteAsync("\n", ct);
        await _resp.WriteAsync(Notification(ChatbotUICommand.MessageId, msg.Id.ToString()), ct);
    }

    public async Task OnUserQuestionAsync(ChatMessageEntity msg, CancellationToken ct)
    {
        await _resp.WriteAsync(Notification(ChatbotUICommand.QuestionId, msg.Id.ToString()), ct);
        await _resp.Body.FlushAsync(ct);
    }

    public async Task OnSummarizationAsync(ChatMessageEntity msg, CancellationToken ct)
    {
        await _resp.WriteAsync(Notification(ChatbotUICommand.System), ct);
        await _resp.WriteAsync(msg.Content!, ct);
        await _resp.WriteAsync("\n", ct);
        await _resp.WriteAsync(Notification(ChatbotUICommand.MessageId, msg.Id.ToString()), ct);
    }

    public async Task OnAssistantStartedAsync(CancellationToken ct)
    {
        await _resp.WriteAsync(Notification(ChatbotUICommand.AssistantAnswer), ct);
    }

    public async Task OnTextChunkAsync(string chunk, CancellationToken ct)
    {
        await _resp.WriteAsync(chunk, ct);
        await _resp.Body.FlushAsync(ct);
    }

    public async Task OnAssistantMessageAsync(ChatMessageEntity msg, CancellationToken ct)
    {
        foreach (var item in msg.ToolCalls)
        {
            await _resp.WriteAsync("\n", ct);
            var cmd = item.IsUITool ? ChatbotUICommand.AssistantUITool : ChatbotUICommand.AssistantTool;
            await _resp.WriteAsync(Notification(cmd, item.ToolId + "/" + item.CallId), ct);
            await _resp.WriteAsync(item.Arguments, ct);
        }
        await _resp.WriteAsync("\n", ct);
        await _resp.WriteAsync(Notification(ChatbotUICommand.MessageId, msg.Id.ToString()), ct);
        await _resp.Body.FlushAsync(ct);
    }

    public async Task OnToolStartAsync(string toolId, string callId, CancellationToken ct)
    {
        await _resp.WriteAsync(Notification(ChatbotUICommand.Tool, toolId + "/" + callId), ct);
    }

    public async Task OnToolFinishedAsync(ChatMessageEntity toolMsg, CancellationToken ct)
    {
        if (toolMsg.Exception != null)
            await _resp.WriteAsync(Notification(ChatbotUICommand.Exception, toolMsg.Exception.Id.ToString()), ct);

        await _resp.WriteAsync(toolMsg.Content!, ct);
        await _resp.WriteAsync("\n", ct);
        await _resp.WriteAsync(Notification(ChatbotUICommand.MessageId, toolMsg.Id.ToString()), ct);
        await _resp.Body.FlushAsync(ct);
    }

    public async Task OnTitleUpdatedAsync(string title, CancellationToken ct)
    {
        await _resp.WriteAsync(Notification(ChatbotUICommand.SessionTitle, title), ct);
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
