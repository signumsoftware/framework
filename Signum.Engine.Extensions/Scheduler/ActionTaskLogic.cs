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
        static Dictionary<Enum, Action> tasks = new Dictionary<Enum, Action>();

        internal static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                MultiEnumLogic<ActionTaskDN>.Start(sb, () => tasks.Keys.ToHashSet());

                sb.Include<ScheduledTaskLogDN>();

                SchedulerLogic.ExecuteTask.Register((ActionTaskDN ct) =>
                {
                    Enum enumValue = MultiEnumLogic<ActionTaskDN>.ToEnum(ct.Key);
                    Action action = tasks.GetOrThrow(enumValue);
                    action();
                });


                dqm.RegisterQuery(typeof(ActionTaskDN), ()=>
                      from ct in Database.Query<ActionTaskDN>()
                       select new
                       {
                           Entity = ct,
                           ct.Id,
                           ct.Key,
                       });

                dqm.RegisterQuery(typeof(ScheduledTaskLogDN), ()=>
                     from cte in Database.Query<ScheduledTaskLogDN>()
                      select new
                      {
                          Entity = cte,
                          cte.Id,
                          ActionTask = cte.Task,
                          cte.StartTime,
                          cte.EndTime,
                          cte.Exception,
                      });

                dqm.RegisterExpression((ActionTaskDN ct) => ct.Executions());
                dqm.RegisterExpression((ActionTaskDN ct) => ct.LastExecution());
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
