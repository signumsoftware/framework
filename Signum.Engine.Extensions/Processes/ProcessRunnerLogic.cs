using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Cache;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Processes;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Processes
{
    public static class ProcessRunnerLogic
    {
        static Dictionary<Lite<ProcessDN>, ExecutingProcess> executing = new Dictionary<Lite<ProcessDN>, ExecutingProcess>();

        static Timer timer = new Timer(ob => autoResetEvent.Set(), // main timer
                   null,
                   Timeout.Infinite,
                   Timeout.Infinite);

        internal static DateTime? nextPlannedExecution;

        public static int MaxDegreeOfParallelism = 2;

        static bool running = false;

        static int initialDelayMiliseconds;

        static CancellationTokenSource CancelNewProcesses;

        static AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        public static ProcessLogicState ExecutionState()
        {
            return new ProcessLogicState
            {
                Running = running,
                InitialDelayMiliseconds = initialDelayMiliseconds,
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                NextPlannedExecution = nextPlannedExecution,
                Executing = executing.Values.Select(p => new ExecutionState
                {
                    IsCancellationRequested = p.CancelationSource.IsCancellationRequested,
                    Process = p.CurrentExecution.ToLite(),
                    State = p.CurrentExecution.State,
                    Progress = p.CurrentExecution.Progress,
                    MachineName = p.CurrentExecution.MachineName,
                }).ToList()
            };
        }

        public static void StartRunningProcesses(int delayMilliseconds)
        {
            initialDelayMiliseconds = delayMilliseconds;

            if (initialDelayMiliseconds == 0)
                StartRunningProcesses();

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(initialDelayMiliseconds);
                StartRunningProcesses();
            });
        }

        static int SetAsQueued(this IQueryable<ProcessDN> query)
        {
            return query.UnsafeUpdate(p => new ProcessDN
            {
                State = ProcessState.Queued,
                QueuedDate = TimeZoneManager.Now,
                ExecutionStart = null,
                ExecutionEnd = null,
                SuspendDate = null,
                Progress = null,
                Exception = null,
                ExceptionDate = null,
                MachineName = ProcessLogic.ExecuteInMyMachine ? Environment.MachineName : ProcessDN.None
            });
        }

        internal static void SetAsQueued(this ProcessDN process)
        {
            process.State = ProcessState.Queued;
            process.QueuedDate = TimeZoneManager.Now;
            process.ExecutionStart = null;
            process.ExecutionEnd = null;
            process.SuspendDate = null;
            process.Progress = null;
            process.Exception = null;
            process.ExceptionDate = null;
            process.MachineName = ProcessLogic.ExecuteInMyMachine ? Environment.MachineName : ProcessDN.None;
        }

        public static void StartRunningProcesses()
        {
            if (running)
                throw new InvalidOperationException("ProcessLogic is running");

            Task.Factory.StartNew(() =>
            {
                using (AuthLogic.Disable())
                {
                    try
                    {
                        running = true;

                        (from p in Database.Query<ProcessDN>()
                         where
                         p.State == ProcessState.Executing && p.MachineName == Environment.MachineName ||
                         p.State == ProcessState.Suspending && p.MachineName == Environment.MachineName ||
                         p.State == ProcessState.Suspended && (p.MachineName == Environment.MachineName || !ProcessLogic.ExecuteInMyMachine && p.MachineName == ProcessDN.None)
                         select p).SetAsQueued();

                        CancelNewProcesses = new CancellationTokenSource();
                        autoResetEvent.Set();
                        while (autoResetEvent.WaitOne())
                        {
                            if (CancelNewProcesses.IsCancellationRequested)
                                return;

                            (from p in Database.Query<ProcessDN>()
                             where p.State == ProcessState.Planned && p.PlannedDate <= TimeZoneManager.Now
                             orderby p.PlannedDate
                             select p).SetAsQueued();

                            var list = Database.Query<ProcessDN>()
                                    .Where(a => ProcessLogic.ExecuteInMyMachine ? a.MachineName == Environment.MachineName : a.MachineName == ProcessDN.None)
                                    .Where(a => a.State == ProcessState.Planned)
                                    .Select(a => a.PlannedDate)
                                    .ToListWithInvalidation("Process", args => autoResetEvent.Set());

                            SetNextPannedExecution(list.Min());

                            lock (executing)
                            {
                                int remaining = MaxDegreeOfParallelism - executing.Count;

                                if (remaining > 0)
                                {
                                    using (Transaction tr = Transaction.ForceNew(IsolationLevel.Serializable))
                                    {
                                        var queued = Database.Query<ProcessDN>()
                                            .Where(p => p.State == ProcessState.Queued)
                                            .Where(p => p.MachineName == Environment.MachineName || !ProcessLogic.ExecuteInMyMachine && p.MachineName == ProcessDN.None)
                                            .Select(a => new { Process = a.ToLite(), a.QueuedDate })
                                            .ToListWithInvalidation("", args => autoResetEvent.Set());

                                        var afordable = queued.OrderBy(a => a.QueuedDate).Take(remaining).ToList();

                                        foreach (var pair in afordable)
                                        {
                                            ProcessDN pro = pair.Process.Retrieve();

                                            IProcessAlgorithm algorithm = ProcessLogic.GetProcessAlgorithm(MultiEnumLogic<ProcessAlgorithmDN>.ToEnum(pro.Algorithm));

                                            ExecutingProcess executingProcess = new ExecutingProcess(algorithm, pro);

                                            executing.Add(pro.ToLite(), executingProcess);

                                            executingProcess.TakeForThisMachine();

                                            Task.Factory.StartNew(() =>
                                            {
                                                try
                                                {
                                                    executingProcess.Execute();
                                                }
                                                finally
                                                {
                                                    lock (executing)
                                                    {
                                                        executing.Remove(pro.ToLite());
                                                        autoResetEvent.Set();
                                                    }
                                                }
                                            });
                                        }

                                        tr.Commit();
                                    }

                                    var suspending = Database.Query<ProcessDN>()
                                            .Where(p => p.State == ProcessState.Suspending)
                                            .Where(p => p.MachineName == Environment.MachineName)
                                            .Select(a => a.ToLite())
                                            .ToListWithInvalidation("", args => autoResetEvent.Set());

                                    foreach (var s in suspending)
                                    {
                                        ExecutingProcess execProc = executing.GetOrThrow(s);

                                        if (execProc.CurrentExecution.State != ProcessState.Finished)
                                        {
                                            execProc.CurrentExecution = s.Retrieve();
                                            execProc.CancelationSource.Cancel();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        e.LogException(edn => { edn.ControllerName = "ProcessWorker"; edn.ControllerName = "MainLoop"; });
                    }
                    finally
                    {
                        running = false;
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private static void SetNextPannedExecution(DateTime? next)
        {
            if (nextPlannedExecution == null)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            else
            {
                TimeSpan ts = nextPlannedExecution.Value - TimeZoneManager.Now;
                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;
                else
                    ts = ts.Add(TimeSpan.FromSeconds(2));

                timer.Change((int)ts.TotalMilliseconds, Timeout.Infinite); // invoke after the timespan
            }

        }

        public static void Stop()
        {
            if (!running)
                throw new InvalidOperationException("ProcessLogic is not running");

            timer.Dispose();
            CancelNewProcesses.Cancel();

            foreach (var p in executing.Values)
            {
                p.CancelationSource.Cancel();
            }
        }
    }


    public sealed class ExecutingProcess
    {
        public ProcessDN CurrentExecution { get; internal set; }
        internal IProcessAlgorithm Algorithm;
        internal CancellationTokenSource CancelationSource;

        public ExecutingProcess(IProcessAlgorithm processAlgorithm, ProcessDN process)
        {
            this.CancelationSource = new CancellationTokenSource();
            this.Algorithm = processAlgorithm;
            this.CurrentExecution = process;
        }

        public IProcessDataDN Data
        {
            get { return CurrentExecution.Data; }
        }

        public CancellationToken CancellationToken
        {
            get { return CancelationSource.Token; }
        }

        public void ProgressChanged(int position, int count)
        {
            decimal progress = ((decimal)position) / count;

            ProgressChanged(progress);
        }

        public void ProgressChanged(decimal progress)
        {
            if (progress != CurrentExecution.Progress)
            {
                CurrentExecution.Progress = progress;
                CurrentExecution.InDB().UnsafeUpdate(a => new ProcessDN { Progress = progress });
            }
        }


        public void TakeForThisMachine()
        {
            CurrentExecution.State = ProcessState.Executing;
            CurrentExecution.ExecutionStart = TimeZoneManager.Now;
            CurrentExecution.Progress = 0;
            CurrentExecution.MachineName = Environment.MachineName;
            using (OperationLogic.AllowSave<ProcessDN>())
                CurrentExecution.Save();
        }

        public void Execute()
        {
            using (ScopeSessionFactory.OverrideSession())
            {
                if (CurrentExecution.Session != null)
                    using (AuthLogic.Disable())
                        ProcessLogic.ApplySession.Invoke(CurrentExecution.Session);

                if (UserDN.Current == null)
                    UserDN.Current = AuthLogic.SystemUser;
                try
                {
                    Algorithm.Execute(this);

                    CurrentExecution.ExecutionEnd = TimeZoneManager.Now;
                    CurrentExecution.State = ProcessState.Finished;
                    CurrentExecution.Progress = null;
                    using (OperationLogic.AllowSave<ProcessDN>())
                        CurrentExecution.Save();
                }
                catch (OperationCanceledException e)
                {
                    if (!e.CancellationToken.Equals(this.CancellationToken))
                        throw;

                    CurrentExecution.SuspendDate = TimeZoneManager.Now;
                    CurrentExecution.State = ProcessState.Suspended;
                    using (OperationLogic.AllowSave<ProcessDN>())
                        CurrentExecution.Save();
                }
                catch (Exception e)
                {
                    if (Transaction.InTestTransaction)
                        throw;

                    CurrentExecution.State = ProcessState.Error;
                    CurrentExecution.ExceptionDate = TimeZoneManager.Now;
                    CurrentExecution.Exception = e.LogException(el => el.ActionName = CurrentExecution.Algorithm.ToString()).ToLite();
                    using (OperationLogic.AllowSave<ProcessDN>())
                        CurrentExecution.Save();
                }
            }
        }

        public override string ToString()
        {
            return "Execution (ID = {0}): {1} ".Formato(CurrentExecution.Id, CurrentExecution);
        }
    }



    public class ProcessLogicState
    {
        public int MaxDegreeOfParallelism;
        public int InitialDelayMiliseconds;
        public bool Running;
        public DateTime? NextPlannedExecution;
        public List<ExecutionState> Executing;
    }

    public class ExecutionState
    {
        public Lite<ProcessDN> Process;
        public ProcessState State;
        public bool IsCancellationRequested;
        public decimal? Progress;
        public string MachineName;
    }
}
