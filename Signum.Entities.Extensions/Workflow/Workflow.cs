using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.UserAssets;
using System.ComponentModel;
using System.Xml.Linq;

namespace Signum.Entities.Workflow;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class WorkflowEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    
    public TypeEntity MainEntityType { get; set; }

    public MList<WorkflowMainEntityStrategy> MainEntityStrategies { get; set; } = new MList<WorkflowMainEntityStrategy>();

    public DateTime? ExpirationDate { get; set; }
    /// <summary>
    /// REDUNDANT! Only for diff logging
    /// </summary>
    [InTypeScript(false), AvoidDump]
    public WorkflowXmlEmbedded? FullDiagramXml { get; set; }

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    public XElement ToXml(IToXmlContext ctx)
    {
        return ctx.GetFullWorkflowElement(this);
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        ctx.SetFullWorkflowElement(this, element);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}

[AutoInit]
public static class WorkflowOperation
{
    public static readonly ConstructSymbol<WorkflowEntity>.Simple Create;
    public static readonly ConstructSymbol<WorkflowEntity>.From<WorkflowEntity> Clone;
    public static readonly ExecuteSymbol<WorkflowEntity> Save;
    public static readonly DeleteSymbol<WorkflowEntity> Delete;
    public static readonly ExecuteSymbol<WorkflowEntity> Activate;
    public static readonly ExecuteSymbol<WorkflowEntity> Deactivate;
}

[InTypeScript(true)]
public enum WorkflowMainEntityStrategy
{
    CreateNew,
    SelectByUser,
    Clone,
}

[InTypeScript(true), DescriptionOptions(DescriptionOptions.Members)]
public enum WorkflowIssueType
{
    Warning,
    Error,
}

public class WorkflowModel : ModelEntity
{
    public string DiagramXml { get; set;  }

    public MList<BpmnEntityPairEmbedded> Entities { get; set; } = new MList<BpmnEntityPairEmbedded>();
}

public class BpmnEntityPairEmbedded : EmbeddedEntity
{
    [ImplementedBy()]
    public ModelEntity Model { get; set; }

    public string BpmnElementId { get; set; }

    public override string ToString()
    {
        return $"{BpmnElementId} -> {Model}";
    }
}

public interface IWithModel
{
    ModelEntity GetModel();
    void SetModel(ModelEntity model);
}

public enum WorkflowMessage
{
    [Description("'{0}' belongs to a different workflow")]
    _0BelongsToADifferentWorkflow,
    [Description("Condition '{0}' is defined for '{1}' not '{2}'")]
    Condition0IsDefinedFor1Not2,
    JumpsToSameActivityNotAllowed,
    [Description("Jump to '{0}' failed because '{1}'")]
    JumpTo0FailedBecause1,
    [Description("To use '{0}', you should save workflow")]
    ToUse0YouSouldSaveWorkflow,
    [Description("To use new nodes on jumps, you should save workflow")]
    ToUseNewNodesOnJumpsYouSouldSaveWorkflow,
    [Description("To use '{0}', you should set the workflow '{1}'")]
    ToUse0YouSouldSetTheWorkflow1,
    [Description("Change workflow main entity type is not allowed because we have nodes that use it.")]
    ChangeWorkflowMainEntityTypeIsNotAllowedBecauseWeHaveNodesThatUseIt,
    [Description("Workflow uses in {0} for decomposition or call workflow.")]
    WorkflowUsedIn0ForDecompositionOrCallWorkflow,
    [Description("Workflow '{0}' already activated.")]
    Workflow0AlreadyActivated,
    [Description("Workflow '{0}' has expired on '{1}'.")]
    Workflow0HasExpiredOn1,
    HasExpired,
    DeactivateWorkflow,
    PleaseChooseExpirationDate,
    ResetZoom,
    [Description("Color: ")]
    Color,
    [Description("Workflow Issues")]
    WorkflowIssues,
    WorkflowProperties,
    [Description("{0} not allowed for {1} (no constructor has been defined in 'WithWorkflow')")]
    _0NotAllowedFor1NoConstructorHasBeenDefinedInWithWorkflow,
    [Description("You are not member of any lane containing an Start event in workflow '{0}'")]
    YouAreNotMemberOfAnyLaneContainingAnStartEventInWorkflow0,

