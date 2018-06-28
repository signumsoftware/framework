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
    public class WorkflowTimerConditionEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullValidator]
        public TypeEntity MainEntityType { get; set; }

        [NotNullValidator, NotifyChildProperty]
        public WorkflowTimerConditionEval Eval { get; set; }

        static Expression<Func<WorkflowTimerConditionEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class WorkflowTimerConditionOperation
    {
        public static readonly ConstructSymbol<WorkflowTimerConditionEntity>.From<WorkflowTimerConditionEntity> Clone;
        public static readonly ExecuteSymbol<WorkflowTimerConditionEntity> Save;
        public static readonly DeleteSymbol<WorkflowTimerConditionEntity> Delete;
    }

    [Serializable]
    public class WorkflowTimerConditionEval : EvalEmbedded<IWorkflowTimerConditionEvaluator>
    {
        protected override CompilationResult Compile()
        {
            var parent = (WorkflowTimerConditionEntity)this.GetParentEntity();

            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var WorkflowEntityTypeName = parent.MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetAssemblies(),
                DynamicCode.GetUsingNamespaces() +
                    @"
                    namespace Signum.Entities.Workflow
                    {
                        class MyWorkflowTimerConditionEvaluator : IWorkflowTimerConditionEvaluator
                        {

                            public bool EvaluateUntyped(CaseActivityEntity ca, DateTime now)
                            {
                                return this.Evaluate(ca, (" + WorkflowEntityTypeName + @")ca.Case.MainEntity, now);
                            }

                            bool Evaluate(CaseActivityEntity ca, " + WorkflowEntityTypeName + @" e, DateTime now)
                            {
                                " + script + @"
                            }
                        }                  
                    }");
        }
    }

    public interface IWorkflowTimerConditionEvaluator
    {
        bool EvaluateUntyped(CaseActivityEntity ca, DateTime now);
    }
}
