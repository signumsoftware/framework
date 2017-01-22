using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class WorkflowEventEntity : Entity, IWorkflowNodeEntity, IWithModel
    {
        [SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Name { get; set; }
        
        [NotNullable]
        [NotNullValidator]
        public WorkflowLaneEntity Lane { get; set; }

        public WorkflowEventType Type { get; set; }

        [NotNullable]
        [NotNullValidator]
        public WorkflowXmlEntity Xml { get; set; }

        static Expression<Func<WorkflowEventEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public ModelEntity GetModel()
        {
            var model = new WorkflowEventModel();
            model.Name = this.Name;
            model.Type = this.Type;
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowEventModel)model;
            this.Name = wModel.Name;
            this.Type = wModel.Type;
        }
    }

    public enum WorkflowEventType
    {
        Start,
        Finish
    }

    [AutoInit]
    public static class WorkflowEventOperation
    {
        public static readonly ExecuteSymbol<WorkflowEventEntity> Save;
        public static readonly DeleteSymbol<WorkflowEventEntity> Delete;
    }

    [Serializable]
    public class WorkflowEventModel : ModelEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public WorkflowEventType Type { get; set; }
    }

}
