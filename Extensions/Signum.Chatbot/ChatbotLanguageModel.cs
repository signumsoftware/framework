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

    public double? Temperature { get; set; }

    public int? MaxTokens { get; set; }

    public bool IsDefault { get; set; }
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
    //public static readonly ChatbotProviderSymbol Gemini; // TODO: Api is not OpenAI based
    public static readonly ChatbotProviderSymbol Mistral; 
    public static readonly ChatbotProviderSymbol Anthropic;
    public static readonly ChatbotProviderSymbol DeepSeek;
    public static readonly ChatbotProviderSymbol Grok;
}


public class ChatbotConfigurationEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password), Description("Open AI API Key")]
    public string? OpenAIAPIKey { get; set; }

    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password)]
    public string? AnthropicAPIKey { get; set; }

    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password)]
    public string? DeepSeekAPIKey { get; set; }

    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password)]
    public string? GeminiAPIKey { get; set; }

    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password)]
    public string? GrokAPIKey { get; set; }

    [StringLengthValidator(Max = 300), Format(FormatAttribute.Password)]
    public string? MistralAPIKey{ get; set; }

}

