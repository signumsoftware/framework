using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Signum.Agent.Skills;
using Signum.API;
using Signum.Engine.Sync;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Signum.Agent;

public static class AgentSkillLogic
{
    public static readonly AsyncThreadVariable<bool> IsMCP = Statics.ThreadVariable<bool>("IsMCP");

    public static Dictionary<string, Type> RegisteredCodes = new();

    public static ConversationSumarizerSkill ConversationSumarizerSkill = null!;
    public static QuestionSumarizerSkill QuestionSumarizerSkill = null!;

    public static ResetLazy<Dictionary<AgentUseCaseSymbol, AgentSkillCode>> RootsByUseCase = null!;

    public static void RegisterCode<T>() where T : AgentSkillCode
    {
        RegisteredCodes[typeof(T).FullName!] = typeof(T);
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        RegisterCode<ConversationSumarizerSkill>();
        RegisterCode<QuestionSumarizerSkill>();

        ConversationSumarizerSkill = new ConversationSumarizerSkill();
        QuestionSumarizerSkill = new QuestionSumarizerSkill();

        SymbolLogic<AgentUseCaseSymbol>.Start(sb, () => [AgentUseCase.DefaultChatbot, AgentUseCase.Summarizer]);

        sb.Include<AgentSkillCodeEntity>()
            .WithQuery(() => e => new { Entity = e, e.Id, e.FullClassName });

        sb.Schema.Generating += Schema_Generating;
        sb.Schema.Synchronizing += Schema_Synchronizing;

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
                    g => ResolveCode(g.SingleEx(), allEntities)
                );
        }, new InvalidateWith(typeof(AgentSkillEntity)));
    }

    static SqlPreCommand? Schema_Generating()
    {
        var table = Schema.Current.Table<AgentSkillCodeEntity>();
        return GenerateCodeEntities()
            .Select(e => table.InsertSqlSync(e))
            .Combine(Spacing.Simple);
    }

    static SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        var table = Schema.Current.Table<AgentSkillCodeEntity>();
        var should = GenerateCodeEntities().ToDictionary(e => e.FullClassName);
        var current = Administrator.TryRetrieveAll<AgentSkillCodeEntity>(replacements)
            .ToDictionary(e => e.FullClassName);

        return Synchronizer.SynchronizeScript(Spacing.Double, should, current,
            createNew: (_, s) => table.InsertSqlSync(s),
            removeOld: (_, c) => table.DeleteSqlSync(c, e => e.FullClassName == c.FullClassName),
            mergeBoth: (_, s, c) => table.UpdateSqlSync(c, e => e.FullClassName == c.FullClassName));
    }

    static List<AgentSkillCodeEntity> GenerateCodeEntities() =>
        RegisteredCodes.Keys.Select(fc => new AgentSkillCodeEntity { FullClassName = fc }).ToList();

    public static AgentSkillCode ResolveCode(
        AgentSkillEntity entity,
        Dictionary<Lite<AgentSkillEntity>, AgentSkillEntity> allEntities)
    {
        var type = RegisteredCodes.GetOrThrow(entity.SkillCode.FullClassName,
            $"AgentSkillCode type '{entity.SkillCode.FullClassName}' is not registered.");

        var code = (AgentSkillCode)Activator.CreateInstance(type)!;
        code.InstanceName = entity.Name;

        if (entity.ShortDescription != null)
            code.ShortDescription = entity.ShortDescription;
        if (entity.Instructions != null)
            code.OriginalInstructions = entity.Instructions;

        code.ApplyPropertyOverrides(entity);

        foreach (var ss in entity.SubSkills)
            code.SubSkills.Add((ResolveCode(allEntities.GetOrThrow(ss.Skill), allEntities), ss.Activation));

        return code;
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

    public static AgentSkillCode? GetRootForUseCase(AgentUseCaseSymbol symbol) =>
        RootsByUseCase.Value.TryGetC(symbol);

    public static SkillCodeInfo GetSkillCodeInfo(string fullClassName)
    {
        if (!RegisteredCodes.TryGetValue(fullClassName, out var type))
            throw new KeyNotFoundException($"AgentSkillCode type '{fullClassName}' is not registered.");

        var instance = (AgentSkillCode)Activator.CreateInstance(type)!;

        var properties = type
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

        return new SkillCodeInfo
        {
            DefaultShortDescription = instance.ShortDescription,
            DefaultInstructions = instance.OriginalInstructions,
            Properties = properties,
        };
    }
}

