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
    internal static readonly AsyncThreadVariable<ResolvedSkillNode?> CurrentMcpRoot = Statics.ThreadVariable<ResolvedSkillNode?>("CurrentMcpRoot");

    // Registered code definitions: key = AgentSkillCode.Name (e.g. "Search", "Retrieve")
    public static Dictionary<string, (AgentSkillCode Default, Func<AgentSkillCode>? Factory)> RegisteredCodes = new();

    // Legacy instances kept for ChatbotLogic internal use (summarization)
    public static ConversationSumarizerSkill ConversationSumarizerSkill = null!;
    public static QuestionSumarizerSkill QuestionSumarizerSkill = null!;

    public static ResetLazy<Dictionary<AgentUseCaseSymbol, ResolvedSkillNode>> RootsByUseCase = null!;

    /// <summary>Registers an AgentSkillCode singleton (no cloning on entity resolve).</summary>
    public static T RegisterCode<T>(T instance) where T : AgentSkillCode
    {
        RegisteredCodes[instance.Name] = (instance, null);
        return instance;
    }

    /// <summary>Registers an AgentSkillCode with a factory so each entity gets a fresh instance with property overrides applied.</summary>
    public static T RegisterCode<T>(Func<T> factory) where T : AgentSkillCode
    {
        var defaultInstance = factory();
        RegisteredCodes[defaultInstance.Name] = (defaultInstance, () => factory());
        return defaultInstance;
    }

    public static void Start(SchemaBuilder sb, AgentSkillCode? introductionSkill = null)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        ConversationSumarizerSkill = RegisterCode(new ConversationSumarizerSkill());
        QuestionSumarizerSkill = RegisterCode(new QuestionSumarizerSkill());

        if (introductionSkill != null)
            RegisterCode(introductionSkill);

        SymbolLogic<AgentUseCaseSymbol>.Start(sb, () => [AgentUseCase.DefaultChatbot, AgentUseCase.Summarizer]);

        sb.Include<AgentSkillEntity>()
            .WithSave(AgentSkillOperation.Save)
            .WithDelete(AgentSkillOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name,
                e.SkillCode,
                e.Active,
                e.UseCase,
                e.ShortDescription,
            });

        sb.Schema.EntityEvents<AgentSkillEntity>().Saving += entity =>
        {
            if (!entity.IsNew && entity.SubSkills.IsGraphModified)
                ValidateNoCircularReferences(entity);
        };

        RootsByUseCase = sb.GlobalLazy(() =>
        {
            var allEntities = Database.Query<AgentSkillEntity>()
                .ToList()
                .ToDictionary(e => e.ToLite());

            return allEntities.Values
                .Where(e => e.UseCase != null && e.Active)
                .GroupBy(e => e.UseCase!)
                .ToDictionary(
                    g => g.Key,
                    g => ResolvedSkillNode.Resolve(g.SingleEx(), allEntities)
                );
        }, new InvalidateWith(typeof(AgentSkillEntity)));
    }

    static void ValidateNoCircularReferences(AgentSkillEntity entity)
    {
        using (new EntityCache(EntityCacheType.ForceNew))
        {
            EntityCache.AddFullGraph(entity);
            var allEntities = Database.RetrieveAll<AgentSkillEntity>();

            var graph = DirectedGraph<AgentSkillEntity>.Generate(
                allEntities,
                e => e.Is(entity)
                    ? entity.SubSkills.Select(s => s.Skill.RetrieveAndRemember()).ToList()
                    : e.SubSkills.Select(s => s.Skill.RetrieveAndRemember()).ToList()
            );

            var problems = graph.FeedbackEdgeSet().Edges.ToList();
            if (problems.Count > 0)
                throw new ApplicationException(
                    $"{problems.Count} cycle(s) found in AgentSkill graph:\n" +
                    problems.ToString(e => $"  {e.From.Name} → {e.To.Name}", "\n"));
        }
    }

    public static ResolvedSkillNode? GetRootForUseCase(AgentUseCaseSymbol symbol) =>
        RootsByUseCase.Value.TryGetC(symbol);

    /// <summary>Returns skill code metadata (properties marked [AgentSkillProperty]) for the frontend.</summary>
    public static List<SkillPropertyMeta> GetSkillCodeProperties(string skillCode)
    {
        if (!RegisteredCodes.TryGetValue(skillCode, out var entry))
            return new List<SkillPropertyMeta>();

        return entry.Default.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(pi => new { pi, attr = pi.GetCustomAttribute<AgentSkillPropertyAttribute>() })
            .Where(x => x.attr != null)
            .Select(x => new SkillPropertyMeta
            {
                PropertyName = x.pi.Name,
                AttributeName = x.attr!.GetType().Name.Before("Attribute"),
                ValueHint = x.attr.ValueHint,
                PropertyType = x.pi.PropertyType.TypeName(),
            })
            .ToList();
    }

    /// <summary>Returns the default short description and instructions for a registered SkillCode.</summary>
    public static SkillCodeDefaults GetSkillCodeDefaults(string skillCode)
    {
        if (!RegisteredCodes.TryGetValue(skillCode, out var entry))
            throw new KeyNotFoundException($"SkillCode '{skillCode}' is not registered.");

        return new SkillCodeDefaults
        {
            DefaultShortDescription = entry.Default.ShortDescription,
            DefaultInstructions = entry.Default.OriginalInstructions,
        };
    }
}

