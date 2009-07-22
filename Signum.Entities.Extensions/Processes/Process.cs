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
using System.ComponentModel;
using Signum.Entities.Extensions.Properties;
using Signum.Entities.Basics;

namespace Signum.Entities.Processes
{
    [Serializable]
    public class ProcessDN : EnumDN
    {
    }

    [Serializable]
    public class ProcessExecutionDN : IdentifiableEntity
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
            set { if (Set(ref executionStart, value, "ExecutionStart"))Notify("ExecutionEnd"); }
        }

        DateTime? executionEnd;
        public DateTime? ExecutionEnd
        {
            get { return executionEnd; }
            set { if (Set(ref executionEnd, value, "ExecutionEnd"))Notify("ExecutionStart"); }
        }

        DateTime? suspendDate;
        public DateTime? SuspendDate
        {
            get { return suspendDate; }
            set { Set(ref suspendDate, value, "SuspendDate"); }
        }

        DateTime? errorDate;
        public DateTime? ErrorDate
        {
            get { return errorDate; }
            set { Set(ref errorDate, value, "ErrorDate"); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string exception;
        public string Exception
        {
            get { return exception; }
            set { Set(ref exception, value, "Exception"); }
        }

        decimal? progress;
        public decimal? Progress
        {
            get { return progress; }
            set { Set(ref progress, value, "Progress"); }
        }

        static StateValidator<ProcessExecutionDN, ProcessState> stateValidator = new StateValidator<ProcessExecutionDN, ProcessState>
        (e => e.State, e=>e.PlannedDate, e => e.CancelationDate, e => e.QueuedDate, e => e.ExecutionStart, e => e.ExecutionEnd, e => e.SuspendDate, e => e.Progress, e=>e.ErrorDate, e=>e.Exception)
        {
       {ProcessState.Created,   false,   false,                  false,             false,                 false,               false,              false,            false,         false}, 
       {ProcessState.Planned,   true,    null,                   null,              null,                  false,               null,               null,             null,          null}, 
       {ProcessState.Canceled,  true,    true,                   null,              null,                  false,               null,               null,             null,          null}, 
       {ProcessState.Queued,    null,    null,                   true,              false,                 false,               false,              false,            false,         false},
       {ProcessState.Executing, null,    null,                   true,              true,                  false,               false,              true,             false,         false},
       {ProcessState.Suspending,null,    null,                   true,              true,                  false,               true,               true,             false,         false},
       {ProcessState.Suspended, null,    null,                   true,              true,                  false,               true,               true,             false,         false},
       {ProcessState.Finished,  null,    null,                   true,              true,                  true,                false,              false,            false,         false},
       {ProcessState.Error,     null,    null,                   null,              null,                  null,                null,               null,             true,          true},
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
                case ProcessState.Created: return "{0} {1} on {2}".Formato(process, ProcessState.Created.NiceToString(), creationDate);
                case ProcessState.Planned: return "{0} {1} for {2}".Formato(process, ProcessState.Planned.NiceToString(), plannedDate);
                case ProcessState.Canceled: return "{0} {1} on {2}".Formato(process, ProcessState.Canceled.NiceToString(), cancelationDate);
                case ProcessState.Queued: return "{0} {1} on {2}".Formato(process, ProcessState.Queued.NiceToString(), queuedDate);
                case ProcessState.Executing: return "{0} {1} since {2}".Formato(process, ProcessState.Executing.NiceToString(), executionStart);
                case ProcessState.Suspending: return "{0} {1} since {2}".Formato(process, ProcessState.Suspending.NiceToString(), suspendDate);
                case ProcessState.Suspended: return "{0} {1} on {2}".Formato(process, ProcessState.Suspended.NiceToString(), suspendDate);
                case ProcessState.Finished: return "{0} {1} on {2}".Formato(process, ProcessState.Finished.NiceToString(), executionEnd);
                case ProcessState.Error: return "{0} {1} on {2}".Formato(process, ProcessState.Error.NiceToString(), executionEnd);
                default: return "{0} ??".Formato(process);
            }
        }
    }

    public interface IProcessData : IIdentifiable
    {

    }

    public enum ProcessState
    {
        [LocDescription]
        Created,
        [LocDescription]
        Planned,
        [LocDescription]
        Canceled,
        [LocDescription]
        Queued,
        [LocDescription]
        Executing,
        [LocDescription]
        Suspending,
        [LocDescription]
        Suspended,
        [LocDescription]
        Finished,
        [LocDescription]
        Error,
    }

    public enum ProcessOperation
    {
        [LocDescription]
        FromProcess,
        [LocDescription]
        Plan,
        [LocDescription]
        Cancel,
        [LocDescription]
        Execute,
        [LocDescription]
        Suspend,
    }

    public enum ProcessQueries
    {
        CurrentExecutions,
        ErrorExecutions,
    }
}
