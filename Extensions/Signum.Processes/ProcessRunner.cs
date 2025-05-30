using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Signum.Authorization;
using Signum.Cache;
using System.Threading.Channels;

namespace Signum.Processes;

public static class ProcessRunner
{
    public static Action<ExecutingProcess>? OnFinally;

    static Dictionary<Lite<ProcessEntity>, ExecutingProcess> executing = new Dictionary<Lite<ProcessEntity>, ExecutingProcess>();

    static Timer timerNextExecution = null!;
    static Timer timerPeriodic = null!;
    public static int PoolingPeriodMilliseconds = 30 * 1000;

    internal static DateTime? nextPlannedExecution;

    public static int MaxDegreeOfParallelism = 2;

    static bool running = false;

    static int? initialDelayMilliseconds;

    static CancellationTokenSource CancelNewProcesses = null!;

    static AutoResetEvent autoResetEvent = new AutoResetEvent(false);

    public static StringBuilder? LogStringBuilder;

    public static ProcessLogicState ExecutionState()
    {
        string? log = null;
        if (LogStringBuilder != null)
            lock (LogStringBuilder)
                log = LogStringBuilder?.ToString();

        return new ProcessLogicState
        {
            Running = running,
            InitialDelayMilliseconds = initialDelayMilliseconds,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            NextPlannedExecution = nextPlannedExecution,
            JustMyProcesses = ProcessLogic.JustMyProcesses,
            MachineName = Schema.Current.MachineName,
            ApplicationName = Schema.Current.ApplicationName,
            Log = log,
            Executing = executing.Values.Select(p => new ExecutionState
            {
                IsCancellationRequested = p.CancelationSource.IsCancellationRequested,
                Process = p.CurrentProcess.ToLite(),
                State = p.CurrentProcess.State,
                Progress = p.CurrentProcess.Progress,
                MachineName = p.CurrentProcess.MachineName,
                ApplicationName = p.CurrentProcess.ApplicationName,
            }).ToList()
        };
    }

    public static void Log(string line, ExecutingProcess? ep = null)
    {
        if (LogStringBuilder != null)
            lock (LogStringBuilder)
                LogStringBuilder.AppendLine(DateTime.Now.ToString("o") + (ep == null ? null : " " + ep.ToString()) + " " + line);

    }

    public static HealthCheckResult GetHealthStatus()
    {
        return running ? new HealthCheckResult(HealthStatus.Healthy, "Running") :
            initialDelayMilliseconds == null ? new HealthCheckResult(HealthStatus.Healthy, "Disabled") :
            new HealthCheckResult(HealthStatus.Unhealthy, "Not Running!");
    }

    public static void StartRunningProcessesAfter(int delayMilliseconds)
    {
        Log("Starting after " + delayMilliseconds);
        initialDelayMilliseconds = delayMilliseconds;

        Task.Run(() =>
        {
            Thread.Sleep(delayMilliseconds);
            StartRunningProcesses();
        });
    }

    static int SetAsQueued(this IQueryable<ProcessEntity> query)
    {
        return query.UnsafeUpdate()
        .Set(p => p.State, p => ProcessState.Queued)
        .Set(p => p.QueuedDate, p => Clock.Now)
        .Set(p => p.ExecutionStart, p => null)
        .Set(p => p.ExecutionEnd, p => null)
        .Set(p => p.SuspendDate, p => null)
        .Set(p => p.Progress, p => null)
        .Set(p => p.Status, p => null)
        .Set(p => p.Exception, p => null)
        .Set(p => p.ExceptionDate, p => null)
        .Set(p => p.MachineName, p => ProcessLogic.JustMyProcesses ? Schema.Current.MachineName : ProcessEntity.None)
        .Set(p => p.ApplicationName, p => ProcessLogic.JustMyProcesses ? Schema.Current.ApplicationName : ProcessEntity.None)
        .Execute();
    }

