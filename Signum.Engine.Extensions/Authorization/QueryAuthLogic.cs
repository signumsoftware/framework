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

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                QueryLogic.Start(sb);

                dqm.AllowQuery += new Func<object, bool>(dqm_AllowQuery);

                cache = new AuthCache<RuleQueryDN, QueryAllowedRule, QueryDN, object, bool>(sb,
                    qn => QueryLogic.ToQueryName(qn.Key),
                    QueryLogic.RetrieveOrGenerateQuery, AuthUtils.MaxAllowed, true);
            }
        }

        static bool dqm_AllowQuery(object queryName)
        {
            return cache.GetAllowed(queryName);
        }

        public static HashSet<object> AuthorizedQueryNames()
        {
            return DynamicQueryManager.Current.GetQueryNames().Where(q => cache.GetAllowed(q)).ToHashSet();
        }


        public static QueryRulePack GetQueryRules(Lite<RoleDN> roleLite, TypeDN typeDN)
        {
            return new QueryRulePack
            {
                Role = roleLite,
                Type = typeDN,
                Rules = cache.GetRules(roleLite, QueryLogic.RetrieveOrGenerateQueries(typeDN)).ToMList()
            };
        }

        public static void SetQueryRules(QueryRulePack rules)
        {
            string[] queryNames = DynamicQueryManager.Current.GetQueries(TypeLogic.DnToType[rules.Type]).Keys.Select(qn => QueryUtils.GetQueryName(qn)).ToArray();

            cache.SetRules(rules, r => queryNames.Contains(r.Key));
        }

        public static bool GetQueryAllowed(object queryName)
        {
            return cache.GetAllowed(queryName);
        }

        public static bool GetQueryAllowed(Lite<RoleDN> role, object queryName)
        {
            return cache.GetAllowed(role, queryName);
        }

        public static void SetQueryAllowed(Lite<RoleDN> role, object queryName, bool allowed)
        {
            cache.SetAllowed(role, queryName, allowed);
        }
    }
}
