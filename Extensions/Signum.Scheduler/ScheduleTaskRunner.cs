using Microsoft.Extensions.Diagnostics.HealthChecks;
using Signum.Authorization;
using Signum.Basics;
using Signum.Utilities.DataStructures;
using System.Collections.Concurrent;

namespace Signum.Scheduler;

public static class ScheduleTaskRunner
{

    static PriorityQueue<ScheduledTaskPair> priorityQueue = new PriorityQueue<ScheduledTaskPair>((a, b) => a.NextDate.CompareTo(b.NextDate));

    static Timer timer = new Timer(new TimerCallback(TimerCallback), // main timer
                            null,
                            Timeout.Infinite,
                            Timeout.Infinite);

    public static ConcurrentDictionary<ScheduledTaskLogEntity, ScheduledTaskContext> RunningTasks = new ConcurrentDictionary<ScheduledTaskLogEntity, ScheduledTaskContext>();

    static bool running = false;
    public static bool Running
    {
        get { return running; }
    }


    internal static void ScheduledTasksLazy_OnReset(object? sender, EventArgs e)
    {
        if (running)
            using (ExecutionContext.SuppressFlow())
                Task.Run(() => { Thread.Sleep(1000); ReloadPlan(); });
    }

    public static HealthCheckResult GetHealthStatus()
    {
        return running ? new HealthCheckResult(HealthStatus.Healthy, "Running") :
          InitialDelayMilliseconds == null ? new HealthCheckResult(HealthStatus.Healthy, "Disabled") :
          new HealthCheckResult(HealthStatus.Unhealthy, "Not Running!");
    }

