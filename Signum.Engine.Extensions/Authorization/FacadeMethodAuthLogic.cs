using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities;

namespace Signum.Engine.Authorization
{
    public static class FacadeMethodAuthLogic
    {
        static Dictionary<RoleDN, Dictionary<string, bool>> _runtimeRules;
        public static Dictionary<RoleDN, Dictionary<string, bool>> RuntimeRules
        {
            get { return Sync.Initialize(ref _runtimeRules, () => NewCache()); }
        }

        public static void Start(SchemaBuilder sb, Type serviceInterface)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                FacadeMethodLogic.Start(sb, serviceInterface);
                sb.Include<RuleFacadeMethodDN>();
                sb.Schema.Initializing += Schema_Initializing;
                sb.Schema.Saved += Schema_Saved;
                AuthLogic.RolesModified+= UserAndRoleLogic_RolesModified;
            }
        }

        static void Schema_Initializing(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        static void Schema_Saved(Schema sender, IdentifiableEntity ident)
        {
            if (ident is RuleFacadeMethodDN)
            {
                Transaction.RealCommit += () => _runtimeRules = null;
            }
        }

        static void UserAndRoleLogic_RolesModified(Schema sender)
        {
            Transaction.RealCommit += () => _runtimeRules = null;
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

        public static List<AllowedRule> GetAllowedRule(Lazy<RoleDN> roleLazy)
        {
            var role = roleLazy.Retrieve();

            var operations = FacadeMethodLogic.RetrieveOrGenerateServiceOperations();
            return operations.Select(o => new AllowedRule(GetBaseAllowed(role, o.Name))
                    {
                        Resource = o,
                        Allowed = GetAllowed(role, o.Name),
                    }).ToList();
        }

        public static void SetAllowedRule(List<AllowedRule> rules, Lazy<RoleDN> roleLazy)
        {
            var role = roleLazy.Retrieve();
            var current = Database.Query<RuleFacadeMethodDN>().Where(r => r.Role == role).ToDictionary(a => a.ServiceOperation);
            var should = rules.Where(a => a.Overriden).ToDictionary(r => (FacadeMethodDN)r.Resource);

            Synchronizer.Syncronize(current, should,
                (s, sr) => sr.Delete(),
                (s, ar) => new RuleFacadeMethodDN { ServiceOperation = s, Allowed = ar.Allowed, Role = role }.Save(),
                (s, sr, ar) => { sr.Allowed = ar.Allowed; sr.Save(); });

            _runtimeRules = null; 
        }

        public static void AuthorizeAccess(MethodInfo mi)
        {
            if (!GetAllowed(UserDN.Current.Role, mi.Name))
                throw new UnauthorizedAccessException("Access to Service Operation '{0}' is not allowed".Formato(mi.Name));
        }

        public static Dictionary<RoleDN, Dictionary<string, bool>> NewCache()
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
            {
                List<RoleDN> roles = AuthLogic.RolesInOrder().ToList();

                Dictionary<RoleDN, Dictionary<string, bool>> realRules = Database.RetrieveAll<RuleFacadeMethodDN>()
                    .AgGroupToDictionary(ru => ru.Role, gr => gr.ToDictionary(a => a.ServiceOperation.Name, a => a.Allowed));

                Dictionary<RoleDN, Dictionary<string, bool>> newRules = new Dictionary<RoleDN, Dictionary<string, bool>>();
                foreach (var role in roles)
                {
                    var permissions = role.Roles.Count == 0 ?
                         null :
                         role.Roles.Select(r => newRules.TryGetC(r)).OuterCollapseDictionariesS(vals => vals.MaxAllowed());

                    permissions = permissions.Override(realRules.TryGetC(role)).Simplify(a => a);

                    if (permissions != null)
                        newRules.Add(role, permissions);
                }

                return newRules;
            }
        }
    }
}
