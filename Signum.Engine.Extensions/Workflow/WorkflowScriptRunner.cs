using Signum.Engine.Authorization;
using Signum.Engine.Cache;
using Signum.Entities.Workflow;
using Signum.Entities.Basics;
using Signum.Engine.Scheduler;
using Microsoft.Data.SqlClient;

namespace Signum.Engine.Workflow;

public static class WorkflowScriptRunner
{
    static Timer timer = null!;
    internal static DateTime? nextPlannedExecution;
    static bool running = false;
    static CancellationTokenSource CancelProcess = null!;
    static long queuedItems;
    static Guid processIdentifier;
    static AutoResetEvent autoResetEvent = new AutoResetEvent(false);
    static int? initialDelayMilliseconds;

    public static WorkflowScriptRunnerState ExecutionState()
    {
        return new WorkflowScriptRunnerState
        {
            Running = running,
            InitialDelayMilliseconds = initialDelayMilliseconds,
            CurrentProcessIdentifier = processIdentifier,
            ScriptRunnerPeriod = WorkflowLogic.Configuration.ScriptRunnerPeriod,
            NextPlannedExecution = nextPlannedExecution,
            IsCancelationRequested = CancelProcess != null && CancelProcess.IsCancellationRequested,
            QueuedItems = queuedItems,
        };
    }

    public static void StartRunningScriptsAfter(int initialDelayMilliseconds)
    {
        WorkflowScriptRunner.initialDelayMilliseconds = initialDelayMilliseconds;
        using (ExecutionContext.SuppressFlow())
            Task.Run(() =>
            {
                Thread.Sleep(initialDelayMilliseconds);
                StartRunningScripts();
            });
    }

    public static SimpleStatus GetSimpleStatus()
    {
        return running ? SimpleStatus.Ok :
            initialDelayMilliseconds == null ? SimpleStatus.Disabled :
            SimpleStatus.Error;
    }

