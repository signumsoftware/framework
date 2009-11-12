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
using System.Reflection;

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
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertIsStarted(sb);
                OperationLogic.AssertIsStarted(sb); 

                OperationLogic.Register(new BasicExecute<ITaskDN>(TaskOperation.ExecutePrivate)
                {
                    Execute = (ct, _) => { throw new NotImplementedException(); }
                });

                CustomTaskLogic.Start(sb, dqm);
                sb.Include<ScheduledTaskDN>();
                sb.Schema.Initializing(InitLevel.Level4BackgroundProcesses, Schema_InitializingApplicaton);
                sb.Schema.EntityEvents<ScheduledTaskDN>().Saved += Schema_Saved;

                dqm[typeof(ScheduledTaskDN)] =
                    (from st in Database.Query<ScheduledTaskDN>()
                     select new
                     {
                         Entity = st.ToLite(),
                         st.Id,
                         st.ToStr,
                         st.NextDate,
                         st.Suspended,
                     }).ToDynamic();

                CustomTaskLogic.Start(sb, dqm); 
            }
        }

        static void Schema_InitializingApplicaton(Schema sender)
        {
            ReloadPlan();
        }

        static void Schema_Saved(ScheduledTaskDN task, bool isRoot)
        {
            if (!isSafeSave)
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
            using (AuthLogic.Disable())
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
            using (new EntityCache(true))
            using (AuthLogic.Disable())
            lock (priorityQueue)
            {
                if (priorityQueue.Empty)
                    OnError("Inconstency in SchedulerLogic PriorityQueue", null);

                ScheduledTaskDN st = priorityQueue.Pop();
                using (SafeSaving())
                    st.Save();
                priorityQueue.Push(st);

                new Thread(() =>
                {
                    using (AuthLogic.User(AuthLogic.SystemUser))
                        st.Task.ToLite().ExecuteLite(TaskOperation.ExecutePrivate);
                }).Start();

                SetTimer(); 
            }
        }
    }
}
