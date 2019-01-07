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
using Signum.Engine.Authorization;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Entities.Isolation;
using System.Collections.Concurrent;

namespace Signum.Engine.Scheduler
{
    public static class SchedulerLogic
    {
        public static Action<ScheduledTaskLogEntity> OnFinally;
        
        static Expression<Func<ITaskEntity, IQueryable<ScheduledTaskLogEntity>>> ExecutionsExpression =
         ct => Database.Query<ScheduledTaskLogEntity>().Where(a => a.Task == ct);
        [ExpressionField]
        public static IQueryable<ScheduledTaskLogEntity> Executions(this ITaskEntity e)
        {
            return ExecutionsExpression.Evaluate(e);
        }

        static Expression<Func<ITaskEntity, ScheduledTaskLogEntity>> LastExecutionExpression =
            e => e.Executions().OrderByDescending(a => a.StartTime).FirstOrDefault();
        [ExpressionField]
        public static ScheduledTaskLogEntity LastExecution(this ITaskEntity e)
        {
            return LastExecutionExpression.Evaluate(e);
        }

        static Expression<Func<ScheduledTaskEntity, IQueryable<ScheduledTaskLogEntity>>> ExecutionsSTExpression =
            ct => Database.Query<ScheduledTaskLogEntity>().Where(a => a.ScheduledTask == ct);
        [ExpressionField]
        public static IQueryable<ScheduledTaskLogEntity> Executions(this ScheduledTaskEntity e)
        {
            return ExecutionsSTExpression.Evaluate(e);
        }


        static Expression<Func<ScheduledTaskLogEntity, IQueryable<SchedulerTaskExceptionLineEntity>>> ExceptionLinesExpression =
        e => Database.Query<SchedulerTaskExceptionLineEntity>().Where(a => a.SchedulerTaskLog.Is(e));
        [ExpressionField]
        public static IQueryable<SchedulerTaskExceptionLineEntity> ExceptionLines(this ScheduledTaskLogEntity e)
        {
            return ExceptionLinesExpression.Evaluate(e);
        }

        public static Polymorphic<Func<ITaskEntity, ScheduledTaskContext, Lite<IEntity>>> ExecuteTask = new Polymorphic<Func<ITaskEntity, ScheduledTaskContext, Lite<IEntity>>>();

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

        public static ConcurrentDictionary<ScheduledTaskLogEntity, ScheduledTaskContext> RunningTasks = new ConcurrentDictionary<ScheduledTaskLogEntity, ScheduledTaskContext>();


        public static void Start(SchemaBuilder sb)
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

                ExecuteTask.Register((ITaskEntity t, ScheduledTaskContext ctx) => { throw new NotImplementedException("SchedulerLogic.ExecuteTask not registered for {0}".FormatWith(t.GetType().Name)); });

                SimpleTaskLogic.Start(sb);
                sb.Include<ScheduledTaskEntity>()
                    .WithQuery(() => st => new
                    {
                        Entity = st,
                        st.Id,
                        st.Task,
                        st.Rule,
                        st.Suspended,
                        st.MachineName,
                        st.ApplicationName
                    });

