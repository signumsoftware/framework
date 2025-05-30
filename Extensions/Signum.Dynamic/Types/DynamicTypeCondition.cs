using Signum.Eval;

namespace Signum.Dynamic.Types;

[EntityKind(EntityKind.Main, EntityData.Transactional)]
public class DynamicTypeConditionEntity : Entity
{

    public DynamicTypeConditionSymbolEntity SymbolName { get; set; }


    public TypeEntity EntityType { get; set; }

    [BindParent]
    public DynamicTypeConditionEval Eval { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => (EntityType == null ? "" : EntityType.CleanName + " : ") + SymbolName);
}

[AutoInit]
public static class DynamicTypeConditionOperation
{
    public static readonly ConstructSymbol<DynamicTypeConditionEntity>.From<DynamicTypeConditionEntity> Clone;
    public static readonly ExecuteSymbol<DynamicTypeConditionEntity> Save;
}

public class DynamicTypeConditionEval : EvalEmbedded<IDynamicTypeConditionEvaluator>
{
    protected override CompilationResult Compile()
    {
        var script = this.Script.Trim();
        script = script.Contains(';') ? script : "return " + script + ";";
        var entityTypeName = this.GetParentEntity<DynamicTypeConditionEntity>().EntityType.ToType().FullName;

        return Compile(EvalLogic.GetCoreMetadataReferences()
            .Concat(EvalLogic.GetMetadataReferences()), EvalLogic.GetUsingNamespaces() +
@"
namespace Signum.Dynamic
{
class Evaluator : Signum.Dynamic.IDynamicTypeConditionEvaluator
{
    public bool EvaluateUntyped(ModifiableEntity e)
    {
        return this.Evaluate((" + entityTypeName + @")e);
    }

    bool Evaluate(" + entityTypeName + @" e)
    {
        " + script + @"
    }
}                   
}");
    }
}

public interface IDynamicTypeConditionEvaluator
{
    bool EvaluateUntyped(ModifiableEntity c);
}
