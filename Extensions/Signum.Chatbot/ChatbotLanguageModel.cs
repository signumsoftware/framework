using Signum.Basics;
using Signum.Entities;
using Signum.Entities.Validation;
using Signum.Operations;
using System;
using System.ComponentModel;

namespace Signum.Chatbot;



[EntityKind(EntityKind.Main, EntityData.Master)]
public class ChatbotLanguageModelEntity : Entity
{
    public ChatbotProviderSymbol Provider { get; set; }

    [StringLengthValidator(Max = 50)]
    public string Model { get; set; }

    public float? Temperature { get; set; }

    public int? MaxTokens { get; set; }

    public bool IsDefault { get; set; }

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



[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class ChatbotProviderSymbol : Symbol
{
    private ChatbotProviderSymbol() { }

    public ChatbotProviderSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}



[AutoInit]
public static class ChatbotProviders
{
    public static readonly ChatbotProviderSymbol OpenAI;
    public static readonly ChatbotProviderSymbol Gemini;
    public static readonly ChatbotProviderSymbol Anthropic;
    public static readonly ChatbotProviderSymbol Mistral; 
    public static readonly ChatbotProviderSymbol GithubModels; 
    public static readonly ChatbotProviderSymbol Ollama;
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

    [StringLengthValidator(Max = 300)]
    public string? OllamaUrl { get; set; }
}

