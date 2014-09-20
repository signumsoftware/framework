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
using Signum.Engine.Cache;
using Signum.Entities.Basics;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Scheduler
{
    public static class SchedulerLogic
    {
        public static Func<ITaskDN, IDisposable> ApplySession;

        static Expression<Func<ITaskDN, IQueryable<ScheduledTaskLogDN>>> ExecutionsExpression =
         ct => Database.Query<ScheduledTaskLogDN>().Where(a => a.Task == ct);
        public static IQueryable<ScheduledTaskLogDN> Executions(this ITaskDN e)
        {
            return ExecutionsExpression.Evaluate(e);
        }

        static Expression<Func<ITaskDN, ScheduledTaskLogDN>> LastExecutionExpression =
            e => e.Executions().OrderByDescending(a => a.StartTime).FirstOrDefault();
        public static ScheduledTaskLogDN LastExecution(this ITaskDN e)
        {
            return LastExecutionExpression.Evaluate(e);
        }

        static Expression<Func<ScheduledTaskDN, IQueryable<ScheduledTaskLogDN>>> ExecutionsSTExpression =
            ct => Database.Query<ScheduledTaskLogDN>().Where(a => a.ScheduledTask == ct);
        [ExpressionField("ExecutionsSTExpression")]
        public static IQueryable<ScheduledTaskLogDN> Executions(this ScheduledTaskDN e)
        {
            return ExecutionsSTExpression.Evaluate(e);
        }

        public static Polymorphic<Func<ITaskDN, Lite<IIdentifiable>>> ExecuteTask = new Polymorphic<Func<ITaskDN, Lite<IIdentifiable>>>();
        
        public class ScheduledTaskPair
        {
            public ScheduledTaskDN ScheduledTask;
            public DateTime NextDate; 
        }

        static ResetLazy<List<ScheduledTaskDN>> ScheduledTasksLazy;

        static PriorityQueue<ScheduledTaskPair> priorityQueue = new PriorityQueue<ScheduledTaskPair>((a, b) => a.NextDate.CompareTo(b.NextDate));

        static Timer timer = new Timer(new TimerCallback(TimerCallback), // main timer
                                null,
                                Timeout.Infinite,
                                Timeout.Infinite);


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

                PermissionAuthLogic.RegisterPermissions(SchedulerPermission.ViewSchedulerPanel);

                ExecuteTask.Register((ITaskDN t) => { throw new NotImplementedException("SchedulerLogic.ExecuteTask not registered for {0}".Formato(t.GetType().Name)); });

                SimpleTaskLogic.Start(sb, dqm);
                sb.Include<ScheduledTaskDN>();
                sb.Include<ScheduledTaskLogDN>();

                dqm.RegisterQuery(typeof(HolidayCalendarDN), () =>
                     from st in Database.Query<HolidayCalendarDN>()
                     select new
                     {
                         Entity = st,
                         st.Id,
                         st.Name,
                         Holidays = st.Holidays.Count,
                     });


                dqm.RegisterQuery(typeof(ScheduledTaskDN), () =>
                    from st in Database.Query<ScheduledTaskDN>()
                    select new
                    {
                        Entity = st,
                        st.Id,
                        st.Task,
                        st.Rule,
                        st.Suspended,
                        st.MachineName,
                        st.ApplicationName
                    });

                dqm.RegisterQuery(typeof(ScheduledTaskLogDN), () =>
                    from cte in Database.Query<ScheduledTaskLogDN>()
                    select new
                    {
                        Entity = cte,
                        cte.Id,
                        cte.Task,
                        cte.ScheduledTask,
                        cte.StartTime,
                        cte.EndTime,
                        cte.ProductEntity,
                        cte.MachineName,
                        cte.User,
                        cte.Exception,
                        
                    });

                dqm.RegisterExpression((ITaskDN ct) => ct.Executions(), () => TaskMessage.Executions.NiceToString());
                dqm.RegisterExpression((ITaskDN ct) => ct.LastExecution(), () => TaskMessage.LastExecution.NiceToString());
                dqm.RegisterExpression((ScheduledTaskDN ct) => ct.Executions(), () => TaskMessage.Executions.NiceToString());

                new Graph<HolidayCalendarDN>.Execute(HolidayCalendarOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (c, _) => { },
                }.Register();

                new Graph<HolidayCalendarDN>.Delete(HolidayCalendarOperation.Delete)
                {
                    Delete = (c, _) => { c.Delete(); },
                }.Register();

                new Graph<ScheduledTaskDN>.Execute(ScheduledTaskOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (st, _) => { },
                }.Register();

                new Graph<ScheduledTaskDN>.Delete(ScheduledTaskOperation.Delete)
                {
                    Delete = (st, _) => { st.Executions().UnsafeDelete(); var rule = st.Rule; st.Delete(); rule.Delete(); },
                }.Register();


                new Graph<IIdentifiable>.ConstructFrom<ITaskDN>(TaskOperation.ExecuteSync)
                {
                    Construct = (task, _) => ExecuteSync(task, null, UserHolder.Current).Try(l => l.Retrieve())
                }.Register();

                new Graph<ITaskDN>.Execute(TaskOperation.ExecuteAsync)
                {
                    Execute = (task, _) => ExecuteAsync(task, null, UserHolder.Current)
                }.Register();

                ScheduledTasksLazy = sb.GlobalLazy(() =>
                    Database.Query<ScheduledTaskDN>().Where(a => !a.Suspended && 
                        (a.MachineName == ScheduledTaskDN.None || a.MachineName == Environment.MachineName && a.ApplicationName == Schema.Current.ApplicationName)).ToList(),
                    new InvalidateWith(typeof(ScheduledTaskDN)));

                ScheduledTasksLazy.OnReset += ScheduledTasksLazy_OnReset;

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            }
        }

        public static void ExceptionLogic_DeleteLogs(DateTime limite)
        {
            Database.Query<ScheduledTaskLogDN>().Where(a => a.StartTime < limite).UnsafeDelete();
        }

        static void ScheduledTasksLazy_OnReset(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() => { Thread.Sleep(1000); ReloadPlan(); });
        }


        static bool running = false;
        public static bool Running
        {
            get { return running; }
        }

        public static void StartScheduledTasks()
        {
            if (running)
                throw new InvalidOperationException("SchedulerLogic is already Running in {0}".Formato(Environment.MachineName));

            running = true;

            ReloadPlan();
        }

        public static void StopScheduledTasks()
        {
            if (!running)
                throw new InvalidOperationException("SchedulerLogic is already Stopped in {0}".Formato(Environment.MachineName));

            lock (priorityQueue)
            {
                if (!running)
                    return;

                running = false;

                priorityQueue.Clear();
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        static void ReloadPlan()
        {
            if (!running)
                return;

            using (new EntityCache(EntityCacheType.ForceNew))
            using (AuthLogic.Disable())
                lock (priorityQueue)
                {
                    priorityQueue.Clear();
                    priorityQueue.PushAll(ScheduledTasksLazy.Value.Select(st => new ScheduledTaskPair
                    {
                        ScheduledTask = st,
                        NextDate = st.Rule.Next(),
                    }));

                    SetTimer();
                }
        }

        static TimeSpan SchedulerMargin = TimeSpan.FromSeconds(0.5); //stabilize sub-day schedulers

        static DateTime? NextExecution;

        //Lock priorityQueue
        static void SetTimer()
        {
            lock (priorityQueue)
                NextExecution = priorityQueue.Empty ? (DateTime?)null : priorityQueue.Peek().NextDate;

            if (NextExecution == null)
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            else
            {
                TimeSpan ts = NextExecution.Value - TimeZoneManager.Now;

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero; // cannot be negative !
                if (ts.TotalMilliseconds > int.MaxValue)
                    ts = TimeSpan.FromMilliseconds(int.MaxValue);

                timer.Change(ts.Add(SchedulerMargin), new TimeSpan(-1)); // invoke after the timespan
            }
        }

        static void TimerCallback(object obj) // obj ignored
        {
            try
            {
                using (new EntityCache(EntityCacheType.ForceNew))
                using (AuthLogic.Disable())
                    lock (priorityQueue)
                    {
                        if (priorityQueue.Empty)
                        {
                            throw new InvalidOperationException("Inconstency in SchedulerLogic PriorityQueue");
                        }

                        var pair = priorityQueue.Pop(); //Exceed timer change
                        if (Math.Abs((pair.NextDate - TimeZoneManager.Now).Ticks) < ScheduledTaskDN.MinimumSpan.Ticks)
                        {
                            ExecuteAsync(pair.ScheduledTask.Task, pair.ScheduledTask, null);
                        }

                        pair.NextDate = pair.ScheduledTask.Rule.Next();
                        priorityQueue.Push(pair);
                        SetTimer();
                        return;
                    }
            }
            catch (Exception e)
            {
                e.LogException(ex =>
                {
                    ex.ControllerName = "SchedulerLogic";
                    ex.ActionName = "TimerCallback";
                });
            }
        }

        public static void ExecuteAsync(ITaskDN task, ScheduledTaskDN scheduledTask, IUserDN user)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    ExecuteSync(task, scheduledTask, user);
                }
                catch (Exception e)
                {
                    e.LogException(ex =>
                    {
                        ex.ControllerName = "SchedulerLogic";
                        ex.ActionName = "ExecuteAsync";
                    });
                }
            }); 
        }

        public static Lite<IIdentifiable> ExecuteSync(ITaskDN task, ScheduledTaskDN scheduledTask, IUserDN user)
        {
            using (AuthLogic.UserSession(AuthLogic.SystemUser))
            using (Disposable.Combine(ApplySession, f => f(task)))
            {
                ScheduledTaskLogDN stl = new ScheduledTaskLogDN
                {
                    Task = task,
                    ScheduledTask = scheduledTask, 
                    User = user.ToLite(),
                    StartTime = TimeZoneManager.Now,
                    MachineName = Environment.MachineName,
                    ApplicationName = Schema.Current.ApplicationName
                };

                using (Transaction tr = Transaction.ForceNew())
                {
                    stl.Save();
                 
                    tr.Commit();
                }

                try
                {
                    using (Transaction tr = new Transaction())
                    {
                        

                        stl.ProductEntity = ExecuteTask.Invoke(task);

                        stl.EndTime = TimeZoneManager.Now;
                        stl.Save();

                        return tr.Commit(stl.ProductEntity);
                    }
                }
                catch (Exception ex)
                {
                    if (Transaction.InTestTransaction)
                        throw;

                    var exLog = ex.LogException().ToLite();

                    using (Transaction tr2 = Transaction.ForceNew())
                    {
                        stl.Exception = exLog;

                        stl.Save();

                        tr2.Commit();
                    }

                    throw;
                }
            }
        }

        public static SchedulerState GetSchedulerState()
        {
            return new SchedulerState
            {
                Running = Running,
                SchedulerMargin = SchedulerMargin,
                NextExecution = NextExecution,
                Queue = priorityQueue.GetOrderedList().Select(p => new SchedulerItemState
                {
                    ScheduledTask = p.ScheduledTask.ToLite(),
                    Rule = p.ScheduledTask.Rule.ToString(),
                    NextExecution = p.NextDate,
                }).ToList()
            };
        }
    }

    public class SchedulerState
    {
        public bool Running;
        public TimeSpan SchedulerMargin;
        public DateTime? NextExecution;
        public List<SchedulerItemState> Queue; 
    }

    public class SchedulerItemState
    {
        public Lite<ScheduledTaskDN> ScheduledTask;
        public string Rule; 
        public DateTime NextExecution; 
    }
}
