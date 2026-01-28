using System.ComponentModel;

namespace Signum.Processes;

public class ProcessAlgorithmSymbol : Symbol
{
    private ProcessAlgorithmSymbol() { }

    public ProcessAlgorithmSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[EntityKind(EntityKind.Main, EntityData.Transactional), TicksColumn(false)]
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

    public DateTime CreationDate { get; private set; } = Clock.Now;

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

    [DbType(Size = int.MaxValue)]
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
        return State switch
        {
            ProcessState.Created => "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Created.NiceToString(), CreationDate),
            ProcessState.Planned => "{0} {1} for {2}".FormatWith(Algorithm, ProcessState.Planned.NiceToString(), PlannedDate),
            ProcessState.Canceled => "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Canceled.NiceToString(), CancelationDate),
            ProcessState.Queued => "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Queued.NiceToString(), QueuedDate),
            ProcessState.Executing => "{0} {1} since {2}".FormatWith(Algorithm, ProcessState.Executing.NiceToString(), executionStart),
            ProcessState.Suspending => "{0} {1} since {2}".FormatWith(Algorithm, ProcessState.Suspending.NiceToString(), SuspendDate),
            ProcessState.Suspended => "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Suspended.NiceToString(), SuspendDate),
            ProcessState.Finished => "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Finished.NiceToString(), executionEnd),
            ProcessState.Error => "{0} {1} on {2}".FormatWith(Algorithm, ProcessState.Error.NiceToString(), executionEnd),
            _ => "{0} ??".FormatWith(Algorithm),
        };
    }
}

public interface IProcessDataEntity : IEntity
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
    public static ExecuteSymbol<ProcessEntity> Save;
    public static ExecuteSymbol<ProcessEntity> Execute;
    public static ExecuteSymbol<ProcessEntity> Suspend;
    public static ExecuteSymbol<ProcessEntity> Cancel;
    public static ExecuteSymbol<ProcessEntity> Plan;
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
    SuspendIsTheSaferWayOfStoppingARunningProcessCancelAnyway,
    ProcessSettings,
    OnlyActive,

    [Description("ProcessLogic state (loading...) ")]
    ProcessLogicStateLoading,
    ProcessPanel,
    Start,
    Stop,
    [Description("RUNNING")]
    Running,
    [Description("STOPPED")]
    Stopped,
    SimpleStatus,
    JustMyProcesses,
    MachineName,
    ApplicationName,
    MaxDegreeOfParallelism,
    InitialDelayMilliseconds,
    NextPlannedExecution,
    None,
    ExecutingProcesses,
    Process,
    State,
    Progress,
    IsCancellationRequest,
    [Description("{0} processes executing in {1} / {2}")]
    _0ProcessesExcecutingIn1_2,
    LatestProcesses,
}

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class ProcessExceptionLineEntity : Entity
{
    [DbType(Size = int.MaxValue)]
    public string? ElementInfo { get; set; }

    public Lite<IEntity>? Line { get; set; }
    
    public Lite<ProcessEntity> Process { get; set; }
    
    public Lite<ExceptionEntity> Exception { get; set; }

#pragma warning disable IDE0052 // Remove unread private members
    static Expression<Func<ProcessExceptionLineEntity, string>> ToStringExpression = pel => "ProcessExceptionLine (" + pel.Id + ")";
    [ExpressionField("ToStringExpression")]
    public override string ToString() => "ProcessExceptionLine (" + (this.IdOrNull == null ? "New" : this.Id.ToString()) + ")";
#pragma warning restore IDE0052 // Remove unread private members
}
