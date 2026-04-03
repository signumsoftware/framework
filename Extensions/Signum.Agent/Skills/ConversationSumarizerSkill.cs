using Microsoft.Extensions.AI;

namespace Signum.Agent.Skills;

public class ConversationSumarizerSkill : AgentSkillCode
{
    public ConversationSumarizerSkill()
    {
        ShortDescription = "Summarizes conversation history for context window management";
        IsAllowed = () => true;
        Replacements = new Dictionary<string, Func<object?, string>>()
        {
            { "<ConversationToSummarize>", obj => (string)obj! }
        };
    }
}
