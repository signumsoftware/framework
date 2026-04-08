using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Signum.Agent.Skills;
using Signum.Engine.Sync;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Signum.Agent;


public static class AgentLogic
{
    public static readonly AsyncThreadVariable<bool> IsMCP = Statics.ThreadVariable<bool>("IsMCP");

    public static Dictionary<AgentSymbol, Func<SkillCode>> RegisteredAgents = new();

    public static ResetLazy<ConcurrentDictionary<AgentSymbol, SkillCode>> SkillCodeByAgent = null!;

    public static ResetLazy<FrozenDictionary<AgentSymbol, SkillCustomizationEntity>> SkillCustomizationByAgent = null!;


    public static void RegisterAgent(AgentSymbol agent, Func<SkillCode> factory)
    {
        RegisteredAgents[agent] = factory;

        using (SkillCodeLogic.AutoRegister())
            factory(); //Check if it works at registration time
    }

    public static void Start(SchemaBuilder sb, Func<SkillCode>? getChatBot)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        SkillCodeLogic.Start(sb);

        SkillCodeLogic.Register<ConversationSumarizerSkill>();
        SkillCodeLogic.Register<QuestionSumarizerSkill>();

        if (getChatBot != null)
            RegisterAgent(DefaultAgent.Chatbot, getChatBot);

        RegisterAgent(DefaultAgent.QuestionSummarizer, () => new QuestionSumarizerSkill());
        RegisterAgent(DefaultAgent.ConversationSumarizer, () => new ConversationSumarizerSkill());

        SymbolLogic<AgentSymbol>.Start(sb, () => RegisteredAgents.Keys);

        
        sb.Include<SkillCustomizationEntity>()
            .WithUniqueIndex(a => a.Agent, a => a.Agent != null)
            .WithSave(SkillCustomizationOperation.Save)
            .WithDelete(SkillCustomizationOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.SkillCode,
                e.Agent,
                e.ShortDescription,
            });

        new Graph<SkillCustomizationEntity>.ConstructFrom<AgentSymbol>(SkillCustomizationOperation.CreateFromAgent)
        {
            Construct = (agentSymbol, _) =>
            {
                if (!RegisteredAgents.TryGetValue(agentSymbol, out var factory))
                    return new SkillCustomizationEntity { Agent = agentSymbol };

                var code = factory();
                return code.ToCustomizationEntity(agentSymbol);
            }
        }.Register();

        sb.Schema.EntityEvents<SkillCustomizationEntity>().Saving += entity =>
        {
            if (!entity.IsNew && entity.SubSkills.IsGraphModified)
                ValidateNoCircularReferences(entity);
        };

        SkillCodeByAgent = sb.GlobalLazy(() => new ConcurrentDictionary<AgentSymbol, SkillCode>(),
            new InvalidateWith(typeof(SkillCustomizationEntity)));


        SkillCustomizationByAgent = sb.GlobalLazy(() => Database.Query<SkillCustomizationEntity>().Where(a=>a.Agent != null).ToFrozenDictionaryEx(a=>a.Agent!),
            new InvalidateWith(typeof(SkillCustomizationEntity)));
    }
   
    public static SkillCode ToSkillCode(this SkillCustomizationEntity entity)
    {
        var code = (SkillCode)Activator.CreateInstance(entity.SkillCode.ToType())!;
        code.Customization = entity.ToLite();

        if (entity.ShortDescription != null)
            code.ShortDescription = entity.ShortDescription;
        if (entity.Instructions != null)
            code.OriginalInstructions = entity.Instructions;

        code.ApplyPropertyOverrides(entity);

        foreach (var ss in entity.SubSkills)
        {
            SkillCode subCode =
                ss.Skill is SkillCustomizationEntity c ? c.ToSkillCode() :
                ss.Skill is SkillCodeEntity sc ? (SkillCode)Activator.CreateInstance(sc.ToType())! :
                    throw new UnexpectedValueException(ss.Skill);

            code.SubSkills.Add((subCode, ss.Activation));
        }

        return code;
    }

    static void ValidateNoCircularReferences(SkillCustomizationEntity entity)
    {
        using (new EntityCache(EntityCacheType.ForceNew))
        {
            EntityCache.AddFullGraph(entity);
            var allEntities = Database.RetrieveAll<SkillCustomizationEntity>();

            var graph = DirectedGraph<SkillCustomizationEntity>.Generate(
                allEntities,
                e =>
                {
                    var subSkills = e.Is(entity) ? entity.SubSkills : e.SubSkills;
                    return subSkills
                        .Where(s => s.Skill is SkillCustomizationEntity)
                        .Select(s => (SkillCustomizationEntity)s.Skill)
                        .ToList();
                }
            );

            var problems = graph.FeedbackEdgeSet().Edges.ToList();
            if (problems.Count > 0)
                throw new ApplicationException(
                    $"{problems.Count} cycle(s) found in AgentSkill graph:\n" +
                    problems.ToString(e => $"  {e.From} → {e.To}", "\n"));
        }
    }

    public static SkillCode GetEffectiveSkillCode(this AgentSymbol agentSymbol)
    {
        return SkillCodeByAgent.Value.GetOrCreate(agentSymbol, s =>
        {
            var skillCustomization = SkillCustomizationByAgent.Value.TryGetC(s);
            if(skillCustomization != null)
                skillCustomization.ToSkillCode();

            var def = RegisteredAgents.GetOrThrow(agentSymbol);

            return def();
        });

    }

    static SkillCustomizationEntity ToCustomizationEntity(this SkillCode code, AgentSymbol? agent)
    {
        var type = code.GetType();

        var entity = new SkillCustomizationEntity
        {
            SkillCode = SkillCodeLogic.ToSkillCodeEntity(type),
            Agent = agent,
            ShortDescription = code.ShortDescription,
            Instructions = code.OriginalInstructions,
        };

        foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = pi.GetCustomAttribute<SkillPropertyAttribute>();
            if (attr == null) continue;

            var currentStr = attr.ConvertValueToString(pi.GetValue(code), pi.PropertyType);
            entity.Properties.Add(new SkillPropertyEmbedded
            {
                PropertyName = pi.Name,
                Value = currentStr,
            });
        }

        foreach (var (sub, activation) in code.SubSkills)
        {
            Entity skillLite = !sub.IsDefault() ? sub.ToCustomizationEntity(agent: null) :
                SkillCodeLogic.ToSkillCodeEntity(sub.GetType());
            entity.SubSkills.Add(new SubSkillEmbedded { Skill = skillLite, Activation = activation });
        }

        return entity;
    }

    
}

