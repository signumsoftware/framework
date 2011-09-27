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
using System.Threading.Tasks;

namespace Signum.Engine.Scheduler
{
    public static class SchedulerLogic
    {
        public static Polymorphic<Action<ITaskDN>> ExecuteTask = new Polymorphic<Action<ITaskDN>>(); 

        public static event Action<string, Exception> Error;

        static PriorityQueue<ScheduledTaskDN> priorityQueue = new PriorityQueue<ScheduledTaskDN>(new LambdaComparer<ScheduledTaskDN, DateTime>(st => st.NextDate.Value));

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
                AuthLogic.AssertStarted(sb);
                OperationLogic.AssertStarted(sb);

                ExecuteTask.Register((ITaskDN t) => { throw new NotImplementedException("SchedulerLogic.ExecuteTask not registered for {0}".Formato(t.GetType().Name)); });

                CustomTaskLogic.Start(sb, dqm);
                sb.Include<ScheduledTaskDN>();
                sb.Schema.Initializing[InitLevel.Level4BackgroundProcesses] += Schema_InitializingApplicaton;
                sb.Schema.EntityEvents<ScheduledTaskDN>().Saving += Schema_Saving;

                dqm[typeof(ScheduledTaskDN)] =
                    (from st in Database.Query<ScheduledTaskDN>()
                     select new
                     {
                         Entity = st.ToLite(),
                         st.Id,
                         //st.ToStr,
                         st.NextDate,
                         st.Suspended,
                     }).ToDynamic();

                dqm[typeof(CalendarDN)] =
                 (from st in Database.Query<CalendarDN>()
                  select new
                  {
                      Entity = st.ToLite(),
                      st.Id,
                      st.Name,
                      st.Holidays.Count,

                  }).ToDynamic();

                dqm[typeof(CustomTaskExecutionDN)] =
                    (from st in Database.Query<CustomTaskExecutionDN>()
                     select new
                     {
                         Entity = st.ToLite(),
                         st.Id,
                         st.StartTime,
                         st.EndTime,
                         st.Exception,

                     }).ToDynamic();

                dqm[typeof(ScheduledTaskDN)] =
                (from st in Database.Query<ScheduledTaskDN>()
                 select new
                 {
                     Entity = st.ToLite(),
                     st.Id,
                     st.NextDate,
                     st.Suspended,
                     Rule = st.Rule.ToLite(),
                     Task = st.Task.ToLite(),


                 }).ToDynamic();
            }
        }

        static void Schema_InitializingApplicaton()
        {
            ReloadPlan();
        }

        static void Schema_Saving(ScheduledTaskDN task)
        {
            if (!isSafeSave && task.Modified.Value)
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
                lock (priorityQueue)
                {
                    List<ScheduledTaskDN> schTasks = Database.Query<ScheduledTaskDN>().Where(st => !st.Suspended).ToList();

                    using (SafeSaving())
                    {
                        schTasks.SaveList(); //Force replanification
                    }

                    priorityQueue.Clear();
                    priorityQueue.PushAll(schTasks);

                    SetTimer();
                }
        }

        static TimeSpan SchedulerMargin = TimeSpan.FromSeconds(0.5); //stabilize sub-day schedulers

        //Lock priorityQueue
        private static void SetTimer()
        {
            if (priorityQueue.Empty)
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            else
            {
                TimeSpan ts = priorityQueue.Peek().NextDate.Value - TimeZoneManager.Now;
                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero; // cannot be negative !
                if (ts.TotalMilliseconds > int.MaxValue)
                    ts = TimeSpan.FromMilliseconds(int.MaxValue);

                timer.Change(ts.Add(SchedulerMargin), new TimeSpan(-1)); // invoke after the timespan
            }
        }

        static void OnError(string message, Exception ex)
        {
            if (Error != null)
                Error(message, ex);
        }

        static readonly TimeSpan MinimumSpan = TimeSpan.FromSeconds(10);

        static void DispatchEvents(object obj) // obj ignored
        {
            using (new EntityCache(true))
            using (AuthLogic.Disable())
                lock (priorityQueue)
                {
                    if (priorityQueue.Empty)
                    {
                        OnError("Inconstency in SchedulerLogic PriorityQueue", null);
                        return;
                    }

                    ScheduledTaskDN st = priorityQueue.Pop(); //Exceed timer change
                    if (st.NextDate.HasValue && (st.NextDate - TimeZoneManager.Now) > MinimumSpan)
                    {
                        priorityQueue.Push(st);
                        SetTimer();
                        return;
                    }

                    using (SafeSaving())
                        st.Save();
                    priorityQueue.Push(st);

                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            using (AuthLogic.User(AuthLogic.SystemUser))
                                ExecuteTask.Invoke(st.Task);
                        }
                        catch (Exception e)
                        {
                            OnError("Error executing task '{0}' with rule '{1}'".Formato(st.Task, st.Rule), e);
                        }
                    });

                    SetTimer();
                }
        }
    }
}
