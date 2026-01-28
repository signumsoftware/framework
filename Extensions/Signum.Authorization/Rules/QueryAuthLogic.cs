using Signum.Authorization;
using Signum.DynamicQuery.Tokens;

namespace Signum.Authorization.Rules;


public static class QueryAuthLogic
{
    static QueryCache cache = null!;

    public static HashSet<object> AvoidCoerce = new HashSet<object>();

    public static IManualAuth<object, QueryAllowed> Manual { get { return cache; } }

    public static bool IsStarted { get { return cache != null; } }

    public readonly static Dictionary<object, QueryAllowed> MaxAutomaticUpgrade = new Dictionary<object, QueryAllowed>();

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        AuthLogic.AssertStarted(sb);
        QueryLogic.Start(sb);

        QueryLogic.Queries.AllowQuery += new Func<object, bool, bool>(dqm_AllowQuery);

        sb.Include<RuleQueryEntity>()
            .WithUniqueIndex(rt => new { rt.Resource, rt.Role });

        cache = new QueryCache(sb);


        AuthLogic.ExportToXml += cache.ExportXml;
        AuthLogic.ImportFromXml += cache.ImportXml;
        AuthLogic.HasRuleOverridesEvent += cache.HasRealOverrides;
        sb.Schema.EntityEvents<QueryEntity>().PreDeleteSqlSync += query => Administrator.DeleteWhereScript((RuleQueryEntity rt) => rt.Resource, query);
        sb.Schema.EntityEvents<RoleEntity>().PreDeleteSqlSync += role => Administrator.UnsafeDeletePreCommand(Database.Query<RuleQueryEntity>().Where(a => a.Role.Is(role)));
        sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query => { Database.Query<RuleQueryEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete(); return null; };
    }

    public static void SetMaxAutomaticUpgrade(object queryName, QueryAllowed allowed)
    {
        MaxAutomaticUpgrade.Add(queryName, allowed);
    }

    static bool dqm_AllowQuery(object queryName, bool fullScreen)
    {
        var allowed = GetQueryAllowed(queryName);
        return allowed == QueryAllowed.Allow || allowed == QueryAllowed.EmbeddedOnly && !fullScreen;
    }

    public static DefaultDictionary<object, QueryAllowed> QueryRules()
    {
        return cache.GetDefaultDictionary();
    }

    public static QueryRulePack GetQueryRules(Lite<RoleEntity> role, TypeEntity typeEntity)
    {
        var result = new QueryRulePack { Role = role, Type = typeEntity };
        cache.GetRules(result, QueryLogic.GetTypeQueries(typeEntity));

        result.Rules.ForEach(r => r.Coerced = cache.CoerceValue(role, QueryLogic.ToQueryName(r.Resource.Key), QueryAllowed.Allow));

        return result;
    }

    public static void SetQueryRules(QueryRulePack rules)
    {
        string[] queryKeys = QueryLogic.Queries.GetTypeQueries(TypeLogic.EntityToType[rules.Type]).Keys.Select(qn => QueryUtils.GetKey(qn)).ToArray();

        cache.SetRules(rules, r => queryKeys.Contains(r.Key));
    }

    public static QueryAllowed GetQueryAllowed(object queryName)
    {
        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
            return QueryAllowed.Allow;

        return cache.GetAllowed(RoleEntity.Current, queryName);
    }

    public static QueryAllowed GetQueryAllowed(Lite<RoleEntity> role, object queryName)
    {
        return cache.GetAllowed(role, queryName);
    }

    public static AuthThumbnail? GetAllowedThumbnail(Lite<RoleEntity> role, Type entityType)
    {
        return QueryLogic.Queries.GetTypeQueries(entityType).Keys.Select(qn => cache.GetAllowed(role, qn)).Collapse();
    }

    internal static bool AllCanRead(this Implementations implementations, Func<Type, WithConditions<TypeAllowed>> getAllowed)
    {
        if (implementations.IsByAll)
            return true;

        return implementations.Types.All(t => getAllowed(t).MaxUI() != TypeAllowedBasic.None);
    }

    public static void SetAvoidCoerce(object queryName)
    {
        AvoidCoerce.Add(queryName);
    }
}
