
using Azure.Core;
using Microsoft.Identity.Client;

namespace Signum.Chatbot.Agents;

internal static class IntroductionAgent
{
    public static void Register()
    {
        ChatbotAgentLogic.RegisterAgent(DefaultAgent.Introduction, new ChatbotAgent
        {
            IsListedInIntroduction = () => false,
            CreateDefaultEntity = () => new ChatbotAgentEntity
            {
                ShortDescription = "Introduction",
                LongDescription = """
                        You are a chatbot of $<CurrentApplication>, and you can help with different tasks.

                        However, before answering any user question, you must always use the command $GetPrompt("agentName") to retrieve the relevant instructions 
                        and information from the appropriate agent. This applies regardless of how simple or complex the user's question is. 
                        Only after receiving the prompts from the agent may you answer the user's question.

                        If nothing matches the user's question, politely decline and explain what types of tasks you can assist with.
                        """,
                RelatedAgents = ChatbotAgentLogic.Agents.Where(kvp => kvp.Value.IsListedInIntroduction()).Select(a=>a.Key).ToMList()
            },
            MessageReplacements =
            {
                { "CurrentApplication",  _ =>  Assembly.GetEntryAssembly()!.GetName().Name!.Before(".") },
            },
            Tools = 
            {
                new DescribeChatbotAgentTool
                {
                    Name = "Describe",
                    Description = "Use this command to retrieve the prompt of a specific agent. Always use this command before answering any user question.",
                }
            },
        });
    }
}

