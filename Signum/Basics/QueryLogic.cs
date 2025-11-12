using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using Signum.DynamicQuery.Tokens;
using Signum.Engine.Sync;
using System.Collections.Frozen;
using System.Data.Common;

namespace Signum.Basics;

public static class QueryLogic
{
    static ResetLazy<FrozenDictionary<string, object>> queryNamesLazy = null!;
    public static FrozenDictionary<string, object> QueryNames => queryNamesLazy.Value;

    static ResetLazy<FrozenDictionary<object, QueryEntity>> queryNameToEntityLazy = null!;
    public static FrozenDictionary<object, QueryEntity> QueryNameToEntity => queryNameToEntityLazy.Value;

    static ResetLazy<FrozenDictionary<Lite<QueryEntity>, QueryEntity>> liteToEntityLazy = null!;

    public static DynamicQueryContainer Queries { get; } = new DynamicQueryContainer();
    public static ExpressionContainer Expressions { get; } = new ExpressionContainer();

    static QueryLogic()
    {
        FilterSqlServerFullText.miContains = ReflectionTools.GetMethodInfo(() => FullTextSearch.Contains(new string[0], ""));
        FilterSqlServerFullText.miFreeText = ReflectionTools.GetMethodInfo(() => FullTextSearch.FreeText(new string[0], ""));

        ExtensionToken.BuildExtension = (parentType, key, parentExpression) => Expressions.BuildExtension(parentType, key, parentExpression);
    }

    public static bool IsSystemVersioned(Type type)
    {
        var table = Schema.Current.Tables.TryGetC(type);
        return table != null && table.SystemVersioned != null;
    }

    public static bool HasPartitionId(Type onlyType)
    {
        var table = Schema.Current.Tables.TryGetC(onlyType);
        return table != null && table.PartitionId != null;
    }

    public static List<QueryToken> GetImplementedByAllSubTokens(QueryToken queryToken, Type type, SubTokensOptions options)
    {
        var cleanType = type.CleanType();
        return Schema.Current.Tables.Keys
            .Where(t => cleanType.IsAssignableFrom(t))
            .Select(t => (QueryToken)new AsTypeToken(queryToken, t))
            .ToList();
    }


    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(sb)));
    }

    public static void AssertLoaded()
    {
        var a = QueryLogic.QueryNameToEntity;
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        QueryEntity.GetEntityImplementations = query => Queries.GetEntityImplementations(query.ToQueryName());
        var schema = Schema.Current;
        FilterCondition.ToLowerString = token =>
        {
            if (!schema.Settings.IsPostgres)
                return false;

            var pr = token?.GetPropertyRoute();

            var dbType = pr == null ? null : schema.Settings.FieldAttribute<DbTypeAttribute>(pr);

            return dbType == null || !dbType.CollationPostgres_AvoidToLower;
        };

        // QueryManagers = queryManagers;
        sb.Schema.Initializing += () =>
        {
            queryNamesLazy.Load();

            queryNameToEntityLazy.Load();
        };

        sb.Include<QueryEntity>()
            .WithQuery(() => q => new
            {
                Entity = q,
                q.Key,
            });

        sb.Schema.Synchronizing += SynchronizeQueries;
        sb.Schema.Generating += Schema_Generating;

        queryNamesLazy = sb.GlobalLazy(() => CreateQueryNames(),
            new InvalidateWith(typeof(QueryEntity)),
            Schema.Current.InvalidateMetadata);

        queryNameToEntityLazy = sb.GlobalLazy(() =>
            EnumerableExtensions.JoinRelaxed(
                Database.Query<QueryEntity>().ToList(),
                QueryNames,
                q => q.Key,
                kvp => kvp.Key,
                (q, kvp) => KeyValuePair.Create(kvp.Value, q),
                "caching " + nameof(QueryEntity)).ToFrozenDictionaryEx(),
            new InvalidateWith(typeof(QueryEntity)),
            Schema.Current.InvalidateMetadata);

        liteToEntityLazy = sb.GlobalLazy(() => queryNameToEntityLazy.Value.Values.ToFrozenDictionaryEx(a => a.ToLite()),
            new InvalidateWith(typeof(QueryEntity)),
            Schema.Current.InvalidateMetadata);

        sb.Schema.SchemaCompleted += () =>
        {
            if (Schema.Current.Tables.Any(a => a.Value.SystemVersioned != null))
            {
                QueryTimeSeriesLogic.Start(sb);
            }
        };
    }

    public static object ToQueryName(this QueryEntity query)
    {
        return QueryNames.GetOrThrow(query.Key, "QueryName with key {0} not found");
    }

    public static object? ToQueryNameCatch(this QueryEntity query)
    {
        try
        {
            return query.ToQueryName();
        }
        catch (KeyNotFoundException ex) when (StartParameters.IgnoredCodeErrors != null)
        {
            StartParameters.IgnoredCodeErrors.Add(ex);

            return null;
        }
    }

    public static object ToQueryName(string queryKey)
    {
        return QueryNames.GetOrThrow(queryKey, "QueryName with unique name {0} not found");
    }

    public static object? TryToQueryName(string queryKey)
    {
        return QueryNames.TryGetC(queryKey);
    }

    private static FrozenDictionary<string, object> CreateQueryNames()
    {
        return Queries.GetQueryNames().ToFrozenDictionaryEx(qn => QueryUtils.GetKey(qn), "queryName");
    }

    static IEnumerable<QueryEntity> GenerateQueries()
    {
        return Queries.GetQueryNames()
            .Select(qn => new QueryEntity
            {
                Key = QueryUtils.GetKey(qn)
            });
    }

    public static List<QueryEntity> GetTypeQueries(TypeEntity typeEntity)
    {
        Type type = TypeLogic.GetType(typeEntity.CleanName);

        return Queries.GetTypeQueries(type).Keys.Select(GetQueryEntity).ToList();
    }


    public const string QueriesKey = "Queries";

    static SqlPreCommand? Schema_Generating()
    {
        Table table = Schema.Current.Table<QueryEntity>();

        var should = GenerateQueries();

        return should.Select((q, i) => table.InsertSqlSync(q, suffix: i.ToString())).Combine(Spacing.Simple)?.PlainSqlCommand();

    }

    static SqlPreCommand? SynchronizeQueries(Replacements replacements)
    {
        var should = GenerateQueries();

        var current = Administrator.TryRetrieveAll<QueryEntity>(replacements);

        Table table = Schema.Current.Table<QueryEntity>();

        using (replacements.WithReplacedDatabaseName())
            return Synchronizer.SynchronizeScriptReplacing(replacements, QueriesKey, Spacing.Double,
                should.ToDictionaryEx(a => a.Key, "query in memory"),
                current.ToDictionaryEx(a => a.Key, "query in database"),
                createNew: (n, s) => table.InsertSqlSync(s),
                removeOld: (n, c) => table.DeleteSqlSync(c, q => q.Key == c.Key),
                mergeBoth: (fn, s, c) =>
                {
                    var originalKey = c.Key;
                    c.Key = s.Key;
                    return table.UpdateSqlSync(c, q => q.Key == originalKey);
                });
    }

    public static QueryEntity GetQueryEntity(object queryName)
    {
        return QueryNameToEntity.GetOrThrow(queryName, "QueryName {0} not found on the database");
    }

    public static QueryEntity RetrieveFromCache(this Lite<QueryEntity> query)
    {
        return liteToEntityLazy.Value.GetOrThrow(query);
    }


}
