using Microsoft.Extensions.AI;
using Microsoft.Identity.Client;
using Signum.Chatbot.Agents;
using Signum.Chatbot.Skills;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Signum.Chatbot;

public static class ChatbotSkillLogic
{
    public static Dictionary<Type, ChatbotSkill> SkillsByType = new Dictionary<Type, ChatbotSkill>();

    public static ResetLazy<Dictionary<string, ChatbotSkill>> SkillByName =
        new ResetLazy<Dictionary<string, ChatbotSkill>>(() => SkillsByType.Values.ToDictionary(a => a.Name));

    public static ResetLazy<Dictionary<string, AITool>> AllTools;

    public static ChatbotSkill? IntroductionSkill;

    public static void Start(SchemaBuilder sb, ChatbotSkill? defaultChatbotSkill)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            if (defaultChatbotSkill != null)
            {
                GetSkill(defaultChatbotSkill.GetType()); //Assert
                IntroductionSkill = defaultChatbotSkill;
            }

            AllTools = new ResetLazy<Dictionary<string, AITool>>(() => SkillsByType
            .SelectMany(a => a.Value.GetTools())
            .ToDictionaryEx(a => a.Name, StringComparer.InvariantCultureIgnoreCase));

            new QuestionSumarizerSkill().Register();
        }
    }

    public static ChatbotSkill GetSkill(string skillName)
    {
        return SkillByName.Value.GetOrThrow(skillName, "{0} not registered");
    }

    public static T GetSkill<T>() where T : ChatbotSkill
    {
        return (T)SkillsByType.GetOrThrow(typeof(T));
    }

    public static ChatbotSkill GetSkill(Type skillType)
    {
        return SkillsByType.GetOrThrow(skillType, "{0} not registered");
    }

    public static ChatbotSkill Register(this ChatbotSkill chatbotSkill)
    {
        SkillsByType.Add(chatbotSkill.GetType(), chatbotSkill);
        AllTools?.Reset();
        SkillByName?.Reset();

        return chatbotSkill;
    }

    public static ChatbotSkill WithSubSkill(this ChatbotSkill parent, SkillActivation activation, ChatbotSkill children)
    {
        GetSkill(children.GetType()); //Assert
        parent.SubSkills.Add(children.GetType(), activation);
        return parent;
    }
}


public abstract class ChatbotSkill
{
    public string Name => this.GetType().Name.Before("Skill");
    public string ShortDescription;
    public Func<bool> IsAllowed;
    public Dictionary<string, Func<object?, string>>? Replacements;


    public static string TranslationDirectory = Path.Combine(Path.GetDirectoryName(typeof(ChatbotSkill).Assembly.Location)!, "Skills");

    string? originalInstructions;
    public string OriginalInstructions
    {
        get { return originalInstructions ??= File.ReadAllText(Path.Combine(TranslationDirectory, this.Name + ".md")); }
        set { originalInstructions = value; }
    }

    public string GetInstruction(object? context)
    {
        StringBuilder sb = new StringBuilder();
        if (Replacements.IsNullOrEmpty())
            sb.AppendLineLF(OriginalInstructions);
        else
            sb.AppendLineLF(OriginalInstructions.Replace(Replacements.SelectDictionary(k => k, v => v(context))));

        foreach (var item in SubSkills)
        {
            var skill = ChatbotSkillLogic.GetSkill(item.Key);

            sb.AppendLineLF("## Skill " + skill.Name);
            sb.AppendLineLF("**Summary**: " + skill.ShortDescription);
            sb.AppendLineLF();

            if (item.Value == SkillActivation.Eager)
                sb.AppendLineLF(skill.GetInstruction(null));
            else
                sb.AppendLineLF("Use the tool 'describe' to get more information about this skill.");
        }

        return sb.ToString(); 
    }


    IEnumerable<AITool> chatbotTools; 
    internal IEnumerable<AITool> GetTools()
    {
        return (chatbotTools ??= this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<SkillToolAttribute>() != null)
            .Select(m =>
            {
                Type types = Expression.GetDelegateType(m.GetParameters().Select(a => a.ParameterType).And(m.ReturnType).ToArray());
                Delegate del = Delegate.CreateDelegate(types, this, m);
                string? description = m.GetCustomAttribute<DescriptionAttribute>()?.Description;
                return (AITool)AIFunctionFactory.Create(del, m.Name, description);
            })
            .ToList());
    }

    public IEnumerable<AITool> GetToolsRecursive()
    {
        var list = GetTools().ToList();

        foreach (var item in SubSkills)
        {
            if (item.Value == SkillActivation.Eager)
            {
                var skill = ChatbotSkillLogic.GetSkill(item.Key);
                list.AddRange(skill.GetToolsRecursive());
            }
        }

        return list;
    }

    public Dictionary<Type, SkillActivation> SubSkills = new Dictionary<Type, SkillActivation>();
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SkillToolAttribute : Attribute
{
  
}

public enum SkillActivation
{
    Eager,
    Lazy,
}

