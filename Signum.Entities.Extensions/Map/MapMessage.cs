using Signum.Entities.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Map
{
    public enum MapMessage
    {
        Map,
        Namespace,
        TableSize,
        Columns,
        Rows,
    }

    public static class MapPermission
    {
        public static PermissionSymbol ViewMap = new PermissionSymbol();
    }
}
