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

namespace Signum.Engine.Scheduler
{
    public static class TaskLogic
    {
        static Dictionary<Enum, Action> customTask = new Dictionary<Enum, Action>();

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined<ITask>())
            {
                EnumBag<CustomTaskDN>.Start(sb, () => customTask.Keys.ToHashSet());

                sb.Include<ProcessTaskDN>();

                OperationLogic.Register(new BasicExecute<ITask>(TaskOperation.Execute) 
                { 
                    Execute = (_, __) => { throw new NotImplementedException(); } 
                });

                OperationLogic.Register(new BasicExecute<ProcessTaskDN>(TaskOperation.Execute) 
                { 
                    Execute = (pt, _) => ProcessLogic.Create(pt.Process).ExecuteLazy(ProcessOperation.Execute) 
                });

                OperationLogic.Register(new BasicExecute<CustomTaskDN>(TaskOperation.Execute)
                {
                    Execute = (ct, _) => ExecuteCustomTask(EnumBag<CustomTaskDN>.ToEnum(ct.Key))
                });
            }
        }

        public static void ExecuteCustomTask(Enum key)
        {
            customTask[key]();
        }
    }
}
