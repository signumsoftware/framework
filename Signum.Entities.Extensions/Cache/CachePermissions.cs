using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;

namespace Signum.Entities.Cache
{
    public static class CachePermission
    {
        public static readonly PermissionSymbol ViewCache = new PermissionSymbol();
        public static readonly PermissionSymbol InvalidateCache = new PermissionSymbol();
    }
}
