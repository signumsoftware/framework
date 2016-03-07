using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Engine.Authorization;
using Signum.Engine;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Authorization;
using System.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Basics;
using Signum.Engine.Operations;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Entities.Notes;
using Signum.Engine.Extensions.Basics;
using Signum.Entities.Map;

namespace Signum.Engine.Map
{
    public static class MapLogic
    {
       
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                PermissionAuthLogic.RegisterPermissions(MapPermission.ViewMap);
            }
        }
    }
}
