using ModelContextProtocol.Server;
using Signum.Engine.Sync;
using System.Collections.Frozen;
using System.ComponentModel;

namespace Signum.Agent;

public static class SkillCodeLogic
{
    public static ResetLazy<FrozenDictionary<Type, SkillCodeEntity>> TypeToEntity = null!;
    public static ResetLazy<FrozenDictionary<SkillCodeEntity, Type>> EntityToType = null!;

    public static Dictionary<string, Type> RegisteredCodes = new();

    internal static void Register(Type type)
    {
        if (!typeof(SkillCode).IsAssignableFrom(type))
            throw new InvalidOperationException($"Type '{type.FullName}' must derive from SkillCode");

        if(RegisteredCodes.TryGetValue(type.Name, out var already) && already != type)
            throw new InvalidOperationException($"Type '{type.FullName}' is already registered with a different type.");

        RegisteredCodes[type.Name!] = type;
    }

    public static void Register<T>()
        where T : SkillCode => Register(typeof(T));

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Schema.Generating += Schema_Generating;
        sb.Schema.Synchronizing += Schema_Synchronizing;
        sb.Include<SkillCodeEntity>()
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.ClassName
            });


        TypeToEntity = sb.GlobalLazy(() =>
        {
            var dbAtentCodes = Database.RetrieveAll<SkillCodeEntity>();
            return EnumerableExtensions.JoinRelaxed(
                dbAtentCodes,
                RegisteredCodes.Values,
                entity => entity.ClassName,
                type => type!.Name!,
                (entity, type) => KeyValuePair.Create(type, entity),
                "caching " + nameof(SkillCodeEntity))
                .ToFrozenDictionaryEx();
        }, new InvalidateWith(typeof(SkillCodeEntity)));

        sb.Schema.Initializing += () => TypeToEntity.Load();

        EntityToType = sb.GlobalLazy(() => TypeToEntity.Value.Inverse().ToFrozenDictionaryEx(),
            new InvalidateWith(typeof(SkillCodeEntity)));
    }

    public static Type ToType( this SkillCodeEntity codeEntity)
    {
        return EntityToType.Value.GetOrThrow(codeEntity);
    }

    public static SkillCodeEntity ToSkillCodeEntity(Type type)
    {
        return TypeToEntity.Value.GetOrThrow(type);
    }

    static SqlPreCommand? Schema_Generating()
    {
        var table = Schema.Current.Table<SkillCodeEntity>();
        return GenerateCodeEntities()
            .Select(e => table.InsertSqlSync(e))
            .Combine(Spacing.Simple);
    }

    static SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        var table = Schema.Current.Table<SkillCodeEntity>();
        var should = GenerateCodeEntities().ToDictionary(e => e.ClassName);
        var current = Administrator.TryRetrieveAll<SkillCodeEntity>(replacements)
            .ToDictionary(e => e.ClassName);

        return Synchronizer.SynchronizeScript(Spacing.Double, should, current,
            createNew: (_, s) => table.InsertSqlSync(s),
            removeOld: (_, c) => table.DeleteSqlSync(c, e => e.ClassName == c.ClassName),
            mergeBoth: (_, s, c) => table.UpdateSqlSync(c, e => e.ClassName == c.ClassName));
    }

    static List<SkillCodeEntity> GenerateCodeEntities() =>
        RegisteredCodes.Values.Select(type => new SkillCodeEntity { ClassName = type.Name! }).ToList();


    public static DefaultSkillCodeInfo GetDefaultSkillCodeInfo(string skillCodeName)
    {
        if (!SkillCodeLogic.RegisteredCodes.TryGetValue(skillCodeName, out var type))
            throw new KeyNotFoundException($"AgentSkillCode type '{skillCodeName}' is not registered.");

        return GetDefaultSkillCodeInfo((SkillCode)Activator.CreateInstance(type)!);
    }

    public static DefaultSkillCodeInfo GetDefaultSkillCodeInfo(SkillCode instance)
    {
        var type = instance.GetType();

        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(pi => new { pi, attr = pi.GetCustomAttribute<SkillPropertyAttribute>() })
            .Where(x => x.attr != null)
            .Select(x => new DefaultSkillCodeProperty
            {
                PropertyName = x.pi.Name,
                AttributeName = x.attr!.GetType().Name.Before("Attribute"),
                ValueHint = x.attr.ValueHint,
                PropertyType = x.pi.PropertyType.TypeName(),
                DefaultValue = x.attr.ConvertValueToString(x.pi.GetValue(instance), x.pi.PropertyType),
            })
            .ToList();

        var methods = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
            .ToList();

        var mcpTools = instance.GetMcpServerTools().ToList();

        var tools = methods.Zip(mcpTools, (method, mcpTool) => new DefaultToolInfo
        {
            McpName = mcpTool.ProtocolTool.Name,
            Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description,
            ReturnType = UnwrapTaskType(method.ReturnType).TypeName(),
            Parameters = method.GetParameters()
                .Where(p => p.ParameterType != typeof(CancellationToken))
                .Select(p => new DefaultToolParameter
                {
                    Name = p.Name!,
                    Type = p.ParameterType.TypeName(),
                    IsRequired = !p.HasDefaultValue && Nullable.GetUnderlyingType(p.ParameterType) == null,
                    Description = p.GetCustomAttribute<DescriptionAttribute>()?.Description,
                })
                .ToList(),
        }).ToList();

        var subSkills = instance.SubSkills
            .Select(ss => new DefaultSubSkillInfo
            {
                ClassName = ss.Code.GetType().Name,
                Activation = ss.Activation,
                Info = GetDefaultSkillCodeInfo(ss.Code),
            })
            .ToList();

        return new DefaultSkillCodeInfo
        {
            DefaultShortDescription = instance.ShortDescription,
            DefaultInstructions = instance.OriginalInstructions,
            Properties = properties,
            Tools = tools,
            SubSkills = subSkills,
        };
    }

    static Type UnwrapTaskType(Type t)
    {
        if (t == typeof(Task) || t == typeof(void)) return typeof(void);
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def == typeof(Task<>) || def == typeof(ValueTask<>))
                return t.GetGenericArguments()[0];
        }
        return t;
    }

    public static bool IsAutoRegister;
    internal static IDisposable AutoRegister()
    {
        IsAutoRegister = true;
        return new Disposable(() => IsAutoRegister = false);
    }

    
}

public class DefaultSkillCodeInfo
{
    public string DefaultShortDescription { get; set; } = null!;
    public string DefaultInstructions { get; set; } = null!;
    public List<DefaultSkillCodeProperty> Properties { get; set; } = null!;
    public List<DefaultToolInfo> Tools { get; set; } = null!;
    public List<DefaultSubSkillInfo> SubSkills { get; set; } = null!;
}

public class DefaultSubSkillInfo
{
    public string ClassName { get; set; } = null!;
    public SkillActivation Activation { get; set; }
    public DefaultSkillCodeInfo Info { get; set; } = null!;
}

public class DefaultSkillCodeProperty
{
    public string PropertyName { get; set; } = null!;
    public string AttributeName { get; set; } = null!;
    public string? ValueHint { get; set; }
    public string PropertyType { get; set; } = null!;
    public string? DefaultValue { get; set; }
}

public class DefaultToolInfo
{
    public string McpName { get; set; } = null!;
    public string? Description { get; set; }
    public string ReturnType { get; set; } = null!;
    public List<DefaultToolParameter> Parameters { get; set; } = null!;
}

public class DefaultToolParameter
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool IsRequired { get; set; }
    public string? Description { get; set; }
}
