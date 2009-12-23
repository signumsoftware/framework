using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using System.Threading;
using Signum.Entities;
using Signum.Utilities;
using System.Reflection;
using System.Security.Authentication;
using Signum.Engine.Extensions.Properties;

namespace Signum.Engine.Authorization
{
    public static class TypeAuthLogic
    {
        public static Dictionary<RoleDN, Dictionary<Type, TypeAccess>> _runtimeRules;
        static Dictionary<RoleDN, Dictionary<Type, TypeAccess>> RuntimeRules
        {
            get { return Sync.Initialize(ref _runtimeRules, () => NewCache()); }
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                TypeLogic.Start(sb);
                sb.Include<RuleTypeDN>();
                sb.Schema.Initializing(InitLevel.Level0SyncEntities, Schema_Initializing);
                sb.Schema.EntityEvents<RuleTypeDN>().Saved += RuleType_Saved;
                sb.Schema.EntityEventsGlobal.Saving += Schema_Saving;
                sb.Schema.EntityEventsGlobal.Retrieving += Schema_Retrieving;
                AuthLogic.RolesModified += UserAndRoleLogic_RolesModified;
            }
        }

        static void Schema_Initializing(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        static void RuleType_Saved(RuleTypeDN rule, bool isRoot)
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        static void UserAndRoleLogic_RolesModified()
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        static void Schema_Saving(IdentifiableEntity ident, bool isRoot, ref bool graphModified)
        {
            if (AuthLogic.IsEnabled)
            {
                TypeAccess access = GetAccess(RoleDN.Current, ident.GetType());
                if (access < TypeAccess.Modify || ident.IsNew && access != TypeAccess.Create)
                    throw new UnauthorizedAccessException(Resources.NotAuthorizedToSave0.Formato(ident.GetType()));
            }
        }

        static void Schema_Retrieving(Type type, int id, bool isRoot)
        {
            if (AuthLogic.IsEnabled)
            {
                TypeAccess access = GetAccess(RoleDN.Current, type);
                if (access < TypeAccess.Read)
                    throw new UnauthorizedAccessException(Resources.NotAuthorizedToRetrieve0.Formato(type));
            }
        }

        static TypeAccess GetAccess(RoleDN role, Type type)
        {
            return RuntimeRules.TryGetC(role).TryGetS(type) ?? TypeAccess.Create;
        }

        static TypeAccess GetBaseAccess(RoleDN role, Type type)
        {
            return role.Roles.Count == 0 ? TypeAccess.Create :
                  role.Roles.Select(r => GetAccess(r, type)).MaxTypeAccess();
        }

        public static List<TypeAccessRule> GetAccessRule(Lite<RoleDN> roleLite)
        {
            var role = roleLite.Retrieve();

            return TypeLogic.TypeToDN.Select(t => new TypeAccessRule(GetBaseAccess(role, t.Key))
                    {
                        Resource = t.Value,
                        Access = GetAccess(role, t.Key),
                    }).ToList();
        }

        public static void SetAccessRule(List<TypeAccessRule> rules, Lite<RoleDN> roleLite)
        {
            var role = roleLite.Retrieve(); 

            var current = Database.Query<RuleTypeDN>().Where(r => r.Role == role).ToDictionary(a => a.Type);
            var should = rules.Where(a => a.Overriden).ToDictionary(r => (TypeDN)r.Resource);

            Synchronizer.Synchronize(current, should,
                (t, tr) => tr.Delete(),
                (t, ar) => new RuleTypeDN { Type = t, Access = ar.Access, Role = role }.Save(),
                (t, tr, ar) => { tr.Access = ar.Access; tr.Save(); });

            _runtimeRules = null;
        }

        public static TypeAccess GetTypeAccess(Type type)
        {
            return GetAccess(RoleDN.Current, type);
        }

        public static Dictionary<Type, TypeAccess> AuthorizedTypes()
        {
            return RuntimeRules.TryGetC(RoleDN.Current) ?? new Dictionary<Type, TypeAccess>();
        }

        public static Dictionary<RoleDN, Dictionary<Type, TypeAccess>> NewCache()
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
            {
                List<RoleDN> roles = AuthLogic.RolesInOrder().ToList();

                Dictionary<RoleDN, Dictionary<Type, TypeAccess>> realRules = Database.RetrieveAll<RuleTypeDN>()
                    .AgGroupToDictionary(ru => ru.Role, gr => gr.ToDictionary(a => TypeLogic.DnToType[a.Type], a => a.Access));

                Dictionary<RoleDN, Dictionary<Type, TypeAccess>> newRules = new Dictionary<RoleDN, Dictionary<Type, TypeAccess>>();
                foreach (var role in roles)
                {
                    var permissions = (role.Roles.Count == 0 ?
                         null :
                         role.Roles.Select(r => newRules.TryGetC(r)).OuterCollapseDictionariesS(vals => vals.MaxTypeAccess()));

                    permissions = permissions.Override(realRules.TryGetC(role)).Simplify(a => a == TypeAccess.Create);

                    if (permissions != null)
                        newRules.Add(role, permissions);
                }

                return newRules;
            }
        }
    }
}