public class SkillCodeInfo
{
    public string DefaultShortDescription { get; set; } = null!;
    public string DefaultInstructions { get; set; } = null!;
    public List<SkillPropertyMeta> Properties { get; set; } = null!;
}

public class SkillPropertyMeta
{
    public string PropertyName { get; set; } = null!;
    public string AttributeName { get; set; } = null!;
    public string? ValueHint { get; set; }
    public string PropertyType { get; set; } = null!;
}

[AttributeUsage(AttributeTargets.Property)]
public class AgentSkillPropertyAttribute : Attribute
{
    public virtual object? ConvertFromString(string? value, Type targetType)
    {
        if (value == null)
            return null;

        return ReflectionTools.ChangeType(value, targetType);
    }

    public virtual string? ValidateValue(string? value, Type targetType) => null;

    public virtual string? ValueHint => null;
}

[AttributeUsage(AttributeTargets.Property)]
public class AgentSkillProperty_QueryListAttribute : AgentSkillPropertyAttribute
{
    public override object? ConvertFromString(string? value, Type targetType)
    {
        if (value == null)
            return null;

        return value
            .Split(',')
            .Select(k => QueryLogic.ToQueryName(k.Trim()))
            .ToHashSet();
    }

    public override string? ValidateValue(string? value, Type targetType)
    {
        if (value == null)
            return null;

        var errors = value.Split(',')
            .Select(k => k.Trim())
            .Where(k => k.HasText() && QueryLogic.ToQueryName(k) == null)
            .ToList();

        return errors.Any()
            ? $"Unknown query key(s): {errors.ToString(", ")}"
            : null;
    }

    public override string? ValueHint => "Comma-separated query keys";
}

public abstract class AgentSkillCode
{
    public string? InstanceName { get; internal set; }
    public string Name => InstanceName ?? this.GetType().Name.Before("Skill");

    public string ShortDescription { get; set; } = "";
    public Func<bool> IsAllowed { get; set; } = () => true;
    public Dictionary<string, Func<object?, string>>? Replacements;

    public static string SkillsDirectory = Path.Combine(
        Path.GetDirectoryName(typeof(AgentSkillCode).Assembly.Location)!, "Skills");

    string? originalInstructions;
    public string OriginalInstructions
    {
        get { return originalInstructions ??= File.ReadAllText(Path.Combine(SkillsDirectory, this.GetType().Name.Before("Skill") + ".md")); }
        set { originalInstructions = value; }
    }

    // Populated from DB at resolve time; never set in code.
    public List<(AgentSkillCode Code, SkillActivation Activation)> SubSkills { get; } = new();

    public string GetInstruction(object? context)
    {
        var text = OriginalInstructions;
        if (!Replacements.IsNullOrEmpty())
            text = text.Replace(Replacements.SelectDictionary(k => k, v => v(context)));

        if (SubSkills.Any())
        {
            var sb = new StringBuilder(text);
            foreach (var (sub, activation) in SubSkills)
            {
                sb.AppendLineLF("# Skill " + sub.Name);
                sb.AppendLineLF("**Summary**: " + sub.ShortDescription);
                sb.AppendLineLF();
                if (activation == SkillActivation.Eager)
                    sb.AppendLineLF(sub.GetInstruction(null));
                else
                    sb.AppendLineLF("Use the tool 'describe' to get more information about this skill and discover additional tools.");
            }
            return sb.ToString();
        }

        return text;
    }

