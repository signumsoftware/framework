using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Operations;
using Signum.Entities.Scheduler;
using Signum.Engine.Processes;
using Signum.Engine.Maps;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Entities.Processes;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.Authorization;
using System.Linq.Expressions;
using Signum.Engine.Authorization;
using Signum.Entities.Basics;

namespace Signum.Engine.Scheduler
{
    public static class ApplicationEventLogLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ApplicationEventLogEntity>();
                dqm.RegisterQuery(typeof(ApplicationEventLogEntity), () =>
                   from s in Database.Query<ApplicationEventLogEntity>()
                   select new
                   {
                       Entity = s,
                       s.Id,
                       s.MachineName,
                       s.GlobalEvent,
                       s.Date,
                   });


                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;

            }
        }

        public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEntity parameters)
        {
            Database.Query<ApplicationEventLogEntity>().Where(a => a.Date < parameters.DateLimit).UnsafeDeleteChunks(parameters.ChunkSize, parameters.MaxChunks);
        }

        public static void ApplicationStart()
        {
            using (AuthLogic.Disable())
                new ApplicationEventLogEntity { Date = TimeZoneManager.Now, MachineName = Environment.MachineName, GlobalEvent = TypeEvent.Start }.Save();
        }

        public static void ApplicationEnd()
        {
            using (AuthLogic.Disable())
                new ApplicationEventLogEntity { Date = TimeZoneManager.Now, MachineName = Environment.MachineName, GlobalEvent = TypeEvent.Stop }.Save();
        }

    }
}
