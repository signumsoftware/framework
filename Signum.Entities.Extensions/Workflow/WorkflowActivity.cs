using Signum.Entities.Dynamic;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.ComponentModel;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WorkflowActivityEntity : Entity, IWorkflowNodeEntity, IWithModel
    {
        [NotNullable]
        [NotNullValidator]
        public WorkflowLaneEntity Lane { get; set; }
        
        [NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
        public int Thread { get; set; }

        [SqlDbType(Size = 100), NotNullable]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [SqlDbType(Size = 400)]
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 400, MultiLine = true)]
        public string Description { get; set; }

        public WorkflowActivityType Type { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 255)]
        public string ViewName { get; set; }

        [NotNullable]
        [NotNullValidator, NoRepeatValidator]
        public MList<WorkflowActivityValidationEntity> ValidationRules { get; set; } = new MList<WorkflowActivityValidationEntity>();

        [NotNullable]
        [NotNullValidator]
        public WorkflowXmlEntity Xml { get; set; }

        static Expression<Func<WorkflowActivityEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public ModelEntity GetModel()
        {
            var model = new WorkflowActivityModel();
            model.MainEntityType = this.Lane.Pool.Workflow.MainEntityType;
            model.Name = this.Name;
            model.Type = this.Type;
            model.ValidationRules.AssignMList(this.ValidationRules);
            model.ViewName = this.ViewName;
            model.Description = this.Description;
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowActivityModel)model;
            this.Name = wModel.Name;
            this.Type = wModel.Type;
            this.ValidationRules.AssignMList(wModel.ValidationRules);
            this.ViewName = wModel.ViewName;
            this.Description = wModel.Description;
        }
    }

    public enum WorkflowActivityType
    {
        Task,
        //UserTask,
        DecisionTask
    }

    [AutoInit]
    public static class WorkflowActivityOperation
    {
        public static readonly ExecuteSymbol<WorkflowActivityEntity> Save;
        public static readonly DeleteSymbol<WorkflowActivityEntity> Delete;
    }

    [Serializable]
    public class WorkflowActivityValidationEntity : EmbeddedEntity
    {
        [NotNullable]
        [NotNullValidator]
        public Lite<DynamicValidationEntity> Rule { get; set; }

        public bool OnAccept { get; set; }
        public bool OnDecline { get; set; }
    }

    [Serializable]
    public class WorkflowActivityModel : ModelEntity
    {
        [NotNullable]
        [NotNullValidator, InTypeScript(Undefined = false, Null = false)]
        public TypeEntity MainEntityType { get; set; }
        
        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public WorkflowActivityType Type { get; set; }

        [NotNullable]
        [NotNullValidator, NoRepeatValidator]
        public MList<WorkflowActivityValidationEntity> ValidationRules { get; set; } = new MList<WorkflowActivityValidationEntity>();

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 255)]
        public string ViewName { get; set; }

        [SqlDbType(Size = 400)]
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 400, MultiLine = true)]
        public string Description { get; set; }
    }

    public enum WorkflowActivityMessage {
        [Description("Duplicate view name found: {0}")]
        DuplicateViewNameFound0,
    }

}