public class SkillPropertyMeta
{
    public string PropertyName { get; set; } = null!;
    public string AttributeName { get; set; } = null!;
    public string? ValueHint { get; set; }
    public string PropertyType { get; set; } = null!;
}

public class SkillCodeDefaults
{
    public string DefaultShortDescription { get; set; } = null!;
    public string DefaultInstructions { get; set; } = null!;
}

// ─── AgentSkillPropertyAttribute ──────────────────────────────────────────────

[AttributeUsage(AttributeTargets.Property)]
public class AgentSkillPropertyAttribute : Attribute
{
    /// <summary>Converts a string stored in the DB to the typed property value. Default uses ReflectionTools.Convert.</summary>
    public virtual object? ConvertFromString(string? value, Type targetType)
    {
        if (value == null)
            return null;

        return ReflectionTools.Convert(value, targetType);
    }

    public virtual string? ValueHint => null;
}

[AttributeUsage(AttributeTargets.Property)]
public class AgentSkillProperty_QueryListAttribute : AgentSkillPropertyAttribute
{
    /// <summary>Splits a comma-separated list of query keys and converts each via QueryLogic.ToQueryName.</summary>
    public override object? ConvertFromString(string? value, Type targetType)
    {
        if (value == null)
            return null;

        return value
            .Split(',')
            .Select(k => QueryLogic.ToQueryName(k.Trim()))
            .ToHashSet();
    }

    public override string? ValueHint => "Comma-separated query keys";
}

// ─── AgentSkillCode ───────────────────────────────────────────────────────────

public abstract class AgentSkillCode
{
    public string Name => this.GetType().Name.Before("Skill");
    public string ShortDescription { get; set; } = "";
    public Func<bool> IsAllowed { get; set; } = () => true;
    public Dictionary<string, Func<object?, string>>? Replacements;

    public static string SkillsDirectory = Path.Combine(Path.GetDirectoryName(typeof(AgentSkillCode).Assembly.Location)!, "Skills");

    string? originalInstructions;
    public string OriginalInstructions
    {
        get { return originalInstructions ??= File.ReadAllText(Path.Combine(SkillsDirectory, this.Name + ".md")); }
        set { originalInstructions = value; }
    }

    public string GetInstruction(object? context)
    {
        var text = OriginalInstructions;
        if (!Replacements.IsNullOrEmpty())
            text = text.Replace(Replacements.SelectDictionary(k => k, v => v(context)));
        return text;
    }

