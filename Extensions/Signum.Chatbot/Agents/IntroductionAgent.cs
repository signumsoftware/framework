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
                        
                        However, before answering any user question, you must always use the command $GetPrompt("agentName") to retrieve the relevant instructions 
                        and information from the appropriate agent. This applies regardless of how simple or complex the user's question is. 
                        Only after receiving the prompts from the agent may you answer the user's question.

                        If nothing matches the user's question, politely decline and explain what types of tasks you can assist with.
                        """
                    },
                }
            },
            MessageReplacement =
            {
                { "CurrentApplication",  _ =>  Assembly.GetEntryAssembly()!.GetName().Name!.Before(".") },
                { "Agents",  _ => ChatbotAgentLogic.GetListedAgents().ToString(a => $"* {a.Entity.Code.Key.After(".")}: {a.Entity.ShortDescription}", "\n") }
            },
            Resources = {
                //{"GetPrompt", (CommandArguments args) =>
                //    {
                //        StringBuilder sb = new StringBuilder();

                //        sb = ShortDescription

                //        return sb.ToString();
                //    }
                //}
            },
            
        });
    }
}
