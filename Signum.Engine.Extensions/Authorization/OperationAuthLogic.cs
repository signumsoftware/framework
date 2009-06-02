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
using Signum.Entities.Operations;
using Signum.Engine.Operations;

namespace Signum.Engine.Authorization
{
    public static class OperationAuthLogic
    {
        static Dictionary<RoleDN, Dictionary<Enum, bool>> _runtimeRules;
        static Dictionary<RoleDN, Dictionary<Enum, bool>> RuntimeRules
        {
            get { return Sync.Initialize(ref _runtimeRules, () => NewCache()); }
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined<RuleOperationDN>())
            {
                AuthLogic.Start(sb);
                OperationLogic.Start(sb);

                OperationLogic.ExecutingEvent += new ExecuteOperationHandler(OperationLogic_ExecutingEvent);

                sb.Include<RuleOperationDN>();
                sb.Schema.Initializing += Schema_Initializing;
                sb.Schema.Saved += Schema_Saved;
                AuthLogic.RolesModified += UserAndRoleLogic_RolesModified;
            }
        }

        static void Schema_Initializing(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        static void Schema_Saved(Schema sender, IdentifiableEntity ident)
        {
            if (ident is RuleOperationDN)
            {
                Transaction.RealCommit += () => _runtimeRules = null;
            }
        }

        static void UserAndRoleLogic_RolesModified(Schema sender)
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        static void OperationLogic_ExecutingEvent(Enum operationKey, OperationDN action, IdentifiableEntity entity, object[] parameters)
        {
            if (!GetAllowed(UserDN.Current.Role, operationKey))
                throw new UnauthorizedAccessException("Access to Action '{0}' is not allowed".Formato(operationKey));
        }

        static bool GetAllowed(RoleDN role, Enum operationKey)
        {
            return RuntimeRules.TryGetC(role).TryGetS(operationKey) ?? true;
        }

        static bool GetBaseAllowed(RoleDN role, Enum operationKey)
        {
            return role.Roles.Count == 0 ? true :
                  role.Roles.Select(r => GetAllowed(r, operationKey)).MaxAllowed();
        }

        public static List<OperationInfo> GetActionInfos(RoleDN role, Lazy lazy)
        {
            return OperationLogic.GetOperationInfos(lazy).Where(ai => GetAllowed(role, ai.OperationKey)).ToList(); 
        }

        public static List<AllowedRule> GetAllowedRule(Lazy<RoleDN> roleLazy)
        {
            var role = roleLazy.Retrieve();

            var queries = Database.RetrieveAll<OperationDN>();
            return (from a in queries
                   let ak = OperationLogic.ToEnum[a.Key]     
                   select new AllowedRule(GetBaseAllowed(role, ak))
                   {
                       Resource = a,
                       Allowed = GetAllowed(role, ak),
                   }).ToList();    
        }

        public static void SetAllowedRule(List<AllowedRule> rules, Lazy<RoleDN> roleLazy)
        {
            var role = roleLazy.Retrieve();

            var current = Database.Query<RuleQueryDN>().Where(r => r.Role == role).ToDictionary(a => a.Query);
            var should = rules.Where(a => a.Overriden).ToDictionary(r => (QueryDN)r.Resource);

            Synchronizer.Syncronize(current, should,
                (q,qr)=>qr.Delete(),
                (q,ar)=>new RuleQueryDN{ Query = q, Allowed = ar.Allowed, Role = role}.Save(),
                (q, qr, ar) => { qr.Allowed = ar.Allowed; qr.Save(); });

            _runtimeRules = null; 
        }

        static Dictionary<RoleDN, Dictionary<Enum, bool>> NewCache()
        {
            using (AuthLogic.Disable())
            using (new ObjectCache(true))
            {
                List<RoleDN> roles = AuthLogic.RolesInOrder().ToList();

                Dictionary<RoleDN, Dictionary<Enum, bool>> realRules = Database.RetrieveAll<RuleOperationDN>()
                    .AgGroupToDictionary(ru => ru.Role, gr => gr.ToDictionary(a => OperationLogic.ToEnum[a.Operation.Key], a => a.Allowed));

                Dictionary<RoleDN, Dictionary<Enum, bool>> newRules = new Dictionary<RoleDN, Dictionary<Enum, bool>>();
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
