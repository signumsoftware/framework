namespace Signum.Workflow;


[EntityKind(EntityKind.Main, EntityData.Master)]
public class WorkflowConnectionEntity : Entity, IWorkflowObjectEntity, IWithModel
{
    [ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity), typeof(WorkflowGatewayEntity))]
    [ForceNullable]
    public IWorkflowNodeEntity From { get; set; }

    [ImplementedBy(typeof(WorkflowActivityEntity), typeof(WorkflowEventEntity), typeof(WorkflowGatewayEntity))]
    [ForceNullable]
    public IWorkflowNodeEntity To { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? Name { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? DecisionOptionName { get; set; }

    public string? GetName() => Name;

    [StringLengthValidator(Min = 1, Max = 100)]
    public string BpmnElementId { get; set; }

    public ConnectionType Type { get; set; }

    public Lite<WorkflowConditionEntity>? Condition { get; set; }

    public Lite<WorkflowActionEntity>? Action { get; set; }

    public int? Order { get; set; }

    [AvoidDump]
    public WorkflowXmlEmbedded Xml { get; set; }

    public ModelEntity GetModel()
    {
        var model = new WorkflowConnectionModel()
        {
            MainEntityType = this.From!.Lane.Pool.Workflow.MainEntityType,
            Name = this.Name,
            DecisionOptionName = this.DecisionOptionName,
            Type = this.Type,
            Condition = this.Condition,
            Action = this.Action,
            Order = this.Order
        };
        model.CopyMixinsFrom(this);
        return model;
    }

    public void SetModel(ModelEntity model)
    {
        var wModel = (WorkflowConnectionModel)model;
        this.Name = wModel.Name;
        this.DecisionOptionName = wModel.Type == ConnectionType.Decision ? wModel.DecisionOptionName : null;
        this.Type = wModel.Type;
        this.Condition = wModel.Condition;
        this.Action = wModel.Action;
        this.Order = wModel.Order;
        this.CopyMixinsFrom(wModel);
    }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name ?? BpmnElementId);

    internal string? DoneDecision() => Type == ConnectionType.Decision ? DecisionOptionName : (string?)null;

    protected override string? PropertyValidation(PropertyInfo pi)
    {

        if(pi.Name == nameof(DecisionOptionName))
        {
            return (pi, DecisionOptionName).IsSetOnlyWhen(Type == ConnectionType.Decision);
        }

        return base.PropertyValidation(pi);
    }

}

public enum ConnectionType
{
    Normal,
    Decision,
    Jump,
    ScriptException,
}

[AutoInit]
public static class WorkflowConnectionOperation
{
    public static readonly ExecuteSymbol<WorkflowConnectionEntity> Save;
    public static readonly DeleteSymbol<WorkflowConnectionEntity> Delete;
}

public class WorkflowConnectionModel : ModelEntity
{
    public TypeEntity MainEntityType { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? Name { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? DecisionOptionName { get; set; }

    public bool NeedCondition { get; set; }

    public bool NeedOrder { get; set; }

    public ConnectionType Type { get; set; }

    public Lite<WorkflowConditionEntity>? Condition { get; set; }

    public Lite<WorkflowActionEntity>? Action { get; set; }

    public int? Order { get; set; }

    public MList<ButtonOptionEmbedded> DecisionOptions { get; set; } = new MList<ButtonOptionEmbedded>();

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if(pi.Name == nameof(DecisionOptionName) && DecisionOptionName == null && Type == ConnectionType.Decision)
        {
            return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());
        }

        return base.PropertyValidation(pi);
    }
}