[AttributeUsage(AttributeTargets.Property)]
public class SkillPropertyAttribute : Attribute
{
    public virtual object? ConvertFromString(string? value, Type targetType)
    {
        if (value == null)
            return null;

        return ReflectionTools.ChangeType(value, targetType);
    }

    public virtual string? ConvertValueToString(object? value, Type targetType) => value?.ToString();

    public virtual string? ValidateValue(string? value, Type targetType) => null;

    public virtual string? ValueHint => null;
}

[AttributeUsage(AttributeTargets.Property)]
public class SkillProperty_QueryListAttribute : SkillPropertyAttribute
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

    public override string? ConvertValueToString(object? value, Type targetType)
    {
        if (value is not System.Collections.IEnumerable enumerable) return value?.ToString();
        return enumerable.Cast<object>().Select(q => QueryLogic.GetQueryEntity(q).Key).ToString(", ");
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
    public static IMcpServerBuilder WithSignumSkill(this IMcpServerBuilder builder, AgentSymbol useCase)
    {
        var sessionActivated = new ConcurrentDictionary<string, HashSet<string>>();

        SkillCode GetRoot() =>
            AgentLogic.GetEffectiveSkillCode(useCase)
            ?? throw new InvalidOperationException($"No active AgentSkillEntity with UseCase = {useCase.Key}");

        IEnumerable<string> GetActivated(SkillCode code, string? sessionId) =>
            sessionId != null && sessionActivated.TryGetValue(sessionId, out var s) ? s
                : code.GetEagerSkillsRecursive().Select(s => s.Name).ToHashSet();

        return builder
            .WithHttpTransport(options =>
            {
#pragma warning disable MCPEXP002
                options.RunSessionHandler = async (httpContext, mcpServer, token) =>
                {
                    if (mcpServer.SessionId != null)
                        sessionActivated[mcpServer.SessionId] = GetRoot().GetEagerSkillsRecursive().Select(s => s.Name).ToHashSet();
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
                var activated = GetActivated(root, ctx.Server.SessionId);
                var tools = activated
                    .Select(name => root.FindSkill(name))
                    .OfType<SkillCode>()
                    .SelectMany(s => s.GetMcpServerTools())
                    .Select(t => t.ProtocolTool)
                    .ToList();

                return new ListToolsResult { Tools = tools };
            })
            .WithCallToolHandler(async (ctx, ct) =>
            {
                var toolName = ctx.Params!.Name;
                var root = GetRoot();
                var activated = GetActivated(root, ctx.Server.SessionId);

                var tool = activated
                    .Select(name => root.FindSkill(name))
                    .OfType<SkillCode>()
                    .SelectMany(s => s.GetMcpServerTools())
                    .FirstOrDefault(t => t.ProtocolTool.Name == toolName)
                    ?? throw new McpException($"Tool '{toolName}' not found");

                CallToolResult result;
                using (AgentLogic.IsMCP.Override(true))
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
