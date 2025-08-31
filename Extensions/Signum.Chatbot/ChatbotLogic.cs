using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Chatbot.Agents;
using Signum.Chatbot.Providers;
using Signum.Chatbot.Skills;
using Signum.Utilities.Synchronization;
using System.Formats.Tar;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Signum.Chatbot;


public static class ChatbotLogic
{
    [AutoExpressionField]
    public static IQueryable<ChatMessageEntity> Messages(this ChatSessionEntity session) =>
        As.Expression(() => Database.Query<ChatMessageEntity>().Where(a => a.ChatSession.Is(session)));

    public static ResetLazy<Dictionary<Lite<ChatbotLanguageModelEntity>, ChatbotLanguageModelEntity>> LanguageModels = null!;
    public static ResetLazy<Lite<ChatbotLanguageModelEntity>?> DefaultLanguageModel = null!;

    public static Dictionary<ChatbotProviderSymbol, IChatbotProvider> Providers = new Dictionary<ChatbotProviderSymbol, IChatbotProvider>
    {
        { ChatbotProviders.OpenAI, new OpenAIChatbotProvider()},
        { ChatbotProviders.Gemini, new GeminiChatbotProvider()},
        { ChatbotProviders.Anthropic, new AnthropicChatbotProvider()},
        { ChatbotProviders.GithubModels, new GithubModelsChatbotProvider()},
        { ChatbotProviders.Mistral, new MistralChatbotProvider()},
        { ChatbotProviders.Ollama, new OllamaChatbotProvider()},
    };

    public static Func<ChatbotConfigurationEmbedded> GetConfig;

    public static ChatbotLanguageModelEntity RetrieveFromCache(this Lite<ChatbotLanguageModelEntity> lite)
    {
        return LanguageModels.Value.GetOrThrow(lite);
    }

    public static void Start(SchemaBuilder sb, Func<ChatbotConfigurationEmbedded> config)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            GetConfig = config;

            SymbolLogic<ChatbotProviderSymbol>.Start(sb, () => Providers.Keys);

            sb.Include<ChatbotLanguageModelEntity>()
                .WithSave(ChatbotLanguageModelOperation.Save)
                .WithUniqueIndex(a=>a.IsDefault, a => a.IsDefault == true)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.IsDefault,
                    e.Provider,
                    e.Model,
                    e.Temperature,
                    e.MaxTokens,
                });

            new Graph<ChatbotLanguageModelEntity>.Execute(ChatbotLanguageModelOperation.MakeDefault)
            {
                CanExecute = a => !a.IsDefault ? null : ValidationMessage._0IsSet.NiceToString(Entity.NicePropertyName(() => a.IsDefault)),
                Execute = (e, _) =>
                {
                    var other = Database.Query<ChatbotLanguageModelEntity>().Where(a => a.IsDefault).SingleOrDefaultEx();
                    if(other != null)
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

            sb.Include<ChatSessionEntity>()
               .WithDelete(ChatSessionOperation.Delete)
               .WithQuery(() => e => new
               {
                   Entity = e,
                   e.Id,
                   e.Title,
                   e.LanguageModel,
                   e.User,
                   e.StartDate,
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

            PermissionLogic.RegisterTypes(typeof(ChatbotPermission));
        }
    }

    public static void RegisterUserTypeCondition(TypeConditionSymbol userEntities)
    {
        TypeConditionLogic.RegisterCompile<ChatSessionEntity>(userEntities, cm => cm.User.Entity.Is(UserEntity.Current));
        TypeConditionLogic.RegisterCompile<ChatMessageEntity>(userEntities, cm => cm.ChatSession.Entity.InCondition(userEntities));
    }

    public static async Task<string> SumarizeTitle(ConversationHistory history, CancellationToken ct)
    {
        var prompt = ChatbotSkillLogic.GetSkill<QuestionSumarizerSkill>().GetInstruction(history);
        var client = GetChatClient(history.LanguageModel);
        var options = ChatbotLogic.ChatOptions(history.LanguageModel, []);
        var cr = await client.GetResponseAsync(prompt, options, cancellationToken: ct);
        return cr.Text;
    }

    public static void RegisterProvider(ChatbotProviderSymbol symbol, IChatbotProvider provider)
    {
        Providers.Add(symbol, provider);
    }


    public static Task<List<string>> GetModelNamesAsync(ChatbotProviderSymbol provider, CancellationToken ct)
    {
        return Providers.GetOrThrow(provider).GetModelNames(ct);
    }


    public static IChatClient GetChatClient(ChatbotLanguageModelEntity model)
    {
        var result = Providers.GetOrThrow(model.Provider).CreateChatClient(model);

        return result;
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


public interface IChatbotProvider
{
    Task<List<string>> GetModelNames(CancellationToken ct);

    IChatClient CreateChatClient(ChatbotLanguageModelEntity model);
}

public class ConversationHistory
{
    public ChatSessionEntity Session; 

    public ChatbotLanguageModelEntity LanguageModel;

    public List<ChatMessageEntity> Messages;


    public List<ChatMessage> GetMessages()
    {
        return Messages.Select(c =>
        {
            var role = ToChatRole(c.Role);

            var content = c.Exception == null ? c.Content :
                $"{c.Exception.Entity.ExceptionType}:\n{c.Exception.Entity.ExceptionMessage}";

            if(c.Role == ChatMessageRole.Tool)
            {
                return new ChatMessage(role, [
                    new FunctionResultContent(c.ToolCallID!, c.Content)
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

            if (c.Content.HasText())
                contents.Insert(0, new TextContent(c.Content!));

            return new ChatMessage(role, contents);
        }).ToList();
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
        var skills = Messages.Select(m =>
            m.Role == ChatMessageRole.System ? ChatbotSkillLogic.IntroductionSkill?.Name :
            m.Role == ChatMessageRole.Assistant && m.ToolID == nameof(IntroductionSkill.Describe) ? JsonDocument.Parse(m.Content!).RootElement.GetProperty("skillName").GetString() :
            null)
            .NotNull()
            .Distinct()
            .ToList();

        return skills
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
