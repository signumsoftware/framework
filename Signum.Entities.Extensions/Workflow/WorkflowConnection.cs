using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Workflow
{

    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WorkflowConnectionEntity : Entity, IWorkflowObjectEntity, IWithModel
    {
        [ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity), typeof(WorkflowGatewayEntity))]
        //[NotNullValidator] needs to be disabled temporally
        public IWorkflowNodeEntity From { get; set; }

        [ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity), typeof(WorkflowGatewayEntity))]
        //[NotNullValidator] needs to be disabled temporally
        public IWorkflowNodeEntity To { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Name { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string BpmnElementId { get; set; }

        public ConnectionType Type { get; set; }

        public Lite<WorkflowConditionEntity> Condition { get; set; }

        public Lite<WorkflowActionEntity> Action { get; set; }

        public int? Order { get; set; }

        [NotNullValidator, AvoidDump]
        public WorkflowXmlEmbedded Xml { get; set; }

        public ModelEntity GetModel()
        {
            var model = new WorkflowConnectionModel()
            {
                MainEntityType = this.From.Lane.Pool.Workflow.MainEntityType,
                Name = this.Name,
                Type = this.Type,
                Condition = this.Condition,
                Action = this.Action,
                Order = this.Order
            };
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowConnectionModel)model;
            this.Name = wModel.Name;
            this.Type = wModel.Type;
            this.Condition = wModel.Condition;
            this.Action = wModel.Action;
            this.Order = wModel.Order;
        }


        static Expression<Func<WorkflowConnectionEntity, string>> ToStringExpression = @this => @this.Name ?? @this.BpmnElementId;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public enum ConnectionType
    {
        Normal,
        Approve,
        Decline,
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
        [NotNullValidator, InTypeScript(Undefined = false, Null = false)]
        public TypeEntity MainEntityType { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Name { get; set; }

        public bool NeedCondition { get; set; }
        public bool NeedOrder { get; set; }

        public ConnectionType Type { get; set; }

        public Lite<WorkflowConditionEntity> Condition { get; set; }

        public Lite<WorkflowActionEntity> Action { get; set; }

        public int? Order { get; set; }
    }
}
