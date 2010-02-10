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
using System.Reflection;
using Signum.Engine.Extensions.Properties;

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
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                OperationLogic.AssertIsStarted(sb);

                OperationLogic.AllowOperation += new AllowOperationHandler(OperationLogic_AllowOperation);

                sb.Include<RuleOperationDN>();
                sb.Schema.Initializing(InitLevel.Level1SimpleEntities, Schema_Initializing);
                sb.Schema.EntityEvents<RuleOperationDN>().Saved += Schema_Saved;
                AuthLogic.RolesModified += UserAndRoleLogic_RolesModified;
            }
        }

        static bool OperationLogic_AllowOperation(Enum operationKey)
        {
            return GetAllowed(RoleDN.Current, operationKey);
        }

        static void Schema_Initializing(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        static void Schema_Saved(RuleOperationDN rule, bool isRoot)
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        static void UserAndRoleLogic_RolesModified()
        {
            Transaction.RealCommit += () => _runtimeRules = null;
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

        public static List<OperationInfo> Filter(List<OperationInfo> operationInfos)
        {
            RoleDN role = RoleDN.Current;

            return operationInfos.Where(ai => GetAllowed(role, ai.Key)).ToList(); 
        }

        public static List<AllowedRule> GetAllowedRule(Lite<RoleDN> roleLite, TypeDN typeDN)
        {
            var role = roleLite.Retrieve();

            Type type = TypeLogic.DnToType[typeDN];

            var operations = Database.RetrieveAll<OperationDN>();
            return (from oi in OperationLogic.GetAllOperationInfos(type)
                    select new AllowedRule(GetBaseAllowed(role, oi.Key))
                    {
                        Resource = EnumLogic<OperationDN>.ToEntity(oi.Key),
                        Allowed = GetAllowed(role, oi.Key),
                    }).ToList();
        }

        public static void SetAllowedRule(List<AllowedRule> rules, Lite<RoleDN> roleLite, TypeDN typeDN)
        {
            var role = roleLite.Retrieve();

            Type type = TypeLogic.DnToType[typeDN];
            var operations = OperationLogic.GetAllOperationInfos(type).Select(a => EnumLogic<OperationDN>.ToEntity(a.Key)).ToArray();
            var current = Database.Query<RuleOperationDN>().Where(r => r.Role == role && operations.Contains(r.Operation)).ToDictionary(a => a.Operation);
            var should = rules.Where(a => a.Overriden).ToDictionary(r => (OperationDN)r.Resource);

            Synchronizer.Synchronize(current, should,
                (o, or)=>or.Delete(),
                (o, ar) => new RuleOperationDN { Operation = o, Allowed = ar.Allowed, Role = role }.Save(),
                (o, or, ar) => { or.Allowed = ar.Allowed; or.Save(); });

            _runtimeRules = null; 
        }

        static Dictionary<RoleDN, Dictionary<Enum, bool>> NewCache()
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
            {
                List<RoleDN> roles = AuthLogic.RolesInOrder().ToList();

                Dictionary<RoleDN, Dictionary<Enum, bool>> realRules = Database.RetrieveAll<RuleOperationDN>()
                    .AgGroupToDictionary(ru => ru.Role, gr => gr.ToDictionary(a => EnumLogic<OperationDN>.ToEnum(a.Operation.Key), a => a.Allowed));

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