                sb.Include<ScheduledTaskLogEntity>()
                    .WithQuery(() => cte => new
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

                sb.Include<SchedulerTaskExceptionLineEntity>()
                    .WithQuery(() => cte => new
                    {
                        Entity = cte,
                        cte.Id,
                        cte.ElementInfo,
                        cte.Exception,
                        cte.SchedulerTaskLog,
                    });

                new Graph<ScheduledTaskLogEntity>.Execute(ScheduledTaskLogOperation.CancelRunningTask)
                {
                    CanExecute = e => RunningTasks.ContainsKey(e) ? null : SchedulerMessage.TaskIsNotRunning.NiceToString(),
                    Execute = (e, _) => { RunningTasks[e].CancellationTokenSource.Cancel(); },
                }.Register();

                sb.Include<HolidayCalendarEntity>()
                    .WithQuery(() => st => new
                    {
                        Entity = st,
                        st.Id,
                        st.Name,
                        Holidays = st.Holidays.Count,
                    });

                QueryLogic.Expressions.Register((ITaskEntity ct) => ct.Executions(), () => ITaskMessage.Executions.NiceToString());
                QueryLogic.Expressions.Register((ITaskEntity ct) => ct.LastExecution(), () => ITaskMessage.LastExecution.NiceToString());
                QueryLogic.Expressions.Register((ScheduledTaskEntity ct) => ct.Executions(), () => ITaskMessage.Executions.NiceToString());
                QueryLogic.Expressions.Register((ScheduledTaskLogEntity ct) => ct.ExceptionLines(), () => ITaskMessage.ExceptionLines.NiceToString());

                new Graph<HolidayCalendarEntity>.Execute(HolidayCalendarOperation.Save)
                {
                    CanBeNew = true,
                    CanBeModified = true,
                    Execute = (c, _) => { },
                }.Register();

                new Graph<HolidayCalendarEntity>.Delete(HolidayCalendarOperation.Delete)
                {
                    Delete = (c, _) => { c.Delete(); },
                }.Register();

                new Graph<ScheduledTaskEntity>.Execute(ScheduledTaskOperation.Save)
                {
                    CanBeNew = true,
                    CanBeModified = true,
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


                new Graph<ScheduledTaskLogEntity>.ConstructFrom<ITaskEntity>(ITaskOperation.ExecuteSync)
                {
                    Construct = (task, _) => ExecuteSync(task, null, UserHolder.Current)
                }.Register();

                new Graph<ITaskEntity>.Execute(ITaskOperation.ExecuteAsync)
                {
                    Execute = (task, _) => ExecuteAsync(task, null, UserHolder.Current)
                }.Register();

                ScheduledTasksLazy = sb.GlobalLazy(() =>
                    Database.Query<ScheduledTaskEntity>().Where(a => !a.Suspended &&
                        (a.MachineName == ScheduledTaskEntity.None || a.MachineName == Environment.MachineName && a.ApplicationName == Schema.Current.ApplicationName)).ToList(),
                    new InvalidateWith(typeof(ScheduledTaskEntity)));

                ScheduledTasksLazy.OnReset += ScheduledTasksLazy_OnReset;

                sb.Schema.EntityEvents<ScheduledTaskLogEntity>().PreUnsafeDelete += query =>
                {
                    query.SelectMany(e => e.ExceptionLines()).UnsafeDelete();
                    return null;
                };

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            }
        }

        public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
        {
            var dateLimit = parameters.GetDateLimitDelete(typeof(ScheduledTaskLogEntity).ToTypeEntity());

            if (dateLimit == null)
                return;

            Database.Query<ScheduledTaskLogEntity>().Where(a => a.StartTime < dateLimit.Value).UnsafeDeleteChunksLog(parameters, sb, token);
        }

        static void ScheduledTasksLazy_OnReset(object sender, EventArgs e)
        {
            if (running)
                using (ExecutionContext.SuppressFlow())
                    Task.Run(() => { Thread.Sleep(1000); ReloadPlan(); });
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

            SystemEventLogLogic.Log("Start ScheduledTasks");
        }

