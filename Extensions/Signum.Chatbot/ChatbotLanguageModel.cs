using Signum.Basics;
using Signum.Entities;
using Signum.Entities.Validation;
using Signum.Operations;
using System;

namespace Signum.Chatbot;



[EntityKind(EntityKind.Main, EntityData.Master)]
public class ChatbotLanguageModelEntity : Entity
{
    public ChatbotProviderSymbol Provider { get; set; }

    [StringLengthValidator(Max = 50)]
    public string Model { get; set; }

    [StringLengthValidator(Max = 24)]
    public string? Version { get; set; }

    public double Temperature { get; set; }

    public bool IsDefault { get; set; }
}


[AutoInit]
public static class ChatbotLanguageModelOperation
{
    public static readonly ExecuteSymbol<ChatbotLanguageModelEntity> Save;
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
    public static readonly ChatbotProviderSymbol Mistral; 
    public static readonly ChatbotProviderSymbol Anthropic;
}


public class ChatbotConfigurationEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 100), Format(FormatAttribute.Password)]
    public string? OpenAIAPIKey { get; set; }


    [StringLengthValidator(Max = 100), Format(FormatAttribute.Password)]
    public string? ClaudeAPIKey { get; set; }

    [StringLengthValidator(Max = 100), Format(FormatAttribute.Password)]
    public string? MistralAPIKey{ get; set; }

}

