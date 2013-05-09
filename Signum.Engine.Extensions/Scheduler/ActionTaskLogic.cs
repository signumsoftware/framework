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
using Signum.Engine.Exceptions;

namespace Signum.Engine.Scheduler
{
    public static class ActionTaskLogic
    {
        static Expression<Func<ActionTaskDN, IQueryable<ActionTaskLogDN>>> ExecutionsExpression =
            ct => Database.Query<ActionTaskLogDN>().Where(a => a.ActionTask == ct);
        public static IQueryable<ActionTaskLogDN> Executions(this ActionTaskDN e)
        {
            return ExecutionsExpression.Evaluate(e);
        }

        static Expression<Func<ActionTaskDN, IQueryable<ActionTaskLogDN>>> LastExecutionExpression =
            e => e.Executions().OrderByDescending(a => a.StartTime).Take(1);
        public static IQueryable<ActionTaskLogDN> LastExecution(this ActionTaskDN e)
        {
            return LastExecutionExpression.Evaluate(e);
        }

        static Dictionary<Enum, Action> tasks = new Dictionary<Enum, Action>();

        internal static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                MultiEnumLogic<ActionTaskDN>.Start(sb, () => tasks.Keys.ToHashSet());

                sb.Include<ActionTaskLogDN>();

                new BasicExecute<ActionTaskDN>(ActionTaskOperation.Execute)
                {
                    Execute = (ct, _) => Execute(MultiEnumLogic<ActionTaskDN>.ToEnum(ct.Key))
                }.Register();

                SchedulerLogic.ExecuteTask.Register((ActionTaskDN ct) =>
                    Execute(MultiEnumLogic<ActionTaskDN>.ToEnum(ct.Key)));

                dqm.RegisterQuery(typeof(ActionTaskDN), ()=>
                      from ct in Database.Query<ActionTaskDN>()
                       select new
                       {
                           Entity = ct,
                           ct.Id,
                           ct.Key,
                       });

                dqm.RegisterQuery(typeof(ActionTaskLogDN), ()=>
                     from cte in Database.Query<ActionTaskLogDN>()
                      select new
                      {
                          Entity = cte,
                          cte.Id,
                          cte.ActionTask,
                          cte.StartTime,
                          cte.EndTime,
                          cte.Exception,
                      });

                dqm.RegisterExpression((ActionTaskDN ct) => ct.Executions());
                dqm.RegisterExpression((ActionTaskDN ct) => ct.LastExecution());
            }
        }

        public static void Execute(Enum key)
        {
            ActionTaskLogDN cte = new ActionTaskLogDN
            {
                ActionTask = MultiEnumLogic<ActionTaskDN>.ToEntity(key),
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
                if (Transaction.InTestTransaction)
                    throw; 

                var exLog = ex.LogException().ToLite();

                using (Transaction tr2 = Transaction.ForceNew())
                {
                    ActionTaskLogDN cte2 = new ActionTaskLogDN
                    {
                        ActionTask = cte.ActionTask,
                        StartTime = cte.StartTime,
                        EndTime = TimeZoneManager.Now,
                        Exception = exLog,
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
