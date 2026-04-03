using Microsoft.Extensions.AI;
using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Agent.Skills;
using Signum.Utilities.Synchronization;
using System.Text.Json;

namespace Signum.Agent;

public static class ChatbotLogic
{
    [AutoExpressionField]
    public static IQueryable<ChatMessageEntity> Messages(this ChatSessionEntity session) =>
        As.Expression(() => Database.Query<ChatMessageEntity>().Where(a => a.ChatSession.Is(session)));

    [AutoExpressionField]
    public static decimal? Price(this ChatMessageEntity message) =>
        As.Expression(() =>
            message.LanguageModel == null ? (decimal?)null :
            (message.LanguageModel.Entity.PricePerInputToken == null &&
             message.LanguageModel.Entity.PricePerOutputToken == null &&
             message.LanguageModel.Entity.PricePerCachedInputToken == null &&
             message.LanguageModel.Entity.PricePerReasoningOutputToken == null)
            ? (decimal?)null
            : (
                (decimal)(message.InputTokens ?? 0) * (message.LanguageModel.Entity.PricePerInputToken ?? 0) +
                (decimal)(message.OutputTokens ?? 0) * (message.LanguageModel.Entity.PricePerOutputToken ?? 0) +
                (decimal)(message.CachedInputTokens ?? 0) * (message.LanguageModel.Entity.PricePerCachedInputToken ?? 0) +
                (decimal)(message.ReasoningOutputTokens ?? 0) * (message.LanguageModel.Entity.PricePerReasoningOutputToken ?? 0)
              ) / 1_000_000m
        );

    [AutoExpressionField]
    public static decimal? TotalPrice(this ChatSessionEntity session) =>
        As.Expression(() => session.Messages().Sum(m => m.Price()));

    public static void Start(SchemaBuilder sb, Func<ChatbotConfigurationEmbedded> config)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        LanguageModelLogic.Start(sb, config);

        sb.Include<ChatSessionEntity>()
           .WithDelete(ChatSessionOperation.Delete)
           .WithQuery(() => e => new
           {
               Entity = e,
               e.Id,
               e.Title,
               e.User,
               e.StartDate,
               e.TotalInputTokens,
               e.TotalReasoningOutputTokens,
               e.TotalOutputTokens,
               e.TotalToolCalls,
               e.LanguageModel,
               TotalPrice = e.TotalPrice(),
           });

        sb.Schema.EntityEvents<ChatSessionEntity>().PreUnsafeDelete += query =>
        {
            query.SelectMany(a => a.Messages()).UnsafeDelete();
            return null;
        };

