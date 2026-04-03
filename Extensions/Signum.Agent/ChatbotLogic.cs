using Microsoft.Extensions.AI;
using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Agent.Skills;
using Signum.Agent.Providers;
using Signum.Utilities.Synchronization;
using System.Text.Json;
using Pgvector;

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

    public static ResetLazy<Dictionary<Lite<ChatbotLanguageModelEntity>, ChatbotLanguageModelEntity>> LanguageModels = null!;
    public static ResetLazy<Lite<ChatbotLanguageModelEntity>?> DefaultLanguageModel = null!;

    public static ResetLazy<Dictionary<Lite<EmbeddingsLanguageModelEntity>, EmbeddingsLanguageModelEntity>> EmbeddingsModels = null!;
    public static ResetLazy<Lite<EmbeddingsLanguageModelEntity>?> DefaultEmbeddingsModel = null!;

    public static Dictionary<LanguageModelProviderSymbol, IChatbotModelProvider> ChatbotModelProviders = new Dictionary<LanguageModelProviderSymbol, IChatbotModelProvider>
    {
        { LanguageModelProviders.OpenAI, new OpenAIProvider()},
        { LanguageModelProviders.Gemini, new GeminiProvider()},
        { LanguageModelProviders.Anthropic, new AnthropicProvider()},
        { LanguageModelProviders.GithubModels, new GithubModelsProvider()},
        { LanguageModelProviders.Mistral, new MistralProvider()},
        { LanguageModelProviders.Ollama, new OllamaProvider()},
        { LanguageModelProviders.DeepSeek, new DeepSeekProvider()},
    };

    public static Dictionary<LanguageModelProviderSymbol, IEmbeddingsProvider> EmbeddingsProviders = new Dictionary<LanguageModelProviderSymbol, IEmbeddingsProvider>
    {
        { LanguageModelProviders.OpenAI, new OpenAIProvider()},
        { LanguageModelProviders.Gemini, new GeminiProvider()},
        { LanguageModelProviders.GithubModels, new GithubModelsProvider()},
        { LanguageModelProviders.Mistral, new MistralProvider()},
        { LanguageModelProviders.Ollama, new OllamaProvider()},
    };

    public static Func<ChatbotConfigurationEmbedded> GetConfig;

    public static ChatbotLanguageModelEntity RetrieveFromCache(this Lite<ChatbotLanguageModelEntity> lite)
    {
        return LanguageModels.Value.GetOrThrow(lite);
    }

    public static EmbeddingsLanguageModelEntity RetrieveFromCache(this Lite<EmbeddingsLanguageModelEntity> lite)
    {
        return EmbeddingsModels.Value.GetOrThrow(lite);
    }

    public static void Start(SchemaBuilder sb, Func<ChatbotConfigurationEmbedded> config)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        GetConfig = config;

        SymbolLogic<LanguageModelProviderSymbol>.Start(sb, () => ChatbotModelProviders.Keys.Union(EmbeddingsProviders.Keys));

        sb.Include<ChatbotLanguageModelEntity>()
            .WithSave(ChatbotLanguageModelOperation.Save, (m, args) =>
            {
                if (!m.IsNew && Database.Query<ChatMessageEntity>().Any(a => a.LanguageModel.Is(m)))
                {
                    var inDb = m.InDB(a => new { a.Model, a.Provider });
                    if (inDb.Model != m.Model || !inDb.Provider.Is(m.Provider))
                    {
                        throw new ArgumentNullException(ChatbotMessage.UnableToChangeModelOrProviderOnceUsed.NiceToString());
                    }
                }
            })
            .WithUniqueIndex(a => a.IsDefault, a => a.IsDefault == true)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.IsDefault,
                e.Provider,
                e.Model,
                e.Temperature,
                e.MaxTokens,
                e.PricePerInputToken,
                e.PricePerOutputToken,
                e.PricePerCachedInputToken,
                e.PricePerReasoningOutputToken,
            });

        new Graph<ChatbotLanguageModelEntity>.Execute(ChatbotLanguageModelOperation.MakeDefault)
        {
            CanExecute = a => !a.IsDefault ? null : ValidationMessage._0IsSet.NiceToString(Entity.NicePropertyName(() => a.IsDefault)),
            Execute = (e, _) =>
            {
                var other = Database.Query<ChatbotLanguageModelEntity>().Where(a => a.IsDefault).SingleOrDefaultEx();
                if (other != null)
                {
                    other.IsDefault = false;
                    other.Execute(ChatbotLanguageModelOperation.Save);
                }

                e.IsDefault = true;
                e.Save();
            }
        }.Register();


        new Graph<ChatbotLanguageModelEntity>.Delete(ChatbotLanguageModelOperation.Delete)
        {
            Delete = (e, _) => { e.Delete(); },
        }.Register();


        LanguageModels = sb.GlobalLazy(() => Database.Query<ChatbotLanguageModelEntity>().ToDictionary(a => a.ToLite()), new InvalidateWith(typeof(ChatbotLanguageModelEntity)));
        DefaultLanguageModel = sb.GlobalLazy(() => LanguageModels.Value.Values.SingleOrDefaultEx(a => a.IsDefault)?.ToLite(), new InvalidateWith(typeof(ChatbotLanguageModelEntity)));

        sb.Include<EmbeddingsLanguageModelEntity>()
            .WithSave(EmbeddingsLanguageModelOperation.Save)
            .WithUniqueIndex(a => a.IsDefault, a => a.IsDefault == true)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.IsDefault,
                e.Provider,
                e.Model,
                e.Dimensions,
            });

        new Graph<EmbeddingsLanguageModelEntity>.Execute(EmbeddingsLanguageModelOperation.MakeDefault)
        {
            CanExecute = a => !a.IsDefault ? null : ValidationMessage._0IsSet.NiceToString(Entity.NicePropertyName(() => a.IsDefault)),
            Execute = (e, _) =>
            {
                var other = Database.Query<EmbeddingsLanguageModelEntity>().Where(a => a.IsDefault).SingleOrDefaultEx();
                if (other != null)
                {
                    other.IsDefault = false;
                    other.Execute(EmbeddingsLanguageModelOperation.Save);
                }

                e.IsDefault = true;
                e.Save();
            }
        }.Register();

        new Graph<EmbeddingsLanguageModelEntity>.Delete(EmbeddingsLanguageModelOperation.Delete)
        {
            Delete = (e, _) => { e.Delete(); },
        }.Register();

        EmbeddingsModels = sb.GlobalLazy(() => Database.Query<EmbeddingsLanguageModelEntity>().ToDictionary(a => a.ToLite()), new InvalidateWith(typeof(EmbeddingsLanguageModelEntity)));
        DefaultEmbeddingsModel = sb.GlobalLazy(() => EmbeddingsModels.Value.Values.SingleOrDefaultEx(a => a.IsDefault)?.ToLite(), new InvalidateWith(typeof(EmbeddingsLanguageModelEntity)));

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

        Filter.GetEmbeddingForSmartSearch = (vectorToken, searchString) =>
        {
            // Get the default embeddings model
            var modelLite = ChatbotLogic.DefaultEmbeddingsModel.Value;
            if (modelLite == null)
                throw new InvalidOperationException("No default EmbeddingsLanguageModelEntity configured.");

            // Retrieve and call the embeddings API
            var model = modelLite.RetrieveFromCache();
            var embeddings = model.GetEmbeddingsAsync(new[] { searchString }, CancellationToken.None).ResultSafe();

            return new Vector(embeddings.SingleEx());
        };
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
        var client = GetChatClient(languageModel);
        var options = ChatOptions(languageModel, []);
        var cr = await client.GetResponseAsync(prompt, options, cancellationToken: ct);
        return cr.Text;
    }

    public static async Task<string> SumarizeTitle(ConversationHistory history, CancellationToken ct)
    {
        var prompt = AgentSkillLogic.QuestionSumarizerSkill.GetInstruction(history);
        var client = GetChatClient(history.LanguageModel);
        var options = ChatbotLogic.ChatOptions(history.LanguageModel, []);
        var cr = await client.GetResponseAsync(prompt, options, cancellationToken: ct);
        return cr.Text;
    }

    public static void RegisterChatbotModelProvider(LanguageModelProviderSymbol symbol, IChatbotModelProvider provider)
    {
        ChatbotModelProviders.Add(symbol, provider);
    }

    public static void RegisterEmbeddingsProvider(LanguageModelProviderSymbol symbol, IEmbeddingsProvider provider)
    {
        EmbeddingsProviders.Add(symbol, provider);
    }


    public static Task<List<string>> GetModelNamesAsync(LanguageModelProviderSymbol provider, CancellationToken ct)
    {
        return ChatbotModelProviders.GetOrThrow(provider).GetModelNames(ct);
    }

    public static Task<List<string>> GetEmbeddingModelNamesAsync(LanguageModelProviderSymbol provider, CancellationToken ct)
    {
        return EmbeddingsProviders.GetOrThrow(provider).GetEmbeddingModelNames(ct);
    }

    public static IChatbotModelProvider GetProvider(ChatbotLanguageModelEntity model)
    {
        return ChatbotModelProviders.GetOrThrow(model.Provider);
    }

    public static IChatClient GetChatClient(ChatbotLanguageModelEntity model)
    {
        return GetProvider(model).CreateChatClient(model);
    }

    public static Task<List<float[]>> GetEmbeddingsAsync(this EmbeddingsLanguageModelEntity model, string[] inputs, CancellationToken ct)
    {
        using (HeavyProfiler.Log("GetEmbeddings", () => model.GetMessage() + "\n" + inputs.ToString("\n")))
        {
            return EmbeddingsProviders.GetOrThrow(model.Provider).GetEmbeddings(inputs, model, ct);
        }
    }

    public static ChatOptions ChatOptions(ChatbotLanguageModelEntity languageModel, List<AITool>? tools)
    {
        var opts = new ChatOptions
        {
            ModelId = languageModel.Model,
        };

        if (languageModel.MaxTokens != null)
            opts.MaxOutputTokens = languageModel.MaxTokens;
        else
            opts.MaxOutputTokens = 64000;

        if (languageModel.Temperature != null)
            opts.Temperature = languageModel.Temperature;

        if (tools.HasItems())
            opts.Tools = tools;

        return opts;
    }

    // ─── Headless / extracted loop ────────────────────────────────────────────

    /// <summary>
    /// Runs the full agent loop on an already-initialised ConversationHistory.
    /// history.Messages must already contain the system message and any initial
    /// user / tool messages to resume from.
    /// Stops when the LLM produces a response with no tool calls, or when a
    /// UITool is invoked (caller must resume via a new request).
    /// </summary>
    public static async Task RunAgentLoopAsync(ConversationHistory history, IAgentOutput output, CancellationToken ct)
    {
        var client = GetChatClient(history.LanguageModel);

        while (true)
        {
            // Context-window management: summarise when close to the token limit
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
            var options = ChatOptions(history.LanguageModel, tools);
            var messages = history.GetMessages();
            GetProvider(history.LanguageModel).CustomizeMessagesAndOptions(messages, options);

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

        // Title summarisation (runs regardless of UITool pause or normal completion)
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

    /// <summary>
    /// Creates a new session and runs the agent loop headlessly (no HTTP connection).
    /// The caller is responsible for setting up the authentication context if needed.
    /// </summary>
    public static async Task<ConversationHistory> RunHeadlessAsync(
        string prompt,
        AgentUseCaseSymbol? useCase = null,
        Lite<ChatbotLanguageModelEntity>? languageModel = null,
        IAgentOutput? output = null,
        CancellationToken ct = default)
    {
        useCase ??= AgentUseCase.DefaultChatbot;
        output ??= NullAgentOutput.Instance;

        var modelLite = languageModel ?? DefaultLanguageModel.Value
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




// ─── IAgentOutput ─────────────────────────────────────────────────────────────

/// <summary>
/// Receives events from the agent loop. All methods have no-op default implementations
/// so implementations only override what they care about.
/// </summary>
public interface IAgentOutput
{
    /// <summary>Called when the initial system message is saved (new session).</summary>
    Task OnSystemMessageAsync(ChatMessageEntity msg, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Called when a user question message is saved.</summary>
    Task OnUserQuestionAsync(ChatMessageEntity msg, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Called when the context-window summary message is saved.</summary>
    Task OnSummarizationAsync(ChatMessageEntity summaryMsg, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Called once at the start of each assistant response, before any text chunks.</summary>
    Task OnAssistantStartedAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>Called for each streaming text chunk from the LLM.</summary>
    Task OnTextChunkAsync(string chunk, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Called when the assistant message entity is fully saved (includes tool call metadata).</summary>
    Task OnAssistantMessageAsync(ChatMessageEntity msg, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Called just before a tool is invoked.</summary>
    Task OnToolStartAsync(string toolId, string callId, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Called after a tool completes. Check toolMsg.Exception for errors.</summary>
    Task OnToolFinishedAsync(ChatMessageEntity toolMsg, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Called when the session title is determined or updated.</summary>
    Task OnTitleUpdatedAsync(string title, CancellationToken ct) => Task.CompletedTask;
}

public sealed class NullAgentOutput : IAgentOutput
{
    public static readonly NullAgentOutput Instance = new();
    private NullAgentOutput() { }
}

public interface IChatbotModelProvider
{
    Task<List<string>> GetModelNames(CancellationToken ct);

    IChatClient CreateChatClient(ChatbotLanguageModelEntity model);

    void CustomizeMessagesAndOptions(List<ChatMessage> messages, ChatOptions options) { }
}

public interface IEmbeddingsProvider
{
    Task<List<string>> GetEmbeddingModelNames(CancellationToken ct);

    Task<List<float[]>> GetEmbeddings(string[] inputs, EmbeddingsLanguageModelEntity model, CancellationToken ct);
}

public class ConversationHistory
{
    public Lite<ChatSessionEntity> Session;

    public ChatbotLanguageModelEntity LanguageModel;

    public List<ChatMessageEntity> Messages;

    public string? SessionTitle { get; internal set; }

    public ResolvedSkillNode? RootSkill { get; set; }

    public List<ChatMessage> GetMessages()
    {
        return Messages.Select(m => ToChatMessage(m)).ToList();
    }

    ChatMessage ToChatMessage(ChatMessageEntity c)
    {
        var role = ToChatRole(c.Role);

        var content = c.Content ?? (c.Exception != null
            ? $"{c.Exception.Entity.ExceptionType}:\n{c.Exception.Entity.ExceptionMessage}"
            : null);

        if (c.Role == ChatMessageRole.Tool)
        {
            return new ChatMessage(role, [
                new FunctionResultContent(c.ToolCallID!, content)
            ]);
        }

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

    private static object? CleanValue(object? value)
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
            return new List<AITool>();

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
            .OfType<ResolvedSkillNode>()
            .SelectMany(skill => skill.GetTools())
            .ToList();
    }

    private ChatRole ToChatRole(ChatMessageRole role) => role switch
    {
        ChatMessageRole.System => ChatRole.System,
        ChatMessageRole.User => ChatRole.User,
        ChatMessageRole.Assistant => ChatRole.Assistant,
        ChatMessageRole.Tool => ChatRole.Tool,
        _ => throw new InvalidOperationException($"Unexpected {nameof(ChatMessageRole)} {role}"),
    };
}
