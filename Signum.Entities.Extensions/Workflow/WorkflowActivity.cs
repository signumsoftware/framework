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
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class WorkflowActivityEntity : Entity, IWorkflowNodeEntity, IWithModel
    {
        [NotNullValidator]
        public WorkflowLaneEntity Lane { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string BpmnElementId { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 400, MultiLine = true)]
        public string Comments { get; set; }

        public WorkflowActivityType Type { get; set; }

        public bool RequiresOpen { get; set; }

        public WorkflowRejectEmbedded Reject { get; set; }
        
        [PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<WorkflowTimerEmbedded> Timers { get; set; } = new MList<WorkflowTimerEmbedded>();
        
        [Unit("min")]
        public double? EstimatedDuration { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 255)]
        public string ViewName { get; set; }
        
        [PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<WorkflowJumpEmbedded> Jumps { get; set; } = new MList<WorkflowJumpEmbedded>();

        [NotifyChildProperty]
        public WorkflowScriptPartEmbedded Script { get; set; }

        [NotNullValidator]
        public WorkflowXmlEmbedded Xml { get; set; }
        
        [NotifyChildProperty]
        public SubWorkflowEmbedded SubWorkflow { get; set; }

        [StringLengthValidator(AllowNulls = true, MultiLine = true)]
        public string UserHelp { get; set; }

        static Expression<Func<WorkflowActivityEntity, string>> ToStringExpression = @this => @this.Name ?? @this.BpmnElementId;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }



        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(SubWorkflow))
            {
                var requiresSubWorkflow = this.Type == WorkflowActivityType.CallWorkflow || this.Type == WorkflowActivityType.DecompositionWorkflow;
                if (SubWorkflow != null && !requiresSubWorkflow)
                    return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());

                if (SubWorkflow == null && requiresSubWorkflow)
                    return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
            }

            if (pi.Name == nameof(Script))
            {
                var requiresScript = this.Type == WorkflowActivityType.Script;
                if (Script != null && !requiresScript)
                    return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());

                if (Script == null && requiresScript)
                    return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
            }

            if (pi.Name == nameof(Timers) && this.Type == WorkflowActivityType.Delay)
            {
                if (Timers.Count != 1 || Timers.SingleEx().Interrupting == false)
                    return WorkflowValidationMessage.DelayActivitiesShouldHaveExactlyOneInterruptingTimer.NiceToString();
            }

            if (pi.Name == nameof(Jumps))
            {
                var repated = NoRepeatValidatorAttribute.ByKey(Jumps, j => j.To);
                if (repated.HasText())
                    return ValidationMessage._0HasSomeRepeatedElements1.NiceToString(pi.NiceName(), repated);

                if (Jumps.Any(j => j.To.RefersTo(this)))
                    return WorkflowMessage.JumpsToSameActivityNotAllowed.NiceToString();
            }
            return base.PropertyValidation(pi);
        }

        public ModelEntity GetModel()
        {
            var model = new WorkflowActivityModel()
            {
                WorkflowActivity = this.ToLite(),
                Workflow = this.Lane.Pool.Workflow
            };
            model.MainEntityType = model.Workflow.MainEntityType;
            model.Name = this.Name;
            model.Type = this.Type;
            model.RequiresOpen = this.RequiresOpen;
            model.Reject = this.Reject;
            model.Timers.AssignMList(this.Timers);
            model.EstimatedDuration = this.EstimatedDuration;
            model.Jumps.AssignMList(this.Jumps);
            model.Script = this.Script;
            model.ViewName = this.ViewName;
            model.UserHelp = this.UserHelp;
            model.SubWorkflow = this.SubWorkflow;
            model.Comments = this.Comments;
            return model;
        }

        public void SetModel(ModelEntity model)
        {
            var wModel = (WorkflowActivityModel)model;
            this.Name = wModel.Name;
            this.Type = wModel.Type;
            this.RequiresOpen = wModel.RequiresOpen;
            this.Reject = wModel.Reject;
            this.Timers.AssignMList(wModel.Timers);
            this.EstimatedDuration = wModel.EstimatedDuration;
            this.Jumps.AssignMList(wModel.Jumps);
            this.Script = wModel.Script;
            this.ViewName = wModel.ViewName;
            this.UserHelp = wModel.UserHelp;
            this.Comments = wModel.Comments;
            this.SubWorkflow = wModel.SubWorkflow;
        }
    }

    public class WorkflowActivityInfo
    {
        static readonly WorkflowActivityInfo Empty = new WorkflowActivityInfo();

        public static readonly ThreadVariable<WorkflowActivityInfo> CurrentVariable = Statics.ThreadVariable<WorkflowActivityInfo>("CurrentWorkflowActivity");
        public static WorkflowActivityInfo Current => CurrentVariable.Value ?? Empty;

        public static IDisposable Scope(WorkflowActivityInfo wa)
        {
            var old = Current;
            CurrentVariable.Value = wa;
            return new Disposable(() => CurrentVariable.Value = old);
        }

        public WorkflowActivityEntity WorkflowActivity { get; internal set; }
        public CaseActivityEntity CaseActivity { get; internal set; }
        public DecisionResult? DecisionResult { get; internal set; }
        public IWorkflowTransition Transition { get; internal set; }

        public bool Is(string workflowName, string activityName)
        {
            return this.WorkflowActivity != null && this.WorkflowActivity.Name == activityName && this.WorkflowActivity.Lane.Pool.Workflow.Name == workflowName;
        }


       
    }
    
    [Serializable]
    public class WorkflowScriptPartEmbedded : EmbeddedEntity, IWorkflowTransitionTo
    {
        public Lite<WorkflowScriptEntity> Script { get; set; }

        public WorkflowScriptRetryStrategyEntity RetryStrategy { get; set; }

        [NotNullValidator, ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity), typeof(WorkflowGatewayEntity))]
        public Lite<IWorkflowNodeEntity> OnFailureJump { get; set; }

        Lite<IWorkflowNodeEntity> IWorkflowTransitionTo.To => this.OnFailureJump;

        Lite<WorkflowConditionEntity> IWorkflowTransition.Condition => null;

        Lite<WorkflowActionEntity> IWorkflowTransition.Action => null;

        public WorkflowScriptPartEmbedded Clone()
        {
            return new WorkflowScriptPartEmbedded()
            {
                Script = this.Script,
                RetryStrategy = this.RetryStrategy,
                OnFailureJump = this.OnFailureJump,
            };
        }
    }

    [Serializable]
    public class WorkflowRejectEmbedded : EmbeddedEntity, IWorkflowTransition
    {
        public Lite<WorkflowConditionEntity> Condition { get; set; }

        public Lite<WorkflowActionEntity> Action { get; set; }
    }

    [Serializable]
    public class WorkflowTimerEmbedded : EmbeddedEntity, IWorkflowTransitionTo
    {
        public TimeSpanEmbedded Duration { get; set; }

        public Lite<WorkflowTimerConditionEntity> Condition { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100)]
        public string BpmnElementId { get; set; }

        [ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity), typeof(WorkflowGatewayEntity))]
        [NotNullValidator]
        public Lite<IWorkflowNodeEntity> To { get; set; }

        public Lite<WorkflowActionEntity> Action { get; set; }

        Lite<WorkflowConditionEntity> IWorkflowTransition.Condition => null;

        public bool Interrupting { get; set; }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if(pi.Name == nameof(Duration) && Duration == null && Condition == null)
            {
                return ValidationMessage._0IsMandatoryWhen1IsNotSet.NiceToString(pi.NiceName(), NicePropertyName(() => Condition));
            }
            return base.PropertyValidation(pi);
        }

        internal WorkflowTimerEmbedded Clone() => new WorkflowTimerEmbedded
        {
            Duration = Duration?.Clone(),
            Condition = Condition,
            BpmnElementId = BpmnElementId,
            To = To,
            Action = Action,
            Interrupting = Interrupting
        };
    }



    [Serializable]
    public class WorkflowJumpEmbedded : EmbeddedEntity, IWorkflowTransitionTo
    {
        [ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity), typeof(WorkflowGatewayEntity))]
        public Lite<IWorkflowNodeEntity> To { get; set; }

        public Lite<WorkflowConditionEntity> Condition { get; set; }

        public Lite<WorkflowActionEntity> Action { get; set; }

        public WorkflowJumpEmbedded Clone()
        {
            return new WorkflowJumpEmbedded
            {
                To = this.To,
                Condition = this.Condition,
                Action = this.Action
            };
        }
    }

    public enum WorkflowActivityType
    {
        Task,
        Decision,
        DecompositionWorkflow,
        CallWorkflow,
        Script,
        Delay
    }

    [AutoInit]
    public static class WorkflowActivityOperation
    {
        public static readonly ExecuteSymbol<WorkflowActivityEntity> Save;
        public static readonly DeleteSymbol<WorkflowActivityEntity> Delete;
    }
    
    [Serializable]
    public class SubWorkflowEmbedded : EmbeddedEntity
    {
        [NotNullValidator]
        public WorkflowEntity Workflow { get; set; }

        [NotNullValidator, NotifyChildProperty]
        public SubEntitiesEval SubEntitiesEval { get; set; }

        public SubWorkflowEmbedded Clone()
        {
            return new SubWorkflowEmbedded()
            {
                Workflow = this.Workflow,
                SubEntitiesEval = this.SubEntitiesEval.Clone(),
            };
        }
    }

    [Serializable]
    public class SubEntitiesEval : EvalEmbedded<ISubEntitiesEvaluator>
    {
        protected override CompilationResult Compile()
        {
            var decomposition = (SubWorkflowEmbedded)this.GetParentEntity();
            var activity = (WorkflowActivityEntity)decomposition.GetParentEntity();

            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var MainEntityTypeName = activity.Lane.Pool.Workflow.MainEntityType.ToType().FullName;
            var SubEntityTypeName = decomposition.Workflow.MainEntityType.ToType().FullName;

            return Compile(DynamicCode.GetAssemblies(),
                DynamicCode.GetUsingNamespaces() +
                    @"
                    namespace Signum.Entities.Workflow
                    {
                        class MySubEntitiesEvaluator : ISubEntitiesEvaluator
                        {
                            public List<ICaseMainEntity> GetSubEntities(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx)
                            {
                                return this.Evaluate((" + MainEntityTypeName + @")mainEntity, ctx).EmptyIfNull().Cast<ICaseMainEntity>().ToList();
                            }

                            IEnumerable<" + SubEntityTypeName + "> Evaluate(" + MainEntityTypeName + @" e, WorkflowTransitionContext ctx)
                            {
                                " + script + @"
                            }
                        }                  
                    }");
        }

        public SubEntitiesEval Clone()
        {
            return new SubEntitiesEval()
            {
                Script = this.Script
            };
        }
    }

    public interface ISubEntitiesEvaluator
    {
        List<ICaseMainEntity> GetSubEntities(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx);
    }

    [Serializable]
    public class WorkflowActivityModel : ModelEntity
    {
        public Lite<WorkflowActivityEntity>  WorkflowActivity { get; set; }

        public WorkflowEntity Workflow { get; set; }

        [NotNullValidator, InTypeScript(Undefined = false, Null = false)]
        public TypeEntity MainEntityType { get; set; }
        
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public WorkflowActivityType Type { get; set; }

        public bool RequiresOpen { get; set; }

        public WorkflowRejectEmbedded Reject { get; set; }
        
        [PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<WorkflowTimerEmbedded> Timers { get; set; } = new MList<WorkflowTimerEmbedded>();

        [Unit("min")]
        public double? EstimatedDuration { get; set; }

        [PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<WorkflowJumpEmbedded> Jumps { get; set; } = new MList<WorkflowJumpEmbedded>();

        public WorkflowScriptPartEmbedded Script { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 255)]
        public string ViewName { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 400, MultiLine = true)]
        public string Comments { get; set; }

        [StringLengthValidator(AllowNulls = true, MultiLine = true)]
        public string UserHelp { get; set; }

        public SubWorkflowEmbedded SubWorkflow { get; set; }
    }

    public enum WorkflowActivityMessage
    {
        [Description("Duplicate view name found: {0}")]
        DuplicateViewNameFound0,
        ChooseADestinationForWorkflowJumping,
        CaseFlow,
        AverageDuration,
        [Description("Activity Is")]
        ActivityIs,
        BehavesLikeParallelGateway,
        BehavesLikeExclusiveGateway,
        NoActiveTimerFound,
    }
}
