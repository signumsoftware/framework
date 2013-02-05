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
using Signum.Engine.Extensions.Properties;
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

namespace Signum.Engine.Processes
{
    public static class ProcessLogic
    {
        public static Polymorphic<Action<ISessionDataDN>> ApplySession = new Polymorphic<Action<ISessionDataDN>>();

        public static Func<ISessionDataDN> CreateDefaultProcessSession;

        static Expression<Func<ProcessDN, IQueryable<ProcessExecutionDN>>> ExecutionsExpression =
            p => Database.Query<ProcessExecutionDN>().Where(a => a.Process == p);
        public static IQueryable<ProcessExecutionDN> Executions(this ProcessDN p)
        {
            return ExecutionsExpression.Evaluate(p);
        }

        static Expression<Func<ProcessDN, IQueryable<ProcessExecutionDN>>> LastExecutionExpression =
            p => p.Executions().OrderByDescending(a => a.ExecutionStart).Take(1);
        public static IQueryable<ProcessExecutionDN> LastExecution(this ProcessDN p)
        {
            return LastExecutionExpression.Evaluate(p);
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
                sb.Include<ProcessDN>();
                sb.Include<ProcessExecutionDN>();

                ProcessLogic.MaxDegreeOfParallelism = maxDegreeOfParallelism;

                PermissionAuthLogic.RegisterPermissions(ProcessPermission.ViewProcessControlPanel);

                MultiEnumLogic<ProcessDN>.Start(sb, () => registeredProcesses.Keys.ToHashSet());

                OperationLogic.AssertStarted(sb);
                AuthLogic.AssertStarted(sb);
                ProcessExecutionGraph.Register();

                sb.Schema.EntityEvents<ProcessExecutionDN>().Saving += ProcessExecution_Saving;

                dqm[typeof(ProcessDN)] =
                             (from p in Database.Query<ProcessDN>()
                              join pe in Database.Query<ProcessExecutionDN>().DefaultIfEmpty() on p equals pe.Process into g
                              select new
                              {
                                  Entity = p,
                                  p.Id,
                                  p.Name
                              }).ToDynamic();

                dqm[typeof(ProcessExecutionDN)] =
                             (from pe in Database.Query<ProcessExecutionDN>()
                              select new
                              {
                                  Entity = pe,
                                  pe.Id,
                                  Resume = pe.ToString(),
                                  pe.Process,
                                  State = pe.State,
                                  pe.CreationDate,
                                  pe.PlannedDate,
                                  pe.CancelationDate,
                                  pe.QueuedDate,
                                  pe.ExecutionStart,
                                  pe.ExecutionEnd,
                                  pe.SuspendDate,
                                  ErrorDate = pe.ExceptionDate,
                              }).ToDynamic();

                dqm.RegisterExpression((ProcessDN p) => p.Executions());
                dqm.RegisterExpression((ProcessDN p) => p.LastExecution());

                if (userProcessSession)
                {
                    sb.Settings.AssertImplementedBy((ProcessExecutionDN pe) => pe.SessionData, typeof(UserProcessSessionDN));
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

        static void ProcessExecution_Saving(ProcessExecutionDN pe)
        {
            if (pe.Modified.Value)
                Transaction.PostRealCommit += ud =>
                {
                    switch (pe.State)
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
                            Suspend(pe);
                            break;
                        case ProcessState.Queued:
                            Enqueue(pe);
                            break;
                    }
                };
        }

        static void Enqueue(ProcessExecutionDN pe)
        {
            if (pe.State != ProcessState.Queued || queue.Any(a => a.Execution.Id == pe.Id))
                return;

            var ep = new ExecutingProcess
            {
                Algorithm = registeredProcesses[MultiEnumLogic<ProcessDN>.ToEnum(pe.Process.Key)],
                Data = pe.ProcessData,
                Execution = pe,
            };

            queue.Add(ep);
        }

        static void Suspend(ProcessExecutionDN pe)
        {
            if (pe.State != ProcessState.Suspending)
                return;

            ExecutingProcess execProc;

            if (!executing.TryGetValue(pe.Id, out execProc))
                throw new ApplicationException(ProcessMessage.ProcessExecution0IsNotRunningAnymore.NiceToString().Formato(pe.Id));

            execProc.Execution = pe;
            execProc.CancelationSource.Cancel();
        }

        static bool running = false;

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
            using (new EntityCache(true))
            {
                var pes = (from pe in Database.Query<ProcessExecutionDN>()
                           where pe.State == ProcessState.Executing ||
                                 pe.State == ProcessState.Queued ||
                                 pe.State == ProcessState.Suspending ||
                                 pe.State == ProcessState.Suspended
                           select pe).AsEnumerable().OrderByDescending(pe => pe.State).ToArray();

                foreach (var pe in pes)
                {
                    pe.SetAsQueue();
                    using (OperationLogic.AllowSave<ProcessExecutionDN>())
                        pe.Save();
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
                                dbState = ep.Execution.InDBEntity(pe => pe.State);

                            if (dbState == ProcessState.Queued) //Not canceled
                            {
                                try
                                {
                                    executing.TryAdd(ep.Execution.Id, ep);

                                    ep.Execute();
                                }

                                finally
                                {
                                    ExecutingProcess rubish;
                                    executing.TryRemove(ep.Execution.Id, out rubish);
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

            foreach (var pe in executing.Values)
            {
                pe.CancelationSource.Cancel();
            }
        }

        static DateTime? nextPlannedExecution;

        static void RefreshPlan()
        {
            using (new EntityCache(true))
            using (AuthLogic.Disable())
            {
                nextPlannedExecution = Database.Query<ProcessExecutionDN>()
                    .Where(pe => pe.State == ProcessState.Planned)
                    .Min(pe => pe.PlannedDate);
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
            using (new EntityCache(true))
            using (AuthLogic.Disable())
            {
                var pes = (from pe in Database.Query<ProcessExecutionDN>()
                           where pe.State == ProcessState.Planned && pe.PlannedDate <= TimeZoneManager.Now
                           orderby pe.PlannedDate
                           select pe).ToArray();

                foreach (var pe in pes)
                {
                    pe.SetAsQueue();
                    using (OperationLogic.AllowSave<ProcessExecutionDN>())
                        pe.Save();
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

        public class ProcessExecutionGraph : Graph<ProcessExecutionDN, ProcessState>
        {
            public static void Register()
            {
                GetState = e => e.State;

                new Execute(ProcessOperation.Save)
                {
                    FromStates = new[] { ProcessState.Created },
                    ToState = ProcessState.Created,
                    AllowsNew = true,
                    Lite = false,
                    Execute = (pe, args) =>
                    {
                        pe.Save();
                    }
                }.Register();


                new Execute(ProcessOperation.Plan)
                {
                    FromStates = new[] { ProcessState.Created, ProcessState.Canceled, ProcessState.Planned, ProcessState.Suspended },
                    ToState = ProcessState.Planned,
                    Execute = (pe, args) =>
                    {
                        pe.State = ProcessState.Planned;
                        pe.PlannedDate = (DateTime)args[0];
                    }
                }.Register();

                new Execute(ProcessOperation.Cancel)
                {
                    FromStates = new[] { ProcessState.Planned, ProcessState.Created, ProcessState.Suspended, ProcessState.Queued },
                    ToState = ProcessState.Canceled,
                    Execute = (pe, _) =>
                    {
                        pe.State = ProcessState.Canceled;
                        pe.CancelationDate = TimeZoneManager.Now;
                    }
                }.Register();

                new Execute(ProcessOperation.Execute)
                {
                    FromStates = new[] { ProcessState.Created, ProcessState.Planned, ProcessState.Canceled, ProcessState.Suspended },
                    ToState = ProcessState.Queued,
                    Execute = (pe, _) =>
                    {
                        pe.SetAsQueue();
                    }
                }.Register();

                new Execute(ProcessOperation.Suspend)
                {
                    FromStates = new[] { ProcessState.Executing },
                    ToState = ProcessState.Suspending,
                    Execute = (pe, _) =>
                    {
                        pe.State = ProcessState.Suspending;
                        pe.SuspendDate = TimeZoneManager.Now;
                    }
                }.Register();

                new ConstructFrom<ProcessExecutionDN>(ProcessOperation.Retry)
                {
                    CanConstruct = pe=>pe.State.InState(ProcessOperation.Retry, ProcessState.Error),
                    ToState = ProcessState.Created,
                    Construct = (pe, _) =>
                        pe.Process.Create(pe.ProcessData, pe.SessionData)
                }.Register();
            }
        }


        public static ProcessExecutionDN Create(Enum processKey, IProcessDataDN processData, ISessionDataDN session = null)
        {
            return MultiEnumLogic<ProcessDN>.ToEntity(processKey).Create(processData, session);
        }

        public static ProcessExecutionDN Create(this ProcessDN process, IProcessDataDN processData, ISessionDataDN session = null)
        {
            if (session == null)
            {
                session = ProcessLogic.CreateDefaultProcessSession();
            }

            using (OperationLogic.AllowSave<ProcessExecutionDN>())
                return new ProcessExecutionDN(process)
                {
                    State = ProcessState.Created,
                    ProcessData = processData,
                    SessionData = session,
                }.Save();
        }

        public static void ExecuteTest(this ProcessExecutionDN pe)
        {
            pe.QueuedDate = TimeZoneManager.Now;
            var ep = new ExecutingProcess
            {
                Algorithm = registeredProcesses[MultiEnumLogic<ProcessDN>.ToEnum(pe.Process.Key)],
                Data = pe.ProcessData,
                Execution = pe,
            };

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
                    ProcessExecution = p.Execution.ToLite(),
                    State = p.Execution.State,
                    Progress = p.Execution.Progress
                }).ToList(),
                Queue = queue.Select(p => new ExecutionState
                {
                    IsCancellationRequested = p.CancelationSource.IsCancellationRequested,
                    ProcessExecution = p.Execution.ToLite(),
                    State = p.Execution.State,
                    Progress = p.Execution.Progress
                }).ToList()
            };
        }

        public static void ForEachLine<T>(this IExecutingProcess executingProcess, IQueryable<T> remainingLines, Action<T> action)
            where T : IdentifiableEntity, IProcessLineDN, new()
        {
            var totalCount = remainingLines.Count();

            while (true)
            {
                List<T> lines = remainingLines.Take(100).ToList();
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
                            pl.InDB().UnsafeUpdate(p => new T { Exception = exLog.ToLite() });
                            tr.Commit();
                        }
                    }

                    executingProcess.ProgressChanged(i, totalCount);
                }
            }
        }
    }

    public interface IProcessAlgorithm
    {
        void Execute(IExecutingProcess executingProcess);
    }

    public interface IExecutingProcess
    {
        IProcessDataDN Data { get; }
        CancellationToken CancellationToken { get; }
        void ProgressChanged(decimal progress);
        void ProgressChanged(int position, int count);
    }

    internal class ExecutingProcess : IExecutingProcess
    {
        public ProcessExecutionDN Execution { get; set; }
        public IProcessAlgorithm Algorithm { get; set; }
        public IProcessDataDN Data { get; set; }

        public CancellationTokenSource CancelationSource { get; private set; }

        public ExecutingProcess()
        {
            CancelationSource = new CancellationTokenSource();
        }

        public CancellationToken CancellationToken
        {
            get { return CancelationSource.Token; }
        }

        public void ProgressChanged(int position, int count)
        {
            decimal progress = (100 * position) / count;

            ProgressChanged(progress);
        }

        public void ProgressChanged(decimal progress)
        {
            if (progress != Execution.Progress)
            {
                Execution.Progress = progress;
                Execution.InDB().UnsafeUpdate(a => new ProcessExecutionDN { Progress = progress });
            }
        }

        public void Execute()
        {
            using (ScopeSessionFactory.OverrideSession())
            {
                if (Execution.SessionData != null)
                    using (AuthLogic.Disable())
                        ProcessLogic.ApplySession.Invoke(Execution.SessionData);

                if (UserDN.Current == null)
                    UserDN.Current = AuthLogic.SystemUser;

                Execution.State = ProcessState.Executing;
                Execution.ExecutionStart = TimeZoneManager.Now;
                Execution.Progress = 0;
                using (OperationLogic.AllowSave<ProcessExecutionDN>())
                    Execution.Save();

                try
                {
                    Algorithm.Execute(this);

                    Execution.ExecutionEnd = TimeZoneManager.Now;
                    Execution.State = ProcessState.Finished;
                    Execution.Progress = null;
                    using (OperationLogic.AllowSave<ProcessExecutionDN>())
                        Execution.Save();
                }
                catch (OperationCanceledException e)
                {
                    if (!e.CancellationToken.Equals(this.CancellationToken))
                        throw;

                    Execution.SuspendDate = TimeZoneManager.Now;
                    Execution.State = ProcessState.Suspended;
                    using (OperationLogic.AllowSave<ProcessExecutionDN>())
                        Execution.Save();
                }
                catch (Exception e)
                {
                    if (Transaction.InTestTransaction)
                        throw;

                    Execution.State = ProcessState.Error;
                    Execution.ExceptionDate = TimeZoneManager.Now;
                    Execution.Exception = e.LogException(el => el.ActionName = Execution.Process.ToString()).ToLite();
                    using (OperationLogic.AllowSave<ProcessExecutionDN>())
                        Execution.Save();
                }
            }
        }

        public override string ToString()
        {
            return "Execution (ID = {0}): {1} ".Formato(Execution.Id, Execution);
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
        public Lite<ProcessExecutionDN> ProcessExecution;
        public ProcessState State;
        public bool IsCancellationRequested;
        public decimal? Progress;
    }
}
