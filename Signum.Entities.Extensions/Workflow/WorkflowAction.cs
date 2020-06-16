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
    public class WorkflowActionEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        
        public TypeEntity MainEntityType { get; set; }

        [NotifyChildProperty]
        public WorkflowActionEval Eval { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);
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
            var parent = this.GetParentEntity<WorkflowActionEntity>();
            var script = this.Script.Trim();
            var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetCoreMetadataReferences()
                .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
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