    public void ApplyPropertyOverrides(AgentSkillEntity entity)
    {
        foreach (var po in entity.PropertyOverrides)
        {
            var pi = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name == po.PropertyName && p.GetCustomAttribute<AgentSkillPropertyAttribute>() != null);

            if (pi == null)
                continue;

            var attr = pi.GetCustomAttribute<AgentSkillPropertyAttribute>()!;
            var value = attr.ConvertFromString(po.Value, pi.PropertyType);
            pi.SetValue(this, value);
        }
    }

    IEnumerable<AITool>? cachedTools;
    internal IEnumerable<AITool> GetTools()
    {
        return (cachedTools ??= this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
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
}

// ─── ResolvedSkillNode ────────────────────────────────────────────────────────

/// <summary>A runtime node combining an AgentSkillEntity with its resolved AgentSkillCode and sub-skill tree.</summary>
public class ResolvedSkillNode
{
    public AgentSkillEntity Entity { get; }
    public AgentSkillCode Code { get; }
    public List<(ResolvedSkillNode Node, SkillActivation Activation)> SubSkills { get; }

    public string Name => Entity.Name;
    public string ShortDescription => Entity.ShortDescription ?? Code.ShortDescription;

    ResolvedSkillNode(AgentSkillEntity entity, AgentSkillCode code, List<(ResolvedSkillNode, SkillActivation)> subSkills)
    {
        Entity = entity;
        Code = code;
        SubSkills = subSkills;
    }

    public string GetInstruction(object? context)
    {
        var text = Entity.Instructions ?? Code.OriginalInstructions;
        if (!Code.Replacements.IsNullOrEmpty())
            text = text.Replace(Code.Replacements.SelectDictionary(k => k, v => v(context)));

        var sb = new StringBuilder(text);
        foreach (var (node, activation) in SubSkills)
        {
            sb.AppendLineLF("# Skill " + node.Name);
            sb.AppendLineLF("**Summary**: " + node.ShortDescription);
            sb.AppendLineLF();
            if (activation == SkillActivation.Eager)
                sb.AppendLineLF(node.GetInstruction(null));
            else
                sb.AppendLineLF("Use the tool 'describe' to get more information about this skill and discover additional tools.");
        }

        return sb.ToString();
    }

    public IEnumerable<AITool> GetTools() => Code.GetTools();

    public IEnumerable<AITool> GetToolsRecursive()
    {
        var list = GetTools().ToList();
        foreach (var (node, activation) in SubSkills)
        {
            if (activation == SkillActivation.Eager)
                list.AddRange(node.GetToolsRecursive());
        }
        return list;
    }

    public ResolvedSkillNode? FindSkill(string name)
    {
        if (this.Name == name) return this;
        foreach (var (node, _) in SubSkills)
        {
            var found = node.FindSkill(name);
            if (found != null) return found;
        }
        return null;
    }

    public AITool? FindTool(string toolName)
    {
        var tool = GetTools().FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.InvariantCultureIgnoreCase));
        if (tool != null) return tool;
        foreach (var (node, _) in SubSkills)
        {
            var found = node.FindTool(toolName);
            if (found != null) return found;
        }
        return null;
    }

    public IEnumerable<ResolvedSkillNode> GetSkillsRecursive()
    {
        yield return this;
        foreach (var (node, _) in SubSkills)
            foreach (var s in node.GetSkillsRecursive())
                yield return s;
    }

    public IEnumerable<ResolvedSkillNode> GetEagerSkillsRecursive()
    {
        yield return this;
        foreach (var (node, activation) in SubSkills)
        {
            if (activation == SkillActivation.Eager)
                foreach (var s in node.GetEagerSkillsRecursive())
                    yield return s;
        }
    }

    public static ResolvedSkillNode Resolve(AgentSkillEntity entity, Dictionary<Lite<AgentSkillEntity>, AgentSkillEntity> allEntities)
    {
        var (defaultInstance, factory) = AgentSkillLogic.RegisteredCodes.GetOrThrow(entity.SkillCode,
            $"SkillCode '{entity.SkillCode}' not registered. Entity: '{entity.Name}'");

        AgentSkillCode code;
        if (factory != null && entity.PropertyOverrides.Any())
        {
            code = factory();
            code.ApplyPropertyOverrides(entity);
        }
        else
        {
            code = defaultInstance;
        }

        var subSkills = entity.SubSkills
            .Select(ss => (Resolve(allEntities.GetOrThrow(ss.Skill), allEntities), ss.Activation))
            .ToList();

        return new ResolvedSkillNode(entity, code, subSkills);
    }
}

