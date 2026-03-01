using Signum.Basics;
using Signum.Entities;
using Signum.Entities.Validation;
using Signum.Operations;
using System;
using System.ComponentModel;

namespace Signum.Agent;



[EntityKind(EntityKind.Main, EntityData.Master)]
public class ChatbotLanguageModelEntity : Entity
{
    public LanguageModelProviderSymbol Provider { get; set; }

    [StringLengthValidator(Max = 50)]
    public string Model { get; set; }

    public float? Temperature { get; set; }

    public int? MaxTokens { get; set; }

    public bool IsDefault { get; set; }

    [Unit("$ / 1M tokens"), DecimalsValidator(4)]
    public decimal? PricePerInputToken { get; set; }

    [Unit("$ / 1M tokens"), DecimalsValidator(4)]
    public decimal? PricePerOutputToken { get; set; }

    [Unit("$ / 1M tokens"), DecimalsValidator(4)]
    public decimal? PricePerCachedInputToken { get; set; }

    [Unit("$ / 1M tokens"), DecimalsValidator(4)]
    public decimal? PricePerReasoningOutputToken { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{Provider}: {Model}");
}


[AutoInit]
public static class ChatbotLanguageModelOperation
{
    public static readonly ExecuteSymbol<ChatbotLanguageModelEntity> Save;
    public static readonly ExecuteSymbol<ChatbotLanguageModelEntity> MakeDefault;
    public static readonly DeleteSymbol<ChatbotLanguageModelEntity> Delete;
}


[EntityKind(EntityKind.Main, EntityData.Master)]
public class EmbeddingsLanguageModelEntity : Entity
{
    public LanguageModelProviderSymbol Provider { get; set; }

    [StringLengthValidator(Max = 50)]
    public string Model { get; set; }

    public int? Dimensions { get; set; }

    public bool IsDefault { get; set; }

    internal string GetMessage() => $"{Provider} - {Model}" + (Dimensions == null ? null : $" ({Dimensions} dims)");

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{Provider}: {Model}");
}


[AutoInit]
public static class EmbeddingsLanguageModelOperation
{
    public static readonly ExecuteSymbol<EmbeddingsLanguageModelEntity> Save;
    public static readonly ExecuteSymbol<EmbeddingsLanguageModelEntity> MakeDefault;
    public static readonly DeleteSymbol<EmbeddingsLanguageModelEntity> Delete;
}


[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class LanguageModelProviderSymbol : Symbol
{
    private LanguageModelProviderSymbol() { }

    public LanguageModelProviderSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}



[AutoInit]
public static class LanguageModelProviders
{
    public static readonly LanguageModelProviderSymbol OpenAI;
    public static readonly LanguageModelProviderSymbol Gemini;
    public static readonly LanguageModelProviderSymbol Anthropic;
    public static readonly LanguageModelProviderSymbol Mistral; 
    public static readonly LanguageModelProviderSymbol GithubModels; 
    public static readonly LanguageModelProviderSymbol Ollama;
    public static readonly LanguageModelProviderSymbol DeepSeek;
}


public class ChatbotConfigurationEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password), Description("Open AI API Key")]
    public string? OpenAIAPIKey { get; set; }

    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password)]
    public string? AnthropicAPIKey { get; set; }

    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password)]
    public string? GeminiAPIKey { get; set; }

    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password)]
    public string? MistralAPIKey { get; set; }

    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password)]
    public string? GithubModelsToken { get; set; }

    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password)]
    public string? DeepSeekAPIKey { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? OllamaUrl { get; set; }
}

