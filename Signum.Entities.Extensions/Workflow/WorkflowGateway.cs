using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class WorkflowGatewayEntity : Entity, IWorkflowNodeEntity, IWithModel
    {   
        public WorkflowLaneEntity Lane { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string? Name { get; set; }
        
        public string? GetName() => Name;

        [StringLengthValidator(Min = 1, Max = 100)]
        public string BpmnElementId { get; set; }

        public WorkflowGatewayType Type { get; set; }
        public WorkflowGatewayDirection Direction { get; set; }

        [AvoidDump]
        public WorkflowXmlEmbedded Xml { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name ?? BpmnElementId);
        public ModelEntity GetModel()
        {
            var model = new WorkflowGatewayModel()
            {
                Name = this.Name,
                Type = this.Type,
                Direction = this.Direction
            };
            model.CopyMixinsFrom(this);
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowGatewayModel)model;
            this.Name = wModel.Name;
            this.Type = wModel.Type;
            this.Direction = wModel.Direction;
            this.CopyMixinsFrom(wModel);
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
        [StringLengthValidator(Min = 3, Max = 100)]
        public string? Name { get; set; }

        public WorkflowGatewayType Type { get; set; }
        public WorkflowGatewayDirection Direction { get; set; }
    }

}
