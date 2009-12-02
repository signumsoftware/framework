using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Services;

namespace Signum.Windows
{
    public static class AuthServer
    {
        public static bool IsAuthorized(this Enum permissionKey)
        {
            return Server.Return((IPermissionAuthServer s) => s.IsAuthorized(permissionKey)); 
        }
    }
}
