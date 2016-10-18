using Signum.Entities;
using Signum.Entities.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Dynamic
{
    [AutoInit]
    public static class DynamicPanelPermission
    {
        public static PermissionSymbol ViewDynamicPanel;
        public static PermissionSymbol RestartApplication;
    }
}
