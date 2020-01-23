using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class WorkflowPoolEntity : Entity, IWorkflowObjectEntity, IWithModel
    {   
        public WorkflowEntity Workflow { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        public string? GetName() => Name;

        [StringLengthValidator(Min = 1, Max = 100)]
        public string BpmnElementId { get; set; }

        [AvoidDump]
        public WorkflowXmlEmbedded Xml { get; set; }

        [AutoExpressionField]
       public override string ToString() => As.Expression(() => Name ?? BpmnElementId);

        public ModelEntity GetModel()
        {
            var model = new WorkflowPoolModel()
            {
                Name = this.Name
            };
            model.CopyMixinsFrom(this);
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowPoolModel)model;
            this.Name = wModel.Name;
            this.CopyMixinsFrom(wModel);
        }
    }

    [AutoInit]
    public static class WorkflowPoolOperation
    {
        public static readonly ExecuteSymbol<WorkflowPoolEntity> Save;
        public static readonly DeleteSymbol<WorkflowPoolEntity> Delete;
    }

    [Serializable]
    public class WorkflowPoolModel : ModelEntity
    {
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }
    }
}
