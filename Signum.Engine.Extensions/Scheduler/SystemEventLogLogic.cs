using System;
using System.Linq;
using System.Text;
using Signum.Entities.Scheduler;
using Signum.Engine.Maps;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.Basics;
using System.Threading;

namespace Signum.Engine.Scheduler
{
    public static class SystemEventLogLogic
    {
        public static bool Started = false;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SystemEventLogEntity>()
                    .WithQuery(() => s => new
                    {
                        Entity = s,
                        s.Id,
                        s.Date,
                        s.MachineName,
                        s.EventType,
                        s.Exception,
                    });
                
                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;

                Started = true;
            }
        }

        public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
        {
            var dateLimit = parameters.GetDateLimitDelete(typeof(SystemEventLogEntity).ToTypeEntity());
            if (dateLimit != null)
                Database.Query<SystemEventLogEntity>().Where(a => a.Date < dateLimit.Value).UnsafeDeleteChunksLog(parameters, sb, token);

            dateLimit = parameters.GetDateLimitDeleteWithExceptions(typeof(SystemEventLogEntity).ToTypeEntity());
            if (dateLimit == null)
                return;

            Database.Query<SystemEventLogEntity>().Where(a => a.Date < dateLimit.Value && a.Exception != null)
                .UnsafeUpdate()
                .Set(a => a.Exception, a => null)
                .ExecuteChunksLog(parameters, sb, token);
        }

        public static bool Log(string eventType, ExceptionEntity? exception = null)
        {
            if (!Started)
                return false;
            try
            {
                using (Transaction tr = Transaction.ForceNew())
                {
                    using (ExecutionMode.Global())
                        new SystemEventLogEntity
                        {
                            Date = TimeZoneManager.Now,
                            MachineName = Environment.MachineName,
                            User = UserHolder.Current?.ToLite(),
                            EventType = eventType,
                            Exception = exception?.ToLite()
                        }.Save();

                    tr.Commit();
                }

                return true;
            }
            catch (Exception e)
            {
                e.LogException(ex => ex.ControllerName = "SystemEventLog.Log");

                return false;
            }
        }
    }
}
