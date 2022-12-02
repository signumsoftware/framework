using Signum.Entities.Dynamic;
using Signum.Entities.Basics;
using System.ComponentModel;
using System.Xml.Linq;

namespace Signum.Entities.Workflow;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class WorkflowActivityEntity : Entity, IWorkflowNodeEntity, IWithModel
{
    public WorkflowLaneEntity Lane { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public string? GetName() => Name;

    [StringLengthValidator(Min = 1, Max = 100)]
    public string BpmnElementId { get; set; }

    public WorkflowActivityType Type { get; set; }

    [StringLengthValidator(Min = 3, Max = 400, MultiLine = true)]
    public string? Comments { get; set; }

    public bool RequiresOpen { get; set; }

    [PreserveOrder]
    public MList<ButtonOptionEmbedded> DecisionOptions { get; set; } = new MList<ButtonOptionEmbedded>();

    public ButtonOptionEmbedded? CustomNextButton { get; set; }

    [Ignore, QueryableProperty]
    [NoRepeatValidator]
    public MList<WorkflowEventEntity> BoundaryTimers { get; set; } = new MList<WorkflowEventEntity>();

    [Unit("min")]
    public double? EstimatedDuration { get; set; }

    [StringLengthValidator(Min = 3, Max = 255)]
    public string? ViewName { get; set; }

    [PreserveOrder, NoRepeatValidator, BindParent]
    public MList<ViewNamePropEmbedded> ViewNameProps { get; set; } = new MList<ViewNamePropEmbedded>();

    [BindParent]
    public WorkflowScriptPartEmbedded? Script { get; set; }

    [AvoidDump]
    public WorkflowXmlEmbedded Xml { get; set; }

    [BindParent]
    public SubWorkflowEmbedded? SubWorkflow { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? UserHelp { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name ?? BpmnElementId);



    protected override string? PropertyValidation(PropertyInfo pi)
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

        if(pi.Name == nameof(ViewNameProps))
        {
            if (ViewNameProps.Count > 0 && !ViewName.HasText())
                return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());

            if (ViewName.HasText())
            {
                var dv = DynamicViewEntity.TryGetDynamicView(Lane.Pool.Workflow.MainEntityType.ToType(), ViewName);
                if (dv != null)
                    return ViewNamePropEmbedded.ValidateViewNameProps(dv, ViewNameProps);
            }
        }

        if (pi.Name == nameof(DecisionOptions))
        {
            return (pi, DecisionOptions).IsSetOnlyWhen(Type == WorkflowActivityType.Decision);
        }

        if (pi.Name == nameof(CustomNextButton) && CustomNextButton != null && Type != WorkflowActivityType.Task)
        {
            return ValidationMessage._0IsSet.NiceToString(pi.NiceName());
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
        model.BoundaryTimers.AssignMList(this.BoundaryTimers.Select(we => new WorkflowEventModel
        {
            Name = we.Name,
            MainEntityType = we.Lane.Pool.Workflow.MainEntityType,
            Type = we.Type,
            RunRepeatedly = we.RunRepeatedly,
            DecisionOptionName = we.DecisionOptionName,
            Timer = we.Timer,
            BpmnElementId = we.BpmnElementId
        }).ToMList());

        model.DecisionOptions.AssignMList(this.DecisionOptions);
        model.CustomNextButton = this.CustomNextButton;
        model.EstimatedDuration = this.EstimatedDuration;
        model.Script = this.Script;
        model.ViewName = this.ViewName;
        model.ViewNameProps.AssignMList(this.ViewNameProps);
        model.UserHelp = this.UserHelp;
        model.SubWorkflow = this.SubWorkflow;
        model.Comments = this.Comments;
        model.CopyMixinsFrom(this);

        return model;
    }

    public void SetModel(ModelEntity model)
    {
        var wModel = (WorkflowActivityModel)model;
        this.Name = wModel.Name;
        this.Type = wModel.Type;
        this.RequiresOpen = wModel.RequiresOpen;
        this.DecisionOptions.AssignMList(wModel.Type == WorkflowActivityType.Decision ? wModel.DecisionOptions : new MList<ButtonOptionEmbedded>());
        this.CustomNextButton =  wModel.CustomNextButton;
        // We can not set boundary timers in model
        //this.BoundaryTimers.AssignMList(wModel.BoundaryTimers);
        this.EstimatedDuration = wModel.EstimatedDuration;
        this.Script = wModel.Script;
        this.ViewName = wModel.ViewName;
        this.ViewNameProps.AssignMList(wModel.ViewNameProps);
        this.UserHelp = wModel.UserHelp;
        this.Comments = wModel.Comments;
        this.SubWorkflow = wModel.SubWorkflow;
        this.CopyMixinsFrom(wModel);
    }
}

public class ViewNamePropEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 100), IdentifierValidator(IdentifierType.Ascii)]
    public string Name { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? Expression { get; set; }

    internal static string? ValidateViewNameProps(DynamicViewEntity dv, MList<ViewNamePropEmbedded> viewNameProps)
    {
        var extra = viewNameProps.Where(a => !dv.Props.Any(p => p.Name == a.Name)).CommaAnd(a => a.Name);
        var missing = dv.Props.Where(p => !p.Type.EndsWith("?") && !viewNameProps.Any(a => a.Expression.HasText() && p.Name == a.Name)).CommaAnd(a => a.Name);

        return " and ".Combine(
            extra.HasText() ? "The ViewProps " + extra + " are not declared in " + dv.ViewName : null,
            missing.HasText() ? "The ViewProps " + missing + " are mandatory in " + dv.ViewName : null
            ).DefaultToNull();
    }
}

