
using ModelContextProtocol.Server;
using Signum.Authorization;
using System.ComponentModel;

namespace Signum.Agent.Skills;


public class IntroductionSkill : AgentSkillCode
{
    public IntroductionSkill()
    {
        ShortDescription = "Introduction to the application's Chatbot";
        IsAllowed = () => true;
        Replacements = new Dictionary<string, Func<object?, string>>()
        {
            { "<CurrentApplication>", obj => Assembly.GetEntryAssembly()!.GetName().Name!.Before(".") },
        };
    }

    [McpServerTool, Description("Gets the instructions for a skill and discovers its tools")]
    public string Describe(string skillName)
    {
        if (skillName.Contains("error"))
            throw new Exception(skillName + " has an error");

        var skill = this.FindSkill(skillName)
            ?? throw new KeyNotFoundException($"Skill '{skillName}' not found");

        return skill.GetInstruction(null);
    }

    [McpServerTool, Description("List available skills with a short description, start here to discover new tools.")]
    public Dictionary<string, string> ListSkillNames()
    {
        return this.GetSkillsRecursive().ToDictionary(a => a.Name, a => a.ShortDescription);
    }
}


