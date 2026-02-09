using Signum.Utilities.Reflection;
using System.ComponentModel;

namespace Signum.Scheduler;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class ScheduledTaskEntity : Entity
{
    [ImplementedBy(
        typeof(ScheduleRuleMinutelyEntity),
        typeof(ScheduleRuleWeekDaysEntity),
        typeof(ScheduleRuleMonthsEntity))]
    
    public IScheduleRuleEntity Rule { get; set; }

    [ImplementedBy(typeof(SimpleTaskSymbol))]
    
    public ITaskEntity Task { get; set; }

    public bool Suspended { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string MachineName { get; set; } = None;

    
    public Lite<IUserEntity> User { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string ApplicationName { get; set; } = None;

    public override string ToString()
    {
        return "{0} {1}".FormatWith(Task, Rule) + (Suspended ? " [{0}]".FormatWith(ReflectionTools.GetPropertyInfo(() => Suspended).NiceName()) : "");
    }

    public const string None = "none";
}

[AutoInit]
public static class ScheduledTaskOperation
{
    public static ExecuteSymbol<ScheduledTaskEntity> Save;
    public static DeleteSymbol<ScheduledTaskEntity> Delete;
}

public enum ITaskMessage
{
    Execute,
    Executions,
    LastExecution,
    ExceptionLines
}

[AutoInit]
public static class ITaskOperation
{
    public static ConstructSymbol<ScheduledTaskLogEntity>.From<ITaskEntity> ExecuteSync;
}



[AutoInit]
public static class SchedulerPermission
{
    public static PermissionSymbol ViewSchedulerPanel;
}

public interface ITaskEntity : IEntity
{
}

public enum ScheduledTaskMessage
{
    State,
    InitialDelayMilliseconds,
    SchedulerMargin,
    MachineName,
    ApplicationName,
    ServerTimeZone,
    ServerLocalTime,
    NextExecution,
    InMemoryQueue,
    RunningTasks,
    AvailableTasks,
    Rule,
    NextDate,
    ScheduledTask,
    [Description("RUNNING")]
    Running,
    [Description("STOPPED")]
    Stopped,
    None,
    SimpleStatus,
    ThereIsNoActiveScheduledTask,
    ThereAreNoTasksRunning,
    SchedulerTaskLog,
    StartTime,
    Remarks,
    Cancel,
    SchedulePanel,
    Start,
    Stop,
}