    internal static void SetAsQueued(this ProcessEntity process)
    {
        process.State = ProcessState.Queued;
        process.QueuedDate = Clock.Now;
        process.ExecutionStart = null;
        process.ExecutionEnd = null;
        process.SuspendDate = null;
        process.Progress = null;
        process.Status = null;
        process.Exception = null;
        process.ExceptionDate = null;
        process.MachineName = ProcessLogic.JustMyProcesses ? Schema.Current.MachineName : ProcessEntity.None;
        process.ApplicationName = ProcessLogic.JustMyProcesses ? Schema.Current.ApplicationName : ProcessEntity.None;
    }



    [AutoExpressionField]
    public static bool IsMine(this ProcessEntity p) => 
        As.Expression(() => p.MachineName == Schema.Current.MachineName && p.ApplicationName == Schema.Current.ApplicationName);

    [AutoExpressionField]
    public static bool IsShared(this ProcessEntity p) => 
        As.Expression(() => !ProcessLogic.JustMyProcesses && p.MachineName == ProcessEntity.None);

    internal static List<T> ToListWakeup<T>(this IQueryable<T> query, string action)
    {
        if (CacheLogic.WithSqlDependency)
            query.ToListWithInvalidation(typeof(ProcessEntity), action, a => WakeUp(action, a));

        return query.ToList();
    }

