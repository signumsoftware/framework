namespace Signum.Authorization.Rules;



[AutoInit]
public static class BasicPermission
{
    public static PermissionSymbol AdminRules;
    public static PermissionSymbol AutomaticUpgradeOfProperties;
    public static PermissionSymbol AutomaticUpgradeOfQueries;
    public static PermissionSymbol AutomaticUpgradeOfOperations;
    /// <summary>Stack traces and the link to the logged exception, in the error dialog and in server error responses.</summary>
    public static PermissionSymbol ViewErrorDetails;
}

