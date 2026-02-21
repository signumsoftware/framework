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
                    if (inDb.Model != m.Model || inDb.Provider.Is(m.Provider))
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

        var skill = ChatbotSkillLogic.GetSkill<ConversationSumarizerSkill>();
        var prompt = skill.GetInstruction(conversationText.ToString());
        var client = GetChatClient(languageModel);
        var options = ChatOptions(languageModel, []);
        var cr = await client.GetResponseAsync(prompt, options, cancellationToken: ct);
        return cr.Text;
    }

    public static async Task<string> SumarizeTitle(ConversationHistory history, CancellationToken ct)
    {
        var prompt = ChatbotSkillLogic.GetSkill<QuestionSumarizerSkill>().GetInstruction(history);
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
    public ChatSessionEntity Session;

    public ChatbotLanguageModelEntity LanguageModel;

    public List<ChatMessageEntity> Messages;

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
        var activatedSkills = new HashSet<string>();

        if (ChatbotSkillLogic.IntroductionSkill != null)
            activatedSkills.Add(ChatbotSkillLogic.IntroductionSkill.Name);

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
                                activatedSkills.Add(sn.GetString()!);
                        }
                        catch { }
                    }
                }
            }
        }

        return activatedSkills
            .SelectMany(skillName => ChatbotSkillLogic.GetSkill(skillName).GetToolsRecursive())
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
