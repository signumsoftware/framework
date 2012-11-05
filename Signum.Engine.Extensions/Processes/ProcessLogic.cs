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
using Signum.Entities.Operations;
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
using Signum.Entities.Exceptions;
using Signum.Engine.Exceptions;
using System.IO;

namespace Signum.Engine.Processes
{
    public static class ProcessLogic
    {
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
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => ProcessLogic.Start(null, null, 0)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, int maxDegreeOfParallelism)
        {

            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ProcessDN>();
                sb.Include<ProcessExecutionDN>();

                ProcessLogic.MaxDegreeOfParallelism = maxDegreeOfParallelism;

                PermissionAuthLogic.RegisterPermissions(ProcessPermissions.ViewProcessControlPanel);

                EnumLogic<ProcessDN>.Start(sb, () => registeredProcesses.Keys.ToHashSet());

                OperationLogic.AssertStarted(sb);
                AuthLogic.AssertStarted(sb);
                ProcessExecutionGraph.Register();

                SchedulerLogic.ExecuteTask.Register((ProcessDN p) =>
                    ProcessLogic.Create(p).Execute(ProcessOperation.Execute));

                if (InitialDelayMiliseconds > -1)
                    sb.Schema.Initializing[InitLevel.Level4BackgroundProcesses] += () =>
                {
                    if(InitialDelayMiliseconds == 0)
                        Start();

                    Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(InitialDelayMiliseconds);
                        Start(); 
                    }); 
                };

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

                PackageLogic.Start(sb, dqm);
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
                            Execute(pe);
                            break;
                    }
                };
        }

        static void Execute(ProcessExecutionDN pe)
        {
            if (pe.State != ProcessState.Queued || queue.Any(a => a.Execution.Id == pe.Id))
                return;

            var ep = new ExecutingProcess
            {
                Algorithm = registeredProcesses[EnumLogic<ProcessDN>.ToEnum(pe.Process.Key)],
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
                throw new ApplicationException(Resources.ProcessExecution0IsNotRunningAnymore.Formato(pe.Id));

            execProc.Execution = pe;
            execProc.CancelationSource.Cancel();
        }

        static bool running = false;

        public static void Start()
        {
            if (running)
                throw new InvalidOperationException("ProcessLogic is running");

            using (Schema.Current.GlobalMode())
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
                            try
                            {
                                executing.TryAdd(ep.Execution.Id, ep);

                                using (AuthLogic.Disable())
                                {
                                    ep.Execute();
                                }
                            }
                            finally
                            {
                                ExecutingProcess rubish;
                                executing.TryRemove(ep.Execution.Id, out rubish);
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

        public static int InitialDelayMiliseconds = -1;
        public static int MaxDegreeOfParallelism = 4;

        static CancellationTokenSource CancelNewProcesses;

        public class ProcessExecutionGraph : Graph<ProcessExecutionDN, ProcessState>
        {
            static ProcessExecutionDN Create(ProcessDN process, Enum processKey, params object[] args)
            {
                IProcessDataDN data;
                if (args != null && args.Length != 0 && args[0] is IProcessDataDN)
                {
                    data = (IProcessDataDN)args[0];
                }
                else
                {
                    IProcessAlgorithm processAlgorithm = registeredProcesses[processKey];
                    data = processAlgorithm.CreateData(args);
                }

                using (OperationLogic.AllowSave<ProcessExecutionDN>())
                return new ProcessExecutionDN(process)
                {
                    User = UserDN.Current.ToLite(),
                    State = ProcessState.Created,
                    ProcessData = data
                }.Save();
            }


            public static void Register()
            {
                GetState = e => e.State;

                new Construct(ProcessOperation.Create)
                    {                     
                    ToState = ProcessState.Created,
                    Construct = args =>
                        {
                            Enum processKey = args.GetArg<Enum>(0); 
                            return Create(EnumLogic<ProcessDN>.ToEntity(processKey), processKey, args.Skip(1).ToArray());
                        }
                }.Register();

                new ConstructFrom<ProcessDN>(ProcessOperation.FromProcess)
                    {
                    ToState = ProcessState.Created,
                        Lite = false,
                        Construct = (process, args)=>
                        {
                            return Create(process, EnumLogic<ProcessDN>.ToEnum(process), args);
                        }
                }.Register();

                new Execute(ProcessOperation.Save)
                    {
                         FromStates = new []{ProcessState.Created },
                    ToState = ProcessState.Created,
                         AllowsNew = true,
                         Lite = false,
                         Execute = (pe, args)=>
                         {
                            
                             pe.Save(); 
                         }
                }.Register();

                //new Execute(ProcessOperation.Save, ProcessState.Planned)
                    //{
                    //     FromStates = new []{ProcessState.Planned},
                    //     AllowsNew = true,
                    //     Lite = false,
                    //     Execute = (pe, args)=>
                    //     {
                              //pe.State=ProcessState.Planned ;
                    //         pe.Save(); 
                    //     }
                //}.Register();

                new Execute(ProcessOperation.Plan)
                    {
                         FromStates = new []{ProcessState.Created, ProcessState.Canceled, ProcessState.Planned, ProcessState.Suspended},
                    ToState = ProcessState.Planned,
                         Execute = (pe, args)=>
                         {
                             pe.State = ProcessState.Planned;
                             pe.PlannedDate = (DateTime)args[0]; 
                         }
                }.Register();

                new Execute(ProcessOperation.Cancel)
                    {
                         FromStates = new []{ProcessState.Planned, ProcessState.Created, ProcessState.Suspended},
                    ToState = ProcessState.Canceled,
                         Execute = (pe, _)=>
                         {
                             pe.State = ProcessState.Canceled;
                             pe.CancelationDate = TimeZoneManager.Now; 
                         }
                }.Register();

                new Execute(ProcessOperation.Execute)
                    {
                         FromStates = new []{ProcessState.Created, ProcessState.Planned, ProcessState.Canceled, ProcessState.Suspended},
                    ToState = ProcessState.Queued,
                         Execute = (pe, _)=>
                         {
                        pe.SetAsQueue();
                         }
                }.Register();

                new Execute(ProcessOperation.Suspend)
                    {
                         FromStates = new []{ProcessState.Queued, ProcessState.Executing},
                    ToState = ProcessState.Suspending,
                         Execute = (pe, _)=>
                         {
                             pe.State = ProcessState.Suspending;
                             pe.SuspendDate = TimeZoneManager.Now;
                         }
                }.Register();
            }
        }

        public static ProcessExecutionDN Create(Enum processKey, params object[] args)
        {
            return EnumLogic<ProcessDN>.ToEntity(processKey).ConstructFrom<ProcessExecutionDN>(ProcessOperation.FromProcess, args);
        }

        public static ProcessExecutionDN Create(Enum processKey, IProcessDataDN processData)
        {
            return EnumLogic<ProcessDN>.ToEntity(processKey).ConstructFrom<ProcessExecutionDN>(ProcessOperation.FromProcess, processData);
        }

        public static ProcessExecutionDN Create(ProcessDN process, params object[] args)
        {
            return process.ConstructFrom<ProcessExecutionDN>(ProcessOperation.FromProcess, args);
        }

        public static ProcessExecutionDN Create(ProcessDN process, IProcessDataDN processData)
        {
            return process.ConstructFrom<ProcessExecutionDN>(ProcessOperation.FromProcess, processData);
        }

        public static void ExecuteTest(this ProcessExecutionDN pe)
        {
            pe.QueuedDate = TimeZoneManager.Now;
            var ep = new ExecutingProcess
            {
                Algorithm = registeredProcesses[EnumLogic<ProcessDN>.ToEnum(pe.Process.Key)],
                Data = pe.ProcessData,
                Execution = pe,
            };

            ep.Execute();
        }

        public static ProcessLogicState ExecutionState()
        {
            return new ProcessLogicState
            {
                Running = running,
                InitialDelayMiliseconds = InitialDelayMiliseconds,
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
    }

    public interface IProcessAlgorithm
    {
        IProcessDataDN CreateData(object[] args);

        void Execute(IExecutingProcess executingProcess);
    }

    public interface IExecutingProcess
    {
        Lite<UserDN> User { get; }
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

        public Lite<UserDN> User
        {
            get { return Execution.User; }
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
            using (AuthLogic.UserSession(Execution.User != null ? Execution.User.Retrieve() : AuthLogic.SystemUser))
            {
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