        sb.Include<ChatMessageEntity>()
            .WithIndex(a => new { a.ChatSession, a.CreationDate })
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Role,
                e.ToolID,
                e.Content,
                e.Exception,
                e.ChatSession,
            });

        new Graph<ChatMessageEntity>.Delete(ChatMessageOperation.Delete)
        {
            CanDelete = m => m.Is(Database.Query<ChatMessageEntity>().Where(a => a.ChatSession.Is(m.ChatSession)).OrderByDescending(a => a.CreationDate).Select(a => a.ToLite()).First()) ? null : ChatbotMessage.MessageMustBeTheLastToDelete.NiceToString(),
            Delete = (e, _) => { e.Delete(); },
        }.Register();

        QueryLogic.Expressions.Register((ChatMessageEntity cm) => cm.Price(), ChatbotMessage.Price);
        QueryLogic.Expressions.Register((ChatSessionEntity cm) => cm.TotalPrice(), ChatbotMessage.TotalPrice);

        PermissionLogic.RegisterTypes(typeof(ChatbotPermission));
    }

    public static void RegisterUserTypeCondition(TypeConditionSymbol userEntities)
    {
        TypeConditionLogic.RegisterCompile<ChatSessionEntity>(userEntities, cm => cm.User.Entity.Is(UserEntity.Current));
        TypeConditionLogic.RegisterCompile<ChatMessageEntity>(userEntities, cm => cm.ChatSession.Entity.InCondition(userEntities));
    }

    public static async Task<string> SumarizeConversation(List<ChatMessageEntity> messagesToSummarize, ChatbotLanguageModelEntity languageModel, CancellationToken ct)
    {
        var conversationText = new StringBuilder();
        foreach (var msg in messagesToSummarize)
        {
            if (msg.Role == ChatMessageRole.System)
                continue;

            var roleName = msg.Role switch
            {
                ChatMessageRole.User => "User",
                ChatMessageRole.Assistant => "Assistant",
                ChatMessageRole.Tool => $"Tool({msg.ToolID})",
                _ => msg.Role.ToString()
            };

            var content = msg.Content?.Etc(500) ?? (msg.Exception != null ? "[error]" : "[empty]");
            conversationText.AppendLine($"{roleName}: {content}");
        }

        var skill = AgentSkillLogic.ConversationSumarizerSkill;
        var prompt = skill.GetInstruction(conversationText.ToString());
        var client = LanguageModelLogic.GetChatClient(languageModel);
        var options = LanguageModelLogic.ChatOptions(languageModel, []);
        var cr = await client.GetResponseAsync(prompt, options, cancellationToken: ct);
        return cr.Text;
    }

    public static async Task<string> SumarizeTitle(ConversationHistory history, CancellationToken ct)
    {
        var prompt = AgentSkillLogic.QuestionSumarizerSkill.GetInstruction(history);
        var client = LanguageModelLogic.GetChatClient(history.LanguageModel);
        var options = LanguageModelLogic.ChatOptions(history.LanguageModel, []);
        var cr = await client.GetResponseAsync(prompt, options, cancellationToken: ct);
        return cr.Text;
    }

    public static async Task RunAgentLoopAsync(ConversationHistory history, IAgentOutput output, CancellationToken ct)
    {
        var client = LanguageModelLogic.GetChatClient(history.LanguageModel);

        while (true)
        {
            if (history.LanguageModel.MaxTokens != null &&
                history.Messages.Skip(1).LastOrDefault()?.InputTokens > history.LanguageModel.MaxTokens * 0.8)
            {
                var systemMsg = history.Messages.FirstEx();
                if (systemMsg.Role != ChatMessageRole.System)
                    throw new InvalidOperationException("First message is expected to be system");

                var normalMessages = history.Messages.Skip(1).ToList();
                var toKeepIndex = normalMessages.FindLastIndex(a => a.InputTokens < history.LanguageModel.MaxTokens * 0.5)
                    .NotFoundToNull() ?? (normalMessages.Count - 1);

                var toSumarize = normalMessages.Take(toKeepIndex).ToList();
                var toKeep = normalMessages.Skip(toKeepIndex).ToList();

                var summaryContent = await SumarizeConversation(toSumarize, history.LanguageModel, ct);

                var summary = new ChatMessageEntity
                {
                    ChatSession = history.Session,
                    Role = ChatMessageRole.System,
                    Content = $"## Summary of earlier conversation\n{summaryContent}\n\n---\nRecent messages follow:",
                }.Save();

                await output.OnSummarizationAsync(summary, ct);
                history.Messages = [systemMsg, summary, .. toKeep];
            }

            var tools = history.GetTools();
            var options = LanguageModelLogic.ChatOptions(history.LanguageModel, tools);
            var messages = history.GetMessages();
            LanguageModelLogic.GetProvider(history.LanguageModel).CustomizeMessagesAndOptions(messages, options);

            List<ChatResponseUpdate> updates = [];
            var sw = Stopwatch.StartNew();
            bool assistantStarted = false;

            await foreach (var update in client.GetStreamingResponseAsync(messages, options, ct))
            {
                if (!assistantStarted)
                {
                    await output.OnAssistantStartedAsync(ct);
                    assistantStarted = true;
                }
                updates.Add(update);
                if (update.Text.HasText())
                    await output.OnTextChunkAsync(update.Text, ct);
            }
            sw.Stop();

            var response = updates.ToChatResponse();
            var responseMsg = response.Messages.SingleEx();

            var notSupported = responseMsg.Contents
                .Where(a => a is not FunctionCallContent and not Microsoft.Extensions.AI.TextContent)
                .ToList();
            if (notSupported.Any())
                throw new InvalidOperationException("Unexpected response: " + notSupported.ToString(a => a.GetType().Name, ", "));

            var usage = response.Usage;
            var toolCalls = responseMsg.Contents.OfType<FunctionCallContent>().ToList();

            var uiToolCalls = toolCalls.Where(fc =>
            {
                var tool = history.RootSkill?.FindTool(fc.Name)
                    ?? throw new InvalidOperationException($"Tool '{fc.Name}' not found");
                return ((AIFunction)tool).UnderlyingMethod?.GetCustomAttribute<UIToolAttribute>() != null;
            }).ToList();

            if (uiToolCalls.Count > 1)
                throw new InvalidOperationException(
                    $"The LLM invoked more than one UITool in a single response ({string.Join(", ", uiToolCalls.Select(t => t.Name))}). Only one UITool can be active at a time.");

            var answer = new ChatMessageEntity
            {
                ChatSession = history.Session,
                Role = ChatMessageRole.Assistant,
                Content = responseMsg.Text,
                LanguageModel = history.Session.InDB(s => s.LanguageModel),
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

            Expression<Func<int?, int?, int?>> NullableAdd = (a, b) =>
                a == null && b == null ? null : (a ?? 0) + (b ?? 0);

            history.Session.InDB().UnsafeUpdate()
                .Set(a => a.TotalInputTokens, a => NullableAdd.Evaluate(a.TotalInputTokens, answer.InputTokens))
                .Set(a => a.TotalCachedInputTokens, a => NullableAdd.Evaluate(a.TotalCachedInputTokens, answer.CachedInputTokens))
                .Set(a => a.TotalOutputTokens, a => NullableAdd.Evaluate(a.TotalOutputTokens, answer.OutputTokens))
                .Set(a => a.TotalReasoningOutputTokens, a => NullableAdd.Evaluate(a.TotalReasoningOutputTokens, answer.ReasoningOutputTokens))
                .Set(a => a.TotalToolCalls, a => a.TotalToolCalls + answer.ToolCalls.Count)
                .Execute();

            await output.OnAssistantMessageAsync(answer, ct);
            history.Messages.Add(answer);

            if (toolCalls.IsEmpty() || uiToolCalls.Any())
                break;

            foreach (var funCall in toolCalls)
                await ExecuteToolAsync(history, funCall.Name, funCall.CallId, funCall.Arguments!, output, ct);
        }

        if (history.SessionTitle == null || history.SessionTitle.StartsWith("!*$"))
        {
            history.SessionTitle = history.Session.InDB(a => a.Title);
            if (history.SessionTitle == null || history.SessionTitle.StartsWith("!*$"))
            {
                string title = await SumarizeTitle(history, ct);
                if (title.HasText() && title.ToLower() != "pending")
                {
                    history.Session.InDB().UnsafeUpdate(a => a.Title, a => title);
                    history.SessionTitle = title;
                    await output.OnTitleUpdatedAsync(title, ct);
                }
            }
        }
    }

    public static async Task ExecuteToolAsync(
        ConversationHistory history,
        string toolId, string callId,
        IDictionary<string, object?> arguments,
        IAgentOutput output,
        CancellationToken ct)
    {
        await output.OnToolStartAsync(toolId, callId, ct);
        var toolSw = Stopwatch.StartNew();
        try
        {
            AITool tool = history.RootSkill?.FindTool(toolId)
                ?? throw new InvalidOperationException($"Tool '{toolId}' not found");
            var obj = await ((AIFunction)tool).InvokeAsync(new AIFunctionArguments(arguments), ct);
            toolSw.Stop();

            var toolMsg = new ChatMessageEntity
            {
                ChatSession = history.Session,
                Role = ChatMessageRole.Tool,
                ToolCallID = callId,
                ToolID = toolId,
                Content = JsonSerializer.Serialize(obj),
                Duration = toolSw.Elapsed,
            }.Save();

            await output.OnToolFinishedAsync(toolMsg, ct);
            history.Messages.Add(toolMsg);
        }
        catch (Exception e)
        {
            toolSw.Stop();
            var errorContent = FormatToolError(toolId, e, arguments);
            ChatMessageEntity toolMsg;
            using (AuthLogic.Disable())
            {
                toolMsg = new ChatMessageEntity
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
            await output.OnToolFinishedAsync(toolMsg, ct);
            history.Messages.Add(toolMsg);
        }
    }

    public static string FormatToolError(string toolName, Exception e, IDictionary<string, object?>? arguments)
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

    public static async Task<ConversationHistory> RunHeadlessAsync(
        string prompt,
        AgentUseCaseSymbol? useCase = null,
        Lite<ChatbotLanguageModelEntity>? languageModel = null,
        IAgentOutput? output = null,
        CancellationToken ct = default)
    {
        useCase ??= AgentUseCase.DefaultChatbot;
        output ??= NullAgentOutput.Instance;

        var modelLite = languageModel ?? LanguageModelLogic.DefaultLanguageModel.Value
            ?? throw new InvalidOperationException($"No default {nameof(ChatbotLanguageModelEntity)} configured.");

        var rootSkill = AgentSkillLogic.GetRootForUseCase(useCase)
            ?? throw new InvalidOperationException($"No active AgentSkillEntity with UseCase = {useCase.Key}.");

        var session = new ChatSessionEntity
        {
            LanguageModel = modelLite,
            User = UserEntity.Current,
            StartDate = Clock.Now,
        }.Save();

        var systemMsg = new ChatMessageEntity
        {
            Role = ChatMessageRole.System,
            ChatSession = session.ToLite(),
            Content = rootSkill.GetInstruction(null),
        }.Save();

        await output.OnSystemMessageAsync(systemMsg, ct);

        var userMsg = new ChatMessageEntity
        {
            Role = ChatMessageRole.User,
            ChatSession = session.ToLite(),
            Content = prompt,
        }.Save();

        await output.OnUserQuestionAsync(userMsg, ct);

        var history = new ConversationHistory
        {
            Session = session.ToLite(),
            LanguageModel = modelLite.RetrieveFromCache(),
            RootSkill = rootSkill,
            Messages = [systemMsg, userMsg],
        };

        await RunAgentLoopAsync(history, output, ct);
        return history;
    }
}

public interface IAgentOutput
{
    Task OnSystemMessageAsync(ChatMessageEntity msg, CancellationToken ct) => Task.CompletedTask;
    Task OnUserQuestionAsync(ChatMessageEntity msg, CancellationToken ct) => Task.CompletedTask;
    Task OnSummarizationAsync(ChatMessageEntity summaryMsg, CancellationToken ct) => Task.CompletedTask;
    Task OnAssistantStartedAsync(CancellationToken ct) => Task.CompletedTask;
    Task OnTextChunkAsync(string chunk, CancellationToken ct) => Task.CompletedTask;
    Task OnAssistantMessageAsync(ChatMessageEntity msg, CancellationToken ct) => Task.CompletedTask;
    Task OnToolStartAsync(string toolId, string callId, CancellationToken ct) => Task.CompletedTask;
    Task OnToolFinishedAsync(ChatMessageEntity toolMsg, CancellationToken ct) => Task.CompletedTask;
    Task OnTitleUpdatedAsync(string title, CancellationToken ct) => Task.CompletedTask;
}

public sealed class NullAgentOutput : IAgentOutput
{
    public static readonly NullAgentOutput Instance = new();
    private NullAgentOutput() { }
}

public class ConversationHistory
{
    public Lite<ChatSessionEntity> Session;
    public ChatbotLanguageModelEntity LanguageModel;
    public List<ChatMessageEntity> Messages;
    public string? SessionTitle { get; internal set; }
    public AgentSkillCode? RootSkill { get; set; }

    public List<ChatMessage> GetMessages() =>
        Messages.Select(ToChatMessage).ToList();

    ChatMessage ToChatMessage(ChatMessageEntity c)
    {
        var role = ToChatRole(c.Role);

        var content = c.Content ?? (c.Exception != null
            ? $"{c.Exception.Entity.ExceptionType}:\n{c.Exception.Entity.ExceptionMessage}"
            : null);

        if (c.Role == ChatMessageRole.Tool)
            return new ChatMessage(role, [new FunctionResultContent(c.ToolCallID!, content)]);

        if (c.ToolCalls.IsEmpty())
            return new ChatMessage(role, content);

        var contents = c.ToolCalls.Select(c =>
        {
            var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(c.Arguments)!;
            var cleanArguments = arguments.ToDictionary(kvp => kvp.Key, kvp => CleanValue(kvp.Value));
            return (AIContent)new FunctionCallContent(c.CallId, c.ToolId, cleanArguments);
        }).ToList();

        if (content.HasText())
            contents.Insert(0, new TextContent(content));

        return new ChatMessage(role, contents);
    }

    static object? CleanValue(object? value)
    {
        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => je.GetString()!,
                JsonValueKind.Number => je.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => je
            };
        }
        return value;
    }

    public List<AITool> GetTools()
    {
        if (RootSkill == null)
            return [];

        var activatedSkills = new HashSet<string>(
            RootSkill.GetEagerSkillsRecursive().Select(s => s.Name));

        foreach (var m in Messages)
        {
            if (m.Role == ChatMessageRole.Assistant)
            {
                foreach (var tc in m.ToolCalls)
                {
                    if (tc.ToolId == nameof(IntroductionSkill.Describe))
                    {
                        try
                        {
                            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tc.Arguments);
                            if (args != null && args.TryGetValue("skillName", out var sn))
                            {
                                var skillName = sn.GetString()!;
                                var newSkill = RootSkill.FindSkill(skillName);
                                if (newSkill != null)
                                    foreach (var s in newSkill.GetEagerSkillsRecursive())
                                        activatedSkills.Add(s.Name);
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        return activatedSkills
            .Select(name => RootSkill.FindSkill(name))
            .OfType<AgentSkillCode>()
            .SelectMany(skill => skill.GetTools())
            .ToList();
    }

    ChatRole ToChatRole(ChatMessageRole role) => role switch
    {
        ChatMessageRole.System => ChatRole.System,
        ChatMessageRole.User => ChatRole.User,
        ChatMessageRole.Assistant => ChatRole.Assistant,
        ChatMessageRole.Tool => ChatRole.Tool,
        _ => throw new InvalidOperationException($"Unexpected {nameof(ChatMessageRole)} {role}"),
    };
}