    public static void StartRunningScripts()
    {
        if (running)
            throw new InvalidOperationException("WorkflowScriptRunner process is already running");


        using (ExecutionContext.SuppressFlow())
        {
            Task.Factory.StartNew(() =>
            {
                SystemEventLogLogic.Log("Start WorkflowScriptRunner");
                ExceptionEntity? exception = null;
                try
                {
                    running = true;
                    CancelProcess = new CancellationTokenSource();
                    autoResetEvent.Set();

                    timer = new Timer(ob => WakeUp("TimerNextExecution", null),
                         null,
                         Timeout.Infinite,
                         Timeout.Infinite);

                    GC.KeepAlive(timer);

                    using (UserHolder.UserSession(AuthLogic.SystemUser!))
                    {
                        if (CacheLogic.WithSqlDependency)
                        {
                            SetSqlDependency();
                        }

                        while (autoResetEvent.WaitOne())
                        {
                            if (CancelProcess.IsCancellationRequested)
                                return;

                            timer.Change(Timeout.Infinite, Timeout.Infinite);
                            nextPlannedExecution = null;

                            using (HeavyProfiler.Log("WorkflowScriptRunner", () => "Execute process"))
                            {
                                var queryCaseActivities = Database.Query<CaseActivityEntity>()
                                                              .Where(m => !m.Workflow().HasExpired() &&
                                                                     m.DoneDate == null &&
                                                                     m.ScriptExecution!.ProcessIdentifier == processIdentifier);
                                processIdentifier = Guid.NewGuid();
                                if (RecruitQueuedItems())
                                {
                                    while (queuedItems > 0 || RecruitQueuedItems())
                                    {
                                        var items = queryCaseActivities.Take(WorkflowLogic.Configuration.ChunkSizeRunningScripts).ToList();
                                        queuedItems = items.Count;
                                        foreach (var caseActivity in items)
                                        {
                                            CancelProcess.Token.ThrowIfCancellationRequested();

                                            try
                                            {
                                                using (var tr = Transaction.ForceNew())
                                                {
                                                    caseActivity.Execute(CaseActivityOperation.ScriptExecute);

                                                    tr.Commit();
                                                }
                                            }
                                            catch
                                            { 
                                                try
                                                {
                                                    var ca = caseActivity.ToLite().RetrieveAndRemember();
                                                    var retry = ((WorkflowActivityEntity)ca.WorkflowActivity).Script!.RetryStrategy;
                                                    var nextDate = retry?.NextDate(ca.ScriptExecution!.RetryCount);
                                                    if(nextDate == null)
                                                    {
                                                        ca.Execute(CaseActivityOperation.ScriptFailureJump);
                                                    }
                                                    else
                                                    {
                                                        ca.Execute(CaseActivityOperation.ScriptScheduleRetry, nextDate.Value);
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    e.LogException();
                                                    throw;
                                                }
                                            }
                                            queuedItems--;
                                        }
                                        queuedItems = queryCaseActivities.Count();
                                    }
                                }
                                SetTimer();

                                if (CacheLogic.WithSqlDependency)
                                    SetSqlDependency();
                            }
                        }
                    }
                }
                catch (ThreadAbortException)
                {

                }
                catch (Exception e)
                {
                    try
                    {
                        exception = e.LogException(edn =>
                        {
                            edn.ControllerName = "WorkflowScriptRunner";
                            edn.ActionName = "ExecuteProcess";
                        });
                    }
                    catch { }
                }
                finally
                {
                    SystemEventLogLogic.Log("Stop WorkflowScriptRunner", exception);
                    running = false;
                }
            }, TaskCreationOptions.LongRunning);
        }
    }

    static bool sqlDependencyRegistered = false;


    private static void SetSqlDependency()
    {
        if (sqlDependencyRegistered)
            return;
        
        var query = Database.Query<CaseActivityEntity>().Where(m => m.DoneDate == null && m.ScriptExecution != null).Select(m => m.Id);
        sqlDependencyRegistered = true;
        query.ToListWithInvalidation(typeof(CaseActivityEntity), "WorkflowScriptRunner ReadyToExecute dependency", a => {
            sqlDependencyRegistered = false;
            WakeUp("WorkflowScriptRunner ReadyToExecute dependency", a);
        });
    }

    private static bool RecruitQueuedItems()
    {
        DateTime? firstDate = WorkflowLogic.Configuration.AvoidExecutingScriptsOlderThan == null ?
            null : (DateTime?)Clock.Now.AddHours(-WorkflowLogic.Configuration.AvoidExecutingScriptsOlderThan.Value);

        queuedItems = Database.Query<CaseActivityEntity>()
            .Where(ca => !ca.Workflow().HasExpired() &&
                         ca.DoneDate == null &&
                         ((firstDate == null || firstDate < ca.ScriptExecution!.NextExecution) &&
                         ca.ScriptExecution!.NextExecution < Clock.Now))
            .UnsafeUpdate()
            .Set(m => m.ScriptExecution!.ProcessIdentifier, m => processIdentifier)
            .Execute();

        return queuedItems > 0;
    }

    public static void WakeupOnCommit()
    {
        Transaction.PostRealCommit -= Transaction_PostRealCommit;
        Transaction.PostRealCommit += Transaction_PostRealCommit;
    }

    private static void Transaction_PostRealCommit(System.Collections.Generic.Dictionary<string, object> obj)
    {
        WakeUp("Save Transaction Commit", null);
    }

    internal static bool WakeUp(string reason, SqlNotificationEventArgs? args)
    {
        using (HeavyProfiler.Log("WorkflowScriptRunner WakeUp", () => "WakeUp! " + reason + ToString(args)))
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

    private static void SetTimer()
    {
        nextPlannedExecution = Clock.Now.AddMilliseconds(WorkflowLogic.Configuration.ScriptRunnerPeriod * 1000);
        timer.Change(WorkflowLogic.Configuration.ScriptRunnerPeriod * 1000, Timeout.Infinite);
    }

    public static void Stop()
    {
        if (!running)
            throw new InvalidOperationException("WorkflowScriptRunner is not running");

        using (HeavyProfiler.Log("WorkflowScriptRunner", () => "Stopping process"))
        {
            timer.Dispose();
            CancelProcess.Cancel();
            WakeUp("Stop", null);
            nextPlannedExecution = null;
        }
    }
}

public class WorkflowScriptRunnerState
{
    public int ScriptRunnerPeriod;
    public int? InitialDelayMilliseconds;
    public bool Running;
    public bool IsCancelationRequested;
    public DateTime? NextPlannedExecution;
    public long QueuedItems;
    public Guid CurrentProcessIdentifier;

}