    public void ApplyPropertyOverrides(AgentSkillEntity entity)
    {
        foreach (var po in entity.PropertyOverrides)
        {
            var pi = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name == po.PropertyName
                    && p.GetCustomAttribute<AgentSkillPropertyAttribute>() != null);

            if (pi == null) continue;

            var attr = pi.GetCustomAttribute<AgentSkillPropertyAttribute>()!;
            var value = attr.ConvertFromString(po.Value, pi.PropertyType);
            pi.SetValue(this, value);
        }
    }

    public AgentSkillCode? FindSkill(string name)
    {
        if (this.Name == name) return this;
        foreach (var (sub, _) in SubSkills)
        {
            var found = sub.FindSkill(name);
            if (found != null) return found;
        }
        return null;
    }

    public AITool? FindTool(string toolName)
    {
        var tool = GetTools().FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.InvariantCultureIgnoreCase));
        if (tool != null) return tool;
        foreach (var (sub, _) in SubSkills)
        {
            var found = sub.FindTool(toolName);
            if (found != null) return found;
        }
        return null;
    }

    public IEnumerable<AgentSkillCode> GetSkillsRecursive()
    {
        yield return this;
        foreach (var (sub, _) in SubSkills)
            foreach (var s in sub.GetSkillsRecursive())
                yield return s;
    }

    public IEnumerable<AgentSkillCode> GetEagerSkillsRecursive()
    {
        yield return this;
        foreach (var (sub, activation) in SubSkills)
            if (activation == SkillActivation.Eager)
                foreach (var s in sub.GetEagerSkillsRecursive())
                    yield return s;
    }

    public IEnumerable<AITool> GetToolsRecursive()
    {
        var list = GetTools().ToList();
        foreach (var (sub, activation) in SubSkills)
            if (activation == SkillActivation.Eager)
                list.AddRange(sub.GetToolsRecursive());
        return list;
    }

    IEnumerable<AITool>? cachedTools;
    internal IEnumerable<AITool> GetTools()
    {
        return (cachedTools ??= this.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
            .Select(m =>
            {
                Type delType = Expression.GetDelegateType(
                    m.GetParameters().Select(a => a.ParameterType).And(m.ReturnType).ToArray());
                Delegate del = m.IsStatic
                    ? Delegate.CreateDelegate(delType, m)
                    : Delegate.CreateDelegate(delType, this, m);
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

public enum SkillActivation
{
    Eager,
    Lazy,
}

/// <summary>
/// Marks a [McpServerTool] as a UI tool: the server never invokes its body.
/// The controller routes the call to the client via the $!AssistantUITool streaming command.
/// The method body must throw InvalidOperationException.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class UIToolAttribute : Attribute { }

public static partial class SignumMcpServerBuilderExtensions
{
    public static IMcpServerBuilder WithSignumSkill(this IMcpServerBuilder builder, AgentUseCaseSymbol? useCase = null)
    {
        useCase ??= AgentUseCase.DefaultChatbot;

        var sessionActivated = new ConcurrentDictionary<string, HashSet<string>>();

        AgentSkillCode GetRoot() =>
            AgentSkillLogic.GetRootForUseCase(useCase)
            ?? throw new InvalidOperationException($"No active AgentSkillEntity with UseCase = {useCase.Key}");

        HashSet<string> InitialActivated() =>
            GetRoot().GetEagerSkillsRecursive().Select(s => s.Name).ToHashSet();

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
                var root = GetRoot();
                var activated = GetActivated(ctx.Server.SessionId);
                var tools = activated
                    .Select(name => root.FindSkill(name))
                    .OfType<AgentSkillCode>()
                    .SelectMany(s => s.GetMcpServerTools())
                    .Select(t => t.ProtocolTool)
                    .ToList();

                return new ListToolsResult { Tools = tools };
            })
            .WithCallToolHandler(async (ctx, ct) =>
            {
                var toolName = ctx.Params!.Name;
                var root = GetRoot();
                var activated = GetActivated(ctx.Server.SessionId);

                var tool = activated
                    .Select(name => root.FindSkill(name))
                    .OfType<AgentSkillCode>()
                    .SelectMany(s => s.GetMcpServerTools())
                    .FirstOrDefault(t => t.ProtocolTool.Name == toolName)
                    ?? throw new McpException($"Tool '{toolName}' not found");

                CallToolResult result;
                using (AgentSkillLogic.IsMCP.Override(true))
                    result = await tool.InvokeAsync(ctx, ct);

                if (toolName == nameof(IntroductionSkill.Describe)
                    && ctx.Params.Arguments?.TryGetValue("skillName", out var je) == true
                    && je.GetString() is { } skillName
                    && ctx.Server.SessionId is { } sessionId)
                {
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
