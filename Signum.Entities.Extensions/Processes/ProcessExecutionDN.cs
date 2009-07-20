using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities.Reflection;
using Signum.Entities.Reflection;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Processes
{
    [Serializable]
    public class ProcessExecutionDN : Entity
    {
        private ProcessExecutionDN() { }

        public ProcessExecutionDN(ProcessDN process) 
        {
            this.process = process;
        }

        ProcessDN process;
        [NotNullValidator]
        public ProcessDN Process
        {
            get { return process; }
        }

        IProcessData processData;
        public IProcessData ProcessData
        {
            get { return processData; }
            set { Set(ref processData, value, "ProcessData"); }
        }

        ProcessState state;
        public ProcessState State
        {
            get { return state; }
            set { Set(ref state, value, "State"); }
        }

        DateTime creationDate = DateTime.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value, "CreationDate"); }
        }
        
        DateTime? plannedDate;
        public DateTime? PlannedDate
        {
            get { return plannedDate; }
            set { Set(ref plannedDate, value, "PlannedDate"); }
        }

        DateTime? cancelationDate;
        public DateTime? CancelationDate
        {
            get { return cancelationDate; }
            set { Set(ref cancelationDate, value, "CancelationDate"); }
        }

        DateTime? queuedDate;
        public DateTime? QueuedDate
        {
            get { return queuedDate; }
            set { Set(ref queuedDate, value, "QueuedDate"); }
        }

        DateTime? executionStart;
        public DateTime? ExecutionStart
        {
            get { return executionStart; }
            set { if (Set(ref executionStart, value, "ExecutionStart"))Notify("FechaFin"); }
        }

        DateTime? executionEnd;
        public DateTime? ExecutionEnd
        {
            get { return executionEnd; }
            set { if (Set(ref executionEnd, value, "ProcessEnd"))Notify("ExecutionStart"); }
        }

        DateTime? suspendDate;
        public DateTime? SuspendDate
        {
            get { return executionEnd; }
            set { Set(ref executionEnd, value, "SuspendDate"); }
        }

        decimal? progress;
        public decimal? Progress
        {
            get { return progress; }
            set { Set(ref progress, value, "Progress"); }
        }

        static StateValidator<ProcessExecutionDN, ProcessState> stateValidator = new StateValidator<ProcessExecutionDN, ProcessState>
        (e => e.State, e => e.ProcessData, e=>e.PlannedDate, e => e.CancelationDate, e => e.QueuedDate, e => e.ExecutionStart, e => e.ExecutionEnd, e => e.SuspendDate, e => e.Progress)
        {
       {ProcessState.Created,   null,      null,             null,                   null,              null,                  false,               null,                    null}, 
       {ProcessState.Planned,   null,      true,             null,                   null,              null,                  false,               null,                    null}, 
       {ProcessState.Canceled,  null,      true,             true,                   null,              null,                  false,               null,                    null}, 
       {ProcessState.Queued,    null,      null,             null,                   true,              false,                 false,               false,                   false},
       {ProcessState.Executing, true,      null,             null,                   true,              true,                  false,               false,                   true},
       {ProcessState.Suspending,true,      null,             null,                   true,              true,                  false,               true,                    true},
       {ProcessState.Suspended, true,      null,             null,                   true,              true,                  false,               true,                    true},
       {ProcessState.Finished,  true,      null,             null,                   true,              true,                  true,                false,                   false},
        }; 

        public override string this[string columnName]
        {
            get
            {
                string result = base[columnName];

                if (columnName == "ProcessStart" || columnName == "ProcessEnd")
                {
                    if (this.ExecutionEnd < this.ExecutionStart)
                        result = result.AddLine("Process Start es greater than Process End");

                    if (this.ExecutionStart == null && this.ExecutionEnd != null)
                        result = result.AddLine("Process Start is nulo but Process End is not");
                }

                result = result.AddLine(stateValidator.Validate(this, columnName)); 

                return result;
            }
        }

        public override string ToString()
        {
            switch (state)
            {
                case ProcessState.Created: return "{0} Created on {1}".Formato(process, creationDate);
                case ProcessState.Planned: return "{0} Planned on {1}".Formato(process, plannedDate);
                case ProcessState.Canceled: return "{0} Canceled on {1}".Formato(process, cancelationDate);
                case ProcessState.Queued: return "{0} Queued on {1}".Formato(process, queuedDate);
                case ProcessState.Executing: return "{0} Executing since {1}".Formato(process, executionStart);
                case ProcessState.Suspended: return "{0} Suspended on {1}".Formato(process, suspendDate );
                case ProcessState.Finished: return "{0} Finished on {1}".Formato(process, executionEnd);
                default: return "{0} ??".Formato(process);
            }
        }
    }

    public enum ProcessState
    {
        Created, 
        Planned,
        Canceled, 
        Queued,
        Executing,
        Suspending,
        Suspended,
        Finished
    }

    public enum ProcessOperation
    {
        FromProcess, 
        Plan,
        Cancel,
        Execute,
        Suspend,
    }

    public interface IProcessData : IIdentifiable
    {

    }
}
