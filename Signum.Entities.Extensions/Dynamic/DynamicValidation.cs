using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    [Mixin(typeof(DisabledMixin))]
    public class DynamicValidationEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        public TypeEntity EntityType { get; set; }

        public PropertyRouteEntity? SubEntity { get; set; }

        public static Func<DynamicValidationEntity, Type> GetMainType; 

        [NotifyChildProperty, InTypeScript(Undefined = false, Null = false)]
        public DynamicValidationEval Eval { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => EntityType + (SubEntity == null ? null : (" " + SubEntity))+ ": " + Name);
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
            script = script.Contains(';') ? script : ("return " + script + ";");
            var entityTypeName = DynamicValidationEntity.GetMainType(this.GetParentEntity<DynamicValidationEntity>()).FullName;

            return Compile(DynamicCode.GetCoreMetadataReferences()
                .Concat(DynamicCode.GetMetadataReferences()), DynamicCode.GetUsingNamespaces() +
@"
namespace Signum.Entities.Dynamic
{
    class Evaluator : Signum.Entities.Dynamic.IDynamicValidationEvaluator
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

    public interface IDynamicValidationEvaluator
    {
        string EvaluateUntyped(ModifiableEntity c, PropertyInfo pi);
    }

    public enum DynamicValidationMessage
    {
        PropertyIs
    }
}
