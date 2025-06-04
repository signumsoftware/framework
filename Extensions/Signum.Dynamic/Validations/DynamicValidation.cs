using Signum.Eval;

namespace Signum.Dynamic.Validations;

[EntityKind(EntityKind.Shared, EntityData.Master)]
[Mixin(typeof(DisabledMixin))]
public class DynamicValidationEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public TypeEntity EntityType { get; set; }

    public PropertyRouteEntity? SubEntity { get; set; }

    public static Func<DynamicValidationEntity, Type> GetMainType;

    [BindParent]
    public DynamicValidationEval Eval { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => EntityType + (SubEntity == null ? null : " " + SubEntity) + ": " + Name);
}

[AutoInit]
public static class DynamicValidationOperation
{
    public static readonly ConstructSymbol<DynamicValidationEntity>.From<DynamicValidationEntity> Clone;
    public static readonly ExecuteSymbol<DynamicValidationEntity> Save;
    public static readonly DeleteSymbol<DynamicValidationEntity> Delete;
}

public class DynamicValidationEval : EvalEmbedded<IDynamicValidationEvaluator>
{
    protected override CompilationResult Compile()
    {
        var script = this.Script.Trim();
        script = script.Contains(';') ? script : "return " + script + ";";
        var entityTypeName = DynamicValidationEntity.GetMainType(this.GetParentEntity<DynamicValidationEntity>()).FullName;

        return Compile(EvalLogic.GetCoreMetadataReferences()
            .Concat(EvalLogic.GetMetadataReferences()), EvalLogic.GetUsingNamespaces() +
@"
namespace Signum.Dynamic
{
class Evaluator : Signum.Dynamic.IDynamicValidationEvaluator
{
    public string EvaluateUntyped(ModifiableEntity e, PropertyInfo pi)
    {
        return this.Evaluate((" + entityTypeName + @")e, pi);
    }

    string Evaluate(" + entityTypeName + @" e, PropertyInfo pi)
    {
        " + script + @"
    }
}                   
}");
    }

    private CompilationResult Compile(object value1, object value2)
    {
        throw new NotImplementedException();
    }
}

public interface IDynamicValidationEvaluator
{
    string EvaluateUntyped(ModifiableEntity c, PropertyInfo pi);
}

public enum DynamicValidationMessage
{
    PropertyIs
}
