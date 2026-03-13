
using Azure.Core;
using Microsoft.Identity.Client;
using ModelContextProtocol.Server;
using Signum.Authorization;
using System.ComponentModel;

namespace Signum.Agent.Skills;


public class IntroductionSkill : AgentSkill
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

    [McpServerTool, Description("Gets the introduction for an skill")]
    public string Describe(string skillName)
    {
        //throw new InvalidOperationException("bla");

        if (skillName.Contains("error"))
            throw new Exception(skillName + " has an error");

        var skill = AgentSkillLogic.GetSkill(skillName);

        return skill.GetInstruction(null);
    }

    [McpServerTool, Description("Gets the introduction for an skill")]
    public Dictionary<string, string> ListSkillNames()
    {
        return AgentSkillLogic.SkillByName.Value.ToDictionary(a => a.Key, a => a.Value.ShortDescription);
    }
}

