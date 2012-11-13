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
using Signum.Entities.Scheduler;
using Signum.Entities.Authorization;
using Signum.Entities.Exceptions;

namespace Signum.Entities.Processes
{
    [Serializable]
    public class ProcessDN : MultiEnumDN
    {
    }

    [Serializable]
    public class ProcessExecutionDN : IdentifiableEntity
    {
        internal ProcessExecutionDN() { }

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

        IProcessDataDN processData;
        public IProcessDataDN ProcessData
        {
            get { return processData; }
            set { Set(ref processData, value, () => ProcessData); }
        }

        [ImplementedBy(typeof(UserProcessSessionDN))]
        ISessionDataDN sessionData;
        public ISessionDataDN SessionData
        {
            get { return sessionData; }
            set { Set(ref sessionData, value, () => SessionData); }
        }

        ProcessState state;
        public ProcessState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value, () => CreationDate); }
        }

        DateTime? plannedDate;
        public DateTime? PlannedDate
        {
            get { return plannedDate; }
            set { Set(ref plannedDate, value, () => PlannedDate); }
        }

        DateTime? cancelationDate;
        public DateTime? CancelationDate
        {
            get { return cancelationDate; }
            set { Set(ref cancelationDate, value, () => CancelationDate); }
        }

        DateTime? queuedDate;
        public DateTime? QueuedDate
        {
            get { return queuedDate; }
            set { Set(ref queuedDate, value, () => QueuedDate); }
        }

        DateTime? executionStart;
        public DateTime? ExecutionStart
        {
            get { return executionStart; }
            set { if (Set(ref executionStart, value, () => ExecutionStart))Notify(() => ExecutionEnd); }
        }

        DateTime? executionEnd;
        public DateTime? ExecutionEnd
        {
            get { return executionEnd; }
            set { if (Set(ref executionEnd, value, () => ExecutionEnd))Notify(() => ExecutionStart); }
        }

        DateTime? suspendDate;
        public DateTime? SuspendDate
        {
            get { return suspendDate; }
            set { Set(ref suspendDate, value, () => SuspendDate); }
        }

        DateTime? exceptionDate;
        public DateTime? ExceptionDate
        {
            get { return exceptionDate; }
            set { Set(ref exceptionDate, value, () => ExceptionDate); }
        }

        [SqlDbType(Size = int.MaxValue)]
        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }

        decimal? progress;
        [NumberBetweenValidator(0, 100)]
        public decimal? Progress
        {
            get { return progress; }
            set { Set(ref progress, value, () => Progress); }
        }

        static StateValidator<ProcessExecutionDN, ProcessState> stateValidator = new StateValidator<ProcessExecutionDN, ProcessState>
        (e => e.State, e => e.PlannedDate, e => e.CancelationDate, e => e.QueuedDate, e => e.ExecutionStart, e => e.ExecutionEnd, e => e.SuspendDate, e => e.Progress, e => e.ExceptionDate, e => e.Exception)
        {
       {ProcessState.Created,   false,   false,                  false,             false,                 false,               false,              false,            false,         false}, 
       {ProcessState.Planned,   true,    null,                   null,              null,                  false,               null,               null,             null,          null}, 
       {ProcessState.Canceled,  null,    true,                   null,              null,                  false,               null,               null,             null,          null}, 
       {ProcessState.Queued,    null,    null,                   true,              false,                 false,               false,              false,            false,         false},
       {ProcessState.Executing, null,    null,                   true,              true,                  false,               false,              true,             false,         false},
       {ProcessState.Suspending,null,    null,                   true,              true,                  false,               true,               true,             false,         false},
       {ProcessState.Suspended, null,    null,                   true,              true,                  false,               true,               true,             false,         false},
       {ProcessState.Finished,  null,    null,                   true,              true,                  true,                false,              false,            false,         false},
       {ProcessState.Error,     null,    null,                   null,              null,                  null,                null,               null,             true,          true},
        };

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => ExecutionStart) || pi.Is(() => ExecutionEnd))
            {
                if (this.ExecutionEnd < this.ExecutionStart)
                    return Resources.ProcessStartIsGreaterThanProcessEnd;

                if (this.ExecutionStart == null && this.ExecutionEnd != null)
                    return Resources.ProcessStartIsNullButProcessEndIsNot;
            }

            return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
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

        internal void SetAsQueue()
        {
            State = ProcessState.Queued;
            QueuedDate = TimeZoneManager.Now;
            ExecutionStart = null;
            ExecutionEnd = null;
            SuspendDate = null;
            Progress = null;
            Exception = null;
            ExceptionDate = null;
        }
    }

    public interface IProcessDataDN : IIdentifiable
    {
    }

    public interface ISessionDataDN : IIdentifiable
    {
    }

    [Serializable]
    public class UserProcessSessionDN : Entity, ISessionDataDN
    {
        Lite<UserDN> user;
        public Lite<UserDN> User
        {
            get { return user; }
            set { Set(ref user, value, () => User); }
        }

        public static UserProcessSessionDN CreateCurrent()
        {
            return new UserProcessSessionDN
            {
                User = UserDN.Current.ToLite(),
            };
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
        Finished,
        Error,
    }

    public enum ProcessOperation
    {
        Plan,
        Save,
        Cancel,
        Execute,
        Suspend,
    }

    public enum ProcessPermissions
    {
        ViewProcessControlPanel
    }
}
