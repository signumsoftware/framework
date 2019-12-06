using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    [Mixin(typeof(DisabledMixin))]
    public class DynamicApiEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotifyChildProperty, InTypeScript(Undefined = false, Null = false)]
        public DynamicApiEval Eval { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);
    }

    [AutoInit]
    public static class DynamicApiOperation
    {
        public static readonly ConstructSymbol<DynamicApiEntity>.From<DynamicApiEntity> Clone;
        public static readonly ExecuteSymbol<DynamicApiEntity> Save;
        public static readonly DeleteSymbol<DynamicApiEntity> Delete;
    }

    public class DynamicApiEval : EvalEmbedded<IDynamicApiEvaluator>
    {
        protected override CompilationResult Compile()
        {
            var script = this.Script.Trim();

            return Compile(DynamicCode.GetCoreMetadataReferences()
                .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
@"
namespace Signum.Entities.Dynamic
{
    class Evaluator : ControllerBase, Signum.Entities.Dynamic.IDynamicApiEvaluator
    {
        " + script + @"

        public bool DummyEvaluate() => true;
    }
}");
        }
    }

    public interface IDynamicApiEvaluator
    {
        bool DummyEvaluate();
    }
}
