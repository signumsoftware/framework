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
                    new EntitySettings<CustomTaskExecutionDN>(EntityType.Default){ PartialViewName = _ => ViewPrefix.Formato("CustomTaskExecution") },
                    new EntitySettings<CustomTaskDN>(EntityType.Admin){ PartialViewName = _ => ViewPrefix.Formato("CustomTask") },
                });
            }
        }
    }
}