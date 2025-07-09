namespace Signum.Chatbot.Agents;

internal static class SeachControlAgent
{
    public static void Register()
    {
        ChatbotAgentLogic.RegisterAgent(DefaultAgent.SearchControl, new ChatbotAgentCode
        {
            IsListed = () => true,
            CreateDefaultEntity = () => new ChatbotAgentEntity
            {
                ShortDescription = "SearchControl Signum Framework",
                ChatbotPrompts = new MList<ChatbotAgentPromptEmbedded>
                {
                    new ChatbotAgentPromptEmbedded
                    {
                        Content = """
                         Use the SearchControl of Signum Framework to help
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
