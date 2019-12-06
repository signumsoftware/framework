using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.Entities;
using System.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Authorization
{
    public static class PermissionAuthLogic
    {
        static HashSet<PermissionSymbol> permissions = new HashSet<PermissionSymbol>();
        public static void RegisterPermissions(params PermissionSymbol[] permissions)
        {
            foreach (var p in permissions)
            {
                if (p == null)
                    throw AutoInitAttribute.ArgumentNullException(typeof(PermissionSymbol), nameof(permissions));

                PermissionAuthLogic.permissions.Add(p);
            }
        }

        public static void RegisterTypes(params Type[] types)
        {
            foreach (var t in types)
            {
                if (!t.IsStaticClass())
                    throw new ArgumentException("{0} is not a static class".FormatWith(t.Name));

                foreach (var p in t.GetFields(BindingFlags.Public | BindingFlags.Static).Select(fi => fi.GetValue(null)).Cast<PermissionSymbol>())
                {
                    if (p == null)
                        throw AutoInitAttribute.ArgumentNullException(typeof(PermissionSymbol), nameof(permissions));

                    PermissionAuthLogic.permissions.Add(p);
                }
            }
        }

        public static IEnumerable<PermissionSymbol> RegisteredPermission
        {
            get { return permissions; }
        }

        static AuthCache<RulePermissionEntity, PermissionAllowedRule, PermissionSymbol, PermissionSymbol, bool> cache = null!;

        public static IManualAuth<PermissionSymbol, bool> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null!)));
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);

                sb.Include<PermissionSymbol>();

                SymbolLogic<PermissionSymbol>.Start(sb, () => RegisteredPermission.ToHashSet());

                sb.Include<RulePermissionEntity>()
                   .WithUniqueIndex(rt => new { rt.Resource, rt.Role });

                cache = new AuthCache<RulePermissionEntity, PermissionAllowedRule, PermissionSymbol, PermissionSymbol, bool>(sb,
                    toKey: p => p,
                    toEntity: p => p,
                    isEquals: (p1, p2) => p1 == p2,
                    merger: new PermissionMerger(),
                    invalidateWithTypes: false);

                sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query =>
                {
                    Database.Query<RulePermissionEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete();
                    return null;
                };

                RegisterPermissions(BasicPermission.AdminRules,
                    BasicPermission.AutomaticUpgradeOfProperties,
                    BasicPermission.AutomaticUpgradeOfOperations,
                    BasicPermission.AutomaticUpgradeOfQueries);

                AuthLogic.ExportToXml += exportAll => cache.ExportXml("Permissions", "Permission", a => a.Key, b => b.ToString(),
                    exportAll ? PermissionAuthLogic.RegisteredPermission.ToList() : null);
                AuthLogic.ImportFromXml += (x, roles, replacements) =>
                {
                    string replacementKey = "AuthRules:" + typeof(PermissionSymbol).Name;

                    replacements.AskForReplacements(
                        x.Element("Permissions").Elements("Role").SelectMany(r => r.Elements("Permission")).Select(p => p.Attribute("Resource").Value).ToHashSet(),
                        SymbolLogic<PermissionSymbol>.Symbols.Select(s=>s.Key).ToHashSet(),
                        replacementKey);

                    return cache.ImportXml(x, "Permissions", "Permission", roles,
                        s => SymbolLogic<PermissionSymbol>.TryToSymbol(replacements.Apply(replacementKey, s)), bool.Parse);
                };

                sb.Schema.Table<PermissionSymbol>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);
            }
        }

        static SqlPreCommand AuthCache_PreDeleteSqlSync(Entity arg)
        {
            return Administrator.DeleteWhereScript((RulePermissionEntity rt) => rt.Resource, (PermissionSymbol)arg);
        }

        public static void AssertAuthorized(this PermissionSymbol permissionSymbol)
        {
            if (!IsAuthorized(permissionSymbol))
                throw new UnauthorizedAccessException("Permission '{0}' is denied".FormatWith(permissionSymbol));
        }

        public static string? IsAuthorizedString(this PermissionSymbol permissionSymbol)
        {
            if (!IsAuthorized(permissionSymbol))
                return "Permission '{0}' is denied".FormatWith(permissionSymbol);

            return null;
        }

        public static bool IsAuthorized(this PermissionSymbol permissionSymbol)
        {
            AssertRegistered(permissionSymbol);

            if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal || cache == null)
                return true;

            return cache.GetAllowed(RoleEntity.Current, permissionSymbol);
        }

        public static bool IsAuthorized(this PermissionSymbol permissionSymbol, Lite<RoleEntity> role)
        {
            AssertRegistered(permissionSymbol);

            return cache.GetAllowed(role, permissionSymbol);
        }

        private static void AssertRegistered(PermissionSymbol permissionSymbol)
        {
            if (!permissions.Contains(permissionSymbol))
                throw new InvalidOperationException($"The permission '{permissionSymbol}' has not been registered");
        }

        public static DefaultDictionary<PermissionSymbol, bool> ServicePermissionRules()
        {
            return cache.GetDefaultDictionary();
        }

        public static PermissionRulePack GetPermissionRules(Lite<RoleEntity> roleLite)
        {
            var result = new PermissionRulePack { Role = roleLite };
            cache.GetRules(result, SymbolLogic<PermissionSymbol>.Symbols);
            return result;
        }

        public static void SetPermissionRules(PermissionRulePack rules)
        {
            cache.SetRules(rules, r => true);
        }
    }


    class PermissionMerger : IMerger<PermissionSymbol, bool>
    {
        public bool Merge(PermissionSymbol key, Lite<RoleEntity> role, IEnumerable<KeyValuePair<Lite<RoleEntity>, bool>> baseValues)
        {
            if (AuthLogic.GetMergeStrategy(role) == MergeStrategy.Union)
                return baseValues.Any(a => a.Value);
            else
                return baseValues.All(a => a.Value);
        }

        public Func<PermissionSymbol, bool> MergeDefault(Lite<RoleEntity> role)
        {
            return new ConstantFunction<PermissionSymbol, bool>(AuthLogic.GetDefaultAllowed(role)).GetValue;
        }
    }
}
