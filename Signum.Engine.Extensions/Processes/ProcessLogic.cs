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

namespace Signum.Engine.Processes
{
    public static class ProcessLogic
    {
        static BlockingQueue<ExecutingProcess> processQueue = new BlockingQueue<ExecutingProcess>();

        static ImmutableAVLTree<int, ExecutingProcess> currentProcesses = ImmutableAVLTree<int, ExecutingProcess>.Empty;

        static Dictionary<Enum, IProcessAlgorithm> registeredProcesses = new Dictionary<Enum, IProcessAlgorithm>();

        static Thread[] threads;
        static int numberOfThreads;

        static Timer timer = new Timer(new TimerCallback(DispatchEvents), // main timer
								null,
								Timeout.Infinite,
								Timeout.Infinite);

        public static void AssertIsStarted(SchemaBuilder sb, bool packages)
        {
            if (!sb.ContainsDefinition<ProcessDN>())
                throw new ApplicationException("Call ProcessLogic.Start first");

            if (!sb.ContainsDefinition<PackageDN>())
                throw new ApplicationException("Call ProcessLogic.Start first with packages = true");
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, int numberOfThreads, bool packages)
        {
            if (packages && sb.NotDefined<PackageDN>())
            {
                sb.Include<PackageDN>();
                sb.Include<PackageLineDN>();

                OperationLogic.Register(new BasicExecute<ProcessDN>(TaskOperation.ExecutePrivate)
                {
                    Execute = (pc, _) => ProcessLogic.Create(pc).Execute(ProcessOperation.Execute)
                });

                dqm[typeof(PackageDN)] =
                     (from p in Database.Query<PackageDN>()
                      select new
                      {
                          Entity = p.ToLazy(),
                          p.Id,
                          Operation = p.Operation.ToLazy(),
                          Lines = (int?)Database.Query<PackageLineDN>().Count(pl=>pl.Package == p.ToLazy())
                      }).ToDynamic();

                dqm[typeof(PackageLineDN)] =
                    (from pl in Database.Query<PackageLineDN>()
                     select new
                     {
                         Entity = pl.ToLazy(),
                         Package = pl.Package,
                         pl.Id,
                         pl.Target,
                         pl.FinishTime,
                         pl.Exception
                     }).ToDynamic()
                     .ChangeColumn(a => a.Package, c => c.Visible = false)
                     .ChangeColumn(a => a.Target, c => c.Filterable = false); 
            }

            if (sb.NotDefined<ProcessDN>())
            {
                sb.Include<ProcessDN>();
                sb.Include<ProcessExecutionDN>();

                ProcessLogic.numberOfThreads = numberOfThreads;

                EnumLogic<ProcessDN>.Start(sb, () => registeredProcesses.Keys.ToHashSet());

                OperationLogic.AssertIsStarted(sb);
                AuthLogic.AssertIsStarted(sb); 
                new ExecutingProcessGraph().Register();   

                sb.Schema.Initializing += Schema_Initializing;
                sb.Schema.Saved += Schema_Saved;

                dqm[typeof(ProcessDN)] =
                             (from p in Database.Query<ProcessDN>()
                              join pe in Database.Query<ProcessExecutionDN>().DefaultIfEmpty() on p equals pe.Process into g
                              select new
                              {
                                  Entity = p.ToLazy(),
                                  p.Id,
                                  p.Name,
                                  NumExecutions = (int?)g.Count(),
                                  LastExecution = (from pe2 in Database.Query<ProcessExecutionDN>()
                                                   where pe2.Id == g.Max(a => (int?)a.Id)
                                                   select pe2.ToLazy()).FirstOrDefault()
                              }).ToDynamic()
                              .ChangeColumn(a => a.NumExecutions, a => a.DisplayName = Resources.Executions)
                              .ChangeColumn(a => a.LastExecution, a => a.DisplayName = Resources.LastExecution);

                dqm[typeof(ProcessExecutionDN)] = 
                             (from pe in Database.Query<ProcessExecutionDN>()
                              select new
                              {
                                  Entity = pe.ToLazy(),
                                  pe.Id,
                                  Resume = pe.ToStr,
                                  Process = pe.Process.ToLazy(),
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

                dqm[ProcessQueries.CurrentExecutions] =
                             (from pe in Database.Query<ProcessExecutionDN>()
                              where
                                  pe.State == ProcessState.Queued ||
                                  pe.State == ProcessState.Executing ||
                                  pe.State == ProcessState.Suspending ||
                                  pe.State == ProcessState.Suspended
                              select new
                              {
                                  Entity = pe.ToLazy(),
                                  pe.Id,
                                  Resume = pe.ToStr,
                                  Process = pe.Process.ToLazy(),
                                  State = pe.State,
                                  pe.QueuedDate,
                                  pe.ExecutionStart,
                                  pe.ExecutionEnd,
                                  pe.Progress,
                                  pe.SuspendDate,
                              }).ToDynamic();

                dqm[ProcessQueries.ErrorExecutions] =
                             (from pe in Database.Query<ProcessExecutionDN>()
                              where
                                  pe.State == ProcessState.Error
                              select new
                              {
                                  Entity = pe.ToLazy(),
                                  pe.Id,
                                  Resume = pe.ToStr,
                                  Process = pe.Process.ToLazy(),
                                  pe.CreationDate,
                                  pe.PlannedDate,
                                  pe.CancelationDate,
                                  pe.QueuedDate,
                                  pe.ExecutionStart,
                                  pe.ExecutionEnd,
                                  pe.Progress,
                                  pe.SuspendDate,
                                  ErrorDate = pe.ExceptionDate,
                                  pe.Exception
                              }).ToDynamic(); 
            }
        }

        static void Schema_Saved(Schema sender, IdentifiableEntity ident)
        {
            ProcessExecutionDN pe = ident as ProcessExecutionDN;
            if (pe != null)
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
                        Transaction.RealCommit += ()=>Execute(pe);
                        break;
                }
            }
        }

