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

namespace Signum.Engine.Processes
{
    public static class ProcessLogic
    {
        static BlockingQueue<ExecutingProcess> processQueue = new BlockingQueue<ExecutingProcess>();

        static ImmutableAVLTree<int, ExecutingProcess> currentProcesses = ImmutableAVLTree<int, ExecutingProcess>.Empty;

        static Dictionary<Enum, IProcessLogic> registeredProcesses = new Dictionary<Enum, IProcessLogic>();

        static Thread[] threads;
        static int numberOfThreads;

        static Timer timer;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, int numberOfThreads, bool packages)
        {
            if (packages && sb.NotDefined<PackageDN>())
            {
                sb.Include<PackageDN>();
                sb.Include<PackageLineDN>();

                if (!sb.Settings.IsTypeAttributesOverriden<IProcessData>())
                    sb.Settings.OverrideTypeAttributes<IProcessData>(new ImplementedByAttribute(typeof(PackageDN)));

                dqm[typeof(PackageLineDN)] =
                    (from pl in Database.Query<PackageLineDN>()
                     select new
                     {
                         Entity = pl.ToLazy(),
                         pl.Id,
                         pl.Package,
                         pl.Target,
                         pl.FinishTime,
                         pl.Exception
                     }).ToDynamic(); 



            }

            if (sb.NotDefined<ProcessDN>())
            {
                sb.Include<ProcessDN>();
                sb.Include<ProcessExecutionDN>();

                ProcessLogic.numberOfThreads = numberOfThreads;

                EnumBag<ProcessDN>.Start(sb, () => registeredProcesses.Keys.ToHashSet());

                OperationLogic.AssertIsLoaded(sb); 
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
                                  NumExecutions = g.Count(),
                                  LastExecution = (from pe2 in Database.Query<ProcessExecutionDN>()
                                                   where pe2.Id == g.Max(a => a.Id)
                                                   select pe2.ToLazy()).FirstOrDefault()
                              }).ToDynamic();

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
                                  pe.ErrorDate,
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
                                  pe.ErrorDate,
                                  pe.Exception
                              }).ToDynamic(); 
            }
        }

        static void Schema_Saved(Schema sender, IdentifiableEntity ident)
        {
            ProcessExecutionDN process = ident as ProcessExecutionDN;
            if (process != null)
            {
                switch (process.State)
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
                        Suspend(process.Id);
                        break;
                }
            }
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
                                 pe.State == ProcessState.Queued
                           select pe).AsEnumerable().OrderByDescending(pe => pe.State).ToArray();

                foreach (var pe in pes)
                {
                    processQueue.Enqueue(CreateExecutingProcess(pe));
                }

                threads = 0.To(numberOfThreads).Select(i => new Thread(DoWork)).ToArray();

                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Start(i);
                }

                timer = new Timer(DispatchEvents);

                RefreshPlan();
            }
        }

        static void RefreshPlan()
        {
            DateTime? next = Database.Query<ProcessExecutionDN>().Where(pe => pe.State == ProcessState.Planned).Min(pe => pe.PlannedDate);
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

        static void DispatchEvents(object obj)
        {
            var pes = (from pe in Database.Query<ProcessExecutionDN>()
                       where pe.State == ProcessState.Planned && pe.PlannedDate <= DateTime.Now
                       orderby pe.PlannedDate
                       select pe).ToArray();

            foreach (var pe in pes)
            {
                pe.State = ProcessState.Queued;
                pe.ExecutionStart = null;
                pe.ExecutionEnd = null;
                pe.SuspendDate = null;
                pe.Progress = null;
                pe.Save();

                ExecutingProcess ep = CreateExecutingProcess(pe);

                Sync.SafeUpdate(ref currentProcesses, tree => tree.Add(pe.Id, ep));

                processQueue.Enqueue(ep);
            }

            RefreshPlan();
        }

        public static void Register(Enum processKey, IProcessLogic logic)
        {
            if (processKey == null)
                throw new ArgumentNullException("processKey");

            if (logic == null)
                throw new ArgumentNullException("logic");

            registeredProcesses.Add(processKey, logic); 
        }

        static ExecutingProcess CreateExecutingProcess(ProcessExecutionDN pe)
        {
            return new ExecutingProcess
            {
                Logic = registeredProcesses[EnumBag<ProcessDN>.ToEnum(pe.Process.Key)],
                Data = pe.ProcessData,
                Execution = pe,
            };
        }

        static void DoWork(object number)
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

        public class ExecutingProcessGraph : Graph<ProcessExecutionDN, ProcessState>
        {
            public ExecutingProcessGraph()
            {
                this.GetState = e => e.State;
                this.Operations = new List<IGraphOperation>()
                {   
                    new ConstructFrom<ProcessDN>(ProcessOperation.FromProcess, ProcessState.Created)
                    {
                         FromEntity = (process, args)=>
                         {
                             IProcessData data;
                             if(args.Length != 0 && args[0] is IProcessData)
                             {
                                 data = (IProcessData)args[0];  
                             }
                             else 
                             {
                                 IProcessLogic processLogic = registeredProcesses[EnumBag<ProcessDN>.ToEnum(process.Key)]; 
                                 data = processLogic.CreateData(args); 
                             } 

                             return new ProcessExecutionDN(process)
                             {
                                 State = ProcessState.Created,
                                 ProcessData = data
                             };
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
                             pe.PlannedDate = DateTime.Now; 
                         }
                    },
                    new Goto(ProcessOperation.Execute, ProcessState.Queued)
                    {
                         FromStates = new []{ProcessState.Created, ProcessState.Planned, ProcessState.Canceled, ProcessState.Suspended},
                         Execute = (pe, _)=>
                         {
                             pe.State = ProcessState.Queued;
                             pe.QueuedDate = DateTime.Now; 
                             pe.ExecutionStart = null;
                             pe.ExecutionEnd = null;
                             pe.Progress = null; 
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

        internal static Lazy<ProcessExecutionDN> Create(Enum processKey, params object[] args)
        {
            return Create(EnumBag<ProcessDN>.ToEntity(processKey), args);
        }

        internal static Lazy<ProcessExecutionDN> Create(Enum processKey, IProcessData processData)
        {
            return Create(EnumBag<ProcessDN>.ToEntity(processKey), processData);
        }

        internal static Lazy<ProcessExecutionDN> Create(ProcessDN process, params object[] args)
        {
            return process.ConstructFrom<ProcessExecutionDN>(ProcessOperation.FromProcess, args).ToLazy(); 
        }

        internal static Lazy<ProcessExecutionDN> Create(ProcessDN process, IProcessData processData)
        {
            return process.ConstructFrom<ProcessExecutionDN>(ProcessOperation.FromProcess, processData).ToLazy();
        }
    }

    public interface IProcessLogic
    {
        IProcessData CreateData(object[] args);
        FinalState Execute(IExecutingProcess executingProcess);
    }

    public interface IExecutingProcess
    {
        IProcessData Data { get; }
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
        public IProcessLogic Logic { get; set; }
        public IProcessData Data { get; set; }

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
            Execution.Save();

            try
            {

                FinalState state = Logic.Execute(this);

                if (state == FinalState.Finished)
                {
                    Execution.ExecutionEnd = DateTime.Now;
                    Execution.State = ProcessState.Finished;
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
                    Execution.ErrorDate = DateTime.Today;
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
