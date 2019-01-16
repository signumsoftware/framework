using Signum.Entities.Dynamic;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Reflection;

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

        [Ignore, QueryableProperty]
        [NotNullValidator, NoRepeatValidator]
        public MList<WorkflowEventEntity> BoundaryTimers { get; set; } = new MList<WorkflowEventEntity>();

        [Unit("min")]
        public double? EstimatedDuration { get; set; }

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 255)]
        public string ViewName { get; set; }

        [NotifyChildProperty]
        public WorkflowScriptPartEmbedded Script { get; set; }

        [NotNullValidator, AvoidDump]
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
            model.BoundaryTimers.AssignMList(this.BoundaryTimers.Select(we => new WorkflowEventModel()
            {
                Name = we.Name,
                MainEntityType = we.Lane.Pool.Workflow.MainEntityType,
                Type = we.Type,
                Timer = we.Timer,
                BpmnElementId = we.BpmnElementId
            }).ToMList());
            model.EstimatedDuration = this.EstimatedDuration;
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
            // We can not set boundary timers in model
            //this.BoundaryTimers.AssignMList(wModel.BoundaryTimers);
            this.EstimatedDuration = wModel.EstimatedDuration;
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

        public WorkflowActivityEntity WorkflowActivity => CaseActivity?.WorkflowActivity as WorkflowActivityEntity;
        public CaseActivityEntity CaseActivity { get; internal set; }
        public WorkflowConnectionEntity Connection { get; internal set; }

        public bool Is(string workflowName, string activityName)
        {
            return this.WorkflowActivity != null && this.WorkflowActivity.Name == activityName && this.WorkflowActivity.Lane.Pool.Workflow.Name == workflowName;
        }
    }

    [Serializable]
    public class WorkflowScriptPartEmbedded : EmbeddedEntity
    {
        public Lite<WorkflowScriptEntity> Script { get; set; }

        public WorkflowScriptRetryStrategyEntity RetryStrategy { get; set; }

        public WorkflowScriptPartEmbedded Clone()
        {
            return new WorkflowScriptPartEmbedded()
            {
                Script = this.Script,
                RetryStrategy = this.RetryStrategy,
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

            return Compile(DynamicCode.GetCoreMetadataReferences()
                .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
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

        [PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<WorkflowEventModel> BoundaryTimers { get; set; } = new MList<WorkflowEventModel>();

        [Unit("min")]
        public double? EstimatedDuration { get; set; }

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
        NoActiveTimerFound,
        InprogressWorkflowActivities,
        OpenCaseActivityStats,
        LocateWorkflowActivityInDiagram,
    }
}
