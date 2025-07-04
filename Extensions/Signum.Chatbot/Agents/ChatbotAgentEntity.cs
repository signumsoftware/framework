namespace Signum.Chatbot.Agents;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class ChatbotAgentEntity : Entity
{
    public ChatbotAgentTypeSymbol Key { get; set; }

    [StringLengthValidator(Max = 100)]
    public string ShortDescription { get; set; }

    public MList<ChatbotAgentPromptEmbedded> ChatbotPrompts { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(()  => Key.ToString());
}


[AutoInit]
public static class ChatbotAgentOperation
{
    public static readonly ExecuteSymbol<ChatbotAgentEntity> Save;
    public static readonly DeleteSymbol<ChatbotAgentEntity> Delete;
}


[EntityKind(EntityKind.Main, EntityData.Transactional)]
public class ChatbotAgentPromptEmbedded : EmbeddedEntity
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
public class ChatbotAgentTypeSymbol : Symbol
{
    private ChatbotAgentTypeSymbol() { }

    public ChatbotAgentTypeSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[AutoInit]
public static class DefaultAgent
{
    public static readonly ChatbotAgentTypeSymbol Introduction; 
    public static readonly ChatbotAgentTypeSymbol QuestionSumarizer;
    public static readonly ChatbotAgentTypeSymbol SearchControl;
}

