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
        static Dictionary<RoleDN, Dictionary<string, bool>> _runtimeRules;
        static Dictionary<RoleDN, Dictionary<string, bool>> RuntimeRules
        {
            get { return Sync.Initialize(ref _runtimeRules, () => NewCache()); }
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                QueryLogic.Start(sb);

                dqm.AllowQuery += new Func<object, bool>(dqm_AllowQuery);

                sb.Include<RuleQueryDN>();
                sb.Schema.Initializing(InitLevel.Level1SimpleEntities, Schema_Initializing);
                sb.Schema.EntityEvents<RuleQueryDN>().Saved += Rule_Saved;
                AuthLogic.RolesModified += UserAndRoleLogic_RolesModified;
            }
        }

        static bool dqm_AllowQuery(object queryName)
        {
            if (!AuthLogic.IsEnabled) return true;

            return GetAllowed(RoleDN.Current, QueryUtils.GetQueryName(queryName));
        }

        static void Schema_Initializing(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        static void Rule_Saved(RuleQueryDN rule, bool isRoot)
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        static void UserAndRoleLogic_RolesModified()
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        public static HashSet<object> AuthorizedQueryNames()
        {
            RoleDN role = RoleDN.Current;

            return DynamicQueryManager.Current.GetQueryNames().Where(q => GetAllowed(role, q.ToString())).ToHashSet();
        }

        static bool GetAllowed(RoleDN role, string queryName)
        {
            return RuntimeRules.TryGetC(role).TryGetS(queryName) ?? true;
        }

        static bool GetBaseAllowed(RoleDN role, string queryName)
        {
            return role.Roles.Count == 0 ? true :
                  role.Roles.Select(r => GetAllowed(r, queryName)).MaxAllowed();
        }

        public static bool GetQueryAllowed(object queryName)
        {
            if (!AuthLogic.IsEnabled)
                return true;

            return GetAllowed(RoleDN.Current, queryName.ToString());
        }

        public static List<AllowedRule> GetAllowedRule(Lite<RoleDN> roleLite, TypeDN typeDN)
        {
            var role = roleLite.Retrieve();

            Type type = TypeLogic.DnToType[typeDN]; 

            var queries = QueryLogic.RetrieveOrGenerateQueries(typeDN);
            return queries.Select(q => new AllowedRule(GetBaseAllowed(role, q.Key))
                   {
                       Resource = q,
                       Allowed = GetAllowed(role, q.Key),
                   }).ToList();    
        }

        public static void SetAllowedRule(List<AllowedRule> rules, Lite<RoleDN> roleLite, TypeDN typeDN)
        {
            var role = roleLite.Retrieve();

            Type type = TypeLogic.DnToType[typeDN];
            var queries = QueryLogic.RetrieveOrGenerateQueries(typeDN).Where(qn => !qn.IsNew).ToArray();

            var current = Database.Query<RuleQueryDN>().Where(r => r.Role == role && queries.Contains(r.Query)).ToDictionary(a => a.Query);
            var should = rules.Where(a => a.Overriden).ToDictionary(r => (QueryDN)r.Resource);

            Synchronizer.Synchronize(current, should,
                (q, qr) => qr.Delete(),
                (q, ar) => new RuleQueryDN { Query = q, Allowed = ar.Allowed, Role = role }.Save(),
                (q, qr, ar) => { qr.Allowed = ar.Allowed; qr.Save(); });

            _runtimeRules = null;
        }

        public static Dictionary<RoleDN, Dictionary<string, bool>> NewCache()
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
            {
                List<RoleDN> roles = AuthLogic.RolesInOrder().ToList();

                Dictionary<RoleDN, Dictionary<string, bool>> realRules = Database.RetrieveAll<RuleQueryDN>()
                    .AgGroupToDictionary(ru => ru.Role, gr => gr.ToDictionary(a => a.Query.Key, a => a.Allowed));

                Dictionary<RoleDN, Dictionary<string, bool>> newRules = new Dictionary<RoleDN, Dictionary<string, bool>>();
                foreach (var role in roles)
                {
                    var permissions = (role.Roles.Count == 0 ?
                         null :
                         role.Roles.Select(r => newRules.TryGetC(r)).OuterCollapseDictionariesS(vals => vals.MaxAllowed()));

                    permissions = permissions.Override(realRules.TryGetC(role)).Simplify(a => a);

                    if (permissions != null)
                        newRules.Add(role, permissions);
                }

                return newRules;
            }
        }
    }
}
