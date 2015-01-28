using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System.Threading;
using Signum.Entities;
using System.Reflection;
using Signum.Entities.DynamicQuery;

namespace Signum.Engine.Authorization
{

    public static class QueryAuthLogic
    {
        static AuthCache<RuleQueryEntity, QueryAllowedRule, QueryEntity, object, bool> cache;

        public static IManualAuth<object, bool> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public readonly static HashSet<object> AvoidAutomaticUpgradeCollection = new HashSet<object>();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                QueryLogic.Start(sb);

                dqm.AllowQuery += new Func<object, bool>(dqm_AllowQuery);

                cache = new AuthCache<RuleQueryEntity, QueryAllowedRule, QueryEntity, object, bool>(sb,
                    qn => QueryLogic.ToQueryName(qn.Key),
                    QueryLogic.GetQueryEntity,
                    merger: new QueryMerger(), 
                    invalidateWithTypes : true,
                    coercer: QueryCoercer.Instance);

                AuthLogic.ExportToXml += exportAll => cache.ExportXml("Queries", "Query", QueryUtils.GetQueryUniqueKey, b => b.ToString(), 
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
                    }, bool.Parse);
                };
            }
        }

        static bool dqm_AllowQuery(object queryName)
        {
            return GetQueryAllowed(queryName);
        }

        public static DefaultDictionary<object, bool> QueryRules()
        {
            return cache.GetDefaultDictionary();
        }

        public static QueryRulePack GetQueryRules(Lite<RoleEntity> role, TypeEntity typeEntity)
        {
            var result = new QueryRulePack { Role = role, Type = typeEntity };
            cache.GetRules(result, QueryLogic.GetTypeQueries(typeEntity));

            var coercer = QueryCoercer.Instance.GetCoerceValue(role);
            result.Rules.ForEach(r => r.CoercedValues = new[] { false, true }
                .Where(a => !coercer(QueryLogic.ToQueryName(r.Resource.Key), a).Equals(a))
                .ToArray());

            return result;
        }

        public static void SetQueryRules(QueryRulePack rules)
        {
            string[] queryNames = DynamicQueryManager.Current.GetTypeQueries(TypeLogic.DnToType[rules.Type]).Keys.Select(qn => QueryUtils.GetQueryUniqueKey(qn)).ToArray();

            cache.SetRules(rules, r => queryNames.Contains(r.Key));
        }

        public static bool GetQueryAllowed(object queryName)
        {
            if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal)
                return true;

            return cache.GetAllowed(RoleEntity.Current.ToLite(), queryName);
        }

        public static bool GetQueryAllowed(Lite<RoleEntity> role, object queryName)
        {
            return cache.GetAllowed(role, queryName);
        }

        public static AuthThumbnail? GetAllowedThumbnail(Lite<RoleEntity> role, Type entityType)
        {
            return DynamicQueryManager.Current.GetTypeQueries(entityType).Keys.Select(qn => cache.GetAllowed(role, qn)).Collapse(); 
        }

        internal static bool AllCanRead(this Implementations implementations, Func<Type, TypeAllowedAndConditions> getAllowed)
        {
            if (implementations.IsByAll)
                return true;

            return implementations.Types.All(t => getAllowed(t).MaxUI() != TypeAllowedBasic.None);
        }
    }

    class QueryMerger : IMerger<object, bool>
    {
        public bool Merge(object key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, bool>> baseValues)
        {
            bool best = AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union ?
                baseValues.Any(a => a.Value) :
                baseValues.All(a => a.Value);

            if (!BasicPermission.AutomaticUpgradeOfQueries.IsAuthorized(role) || QueryAuthLogic.AvoidAutomaticUpgradeCollection.Contains(key))
                return best;

            if (baseValues.Where(a => a.Value.Equals(best)).All(a => GetDefault(key, a.Key).Equals(a.Value)))
                return GetDefault(key, role);

            return best;
        }

        public Func<object, bool> MergeDefault(Lite<RoleEntity> role)
        {
            return key =>
            {
                if (!BasicPermission.AutomaticUpgradeOfQueries.IsAuthorized(role) || OperationAuthLogic.AvoidAutomaticUpgradeCollection.Contains(key))
                    return AuthLogic.GetDefaultAllowed(role);

                return GetDefault(key, role);
            };
        }

        bool GetDefault(object key, Lite<RoleEntity> role)
        {
            return DynamicQueryManager.Current.GetEntityImplementations(key).AllCanRead(t => TypeAuthLogic.GetAllowed(role, t));
        }
    }

    class QueryCoercer : Coercer<bool, object>
    {
        public static readonly QueryCoercer Instance = new QueryCoercer();

        private QueryCoercer()
        {
        }

        public override Func<object, bool, bool> GetCoerceValue(Lite<RoleEntity> role)
        {
            return (queryName, allowed) =>
            {
                if (!allowed)
                    return false;

                var implementations = DynamicQueryManager.Current.GetEntityImplementations(queryName);

                return implementations.AllCanRead(t => TypeAuthLogic.GetAllowed(role, t));
            };
        }

        public override Func<Lite<RoleEntity>, bool, bool> GetCoerceValueManual(object queryName)
        {
            return (role, allowed) =>
            {
                if (!allowed)
                    return false;

                var implementations = DynamicQueryManager.Current.GetEntityImplementations(queryName);

                return implementations.AllCanRead(t => TypeAuthLogic.Manual.GetAllowed(role, t));
            };
        }
    }
}
