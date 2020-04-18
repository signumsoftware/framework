using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Authorization;
using System.Reflection;
using Signum.Entities.Basics;
using Signum.Engine.Operations;
using System.Linq.Expressions;
using Signum.Entities.Notes;
using Signum.Engine.Extensions.Basics;
using Signum.Engine.Basics;
using Signum.Engine;
using Signum.Engine.Authorization;
using Signum.Entities.Omnibox;

namespace Signum.Engine.Omnibox
{
    public static class OmniboxLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodBase.GetCurrentMethod()))
            {
                PermissionAuthLogic.RegisterTypes(typeof(OmniboxPermission));
            }
        }
    }
}
