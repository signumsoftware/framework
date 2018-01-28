using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class DynamicTypeConditionEntity : Entity
    {
        [NotNullValidator]
        public DynamicTypeConditionSymbolEntity SymbolName { get; set; }

        [NotNullValidator]
        public TypeEntity EntityType { get; set; }

        [NotNullValidator, NotifyChildProperty, InTypeScript(Undefined = false, Null = false)]
        public DynamicTypeConditionEval Eval { get; set; }

        static Expression<Func<DynamicTypeConditionEntity, string>> ToStringExpression = @this => (@this.EntityType == null ? "" : @this.EntityType.CleanName + " : ") + @this.SymbolName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
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
            script = script.Contains(';') ? script : ("return " + script + ";");
            var entityTypeName = ((DynamicTypeConditionEntity)this.GetParentEntity()).EntityType.ToType().FullName;

            return Compile(DynamicCode.GetAssemblies(),
                DynamicCode.GetUsingNamespaces() +
@"
namespace Signum.Entities.Dynamic
{
    class Evaluator : Signum.Entities.Dynamic.IDynamicTypeConditionEvaluator
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
}