        public static void StartScheduledTaskAfter(int initialDelayMilliseconds)
        {
            using (ExecutionContext.SuppressFlow())
                Task.Run(() =>
                {
                    Thread.Sleep(initialDelayMilliseconds);
                    StartScheduledTasks();
                });
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

            SystemEventLogLogic.Log("Stop ScheduledTasks");
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
                    var lastExecutions = Database.Query<ScheduledTaskLogEntity>().Where(a=>a.ScheduledTask != null).GroupBy(a => a.ScheduledTask).Select(gr => KVP.Create(
                        gr.Key,
                        gr.Max(a => a.StartTime)
                    )).ToDictionary();

                    priorityQueue.Clear();
                    priorityQueue.PushAll(ScheduledTasksLazy.Value.Select(st => {

                        var previous = lastExecutions.TryGetS(st);

                        var next = previous == null ?
                            st.Rule.Next(st.Rule.StartingOn) :
                            st.Rule.Next(previous.Value.Add(SchedulerMargin));

                        bool isMiss = next < now;

                        return new ScheduledTaskPair
                        {
                            ScheduledTask = st,
                            NextDate = isMiss ? now : next,
                        };
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

                            ExecuteAsync(pair.ScheduledTask.Task, pair.ScheduledTask, pair.ScheduledTask.User.Retrieve());

                            pair.NextDate = pair.ScheduledTask.Rule.Next(now);

                            priorityQueue.Push(pair);
                        }

                        SetTimer();

                        return;
                    }
            }
            catch (ThreadAbortException)
            {

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
            using (ExecutionContext.SuppressFlow())
                Task.Run(() =>
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

        public static ScheduledTaskLogEntity ExecuteSync(ITaskEntity task, ScheduledTaskEntity scheduledTask, IUserEntity user)
        {
            IUserEntity entityIUser = user ?? (IUserEntity)scheduledTask.User.Retrieve();

            var isolation = entityIUser.TryIsolation();
            if (isolation == null)
            {
                var ientity = task as IEntity;
                isolation = ientity?.TryIsolation();
            }

            using (IsolationEntity.Override(isolation))
            {
                ScheduledTaskLogEntity stl = new ScheduledTaskLogEntity
                {
                    Task = task,
                    ScheduledTask = scheduledTask,
                    StartTime = TimeZoneManager.Now,
                    MachineName = Environment.MachineName,
                    ApplicationName = Schema.Current.ApplicationName,
                    User = entityIUser.ToLite(),
                };

                using (AuthLogic.Disable())
                {
                    using (Transaction tr = Transaction.ForceNew())
                    {
                        stl.Save();

                        tr.Commit();
                    }
                }

                try
                {
                    var ctx = new ScheduledTaskContext { Log = stl };
                    RunningTasks.TryAdd(stl, ctx);

                    using (UserHolder.UserSession(entityIUser))
                    {
                        using (Transaction tr = Transaction.ForceNew())
                        {
                            stl.ProductEntity = ExecuteTask.Invoke(task, ctx);

                            using (AuthLogic.Disable())
                            {
                                stl.EndTime = TimeZoneManager.Now;
                                stl.Remarks = ctx.StringBuilder.ToString();
                                stl.Save();
                            }

                            tr.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    using (AuthLogic.Disable())
                    {
                        if (Transaction.InTestTransaction)
                            throw;

                        var exLog = ex.LogException().ToLite();

                        using (Transaction tr = Transaction.ForceNew())
                        {
                            stl.Exception = exLog;
                            stl.EndTime = TimeZoneManager.Now;
                            stl.Save();

                            tr.Commit();
                        }
                    }
                    throw;

                }
                finally
                {
                    RunningTasks.TryRemove(stl, out var ctx);
                    OnFinally?.Invoke(stl);
                }

                return stl;
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
                    NextDate = p.NextDate,
                }).ToList(),

                RunningTask = RunningTasks.OrderBy(a => a.Key.StartTime).Select(p => new SchedulerRunningTaskState
                {
                    SchedulerTaskLog = p.Key.ToLite(),
                    StartTime = p.Key.StartTime,
                    Remarks = p.Value.StringBuilder.ToString()
                }).ToList()
            };
        }

        public static void StopRunningTasks()
        {
            foreach (var item in RunningTasks.Values)
            {
                item.CancellationTokenSource.Cancel();
            }
        }
    }

    public class SchedulerState
    {
        public bool Running;
        public TimeSpan SchedulerMargin;
        public DateTime? NextExecution;
        public List<SchedulerItemState> Queue;

        public List<SchedulerRunningTaskState> RunningTask; 
    }

    public class SchedulerItemState
    {
        public Lite<ScheduledTaskEntity> ScheduledTask;
        public string Rule;
        public DateTime NextDate;
    }

    public class SchedulerRunningTaskState
    {
        public Lite<ScheduledTaskLogEntity> SchedulerTaskLog;
        public DateTime StartTime;
        public string Remarks;
    }

    public class ScheduledTaskContext
    {
        public ScheduledTaskLogEntity Log { internal get; set; }

        public StringBuilder StringBuilder { get; } = new StringBuilder();
        internal CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        public CancellationToken CancellationToken => CancellationTokenSource.Token;
        
        public void Foreach<T>(IEnumerable<T> collection, Func<T, string> elementID, Action<T> action)
        {
            foreach (var item in collection)
            {
                this.CancellationToken.ThrowIfCancellationRequested();

                using (HeavyProfiler.Log("Foreach", () => elementID(item)))
                {
                    try
                    {
                        using (Transaction tr = Transaction.ForceNew())
                        {
                            action(item);
                            tr.Commit();
                        }
                    }
                    catch(OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        SafeConsole.WriteLineColor(ConsoleColor.Red, "{0:u} Error in {1}: {2}", DateTime.Now, elementID(item), e.Message);
                        SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.StackTrace.Indent(4));

                        var ex = e.LogException();
                        using (ExecutionMode.Global())
                        using (Transaction tr = Transaction.ForceNew())
                        {
                            new SchedulerTaskExceptionLineEntity
                            {
                                Exception = ex.ToLite(),
                                SchedulerTaskLog = this.Log.ToLite(),
                                ElementInfo = elementID(item)
                            }.Save();

                            tr.Commit();
                        }
                    }
                }
            }
        }


        public void ForeachWriting<T>(IEnumerable<T> collection, Func<T, string> elementID, Action<T> action)
        {
            this.Foreach(collection, elementID, e =>
            {
                this.StringBuilder.AppendLine(elementID(e));
                action(e);
            });
        }
    }
}