    public static SchedulerState GetSchedulerState()
    {
        return new SchedulerState
        {
            Running = Running,
            InitialDelayMilliseconds = InitialDelayMilliseconds,
            SchedulerMargin = SchedulerMargin,
            NextExecution = NextExecution,
            MachineName = Schema.Current.MachineName,
            ApplicationName = Schema.Current.ApplicationName,
            Queue = priorityQueue.GetOrderedList().Select(p => new SchedulerItemState
            {
                ScheduledTask = p.ScheduledTask.ToLite(),
                Rule = p.ScheduledTask.Rule.ToString()!,
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

    public static int? InitialDelayMilliseconds = null;


    public static void StartScheduledTasks()
    {
        if (running)
            throw new InvalidOperationException("SchedulerLogic is already Running in {0}".FormatWith(Schema.Current.MachineName));

        running = true;

        ReloadPlan();

        SystemEventLogLogic.Log("Start ScheduledTasks");
    }

    public static void StartScheduledTaskAfter(int initialDelayMilliseconds)
    {
        InitialDelayMilliseconds = initialDelayMilliseconds;
        Task.Run(() =>
        {
            Thread.Sleep(initialDelayMilliseconds);
            StartScheduledTasks();
        });
    }

    public static void StopScheduledTasks()
    {
        if (!running)
            throw new InvalidOperationException("SchedulerLogic is already Stopped in {0}".FormatWith(Schema.Current.MachineName));

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
                DateTime now = Clock.Now;
                var lastExecutions = Database.Query<ScheduledTaskLogEntity>().Where(a => a.ScheduledTask != null).GroupBy(a => a.ScheduledTask!).Select(gr => KeyValuePair.Create(
                      gr.Key,
                      gr.Max(a => a.StartTime)
                  )).ToDictionary();

                priorityQueue.Clear();
                priorityQueue.PushAll(SchedulerLogic.ScheduledTasksLazy.Value.Select(st =>
                {

                    var previous = lastExecutions.TryGetS(st);

                    var next = previous == null ?
                        st.Rule.Next(st.Rule.StartingOn) :
                        st.Rule.Next(previous.Value.Add(SchedulerMargin));

                    bool isMiss = next < now;

                    return new ScheduledTaskPair
                    (
                        scheduledTask: st,
                        nextDate: isMiss ? now : next
                    );
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
            TimeSpan ts = NextExecution.Value - Clock.Now;

            if (ts < TimeSpan.Zero)
                ts = TimeSpan.Zero; // cannot be negative !
            if (ts.TotalMilliseconds > int.MaxValue)
                ts = TimeSpan.FromMilliseconds(int.MaxValue);

            timer.Change(ts.Add(SchedulerMargin), new TimeSpan(-1)); // invoke after the timespan
        }
    }

    static void TimerCallback(object? obj) // obj ignored
    {
        try
        {
            using (new EntityCache(EntityCacheType.ForceNew))
            using (AuthLogic.Disable())
                lock (priorityQueue)
                {
                    if (priorityQueue.Empty)
                        throw new InvalidOperationException("Inconstency in SchedulerLogic PriorityQueue");

                    DateTime now = Clock.Now;

                    while (priorityQueue.Peek().NextDate < now)
                    {
                        var pair = priorityQueue.Pop();

                        ExecuteAsync(pair.ScheduledTask.Task, pair.ScheduledTask, pair.ScheduledTask.User.RetrieveAndRemember());

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

    public static void ExecuteAsync(ITaskEntity task, ScheduledTaskEntity? scheduledTask, IUserEntity user)
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

    public static event Func<ITaskEntity, ScheduledTaskEntity?, IUserEntity, IDisposable>? SurroundExecuteTask;

    public static ScheduledTaskLogEntity ExecuteSync(ITaskEntity task, ScheduledTaskEntity? scheduledTask, IUserEntity user)
    {
        using (ExecutionMode.SetIsolation((Entity)user) ?? ExecutionMode.SetIsolation((Entity)task))
        using (Disposable.Combine(SurroundExecuteTask, func => func(task, scheduledTask, user)))
        {
            ScheduledTaskLogEntity stl = new ScheduledTaskLogEntity
            {
                Task = task,
                ScheduledTask = scheduledTask,
                StartTime = Clock.Now,
                MachineName = Schema.Current.MachineName,
                ApplicationName = Schema.Current.ApplicationName,
                User = user.ToLite(),
            };

            using (AuthLogic.Disable())
            {
                using (var tr = Transaction.ForceNew())
                {
                    stl.Save();

                    tr.Commit();
                }
            }

            var ctx = new ScheduledTaskContext(stl);
            try
            {
                RunningTasks.TryAdd(stl, ctx);

                using (UserHolder.UserSession(user))
                {
                    using (var tr = Transaction.ForceNew())
                    {
                        stl.ProductEntity = SchedulerLogic.ExecuteTask.Invoke(task, ctx);
                        tr.Commit();
                    }


                    using (var tr = Transaction.ForceNew())
                    {
                        using (AuthLogic.Disable())
                        {
                            stl.EndTime = Clock.Now;
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

                    using (var tr = Transaction.ForceNew())
                    {
                        stl.Exception = exLog;
                        stl.EndTime = Clock.Now;
                        stl.Remarks = ctx.StringBuilder.ToString();
                        stl.Save();

                        tr.Commit();
                    }
                }
                throw;

            }
            finally
            {
                RunningTasks.TryRemove(stl, out ctx);
                SchedulerLogic.OnFinally?.Invoke(stl);
            }

            return stl;
        }
    }

   

    public static void StopRunningTasks()
    {
        foreach (var item in RunningTasks.Values)
        {
            item.CancellationTokenSource.Cancel();
        }
    }
}

public class ScheduledTaskPair
{
    public ScheduledTaskEntity ScheduledTask;
    public DateTime NextDate;

    public ScheduledTaskPair(ScheduledTaskEntity scheduledTask, DateTime nextDate)
    {
        ScheduledTask = scheduledTask;
        NextDate = nextDate;
    }
}



public class SchedulerState
{
    public required bool Running;
    public required int? InitialDelayMilliseconds;
    public required TimeSpan SchedulerMargin;
    public required DateTime? NextExecution;
    public required List<SchedulerItemState> Queue;
    public required string MachineName;
    public required string ApplicationName;

    public required List<SchedulerRunningTaskState> RunningTask;
}

public class SchedulerItemState
{
    public required Lite<ScheduledTaskEntity> ScheduledTask;
    public required string Rule;
    public required DateTime NextDate;
}

public class SchedulerRunningTaskState
{
    public required Lite<ScheduledTaskLogEntity> SchedulerTaskLog;
    public required DateTime StartTime;
    public required string Remarks;
}

public class ScheduledTaskContext
{
    public ScheduledTaskContext(ScheduledTaskLogEntity log)
    {
        Log = log;
    }

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
                    using (var tr = Transaction.ForceNew())
                    {
                        action(item);
                        tr.Commit();
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    SafeConsole.WriteLineColor(ConsoleColor.Red, "{0:u} Error in {1}: {2}", DateTime.Now, elementID(item), e.Message);
                    SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.StackTrace!.Indent(4));

                    var ex = e.LogException();
                    using (ExecutionMode.Global())
                    using (var tr = Transaction.ForceNew())
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
            try
            {
                this.StringBuilder.AppendLine(elementID(e));
                action(e);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                this.StringBuilder.AppendLine("   Error: " + ex.Message);
                throw;
            }
        });
    }
}

