using System.Reflection;
using System.Collections.Generic;
using Signum.Entities.Scheduler;
using Signum.Utilities;

namespace Signum.Web.Extensions.Scheduler
{
    public static class CustomTaskClient
    {
        public static string ViewPrefix = "~/scheduler/Views/{0}.cshtml";
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<CustomTaskDN>(EntityType.SystemString){ PartialViewName = _ => ViewPrefix.Formato("CustomTask") },
                    new EntitySettings<CustomTaskExecutionDN>(EntityType.System){ PartialViewName = _ => ViewPrefix.Formato("CustomTaskExecution") },
                });
            }
        }
    }
}