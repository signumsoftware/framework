using Signum.Chatbot.Providers;
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
                    e.IsToolCall,
                    e.Message,
                    e.ChatSession,
                });
        }
    }

    public static async Task<string> SumarizeTitle(ConversationHistory history, CancellationToken ct)
    {
        var prompt = ChatbotAgentLogic.GetAgent(DefaultAgent.QuestionSumarizer).LongDescriptionWithReplacements(history);
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


    public static  IAsyncEnumerable<string> AskStreaming(List<ChatMessage> messages, ChatbotLanguageModelEntity model,  CancellationToken ct)
    {
        return  Providers.GetOrThrow(model.Provider).AskStreaming(messages, model, ct);
    }

    //public static Task<string?> AskAsync(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    //{
    //    return Providers.GetOrThrow(model.Provider).AskAsync(messages, model, ct);
    //}
}


public interface IChatbotProvider
{
    IAsyncEnumerable<string> AskStreaming(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct);
    string[] GetModelNames();
}



public class ConversationHistory
{
    public ChatSessionEntity Session; 

    public ChatbotLanguageModelEntity LanguageModel;

    public List<ChatMessageEntity> Messages; 


    public List<ChatMessage> GetMessages()
    {
        return Messages.Select( c => new ChatMessage()
        {
            Role = c.Role,
            Content = c.Message,
            ToolID = c.ToolID
        }).ToList();
    }
}


public class ChatMessage
{
    public ChatMessageRole Role;
    public string Content;
    public string? ToolID;

    public override string ToString() => $"{Role}: {Content}";
}
