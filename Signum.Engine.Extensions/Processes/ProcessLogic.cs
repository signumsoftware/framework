using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Processes;
using Signum.Entities;
using Signum.Engine.Operations;
using Signum.Engine.Authorization;
using Signum.Utilities;
using System.Threading;
using Signum.Utilities.DataStructures;
using System.Diagnostics;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Entities.Scheduler;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities.Authorization;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Signum.Engine.Scheduler;
using System.Linq.Expressions;
using Signum.Engine.Exceptions;
using System.IO;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Processes
{
    public static class ProcessLogic
    {
        public static Polymorphic<Action<IProcessSessionDN>> ApplySession = new Polymorphic<Action<IProcessSessionDN>>();

        public static Func<IProcessSessionDN> CreateDefaultProcessSession;

        static Expression<Func<ProcessAlgorithmDN, IQueryable<ProcessDN>>> ProcessesFromAlgorithmExpression =
            p => Database.Query<ProcessDN>().Where(a => a.Algorithm == p);
        [ExpressionField("ProcessesFromAlgorithmExpression")]
        public static IQueryable<ProcessDN> Processes(this ProcessAlgorithmDN p)
        {
            return ProcessesFromAlgorithmExpression.Evaluate(p);
        }

        static Expression<Func<ProcessAlgorithmDN, ProcessDN>> LastProcessFromAlgorithmExpression =
            p => p.Processes().OrderByDescending(a => a.ExecutionStart).FirstOrDefault();
          [ExpressionField("LastProcessFromAlgorithmExpression")]
        public static ProcessDN LastProcess(this ProcessAlgorithmDN p)
        {
            return LastProcessFromAlgorithmExpression.Evaluate(p);
        }

        static Expression<Func<ProcessDN, IQueryable<ProcessExceptionLineDN>>> ExceptionLinesProcessExpression =
            p => Database.Query<ProcessExceptionLineDN>().Where(a => a.Process.RefersTo(p));
        [ExpressionField("ExceptionLinesProcessExpression")]
        public static IQueryable<ProcessExceptionLineDN> ExceptionLines(this ProcessDN p)
        {
            return ExceptionLinesProcessExpression.Evaluate(p);
        }


        static Expression<Func<IProcessLineDataDN, IQueryable<ProcessExceptionLineDN>>> ExceptionLinesLineExpression =
            p => Database.Query<ProcessExceptionLineDN>().Where(a => a.Line.RefersTo(p));
        [ExpressionField("ExceptionLinesLineExpression")]
        public static IQueryable<ProcessExceptionLineDN> ExceptionLines(this IProcessLineDataDN pl)
        {
            return ExceptionLinesLineExpression.Evaluate(pl);
        }

        static Expression<Func<IProcessLineDataDN, ProcessDN, ExceptionDN>> ExceptionExpression =
            (pl, p) => p.ExceptionLines().SingleOrDefault(el => el.Line.RefersTo(pl)).Exception.Entity;
        public static ExceptionDN Exception(this IProcessLineDataDN pl, ProcessDN p)
        {
            return ExceptionExpression.Evaluate(pl, p);
        }


        static Expression<Func<IProcessDataDN, IQueryable<ProcessDN>>> ProcessesFromDataExpression =
            e => Database.Query<ProcessDN>().Where(a => a.Data == e);
        [ExpressionField("ProcessesFromDataExpression")]
        public static IQueryable<ProcessDN> Processes(this IProcessDataDN e)
        {
            return ProcessesFromDataExpression.Evaluate(e);
        }

        static Expression<Func<IProcessDataDN, ProcessDN>> LastProcessFromDataExpression =
          e => e.Processes().OrderByDescending(a => a.ExecutionStart).FirstOrDefault();
        [ExpressionField("LastProcessFromDataExpression")]
        public static ProcessDN LastProcess(this IProcessDataDN e)
        {
            return LastProcessFromDataExpression.Evaluate(e);
        }

        static BlockingCollection<ExecutingProcess> queue = new BlockingCollection<ExecutingProcess>();
        static ConcurrentDictionary<int, ExecutingProcess> executing = new ConcurrentDictionary<int, ExecutingProcess>();

        static Dictionary<Enum, IProcessAlgorithm> registeredProcesses = new Dictionary<Enum, IProcessAlgorithm>();

        static Timer timer = new Timer(new TimerCallback(DispatchEvents), // main timer
                                null,
                                Timeout.Infinite,
                                Timeout.Infinite);

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => ProcessLogic.Start(null, null, 0, false)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, int maxDegreeOfParallelism, bool userProcessSession)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ProcessAlgorithmDN>();
                sb.Include<ProcessDN>();
                sb.Include<ProcessExceptionLineDN>();

                ProcessLogic.MaxDegreeOfParallelism = maxDegreeOfParallelism;

                PermissionAuthLogic.RegisterPermissions(ProcessPermission.ViewProcessControlPanel);

                MultiEnumLogic<ProcessAlgorithmDN>.Start(sb, () => registeredProcesses.Keys.ToHashSet());

                OperationLogic.AssertStarted(sb);
                AuthLogic.AssertStarted(sb);
                ProcessGraph.Register();

                sb.Schema.EntityEvents<ProcessDN>().Saving += Process_Saving;

                dqm.RegisterQuery(typeof(ProcessAlgorithmDN), () =>
                             from pa in Database.Query<ProcessAlgorithmDN>()
                             select new
                             {
                                 Entity = pa,
                                 pa.Id,
                                 pa.Key
                             });

                dqm.RegisterQuery(typeof(ProcessDN), ()=>
                             from p in Database.Query<ProcessDN>()
                              select new
                              {
                                  Entity = p,
                                  p.Id,
                                  Resume = p.ToString(),
                                  Process = p.Algorithm,
                                  State = p.State,
                                  p.CreationDate,
                                  p.PlannedDate,
                                  p.CancelationDate,
                                  p.QueuedDate,
                                  p.ExecutionStart,
                                  p.ExecutionEnd,
                                  p.SuspendDate,
                                  ErrorDate = p.ExceptionDate,
                              });

                dqm.RegisterQuery(typeof(ProcessExceptionLineDN), () =>
                             from p in Database.Query<ProcessExceptionLineDN>()
                             select new
                             {
                                 Entity = p,
                                 p.Line,
                                 p.Process,
                                 p.Exception,
                             });

                dqm.RegisterExpression((ProcessAlgorithmDN p) => p.Processes());
                dqm.RegisterExpression((ProcessAlgorithmDN p) => p.LastProcess());

                dqm.RegisterExpression((IProcessDataDN p) => p.Processes());
                dqm.RegisterExpression((IProcessDataDN p) => p.LastProcess());

                dqm.RegisterExpression((IProcessLineDataDN p) => p.ExceptionLines());

                if (userProcessSession)
                {
                    sb.Settings.AssertImplementedBy((ProcessDN p) => p.Session, typeof(UserProcessSessionDN));
                    ApplySession.Register((UserProcessSessionDN ups) =>
                    {
                        if (ups.User != null)
                            UserDN.Current = ups.User.Retrieve();
                    });

                    CreateDefaultProcessSession = UserProcessSessionDN.CreateCurrent;
                }
                else
                    CreateDefaultProcessSession = () => null;
            }
        }

        static void Process_Saving(ProcessDN p)
        {
            if (p.IsGraphModified)
                Transaction.PostRealCommit += ud =>
                {
                    switch (p.State)
                    {
                        case ProcessState.Created:
                        case ProcessState.Suspended:
                        case ProcessState.Finished:
                        case ProcessState.Executing:
                            break;
                        case ProcessState.Planned:
                        case ProcessState.Canceled:
                            RefreshPlan();
                            break;
                        case ProcessState.Suspending:
                            Suspend(p);
                            break;
                        case ProcessState.Queued:
                            Enqueue(p);
                            break;
                    }
                };
        }

        static void Enqueue(ProcessDN p)
        {
            if (p.State != ProcessState.Queued || queue.Any(a => a.CurrentExecution.Id == p.Id))
                return;

            var ep = new ExecutingProcess(registeredProcesses[MultiEnumLogic<ProcessAlgorithmDN>.ToEnum(p.Algorithm.Key)], p);

            queue.Add(ep);
        }

        static void Suspend(ProcessDN p)
        {
            if (p.State != ProcessState.Suspending)
                return;

            ExecutingProcess execProc;

            if (!executing.TryGetValue(p.Id, out execProc))
                throw new ApplicationException(ProcessMessage.Process0IsNotRunningAnymore.NiceToString().Formato(p.Id));

            execProc.CurrentExecution = p;
            execProc.CancelationSource.Cancel();
        }

        static bool running = false;

        public static bool ExecuteProcessesFromOtherMachines = false;

        static int initialDelayMiliseconds;

        public static void StartBackgroundProcess(int delayMilliseconds)
        {
            initialDelayMiliseconds = delayMilliseconds;

            if (initialDelayMiliseconds == 0)
                StartBackgroundProcess();

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(initialDelayMiliseconds);
                StartBackgroundProcess();
            });
        }

        public static void StartBackgroundProcess()
        {
            if (running)
                throw new InvalidOperationException("ProcessLogic is running");

            using (ExecutionMode.Global())
            using (new EntityCache(EntityCacheType.ForceNew))
            {
                var pes = (from p in Database.Query<ProcessDN>()
                           where p.State == ProcessState.Executing ||
                                 p.State == ProcessState.Queued ||
                                 p.State == ProcessState.Suspending ||
                                 p.State == ProcessState.Suspended
                           where ExecuteProcessesFromOtherMachines || p.MachineName == null || p.MachineName == Environment.MachineName
                           select p).AsEnumerable().OrderByDescending(p => p.State).ToArray();

                foreach (var p in pes)
                {
                    p.SetAsQueue();
                    using (OperationLogic.AllowSave<ProcessDN>())
                        p.Save();
                }

                Task.Factory.StartNew(() =>
                {
                    running = true;

                    CancelNewProcesses = new CancellationTokenSource();

                    var po = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = CancelNewProcesses.Token };
                    try
                    {
                        Parallel.ForEach(queue.GetConsumingEnumerable(CancelNewProcesses.Token), po, ep =>
                        {

                            ProcessState dbState;
                            using (AuthLogic.Disable())
                                dbState = ep.CurrentExecution.InDBEntity(p => p.State);

                            if (dbState == ProcessState.Queued) //Not canceled
                            {
                                try
                                {
                                    executing.TryAdd(ep.CurrentExecution.Id, ep);

                                    ep.Execute();
                                }

                                finally
                                {
                                    ExecutingProcess rubish;
                                    executing.TryRemove(ep.CurrentExecution.Id, out rubish);
                                }
                            }
                        });
                    }
                    catch (OperationCanceledException oc)
                    {
                        if (oc.CancellationToken != CancelNewProcesses.Token)
                            throw;
                    }
                    finally
                    {
                        running = false;
                    }

                }, TaskCreationOptions.LongRunning);

                RefreshPlan();
            }
        }

        public static void Stop()
        {
            if (!running)
                throw new InvalidOperationException("ProcessLogic is not running");

            CancelNewProcesses.Cancel();

            foreach (var p in executing.Values)
            {
                p.CancelationSource.Cancel();
            }
        }

        static DateTime? nextPlannedExecution;

        static void RefreshPlan()
        {
            using (new EntityCache(EntityCacheType.ForceNew))
            using (AuthLogic.Disable())
            {
                nextPlannedExecution = Database.Query<ProcessDN>()
                    .Where(p => p.State == ProcessState.Planned)
                    .Min(p => p.PlannedDate);
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
        }

        static void DispatchEvents(object obj)
        {
            using (new EntityCache(EntityCacheType.ForceNew))
            using (AuthLogic.Disable())
            {
                var pes = (from p in Database.Query<ProcessDN>()
                           where p.State == ProcessState.Planned && p.PlannedDate <= TimeZoneManager.Now
                           orderby p.PlannedDate
                           select p).ToArray();

                foreach (var p in pes)
                {
                    p.SetAsQueue();
                    using (OperationLogic.AllowSave<ProcessDN>())
                        p.Save();
                }

                RefreshPlan();
            }
        }

        public static void Register(Enum processKey, IProcessAlgorithm logic)
        {
            if (processKey == null)
                throw new ArgumentNullException("processKey");

            if (logic == null)
                throw new ArgumentNullException("logic");

            registeredProcesses.Add(processKey, logic);
        }

        public static int MaxDegreeOfParallelism = 4;

        static CancellationTokenSource CancelNewProcesses;

        public class ProcessGraph : Graph<ProcessDN, ProcessState>
        {
            public static void Register()
            {
                GetState = e => e.State;

                new Execute(ProcessOperation.Save)
                {
                    FromStates = { ProcessState.Created },
                    ToState = ProcessState.Created,
                    AllowsNew = true,
                    Lite = false,
                    Execute = (p, args) =>
                    {
                        p.Save();
                    }
                }.Register();

                new Execute(ProcessOperation.Plan)
                {
                    FromStates = { ProcessState.Created, ProcessState.Canceled, ProcessState.Planned, ProcessState.Suspended },
                    ToState = ProcessState.Planned,
                    Execute = (p, args) =>
                    {
                        p.State = ProcessState.Planned;
                        p.PlannedDate = (DateTime)args[0];
                    }
                }.Register();

                new Execute(ProcessOperation.Cancel)
                {
                    FromStates = { ProcessState.Planned, ProcessState.Created, ProcessState.Suspended, ProcessState.Queued },
                    ToState = ProcessState.Canceled,
                    Execute = (p, _) =>
                    {
                        p.State = ProcessState.Canceled;
                        p.CancelationDate = TimeZoneManager.Now;
                    }
                }.Register();

                new Execute(ProcessOperation.Execute)
                {
                    FromStates = { ProcessState.Created, ProcessState.Planned, ProcessState.Canceled, ProcessState.Suspended },
                    ToState = ProcessState.Queued,
                    Execute = (p, _) =>
                    {
                        p.SetAsQueue();
                    }
                }.Register();

                new Execute(ProcessOperation.Suspend)
                {
                    FromStates = { ProcessState.Executing },
                    ToState = ProcessState.Suspending,
                    Execute = (p, _) =>
                    {
                        p.State = ProcessState.Suspending;
                        p.SuspendDate = TimeZoneManager.Now;
                    }
                }.Register();

                new ConstructFrom<ProcessDN>(ProcessOperation.Retry)
                {
                    CanConstruct = p => p.State.InState(ProcessState.Error, ProcessState.Canceled, ProcessState.Finished, ProcessState.Suspended),
                    ToState = ProcessState.Created,
                    Construct = (p, _) => p.Algorithm.Create(p.Data, p.Session)
                }.Register();
            }
        }


        public static ProcessDN Create(Enum processKey, IProcessDataDN processData, IProcessSessionDN session = null)
        {
            return MultiEnumLogic<ProcessAlgorithmDN>.ToEntity(processKey).Create(processData, session);
        }

        public static ProcessDN Create(this ProcessAlgorithmDN process, IProcessDataDN processData, IProcessSessionDN session = null)
        {
            if (session == null)
            {
                session = ProcessLogic.CreateDefaultProcessSession();
            }

            using (OperationLogic.AllowSave<ProcessDN>())
                return new ProcessDN(process)
                {
                    State = ProcessState.Created,
                    Data = processData,
                    Session = session,
                    MachineName = Environment.MachineName,
                }.Save();
        }

        public static void ExecuteTest(this ProcessDN p)
        {
            p.QueuedDate = TimeZoneManager.Now;
            var ep = new ExecutingProcess(
                registeredProcesses[MultiEnumLogic<ProcessAlgorithmDN>.ToEnum(p.Algorithm.Key)],
                p
            );

            ep.Execute();
        }

        public static IProcessAlgorithm GetProcessAlgorithm(Enum processKey)
        {
            return registeredProcesses.GetOrThrow(processKey, "The process {0} is not registered");
        }

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
                    Progress = p.CurrentExecution.Progress
                }).ToList(),
                Queue = queue.Select(p => new ExecutionState
                {
                    IsCancellationRequested = p.CancelationSource.IsCancellationRequested,
                    Process = p.CurrentExecution.ToLite(),
                    State = p.CurrentExecution.State,
                    Progress = p.CurrentExecution.Progress
                }).ToList()
            };
        }

        public static void ForEachLine<T>(this ExecutingProcess executingProcess, IQueryable<T> remainingLines, Action<T> action, int groupsOf = 100)
            where T : IdentifiableEntity, IProcessLineDataDN, new()
        {
            var ramainingNotExceptionsLines = remainingLines.Where(li => li.Exception(executingProcess.CurrentExecution) == null);

            var totalCount = ramainingNotExceptionsLines.Count();
            int j = 0; 
            while (true)
            {
                List<T> lines = ramainingNotExceptionsLines.Take(groupsOf).ToList();
                if (lines.IsEmpty())
                    return;

                for (int i = 0; i < lines.Count; i++)
                {
                    executingProcess.CancellationToken.ThrowIfCancellationRequested();

                    T pl = lines[i];

                    try
                    {
                        using (Transaction tr = Transaction.ForceNew())
                        {
                            action(pl);
                            tr.Commit();
                        }
                    }
                    catch (Exception e)
                    {
                        if (Transaction.InTestTransaction)
                            throw;

                        var exLog = e.LogException();

                        using (Transaction tr = Transaction.ForceNew())
                        {
                            new ProcessExceptionLineDN
                            {
                                Exception = exLog.ToLite(),
                                Line = pl.ToLite(),
                                Process = executingProcess.CurrentExecution.ToLite()
                            }.Save();

                            tr.Commit();
                        }
                    }

                    executingProcess.ProgressChanged(j++, totalCount);
                }
            }
        }
    }

    public interface IProcessAlgorithm
    {
        void Execute(ExecutingProcess executingProcess);
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

        public void Execute()
        {
            using (ScopeSessionFactory.OverrideSession())
            {
                if (CurrentExecution.Session != null)
                    using (AuthLogic.Disable())
                        ProcessLogic.ApplySession.Invoke(CurrentExecution.Session);

                if (UserDN.Current == null)
                    UserDN.Current = AuthLogic.SystemUser;

                CurrentExecution.State = ProcessState.Executing;
                CurrentExecution.ExecutionStart = TimeZoneManager.Now;
                CurrentExecution.Progress = 0;
                using (OperationLogic.AllowSave<ProcessDN>())
                    CurrentExecution.Save();

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
        public List<ExecutionState> Queue;
    }

    public class ExecutionState
    {
        public Lite<ProcessDN> Process;
        public ProcessState State;
        public bool IsCancellationRequested;
        public decimal? Progress;
    }
}
