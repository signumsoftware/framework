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
using Signum.Entities.Logging;

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

        static BlockingCollection<ExecutingProcess> processQueue = new BlockingCollection<ExecutingProcess>();

        static Dictionary<Enum, IProcessAlgorithm> registeredProcesses = new Dictionary<Enum, IProcessAlgorithm>();

        static int numberOfThreads;

        static Timer timer = new Timer(new TimerCallback(DispatchEvents), // main timer
                                null,
                                Timeout.Infinite,
                                Timeout.Infinite);

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => ProcessLogic.Start(null, null, 0)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, int numberOfThreads)
        {

            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ProcessDN>();
                sb.Include<ProcessExecutionDN>();

                ProcessLogic.numberOfThreads = numberOfThreads;

                EnumLogic<ProcessDN>.Start(sb, () => registeredProcesses.Keys.ToHashSet());

                OperationLogic.AssertStarted(sb);
                AuthLogic.AssertStarted(sb);
                ProcessExecutionGraph.Register();

                SchedulerLogic.ExecuteTask.Register((ProcessDN p) =>
                    ProcessLogic.Create(p).Execute(ProcessOperation.Execute));

                sb.Schema.Initializing[InitLevel.Level4BackgroundProcesses] += Schema_InitializingApplication;
                sb.Schema.EntityEvents<ProcessExecutionDN>().Saving += ProcessExecution_Saving;

                dqm[typeof(ProcessDN)] =
                             (from p in Database.Query<ProcessDN>()
                              join pe in Database.Query<ProcessExecutionDN>().DefaultIfEmpty() on p equals pe.Process into g
                              select new
                              {
                                  Entity = p.ToLite(),
                                  p.Id,
                                  p.Name
                              }).ToDynamic();

                dqm[typeof(ProcessExecutionDN)] =
                             (from pe in Database.Query<ProcessExecutionDN>()
                              select new
                              {
                                  Entity = pe.ToLite(),
                                  pe.Id,
                                  Resume = pe.ToStr,
                                  Process = pe.Process.ToLite(),
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
                Transaction.PostRealCommit += () =>
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
                            Suspend(pe.Id);
                            break;
                        case ProcessState.Queued:
                            Execute(pe);
                            break;
                    }
                };
        }

        static void Execute(ProcessExecutionDN pe)
        {
            if (pe.State != ProcessState.Queued || processQueue.Any(a=>a.Execution.Id == pe.Id))
                return; 

            var ep = new ExecutingProcess
            {
                Algorithm = registeredProcesses[EnumLogic<ProcessDN>.ToEnum(pe.Process.Key)],
                Data = pe.ProcessData,
                Execution = pe,
            };

            processQueue.Add(ep);
        }

        static void Suspend(int processExecutionId)
        {
            ExecutingProcess process = processQueue.SingleOrDefault(p => p.Execution.Id == processExecutionId);

            if (process == null)
                throw new ApplicationException(Resources.ProcessExecution0IsNotRunningAnymore.Formato(processExecutionId));

            process.Suspend();
        }

        static void Schema_InitializingApplication()
        {
            using (new EntityCache(true))
            {
                var pes = (from pe in Database.Query<ProcessExecutionDN>()
                           where pe.State == ProcessState.Executing ||
                                 pe.State == ProcessState.Queued ||
                                 pe.State == ProcessState.Suspending
                           select pe).AsEnumerable().OrderByDescending(pe => pe.State).ToArray();

                foreach (var pe in pes)
                {
                    pe.SetAsQueue();
                    using (OperationLogic.AllowSave<ProcessExecutionDN>())
                        pe.Save();
                }

                

                Task.Factory.StartNew(DoWork, TaskCreationOptions.LongRunning); 
           
                RefreshPlan();
            }
        }

        static void RefreshPlan()
        {
            using (new EntityCache(true))
            using (AuthLogic.Disable())
            {
                DateTime? next = Database.Query<ProcessExecutionDN>()
                    .Where(pe => pe.State == ProcessState.Planned)
                    .Min(pe => pe.PlannedDate);
                if (next == null)
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                else
                {
                    TimeSpan ts = next.Value - TimeZoneManager.Now;
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

        public static int InitialDelayMiliseconds = 30 * 1000;
        public static ParallelOptions ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 }; 

        static void DoWork()
        {
            Thread.Sleep(InitialDelayMiliseconds);

            Parallel.ForEach(processQueue.GetConsumingEnumerable(), ParallelOptions, ep =>
            {
                using (UserDN.Scope(AuthLogic.SystemUser))
                {
                    ep.Execute();
                }
            });
        }

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
                    Construct = (process, args) =>
                    {
                        return Create(process, EnumLogic<ProcessDN>.ToEnum(process), args);
                    }
                }.Register();

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
                    FromStates = new[] { ProcessState.Planned, ProcessState.Created, ProcessState.Suspended },
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
                    FromStates = new[] { ProcessState.Queued, ProcessState.Executing },
                    ToState = ProcessState.Suspending,
                    Execute = (pe, _) =>
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
    }

    public interface IProcessAlgorithm
    {
        IProcessDataDN CreateData(object[] args);
        FinalState Execute(IExecutingProcess executingProcess);
    }

    public interface IExecutingProcess
    {
        Lite<UserDN> User { get; }
        IProcessDataDN Data { get; }
        bool Suspended { get; }
        void ProgressChanged(decimal progress);
    }

    public enum FinalState
    {
        Suspended,
        Finished
    }

    internal class ExecutingProcess : IExecutingProcess
    {
        public ProcessExecutionDN Execution { get; set; }
        public IProcessAlgorithm Algorithm { get; set; }
        public IProcessDataDN Data { get; set; }

        public bool Suspended { get; private set; }

        public Lite<UserDN> User
        {
            get { return Execution.User; }
        }

        public void ProgressChanged(decimal progress)
        {
            Execution.Progress = progress;
            using (OperationLogic.AllowSave<ProcessExecutionDN>())
            Execution.Save();
        }

        public void Execute()
        {
            using (ScopeSessionFactory.OverrideSession())
            {
                UserDN.SetSessionUser(AuthLogic.SystemUser);

                Execution.State = ProcessState.Executing;
                Execution.ExecutionStart = TimeZoneManager.Now;
                Execution.Progress = 0;
                using (OperationLogic.AllowSave<ProcessExecutionDN>())
                    Execution.Save();

                try
                {
                    FinalState state = Algorithm.Execute(this);

                    if (state == FinalState.Finished)
                    {
                        Execution.ExecutionEnd = TimeZoneManager.Now;
                        Execution.State = ProcessState.Finished;
                        Execution.Progress = null;
                        using (OperationLogic.AllowSave<ProcessExecutionDN>())
                            Execution.Save();
                    }
                    else
                    {
                        Execution.SuspendDate = TimeZoneManager.Now;
                        Execution.State = ProcessState.Suspended;
                        using (OperationLogic.AllowSave<ProcessExecutionDN>())
                            Execution.Save();
                    }

                }
                catch (Exception e)
                {
                    Execution.State = ProcessState.Error;
                    Execution.ExceptionDate = TimeZoneManager.Now;
                    Execution.Exception = new ExceptionLogDN(e)
                    {
                        ActionName = Execution.Process.ToString()
                    }.Save().ToLite();
                    using (OperationLogic.AllowSave<ProcessExecutionDN>())
                        Execution.Save();
                }
            }
        }

        public void Suspend()
        {
            Suspended = true;
        }

        public override string ToString()
        {
            return "Execution (ID = {0}): {1} ".Formato(Execution.Id, Execution);
        }
    }
}
