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

namespace Signum.Engine.Authorization
{

    public static class PermissionAuthLogic
    {
        static List<Type> permissionTypes;
        static Dictionary<RoleDN, Dictionary<string, bool>> _runtimeRules;
        static Dictionary<RoleDN, Dictionary<string, bool>> RuntimeRules
        {
            get { return Sync.Initialize(ref _runtimeRules, () => NewCache()); }
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, params Type[] types)
        {
            if (sb.NotDefined<RulePermissionDN>())
            {
                AuthLogic.Start(sb, dqm);
                permissionTypes = new List<Type>(types);

                sb.Include<RulePermissionDN>();
                sb.Include<PermissionDN>();
                sb.Schema.Initializing += Schema_Initializing;
                sb.Schema.Synchronizing += SynchronizationScript;
                sb.Schema.Saved += Schema_Saved;
                AuthLogic.RolesModified += UserAndRoleLogic_RolesModified;
            }
            else
            {
                permissionTypes.AddRange(types);
            }
        }

        static List<PermissionDN> GeneratePermissions()
        {
            return (from type in permissionTypes
                    from item in Enum.GetValues(type).Cast<object>()
                    select PermissionDN.FromEnum(item)).ToList();
        }

        public static SqlPreCommand GenerateScript()
        {
            Table table = Schema.Current.Table<PermissionDN>();

            return GeneratePermissions().Select(a => table.InsertSqlSync(a)).Combine(Spacing.Simple);
        }

        const string PersmissionKey = "Permissions";

        public static SqlPreCommand SynchronizationScript(Replacements replacements)
        {
            Table table = Schema.Current.Table<PermissionDN>();

            List<PermissionDN> current = Administrator.TryRetrieveAll<PermissionDN>(replacements);

            return Synchronizer.SyncronizeReplacing(replacements, PersmissionKey,
                current.ToDictionary(c => c.Key),
                GeneratePermissions().ToDictionary(s => s.Key),
                (k, c) => table.DeleteSqlSync(c),
                (k, s) => null,
                (k, c, s) =>
                {
                    c.Name = s.Name;
                    c.Key = s.Key;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }

        static void Schema_Initializing(Schema sender)
        {
            _runtimeRules = NewCache();
        }

        static void Schema_Saved(Schema sender, IdentifiableEntity ident)
        {
            if (ident is RulePermissionDN)
            {
                Transaction.RealCommit += () => _runtimeRules = null;
            }
        }

        static void UserAndRoleLogic_RolesModified(Schema sender)
        {
            Transaction.RealCommit += () => _runtimeRules = null;
        }

        public static void Authorize(object permission)
        {
            if (!GetAllowed(UserDN.Current.Role, PermissionDN.UniqueKey(permission)))
                throw new UnauthorizedAccessException("Permission '{0}' is denied".Formato(permission));
        }

        public static bool IsAuthorizedFor(object permission)
        {
            return GetAllowed(UserDN.Current.Role, PermissionDN.UniqueKey(permission));
        }

        static bool GetAllowed(RoleDN role, string persmissionKey)
        {
            return RuntimeRules.TryGetC(role).TryGetS(persmissionKey) ?? true;
        }

        static bool GetBaseAllowed(RoleDN role, string persmissionKey)
        {
            return role.Roles.Count == 0 ? true :
                  role.Roles.Select(r => GetAllowed(r, persmissionKey)).MaxAllowed();
        }

        public static List<PermissionDN> RetrieveOrGeneratePermissions()
        {
            var current = Database.RetrieveAll<PermissionDN>().ToDictionary(a => a.Name);
            var total = GeneratePermissions().ToDictionary(a => a.Name);

            total.SetRange(current);
            return total.Values.ToList();
        }

        public static List<AllowedRule> GetAllowedRule(Lazy<RoleDN> roleLazy)
        {
            var role = roleLazy.Retrieve();

            var permissions = RetrieveOrGeneratePermissions();
            return permissions.Select(p => new AllowedRule(GetBaseAllowed(role, p.Key))
                   {
                       Resource = p,
                       Allowed = GetAllowed(role, p.Key),
                   }).ToList();
        }

        public static void SetAllowedRule(List<AllowedRule> rules, Lazy<RoleDN> roleLazy)
        {
            var role = roleLazy.Retrieve();

            var current = Database.Query<RulePermissionDN>().Where(r => r.Role == role).ToDictionary(a => a.Permission);
            var should = rules.Where(a => a.Overriden).ToDictionary(r => (PermissionDN)r.Resource);

            Synchronizer.Syncronize(current, should,
                (p, pr) => pr.Delete(),
                (p, ar) => new RulePermissionDN { Permission = p, Allowed = ar.Allowed, Role = role }.Save(),
                (p, pr, ar) => { pr.Allowed = ar.Allowed; pr.Save(); });

            _runtimeRules = null;
        }

        public static Dictionary<RoleDN, Dictionary<string, bool>> NewCache()
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
            {
                List<RoleDN> roles = AuthLogic.RolesInOrder().ToList();

                Dictionary<RoleDN, Dictionary<string, bool>> realRules = Database.RetrieveAll<RulePermissionDN>()
                    .AgGroupToDictionary(ru => ru.Role, gr => gr.ToDictionary(a => a.Permission.Key, a => a.Allowed));

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
