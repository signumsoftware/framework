using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using Signum.Engine.Sync;

namespace Signum.Basics;


public static class TypeLogic
{
    public static Dictionary<PrimaryKey, Type> IdToType
    {
        get { return Schema.Current.typeCachesLazy.Value.IdToType; }
    }

    public static Dictionary<Type, PrimaryKey> TypeToId
    {
        get { return Schema.Current.typeCachesLazy.Value.TypeToId; }
    }

    public static Dictionary<Type, TypeEntity> TypeToEntity
    {
        get { return Schema.Current.typeCachesLazy.Value.TypeToEntity; }
    }

    public static Dictionary<TypeEntity, Type> EntityToType
    {
        get { return Schema.Current.typeCachesLazy.Value.EntityToType; }
    }

    public static Dictionary<Lite<TypeEntity>, Type> LiteToType
    {
        get { return Schema.Current.typeCachesLazy.Value.LiteToType; }
    }

    public static TypeEntity RetrieveFromCache(this Lite<TypeEntity> type)
    {
        var lazy = Schema.Current.typeCachesLazy.Value;
        var t = lazy.LiteToType.GetOrThrow(type);
        return lazy.TypeToEntity.GetOrThrow(t);
    }

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null!)));
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        Schema schema = Schema.Current;

        sb.Include<TypeEntity>()
          .WithQuery(() => t => new
          {
              Entity = t,
              t.Id,
              t.TableName,
              t.CleanName,
              t.ClassName,
              t.Namespace,
          });

        schema.SchemaCompleted += () =>
        {
            var attributes = schema.Tables.Keys.Select(t => KeyValuePair.Create(t, t.GetCustomAttribute<EntityKindAttribute>(true))).ToList();

            var errors = attributes.Where(a => a.Value == null).ToString(a => "Type {0} does not have an EntityTypeAttribute".FormatWith(a.Key.Name), "\n");

            if (errors.HasText())
                throw new InvalidOperationException(errors);
        };

        schema.Initializing += () =>
        {
            schema.typeCachesLazy.Load();
        };

        schema.typeCachesLazy = sb.GlobalLazy(() => new TypeCaches(schema), new InvalidateWith(typeof(TypeEntity)), Schema.Current.InvalidateMetadata);
    }

    public static Type ToType(this TypeEntity typeEntity)
    {
        return EntityToType.GetOrThrow(typeEntity);
    }

    public static Type ToType(this Lite<TypeEntity> typeLite)
    {
        return LiteToType.GetOrThrow(typeLite);
    }

    public static TypeEntity ToTypeEntity(this Type type)
    {
        return TypeToEntity.GetOrThrow(type);
    }

    public static Dictionary<TypeEntity, Type> TryEntityToType(Replacements replacements)
    {
        return (from dn in Administrator.TryRetrieveAll<TypeEntity>(replacements)
                join t in Schema.Current.Tables.Keys on dn.FullClassName equals (EnumEntity.Extract(t) ?? t).FullName
                select (dn, t)).ToDictionary(a => a.dn, a => a.t);
    }

    public static SqlPreCommand? Schema_Synchronizing(Replacements replacements)
    {
        var schema = Schema.Current;
        var isPostgres = schema.Settings.IsPostgres;

        Dictionary<string, TypeEntity> should = GenerateSchemaTypes().ToDictionaryEx(s => s.TableName, "tableName in memory");

        var currentList = Administrator.TryRetrieveAll<TypeEntity>(replacements);

        const string TypeTableName = "TypeTableName";


        replacements.Add(TypeTableName, replacements.TryGetC(Replacements.KeyTables)?
            .ToDictionary(
                kvp => SimplifyTableName(ObjectName.Parse(kvp.Key, isPostgres)).ToString(),
                kvp => SimplifyTableName(ObjectName.Parse(kvp.Value, isPostgres)).ToString()
            ) ??
            new Dictionary<string, string>());

        replacements.AskForReplacements(
            currentList.Select(a => a.TableName).ToHashSet(),
            should.Values.Select(a => a.TableName).ToHashSet(),
            TypeTableName);

        Dictionary<string, TypeEntity> current = replacements.ApplyReplacementsToOld(
            currentList.ToDictionaryEx(c => c.TableName, "tableName in database"), TypeTableName);

        Table table = schema.Table<TypeEntity>();

        using (replacements.WithReplacedDatabaseName())
            return Synchronizer.SynchronizeScript(
                Spacing.Double,
                should,
                current,
                createNew: (tn, s) => table.InsertSqlSync(s),
                removeOld: (tn, c) => table.DeleteSqlSync(c, t => t.CleanName == c.CleanName),
                mergeBoth: (tn, s, c) =>
                {
                    var originalCleanName = c.CleanName;
                    var originalFullName = c.FullClassName;

                    if (c.TableName != s.TableName)
                    {
                        var pc = ObjectName.Parse(c.TableName, isPostgres);
                        var ps = ObjectName.Parse(s.TableName, isPostgres);

                        if(pc.Schema.Database != null ||  pc.Schema.Database != null ? !EqualsIgnoringDatabasePrefix(pc, ps) : c.TableName != s.TableName)
                        {
                            c.TableName = ps.ToString();
                        }
                    }

                    c.TableName = s.TableName;
                    c.CleanName = s.CleanName;
                    c.Namespace = s.Namespace;
                    c.ClassName = s.ClassName;
                    return table.UpdateSqlSync(c, t => t.CleanName == originalCleanName, comment: originalFullName);
                });
    }

    static bool EqualsIgnoringDatabasePrefix(ObjectName pc, ObjectName ps) =>
        ps.Name == pc.Name &&
        pc.Schema.Name == ps.Schema.Name &&
        Suffix(pc.Schema.Database?.Name) == Suffix(ps.Schema.Database?.Name);

    static string? Suffix(string? name) => name.TryAfterLast("_") ?? name;

    //static Dictionary<string, O> ApplyReplacementsToOld<O>(this Replacements replacements, Dictionary<string, O> oldDictionary, string replacementsKey)
    //{
    //    if (!replacements.ContainsKey(replacementsKey))
    //        return oldDictionary;

    //    Dictionary<string, string> dic = replacements[replacementsKey];

    //    return oldDictionary.SelectDictionary(a => dic.TryGetC(a) ?? a, v => v);
    //}

    internal static SqlPreCommand Schema_Generating()
    {
        Table table = Schema.Current.Table<TypeEntity>();

        return GenerateSchemaTypes()
            .Select((e, i) => table.InsertSqlSync(e, suffix: i.ToString()))
            .Combine(Spacing.Simple)!
            .PlainSqlCommand();
    }

    public static Func<ObjectName, ObjectName> SimplifyTableName = tn => tn;

    internal static List<TypeEntity> GenerateSchemaTypes()
    {
        var list = (from tab in Schema.Current.Tables.Values
                    let type = EnumEntity.Extract(tab.Type) ?? tab.Type
                    select new TypeEntity
                    {
                        TableName = SimplifyTableName(tab.Name).ToString(),
                        CleanName = Reflector.CleanTypeName(type),
                        Namespace = type.Namespace!,
                        ClassName = type.Name,
                    }).ToList();
        return list;
    }

    public static Dictionary<string, Type> NameToType
    {
        get { return Schema.Current.NameToType; }
    }

    public static Dictionary<Type, string> TypeToName
    {
        get { return Schema.Current.TypeToName; }
    }

    public static Type GetType(string cleanName)
    {
        return NameToType.GetOrThrow(cleanName, "Type {0} not found in the schema");
    }

    public static Type? TryGetType(string cleanName)
    {
        return NameToType.TryGetC(cleanName);
    }

    public static string GetCleanName(Type type)
    {
        return TypeToName.GetOrThrow(type, "Type {0} not found in the schema");
    }

    public static string? TryGetCleanName(Type type)
    {
        return TypeToName.TryGetC(type);
    }

    public static void AssertLoaded()
    {
        var a = TypeToId.Values;
    }
}

internal class TypeCaches
{
    public readonly Dictionary<Type, TypeEntity> TypeToEntity;
    public readonly Dictionary<TypeEntity, Type> EntityToType;
    public readonly Dictionary<Lite<TypeEntity>, Type> LiteToType;
    public readonly Dictionary<PrimaryKey, Type> IdToType;
    public readonly Dictionary<Type, PrimaryKey> TypeToId;

    public TypeCaches(Schema current)
    {
        TypeToEntity = EnumerableExtensions.JoinRelaxed(
                Database.RetrieveAll<TypeEntity>(),
                current.Tables.Keys,
                t => t.FullClassName,
                t => (EnumEntity.Extract(t) ?? t).FullName!,
                (typeEntity, type) => (typeEntity, type),
                 "caching {0}".FormatWith(current.Table(typeof(TypeEntity)).Name)
                ).ToDictionary(a => a.type, a => a.typeEntity);

        EntityToType = TypeToEntity.Inverse();
        LiteToType = TypeToEntity.ToDictionaryEx(kvp => kvp.Value.ToLite(), kvp => kvp.Key);

        TypeToId = TypeToEntity.SelectDictionary(k => k, v => v.Id);
        IdToType = TypeToId.Inverse();

    }
}
