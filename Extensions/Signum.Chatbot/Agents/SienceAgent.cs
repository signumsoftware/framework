namespace Signum.Chatbot.Agents;

internal static class SienceAgent
{
    public static void Register()
    {
        ChatbotAgentLogic.RegisterAgent(DefaultAgent.Sience, new ChatbotAgentCode
        {
            IsListed = () => true,
            CreateDefaultEntity = () => new ChatbotAgentEntity
            {
                ShortDescription = "Sience and Technic",
                ChatbotPrompts = new MList<ChatbotAgentPromptEmbedded>
                {
                    new ChatbotAgentPromptEmbedded
                    {
                        Content = """
                         Gib alle mathematischen Ausdrücke und Formeln ausschließlich in LaTeX-Schreibweise zurück, 
                         wobei jede Formel in $-Zeichen eingeschlossen sein muss. Verwende keine anderen Formatierungen oder Zeichen für die Darstellung von Formeln außer den $-Zeichen. 
                         Achte darauf, dass keine zusätzlichen Zeichen wie Backticks oder andere Formatierungen vor oder nach den $-Zeichen stehen.""Gib alle mathematischen Ausdrücke und Formeln ausschließlich in LaTeX-Schreibweise zurück, 
                         wobei jede Formel in $-Zeichen eingeschlossen sein muss. Verwende keine anderen Formatierungen oder Zeichen für die Darstellung von Formeln außer den $-Zeichen. 
                         Achte darauf, dass keine zusätzlichen Zeichen wie Backticks oder andere Formatierungen vor oder nach den $-Zeichen stehen.
                         """
                    },
                },
            },
            MessageReplacement =
            {
            }
        });
    }
}
