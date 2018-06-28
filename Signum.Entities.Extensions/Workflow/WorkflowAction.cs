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
    public class WorkflowActionEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullValidator]
        public TypeEntity MainEntityType { get; set; }

        [NotNullValidator, NotifyChildProperty]
        public WorkflowActionEval Eval { get; set; }

        static Expression<Func<WorkflowActionEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class WorkflowActionOperation
    {
        public static readonly ConstructSymbol<WorkflowActionEntity>.From<WorkflowActionEntity> Clone;
        public static readonly ExecuteSymbol<WorkflowActionEntity> Save;
        public static readonly DeleteSymbol<WorkflowActionEntity> Delete;
    }

    [Serializable]
    public class WorkflowActionEval : EvalEmbedded<IWorkflowActionExecutor>
    {
        protected override CompilationResult Compile()
        {
            var parent = (WorkflowActionEntity)this.GetParentEntity();

            var script = this.Script.Trim();
            var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetAssemblies(),
                DynamicCode.GetUsingNamespaces() +
                    @"
                    namespace Signum.Entities.Workflow
                    {
                        class MyWorkflowActionEvaluator : IWorkflowActionExecutor
                        {
                            public void ExecuteUntyped(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx)
                            {
                                this.Execute((" + WorkflowEntityTypeName + @")mainEntity, ctx);
                            }

                            void Execute(" + WorkflowEntityTypeName + @" e, WorkflowTransitionContext ctx)
                            {
                                " + script + @"
                            }
                        }                  
                    }");
        }
    }

    public interface IWorkflowActionExecutor
    {
        void ExecuteUntyped(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx);
    }
}