    public static void StartRunningProcesses()
    {
        Log("StartRunningProcesses");

        if (running)
            throw new InvalidOperationException("ProcessLogic is running");

        using (ExecutionContext.SuppressFlow())
            Task.Factory.StartNew(() =>
            {
                Log("StartRunningProcesses StartNew");

                var database = Schema.Current.Table(typeof(ProcessEntity)).Name.Schema?.Database;

                SystemEventLogLogic.Log("Start ProcessRunner");
                Exception? ex = null;
                ExceptionEntity? exception = null;
                using (AuthLogic.Disable())
                {
                    try
                    {
                        running = true;

                        int changed = (from p in Database.Query<ProcessEntity>()
                                       where p.IsMine() && (p.State == ProcessState.Executing || p.State == ProcessState.Suspending || p.State == ProcessState.Suspended) ||
                                       p.IsShared() && p.State == ProcessState.Suspended
                                       select p).SetAsQueued();

                        Log("SetAsQueued " + changed);

                        CancelNewProcesses = new CancellationTokenSource();

                        autoResetEvent.Set();

                        Log("AutoResetEvent Set");

                        timerNextExecution = new Timer(ob => WakeUp("TimerNextExecution", null), // main timer
                             null,
                             Timeout.Infinite,
                             Timeout.Infinite);

                        if (!CacheLogic.WithSqlDependency)
                            timerPeriodic = new Timer(ob => WakeUp("TimerPeriodic", null), null, PoolingPeriodMilliseconds, PoolingPeriodMilliseconds);

                        Log("Waiting ARE");
                        while (autoResetEvent.WaitOne())
                        {
                            Log("Loop");

                            if (CancelNewProcesses.IsCancellationRequested)
                            {
                                Log("CancelNewProcesses.IsCancellationRequested = true");
                                return;
                            }

                            using (HeavyProfiler.Log("PWL", () => "Process Runner"))
                            {
                                changed = (from p in Database.Query<ProcessEntity>()
                                           where p.State == ProcessState.Planned && p.PlannedDate <= Clock.Now
                                           select p).SetAsQueued();


                                Log("SetAsQueued " + changed);

                                var dates = Database.Query<ProcessEntity>()
                                        .Where(p => p.IsMine() || p.IsShared())
                                        .Where(p => p.State == ProcessState.Planned)
                                        .Select(p => p.PlannedDate)
                                        .ToListWakeup("Planned dependency");

                                Log("DatesList Count = " + dates.Count + " MinDate = " + dates.Min());

                                SetNextPannedExecution(dates.Min());

                                Log("Waiting Lock");
                                lock (executing)
                                {
                                    var suspending = Database.Query<ProcessEntity>()
                                         .Where(p => p.State == ProcessState.Suspending)
                                         .Where(p => p.IsMine())
                                         .Select(a => a.ToLite())
                                         .ToListWakeup("Suspending dependency");

                                    Log($"Suspending Count= {suspending.Count}");

                                    foreach (var s in suspending)
                                    {
                                        ExecutingProcess execProc = executing.GetOrThrow(s);

                                        if (execProc.CurrentProcess.State != ProcessState.Finished)
                                        {
                                            execProc.CurrentProcess = s.RetrieveAndRemember();
                                            execProc.CancelationSource.Cancel();
                                        }
                                    }

                                    Log("Inside Lock");

                                    int remaining = MaxDegreeOfParallelism - executing.Count;

                                    if (remaining > 0)
                                    {

                                    retry:
                                        var queued = Database.Query<ProcessEntity>()
                                            .Where(p => p.State == ProcessState.Queued)
                                            .Where(p => p.IsMine() || p.IsShared())
                                            .Select(a => new { Process = a.ToLite(), a.QueuedDate, a.MachineName })
                                            .ToListWakeup("Planned dependency");


                                        var afordable = queued
                                            .OrderByDescending(p => p.MachineName == Schema.Current.MachineName)
                                            .OrderBy(a => a.QueuedDate)
                                            .Take(remaining).ToList();

                                        Log($"Queued Count = {queued.Count} Affordable = {afordable.Count} Executing = {executing.Count}");

                                        var taken = afordable.Where(p => p.MachineName == ProcessEntity.None).Select(a => a.Process).ToList();

                                        if (taken.Any())
                                        {
                                            Log($"Taken Count = {taken.Count}");

                                            using (var tr = Transaction.ForceNew())
                                            {
                                                Database.Query<ProcessEntity>()
                                                    .Where(p => taken.Contains(p.ToLite()) && p.MachineName == ProcessEntity.None)
                                                    .UnsafeUpdate()
                                                    .Set(p => p.MachineName, p => Schema.Current.MachineName)
                                                    .Set(p => p.ApplicationName, p => Schema.Current.ApplicationName)
                                                    .Execute();

                                                tr.Commit();
                                            }


                                            goto retry;
                                        }


                                        foreach (var pair in afordable)
                                        {
                                            Log($" Foreach {pair}");

                                            ProcessEntity pro = pair.Process!.RetrieveAndRemember();

                                            IProcessAlgorithm algorithm = ProcessLogic.GetProcessAlgorithm(pro.Algorithm);

                                            ExecutingProcess executingProcess = new ExecutingProcess(algorithm, pro);
                                            Log($" Created", executingProcess);
                                            executing.Add(pro.ToLite(), executingProcess);

                                            Log($" TakeForThisMachine", executingProcess);
                                            executingProcess.TakeForThisMachine();

                                            using (ExecutionContext.SuppressFlow())
                                                Task.Run(() =>
                                                {
                                                    try
                                                    {
                                                        Log($" Task.Run / Execute", executingProcess);
                                                        using (ProcessLogic.OnApplySession(executingProcess.CurrentProcess))
                                                        {
                                                            executingProcess.Execute();
                                                        }

                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        try
                                                        {

                                                            Log($" Task.Run / ERROR " + ex.Message, executingProcess);
                                                            var exEnt = ex.LogException(edn =>
                                                            {
                                                                edn.ControllerName = "ProcessWorker";
                                                                edn.ActionName = executingProcess.CurrentProcess.ToLite().Key();
                                                            });
                                                            Log($" Task.Run / ERROR ExceptionID = " + exEnt.Id, executingProcess);

                                                        }
                                                        catch { }
                                                    }
                                                    finally
                                                    {
                                                        Log($"Task.Run / Finishing", executingProcess);
                                                        lock (executing)
                                                        {
                                                            executing.Remove(pro.ToLite());
                                                            WakeUp("Process ended", null);
                                                        }
                                                        Log($"Task.Run / Finished", executingProcess);
                                                    }
                                                });

                                        }
                                    }

                     
                                    
                                }
                            }

                            Log("Waiting ARE");
                        }
                    }
                    catch (ThreadAbortException tae)
                    {
                        Log("ThreadAbortException " + tae.Message);
                        //Ignore
                    }
                    catch (Exception e)
                    {
                        ex = e;
                        Log(e.GetType() + " " + e.Message);
                        try
                        {
                            exception = e.LogException(edn =>
                            {
                                edn.ControllerName = "ProcessWorker";
                                edn.ActionName = "MainLoop";
                            });
                        }
                        catch { }
                    }
                    finally
                    {
                        Log("finally Stopping");
                        lock (executing)
                            executing.Clear();

                        SystemEventLogLogic.Log("Stop ProcessRunner", exception);

                        running = false;
                    }
                }

                if (ex != null)
                {
                    using (ExecutionContext.SuppressFlow())
                        Task.Delay(1 * 60 * 1000).ContinueWith(t =>
                        {
                            StartRunningProcesses();
                        });
                }

            }, TaskCreationOptions.LongRunning);
    }

