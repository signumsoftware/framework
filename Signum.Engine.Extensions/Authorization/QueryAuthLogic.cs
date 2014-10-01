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
        static AuthCache<RuleQueryDN, QueryAllowedRule, QueryDN, object, bool> cache;

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

                cache = new AuthCache<RuleQueryDN, QueryAllowedRule, QueryDN, object, bool>(sb,
                    qn => QueryLogic.ToQueryName(qn.Key),
                    QueryLogic.GetQuery,
                    merger: new QueryMerger(), 
                    invalidateWithTypes : true,
                    coercer: QueryCoercer.Instance);

                AuthLogic.SuggestRuleChanges += SuggestQueryRules;
                AuthLogic.ExportToXml += exportAll => cache.ExportXml("Queries", "Query", QueryUtils.GetQueryUniqueKey, b => b.ToString(), 
                    exportAll ? QueryLogic.QueryNames.Values.ToList(): null);
                AuthLogic.ImportFromXml += (x, roles, replacements) => 
                {
                    string replacementKey = typeof(QueryDN).Name;

                    replacements.AskForReplacements(
                        x.Element("Queries").Elements("Role").SelectMany(r => r.Elements("Query")).Select(p => p.Attribute("Resource").Value).ToHashSet(),
                        QueryLogic.QueryNames.Keys.ToHashSet(),
                        replacementKey);

                    return cache.ImportXml(x, "Queries", "Query", roles, s =>
                    {
                        var qn = QueryLogic.TryToQueryName(replacements.Apply(replacementKey, s));

                        if (qn == null)
                            return null;

                        return QueryLogic.GetQuery(qn);
                    }, bool.Parse);
                };
            }
        }

        static Action<Lite<RoleDN>> SuggestQueryRules()
        {
            var queries = (from type in Schema.Current.Tables.Keys
                           where EntityKindCache.GetEntityKind(type) != EntityKind.Part
                           let qs = DynamicQueryManager.Current.GetTypeQueries(type).Keys
                           where qs.Any()
                           select KVP.Create(type, qs.ToList())).ToDictionary();

            return r =>
            {
                bool? warnings = null;

                foreach (var type in queries.Keys)
	            {
                    var ta = TypeAuthLogic.GetAllowed(r, type);

                    if (ta.MaxUI() == TypeAllowedBasic.None)
                    {
                        foreach (var query in queries[type].Where(q => QueryAuthLogic.GetQueryAllowed(r, q)))
                        {
                            bool isError = ta.MaxDB() == TypeAllowedBasic.None;

                            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "{0}: Query {1} is allowed but type {2} is [{3}]".Formato(
                                 isError ? "Error" : "Warning",
                                QueryUtils.GetQueryUniqueKey(query), type.Name, ta));


                            SafeConsole.WriteColor(ConsoleColor.DarkRed, "Disallow ");
                            string message = "{0} to {1}?".Formato(QueryUtils.GetQueryUniqueKey(query), r);

                            if (isError ? SafeConsole.Ask(message) : SafeConsole.Ask(ref warnings, message))
                            {
                                Manual.SetAllowed(r, query, false);
                                SafeConsole.WriteLineColor(ConsoleColor.Red, "Disallowed");
                            }
                            else
                            {
                                SafeConsole.WriteLineColor(ConsoleColor.White, "Skipped");
                            }
                        }
                    }
                    else
                    {
                        var qs = queries[type];
                        if (ta.MaxUI() > TypeAllowedBasic.Modify && qs.Any() && !qs.Any(q => QueryAuthLogic.GetQueryAllowed(r, q)))
                        {
                            SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "Warning: Type {0} is [{1}] but no query is allowed".Formato(type.Name, ta));

                            if (qs.Contains(type))
                            {
                                SafeConsole.WriteColor(ConsoleColor.DarkGreen, "Allow ");
                                if (SafeConsole.Ask(ref warnings, "{0} to {1}?".Formato(QueryUtils.GetQueryUniqueKey(type), r)))
                                {
                                    Manual.SetAllowed(r, type, true);
                                    SafeConsole.WriteLineColor(ConsoleColor.Green, "Allowed");
                                }
                                else
                                {
                                    SafeConsole.WriteLineColor(ConsoleColor.White, "Skipped");
                                }
                            }
                        }
                    }
	            }
            };
        }

        static bool dqm_AllowQuery(object queryName)
        {
            return GetQueryAllowed(queryName);
        }

        public static DefaultDictionary<object, bool> QueryRules()
        {
            return cache.GetDefaultDictionary();
        }

        public static QueryRulePack GetQueryRules(Lite<RoleDN> role, TypeDN typeDN)
        {
            var result = new QueryRulePack { Role = role, Type = typeDN };
            cache.GetRules(result, QueryLogic.GetTypeQueries(typeDN));

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

            return cache.GetAllowed(RoleDN.Current.ToLite(), queryName);
        }

        public static bool GetQueryAllowed(Lite<RoleDN> role, object queryName)
        {
            return cache.GetAllowed(role, queryName);
        }

        public static AuthThumbnail? GetAllowedThumbnail(Lite<RoleDN> role, Type entityType)
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
        public bool Merge(object key, Lite<RoleDN> role, IEnumerable<KeyValuePair<Lite<RoleDN>, bool>> baseValues)
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

        public Func<object, bool> MergeDefault(Lite<RoleDN> role)
        {
            return key =>
            {
                if (!BasicPermission.AutomaticUpgradeOfQueries.IsAuthorized(role) || OperationAuthLogic.AvoidAutomaticUpgradeCollection.Contains(key))
                    return AuthLogic.GetDefaultAllowed(role);

                return GetDefault(key, role);
            };
        }

        bool GetDefault(object key, Lite<RoleDN> role)
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

        public override Func<object, bool, bool> GetCoerceValue(Lite<RoleDN> role)
        {
            return (queryName, allowed) =>
            {
                if (!allowed)
                    return false;

                var implementations = DynamicQueryManager.Current.GetEntityImplementations(queryName);

                return implementations.AllCanRead(t => TypeAuthLogic.GetAllowed(role, t));
            };
        }

        public override Func<Lite<RoleDN>, bool, bool> GetCoerceValueManual(object queryName)
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
