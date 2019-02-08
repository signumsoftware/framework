using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class WorkflowConditionEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        
        public TypeEntity MainEntityType { get; set; }

        [NotNullValidator, NotifyChildProperty]
        public WorkflowConditionEval Eval { get; set; }

        static Expression<Func<WorkflowConditionEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class WorkflowConditionOperation
    {
        public static readonly ConstructSymbol<WorkflowConditionEntity>.From<WorkflowConditionEntity> Clone;
        public static readonly ExecuteSymbol<WorkflowConditionEntity> Save;
        public static readonly DeleteSymbol<WorkflowConditionEntity> Delete;
    }

    [Serializable]
    public class WorkflowConditionEval : EvalEmbedded<IWorkflowConditionEvaluator>
    {
        protected override CompilationResult Compile()
        {
            var parent = (WorkflowConditionEntity)this.GetParentEntity()!;

            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetCoreMetadataReferences()
                .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
                    @"
                    namespace Signum.Entities.Workflow
                    {
                        class MyWorkflowConditionEvaluator : IWorkflowConditionEvaluator
                        {
                            public bool EvaluateUntyped(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx)
                            {
                                return this.Evaluate((" + WorkflowEntityTypeName + @")mainEntity, ctx);
                            }

                            bool Evaluate(" + WorkflowEntityTypeName + @" e, WorkflowTransitionContext ctx)
                            {
                                " + script + @"
                            }
                        }
                    }");
        }
    }

    public interface IWorkflowConditionEvaluator
    {
        bool EvaluateUntyped(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx);
    }
}