    internal static bool WakeUp(string reason, SqlNotificationEventArgs? args)
    {
        Log("WakeUp " + reason);
        using (HeavyProfiler.Log("WakeUp", () => "WakeUp! "+ reason + ToString(args)))
        {
            return autoResetEvent.Set();
        }
    }

    private static string? ToString(SqlNotificationEventArgs? args)
    {
        if (args == null)
            return null;

        return " ({0} {1} {2})".FormatWith(args.Type, args.Source, args.Info); 
    }

    private static void SetNextPannedExecution(DateTime? next)
    {
        nextPlannedExecution = next;

        if (next == null)
        {
            timerNextExecution.Change(Timeout.Infinite, Timeout.Infinite);
        }
        else
        {
            TimeSpan ts = next.Value - Clock.Now;
            if (ts < TimeSpan.Zero)
                ts = TimeSpan.Zero;
            else
                ts = ts.Add(TimeSpan.FromSeconds(2));

            timerNextExecution.Change((int)ts.TotalMilliseconds, Timeout.Infinite); // invoke after the timespan
        }

    }

    public static void Stop()
    {
        if (!running)
            throw new InvalidOperationException("ProcessLogic is not running");

        timerNextExecution.Dispose();
        if (timerPeriodic != null)
            timerPeriodic.Dispose();

        CancelNewProcesses.Cancel();

        WakeUp("Stop", null);

        foreach (var p in executing.Values)
        {
            p.CancelationSource.Cancel();
        }
    }

    public static bool IsExecutingInThisMachien(Lite<ProcessEntity> process)
    {
        return executing.ContainsKey(process);
    }
}


public sealed class ExecutingProcess
{
    public ProcessEntity CurrentProcess { get; internal set; }
    internal IProcessAlgorithm Algorithm;
    internal CancellationTokenSource CancelationSource;

    public bool WriteToConsole = false;

    public ExecutingProcess(IProcessAlgorithm processAlgorithm, ProcessEntity process)
    {
        this.CancelationSource = new CancellationTokenSource();
        this.Algorithm = processAlgorithm;
        this.CurrentProcess = process;
    }

    public IProcessDataEntity? Data
    {
        get { return CurrentProcess.Data; }
    }

    public CancellationToken CancellationToken
    {
        get { return CancelationSource.Token; }
    }

    public static int DecimalPlaces = 3;

    public void ProgressChanged(int position, int count, string? status = null)
    {
        if (position > count)
            throw new InvalidOperationException("Position ({0}) should not be greater thant count ({1}). Maybe the process is not making progress.".FormatWith(position, count));

        decimal progress = count == 0 ? 0 : Math.Round(((decimal)position) / count, DecimalPlaces);

        if (WriteToConsole)
            SafeConsole.WriteSameLine("{0:p} [{1}/{2}]".FormatWith(progress, position, count));

        ProgressChanged(progress, status);
    }

