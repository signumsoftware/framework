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

namespace Signum.Engine.Scheduler
{
    public static class CustomTaskLogic
    {
        static Dictionary<Enum, Action> tasks = new Dictionary<Enum, Action>();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                EnumLogic<CustomTaskDN>.Start(sb, () => tasks.Keys.ToHashSet());

                sb.Include<CustomTaskExecutionDN>();

                OperationLogic.Register(new BasicExecute<CustomTaskDN>(CustomTaskOperation.Execute)
                {
                    Execute = (ct, _) => Execute(EnumLogic<CustomTaskDN>.ToEnum(ct.Key))
                });

                OperationLogic.Register(new BasicExecute<CustomTaskDN>(TaskOperation.ExecutePrivate)
                {
                    Execute = (ct, _) =>  Execute(EnumLogic<CustomTaskDN>.ToEnum(ct.Key))
                });


                dqm[typeof(CustomTaskDN)] =
                      (from ct in Database.Query<CustomTaskDN>()
                       join cte in Database.Query<CustomTaskExecutionDN>().DefaultIfEmpty() on ct equals cte.CustomTask into g
                       select new
                       {
                           Entity = ct.ToLazy(),
                           ct.Id,
                           ct.Name,
                           NumExecutions = (int?)g.Count(),
                           LastExecution = (from cte2 in g
                                            where cte2.Id == g.Max(a => a.Id)
                                            select cte2.ToLazy()).SingleOrDefault()
                       }).ToDynamic()
                       .ChangeColumn(a => a.NumExecutions, c => c.DisplayName = Resources.Executions)
                       .ChangeColumn(a => a.LastExecution, c => c.DisplayName = Resources.LastExecution);

                dqm[typeof(CustomTaskExecutionDN)] =
                     (from cte in Database.Query<CustomTaskExecutionDN>()
                      select new
                      {
                          Entity = cte.ToLazy(),
                          cte.Id,
                          cte.StartTime,
                          cte.EndTime,
                          cte.Exception,
                          CustomTask = cte.CustomTask.ToLazy(),
                      }).ToDynamic()
                      .ChangeColumn(p => p.CustomTask, c => c.Visible = false);
            }
        }

        public static void Execute(Enum key)
        {
            CustomTaskExecutionDN cte = new CustomTaskExecutionDN
            {
                CustomTask = EnumLogic<CustomTaskDN>.ToEntity(key),
                StartTime = DateTime.Now,
            };

            try
            {
                cte.Save();

                tasks[key]();

                cte.EndTime = DateTime.Now;
                cte.Save();

            }
            catch(Exception ex)
            {
                using (Transaction tr=new Transaction(true))
                {
                    cte.Exception = ex.Message;
                    cte.Save(); 
                    tr.Commit(); 
                }
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
