using Signum.Authorization;
using Signum.Processes;
using Signum.Scheduler;
using System.ComponentModel;

namespace Signum.Workflow;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class CaseActivityEntity : Entity
{
    public CaseEntity Case { get; set; }
    
    [ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity))]
    public IWorkflowNodeEntity WorkflowActivity { get; set; }
    
    [StringLengthValidator(Min = 3, Max = 255)]
    public string OriginalWorkflowActivityName { get; set; }

    public DateTime StartDate { get; set; } = Clock.Now;

    public Lite<CaseActivityEntity>? Previous { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? Note { get; set; }
    
    public DateTime? DoneDate { get; set; }

    [Unit("min")]
    public double? Duration { get; set; }
    
    [AutoExpressionField]
    public double? DurationRealTime => As.Expression(() => Duration ?? (double?)(Clock.Now - StartDate).TotalMinutes);

    [AutoExpressionField]
    public double? DurationRatio => As.Expression(() => Duration / ((WorkflowActivityEntity)WorkflowActivity).EstimatedDuration);

    [AutoExpressionField]
    public double? DurationRealTimeRatio => As.Expression(() => DurationRealTime / ((WorkflowActivityEntity)WorkflowActivity).EstimatedDuration);

    public Lite<UserEntity>? DoneBy { get; set; }

    public DoneType? DoneType { get; set; }

    public string? DoneDecision { get; set; }

    public ScriptExecutionEmbedded? ScriptExecution { get; set; }

    static Expression<Func<CaseActivityEntity, CaseActivityState>> StateExpression =
    @this => @this.DoneDate.HasValue ? CaseActivityState.Done : CaseActivityState.Pending;
    [ExpressionField("StateExpression")]
    public CaseActivityState State 
    {
        get
        {
            if (this.IsNew)
                return CaseActivityState.New;

            return StateExpression.Evaluate(this);
        }
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => WorkflowActivity + " " + DoneBy);

    protected override void PreSaving(PreSavingContext ctx)
    { 
        base.PreSaving(ctx);
        this.Duration = this.DoneDate == null ? (double?)null :
            (this.DoneDate.Value - this.StartDate).TotalMinutes;
    }
}

public class ScriptExecutionEmbedded : EmbeddedEntity
{
    public DateTime NextExecution { get; set; }
    public int RetryCount { get; set; }
    public Guid? ProcessIdentifier { get; set; }
}

public enum DoneType
{
    Next,
    Jump = 3,
    Timeout,
    ScriptSuccess,
    ScriptFailure,
    Recompose,
}

public enum CaseActivityState
{
    [Ignore]
    New,
    Pending,
    Done,
}


[AutoInit]
public static class CaseActivityOperation
{
    public static readonly ConstructSymbol<CaseActivityEntity>.From<WorkflowEntity> CreateCaseActivityFromWorkflow;
    public static readonly ConstructSymbol<CaseEntity>.From<WorkflowEventTaskEntity> CreateCaseFromWorkflowEventTask;
    public static readonly ExecuteSymbol<CaseActivityEntity> Register;
    public static readonly DeleteSymbol<CaseActivityEntity> Delete;
    public static readonly ExecuteSymbol<CaseActivityEntity> Next;
    public static readonly ExecuteSymbol<CaseActivityEntity> Jump;
    public static readonly ExecuteSymbol<CaseActivityEntity> FreeJump;
    public static readonly ExecuteSymbol<CaseActivityEntity> Timer;
    public static readonly ExecuteSymbol<CaseActivityEntity> MarkAsUnread;
    public static readonly ExecuteSymbol<CaseActivityEntity> Undo;
    public static readonly ExecuteSymbol<CaseActivityEntity> ScriptExecute;
    public static readonly ExecuteSymbol<CaseActivityEntity> ScriptScheduleRetry;
    public static readonly ExecuteSymbol<CaseActivityEntity> ScriptFailureJump;

    //FixCaseDescriptions in Signum.WorkflowDynamic
}

[AutoInit]
public static class CaseActivityTask
{
    public static readonly SimpleTaskSymbol Timeout;
}

[AutoInit]
public static class CaseActivityProcessAlgorithm
{
    public static readonly ProcessAlgorithmSymbol Timeout;
}

public enum CaseActivityMessage
{
    CaseContainsOtherActivities,
    NoNextConnectionThatSatisfiesTheConditionsFound,
    [Description("Case is a decomposition of {0}")]
    CaseIsADecompositionOf0,
    [Description("From {0} on {1}")]
    From0On1,
    [Description("Done by {0} on {1}")]
    DoneBy0On1,
    PersonalRemarksForThisNotification,
    [Description("The activity '{0}' requires to be opened")]
    TheActivity0RequiresToBeOpened,
    NoOpenedOrInProgressNotificationsFound,
    NextActivityAlreadyInProgress,
    NextActivityOfDecompositionSurrogateAlreadyInProgress,
    [Description("Only '{0}' can undo this operation")]
    Only0CanUndoThisOperation,
    [Description("Activity '{0}' has no jumps")]
    Activity0HasNoJumps,
    [Description("Activity '{0}' has no timeout")]
    Activity0HasNoTimers,
    ThereIsNoPreviousActivity,
    OnlyForScriptWorkflowActivities,
    Pending,
    NoWorkflowActivity,
    [Description("Impossible to delete Case Activity {0} (on Workflow Activity '{1}') because has no previouos activity")]
    ImpossibleToDeleteCaseActivity0OnWorkflowActivity1BecauseHasNoPreviousActivity,
    LastCaseActivity,
    CurrentUserHasNotification,
    NoNewOrOpenedOrInProgressNotificationsFound,
    NoActorsFoundToInsertCaseActivityNotifications,
    ThereAreInprogressActivities,
    ShowHelp,
    HideHelp,
    CanceledCase,
    AlreadyFinished,
    NotCanceled
}


public enum CaseActivityQuery
{
    Inbox
}

public class ActivityWithRemarks : ModelEntity
{
    public Lite<WorkflowActivityEntity>? WorkflowActivity { get; set; }
    public Lite<CaseEntity> Case { get; set; }
    public Lite<CaseActivityEntity>? CaseActivity { get; set; }
    public Lite<CaseNotificationEntity>? Notification { get; set; }
    public string? Remarks { get; set; }
    public int Alerts { get; set; }
    public List<CaseTagTypeEntity> Tags { get; set; }
}

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class CaseActivityExecutedTimerEntity : Entity
{
    public DateTime CreationDate { get; private set; } = Clock.Now;

    
    public Lite<CaseActivityEntity> CaseActivity { get; set; }


    
    public Lite<WorkflowEventEntity> BoundaryEvent { get; set; }
}
