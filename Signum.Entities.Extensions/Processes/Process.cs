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
using Signum.Entities.Basics;
using Signum.Entities.Scheduler;
using Signum.Entities.Authorization;

namespace Signum.Entities.Processes
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master)]
    public class ProcessAlgorithmDN : MultiEnumDN
    {

    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class ProcessDN : IdentifiableEntity
    {
        internal ProcessDN() { }

        public ProcessDN(ProcessAlgorithmDN process)
        {
            this.algorithm = process;
        }

        ProcessAlgorithmDN algorithm;
        [NotNullValidator]
        public ProcessAlgorithmDN Algorithm
        {
            get { return algorithm; }
        }

        IProcessDataDN data;
        public IProcessDataDN Data
        {
            get { return data; }
            set { Set(ref data, value); }
        }

        [ImplementedBy(typeof(UserProcessSessionDN))]
        IProcessSessionDN session;
        public IProcessSessionDN Session
        {
            get { return session; }
            set { Set(ref session, value); }
        }

        public const string None = "none";

        [SqlDbType(Size = 100), NotNullable]
        string machineName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string MachineName
        {
            get { return machineName; }
            set { Set(ref machineName, value); }
        }

        [SqlDbType(Size = 100), NotNullable]
        string applicationName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ApplicationName
        {
            get { return applicationName; }
            set { Set(ref applicationName, value); }
        }

        ProcessState state;
        public ProcessState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        DateTime creationDate = TimeZoneManager.Now;
        public DateTime CreationDate
        {
            get { return creationDate; }
            private set { Set(ref creationDate, value); }
        }

        DateTime? plannedDate;
        public DateTime? PlannedDate
        {
            get { return plannedDate; }
            set { Set(ref plannedDate, value); }
        }

        DateTime? cancelationDate;
        public DateTime? CancelationDate
        {
            get { return cancelationDate; }
            set { Set(ref cancelationDate, value); }
        }

        DateTime? queuedDate;
        public DateTime? QueuedDate
        {
            get { return queuedDate; }
            set { Set(ref queuedDate, value); }
        }

        DateTime? executionStart;
        public DateTime? ExecutionStart
        {
            get { return executionStart; }
            set { if (Set(ref executionStart, value))Notify(() => ExecutionEnd); }
        }

        DateTime? executionEnd;
        public DateTime? ExecutionEnd
        {
            get { return executionEnd; }
            set { if (Set(ref executionEnd, value))Notify(() => ExecutionStart); }
        }

        static Expression<Func<ProcessDN, double?>> DurationExpression =
         log => (double?)(log.ExecutionEnd - log.ExecutionStart).Value.TotalMilliseconds;
        public double? Duration
        {
            get { return ExecutionEnd == null ? null : DurationExpression.Evaluate(this); }
        }

        DateTime? suspendDate;
        public DateTime? SuspendDate
        {
            get { return suspendDate; }
            set { Set(ref suspendDate, value); }
        }

        DateTime? exceptionDate;
        public DateTime? ExceptionDate
        {
            get { return exceptionDate; }
            set { Set(ref exceptionDate, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }

        decimal? progress;
        [NumberBetweenValidator(0, 1), Format("p")]
        public decimal? Progress
        {
            get { return progress; }
            set { Set(ref progress, value); }
        }

        static StateValidator<ProcessDN, ProcessState> stateValidator = new StateValidator<ProcessDN, ProcessState>
        (e => e.State, e => e.PlannedDate, e => e.CancelationDate, e => e.QueuedDate, e => e.ExecutionStart, e => e.ExecutionEnd, e => e.SuspendDate, e => e.Progress, e => e.ExceptionDate, e => e.Exception, e=> e.MachineName, e=>e.ApplicationName)
        {
       {ProcessState.Created,   false,          false,                  false,             false,                 false,               false,              false,           false,               false,          null,          null }, 
       {ProcessState.Planned,   true,           null,                   null,              null,                  false,               null,               null,            null,                null ,          null,          null }, 
       {ProcessState.Canceled,  null,           true,                   null,              null,                  false,               null,               null,            null,                null ,          null,          null }, 
       {ProcessState.Queued,    null,           null,                   true,              false,                 false,               false,              false,           false,               false,          null,          null },
       {ProcessState.Executing, null,           null,                   true,              true,                  false,               false,              true,            false,               false,          true,          true },
       {ProcessState.Suspending,null,           null,                   true,              true,                  false,               true,               true,            false,               false,          true,          true },
       {ProcessState.Suspended, null,           null,                   true,              true,                  false,               true,               true,            false,               false,          null,          null },
       {ProcessState.Finished,  null,           null,                   true,              true,                  true,                false,              false,           false,               false,          null,          null },
       {ProcessState.Error,     null,           null,                   null,              null,                  null,                null,               null,            true,                true ,          null,          null },
        };

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => ExecutionStart) || pi.Is(() => ExecutionEnd))
            {
                if (this.ExecutionEnd < this.ExecutionStart)
                    return ProcessMessage.ProcessStartIsGreaterThanProcessEnd.NiceToString();

                if (this.ExecutionStart == null && this.ExecutionEnd != null)
                    return ProcessMessage.ProcessStartIsNullButProcessEndIsNot.NiceToString();
            }

            return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
        }


        public override string ToString()
        {
            switch (state)
            {
                case ProcessState.Created: return "{0} {1} on {2}".Formato(algorithm, ProcessState.Created.NiceToString(), creationDate);
                case ProcessState.Planned: return "{0} {1} for {2}".Formato(algorithm, ProcessState.Planned.NiceToString(), plannedDate);
                case ProcessState.Canceled: return "{0} {1} on {2}".Formato(algorithm, ProcessState.Canceled.NiceToString(), cancelationDate);
                case ProcessState.Queued: return "{0} {1} on {2}".Formato(algorithm, ProcessState.Queued.NiceToString(), queuedDate);
                case ProcessState.Executing: return "{0} {1} since {2}".Formato(algorithm, ProcessState.Executing.NiceToString(), executionStart);
                case ProcessState.Suspending: return "{0} {1} since {2}".Formato(algorithm, ProcessState.Suspending.NiceToString(), suspendDate);
                case ProcessState.Suspended: return "{0} {1} on {2}".Formato(algorithm, ProcessState.Suspended.NiceToString(), suspendDate);
                case ProcessState.Finished: return "{0} {1} on {2}".Formato(algorithm, ProcessState.Finished.NiceToString(), executionEnd);
                case ProcessState.Error: return "{0} {1} on {2}".Formato(algorithm, ProcessState.Error.NiceToString(), executionEnd);
                default: return "{0} ??".Formato(algorithm);
            }
        }
    }

    public interface IProcessDataDN : IIdentifiable
    {
    }

    public interface IProcessLineDataDN : IIdentifiable
    {

    }

    public interface IProcessSessionDN : IIdentifiable
    {
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class UserProcessSessionDN : Entity, IProcessSessionDN
    {
        Lite<UserDN> user;
        public Lite<UserDN> User
        {
            get { return user; }
            set { Set(ref user, value); }
        }

        public static UserProcessSessionDN CreateCurrent()
        {
            return new UserProcessSessionDN
            {
                User = UserDN.Current.ToLite(),
            };
        }

        public override string ToString()
        {
            return GetType().NiceName() + ":" + user.TryToString();
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
        Retry,
    }

    public enum ProcessPermission
    {
        ViewProcessPanel
    }

    public enum ProcessMessage
    {
        [Description("Process {0} is not running anymore")]
        Process0IsNotRunningAnymore,
        [Description("Process Start is greater than Process End")]
        ProcessStartIsGreaterThanProcessEnd,
        [Description("Process Start is null but Process End is not")]
        ProcessStartIsNullButProcessEndIsNot,
        Lines,
        LastProcess,
        ExceptionLines
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ProcessExceptionLineDN : Entity
    {
        [NotNullable]
        Lite<IProcessLineDataDN> line;
        [NotNullValidator]
        public Lite<IProcessLineDataDN> Line
        {
            get { return line; }
            set { Set(ref line, value); }
        }

        [NotNullable]
        Lite<ProcessDN> process;
        [NotNullValidator]
        public Lite<ProcessDN> Process
        {
            get { return process; }
            set { Set(ref process, value); }
        }

        [NotNullable]
        Lite<ExceptionDN> exception;
        [NotNullValidator]
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }
    }

    public enum ProcessExceptionOperation
    {
        Save
    }

}
