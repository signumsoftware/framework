
namespace Signum.Entities.Authorization
{
    public class PermissionSymbol : Symbol
    {
        private PermissionSymbol() { }

        public PermissionSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }

    [AutoInit]
    public static class BasicPermission
    {
        public static PermissionSymbol AdminRules;
        public static PermissionSymbol AutomaticUpgradeOfProperties;
        public static PermissionSymbol AutomaticUpgradeOfQueries;
        public static PermissionSymbol AutomaticUpgradeOfOperations;
    }

    [AutoInit]
    public static class ActiveDirectoryPermission
    {
        public static PermissionSymbol InviteUsersFromAD;
    }
}
