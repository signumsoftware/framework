using System;

namespace Signum.Entities.Authorization
{
    [Serializable]
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
}
