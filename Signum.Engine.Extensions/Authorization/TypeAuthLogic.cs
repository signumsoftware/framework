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
            if (sb.NotDefined<RuleTypeDN>())
            {
                AuthLogic.Start(sb);
                TypeLogic.Start(sb, true);
                sb.Include<RuleTypeDN>();
                sb.Schema.Initializing += Schema_Initializing;
                sb.Schema.Saving += Schema_Saving;
                sb.Schema.Saved += Schema_Saved;
                sb.Schema.Retrieving += Schema_Retrieving;
                AuthLogic.RolesModified += UserAndRoleLogic_RolesModified;
            }
        }

        static void Schema_Initializing(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        static void Schema_Saved(Schema sender, IdentifiableEntity ident)
        {
            if (ident is RuleTypeDN)
            {
                Transaction.RealCommit += () => _runtimeRules = null;
            }
        }

        static void UserAndRoleLogic_RolesModified(Schema sender)
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        static void Schema_Saving(Schema sender, IdentifiableEntity ident)
        {
            if (AuthLogic.IsEnabled)
            {
                if (UserDN.Current == null)
                    throw new ApplicationException("Not user logged");

                TypeAccess access = GetAccess(UserDN.Current.Role, ident.GetType());
                if (ident.IsNew && access != TypeAccess.Create || access < TypeAccess.Modify)
                    throw new UnauthorizedAccessException("Not authorized to Save '{0}'".Formato(ident.GetType()));
            }
        }

        static void Schema_Retrieving(Schema sender, Type type, int id)
        {
            if (AuthLogic.IsEnabled)
            {
                if (UserDN.Current == null)
                    throw new ApplicationException("Not user logged");

                TypeAccess access = GetAccess(UserDN.Current.Role, type);
                if (access < TypeAccess.Read)
                    throw new UnauthorizedAccessException("Not authorized to Retrieve '{0}'".Formato(type));
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

        public static List<TypeAccessRule> GetAccessRule(Lazy<RoleDN> roleLazy)
        {
            var role = roleLazy.Retrieve();

            return TypeLogic.TypeToDN.Select(t => new TypeAccessRule(GetBaseAccess(role, t.Key))
                    {
                        Resource = t.Value,
                        Access = GetAccess(role, t.Key),
                    }).ToList();
        }

        public static void SetAccessRule(List<TypeAccessRule> rules, Lazy<RoleDN> roleLazy)
        {
            var role = roleLazy.Retrieve(); 

            var current = Database.Query<RuleTypeDN>().Where(r => r.Role == role).ToDictionary(a => a.Type);
            var should = rules.Where(a => a.Overriden).ToDictionary(r => (TypeDN)r.Resource);

            Synchronizer.Syncronize(current, should,
                (t, tr) => tr.Delete(),
                (t, ar) => new RuleTypeDN { Type = t, Access = ar.Access, Role = role }.Save(),
                (t, tr, ar) => { tr.Access = ar.Access; tr.Save(); });

            _runtimeRules = null;
        }

        public static Dictionary<Type, TypeAccess> AuthorizedTypes()
        {
            return RuntimeRules.TryGetC(UserDN.Current.Role) ?? new Dictionary<Type, TypeAccess>();
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