public class ButtonOptionEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public BootstrapStyle Style { get; set; }

    public void FromXml(XElement elem)
    {
        Name = elem.Attribute("Name")!.Value;
        Style = elem.Attribute("Style")!.Value.ToEnum<BootstrapStyle>();
    }

    public XElement ToXml(string elementName)
    {
        return new XElement(elementName,
                new XAttribute("Name", Name),
                new XAttribute("Style", Style.ToString())
            );
    }

    public ButtonOptionEmbedded Clone()
    {
        return new ButtonOptionEmbedded()
        {
            Name = this.Name,
            Style = this.Style,
        };
    }
}

public class WorkflowActivityInfo
{
    static readonly WorkflowActivityInfo Empty = new WorkflowActivityInfo();

    public static readonly AsyncThreadVariable<WorkflowActivityInfo> CurrentVariable = Statics.ThreadVariable<WorkflowActivityInfo>("CurrentWorkflowActivity");
    public static WorkflowActivityInfo Current => CurrentVariable.Value ?? Empty;

    public static IDisposable Scope(WorkflowActivityInfo wa)
    {
        var old = Current;
        CurrentVariable.Value = wa;
        return new Disposable(() => CurrentVariable.Value = old);
    }

    public WorkflowActivityEntity? WorkflowActivity => CaseActivity?.WorkflowActivity as WorkflowActivityEntity;
    public CaseActivityEntity? CaseActivity { get; internal set; }
    public WorkflowConnectionEntity? Connection { get; internal set; }
    public string? Decision { get; set; }

    public bool Is(string workflowName, string activityName)
    {
        return this.WorkflowActivity != null && this.WorkflowActivity.Name == activityName && this.WorkflowActivity.Lane.Pool.Workflow.Name == workflowName;
    }
}

public class WorkflowScriptPartEmbedded : EmbeddedEntity
{
    public Lite<WorkflowScriptEntity> Script { get; set; }

    public WorkflowScriptRetryStrategyEntity? RetryStrategy { get; set; }

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

public class SubWorkflowEmbedded : EmbeddedEntity
{   
    public WorkflowEntity Workflow { get; set; }

    [BindParent]
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

public class SubEntitiesEval : EvalEmbedded<ISubEntitiesEvaluator>
{
    protected override CompilationResult Compile()
    {
        var decomposition = this.GetParentEntity<SubWorkflowEmbedded>();
        var activity = decomposition.GetParentEntity<WorkflowActivityEntity>();

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

public class WorkflowActivityModel : ModelEntity
{
    public Lite<WorkflowActivityEntity>? WorkflowActivity { get; set; }

    public WorkflowEntity? Workflow { get; set; }

    public TypeEntity MainEntityType { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public WorkflowActivityType Type { get; set; }

    public bool RequiresOpen { get; set; }

    public MList<ButtonOptionEmbedded> DecisionOptions { get; set; } = new MList<ButtonOptionEmbedded>();

    public ButtonOptionEmbedded? CustomNextButton {get; set; }

    [PreserveOrder]
    [NoRepeatValidator]
    public MList<WorkflowEventModel> BoundaryTimers { get; set; } = new MList<WorkflowEventModel>();

    [Unit("min")]
    public double? EstimatedDuration { get; set; }

    public WorkflowScriptPartEmbedded? Script { get; set; }

    [StringLengthValidator(Min = 3, Max = 255)]
    public string? ViewName { get; set; }

    [PreserveOrder, NoRepeatValidator]
    public MList<ViewNamePropEmbedded> ViewNameProps { get; set; } = new MList<ViewNamePropEmbedded>();

    [StringLengthValidator(Min = 3, Max = 400, MultiLine = true)]
    public string? Comments { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string? UserHelp { get; set; }

    public SubWorkflowEmbedded? SubWorkflow { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(ViewNameProps))
        {
            if (ViewNameProps.Count > 0 && !ViewName.HasText())
                return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());

            if (ViewName.HasText() && Workflow != null)
            {
                var dv = DynamicViewEntity.TryGetDynamicView(Workflow.MainEntityType.ToType(), ViewName);
                if (dv != null)
                    return ViewNamePropEmbedded.ValidateViewNameProps(dv, ViewNameProps);
            }
        }

        if (pi.Name == nameof(DecisionOptions))
        {
            return (pi, DecisionOptions).IsSetOnlyWhen(Type == WorkflowActivityType.Decision);
        }

        if (pi.Name == nameof(CustomNextButton) && CustomNextButton != null && Type != WorkflowActivityType.Task)
        {
            return ValidationMessage._0IsSet.NiceToString(pi.NiceName());
        }

        return base.PropertyValidation(pi);
    }
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
    InprogressCaseActivities,
    OpenCaseActivityStats,
    LocateWorkflowActivityInDiagram,
    Approve,
    Decline,
}
