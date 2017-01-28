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
using Signum.Entities.Workflow;
using System.Reflection;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WorkflowActivityEntity : Entity, IWorkflowNodeEntity, IWithModel
    {
        [NotNullable]
        [NotNullValidator]
        public WorkflowLaneEntity Lane { get; set; }

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
        
        [NotifyChildProperty]
        public DecompositionEntity Decomposition { get; set; }

        static Expression<Func<WorkflowActivityEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Decomposition))
            {
                if (Decomposition != null && this.Type != WorkflowActivityType.DecompositionTask)
                    return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());

                if (Decomposition == null && this.Type == WorkflowActivityType.DecompositionTask)
                    return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
            }
            return base.PropertyValidation(pi);
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
            model.Decomposition = this.Decomposition;
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
            this.Decomposition = wModel.Decomposition;
        }
    }

    public enum WorkflowActivityType
    {
        Task,
        DecisionTask,
        DecompositionTask,
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

        public WorkflowActivityValidationEntity Clone()
        {
            return new WorkflowActivityValidationEntity
            {
                Rule = this.Rule,
                OnAccept = this.OnAccept,
                OnDecline = this.OnDecline
            };
        }
    }

    [Serializable]
    public class DecompositionEntity : EmbeddedEntity
    {
        [NotNullable]
        [NotNullValidator]
        public WorkflowEntity Workflow { get; set; }

        [NotNullable]
        [NotNullValidator, NotifyChildProperty]
        public SubEntitiesEval SubEntitiesEval { get; set; }
    }

    [Serializable]
    public class SubEntitiesEval : EvalEntity<ISubEntitiesEvaluator>
    {
        protected override CompilationResult Compile()
        {
            var decomposition = (DecompositionEntity)this.GetParentEntity();
            var activity = (WorkflowActivityEntity)decomposition.GetParentEntity();

            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var MainEntityTypeName = activity.Lane.Pool.Workflow.MainEntityType.ToType().FullName;
            var SubEntityTypeName = decomposition.Workflow.MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetAssemblies(),
                DynamicCode.GetNamespaces() +
                    @"
                    namespace Signum.Entities.Workflow
                    {
                        class MySubEntitiesEvaluator : ISubEntitiesEvaluator
                        {
                            public List<ICaseMainEntity> GetSubEntities(ICaseMainEntity mainEntity, WorkflowEvaluationContext ctx)
                            {
                                return this.Evaluate((" + MainEntityTypeName + @")mainEntity, ctx).EmptyIfNull().Cast<ICaseMainEntity>().ToList();
                            }

                            IEnumerable<" + SubEntityTypeName + "> Evaluate(" + MainEntityTypeName + @" e, WorkflowEvaluationContext ctx)
                            {
                                " + script + @"
                            }
                        }                  
                    }");
        }
    }

    public interface ISubEntitiesEvaluator
    {
        List<ICaseMainEntity> GetSubEntities(ICaseMainEntity mainEntity, WorkflowEvaluationContext ctx);
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

        public DecompositionEntity Decomposition { get; set; }
    }

    public enum WorkflowActivityMessage {
        [Description("Duplicate view name found: {0}")]
        DuplicateViewNameFound0,
    }

}
