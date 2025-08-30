
using Azure.Core;
using Microsoft.Identity.Client;
using System.ComponentModel;

namespace Signum.Chatbot.Agents;


public class IntroductionSkill : ChatbotSkill
{
    public IntroductionSkill()
    {
        ShortDescription = "Introduction to the application's Chatbot";
        IsAllowed = () => true;
        Replacements = new Dictionary<string, Func<object?, string>>()
        {
            { "<CurrentApplication>", obj => Assembly.GetEntryAssembly()!.GetName().Name!.Before(".") }
        };
    }

    [SkillTool, Description("Gets the introduction for an skill")]
    public string Describe(string skillName)
    {
        var skill = ChatbotSkillLogic.GetSkill(skillName);

        return skill.GetInstruction(null);
    }
}

