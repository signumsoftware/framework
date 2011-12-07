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
using Signum.Engine.Extensions.Properties;
using System.Reflection;
using Signum.Entities.Authorization;
using System.Linq.Expressions;
using Signum.Entities.Logging;
using Signum.Engine.Logging;

namespace Signum.Engine.Scheduler
{
    public static class CustomTaskLogic
    {
        static Expression<Func<CustomTaskDN, IQueryable<CustomTaskExecutionDN>>> ExecutionsExpression =
            ct => Database.Query<CustomTaskExecutionDN>().Where(a => a.CustomTask == ct);
        public static IQueryable<CustomTaskExecutionDN> Executions(this CustomTaskDN e)
        {
            return ExecutionsExpression.Evaluate(e);
        }

        static Expression<Func<CustomTaskDN, IQueryable<CustomTaskExecutionDN>>> LastExecutionExpression =
            e => e.Executions().OrderByDescending(a => a.StartTime).Take(1);
        public static IQueryable<CustomTaskExecutionDN> LastExecution(this CustomTaskDN e)
        {
            return LastExecutionExpression.Evaluate(e);
        }

        static Dictionary<Enum, Action> tasks = new Dictionary<Enum, Action>();

        internal static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                EnumLogic<CustomTaskDN>.Start(sb, () => tasks.Keys.ToHashSet());

                sb.Include<CustomTaskExecutionDN>();

                new BasicExecute<CustomTaskDN>(CustomTaskOperation.Execute)
                {
                    Execute = (ct, _) => Execute(EnumLogic<CustomTaskDN>.ToEnum(ct.Key))
                }.Register();

                SchedulerLogic.ExecuteTask.Register((CustomTaskDN ct) =>
                    Execute(EnumLogic<CustomTaskDN>.ToEnum(ct.Key)));

                dqm[typeof(CustomTaskDN)] =
                      (from ct in Database.Query<CustomTaskDN>()
                       select new
                       {
                           Entity = ct.ToLite(),
                           ct.Id,
                           ct.Name,
                       }).ToDynamic();

                dqm[typeof(CustomTaskExecutionDN)] =
                     (from cte in Database.Query<CustomTaskExecutionDN>()
                      select new
                      {
                          Entity = cte.ToLite(),
                          cte.Id,
                          CustomTask = cte.CustomTask.ToLite(),
                          cte.StartTime,
                          cte.EndTime,
                          cte.Exception,
                      }).ToDynamic();

                dqm.RegisterExpression((CustomTaskDN ct) => ct.Executions());
                dqm.RegisterExpression((CustomTaskDN ct) => ct.LastExecution());
            }
        }

        public static void Execute(Enum key)
        {
            CustomTaskExecutionDN cte = new CustomTaskExecutionDN
            {
                CustomTask = EnumLogic<CustomTaskDN>.ToEntity(key),
                StartTime = TimeZoneManager.Now,
            };

            try
            {
                using (Transaction tr = new Transaction())
                {
                    cte.Save();

                    tasks[key]();

                    cte.EndTime = TimeZoneManager.Now;
                    cte.Save();

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                using (Transaction tr2 = new Transaction(true))
                {
                    CustomTaskExecutionDN cte2 = new CustomTaskExecutionDN
                    {
                        CustomTask = cte.CustomTask,
                        StartTime = cte.StartTime,
                        EndTime = TimeZoneManager.Now,
                        Exception = ex.LogException().ToLite(),
                    }.Save();

                    tr2.Commit();
                }

                throw;
            }
        }

        public static void Register(Enum taskKey, Action actionKey)
        {
            if (taskKey == null)
                throw new ArgumentNullException("taskKey");

            if (actionKey == null)
                throw new ArgumentNullException("actionKey");

            tasks.Add(taskKey, actionKey); 
        }      
    }
}