        static void Execute(ProcessExecutionDN pe)
        {
            var ep = new ExecutingProcess
            {
                Algorithm = registeredProcesses[EnumLogic<ProcessDN>.ToEnum(pe.Process.Key)],
                Data = pe.ProcessData,
                Execution = pe,
            };

            processQueue.Enqueue(ep);
            Sync.SafeUpdate(ref currentProcesses, tree => tree.Add(pe.Id, ep));
        }

        static void Suspend(int processExecutionId)
        {
            ExecutingProcess process;
            if (!currentProcesses.TryGetValue(processExecutionId, out process))
                throw new ApplicationException("ProcessExecution {0} is not running anymore".Formato(processExecutionId));

            process.Suspend();
        }

        static void Schema_Initializing(Schema sender)
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
                    pe.Queue();
                    pe.Save(); 
                }

                threads = 0.To(numberOfThreads).Select(i => new Thread(DoWork)).ToArray();

                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Start(i);
                }

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
                    TimeSpan ts = next.Value - DateTime.Now;
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
                           where pe.State == ProcessState.Planned && pe.PlannedDate <= DateTime.Now
                           orderby pe.PlannedDate
                           select pe).ToArray();

                foreach (var pe in pes)
                {
                    pe.Queue();
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

        static void DoWork(object number)
        {
            using (AuthLogic.User(AuthLogic.SystemUser))
            {
                foreach (var ep in processQueue)
                {
                    try
                    {
                        ep.Execute();
                    }
                    finally
                    {
                        Sync.SafeUpdate(ref currentProcesses, tree => tree.Remove(ep.Execution.Id));
                    }
                }
            }
        }

        public class ExecutingProcessGraph : Graph<ProcessExecutionDN, ProcessState>
        {
            public ExecutingProcessGraph()
            {
                this.GetState = e => e.State;
                this.Operations = new List<IGraphOperation>()
                {   
                    new ConstructFrom<ProcessDN>(ProcessOperation.FromProcess, ProcessState.Created)
                    {
                         Construct = (process, args)=>
                         {
                             IProcessDataDN data;
                             if(args != null && args.Length != 0 && args[0] is IProcessDataDN)
                             {
                                 data = (IProcessDataDN)args[0];  
                             }
                             else 
                             {
                                 IProcessAlgorithm processAlgorithm = registeredProcesses[EnumLogic<ProcessDN>.ToEnum(process.Key)]; 
                                 data = processAlgorithm.CreateData(args); 
                             } 

                             return new ProcessExecutionDN(process)
                             {
                                 State = ProcessState.Created,
                                 ProcessData = data
                             }.Save();
                         }
                    },
                    new Goto(ProcessOperation.Plan, ProcessState.Planned)
                    {
                         FromStates = new []{ProcessState.Created, ProcessState.Canceled, ProcessState.Planned, ProcessState.Suspended},
                         Execute = (pe, args)=>
                         {
                             pe.State = ProcessState.Planned;
                             pe.PlannedDate = (DateTime)args[0]; 
                         }
                    },
                    new Goto(ProcessOperation.Cancel, ProcessState.Canceled)
                    {
                         FromStates = new []{ProcessState.Planned, ProcessState.Created, ProcessState.Suspended},
                         Execute = (pe, _)=>
                         {
                             pe.State = ProcessState.Canceled;
                             pe.CancelationDate = DateTime.Now; 
                         }
                    },
                    new Goto(ProcessOperation.Execute, ProcessState.Queued)
                    {
                         FromStates = new []{ProcessState.Created, ProcessState.Planned, ProcessState.Canceled, ProcessState.Suspended},
                         Execute = (pe, _)=>
                         {
                            pe.Queue();
                            pe.Save();                            
                         }
                    },
                    new Goto(ProcessOperation.Suspend, ProcessState.Suspending)
                    {
                         FromStates = new []{ProcessState.Queued, ProcessState.Executing},
                         Execute = (pe, _)=>
                         {
                             pe.State = ProcessState.Suspending;
                             pe.SuspendDate = DateTime.Now;
                         }
                    }
                };
            }
        }

        public static ProcessExecutionDN Create(Enum processKey, params object[] args)
        {
            return Create(EnumLogic<ProcessDN>.ToEntity(processKey), args);
        }

        public static ProcessExecutionDN Create(Enum processKey, IProcessDataDN processData)
        {
            return Create(EnumLogic<ProcessDN>.ToEntity(processKey), processData);
        }

        public static ProcessExecutionDN Create(ProcessDN process, params object[] args)
        {
            return process.ConstructFrom<ProcessExecutionDN>(ProcessOperation.FromProcess, args); 
        }

        public static ProcessExecutionDN Create(ProcessDN process, IProcessDataDN processData)
        {
            return process.ConstructFrom<ProcessExecutionDN>(ProcessOperation.FromProcess, processData);
        }
    }

    public interface IProcessAlgorithm
    {
        IProcessDataDN CreateData(object[] args);
        FinalState Execute(IExecutingProcess executingProcess);
    }

    public interface IExecutingProcess
    {
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

        public void ProgressChanged(decimal progress)
        {
            Execution.Progress = progress;
            Execution.Save();
        }

        public void Execute()
        {
            Execution.State = ProcessState.Executing;
            Execution.ExecutionStart = DateTime.Now;
            Execution.Progress = 0;
            Execution.Save();

            try
            {

                FinalState state = Algorithm.Execute(this);

                if (state == FinalState.Finished)
                {
                    Execution.ExecutionEnd = DateTime.Now;
                    Execution.State = ProcessState.Finished;
                    Execution.Progress = null;
                    Execution.Save();
                }
                else
                {
                    Execution.SuspendDate = DateTime.Now;
                    Execution.State = ProcessState.Suspended;
                    Execution.Save();
                }
            }
            catch (Exception e)
            {
                using (Transaction tr = new Transaction(true))
                {
                    Execution.State = ProcessState.Error;
                    Execution.ExceptionDate = DateTime.Today;
                    Execution.Exception = e.Message;
                    Execution.Save();

                    tr.Commit();
                }
            }
        }

        public void Suspend()
        {
            Suspended = true;
        }
    }
}
