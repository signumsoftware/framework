using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class WorkflowPoolEntity : Entity, IWorkflowObjectEntity, IWithModel
    {
        [NotNullValidator]
        public WorkflowEntity Workflow { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string BpmnElementId { get; set; }

        [NotNullValidator, AvoidDump]
        public WorkflowXmlEmbedded Xml { get; set; }

        static Expression<Func<WorkflowPoolEntity, string>> ToStringExpression = @this => @this.Name ?? @this.BpmnElementId;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public ModelEntity GetModel()
        {
            var model = new WorkflowPoolModel()
            {
                Name = this.Name
            };
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowPoolModel)model;
            this.Name = wModel.Name;
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
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }
    }
}
