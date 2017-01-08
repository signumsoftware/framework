using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WorkflowLaneEntity : Entity, IWorkflowObjectEntity, IWithModel
    {
        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullable]
        [NotNullValidator]
        public WorkflowXmlEntity Xml { get; set; }

        [NotNullable]
        [NotNullValidator]
        public WorkflowPoolEntity Pool { get; set; }

        [NotNullable, ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
        [NotNullValidator, NoRepeatValidator, CountIsValidator(ComparisonType.GreaterThan, 0)]
        public MList<Lite<Entity>> Actors { get; set; } = new MList<Lite<Entity>>();

        static Expression<Func<WorkflowLaneEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public ModelEntity GetModel()
        {
            var model = new WorkflowLaneModel();
            model.Actors.AssignMList(this.Actors);
            model.Name = this.Name;
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowLaneModel)model;
            this.Name = wModel.Name;
            this.Actors.AssignMList(wModel.Actors);
        }
    }

    [AutoInit]
    public static class WorkflowLaneOperation
    {
        public static readonly ExecuteSymbol<WorkflowLaneEntity> Save;
        public static readonly DeleteSymbol<WorkflowLaneEntity> Delete;
    }

    [Serializable]
    public class WorkflowLaneModel : ModelEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullable, ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
        [NotNullValidator, NoRepeatValidator, CountIsValidator(ComparisonType.GreaterThan, 0)]
        public MList<Lite<Entity>> Actors { get; set; } = new MList<Lite<Entity>>();
    }
}
