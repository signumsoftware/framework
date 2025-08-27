namespace Signum.Chatbot.Agents;

internal static class SeachControlAgent
{
    public static void Register()
    {
        ChatbotAgentLogic.RegisterAgent(DefaultAgent.SearchControl, new ChatbotAgent
        {
            IsListedInIntroduction = () => true,
            CreateDefaultEntity = () => new ChatbotAgentEntity
            {
                ShortDescription = "Helps searching any database table (not working)",
                LongDescription = """
                         Use the SearchControl of Signum Framework to help
                         """,
            },
            MessageReplacements =
            {
            }
        });
    }
}
