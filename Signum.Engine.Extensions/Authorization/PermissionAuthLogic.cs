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
using Signum.Utilities.Reflection;
using System.Xml.Linq;

namespace Signum.Engine.Authorization
{

    public static class PermissionAuthLogic
    {
        static List<PermissionSymbol> permissions = new List<PermissionSymbol>();
        public static void RegisterPermissions(params PermissionSymbol[] type)
        {
            permissions.AddRange(type.NotNull()); 
        }

        public static void RegisterTypes(params Type[] types)
        {
            foreach (var t in types.NotNull())
            {
                if (!t.IsStaticClass())
                    throw new ArgumentException("{0} is not a static class".FormatWith(t.Name));

                permissions.AddRange(t.GetFields(BindingFlags.Public | BindingFlags.Static).Select(fi => fi.GetValue(null)).Cast<PermissionSymbol>());
            }
        }

        public static IEnumerable<PermissionSymbol> RegisteredPermission
        {
            get { return permissions; }
        }

        static AuthCache<RulePermissionEntity, PermissionAllowedRule, PermissionSymbol, PermissionSymbol, bool> cache;

        public static IManualAuth<PermissionSymbol, bool> Manual { get { return cache; } }

        public static bool IsStarted { get { return cache != null; } }

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null)));
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);

                sb.Include<PermissionSymbol>();

                SymbolLogic<PermissionSymbol>.Start(sb, () => RegisteredPermission.ToHashSet());

                cache = new AuthCache<RulePermissionEntity, PermissionAllowedRule, PermissionSymbol, PermissionSymbol, bool>(sb,
                    s=>s,
                    s=>s,
                    merger: new PermissionMerger(),
                    invalidateWithTypes: false);

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
            }
        }

        public static void AssertAuthorized(this PermissionSymbol permissionSymbol)
        {
            if (!IsAuthorized(permissionSymbol))
                throw new UnauthorizedAccessException("Permission '{0}' is denied".FormatWith(permissionSymbol));
        }

        public static string IsAuthorizedString(this PermissionSymbol permissionSymbol)
        {
            if (!IsAuthorized(permissionSymbol))
                return "Permission '{0}' is denied".FormatWith(permissionSymbol);

            return null;
        }

        public static bool IsAuthorized(this PermissionSymbol permissionSymbol)
        {
            if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal || cache == null)
                return true;

            return cache.GetAllowed(RoleEntity.Current.ToLite(), permissionSymbol);
        }

        public static bool IsAuthorized(this PermissionSymbol permissionSymbol, Lite<RoleEntity> role)
        {
            //if (permissionSymbol == BasicPermission.AutomaticUpgradeOfOperations ||
            //  permissionSymbol == BasicPermission.AutomaticUpgradeOfProperties ||
            //  permissionSymbol == BasicPermission.AutomaticUpgradeOfQueries)
            //    return true;

            return cache.GetAllowed(role, permissionSymbol);
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
