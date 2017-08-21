using Signum.Entities.Basics;
using Signum.Entities.Dynamic;
using Signum.Entities.Word;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Templating
{
    public class TemplateApplicableEval : EvalEmbedded<ITemplateApplicable>
    {
        protected override CompilationResult Compile()
        {
            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var entityTypeName = (QueryEntity.GetEntityImplementations(((WordTemplateEntity)this.GetParentEntity()).Query).Types.Only() ?? typeof(Entity)).Name;

            return Compile(DynamicCode.GetAssemblies(),
                DynamicCode.GetUsingNamespaces() +
@"
namespace Signum.Entities.Templating
{
    class Evaluator : Signum.Entities.Templating.ITemplateApplicable
    {
        public bool ApplicableUntyped(Entity e)
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
        bool ApplicableUntyped(Entity e);
    }
}
