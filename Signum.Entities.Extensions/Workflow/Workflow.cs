using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WorkflowEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullable]
        [NotNullValidator]
        public TypeEntity MainEntityType { get; set; }

        /// <summary>
        /// REDUNDANT! Only for diff logging
        /// </summary>
        [InTypeScript(false)]
        public WorkflowXmlEntity FullDiagramXml { get; set; }

        static Expression<Func<WorkflowEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
    
    [AutoInit]
    public static class WorkflowOperation
    {
        public static readonly ConstructSymbol<WorkflowEntity>.From<WorkflowEntity> Clone;
        public static readonly ExecuteSymbol<WorkflowEntity> Save;
        public static readonly DeleteSymbol<WorkflowEntity> Delete;
    }

    [Serializable, InTypeScript(Undefined = false)]
    public class WorkflowModel : ModelEntity
    {
        [NotNullable]
        [NotNullValidator]
        public string DiagramXml { get; set;  }

        public MList<BpmnEntityPair> Entities { get; set; } = new MList<BpmnEntityPair>();
    }

    [Serializable, InTypeScript(Undefined = false)]
    public class BpmnEntityPair : EmbeddedEntity
    {
        [NotNullable]
        [NotNullValidator]
        [ImplementedBy()]
        public ModelEntity Model { get; set; }

        [NotNullable]
        [NotNullValidator]
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
    }

    [Serializable]
    public class WorkflowXmlEntity : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = int.MaxValue)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = int.MaxValue, MultiLine = true)]
        public string DiagramXml { get; set; }
    }

    public interface IWorkflowObjectEntity : IEntity
    {
        WorkflowXmlEntity Xml { get; set; }
        string Name { get; set; }
        string BpmnElementId { get; set; }
    }

    public interface IWorkflowNodeEntity : IWorkflowObjectEntity
    {
        WorkflowLaneEntity Lane { get; set; }
    }

    public interface IWorkflowTransition
    {
        Lite<WorkflowConditionEntity> Condition { get; }

        Lite<WorkflowActionEntity> Action { get; }
    }

    public interface IWorkflowTransitionTo : IWorkflowTransition
    {
        Lite<IWorkflowNodeEntity> To { get; }
    }

    [Serializable]
    public class WorkflowReplacementModel: ModelEntity
    {
        public MList<WorkflowReplacementItemEntity> Replacements { get; set; } = new MList<WorkflowReplacementItemEntity>();
    }

    [Serializable]
    public class WorkflowReplacementItemEntity : EmbeddedEntity
    {
        [NotNullable]
        [NotNullValidator, InTypeScript(Undefined = false, Null= false)]
        public Lite<WorkflowActivityEntity> OldTask { get; set; }
        
        [NotNullValidator]
        public string NewTask { get; set; }
    }

    public class WorkflowEvaluationContext
    {
        public WorkflowEvaluationContext(CaseActivityEntity ca, IWorkflowTransition conn, DecisionResult? dr)
        {
            this.CaseActivity = ca;
            this.Case = ca?.Case;
            this.Connection = conn;
            this.DecisionResult = dr;
        }

        public CaseActivityEntity CaseActivity { get; internal set; }
        public DecisionResult? DecisionResult { get; internal set; }
        public IWorkflowTransition Connection { get; internal set; }
        public CaseEntity Case { get; internal set; }
    }

    public enum WorkflowValidationMessage
    {
        [Description("Node type {0} with Id {1} is invalid.")]
        NodeType0WithId1IsInvalid,
        [Description("Participants and Processes are not synchronized.")]
        ParticipantsAndProcessesAreNotSynchronized,
        [Description("Multiple start events are not allowed.")]
        MultipleFinishEventsAreNotAllowed,
        [Description("Start event is required. each workflow could have one and only one start event.")]
        StartEventIsRequired,
        [Description("The following tasks are going to be deleted :")]
        TheFollowingTasksAreGoingToBeDeleted,
        FinishEventIsRequired,
        [Description("Activity '{0}' can not reject to start.")]
        Activity0CanNotRejectToStart,
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
        [Description("'{0}' has multiple inputs and outputs at same time.")]
        _0HasMultipleInputsAndOutputsAtTheSameTime,
        [Description("'{0}' has multiple outputs.")]
        _0HasMultipleOutputs,
        [Description("Activity '{0}' can not reject to parallel gateways.")]
        Activity0CanNotRejectToParallelGateway,
        IsNotInWorkflow,
        [Description("Activity '{0}' can not jump to '{1}' because '{2}'.")]
        Activity0CanNotJumpTo1Because2,
        [Description("Activity '{0}' can not timeout to '{1}' because '{2}'.")]
        Activity0CanNotTimeoutTo1Because2,
        IsStart,
        IsInDifferentParallelTrack,
        [Description("'{0}' (Track {1}) can not be connected to '{2}' (Track {3} instead of Track {4}).")]
        _0Track1CanNotBeConnectedTo2Track3InsteadOfTrack4,
        StartEventNextNodeShouldBeAnActivity,
        ParallelGatewaysShouldPair
    }
}
