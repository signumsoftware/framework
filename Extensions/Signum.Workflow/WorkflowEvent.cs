using System.Xml.Linq;

namespace Signum.Workflow;

[EntityKind(EntityKind.String, EntityData.Master)]
public class WorkflowEventEntity : Entity, IWorkflowNodeEntity, IWithModel
{
    [StringLengthValidator(Min = 3, Max = 100)]
    public string? Name { get; set; }
    
    public string? GetName() => Name;

    [StringLengthValidator(Min = 1, Max = 100)]
    public string BpmnElementId { get; set; }
    
    public WorkflowLaneEntity Lane { get; set; }

    public WorkflowEventType Type { get; set; }

    public bool RunRepeatedly { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)] 
    public string? DecisionOptionName { get; set; }


     public WorkflowTimerEmbedded? Timer { get; set; }

    public Lite<WorkflowActivityEntity>? BoundaryOf { get; set; }

    [AvoidDump]
    public WorkflowXmlEmbedded Xml { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name ?? BpmnElementId);

    public ModelEntity GetModel()
    {
        var model = new WorkflowEventModel()
        {
            MainEntityType = this.Lane.Pool.Workflow.MainEntityType,
            Name = this.Name,
            Type = this.Type,
            RunRepeatedly = this.RunRepeatedly,
            DecisionOptionName = this.DecisionOptionName,
            Task = WorkflowEventTaskModel.GetModel(this),
            Timer = this.Timer,
            BpmnElementId = this.BpmnElementId,
        };
        model.CopyMixinsFrom(this);
        return model;
    }

    public void SetModel(ModelEntity model)
    {
        var wModel = (WorkflowEventModel)model;
        this.Name = wModel.Name;
        this.Type = wModel.Type;
        this.RunRepeatedly = wModel.RunRepeatedly;
        this.DecisionOptionName = wModel.DecisionOptionName;
        this.Timer = wModel.Timer;
        this.BpmnElementId = wModel.BpmnElementId;
        this.CopyMixinsFrom(wModel);
        //WorkflowEventTaskModel.ApplyModel(this, wModel.Task);
    }

    protected override void PreSaving(PreSavingContext ctx)
    {
        if (Type != WorkflowEventType.BoundaryForkTimer && RunRepeatedly)
            RunRepeatedly = false;

        if (Type != WorkflowEventType.BoundaryInterruptingTimer && !string.IsNullOrWhiteSpace(DecisionOptionName))
            DecisionOptionName = null;

        base.PreSaving(ctx);
    }
}

public class WorkflowTimerEmbedded : EmbeddedEntity
{
    public TimeSpanEmbedded? Duration { get; set; }

    public Lite<WorkflowTimerConditionEntity>? Condition { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Duration) && Duration == null && Condition == null)
            return ValidationMessage._0IsMandatoryWhen1IsNotSet.NiceToString(pi.NiceName(), NicePropertyName(() => Condition));

        if (pi.Name == nameof(Duration) && Duration != null && Condition != null)
            return ValidationMessage._0ShouldBeNullWhen1IsSet.NiceToString(NicePropertyName(() => Condition), pi.NiceName());

        return base.PropertyValidation(pi);
    }

    public WorkflowTimerEmbedded Clone()
    {
        WorkflowTimerEmbedded result = new WorkflowTimerEmbedded
        {
            Condition = this.Condition,
            Duration = this.Duration == null ? null : this.Duration.Clone(),
        };

        return result;
    }
}

public enum WorkflowEventType
{
    Start,
    ScheduledStart,
    Finish,
    BoundaryForkTimer,
    BoundaryInterruptingTimer,
    IntermediateTimer,
}


public static class WorkflowEventTypeExtension
{
    public static bool IsStart(this WorkflowEventType type) =>
        type == WorkflowEventType.Start ||
        type == WorkflowEventType.ScheduledStart;

    public static bool IsScheduledStart(this WorkflowEventType type) =>
        type == WorkflowEventType.ScheduledStart;

    public static bool IsFinish(this WorkflowEventType type) =>
        type == WorkflowEventType.Finish;

    public static bool IsTimer(this WorkflowEventType type) =>
        type == WorkflowEventType.BoundaryForkTimer ||
        type == WorkflowEventType.BoundaryInterruptingTimer ||
        type == WorkflowEventType.IntermediateTimer;

    public static bool IsBoundaryTimer(this WorkflowEventType type) =>
        type == WorkflowEventType.BoundaryForkTimer ||
        type == WorkflowEventType.BoundaryInterruptingTimer;
}

[AutoInit]
public static class WorkflowEventOperation
{
    public static readonly ExecuteSymbol<WorkflowEventEntity> Save;
    public static readonly DeleteSymbol<WorkflowEventEntity> Delete;
}

public class WorkflowEventModel : ModelEntity
{
    public TypeEntity MainEntityType { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? Name { get; set; }

    public WorkflowEventType Type { get; set; }

    public bool RunRepeatedly { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? DecisionOptionName { get; set; }

    public WorkflowEventTaskModel? Task { get; set; }

    public WorkflowTimerEmbedded? Timer { get; set; }

    public string BpmnElementId { get; set; }
}

public class TimeSpanEmbedded : EmbeddedEntity
{
    public int Days { get; set; }

    public int Hours { get; set; }

    public int Minutes { get; set; }

    public int Seconds { get; set; }

    public bool IsZero()
    {
        return Days == 0 && Hours == 0 && Minutes == 0 && Seconds == 0;
    }

    [AutoExpressionField]
    public DateTime Add(DateTime date) => As.Expression(() => date.AddDays(Days).AddHours(Hours).AddMinutes(Minutes).AddSeconds(Seconds));

    [AutoExpressionField]
    public DateTime Subtract(DateTime date) => As.Expression(() => date.AddDays(-Days).AddHours(-Hours).AddMinutes(-Minutes).AddMinutes(-Seconds));

    public TimeSpan ToTimeSpan()
    {
        return new TimeSpan(Days, Hours, Minutes, Seconds);
    }

    public override string ToString()
    {
        return ToTimeSpan().ToString();
    }

    public TimeSpanEmbedded Clone()
    {
        TimeSpanEmbedded ds = new TimeSpanEmbedded
        {
            Days = this.Days,
            Hours = this.Hours,
            Minutes = this.Minutes,
            Seconds = this.Seconds,
        };

        return ds;
    }


    public XElement ToXml(string elementName)
    {
        return new XElement(elementName,
            new XAttribute("Days", Days),
            new XAttribute("Hours", Hours),
            new XAttribute("Minutes", Minutes),
            new XAttribute("Seconds", Seconds));
    }


    public void FromXml(XElement element)
    {
        Days = int.Parse(element.Attribute("Days")!.Value);
        Hours = int.Parse(element.Attribute("Hours")!.Value);
        Minutes = int.Parse(element.Attribute("Minutes")!.Value);
        Seconds = int.Parse(element.Attribute("Seconds")!.Value);
    }
}
