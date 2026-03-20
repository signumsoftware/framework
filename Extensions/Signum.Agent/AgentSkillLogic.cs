using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Signum.Agent.Skills;
using Signum.API;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Signum.Agent;

public static class AgentSkillLogic
{
    public static readonly AsyncThreadVariable<bool> IsMCP = Statics.ThreadVariable<bool>("IsMCP");

    public static AgentSkill? IntroductionSkill;

    public static ConversationSumarizerSkill ConversationSumarizerSkill = new ConversationSumarizerSkill();
    public static QuestionSumarizerSkill QuestionSumarizerSkill = new QuestionSumarizerSkill();

    public static void Start(SchemaBuilder sb, AgentSkill? introductionSkill = null)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        if (introductionSkill != null)
            IntroductionSkill = introductionSkill;
    }

    public static AgentSkill WithSubSkill(this AgentSkill parent, SkillActivation activation, AgentSkill child)
    {
        parent.SubSkills[child] = activation;
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
        foreach (var (skill, activation) in SubSkills)
        {
            sb.AppendLineLF("# Skill " + skill.Name);
            sb.AppendLineLF("**Summary**: " + skill.ShortDescription);
            sb.AppendLineLF();

            if (activation == SkillActivation.Eager)
                sb.AppendLineLF(skill.GetInstruction(null));
            else
                sb.AppendLineLF("Use the tool 'describe' to get more information about this skill and discover additional tools.");
        }
    }

    public Dictionary<AgentSkill, SkillActivation> SubSkills = new Dictionary<AgentSkill, SkillActivation>();

    IEnumerable<AITool>? chatbotTools;
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

    internal IEnumerable<McpServerTool> GetMcpServerTools() =>
        GetTools().Select(t => McpServerTool.Create((AIFunction)t, new McpServerToolCreateOptions
        {
            SerializerOptions = GetJsonSerializerOptions(),
        }));

    static JsonSerializerOptions JsonSerializationOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    }.AddSignumJsonConverters();

    public virtual JsonSerializerOptions GetJsonSerializerOptions() => JsonSerializationOptions;

    public IEnumerable<AITool> GetToolsRecursive()
    {
        var list = GetTools().ToList();

        foreach (var (skill, activation) in SubSkills)
        {
            if (activation == SkillActivation.Eager)
                list.AddRange(skill.GetToolsRecursive());
        }

        return list;
    }

    public AgentSkill? FindSkill(string name)
    {
        if (this.Name == name) return this;
        foreach (var (skill, _) in SubSkills)
        {
            var found = skill.FindSkill(name);
            if (found != null) return found;
        }
        return null;
    }

    public AITool? FindTool(string name)
    {
        var tool = GetTools().FirstOrDefault(t => t.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        if (tool != null) return tool;
        foreach (var (skill, _) in SubSkills)
        {
            var found = skill.FindTool(name);
            if (found != null) return found;
        }
        return null;
    }

    public IEnumerable<AgentSkill> GetSkillsRecursive()
    {
        yield return this;
        foreach (var (skill, _) in SubSkills)
            foreach (var s in skill.GetSkillsRecursive())
                yield return s;
    }

    public IEnumerable<AgentSkill> GetEagerSkillsRecursive()
    {
        yield return this;
        foreach (var (skill, activation) in SubSkills)
        {
            if (activation == SkillActivation.Eager)
                foreach (var s in skill.GetEagerSkillsRecursive())
                    yield return s;
        }
    }
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
    public static IMcpServerBuilder WithSignumSkill(this IMcpServerBuilder builder, AgentSkill rootSkill)
    {
        var allSkillTools = rootSkill.GetSkillsRecursive()
            .ToDictionary(s => s.Name, s => s.GetMcpServerTools().ToList());

        var sessionActivated = new ConcurrentDictionary<string, HashSet<string>>();

        HashSet<string> InitialActivated() =>
            rootSkill.GetEagerSkillsRecursive().Select(s => s.Name).ToHashSet();

        IEnumerable<string> GetActivated(string? sessionId) =>
            sessionId != null && sessionActivated.TryGetValue(sessionId, out var s)
                ? s
                : InitialActivated();

        return builder
            .WithHttpTransport(options =>
            {
#pragma warning disable MCPEXP002
                options.RunSessionHandler = async (httpContext, mcpServer, token) =>
                {
                    if (mcpServer.SessionId != null)
                        sessionActivated[mcpServer.SessionId] = InitialActivated();
                    try { await mcpServer.RunAsync(token); }
                    finally
                    {
                        if (mcpServer.SessionId != null)
                            sessionActivated.TryRemove(mcpServer.SessionId, out _);
                    }
                };
#pragma warning restore MCPEXP002
            })
            .WithListToolsHandler(async (ctx, ct) =>
            {
                var tools = GetActivated(ctx.Server.SessionId)
                    .Where(allSkillTools.ContainsKey)
                    .SelectMany(n => allSkillTools[n])
                    .Select(t => t.ProtocolTool)
                    .ToList();

                return new ListToolsResult { Tools = tools };
            })
            .WithCallToolHandler(async (ctx, ct) =>
            {
                var toolName = ctx.Params!.Name;
                var tool = GetActivated(ctx.Server.SessionId)
                    .Where(allSkillTools.ContainsKey)
                    .SelectMany(n => allSkillTools[n])
                    .FirstOrDefault(t => t.ProtocolTool.Name == toolName)
                    ?? throw new McpException($"Tool '{toolName}' not found");

                CallToolResult result;
                using (AgentSkillLogic.IsMCP.Override(true))
                    result = await tool.InvokeAsync(ctx, ct);


                // When Describe is called for a Lazy skill, activate it for this session
                if (toolName == nameof(IntroductionSkill.Describe)
                    && ctx.Params.Arguments?.TryGetValue("skillName", out var je) == true
                    && je.GetString() is { } skillName
                    && ctx.Server.SessionId is { } sessionId)
                {
                    var newSkill = rootSkill.FindSkill(skillName);
                    if (newSkill != null && sessionActivated.TryGetValue(sessionId, out var skills))
                    {
                        foreach (var s in newSkill.GetEagerSkillsRecursive())
                            skills.Add(s.Name);
                        await ctx.Server.SendNotificationAsync(NotificationMethods.ToolListChangedNotification, ct);
                    }
                }

                return result;
            });
    }
}
