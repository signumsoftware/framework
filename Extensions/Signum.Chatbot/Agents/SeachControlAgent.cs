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
                ShortDescription = "Helps searching any database table (not working)",
                Descriptions = new MList<ChatbotAgentDescriptionsEmbedded>
                {
                    new ChatbotAgentDescriptionsEmbedded
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