public enum SkillActivation
{
    Eager,
    Lazy,
}

// ─── UIToolAttribute ──────────────────────────────────────────────────────────

/// <summary>
/// Marks a [McpServerTool] method as a UI tool: the server never invokes its body.
/// Instead the controller detects this attribute before calling InvokeAsync and routes
/// the call to the client via the $!AssistantUITool streaming command.
/// The method body must throw InvalidOperationException("This method should not be called on the server").
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class UIToolAttribute : Attribute { }

// ─── MCP server builder extension ────────────────────────────────────────────

public static partial class SignumMcpServerBuilderExtensions
{
    public static IMcpServerBuilder WithSignumSkill(this IMcpServerBuilder builder, AgentUseCaseSymbol? useCase = null)
    {
        useCase ??= AgentUseCase.DefaultChatbot;

        var sessionActivated = new ConcurrentDictionary<string, HashSet<string>>();

        ResolvedSkillNode GetRoot() =>
            AgentSkillLogic.GetRootForUseCase(useCase)
            ?? throw new InvalidOperationException($"No active AgentSkillEntity with UseCase = {useCase.Key}");

        HashSet<string> InitialActivated() =>
            GetRoot().GetEagerSkillsRecursive().Select(s => s.Name).ToHashSet();

        IEnumerable<string> GetActivated(string? sessionId) =>
            sessionId != null && sessionActivated.TryGetValue(sessionId, out var s)
                ? s
                : InitialActivated();

        McpServerTool? FindMcpTool(string toolName, IEnumerable<string> activatedNames)
        {
            var root = GetRoot();
            return activatedNames
                .Select(name => root.FindSkill(name))
                .OfType<ResolvedSkillNode>()
                .SelectMany(n => n.Code.GetMcpServerTools())
                .FirstOrDefault(t => t.ProtocolTool.Name == toolName);
        }

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
                var activated = GetActivated(ctx.Server.SessionId);
                var root = GetRoot();
                var tools = activated
                    .Select(name => root.FindSkill(name))
                    .OfType<ResolvedSkillNode>()
                    .SelectMany(n => n.Code.GetMcpServerTools())
                    .Select(t => t.ProtocolTool)
                    .ToList();

                return new ListToolsResult { Tools = tools };
            })
            .WithCallToolHandler(async (ctx, ct) =>
            {
                var toolName = ctx.Params!.Name;
                var activated = GetActivated(ctx.Server.SessionId);
                var tool = FindMcpTool(toolName, activated)
                    ?? throw new McpException($"Tool '{toolName}' not found");

                CallToolResult result;
                using (AgentSkillLogic.CurrentMcpRoot.Override(GetRoot()))
                using (AgentSkillLogic.IsMCP.Override(true))
                    result = await tool.InvokeAsync(ctx, ct);

                // When Describe is called for a Lazy skill, activate it for this session
                if (toolName == nameof(IntroductionSkill.Describe)
                    && ctx.Params.Arguments?.TryGetValue("skillName", out var je) == true
                    && je.GetString() is { } skillName
                    && ctx.Server.SessionId is { } sessionId)
                {
                    var root = GetRoot();
                    var newSkill = root.FindSkill(skillName);
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
