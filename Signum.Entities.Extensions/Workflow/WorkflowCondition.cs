using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class WorkflowConditionEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public TypeEntity MainEntityType { get; set; }

        [NotNullable]
        [NotNullValidator, NotifyChildProperty]
        public WorkflowConnectionEval Eval { get; set; }

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
        public static readonly ExecuteSymbol<WorkflowConditionEntity> Save;
        public static readonly DeleteSymbol<WorkflowConditionEntity> Delete;
    }

    [Serializable]
    public class WorkflowConnectionEval : EvalEntity<IWorkflowEvaluator>
    {
        protected override CompilationResult Compile()
        {
            var parent = (WorkflowConditionEntity)this.GetParentEntity();

            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetAssemblies(),
                DynamicCode.GetNamespaces() +
                    @"
                    namespace Signum.Entities.Workflow
                    {
                        class MyWorkflowEvaluator : IWorkflowEvaluator
                        {
                            public bool EvaluateUntyped(ICaseMainEntity mainEntity, WorkflowEvaluationContext ctx)
                            {
                                return this.Evaluate((" + WorkflowEntityTypeName + @")mainEntity, ctx);
                            }

                            bool Evaluate(" + WorkflowEntityTypeName + @" e, WorkflowEvaluationContext ctx)
                            {
                                " + script + @"
                            }
                        }                  
                    }");
        }
    }

    public interface IWorkflowEvaluator
    {
        bool EvaluateUntyped(ICaseMainEntity mainEntity, WorkflowEvaluationContext ctx);
    }

    public class WorkflowEvaluationContext
    {
        public CaseActivityEntity CaseActivity;
        public DecisionResult? DecisionResult;
    }
}
