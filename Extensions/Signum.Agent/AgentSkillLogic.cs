using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Signum.Agent.Skills;
using Signum.API;
using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace Signum.Agent;

public static class AgentSkillLogic
{
    public static Dictionary<Type, AgentSkill> SkillsByType = new Dictionary<Type, AgentSkill>();

    public static ResetLazy<Dictionary<string, AgentSkill>> SkillByName =
        new ResetLazy<Dictionary<string, AgentSkill>>(() => SkillsByType.Values.ToDictionary(a => a.Name));

    public static ResetLazy<Dictionary<string, AITool>> AllTools;

    public static AgentSkill? IntroductionSkill;

    public static void Start(SchemaBuilder sb, Type? defaultChatbotSkillType = null)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        if (defaultChatbotSkillType != null)
            IntroductionSkill = GetSkill(defaultChatbotSkillType); // Assert registered

        AllTools = new ResetLazy<Dictionary<string, AITool>>(() => SkillsByType
            .SelectMany(a => a.Value.GetTools())
            .ToDictionaryEx(a => a.Name, StringComparer.InvariantCultureIgnoreCase));

        Register<QuestionSumarizerSkill>();
        Register<ConversationSumarizerSkill>();
    }

    public static AgentSkill GetSkill(string skillName)
    {
        return SkillByName.Value.GetOrThrow(skillName, "{0} not registered");
    }

    public static T GetSkill<T>() where T : AgentSkill
    {
        return (T)SkillsByType.GetOrThrow(typeof(T));
    }

    public static AgentSkill GetSkill(Type skillType)
    {
        return SkillsByType.GetOrThrow(skillType, "{0} not registered");
    }

    public static AgentSkill Register<T>() where T : AgentSkill, new()
    {
        if (SkillsByType.TryGetValue(typeof(T), out var existing))
            return existing; // no-op

        var skill = new T();
        SkillsByType[typeof(T)] = skill;
        AllTools?.Reset();
        SkillByName?.Reset();
        return skill;
    }

    public static AgentSkill WithSubSkill(this AgentSkill parent, SkillActivation activation, AgentSkill children)
    {
        GetSkill(children.GetType()); //Assert
        parent.SubSkills.Add(children.GetType(), activation);
        return parent;
    }
}


public abstract class AgentSkill
{
    public string Name => this.GetType().Name.Before("Skill");
    public string ShortDescription;
    public Func<bool> IsAllowed;
    public Dictionary<string, Func<object?, string>>? Replacements;

    public static string SkillsDirectory = Path.Combine(Path.GetDirectoryName(typeof(AgentSkill).Assembly.Location)!, "Skills");

    string? originalInstructions;
    public string OriginalInstructions
    {
        get { return originalInstructions ??= File.ReadAllText(Path.Combine(SkillsDirectory, this.Name + ".md")); }
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
            var skill = AgentSkillLogic.GetSkill(item.Key);

            sb.AppendLineLF("# Skill " + skill.Name);
            sb.AppendLineLF("**Summary**: " + skill.ShortDescription);
            sb.AppendLineLF();

            if (item.Value == SkillActivation.Eager)
                sb.AppendLineLF(skill.GetInstruction(null));
            else
                sb.AppendLineLF("Use the tool 'describe' to get more information about this skill and discover additional tools.");
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


    static JsonSerializerOptions JsonSerializationOptions = new JsonSerializerOptions().AddSignumJsonConverters();

    public virtual JsonSerializerOptions GetJsonSerializerOptions() => JsonSerializationOptions;

    public IEnumerable<AITool> GetToolsRecursive()
    {
        var list = GetTools().ToList();

        foreach (var item in SubSkills)
        {
            if (item.Value == SkillActivation.Eager)
            {
                var skill = AgentSkillLogic.GetSkill(item.Key);
                list.AddRange(skill.GetToolsRecursive());
            }
        }

        return list;
    }

    //public void AddMcpServer(IMcpServerBuilder builder)
    //{
    //    foreach (var toolMethod in this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
    //    {
    //        if (toolMethod.GetCustomAttribute<McpServerToolAttribute>() is not null)
    //        {
    //            builder.Services.AddSingleton(services => McpServerTool.Create(
    //                toolMethod,
    //                toolMethod.IsStatic ? null : this,
    //                new() { Services = services, SerializerOptions = this.GetJsonSerializerOptions() }));
    //        }
    //    }

    //    foreach (var subSkill in this.SubSkills.Where(a => a.Value == SkillActivation.Eager))
    //    {
    //        AgentSkillLogic.GetSkill(subSkill.Key).AddMcpServer(builder);
    //    }
    //}

}

public enum SkillActivation
{
    Eager,
    Lazy,
}

/// <summary>
/// Marks a [McpServerTool] method as a UI tool: the server never invokes its body.
/// Instead the controller detects this attribute before calling InvokeAsync and routes
/// the call to the client via the $!AssistantUITool streaming command.
/// The method body must throw InvalidOperationException("This method should not be called on the server").
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class UIToolAttribute : Attribute { }

public static partial class SignumMcpServerBuilderExtensions
{
    public static IMcpServerBuilder WithSignumSkill<T>(this IMcpServerBuilder builder)
        where T : AgentSkill, new()
    {
        var agent = AgentSkillLogic.Register<T>();
        builder.WithTools<T>(/*agent.GetJsonSerializerOptions()*/);
        return builder;
    }

    //public static IMcpServerBuilder WithSignumSkill(this IMcpServerBuilder builder, AgentSkill skill)
    //{
    //    skill.AddMcpServer(builder);
    //    //builder.Services.Configure((McpServerOptions opts) => opts.ServerInstructions = skill.GetInstruction(null));
    //    return builder;
    //}
}
