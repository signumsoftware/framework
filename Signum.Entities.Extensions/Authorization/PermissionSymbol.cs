using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Runtime.CompilerServices;
using System.Diagnostics;

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

    public static class BasicPermission
    {
        public static readonly PermissionSymbol AdminRules = new PermissionSymbol();
        public static readonly PermissionSymbol AutomaticUpgradeOfProperties = new PermissionSymbol();
        public static readonly PermissionSymbol AutomaticUpgradeOfQueries = new PermissionSymbol();
        public static readonly PermissionSymbol AutomaticUpgradeOfOperations = new PermissionSymbol();
    }
}
