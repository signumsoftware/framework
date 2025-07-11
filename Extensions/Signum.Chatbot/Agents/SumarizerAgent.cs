namespace Signum.Chatbot.Agents;

internal static class SumarizerAgent
{
    public static void Register()
    {
        ChatbotAgentLogic.RegisterAgent(DefaultAgent.QuestionSumarizer, new ChatbotAgentCode
        {
            IsListed = () => false,
            CreateDefaultEntity = () => new ChatbotAgentEntity
            {
                ShortDescription = "Sumarizer",
                Descriptions = new MList<ChatbotAgentDescriptionsEmbedded>
                {
                    new ChatbotAgentDescriptionsEmbedded
                    {
                        Content = """
                        Summarize the user questions in 4 to 6 words.
                        If you think that the content of the questions is too small to give a meaningfull answer, just return "Pending". 
                        Here are the user questions:

                        $<Conversation>
                        """
                    },
                },
            },
            MessageReplacement =
            {
                { "Conversation",  ctx =>  ((ConversationHistory)ctx!).GetMessages().Where(a=>a.Role ==  ChatMessageRole.User).Select((a, i) => $"#Question {(i +1)}:#\n{a.Content}").ToString("\n\n").Etc(500) },
            }
        });
    }
}
