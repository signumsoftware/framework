namespace Signum.Chatbot.Agents;

internal static class IntroductionAgent
{
    public static void Register()
    {
        ChatbotAgentLogic.RegisterAgent(DefaultAgent.Introduction, new ChatbotAgentCode
        {
            IsListed = () => false,
            CreateDefaultEntity = () => new ChatbotAgentEntity
            {
                ShortDescription = "Introduction",
                ChatbotPrompts = new MList<ChatbotAgentPromptEmbedded>
                {
                    new ChatbotAgentPromptEmbedded
                    {
                        Content = """
                        You are a chatbot of $<CurrentApplication>, and you can help with different tasks: 

                        $<Agents>
                        
                        Depending on the user question, check which agent is more appropiate and call the command `$GetPrompt(agentName)` to get more info of how to use it,
                        you can also use `$GetPrompt(agentName, promptName)` to expand even further then you find a link like [promptName].

                        If nothing matches the user question, reject politely and explain what types of task can you help him on.
                        Even if the system promts are in english, answer the user in the language he made the question.
                        """
                    },
                },
            },
            MessageReplacement =
            {
                { "CurrentApplication",  _ =>  Assembly.GetEntryAssembly()!.GetName().Name ?? "" },
                { "Agents",  _ => ChatbotAgentLogic.GetListedAgents().ToString(a => $"* {a.Entity.Key}: {a.Entity.ShortDescription}", "\n") }
            }
        });
    }
}
