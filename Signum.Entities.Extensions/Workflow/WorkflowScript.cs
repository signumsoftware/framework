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
    [Serializable]
    public class WorkflowScriptEntity : EmbeddedEntity, IWorkflowTransitionTo
    {
        [NotNullable]
        [NotNullValidator, NotifyChildProperty]
        public WorkflowScriptEval Eval { get; set; }

        public WorkflowScriptRetryStrategyEntity RetryStrategy { get; set; }
        
        [NotNullable]
        [NotNullValidator, ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity), typeof(WorkflowGatewayEntity))]
        public Lite<IWorkflowNodeEntity> OnFailureJump { get; set; }

        Lite<IWorkflowNodeEntity> IWorkflowTransitionTo.To => this.OnFailureJump;

        Lite<WorkflowConditionEntity> IWorkflowTransition.Condition => null;

        Lite<WorkflowActionEntity> IWorkflowTransition.Action => null;
    }

    [Serializable]
    public class WorkflowScriptEval : EvalEntity<IWorkflowScriptExecutor>
    {
        [SqlDbType(Size = int.MaxValue)]
        [StringLengthValidator(AllowNulls = true, MultiLine = true)]
        public string CustomTypes { get; set; }

        protected override CompilationResult Compile()
        {
            var parent = (WorkflowActivityEntity)this.GetParentEntity().GetParentEntity();

            var script = this.Script.Trim();
            var WorkflowEntityTypeName = parent.Lane.Pool.Workflow.MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetAssemblies(),
                DynamicCode.GetNamespaces() +
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
        public CaseActivityEntity CaseActivity { get; internal set; }
        public int RetryCount { get; internal set; }
    }
}
