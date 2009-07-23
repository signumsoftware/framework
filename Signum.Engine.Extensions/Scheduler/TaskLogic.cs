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

namespace Signum.Engine.Scheduler
{
    public static class TaskLogic
    {
        static Dictionary<Enum, Action> customTask = new Dictionary<Enum, Action>();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined<CustomTaskDN>())
            {
                EnumBag<CustomTaskDN>.Start(sb, () => customTask.Keys.ToHashSet());

                OperationLogic.Register(new BasicExecute<ITaskDN>(TaskOperation.Execute)
                {
                    Execute = (ct, _) => { throw new NotImplementedException(); }
                });

                OperationLogic.Register(new BasicExecute<CustomTaskDN>(TaskOperation.Execute)
                {
                    Execute = (ct, _) => ExecuteCustomTask(EnumBag<CustomTaskDN>.ToEnum(ct.Key))
                });

                OperationLogic.Register(new BasicExecute<ProcessDN>(TaskOperation.Execute)
                {
                    Execute = (pc, _) => ProcessLogic.Create(pc).ExecuteLazy(ProcessOperation.Execute)
                });
            }
        }

        public static void ExecuteCustomTask(Enum key)
        {
            customTask[key]();
        }

        public static void RegisterCustomTask(Enum taskKey, Action actionKey)
        {
            if (taskKey == null)
                throw new ArgumentNullException("taskKey");

            if (actionKey == null)
                throw new ArgumentNullException("actionKey");

            customTask.Add(taskKey, actionKey); 
        }      
    }
}
