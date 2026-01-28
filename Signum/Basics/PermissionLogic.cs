using Signum.Engine.Maps;

namespace Signum.Basics;

public static class PermissionLogic
{
    public static IEnumerable<PermissionSymbol> RegisteredPermission
    {
        get { return permissions; }
    }


    static HashSet<PermissionSymbol> permissions = new HashSet<PermissionSymbol>();
    public static void RegisterPermissions(params PermissionSymbol[] permissions)
    {
        foreach (var p in permissions)
        {
            if (p == null)
                throw AutoInitAttribute.ArgumentNullException(typeof(PermissionSymbol), nameof(permissions));

            PermissionLogic.permissions.Add(p);
        }
    }


    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<PermissionSymbol>();

        SymbolLogic<PermissionSymbol>.Start(sb, () => RegisteredPermission.ToHashSet());
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

                PermissionLogic.permissions.Add(p);
            }
        }
    }


    public static void AssertAuthorized(this PermissionSymbol permissionSymbol)
    {
        var message = permissionSymbol.IsAuthorizedString();

        if (message != null)
            throw new UnauthorizedAccessException(message);
    }

    public static Func<PermissionSymbol, string?> IsAuthorizedImplementation;

    public static string? IsAuthorizedString(this PermissionSymbol permissionSymbol)
    {
        foreach (var f in IsAuthorizedImplementation.GetInvocationListTyped())
        {
            var str = f(permissionSymbol);
            if (str != null)
                return str;
        }

        return null;
    }

    public static bool IsAuthorized(this PermissionSymbol permissionSymbol)
    {
        return permissionSymbol.IsAuthorizedString() == null;
    }

}