    public void ProgressChanged(decimal progress, string? status = null)
    {
        if (progress != CurrentProcess.Progress || status != CurrentProcess.Status)
        {
            CurrentProcess.Progress = progress;
            CurrentProcess.Status = status;
            var ic = CurrentProcess.FullIntegrityCheck();
            if (ic != null)
                throw new IntegrityCheckException(ic);

            CurrentProcess.InDB()
                .UnsafeUpdate()
                .Set(a => a.Progress, a => progress)
                .Set(a => a.Status, a => status)
                .Execute();
        }
    }

    public void WriteMessage(string? status)
    {
        if (status != CurrentProcess.Status)
        {
            CurrentProcess.Status = status;
            CurrentProcess.InDB()
                .UnsafeUpdate()
                .Set(a => a.Status, a => status)
                .Execute();
        }
    }


    public void TakeForThisMachine()
    {
        CurrentProcess.State = ProcessState.Executing;
        CurrentProcess.ExecutionStart = Clock.Now;
        CurrentProcess.ExecutionEnd = null;
        CurrentProcess.Progress = 0;
        CurrentProcess.MachineName = Schema.Current.MachineName;
        CurrentProcess.ApplicationName = Schema.Current.ApplicationName;

        using (var tr = new Transaction())
        {
            if (CurrentProcess.InDB().Any(a => a.State == ProcessState.Executing))
                throw new InvalidOperationException("The process {0} is allready Executing!".FormatWith(CurrentProcess.Id));
                     
            using (OperationLogic.AllowSave<ProcessEntity>())
                CurrentProcess.Save();

            tr.Commit();
        }
    }
    
    public void Execute()
    {
        var user = ExecutionMode.Global().Using(_ => CurrentProcess.User.RetrieveAndRemember());

        using (UserHolder.UserSession(user))
        {
            using (ExecutionMode.SetIsolation(CurrentProcess) ?? 
                (CurrentProcess.Data != null ? ExecutionMode.SetIsolation(CurrentProcess.Data) : null))
            {
                if (UserEntity.Current == null)
                    UserHolder.Current = new UserWithClaims(AuthLogic.SystemUser!);
                try
                {
                    Algorithm.Execute(this);

                    CurrentProcess.ExecutionEnd = Clock.Now;
                    CurrentProcess.State = ProcessState.Finished;
                    CurrentProcess.Progress = null;
                    CurrentProcess.User.ClearEntity();
                    using (OperationLogic.AllowSave<ProcessEntity>())
                        CurrentProcess.Save();
                }
                catch (OperationCanceledException e)
                {
                    if (!e.CancellationToken.Equals(this.CancellationToken))
                        throw;

                    CurrentProcess.SuspendDate = Clock.Now;
                    CurrentProcess.State = ProcessState.Suspended;
                    using (OperationLogic.AllowSave<ProcessEntity>())
                        CurrentProcess.Save();
                }
                catch (Exception e)
                {
                    if (Transaction.InTestTransaction)
                        throw;

                    CurrentProcess.State = ProcessState.Error;
                    CurrentProcess.ExceptionDate = Clock.Now;
                    CurrentProcess.Exception = e.LogException(el => el.ActionName = CurrentProcess.Algorithm.ToString()).ToLite();
                    using (OperationLogic.AllowSave<ProcessEntity>())
                        CurrentProcess.Save();
                }
                finally
                {
                    ProcessRunner.OnFinally?.Invoke(this);
                }
            }
        }
    }

    public override string ToString()
    {
        return "Execution (ID = {0}): {1} ".FormatWith(CurrentProcess.Id, CurrentProcess);
    }

   
}



public class ProcessLogicState
{
    public required int MaxDegreeOfParallelism;
    public required int? InitialDelayMilliseconds;
    public required string MachineName;
    public required string ApplicationName;
    public required bool Running;
    public required bool JustMyProcesses;
    public string? Log;
    public required DateTime? NextPlannedExecution;
    public required List<ExecutionState> Executing;
}

public class ExecutionState
{
    public required Lite<ProcessEntity> Process;
    public required ProcessState State;
    public required bool IsCancellationRequested;
    public required decimal? Progress;
    public required string MachineName;
    public required string ApplicationName; 
}