    [Description("Evaluation order of the contition (if... else)")]
    EvaluationOrderOfTheConnectionForIfElse
}

public class WorkflowXmlEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Min = 3, Max = int.MaxValue, MultiLine = true)]
    public string DiagramXml { get; set; }

    public XElement ToXml()
    {
        return new XElement("DiagramXml", new XCData(this.DiagramXml));
    }
}

public interface IWorkflowObjectEntity : IEntity
{
    WorkflowXmlEmbedded Xml { get; set; }
    string? GetName();
    string BpmnElementId { get; set; }
}

public interface IWorkflowNodeEntity : IWorkflowObjectEntity
{
    WorkflowLaneEntity Lane { get; set; }
}

public class WorkflowReplacementModel: ModelEntity
{
    public MList<WorkflowReplacementItemEmbedded> Replacements { get; set; } = new MList<WorkflowReplacementItemEmbedded>();

    public MList<NewTasksEmbedded> NewTasks { get; set; } = new MList<NewTasksEmbedded>();

    public override string ToString()
    {
        return NicePropertyName(()=> Replacements) + ": " + Replacements.Count;
    }
}

public class NewTasksEmbedded : EmbeddedEntity
{
    public string BpmnId { get; set; }
    public string? Name { get; set; }
    public Lite<WorkflowEntity>? SubWorkflow { get; set; }
}

public class WorkflowReplacementItemEmbedded : EmbeddedEntity
{
    [ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity))]
    public Lite<IWorkflowNodeEntity> OldNode { get; set; }

    public Lite<WorkflowEntity>? SubWorkflow { get; set; }

    public string NewNode { get; set; }
}

public class WorkflowTransitionContext
{
    public WorkflowTransitionContext(CaseEntity? @case, CaseActivityEntity? previous, WorkflowConnectionEntity? conn)
    {
        this.Case = @case;
        this.PreviousCaseActivity = previous;
        this.Connection = conn;
    }

    public CaseActivityEntity? PreviousCaseActivity { get; internal set; }
    public WorkflowConnectionEntity? Connection { get; internal set; }
    public CaseEntity? Case { get; set; }

    public Action<CaseActivityEntity>? OnNextCaseActivityCreated; 
}

