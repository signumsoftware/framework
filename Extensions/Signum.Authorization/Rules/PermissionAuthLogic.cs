using Signum.Utilities.Reflection;

namespace Signum.Authorization.Rules;


public static class PermissionAuthLogic
{
    static PermissionCache cache = null!;

    public static IManualAuth<PermissionSymbol, bool> Manual { get { return cache; } }

    public static bool IsStarted { get { return cache != null; } }

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => Start(null!)));
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        AuthLogic.AssertStarted(sb);
        PermissionLogic.Start(sb);

        sb.Include<RulePermissionEntity>()
           .WithUniqueIndex(rt => new { rt.Resource, rt.Role });

        cache = new PermissionCache(sb);

        sb.Schema.EntityEvents<RoleEntity>().PreUnsafeDelete += query =>
        {
            Database.Query<RulePermissionEntity>().Where(r => query.Contains(r.Role.Entity)).UnsafeDelete();
            return null;
        };

        PermissionLogic.RegisterTypes(typeof(BasicPermission));

        AuthLogic.ExportToXml += cache.ExportXml;
        AuthLogic.ImportFromXml += cache.ImportXml;

        PermissionLogic.IsAuthorizedImplementation = permissionSymbol =>
        {
            if (IsAuthorized(permissionSymbol))
                return null;

            return "Permission '{0}' is denied".FormatWith(permissionSymbol);
        };

        AuthLogic.HasRuleOverridesEvent += role => cache.HasRealOverrides(role);

        sb.Schema.EntityEvents<PermissionSymbol>().PreDeleteSqlSync += AuthCache_PreDeleteSqlSync;
    }

    static SqlPreCommand AuthCache_PreDeleteSqlSync(PermissionSymbol permission)
    {
        return Administrator.DeleteWhereScript((RulePermissionEntity rt) => rt.Resource, permission);
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
