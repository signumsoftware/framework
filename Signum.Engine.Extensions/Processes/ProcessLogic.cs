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

namespace Signum.Engine.Processes
{
    public static class ProcessLogic
    {
        static List<ExecutingProcess> executingProcess = new List<ExecutingProcess>();
        static Dictionary<Enum, IProcess> registeredProcesses = new Dictionary<Enum,IProcess>();

        internal static HashSet<Enum> ProcessKeys;
        internal static Dictionary<Enum, ProcessDN> ToProcess;
        internal static Dictionary<string, Enum> ToEnum;


        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined<ProcessDN>())
            {
                sb.Include<ProcessDN>();
                sb.Include<ProcessExecutionDN>();

                sb.Schema.Initializing += Schema_Initializing;
                sb.Schema.Synchronizing += Schema_Synchronizing;
                sb.Schema.Generating += Schema_Generating;
            }
        }

        static void Schema_Initializing(Schema sender)
        {
            using (AuthLogic.Disable())
            using (new EntityCache(true))
            {
                ProcessKeys = registeredProcesses.Keys.ToHashSet();

                ToProcess = EnumerableExtensions.JoinStrict(
                     Database.RetrieveAll<ProcessDN>(),
                     ProcessKeys,
                     a => a.Key,
                     k => EnumExtensions.UniqueKey(k),
                     (a, k) => new { a, k }, "Caching ProcessDN").ToDictionary(p => p.k, p => p.a);

                ToEnum = ToProcess.Keys.ToDictionary(k => EnumExtensions.UniqueKey(k));
            }
        }

        static SqlPreCommand Schema_Generating()
        {
            Table table = Schema.Current.Table<ProcessDN>();

            return GetProcesses().Select(a => table.InsertSqlSync(a)).Combine(Spacing.Simple);
        }

        const string ProcessKey = "Processes";
        static SqlPreCommand Schema_Synchronizing(Replacements replacements)
        {
            Table table = Schema.Current.Table<ProcessDN>();

            List<ProcessDN> current = Administrator.TryRetrieveAll<ProcessDN>(replacements);

            return Synchronizer.SyncronizeReplacing(replacements, ProcessKey,
                current.ToDictionary(c => c.Key),
                GetProcesses().ToDictionary(s => s.Key),
                (k, c) => table.DeleteSqlSync(c),
                (k, s) => table.InsertSqlSync(s),
                (k, c, s) =>
                {
                    c.Name = s.Name;
                    c.Key = s.Key;
                    return table.UpdateSqlSync(c);
                }, Spacing.Double);
        }

        static List<ProcessDN> GetProcesses()
        {
            return registeredProcesses.Keys.Select(k => ProcessDN.FromEnum(k)).ToList();
        }

        class ExecutingProcessGraph : Graph<ProcessExecutionDN, ProcessState>
        {
            public ExecutingProcessGraph()
            {
                this.GetState = e => e.State;
                this.Operations = new List<IGraphOperation>()
                {   
                    new ConstructFrom<ProcessDN>(ProcessOperation.FromProcess, ProcessState.Created)
                    {
                         FromLazy = (lazy, args)=>
                         {
                             IProcessData data = args.Length > 0? (IProcessData)args[0]: null;

                             return new ProcessExecutionDN(lazy.Retrieve())
                             {
                                 State = ProcessState.Created,
                                 ProcessData = data
                             };
                         }
                    },
                    new Goto(ProcessOperation.Plan, ProcessState.Planned)
                    {
                         FromStates = new []{ProcessState.Created, ProcessState.Canceled, ProcessState.Planned},
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
    }

    public interface IProcess
    {
        IProcessData Create(object[] args); 
        void Execute(IProcessData processData);
        bool Suspended { get; set; }
        event Action<ProcessState, decimal> ProgressChanged; 
    }

    public enum ProgressState
    {
        Executing, 
        Suspended,
        Finished
    }

    public class ExecutingProcess
    {
        ProcessExecutionDN databasEntity;
        IProcessData processData; 
        IProcess process;
    }

    //public class Package : IProcess
    //{
    //    public IProcessData CreateData(object[] args)
    //    {
    //        return new PackageDN { Operation = (OperationDN)args[0] }; 
    //    }

    //    public void Execute(IProcessData processData)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool Suspended {get; set;}

    //    public event Action<ProcessState, decimal> ProgressChanged;
    //}
}
