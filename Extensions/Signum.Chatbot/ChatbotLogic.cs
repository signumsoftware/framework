using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Signum.Chatbot.Agents;
using Signum.Chatbot.Providers;
using Signum.Chatbot.Skills;
using Signum.Utilities.Synchronization;
using System.Formats.Tar;
using System.IO;
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
        { ChatbotProviders.OpenAI, new AnthropicChatbotProvider()/*new OpenAIChatbotProvider()*/},
        { ChatbotProviders.Anthropic, new AnthropicChatbotProvider()},
        { ChatbotProviders.DeepSeek, new AnthropicChatbotProvider()/*new DeepSeekChatbotProvider()*/},
        { ChatbotProviders.Grok, new AnthropicChatbotProvider()/*new GrokChatbotProvider()*/},
        { ChatbotProviders.Mistral, new AnthropicChatbotProvider()/*new MistralChatbotProvider()*/},
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
                    e.ChatSession,
                });
        }
    }

    public static async Task<string> SumarizeTitle(ConversationHistory history, CancellationToken ct)
    {
        var prompt = ChatbotSkillLogic.GetSkill<QuestionSumarizerSkill>().GetInstruction(history);
        var client = GetChatClient(history.LanguageModel);
        var cr = await client.GetResponseAsync(prompt, ChatbotLogic.ChatOptions(history.LanguageModel, history.GetTools()), cancellationToken: ct);

        return cr.Text;
    }

    public static void RegisterProvider(ChatbotProviderSymbol symbol, IChatbotProvider provider)
    {
        Providers.Add(symbol, provider);
    }


    public static string[] GetModelNames(ChatbotProviderSymbol provider)
    {
        return Providers.GetOrThrow(provider).GetModelNames();
    }


    public static IChatClient GetChatClient(ChatbotLanguageModelEntity model)
    {
        var result = Providers.GetOrThrow(model.Provider).CreateChatClient();

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
    string[] GetModelNames();

    IChatClient CreateChatClient();
}

public struct StreamingValue
{
    public string? ToolCallId;
    public string? ToolId;
    public string? Value;

    private StreamingValue(string? toolCallId, string? toolId, string? value)
    {
        ToolCallId = toolCallId;
        ToolId = toolId;
        Value = value;
    }

    public static StreamingValue Answer(string value) => new StreamingValue(null, null, value);
    public static StreamingValue ToolCal_Start(string toolCallId, string toolId) => new StreamingValue(toolCallId, toolId, null);
    public static StreamingValue ToolCall_Argument(string toolCallId, string argument) => new StreamingValue(toolCallId, null, argument);
}

public class ConversationHistory
{
    public ChatSessionEntity Session; 

    public ChatbotLanguageModelEntity LanguageModel;

    public List<ChatMessageEntity> Messages;


    public List<ChatMessage> GetMessages()
    {
        return Messages.Select(c => new ChatMessage(ToChatRole(c.Role), c.Content)).ToList();
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


//public class ChatMessage
//{
//    public ChatMessageRole Role;
//    public string Content;
//    public string? SkillName; // For available tools
//    public string? ToolCallID;

//    public override string ToString() => $"{Role}: {Content}";
//}
