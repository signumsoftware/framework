using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Entities.Workflow
{

    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WorkflowConnectionEntity : Entity, IWorkflowObjectEntity, IWithModel
    {
        [ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity), typeof(WorkflowGatewayEntity))]
        [ForceNullable]
        public IWorkflowNodeEntity From { get; set; }

        [ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity), typeof(WorkflowGatewayEntity))]
        [ForceNullable]
        public IWorkflowNodeEntity To { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string? Name { get; set; }

        public DecisionOptionEmbedded? DecisionOption { get; set; }

        public string? GetName() => Name;

        [StringLengthValidator(Min = 1, Max = 100)]
        public string BpmnElementId { get; set; }

        public ConnectionType Type { get; set; }

        public Lite<WorkflowConditionEntity>? Condition { get; set; }

        public Lite<WorkflowActionEntity>? Action { get; set; }

        public int? Order { get; set; }

        [AvoidDump]
        public WorkflowXmlEmbedded Xml { get; set; }

        public ModelEntity GetModel()
        {
            var model = new WorkflowConnectionModel()
            {
                MainEntityType = this.From!.Lane.Pool.Workflow.MainEntityType,
                Name = this.Name,
                DecisionOption = this.DecisionOption,
                Type = this.Type,
                Condition = this.Condition,
                Action = this.Action,
                Order = this.Order
            };
            model.CopyMixinsFrom(this);
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowConnectionModel)model;
            this.Name = wModel.Name;
            this.DecisionOption = wModel.Type == ConnectionType.Decision ? wModel.DecisionOption : null;
            this.Type = wModel.Type;
            this.Condition = wModel.Condition;
            this.Action = wModel.Action;
            this.Order = wModel.Order;
            this.CopyMixinsFrom(wModel);
        }


        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name ?? BpmnElementId);

        internal string? DoneDecision() => Type == ConnectionType.Decision ? DecisionOption?.Name : (string?)null;

        protected override string? PropertyValidation(PropertyInfo pi)
        {

            if(pi.Name == nameof(DecisionOption))
            {
                return (pi, DecisionOption).IsSetOnlyWhen(Type == ConnectionType.Decision);
            }

            return base.PropertyValidation(pi);
        }

    }

    public enum ConnectionType
    {
        Normal,
        Decision,
        Jump,
        ScriptException,
    }

    [AutoInit]
    public static class WorkflowConnectionOperation
    {
        public static readonly ExecuteSymbol<WorkflowConnectionEntity> Save;
        public static readonly DeleteSymbol<WorkflowConnectionEntity> Delete;
    }

    [Serializable]
    public class WorkflowConnectionModel : ModelEntity
    {
        [InTypeScript(Undefined = false, Null = false)]
        public TypeEntity MainEntityType { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string? Name { get; set; }

        public DecisionOptionEmbedded? DecisionOption { get; set; }

        public bool NeedCondition { get; set; }

        public bool NeedOrder { get; set; }

        public ConnectionType Type { get; set; }

        public Lite<WorkflowConditionEntity>? Condition { get; set; }

        public Lite<WorkflowActionEntity>? Action { get; set; }

        public int? Order { get; set; }

        public MList<DecisionOptionEmbedded> DecisionOptions { get; set; } = new MList<DecisionOptionEmbedded>();

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if(pi.Name == nameof(DecisionOption) && DecisionOption == null && Type == ConnectionType.Decision)
            {
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
            }

            return base.PropertyValidation(pi);
        }
    }
}
