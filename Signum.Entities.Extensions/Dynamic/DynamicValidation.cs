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
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class DynamicValidationEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullable]
        [NotNullValidator]
        public TypeEntity EntityType { get; set; }

        [NotNullable]
        [NotNullValidator]
        public PropertyRouteEntity PropertyRoute { get; set; }

        public bool IsGlobalyEnabled { get; set; }

        [NotNullable]
        [NotNullValidator, NotifyChildProperty, InTypeScript(Undefined = false, Null = false)]
        public DynamicValidationEval Eval { get; set; }

        static Expression<Func<DynamicValidationEntity, string>> ToStringExpression = @this => @this.PropertyRoute + ": " + @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class DynamicValidationOperation
    {
        public static readonly ExecuteSymbol<DynamicValidationEntity> Save;
    }

    public class DynamicValidationEval : EvalEntity<IEvaluator>
    {
        static Func<DynamicValidationEval, IEnumerable<string>> GetAllowedAssemblies = de => Eval.BasicAssemblies;
        static Func<DynamicValidationEval, IEnumerable<string>> GetAllowedNamespaces = de => Eval.BasicNamespaces;
        
        protected override CompilationResult Compile()
        {
            var script = this.Script.Trim();
            script = script.Contains(';') ? script : ("return " + script + ";");
            var entityTypeName = ((DynamicValidationEntity)this.GetParentEntity()).EntityType.ToType().FullName;

            return Compile(GetAllowedAssemblies(this).ToArray(),
                Eval.CreateUsings(GetAllowedNamespaces(this)) +
@"
namespace Signum.Entities.DynamicEntities
{
    class Evaluator : IEvaluator
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
    }

    public interface IEvaluator
    {
        string EvaluateUntyped(ModifiableEntity c, PropertyInfo pi);
    }
}
