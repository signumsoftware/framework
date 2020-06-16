using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.Word;
using Signum.Utilities;
using Signum.Entities.Mailing;
using System.Linq;

namespace Signum.Entities.Templating
{
    public class TemplateApplicableEval : EvalEmbedded<ITemplateApplicable>
    {
        protected override CompilationResult Compile()
        {
            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var parentEntity = this.TryGetParentEntity<Entity>()!;
            var query = 
                parentEntity is WordTemplateEntity wt ? wt.Query :
                parentEntity is EmailTemplateEntity et ? et.Query :
                throw new UnexpectedValueException(parentEntity);

            var entityTypeName = (QueryEntity.GetEntityImplementations(query).Types.Only() ?? typeof(Entity)).Name;

            return Compile(DynamicCode.GetCoreMetadataReferences()
                .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
@"
namespace Signum.Entities.Templating
{
    class Evaluator : Signum.Entities.Templating.ITemplateApplicable
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
}
