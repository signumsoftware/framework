using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class WorkflowScriptEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }
        
        public TypeEntity MainEntityType { get; set; }

        [NotifyChildProperty]
        public WorkflowScriptEval Eval { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);
    }

    [AutoInit]
    public static class WorkflowScriptOperation
    {
        public static readonly ConstructSymbol<WorkflowScriptEntity>.From<WorkflowScriptEntity> Clone;
        public static readonly ExecuteSymbol<WorkflowScriptEntity> Save;
        public static readonly DeleteSymbol<WorkflowScriptEntity> Delete;
    }


    [Serializable]
    public class WorkflowScriptEval : EvalEmbedded<IWorkflowScriptExecutor>
    {
        [StringLengthValidator(MultiLine = true)]
        public string? CustomTypes { get; set; }

        protected override CompilationResult Compile()
        {
            var parent = this.GetParentEntity<WorkflowScriptEntity>();

            var script = this.Script.Trim();
            var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetCoreMetadataReferences()
                .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
                    @"
                    namespace Signum.Entities.Workflow
                    {
                        class MyWorkflowScriptEvaluator : IWorkflowScriptExecutor
                        {
                            public void ExecuteUntyped(ICaseMainEntity mainEntity, WorkflowScriptContext ctx)
                            {
                                this.Execute((" + WorkflowEntityTypeName + @")mainEntity, ctx);
                            }

                            void Execute(" + WorkflowEntityTypeName + @" e, WorkflowScriptContext ctx)
                            {
                                " + script + @"
                            }
                        }

                        " + CustomTypes + @"
                    }");
        }
    }

    public interface IWorkflowScriptExecutor
    {
        void ExecuteUntyped(ICaseMainEntity mainEntity, WorkflowScriptContext ctx);
    }

    public class WorkflowScriptContext
    {
        public CaseActivityEntity? CaseActivity { get; internal set; }
        public int RetryCount { get; internal set; }
    }
}
