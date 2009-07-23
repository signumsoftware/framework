using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Entities.Scheduler;
using Signum.Engine.Basics;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Engine.Operations;
using Signum.Entities;
using System.Threading;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Engine.Authorization;
using Signum.Engine.DynamicQuery;

namespace Signum.Engine.Scheduler
{
    public static class SchedulerLogic
    {
        public static event Action<string, Exception> Error; 

        static PriorityQueue<ScheduledTaskDN> priorityQueue = new PriorityQueue<ScheduledTaskDN>(new LambdaComparer<ScheduledTaskDN, DateTime>(st=>st.NextDate.Value));

        static Timer timer = new Timer(new TimerCallback(DispatchEvents), // main timer
								null,
								Timeout.Infinite,
								Timeout.Infinite);

        [ThreadStatic]
        static bool isSafeSave = false;

        static IDisposable SafeSaving()
        {
            bool lastSafe = isSafeSave;
            isSafeSave = true;
            return new Disposable(() => isSafeSave = lastSafe); 
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined<Schema>())
            {
                TaskLogic.Start(sb, dqm);
                sb.Include<ScheduledTaskDN>();
                sb.Schema.Initializing += new InitEventHandler(Schema_Initializing);
                sb.Schema.Saved += new EntityEventHandler(Schema_Saved);

                dqm[typeof(CustomTaskDN)] =
                     (from ct in Database.Query<CustomTaskDN>()
                      join cte in Database.Query<CustomTaskExecutionDN>().DefaultIfEmpty() on ct equals cte.CustomTask into g
                      select new
                      {
                          Entity = ct.ToLazy(),
                          ct.Id,
                          ct.Name,
                          NumExecutions = g.Count(),
                          LastExecution = (from cte2 in Database.Query<CustomTaskExecutionDN>()
                                           where cte2.Id == g.Max(a => a.Id)
                                           select cte2.ToLazy()).FirstOrDefault()
                      }).ToDynamic();

            }
        }

        static void Schema_Initializing(Schema sender)
        {
            ReloadPlan();
        }

        static void Schema_Saved(Schema sender, IdentifiableEntity ident)
        {
            if (ident is ScheduledTaskDN && !isSafeSave)
            {
                Transaction.RealCommit -= Transaction_RealCommit;
                Transaction.RealCommit += Transaction_RealCommit;
            }
        }

        static void Transaction_RealCommit()
        {
            ReloadPlan(); 
        }

        public static void ReloadPlan()
        {
            using (new EntityCache(true))
            lock(priorityQueue)
            {
                List<ScheduledTaskDN> schTasks = Database.Query<ScheduledTaskDN>().Where(st => !st.Suspended).ToList();

                using(SafeSaving())
                {
                    schTasks.SaveList(); //Force replanification
                }

                priorityQueue.Clear(); 
                priorityQueue.PushAll(schTasks);

                SetTimer(); 
            }
        }

        //Lock priorityQueue
        private static void SetTimer()
        {
            if (priorityQueue.Empty)
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            else
            {
                TimeSpan ts = priorityQueue.Peek().NextDate.Value - DateTime.Now;
                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero; // cannot be negative !

                timer.Change((int)ts.TotalMilliseconds, Timeout.Infinite); // invoke after the timespan
            }
        }

        static void OnError(string message, Exception ex)
        {
            if (Error != null)
                Error(message, ex); 
        }

        static void DispatchEvents(object obj) // obj ignored
        {
            lock (priorityQueue)
            {
                if (priorityQueue.Empty)
                    OnError("Inconstency in SchedulerLogic PriorityQueue", null);

                ScheduledTaskDN st = priorityQueue.Pop();
                using (SafeSaving())
                    st.Save();
                priorityQueue.Push(st);

                new Thread(() => st.Task.Execute(TaskOperation.Execute)).Start();

                SetTimer(); 
            }
        }
    }
}
