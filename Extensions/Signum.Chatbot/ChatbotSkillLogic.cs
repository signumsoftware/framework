using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using ModelContextProtocol.Server;
using Npgsql.Internal;
using Signum.API;
using Signum.Chatbot.Agents;
using Signum.Chatbot.Skills;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

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

        FillSubInstructions(sb);

        return sb.ToString();
    }

    public string FillSubInstructions()
    {
        var sb = new StringBuilder();
        FillSubInstructions(sb);
        return sb.ToString();
    }

    private void FillSubInstructions(StringBuilder sb)
    {
        foreach (var item in SubSkills)
        {
            var skill = ChatbotSkillLogic.GetSkill(item.Key);

            sb.AppendLineLF("# Skill " + skill.Name);
            sb.AppendLineLF("**Summary**: " + skill.ShortDescription);
            sb.AppendLineLF();

            if (item.Value == SkillActivation.Eager)
                sb.AppendLineLF(skill.GetInstruction(null));
            else
                sb.AppendLineLF("Use the tool 'describe' to get more information about this skill.");
        }
    }

    public Dictionary<Type, SkillActivation> SubSkills = new Dictionary<Type, SkillActivation>();

    IEnumerable<AITool> chatbotTools; 
    internal IEnumerable<AITool> GetTools()
    {
        return (chatbotTools ??= this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
            .Select(m =>
            {
                Type delType = Expression.GetDelegateType(m.GetParameters().Select(a => a.ParameterType).And(m.ReturnType).ToArray());
                Delegate del = m.IsStatic ?
                    Delegate.CreateDelegate(delType, m) :
                    Delegate.CreateDelegate(delType, this, m);

                string? description = m.GetCustomAttribute<DescriptionAttribute>()?.Description;
                return (AITool)AIFunctionFactory.Create(del, m.Name, description, GetJsonSerializerOptions());
            })
            .ToList());
    }

    protected virtual JsonSerializerOptions GetJsonSerializerOptions() => SignumServer.JsonSerializerOptions;

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

    public void AddMcpServer(IMcpServerBuilder builder)
    {
        foreach (var toolMethod in this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            if (toolMethod.GetCustomAttribute<McpServerToolAttribute>() is not null)
            {
                builder.Services.AddSingleton(services => McpServerTool.Create(
                    toolMethod,
                    toolMethod.IsStatic ? null : this,
                    new() { Services = services, SerializerOptions = this.GetJsonSerializerOptions() }));
            }
        }

        foreach (var subSkill in this.SubSkills.Where(a => a.Value == SkillActivation.Eager))
        {
            ChatbotSkillLogic.GetSkill(subSkill.Key).AddMcpServer(builder);
        }
    }

}

public enum SkillActivation
{
    Eager,
    Lazy,
}

public static partial class SignumMcpServerBuilderExtensions
{
    public static IMcpServerBuilder WithSignumSkill(this IMcpServerBuilder builder, ChatbotSkill skill)
    {
        skill.AddMcpServer(builder);

        //builder.Services.Configure((McpServerOptions opts) => opts.ServerInstructions = skill.GetInstruction(null));
        return builder;
    }
}

