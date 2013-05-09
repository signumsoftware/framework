using System.Reflection;
using System.Collections.Generic;
using Signum.Entities.Scheduler;
using Signum.Utilities;

namespace Signum.Web.Extensions.Scheduler
{
    public static class ActionTaskClient
    {
        public static string ViewPrefix = "~/scheduler/Views/{0}.cshtml";
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ActionTaskDN>{ PartialViewName = _ => ViewPrefix.Formato("ActionTask") },
                    new EntitySettings<ActionTaskLogDN>{ PartialViewName = _ => ViewPrefix.Formato("ActionTaskLog") },
                });
            }
        }
    }
}