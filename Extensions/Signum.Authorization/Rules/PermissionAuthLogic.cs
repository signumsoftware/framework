using Signum.Utilities.Reflection;

namespace Signum.Authorization.Rules;


public static class PermissionAuthLogic
{
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

            sb.Include<RulePermissionEntity>()
               .WithUniqueIndex(rt => new { rt.Resource, rt.Role });

            cache = new AuthCache<RulePermissionEntity, PermissionAllowedRule, PermissionSymbol, PermissionSymbol, bool>(sb,
                toKey: p => p,
                toEntity: p => p,
                isEquals: (p1, p2) => p1.Is(p2),
                merger: new PermissionMerger(),
                invalidateWithTypes: false);

            sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query =>
            {
                Database.Query<RulePermissionEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete();
                return null;
            };

            PermissionLogic.RegisterTypes(typeof(BasicPermission));

            AuthLogic.ExportToXml += exportAll => cache.ExportXml("Permissions", "Permission", a => a.Key, b => b.ToString(),
                exportAll ? PermissionLogic.RegisteredPermission.ToList() : null);
            AuthLogic.ImportFromXml += (x, roles, replacements) =>
            {
                string replacementKey = "AuthRules:" + typeof(PermissionSymbol).Name;

                replacements.AskForReplacements(
                    x.Element("Permissions")!.Elements("Role").SelectMany(r => r.Elements("Permission")).Select(p => p.Attribute("Resource")!.Value).ToHashSet(),
                    SymbolLogic<PermissionSymbol>.Symbols.Select(s => s.Key).ToHashSet(),
                    replacementKey);

                return cache.ImportXml(x, "Permissions", "Permission", roles,
                    s => SymbolLogic<PermissionSymbol>.TryToSymbol(replacements.Apply(replacementKey, s)), bool.Parse);
            };

            PermissionLogic.IsAuthorizedImplementation = permissionSymbol =>
            {
                if (IsAuthorized(permissionSymbol))
                    return null;

                return "Permission '{0}' is denied".FormatWith(permissionSymbol);
            };

            AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);

            sb.Schema.Table<PermissionSymbol>().PreDeleteSqlSync += new Func<Entity, SqlPreCommand>(AuthCache_PreDeleteSqlSync);
        }
    }

    static SqlPreCommand AuthCache_PreDeleteSqlSync(Entity arg)
    {
        return Administrator.DeleteWhereScript((RulePermissionEntity rt) => rt.Resource, (PermissionSymbol)arg);
    }

    public static bool IsAuthorized(PermissionSymbol permissionSymbol)
    {
        AssertRegistered(permissionSymbol);

        if (!AuthLogic.IsEnabled || ExecutionMode.InGlobal || cache == null)
            return true;

        return cache.GetAllowed(RoleEntity.Current, permissionSymbol);
    }

    public static bool IsAuthorized(PermissionSymbol permissionSymbol, Lite<RoleEntity> role)
    {
        AssertRegistered(permissionSymbol);

        return cache.GetAllowed(role, permissionSymbol);
    }


    static void AssertRegistered(PermissionSymbol permissionSymbol)
    {
        if (!PermissionLogic.RegisteredPermission.Contains(permissionSymbol))
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
