using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;

namespace Signum.Entities.Cache
{
    [AutoInit]
    public static class CachePermission
    {
        public static PermissionSymbol ViewCache;
        public static PermissionSymbol InvalidateCache;
    }
}
