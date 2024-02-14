using Signum.Eval;

namespace Signum.Templating;

public interface IContainsQuery : IEntity
{
    public QueryEntity? Query { get; }
}

public class TemplateApplicableEval : EvalEmbedded<ITemplateApplicable>
{
    protected override CompilationResult Compile()
    {
        var script = this.Script.Trim();
        script = script.Contains(';') ? script : ("return " + script + ";");
        var parentEntity = this.TryGetParentEntity<Entity>()!;
        var query = parentEntity is IContainsQuery wt ? wt.Query :
            throw new UnexpectedValueException(parentEntity);

        var entityTypeName = ((query == null ? null : QueryEntity.GetEntityImplementations(query).Types.Only()) ?? typeof(Entity)).Name;

        return Compile(EvalLogic.GetCoreMetadataReferences()
            .Concat(EvalLogic.GetMetadataReferences()), EvalLogic.GetUsingNamespaces() +
@"
namespace Signum.Templating
{
class Evaluator : Signum.Templating.ITemplateApplicable
{
    public bool ApplicableUntyped(Entity? e)
    {
        return this.Applicable((" + entityTypeName + @")e);
    }

    bool Applicable(" + entityTypeName + @" e)
    {
        " + script + @"
    }
}
}");
    }
}

public interface ITemplateApplicable
{
    bool ApplicableUntyped(Entity? e);
}
