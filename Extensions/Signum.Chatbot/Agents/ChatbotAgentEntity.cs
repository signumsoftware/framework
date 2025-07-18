namespace Signum.Chatbot.Agents;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class ChatbotAgentEntity : Entity
{
    public ChatbotAgentCodeSymbol Code { get; set; }

    [StringLengthValidator(Max = 100)]
    public string ShortDescription { get; set; }

    public MList<ChatbotAgentDescriptionsEmbedded> Descriptions { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(()  => Code.ToString());
}


[AutoInit]
public static class ChatbotAgentOperation
{
    public static readonly ExecuteSymbol<ChatbotAgentEntity> Save;
    public static readonly DeleteSymbol<ChatbotAgentEntity> Delete;
}


[EntityKind(EntityKind.Main, EntityData.Transactional)]
public class ChatbotAgentDescriptionsEmbedded : EmbeddedEntity
{
    public string? PromptName { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string Content { get; set; }
}


public enum ConfigChatbotType
{
    GeneralConfig,
    Title,
}



[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class ChatbotAgentCodeSymbol : Symbol
{
    private ChatbotAgentCodeSymbol() { }

    public ChatbotAgentCodeSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[AutoInit]
public static class DefaultAgent
{
    public static readonly ChatbotAgentCodeSymbol Introduction; 
    public static readonly ChatbotAgentCodeSymbol QuestionSumarizer;
    public static readonly ChatbotAgentCodeSymbol SearchControl;
}

