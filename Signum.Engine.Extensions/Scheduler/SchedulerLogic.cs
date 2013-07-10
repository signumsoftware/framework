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
using Signum.Entities.Authorization;
using System.Linq.Expressions;

namespace Signum.Engine.Scheduler
{
    public static class SchedulerLogic
    {
        static Expression<Func<ITaskDN, IQueryable<ScheduledTaskLogDN>>> ExecutionsExpression =
         ct => Database.Query<ScheduledTaskLogDN>().Where(a => a.Task == ct);
        public static IQueryable<ScheduledTaskLogDN> Executions(this ITaskDN e)
        {
            return ExecutionsExpression.Evaluate(e);
        }

        static Expression<Func<ITaskDN, IQueryable<ScheduledTaskLogDN>>> LastExecutionExpression =
            e => e.Executions().OrderByDescending(a => a.StartTime).Take(1);
        public static IQueryable<ScheduledTaskLogDN> LastExecution(this ITaskDN e)
        {
            return LastExecutionExpression.Evaluate(e);
        }


        public static Polymorphic<Func<ITaskDN, Lite<IIdentifiable>>> ExecuteTask = new Polymorphic<Func<ITaskDN, Lite<IIdentifiable>>>();

        public static event Action<string, Exception> Error;

        static PriorityQueue<ScheduledTaskDN> priorityQueue = new PriorityQueue<ScheduledTaskDN>(new LambdaComparer<ScheduledTaskDN, DateTime>(st => st.NextDate.Value));

        static Timer timer = new Timer(new TimerCallback(DispatchEvents), // main timer
                                null,
                                Timeout.Infinite,
                                Timeout.Infinite);

        static readonly Variable<bool> avoidReloadPlan = Statics.ThreadVariable<bool>("avoidReloadSchedulerPlan");

        static IDisposable AvoidReloadPlanOnSave()
        {
            if (avoidReloadPlan.Value) return null;
            avoidReloadPlan.Value = true;
            return new Disposable(() => avoidReloadPlan.Value = false);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AuthLogic.AssertStarted(sb);
                OperationLogic.AssertStarted(sb);

                Implementations imp = sb.Settings.GetImplementations((ScheduledTaskDN st) => st.Task);
                Implementations imp2 = sb.Settings.GetImplementations((ScheduledTaskLogDN st) => st.Task);

                if (!imp2.Equals(imp2))
                    throw new InvalidOperationException("Implementations of ScheduledTaskDN.Task should be the same as in ScheduledTaskLogDN.Task"); 


                ExecuteTask.Register((ITaskDN t) => { throw new NotImplementedException("SchedulerLogic.ExecuteTask not registered for {0}".Formato(t.GetType().Name)); });

                SimpleTaskLogic.Start(sb, dqm);
                sb.Include<ScheduledTaskDN>();
                sb.Include<ScheduledTaskLogDN>();
                sb.Schema.Initializing[InitLevel.Level4BackgroundProcesses] += Schema_InitializingApplicaton;
                sb.Schema.EntityEvents<ScheduledTaskDN>().Saving += Schema_Saving;
                
                dqm.RegisterQuery(typeof(HolidayCalendarDN), ()=>
                     from st in Database.Query<HolidayCalendarDN>()
                      select new
                      {
                          Entity = st,
                          st.Id,
                          st.Name,
                          Holidays = st.Holidays.Count,
                      });


                dqm.RegisterQuery(typeof(ScheduledTaskDN), ()=>
                    from st in Database.Query<ScheduledTaskDN>()
                     select new
                     {
                         Entity = st,
                         st.Id,
                         st.Task,
                         st.NextDate,
                         st.Suspended,
                         st.Rule,
                     });

                dqm.RegisterQuery(typeof(ScheduledTaskLogDN), () =>
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

                dqm.RegisterExpression((ITaskDN ct) => ct.Executions());
                dqm.RegisterExpression((ITaskDN ct) => ct.LastExecution());

                new Graph<HolidayCalendarDN>.Execute(CalendarOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (c, _) => { },
                }.Register();

                new Graph<ScheduledTaskDN>.Execute(ScheduledTaskOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (st, _) => { },
                }.Register();

                new Graph<IIdentifiable>.ConstructFrom<ITaskDN>(TaskOperation.ExecuteSync)
                {
                    Construct = (task, _) => ExecuteSync(task).TryCC(l => l.Retrieve())
                }.Register();

                new Graph<ITaskDN>.Execute(TaskOperation.ExecuteAsync)
                {
                    Execute = (task, _) => ExecuteAsync(task)
                }.Register();
            }
        }

        static void Schema_InitializingApplicaton()
        {
            ReloadPlan();
        }

        static void Schema_Saving(ScheduledTaskDN task)
        {
            if (!avoidReloadPlan.Value && task.IsGraphModified)
            {
                Transaction.PostRealCommit -= Transaction_RealCommit;
                Transaction.PostRealCommit += Transaction_RealCommit;
            }
        }

        static void Transaction_RealCommit(Dictionary<string, object> userData)
        {
            ReloadPlan();
        }

        static bool enabled = false;
        public static bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value; 
                if (enabled) 
                    ReloadPlan();
            }
        }

        public static void ReloadPlan()
        {
            if (!enabled)
                return;

            using (new EntityCache(EntityCacheType.ForceNew))
            using (AuthLogic.Disable())
                lock (priorityQueue)
                {
                    List<ScheduledTaskDN> schTasks = Database.Query<ScheduledTaskDN>().Where(st => !st.Suspended).ToList();

                    using (AvoidReloadPlanOnSave())
                    using (OperationLogic.AllowSave<ScheduledTaskDN>())
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
            using (new EntityCache(EntityCacheType.ForceNew))
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

                    using (AvoidReloadPlanOnSave())
                    using (OperationLogic.AllowSave<ScheduledTaskDN>())
                        st.Save();

                    priorityQueue.Push(st);


                    ExecuteAsync(st.Task);

                    SetTimer();
                }
        }

        public static void ExecuteAsync(ITaskDN task)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    ExecuteSync(task);
                }
                catch (Exception e)
                {
                    OnError("Error executing task '{0}'".Formato(task), e);
                }
            }); 
        }


        public static Lite<IIdentifiable> ExecuteSync(ITaskDN task)
        {
            using (AuthLogic.UserSession(AuthLogic.SystemUser))
            {
                ScheduledTaskLogDN stl = new ScheduledTaskLogDN
                {
                    Task = task,
                    StartTime = TimeZoneManager.Now,
                };

                try
                {
                    using (Transaction tr = new Transaction())
                    {
                        stl.Save();

                        stl.Entity = ExecuteTask.Invoke(task);

                        stl.EndTime = TimeZoneManager.Now;
                        stl.Save();

                        return tr.Commit(stl.Entity);
                    }
                }
                catch (Exception ex)
                {
                    if (Transaction.InTestTransaction)
                        throw;

                    var exLog = ex.LogException().ToLite();

                    using (Transaction tr2 = Transaction.ForceNew())
                    {
                        ScheduledTaskLogDN cte2 = new ScheduledTaskLogDN
                        {
                            Task = stl.Task,
                            StartTime = stl.StartTime,
                            EndTime = TimeZoneManager.Now,
                            Exception = exLog,
                        }.Save();

                        tr2.Commit();
                    }

                    throw;
                }
            }
        }
    }
}
