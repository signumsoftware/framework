using Signum.DiffLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.TimeMachine;

public static class TimeMachineLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            PermissionLogic.RegisterTypes(typeof(TimeMachinePermission));

            if (sb.WebServerBuilder != null)
                TimeMachineServer.Start(sb.WebServerBuilder.WebApplication);
        }
    }
}
