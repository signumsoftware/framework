using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.DynamicQuery;

namespace Signum.Engine.Authorization
{

    public static class QueryAuthLogic
    {
        static AuthCache<RuleQueryEntity, QueryAllowedRule, QueryEntity, object, QueryAllowed> cache = null!;

        public static HashSet<object> AvoidCoerce = new HashSet<object>();

        public static IManualAuth<object, QueryAllowed> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public readonly static Dictionary<object, QueryAllowed> MaxAutomaticUpgrade = new Dictionary<object, QueryAllowed>();

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                QueryLogic.Start(sb);

                QueryLogic.Queries.AllowQuery += new Func<object, bool, bool>(dqm_AllowQuery);

                sb.Include<RuleQueryEntity>()
                    .WithUniqueIndex(rt => new { rt.Resource, rt.Role });

                cache = new AuthCache<RuleQueryEntity, QueryAllowedRule, QueryEntity, object, QueryAllowed>(sb,
                    toKey: qn => QueryLogic.ToQueryName(qn.Key),
                    toEntity: QueryLogic.GetQueryEntity,
                    isEquals: (q1, q2) => q1 == q2,
                    merger: new QueryMerger(),
                    invalidateWithTypes: true,
                    coercer: QueryCoercer.Instance);

                sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query =>
                {
                    Database.Query<RuleQueryEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete();
                    return null;
                };

                AuthLogic.ExportToXml += exportAll => cache.ExportXml("Queries", "Query", QueryUtils.GetKey, b => b.ToString(),
                    exportAll ? QueryLogic.QueryNames.Values.ToList(): null);
                AuthLogic.ImportFromXml += (x, roles, replacements) =>
                {
                    string replacementKey = "AuthRules:" + typeof(QueryEntity).Name;

                    replacements.AskForReplacements(
                        x.Element("Queries").Elements("Role").SelectMany(r => r.Elements("Query")).Select(p => p.Attribute("Resource").Value).ToHashSet(),
                        QueryLogic.QueryNames.Keys.ToHashSet(),
                        replacementKey);

                    return cache.ImportXml(x, "Queries", "Query", roles, s =>
                    {
                        var qn = QueryLogic.TryToQueryName(replacements.Apply(replacementKey, s));

                        if (qn == null)
                            return null;

                        return QueryLogic.GetQueryEntity(qn);
                    }, str =>
                    {
                        if (Enum.TryParse<QueryAllowed>(str, out var result))
                            return result;

                        var bResult = bool.Parse(str); //For backwards compatibilityS
                        return bResult ? QueryAllowed.Allow : QueryAllowed.None;

                    });
                };

                sb.Schema.Table<QueryEntity>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);
            }
        }

        static SqlPreCommand AuthCache_PreDeleteSqlSync(Entity arg)
        {
            return Administrator.DeleteWhereScript((RuleQueryEntity rt) => rt.Resource, (QueryEntity)arg);
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

            var coercer = QueryCoercer.Instance.GetCoerceValue(role);
            result.Rules.ForEach(r => r.CoercedValues = EnumExtensions.GetValues<QueryAllowed>()
                .Where(a => !coercer(QueryLogic.ToQueryName(r.Resource.Key), a).Equals(a))
                .ToArray());

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

        internal static bool AllCanRead(this Implementations implementations, Func<Type, TypeAllowedAndConditions> getAllowed)
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

    class QueryMerger : IMerger<object, QueryAllowed>
    {
        public QueryAllowed Merge(object key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, QueryAllowed>> baseValues)
        {
            QueryAllowed best = AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union ?
                Max(baseValues.Select(a => a.Value)) :
                Min(baseValues.Select(a => a.Value));

            var maxUp = QueryAuthLogic.MaxAutomaticUpgrade.TryGetS(key);

            if (maxUp.HasValue && maxUp <= best)
                return best;

            if (!BasicPermission.AutomaticUpgradeOfQueries.IsAuthorized(role))
                return best;

            if (baseValues.Where(a => a.Value.Equals(best)).All(a => GetDefault(key, a.Key).Equals(a.Value)))
            {
                var def = GetDefault(key, role);

                return maxUp.HasValue && maxUp <= def ? maxUp.Value : def;
            }

            return best;
        }


        static QueryAllowed Max(IEnumerable<QueryAllowed> baseValues)
        {
            QueryAllowed result = QueryAllowed.None;

            foreach (var item in baseValues)
            {
                if (item > result)
                    result = item;

                if (result == QueryAllowed.Allow)
                    return result;
            }
            return result;
        }

        static QueryAllowed Min(IEnumerable<QueryAllowed> baseValues)
        {
            QueryAllowed result = QueryAllowed.Allow;

            foreach (var item in baseValues)
            {
                if (item < result)
                    result = item;

                if (result == QueryAllowed.None)
                    return result;
            }
            return result;
        }

        public Func<object, QueryAllowed> MergeDefault(Lite<RoleEntity> role)
        {
            return key =>
            {
                if (!BasicPermission.AutomaticUpgradeOfQueries.IsAuthorized(role))
                    return AuthLogic.GetDefaultAllowed(role) ? QueryAllowed.Allow: QueryAllowed.None;

                var maxUp = QueryAuthLogic.MaxAutomaticUpgrade.TryGetS(key);

                var def = GetDefault(key, role);

                return maxUp.HasValue && maxUp <= def ? maxUp.Value : def;
            };
        }

        QueryAllowed GetDefault(object key, Lite<RoleEntity> role)
        {
            return QueryLogic.Queries.GetEntityImplementations(key).AllCanRead(t => TypeAuthLogic.GetAllowed(role, t)) ? QueryAllowed.Allow : QueryAllowed.None;
        }
    }

    class QueryCoercer : Coercer<QueryAllowed, object>
    {
        public static readonly QueryCoercer Instance = new QueryCoercer();

        private QueryCoercer()
        {
        }

        public override Func<object, QueryAllowed, QueryAllowed> GetCoerceValue(Lite<RoleEntity> role)
        {
            return (queryName, allowed) =>
            {
                if (QueryAuthLogic.AvoidCoerce.Contains(queryName))
                    return allowed;

                if (allowed == QueryAllowed.None)
                    return allowed;

                var implementations = QueryLogic.Queries.GetEntityImplementations(queryName);

                return implementations.AllCanRead(t => TypeAuthLogic.GetAllowed(role, t)) ? allowed : QueryAllowed.None;
            };
        }

        public override Func<Lite<RoleEntity>, QueryAllowed, QueryAllowed> GetCoerceValueManual(object queryName)
        {
            return (role, allowed) =>
            {
                if (QueryAuthLogic.AvoidCoerce.Contains(queryName))
                    return allowed;

                if (allowed == QueryAllowed.None)
                    return allowed;

                var implementations = QueryLogic.Queries.GetEntityImplementations(queryName);

                return implementations.AllCanRead(t => TypeAuthLogic.Manual.GetAllowed(role, t)) ? allowed : QueryAllowed.None;
            };
        }
    }
}
