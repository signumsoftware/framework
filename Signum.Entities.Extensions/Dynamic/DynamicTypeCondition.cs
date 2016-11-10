using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
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
        [NotNullable]
        [NotNullValidator]
        public DynamicTypeConditionSymbolEntity SymbolName { get; set; }

        [NotNullable]
        [NotNullValidator]
        public TypeEntity EntityType { get; set; }

        [NotNullable]
        [NotNullValidator, NotifyChildProperty, InTypeScript(Undefined = false, Null = false)]
        public DynamicTypeConditionEval Eval { get; set; }

        static Expression<Func<DynamicTypeConditionEntity, string>> ToStringExpression = @this => @this.EntityType.CleanName + ": " + @this.SymbolName;
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

    public class DynamicTypeConditionEval : EvalEntity<IDynamicTypeConditionEvaluator>
    {
        static Func<DynamicTypeConditionEval, IEnumerable<string>> GetAllowedAssemblies = de => Eval.BasicAssemblies;
        static Func<DynamicTypeConditionEval, IEnumerable<string>> GetAllowedNamespaces = de => Eval.BasicNamespaces;
        
        protected override CompilationResult Compile()
        {
            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var entityTypeName = ((DynamicTypeConditionEntity)this.GetParentEntity()).EntityType.ToType().FullName;

            return Compile(GetAllowedAssemblies(this).ToArray(),
                Eval.CreateUsings(GetAllowedNamespaces(this)) +
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
