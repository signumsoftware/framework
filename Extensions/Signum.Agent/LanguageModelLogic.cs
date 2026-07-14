using Microsoft.Extensions.AI;
using Signum.Agent.Providers;
using Pgvector;
using Signum.Utilities.Synchronization;

namespace Signum.Agent;

public static class LanguageModelLogic
{
    public static Func<ChatbotConfigurationEmbedded> GetConfig = null!;

    public static ResetLazy<Dictionary<Lite<ChatbotLanguageModelEntity>, ChatbotLanguageModelEntity>> LanguageModels = null!;
    public static ResetLazy<Lite<ChatbotLanguageModelEntity>?> DefaultLanguageModel = null!;

    public static ResetLazy<Dictionary<Lite<EmbeddingsLanguageModelEntity>, EmbeddingsLanguageModelEntity>> EmbeddingsModels = null!;
    public static ResetLazy<Lite<EmbeddingsLanguageModelEntity>?> DefaultEmbeddingsModel = null!;

    public static Dictionary<LanguageModelProviderSymbol, IChatbotModelProvider> ChatbotModelProviders = new()
    {
        { LanguageModelProviders.OpenAI, new OpenAIProvider() },
        { LanguageModelProviders.Gemini, new GeminiProvider() },
        { LanguageModelProviders.Anthropic, new AnthropicProvider() },
        { LanguageModelProviders.GithubModels, new GithubModelsProvider() },
        { LanguageModelProviders.Mistral, new MistralProvider() },
        { LanguageModelProviders.Ollama, new OllamaProvider() },
        { LanguageModelProviders.DeepSeek, new DeepSeekProvider() },
    };

    public static Dictionary<LanguageModelProviderSymbol, IEmbeddingsProvider> EmbeddingsProviders = new()
    {
        { LanguageModelProviders.OpenAI, new OpenAIProvider() },
        { LanguageModelProviders.Gemini, new GeminiProvider() },
        { LanguageModelProviders.GithubModels, new GithubModelsProvider() },
        { LanguageModelProviders.Mistral, new MistralProvider() },
        { LanguageModelProviders.Ollama, new OllamaProvider() },
    };

    public static ChatbotLanguageModelEntity RetrieveFromCache(this Lite<ChatbotLanguageModelEntity> lite) =>
        LanguageModels.Value.GetOrThrow(lite);

    public static EmbeddingsLanguageModelEntity RetrieveFromCache(this Lite<EmbeddingsLanguageModelEntity> lite) =>
        EmbeddingsModels.Value.GetOrThrow(lite);

    public static void Start(SchemaBuilder sb, Func<ChatbotConfigurationEmbedded> config)
    {
        GetConfig = config;

        SymbolLogic<LanguageModelProviderSymbol>.Start(sb, () => ChatbotModelProviders.Keys.Union(EmbeddingsProviders.Keys));

        sb.Include<ChatbotLanguageModelEntity>()
            .WithSave(ChatbotLanguageModelOperation.Save, (m, args) =>
            {
                if (!m.IsNew && Database.Query<ChatMessageEntity>().Any(a => a.LanguageModel.Is(m)))
                {
                    var inDb = m.InDB(a => new { a.Model, a.Provider });
                    if (inDb.Model != m.Model || !inDb.Provider.Is(m.Provider))
                        throw new ArgumentNullException(ChatbotMessage.UnableToChangeModelOrProviderOnceUsed.NiceToString());
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

        Filter.GetEmbeddingForSmartSearch = (vectorToken, searchString) =>
        {
            var modelLite = DefaultEmbeddingsModel.Value
                ?? throw new InvalidOperationException("No default EmbeddingsLanguageModelEntity configured.");
            var embeddings = modelLite.RetrieveFromCache().GetEmbeddingsAsync([searchString], CancellationToken.None).ResultSafe();
            return new Vector(embeddings.SingleEx());
        };
    }

    public static void RegisterChatbotModelProvider(LanguageModelProviderSymbol symbol, IChatbotModelProvider provider) =>
        ChatbotModelProviders.Add(symbol, provider);

    public static void RegisterEmbeddingsProvider(LanguageModelProviderSymbol symbol, IEmbeddingsProvider provider) =>
        EmbeddingsProviders.Add(symbol, provider);

    public static Task<List<string>> GetModelNamesAsync(LanguageModelProviderSymbol provider, CancellationToken ct) =>
        ChatbotModelProviders.GetOrThrow(provider).GetModelNames(ct);

    public static Task<List<string>> GetEmbeddingModelNamesAsync(LanguageModelProviderSymbol provider, CancellationToken ct) =>
        EmbeddingsProviders.GetOrThrow(provider).GetEmbeddingModelNames(ct);

    public static IChatbotModelProvider GetProvider(ChatbotLanguageModelEntity model) =>
        ChatbotModelProviders.GetOrThrow(model.Provider);

    public static IChatClient GetChatClient(ChatbotLanguageModelEntity model) =>
        GetProvider(model).CreateChatClient(model);

    public static Task<List<float[]>> GetEmbeddingsAsync(this EmbeddingsLanguageModelEntity model, string[] inputs, CancellationToken ct)
    {
        using (HeavyProfiler.Log("GetEmbeddings", () => model.GetMessage() + "\n" + inputs.ToString("\n")))
            return EmbeddingsProviders.GetOrThrow(model.Provider).GetEmbeddings(inputs, model, ct);
    }

    public static ChatOptions ChatOptions(ChatbotLanguageModelEntity languageModel, List<AITool>? tools)
    {
        var opts = new ChatOptions
        {
            ModelId = languageModel.Model,
            MaxOutputTokens = languageModel.MaxTokens ?? 64000,
        };

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
