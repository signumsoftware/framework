using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class WorkflowGatewayEntity : Entity, IWorkflowNodeEntity, IWithModel
    {
        [NotNullValidator]
        public WorkflowLaneEntity Lane{ get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Name { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string BpmnElementId { get; set; }

        public WorkflowGatewayType Type { get; set; }
        public WorkflowGatewayDirection Direction { get; set; }

        [NotNullValidator, AvoidDump]
        public WorkflowXmlEmbedded Xml { get; set; }

        static Expression<Func<WorkflowGatewayEntity, string>> ToStringExpression = @this => @this.Name ?? @this.BpmnElementId;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
        public ModelEntity GetModel()
        {
            var model = new WorkflowGatewayModel()
            {
                Name = this.Name,
                Type = this.Type,
                Direction = this.Direction
            };
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowGatewayModel)model;
            this.Name = wModel.Name;
            this.Type = wModel.Type;
            this.Direction = wModel.Direction;
        }
    }

    public enum WorkflowGatewayType
    {
       Exclusive, // 1
       Inclusive, // 1...N
       Parallel   // N
    }

    public enum WorkflowGatewayDirection
    {
        Split,
        Join,
    }

    [AutoInit]
    public static class WorkflowGatewayOperation
    {
        public static readonly ExecuteSymbol<WorkflowGatewayEntity> Save;
        public static readonly DeleteSymbol<WorkflowGatewayEntity> Delete;
    }

    [Serializable]
    public class WorkflowGatewayModel : ModelEntity
    {
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public WorkflowGatewayType Type { get; set; }
        public WorkflowGatewayDirection Direction { get; set; }
    }

}
