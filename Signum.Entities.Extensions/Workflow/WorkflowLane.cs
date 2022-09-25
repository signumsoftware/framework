using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Dynamic;

namespace Signum.Entities.Workflow;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class WorkflowLaneEntity : Entity, IWorkflowObjectEntity, IWithModel
{
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public string? GetName() => Name;

    [StringLengthValidator(Min = 1, Max = 100)]
    public string BpmnElementId { get; set; }

    [AvoidDump]
    public WorkflowXmlEmbedded Xml { get; set; }

    
    public WorkflowPoolEntity Pool { get; set; }

    [ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
    [NoRepeatValidator]
    public MList<Lite<Entity>> Actors { get; set; } = new MList<Lite<Entity>>();

    [BindParent]
    public WorkflowLaneActorsEval? ActorsEval { get; set; }

    public bool UseActorEvalForStart { get; set; }

    public bool CombineActorAndActorEvalWhenContinuing { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(UseActorEvalForStart) && UseActorEvalForStart == true && ActorsEval == null)
            return ValidationMessage._0ShouldBe12.NiceToString(pi.NiceName(), ComparisonType.EqualTo.NiceToString(), false);

        if (pi.Name == nameof(CombineActorAndActorEvalWhenContinuing) && CombineActorAndActorEvalWhenContinuing == true && (ActorsEval == null || Actors.IsEmpty()))
            return ValidationMessage._0ShouldBe12.NiceToString(pi.NiceName(), ComparisonType.EqualTo.NiceToString(), false);

        return base.PropertyValidation(pi);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name ?? BpmnElementId);

    public ModelEntity GetModel()
    {
        var model = new WorkflowLaneModel()
        {
            MainEntityType = this.Pool.Workflow.MainEntityType
        };
        model.Actors.AssignMList(this.Actors);
        model.ActorsEval = this.ActorsEval;
        model.Name = this.Name;
        model.UseActorEvalForStart = this.UseActorEvalForStart;
        model.CombineActorAndActorEvalWhenContinuing = this.CombineActorAndActorEvalWhenContinuing;
        model.CopyMixinsFrom(this);
        return model;
    }

    public void SetModel(ModelEntity model)
    {
        var wModel = (WorkflowLaneModel)model;
        this.Name = wModel.Name;
        this.ActorsEval = wModel.ActorsEval;
        this.Actors.AssignMList(wModel.Actors);
        this.UseActorEvalForStart = wModel.UseActorEvalForStart;
        this.CombineActorAndActorEvalWhenContinuing = wModel.CombineActorAndActorEvalWhenContinuing;
        this.CopyMixinsFrom(wModel);
    }
}

public class WorkflowLaneActorsEval : EvalEmbedded<IWorkflowLaneActorsEvaluator>
{
    protected override CompilationResult Compile()
    {
        var parent = this.GetParentEntity<WorkflowLaneEntity>();

        var script = this.Script.Trim();
        script = script.Contains(';') ? script : ("return " + script + ";");
        var WorkflowEntityTypeName = parent.Pool.Workflow.MainEntityType.ToType().FullName;

        return Compile(DynamicCode.GetCoreMetadataReferences()
            .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
                @"
                namespace Signum.Entities.Workflow
                {
                    class MyWorkflowLaneActorEvaluator : IWorkflowLaneActorsEvaluator
                    {
                        public IEnumerable<Lite<Entity>> GetActors(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx)
                        {
                            return this.Evaluate((" + WorkflowEntityTypeName + @")mainEntity, ctx);
                        }

                        IEnumerable<Lite<Entity>> Evaluate(" + WorkflowEntityTypeName + @" e, WorkflowTransitionContext ctx)
                        {
                            " + script + @"
                        }
                    }
                }");
    }

    public WorkflowLaneActorsEval Clone()
    {
        return new WorkflowLaneActorsEval() { Script = this.Script };
    }
}

public interface IWorkflowLaneActorsEvaluator
{
    IEnumerable<Lite<Entity>> GetActors(ICaseMainEntity mainEntity, WorkflowTransitionContext ctx);
}

[AutoInit]
public static class WorkflowLaneOperation
{
    public static readonly ExecuteSymbol<WorkflowLaneEntity> Save;
    public static readonly DeleteSymbol<WorkflowLaneEntity> Delete;
}

public class WorkflowLaneModel : ModelEntity
{
    public TypeEntity MainEntityType { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    [ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
    [NoRepeatValidator]
    public MList<Lite<Entity>> Actors { get; set; } = new MList<Lite<Entity>>();

    public WorkflowLaneActorsEval? ActorsEval { get; set; }

    public bool UseActorEvalForStart { get; set; }

    public bool CombineActorAndActorEvalWhenContinuing { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(UseActorEvalForStart) && UseActorEvalForStart == true && ActorsEval == null)
            return ValidationMessage._0ShouldBe12.NiceToString(pi.NiceName(), ComparisonType.EqualTo.NiceToString(), false);

        if (pi.Name == nameof(CombineActorAndActorEvalWhenContinuing) && CombineActorAndActorEvalWhenContinuing == true && (ActorsEval == null || Actors.IsEmpty()))
            return ValidationMessage._0ShouldBe12.NiceToString(pi.NiceName(), ComparisonType.EqualTo.NiceToString(), false);

        return base.PropertyValidation(pi);
    }
}
