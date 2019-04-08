using System;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using System.ComponentModel;
using Signum.Entities.Basics;
using Signum.Entities.Authorization;

namespace Signum.Entities.Processes
{
    [Serializable]
    public class ProcessAlgorithmSymbol : Symbol
    {
        private ProcessAlgorithmSymbol() { }

        public ProcessAlgorithmSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional), TicksColumn(false)]
    public class ProcessEntity : Entity
    {
        internal ProcessEntity() { }

        public ProcessEntity(ProcessAlgorithmSymbol process)
        {
            this.Algorithm = process;
        }
        
        public ProcessAlgorithmSymbol Algorithm { get; private set; }

        public IProcessDataEntity? Data { get; set; }

        public const string None = "none";

        [StringLengthValidator(Min = 3, Max = 100)]
        public string MachineName { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string ApplicationName { get; set; }

        
        public Lite<IUserEntity> User { get; set; }

        public ProcessState State { get; set; }

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        [DateTimePrecisionValidator(DateTimePrecision.Milliseconds)]
        public DateTime? PlannedDate { get; set; }

        [DateTimePrecisionValidator(DateTimePrecision.Milliseconds)]
        public DateTime? CancelationDate { get; set; }

        [DateTimePrecisionValidator(DateTimePrecision.Milliseconds)]
        public DateTime? QueuedDate { get; set; }

        DateTime? executionStart;
        [DateTimePrecisionValidator(DateTimePrecision.Milliseconds)]
        public DateTime? ExecutionStart
        {
            get { return executionStart; }
            set { if (Set(ref executionStart, value)) Notify(() => ExecutionEnd); }
        }

        DateTime? executionEnd;
        [DateTimePrecisionValidator(DateTimePrecision.Milliseconds)]
        public DateTime? ExecutionEnd
        {
            get { return executionEnd; }
            set { if (Set(ref executionEnd, value)) Notify(() => ExecutionStart); }
        }

        static Expression<Func<ProcessEntity, double?>> DurationExpression =
         log => (double?)(log.ExecutionEnd - log.ExecutionStart)!.Value.TotalMilliseconds;
        [ExpressionField("DurationExpression")]
        public double? Duration
        {
            get { return ExecutionEnd == null ? null : DurationExpression.Evaluate(this); }
        }

        static Expression<Func<ProcessEntity, TimeSpan?>> DurationSpanExpression =
        log => log.ExecutionEnd - log.ExecutionStart;
        [ExpressionField("DurationSpanExpression")]
        public TimeSpan? DurationSpan
        {
            get { return ExecutionEnd == null ? null : DurationSpanExpression.Evaluate(this); }
        }

        public DateTime? SuspendDate { get; set; }

        public DateTime? ExceptionDate { get; set; }

        public Lite<ExceptionEntity>? Exception { get; set; }

        [NumberBetweenValidator(0, 1), Format("p")]
        public decimal? Progress { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string? Status { get; set; }

        static StateValidator<ProcessEntity, ProcessState> stateValidator = new StateValidator<ProcessEntity, ProcessState>
        (e => e.State, e => e.PlannedDate, e => e.CancelationDate, e => e.QueuedDate, e => e.ExecutionStart, e => e.ExecutionEnd, e => e.SuspendDate, e => e.Progress, e => e.Status, e => e.ExceptionDate, e => e.Exception, e => e.MachineName, e => e.ApplicationName)
        {
       {ProcessState.Created,   false,          false,                  false,             false,                 false,               false,              false,       false,        false,               false,          null,          null },
       {ProcessState.Planned,   true,           null,                   null,              null,                  false,               null,               null,        null,         null,                null ,          null,          null },
       {ProcessState.Canceled,  null,           true,                   null,              null,                  false,               null,               null,        null,         null,                null ,          null,          null },
       {ProcessState.Queued,    null,           null,                   true,              false,                 false,               false,              false,       false,        false,               false,          null,          null },
       {ProcessState.Executing, null,           null,                   true,              true,                  false,               false,              true,        null,         false,               false,          true,          true },
       {ProcessState.Suspending,null,           null,                   true,              true,                  false,               true,               true,        null,         false,               false,          true,          true },
       {ProcessState.Suspended, null,           null,                   true,              true,                  false,               true,               true,        null,         false,               false,          null,          null },
       {ProcessState.Finished,  null,           null,                   true,              true,                  true,                false,              false,       null,         false,               false,          null,          null },
       {ProcessState.Error,     null,           null,                   null,              null,                  null,                null,               null,        null,         true,                true ,          null,          null },
        };

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(ExecutionStart) || pi.Name == nameof(ExecutionEnd))
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
            switch (State)
            {
                case ProcessState.Created: return "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Created.NiceToString(), CreationDate);
                case ProcessState.Planned: return "{0} {1} for {2}".FormatWith(Algorithm, ProcessState.Planned.NiceToString(), PlannedDate);
                case ProcessState.Canceled: return "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Canceled.NiceToString(), CancelationDate);
                case ProcessState.Queued: return "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Queued.NiceToString(), QueuedDate);
                case ProcessState.Executing: return "{0} {1} since {2}".FormatWith(Algorithm, ProcessState.Executing.NiceToString(), executionStart);
                case ProcessState.Suspending: return "{0} {1} since {2}".FormatWith(Algorithm, ProcessState.Suspending.NiceToString(), SuspendDate);
                case ProcessState.Suspended: return "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Suspended.NiceToString(), SuspendDate);
                case ProcessState.Finished: return "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Finished.NiceToString(), executionEnd);
                case ProcessState.Error: return "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Error.NiceToString(), executionEnd);
                default: return "{0} ??".FormatWith(Algorithm);
            }
        }
    }

    public interface IProcessDataEntity : IEntity
    {
    }

    public interface IProcessLineDataEntity : IEntity
    {

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

    [AutoInit]
    public static class ProcessOperation
    {
        public static ExecuteSymbol<ProcessEntity> Plan;
        public static ExecuteSymbol<ProcessEntity> Save;
        public static ExecuteSymbol<ProcessEntity> Cancel;
        public static ExecuteSymbol<ProcessEntity> Execute;
        public static ExecuteSymbol<ProcessEntity> Suspend;
        public static ConstructSymbol<ProcessEntity>.From<ProcessEntity> Retry;
    }

    [AutoInit]
    public static class ProcessPermission
    {
        public static PermissionSymbol ViewProcessPanel;
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
        ExceptionLines,
        [Description("Suspend in the safer way of stoping a running process. Cancel anyway?")]
        SuspendIsTheSaferWayOfStoppingARunningProcessCancelAnyway
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class ProcessExceptionLineEntity : Entity
    {
        [SqlDbType(Size = int.MaxValue)]
        public string? ElementInfo { get; set; }

        public Lite<IProcessLineDataEntity>? Line { get; set; }
        
        public Lite<ProcessEntity> Process { get; set; }
        
        public Lite<ExceptionEntity> Exception { get; set; }
    }
}
