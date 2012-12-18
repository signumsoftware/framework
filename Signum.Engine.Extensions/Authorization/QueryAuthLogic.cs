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
using Signum.Engine.Extensions.Properties;
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
                    AuthUtils.MinBool);

                AuthLogic.SuggestRuleChanges += AuthLogic_SuggestRuleChanges;
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

        static Action<Lite<RoleDN>> AuthLogic_SuggestRuleChanges()
        {
            var queries = (from type in Schema.Current.Tables.Keys
                           where TypeLogic.GetEntityType(type) != EntityType.Part
                           let qs = DynamicQueryManager.Current.GetQueries(type).Keys
                           where qs.Any()
                           select KVP.Create(type, qs.ToList())).ToDictionary();

            return r =>
            {
                foreach (var type in queries.Keys)
	            {
                    var ta = TypeAuthLogic.GetAllowed(r, type);

                    if (ta.Max().GetUI() == TypeAllowedBasic.None)
                    {
                        foreach (var query in queries[type].Where(q => QueryAuthLogic.GetQueryAllowed(r, q)))
                        {
                            Console.WriteLine("Error: Query {0} is allowed but type {1} is {2}".Formato(QueryUtils.GetQueryUniqueKey(query), type.Name, ta.Max()));
                        }
                    }
                    else
                    {
                        var qs = queries[type];
                        if (qs.Any() && !qs.Any(q => QueryAuthLogic.GetQueryAllowed(r, q)))
                            Console.WriteLine("Warning: Type {0} is {1} but no query is allowed");
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
}
