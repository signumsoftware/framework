using Microsoft.AspNetCore.Http;
using Signum.Authorization.Rules;
using Signum.Basics;
using Signum.Chatbot.OpenAI;
using Signum.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Operations;
using Signum.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace Signum.Chatbot;


public static class ChatbotLanguageModelLogic
{

    public static ResetLazy<Dictionary<Lite<ChatbotLanguageModelEntity>, ChatbotLanguageModelEntity>> LanguageModels = null!;
    public static ResetLazy<Lite<ChatbotLanguageModelEntity>?> DefaultLanguageModel = null!;

    public static Dictionary<ChatbotProviderSymbol, IChatbotProvider> Providers = new Dictionary<ChatbotProviderSymbol, IChatbotProvider>
    {
        { ChatbotProviders.Mistral, new MistralChatbotProvider()}
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
                    e.Version,
                });

            LanguageModels = sb.GlobalLazy(() => Database.Query<ChatbotLanguageModelEntity>().ToDictionary(a => a.ToLite()), new InvalidateWith(typeof(ChatbotLanguageModelEntity)));
            DefaultLanguageModel = sb.GlobalLazy(() => LanguageModels.Value.Values.SingleOrDefaultEx(a => a.IsDefault)?.ToLite(), new InvalidateWith(typeof(ChatbotLanguageModelEntity)));

            sb.Include<ChatSessionEntity>()
               .WithDelete(ChatSessionOperation.Delete)
               .WithQuery(() => e => new
               {
                   Entity = e,
                   e.Id,
                   e.LanguageModel,
                   e.User,
               });
    
            sb.Include<ChatMessageEntity>()
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Role,
                    e.Message,
                });

        }
    }

    public static void RegisterProvider(ChatbotProviderSymbol symbol, IChatbotProvider provider)
    {
        Providers.Add(symbol, provider);
    }

    public static string[] GetModelNames(ChatbotProviderSymbol provider)
    {
        return Providers.GetOrThrow(provider).GetModelNames();
    }



    public static  IAsyncEnumerable<string> AskQuestionAsync(ConversationHistory history, CancellationToken ct)
    {
        return  Providers.GetOrThrow(history.Model.Provider).AskQuestionAsync(history, ct);
    }
}

public interface IChatbotProvider
{
    string[] GetModelNames();
    string[] GetModelVersions(string name);
    IAsyncEnumerable<string> AskQuestionAsync(ConversationHistory history, CancellationToken ct);
}

public class ConversationHistory
{
    public ChatSessionEntity Session; 
    public ChatbotLanguageModelEntity Model;
    public List<ChatMessageEntity> Chats; 
}
