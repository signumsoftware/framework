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
        { ChatbotProviders.OpenAI, new OpenAIChatbotProvider()},
        { ChatbotProviders.Anthropic, new AnthropicChatbotProvider()},
        { ChatbotProviders.DeepSeek, new DeepSeekChatbotProvider()},
        { ChatbotProviders.Grok, new GrokChatbotProvider()},
        { ChatbotProviders.Mistral, new MistralChatbotProvider()},
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
        StringBuilder sb = new StringBuilder();
        await foreach (var item in AskStreaming([new ChatMessage { Role = ChatMessageRole.System, Content = prompt }], history.LanguageModel, ct))
        {
            sb.Append(item);
        }

        var title = sb.ToString();
        return title;
    }


    public static void RegisterProvider(ChatbotProviderSymbol symbol, ChatbotProviderBase provider)
    {
        Providers.Add(symbol, provider);
    }


    public static string[] GetModelNames(ChatbotProviderSymbol provider)
    {
        return Providers.GetOrThrow(provider).GetModelNames();
    }


    public static IAsyncEnumerable<StreamingValue> AskStreaming(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    {
        var tools = messages.Where(m => m.SkillName != null)
                            .Select(m => m.SkillName!)
                            .Distinct()
                            .SelectMany(skillName => ChatbotSkillLogic.GetSkill(skillName).GetTools())
                            .ToList();

        return Providers.GetOrThrow(model.Provider).AskStreaming(messages, tools, model, ct);
    }

    //public static Task<string?> AskAsync(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    //{
    //    return Providers.GetOrThrow(model.Provider).AskAsync(messages, model, ct);
    //}
}


public interface IChatbotProvider
{
    string[] GetModelNames();

    const string Answer = "<<Answer>>:";
    const string ToolCall = "<<ToolCall>>:";

    IAsyncEnumerable<StreamingValue> AskStreaming(List<ChatMessage> messages, List<IChatbotTool> tools, ChatbotLanguageModelEntity model, CancellationToken ct);
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
        return Messages.Select(c => new ChatMessage()
        {
            Role = c.Role,
            Content = c.Content!,
            ToolCallID = c.ToolCallID,
            SkillName = c.Role == ChatMessageRole.System ? ChatbotSkillLogic.IntroductionSkill?.Name :
            c.Role == ChatMessageRole.Assistant && c.ToolID == nameof(IntroductionSkill.Describe) ? JsonDocument.Parse(c.Content!).RootElement.GetProperty("skillName").GetString() :
                null
        }).ToList();
    }
}


public class ChatMessage
{
    public ChatMessageRole Role;
    public string Content;
    public string? SkillName; // For available tools
    public string? ToolCallID;

    public override string ToString() => $"{Role}: {Content}";
}
