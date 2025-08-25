using System.ComponentModel;

namespace Signum.Chatbot;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class ChatbotAgentEntity: Entity
{
    public ChatbotAgentCodeSymbol Code { get; set; }

    [StringLengthValidator(Max = 100)]
    public string ShortDescription { get; set; }

    public string LongDescription { get; set; }

    [PreserveOrder, NoRepeatValidator]
    public MList<ChatbotAgentCodeSymbol> RelatedAgents { get; set; } = new MList<ChatbotAgentCodeSymbol>();

    protected override bool IsPropertyReadonly(PropertyInfo pi)
    {
        if (pi.Name == nameof(Code))
            return true;

        return base.IsPropertyReadonly(pi);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(()  => Code.ToString());
}


[AutoInit]
public static class ChatbotAgentOperation
{
    public static readonly ExecuteSymbol<ChatbotAgentEntity> Save;
    public static readonly DeleteSymbol<ChatbotAgentEntity> Delete;
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

