using System.ComponentModel;

namespace Signum.Chatbot;


[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class ChatbotAgentSymbol : Symbol
{
    private ChatbotAgentSymbol() { }

    public ChatbotAgentSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[AutoInit]
public static class DefaultAgent
{
    public static readonly ChatbotAgentSymbol Introduction; 
    public static readonly ChatbotAgentSymbol QuestionSumarizer;
    public static readonly ChatbotAgentSymbol SearchControl;
}

