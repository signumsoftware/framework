using Signum.Chatbot.Agents;
using Signum.Chatbot.Providers;
using System.Runtime.CompilerServices;

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
        //{ ChatbotProviders.Gemini, new AnthropicChatbotProvider()},
        { ChatbotProviders.Anthropic, new AnthropicChatbotProvider()},
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
                .WithDelete(ChatbotLanguageModelOperation.Delete)
                .WithUniqueIndex(a=>a.IsDefault, a => a.IsDefault == true)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Provider,
                    e.Model,
                    e.Temperature,
                    e.MaxTokens,
                });

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
                    e.IsCommand,
                    e.Message,
                    e.ChatSession,
                });
        }
    }

    public static async Task<string> SumarizeTitle(ConversationHistory history, CancellationToken ct)
    {
        var prompt = ChatbotAgentLogic.GetAgent(DefaultAgent.QuestionSumarizer).GetDescribe(null, history);
        StringBuilder sb = new StringBuilder();
        await foreach (var item in AskQuestionAsync([new ChatMessage { Role = ChatMessageRole.System, Content = prompt }], history.LanguageModel, ct))
        {
            sb.Append(item);
        }

        var title = sb.ToString();
        return title;
    }


    public static void RegisterProvider(ChatbotProviderSymbol symbol, IChatbotProvider provider)
    {
        Providers.Add(symbol, provider);
    }


    public static string[] GetModelNames(ChatbotProviderSymbol provider)
    {
        return Providers.GetOrThrow(provider).GetModelNames();
    }


    public static  IAsyncEnumerable<string> AskQuestionAsync(List<ChatMessage> messages, ChatbotLanguageModelEntity model,  CancellationToken ct)
    {
        return  Providers.GetOrThrow(model.Provider).AskStreaming(messages, model, ct);
    }

    public static Task<string?> GetAgent(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    {
        return Providers.GetOrThrow(model.Provider).AskAsync(messages, model, ct);
    }

}


public interface IChatbotProvider
{
    string[] GetModelNames();

    string[] GetModelVersions(string name);

    IAsyncEnumerable<string> AskStreaming(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct);

    async Task<string?> AskAsync(List<ChatMessage> messages, ChatbotLanguageModelEntity model, CancellationToken ct)
    {
        var sb = new StringBuilder();

        await foreach (var item in AskStreaming(messages, model, ct))
        {
            sb.Append(item);
        }

        return sb.ToString();
    }
}


public class ConversationHistory
{
    public ChatSessionEntity Session; 

    public ChatbotLanguageModelEntity LanguageModel;

    public List<ChatMessageEntity> Messages; 


    public List<ChatMessage> GetMessages()
    {
        return Messages.Select( c => new ChatMessage() { Role = c.Role, Content = c.Message}).ToList();
    }
}


public class ChatMessage
{
    public ChatMessageRole Role;
    public string Content; 
}
