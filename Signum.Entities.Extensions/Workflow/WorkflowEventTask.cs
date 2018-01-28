using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.Scheduler;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Signum.Entities.Workflow
{
    [Serializable,  EntityKind(EntityKind.Shared, EntityData.Master)]
    public class WorkflowEventTaskEntity : Entity, ITaskEntity
    {
        [NotNullValidator]
        public Lite<WorkflowEntity> Workflow { get; set; }

        [Ignore]
        internal WorkflowEntity fullWorkflow { get; set; }

        public static Func<Lite<WorkflowEntity>, WorkflowEntity> GetWorkflowEntity;
        public WorkflowEntity GetWorkflow()
        {
            return fullWorkflow ?? GetWorkflowEntity(this.Workflow);
        }
        
        [NotNullValidator, UniqueIndex]
        public Lite<WorkflowEventEntity> Event { get; set; }

        public TriggeredOn TriggeredOn { get; set; }

        [NotifyChildProperty]
        public WorkflowEventTaskConditionEval Condition { get; set; }

        [NotNullValidator]
        [NotifyChildProperty]
        public WorkflowEventTaskActionEval Action { get; set; }


        static Expression<Func<WorkflowEventTaskEntity, string>> ToStringExpression = @this => @this.Workflow + " : " + @this.Event;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Condition))
            {
                if (TriggeredOn == TriggeredOn.Always && Condition != null)
                    return ValidationMessage._0IsSet.NiceToString(pi.NiceName());

                if (TriggeredOn != TriggeredOn.Always && Condition == null)
                    return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
            }
            return base.PropertyValidation(pi);
        }
    }


    [Serializable]
    public class WorkflowEventTaskModel : ModelEntity
    {
        public static Func<WorkflowEventEntity, WorkflowEventTaskModel> GetModel;
        public static Action<WorkflowEventEntity, WorkflowEventTaskModel> ApplyModel;

        public bool Suspended { get; set; }
        public IScheduleRuleEntity Rule { get; set; }

        public TriggeredOn TriggeredOn { get; set; }

        [NotifyChildProperty]
        public WorkflowEventTaskConditionEval Condition { get; set; }

        [NotNullValidator]
        [NotifyChildProperty]
        public WorkflowEventTaskActionEval Action { get; set; }
    }

    public enum TriggeredOn
    {
        Always,
        ConditionIsTrue,
        ConditionChangesToTrue,
    }

    [AutoInit]
    public static class WorkflowEventTaskOperation
    {
        public static readonly ExecuteSymbol<WorkflowEventTaskEntity> Save;
        public static readonly DeleteSymbol<WorkflowEventTaskEntity> Delete;
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class WorkflowEventTaskConditionResultEntity : Entity
    {
        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;

        public Lite<WorkflowEventTaskEntity> WorkflowEventTask { get; set; }

        public bool Result { get; set; }
    }

    [Serializable]
    public class WorkflowEventTaskConditionEval : EvalEmbedded<IWorkflowEventTaskConditionEvaluator>
    {
        protected override CompilationResult Compile()
        {
            var parent = (WorkflowEventTaskEntity)this.GetParentEntity();

            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var WorkflowEntityTypeName = parent.GetWorkflow().MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetAssemblies(),
                DynamicCode.GetUsingNamespaces() +
                    @"
                    namespace Signum.Entities.Workflow
                    {
                        class MyWorkflowEventTaskConditionEvaluator : IWorkflowEventTaskConditionEvaluator
                        {
                            public bool Evaluate()
                            {
                                " + script + @"
                            }
                        }                  
                    }");
        }
    }

    public interface IWorkflowEventTaskConditionEvaluator
    {
        bool Evaluate();
    }

    [Serializable]
    public class WorkflowEventTaskActionEval : EvalEmbedded<IWorkflowEventTaskActionEval>
    {
        protected override CompilationResult Compile()
        {
            var parent = (WorkflowEventTaskEntity)this.GetParentEntity();

            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var WorkflowEntityTypeName = parent.GetWorkflow().MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetAssemblies(),
                DynamicCode.GetUsingNamespaces() +
                    @"
                    namespace Signum.Entities.Workflow
                    {
                        class MyWorkflowEventTaskActionEvaluator : IWorkflowEventTaskActionEval
                        {
                            class CreateCaseEvaluator 
                            {
                                public List<ICaseMainEntity> cases = new List<ICaseMainEntity>();
                                public void Evaluate()
                                {
                                    " + script + @"
                                }

                                void CreateCase(" + WorkflowEntityTypeName + @" caseMainEntity)
                                {
                                    cases.Add(caseMainEntity);
                                }
                            }

                            public List<ICaseMainEntity> EvaluateUntyped()
                            {
                                var evaluator = new CreateCaseEvaluator();
                                evaluator.Evaluate();
                                return evaluator.cases;
                            }
                        }                  
                    }");
        }
    }

    public interface IWorkflowEventTaskActionEval
    {
        List<ICaseMainEntity> EvaluateUntyped();
    }
}