public enum WorkflowValidationMessage
{
    [Description("Node type {0} with Id {1} is invalid.")]
    NodeType0WithId1IsInvalid,
    [Description("Participants and Processes are not synchronized.")]
    ParticipantsAndProcessesAreNotSynchronized,
    [Description("Multiple start events are not allowed.")]
    MultipleStartEventsAreNotAllowed,
    [Description("Start event is required. Each workflow could have one and only one start event.")]
    SomeStartEventIsRequired,
    [Description("Normal start event is required when the '{0}' are '{1}' or '{2}'.")]
    NormalStartEventIsRequiredWhenThe0Are1Or2,
    [Description("The following tasks are going to be deleted :")]
    TheFollowingTasksAreGoingToBeDeleted,
    FinishEventIsRequired,
    [Description("'{0}' has inputs.")]
    _0HasInputs,
    [Description("'{0}' has outputs.")]
    _0HasOutputs,
    [Description("'{0}' has no inputs.")]
    _0HasNoInputs,
    [Description("'{0}' has no outputs.")]
    _0HasNoOutputs,
    [Description("'{0}' has just one input and one output.")]
    _0HasJustOneInputAndOneOutput,
    [Description("'{0}' has multiple outputs.")]
    _0HasMultipleOutputs,
    IsNotInWorkflow,
    [Description("Activity '{0}' can not jump to '{1}' because '{2}'.")]
    Activity0CanNotJumpTo1Because2,
    [Description("Activity '{0}' can not timeout to '{1}' because '{2}'.")]
    Activity0CanNotTimeoutTo1Because2,
    IsStart,
    IsSelfJumping,
    IsInDifferentParallelTrack,
    [Description("'{0}' (Track {1}) can not be connected to '{2}' (Track {3} instead of Track {4}).")]
    _0Track1CanNotBeConnectedTo2Track3InsteadOfTrack4,
    StartEventNextNodeShouldBeAnActivity,
    ParallelGatewaysShouldPair,
    TimerOrConditionalStartEventsCanNotGoToJoinGateways,
    [Description("Inclusive Gateway '{0}' should have one default connection without condition.")]
    InclusiveGateway0ShouldHaveOneConnectionWithoutCondition,
    [Description("Gateway '{0}' should has condition or decision on each output except the last one.")]
    Gateway0ShouldHasConditionOrDecisionOnEachOutputExceptTheLast,
    [Description("'{0}' can not be connected to a parallel join because has no previous parallel split.")]
    _0CanNotBeConnectedToAParallelJoinBecauseHasNoPreviousParallelSplit,
    [Description("Activity '{0}' with decision type should go to an exclusive or inclusive gateways.")]
    Activity0WithDecisionTypeShouldGoToAnExclusiveOrInclusiveGateways,
    [Description("Activity '{0}' should be decision.")]
    Activity0ShouldBeDecision,
    [Description("'{0}' is timer start and scheduler is mandatory.")]
    _0IsTimerStartAndSchedulerIsMandatory,
    [Description("'{0}' is timer start and task is mandatory.")]
    _0IsTimerStartAndTaskIsMandatory,
    [Description("'{0}' is conditional start and condition is mandatory.")]
    _0IsConditionalStartAndTaskConditionIsMandatory,
    DelayActivitiesShouldHaveExactlyOneInterruptingTimer,
    [Description("Activity '{0}' of type '{1}' should have exactly one connection of type '{2}'.")]
    Activity0OfType1ShouldHaveExactlyOneConnectionOfType2,
    [Description("Activity '{0}' of type '{1}' can not have connections of type '{2}'.")]
    Activity0OfType1CanNotHaveConnectionsOfType2,
    [Description("Boundary timer '{0}' of activity '{1}' should have exactly one connection of type '{2}'.")]
    BoundaryTimer0OfActivity1ShouldHaveExactlyOneConnectionOfType2,
    [Description("Intermediate timer '{0}' should have one output of type '{1}'.")]
    IntermediateTimer0ShouldHaveOneOutputOfType1,
    [Description("Intermediate timer '{0}' should have name.")]
    IntermediateTimer0ShouldHaveName,
    [Description("Parallel Split '{0}' should have at least one connection.")]
    ParallelSplit0ShouldHaveAtLeastOneConnection,
    [Description("Parallel Split '{0}' should have only normal connections without conditions.")]
    ParallelSplit0ShouldHaveOnlyNormalConnectionsWithoutConditions,
    [Description("Join '{0}' (of type {1}) does not match with its pair, the Split '{2}' (of type {3})")]
    Join0OfType1DoesNotMatchWithItsPairTheSplit2OfType3,
    [Description("Decision option '{0}' is declared but never used in a connection")]
    DecisionOption0IsDeclaredButNeverUsedInAConnection,
    [Description("Decision option name '{0}' is not declared in any activity")]
    DecisionOptionName0IsNotDeclaredInAnyActivity,
    [Description("Boundary timer '{0}' of activity '{1}' can not have {2} because activity is not {3}")]
    BoundaryTimer0OfActivity1CanNotHave2BecauseActivityIsNot3,
    [Description("Boundary timer '{0}' of activity '{1}' should have {2} because activity is {3}")]
    BoundaryTimer0OfActivity1ShouldHave2BecauseActivityIs3,
    [Description("Boundary timer '{0}' of activity '{1}' has invalid {2}: '{3}'")]
    BoundaryTimer0OfActivity1HasInvalid23,
}

public enum WorkflowActivityMonitorMessage
{
    WorkflowActivityMonitor,
    Draw,
    ResetZoom,
    Find,
    Filters,
    Columns
}

[AutoInit]
public static class WorkflowPermission
{
    public static PermissionSymbol ViewWorkflowPanel;
    public static PermissionSymbol ViewCaseFlow;
    public static PermissionSymbol WorkflowToolbarMenu;
}
