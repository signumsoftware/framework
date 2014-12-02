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
        public static Func<ITaskEntity, IDisposable> ApplySession;

        static Expression<Func<ITaskEntity, IQueryable<ScheduledTaskLogEntity>>> ExecutionsExpression =
         ct => Database.Query<ScheduledTaskLogEntity>().Where(a => a.Task == ct);
        public static IQueryable<ScheduledTaskLogEntity> Executions(this ITaskEntity e)
        {
            return ExecutionsExpression.Evaluate(e);
        }

        static Expression<Func<ITaskEntity, ScheduledTaskLogEntity>> LastExecutionExpression =
            e => e.Executions().OrderByDescending(a => a.StartTime).FirstOrDefault();
        public static ScheduledTaskLogEntity LastExecution(this ITaskEntity e)
        {
            return LastExecutionExpression.Evaluate(e);
        }

        static Expression<Func<ScheduledTaskEntity, IQueryable<ScheduledTaskLogEntity>>> ExecutionsSTExpression =
            ct => Database.Query<ScheduledTaskLogEntity>().Where(a => a.ScheduledTask == ct);
        [ExpressionField("ExecutionsSTExpression")]
        public static IQueryable<ScheduledTaskLogEntity> Executions(this ScheduledTaskEntity e)
        {
            return ExecutionsSTExpression.Evaluate(e);
        }

        public static Polymorphic<Func<ITaskEntity, Lite<IEntity>>> ExecuteTask = new Polymorphic<Func<ITaskEntity, Lite<IEntity>>>();
        
        public class ScheduledTaskPair
        {
            public ScheduledTaskEntity ScheduledTask;
            public DateTime NextDate; 
        }

        static ResetLazy<List<ScheduledTaskEntity>> ScheduledTasksLazy;

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

                Implementations imp = sb.Settings.GetImplementations((ScheduledTaskEntity st) => st.Task);
                Implementations imp2 = sb.Settings.GetImplementations((ScheduledTaskLogEntity st) => st.Task);

                if (!imp2.Equals(imp2))
                    throw new InvalidOperationException("Implementations of ScheduledTaskEntity.Task should be the same as in ScheduledTaskLogEntity.Task");

                PermissionAuthLogic.RegisterPermissions(SchedulerPermission.ViewSchedulerPanel);

                ExecuteTask.Register((ITaskEntity t) => { throw new NotImplementedException("SchedulerLogic.ExecuteTask not registered for {0}".FormatWith(t.GetType().Name)); });

                SimpleTaskLogic.Start(sb, dqm);
                sb.Include<ScheduledTaskEntity>();
                sb.Include<ScheduledTaskLogEntity>();

                dqm.RegisterQuery(typeof(HolidayCalendarEntity), () =>
                     from st in Database.Query<HolidayCalendarEntity>()
                     select new
                     {
                         Entity = st,
                         st.Id,
                         st.Name,
                         Holidays = st.Holidays.Count,
                     });


                dqm.RegisterQuery(typeof(ScheduledTaskEntity), () =>
                    from st in Database.Query<ScheduledTaskEntity>()
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

                dqm.RegisterQuery(typeof(ScheduledTaskLogEntity), () =>
                    from cte in Database.Query<ScheduledTaskLogEntity>()
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

                dqm.RegisterExpression((ITaskEntity ct) => ct.Executions(), () => TaskMessage.Executions.NiceToString());
                dqm.RegisterExpression((ITaskEntity ct) => ct.LastExecution(), () => TaskMessage.LastExecution.NiceToString());
                dqm.RegisterExpression((ScheduledTaskEntity ct) => ct.Executions(), () => TaskMessage.Executions.NiceToString());

                new Graph<HolidayCalendarEntity>.Execute(HolidayCalendarOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (c, _) => { },
                }.Register();

                new Graph<HolidayCalendarEntity>.Delete(HolidayCalendarOperation.Delete)
                {
                    Delete = (c, _) => { c.Delete(); },
                }.Register();

                new Graph<ScheduledTaskEntity>.Execute(ScheduledTaskOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (st, _) => { },
                }.Register();

                new Graph<ScheduledTaskEntity>.Delete(ScheduledTaskOperation.Delete)
                {
                    Delete = (st, _) =>
                    {
                        st.Executions().UnsafeUpdate().Set(l => l.ScheduledTask, l => null).Execute();
                        var rule = st.Rule; st.Delete(); rule.Delete();
                    },
                }.Register();


                new Graph<IEntity>.ConstructFrom<ITaskEntity>(TaskOperation.ExecuteSync)
                {
                    Construct = (task, _) => ExecuteSync(task, null, UserHolder.Current).Try(l => l.Retrieve())
                }.Register();

                new Graph<ITaskEntity>.Execute(TaskOperation.ExecuteAsync)
                {
                    Execute = (task, _) => ExecuteAsync(task, null, UserHolder.Current)
                }.Register();

                ScheduledTasksLazy = sb.GlobalLazy(() =>
                    Database.Query<ScheduledTaskEntity>().Where(a => !a.Suspended && 
                        (a.MachineName == ScheduledTaskEntity.None || a.MachineName == Environment.MachineName && a.ApplicationName == Schema.Current.ApplicationName)).ToList(),
                    new InvalidateWith(typeof(ScheduledTaskEntity)));

                ScheduledTasksLazy.OnReset += ScheduledTasksLazy_OnReset;

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            }
        }

        public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEntity parameters)
        {
            Database.Query<ScheduledTaskLogEntity>().Where(a => a.StartTime < parameters.DateLimit).UnsafeDeleteChunks(parameters.ChunkSize, parameters.MaxChunks);
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
                throw new InvalidOperationException("SchedulerLogic is already Running in {0}".FormatWith(Environment.MachineName));

            running = true;

            ReloadPlan();
        }

        public static void StopScheduledTasks()
        {
            if (!running)
                throw new InvalidOperationException("SchedulerLogic is already Stopped in {0}".FormatWith(Environment.MachineName));

            lock (priorityQueue)
            {
                if (!running)
                    return;

                running = false;

                timer.Change(Timeout.Infinite, Timeout.Infinite);
                priorityQueue.Clear();
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
                    DateTime now = TimeZoneManager.Now;
                    priorityQueue.Clear();
                    priorityQueue.PushAll(ScheduledTasksLazy.Value.Select(st => new ScheduledTaskPair
                    {
                        ScheduledTask = st,
                        NextDate = st.Rule.Next(now),
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
                            throw new InvalidOperationException("Inconstency in SchedulerLogic PriorityQueue");

                        DateTime now = TimeZoneManager.Now;

                        while (priorityQueue.Peek().NextDate < now)
                        {
                            var pair = priorityQueue.Pop();

                            ExecuteAsync(pair.ScheduledTask.Task, pair.ScheduledTask, null);

                            pair.NextDate = pair.ScheduledTask.Rule.Next(now);

                            priorityQueue.Push(pair);
                        }

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

        public static void ExecuteAsync(ITaskEntity task, ScheduledTaskEntity scheduledTask, IUserEntity user)
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

        public static Lite<IEntity> ExecuteSync(ITaskEntity task, ScheduledTaskEntity scheduledTask, IUserEntity user)
        {
            using (AuthLogic.UserSession(AuthLogic.SystemUser))
            using (Disposable.Combine(ApplySession, f => f(task)))
            {
                ScheduledTaskLogEntity stl = new ScheduledTaskLogEntity
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
        public Lite<ScheduledTaskEntity> ScheduledTask;
        public string Rule; 
        public DateTime NextExecution; 
    }
}
