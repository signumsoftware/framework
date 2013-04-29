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

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                QueryLogic.Start(sb);

                dqm.AllowQuery += new Func<object, bool>(dqm_AllowQuery);

                cache = new AuthCache<RuleQueryDN, QueryAllowedRule, QueryDN, object, bool>(sb,
                    qn => QueryLogic.ToQueryName(qn.Key),
                    QueryLogic.RetrieveOrGenerateQuery,
                    AuthUtils.MaxBool,
                    AuthUtils.MinBool,
                    coercer: new QueryCoercer());

                AuthLogic.SuggestRuleChanges += SuggestQueryRules;
                AuthLogic.ExportToXml += () => cache.ExportXml("Queries", "Query", p => p.Key, b => b.ToString());
                AuthLogic.ImportFromXml += (x, roles, replacements) => 
                {
                    string replacementKey = typeof(QueryDN).Name;

                    var dic = QueryLogic.RetrieveOrGenerateQueries();

                    replacements.AskForReplacements(
                        x.Element("Queries").Elements("Role").SelectMany(r => r.Elements("Query")).Select(p => p.Attribute("Resource").Value).ToHashSet(),
                        dic.Keys.ToHashSet(),
                        replacementKey);

                    return cache.ImportXml(x, "Queries", "Query", roles, s =>
                    {
                        var query = dic.TryGetC(replacements.Apply(replacementKey, s));
                        if (query == null)
                            return null;

                        if (query.IsNew)
                            return query.Save();

                        return query;
                    }, bool.Parse);
                };
            }
        }

        static Action<Lite<RoleDN>> SuggestQueryRules()
        {
            var queries = (from type in Schema.Current.Tables.Keys
                           where TypeLogic.GetEntityKind(type) != EntityKind.Part
                           let qs = DynamicQueryManager.Current.GetQueries(type).Keys
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

        public static QueryRulePack GetQueryRules(Lite<RoleDN> roleLite, TypeDN typeDN)
        {
            var result = new QueryRulePack { Role = roleLite, Type = typeDN };
            cache.GetRules(result, QueryLogic.RetrieveOrGenerateQueries(typeDN));
            return result;
        }

        public static void SetQueryRules(QueryRulePack rules)
        {
            string[] queryNames = DynamicQueryManager.Current.GetQueries(TypeLogic.DnToType[rules.Type]).Keys.Select(qn => QueryUtils.GetQueryUniqueKey(qn)).ToArray();

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
            return DynamicQueryManager.Current.GetQueries(entityType).Keys.Select(qn => cache.GetAllowed(role, qn)).Collapse(); 
        }
    }

    public class QueryCoercer : Coercer<bool, object>
    {
        public override Func<object, bool, bool> GetCoerceValue(Lite<RoleDN> role)
        {
            return (queryName, allowed) =>
            {
                if (!allowed)
                    return allowed;

                var implementations = DynamicQueryManager.Current.GetEntityImplementations(queryName);

                if (implementations.IsByAll)
                    return allowed;

                return implementations.Types.All(t => TypeAuthLogic.GetAllowed(role, t).MaxUI() != TypeAllowedBasic.None);
            };
        }

        public override Func<Lite<RoleDN>, bool, bool> GetCoerceValueManual(object queryName)
        {
            return (role, allowed) =>
            {
                if (!allowed)
                    return false;

                var implementations = DynamicQueryManager.Current.GetEntityImplementations(queryName);

                if (implementations.IsByAll)
                    return allowed;

                return implementations.Types.All(t => TypeAuthLogic.Manual.GetAllowed(role, t).MaxUI() != TypeAllowedBasic.None);
            };
        }
    }
}
