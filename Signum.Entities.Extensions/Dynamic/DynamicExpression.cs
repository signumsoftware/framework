using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class DynamicExpressionEntity : Entity
    {
        [StringLengthValidator(Min = 3, Max = 100), IdentifierValidator(IdentifierType.PascalAscii)]
        public string Name { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string FromType { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string ReturnType { get; set; }

        [StringLengthValidator(Min = 1, MultiLine = true)]
        public string Body { get; set; }

        [StringLengthValidator(Min = 1, Max = 100)]
        public string? Format { get; set; }

        [StringLengthValidator(Min = 1, Max = 100)]
        public string? Unit { get; set; }

        public DynamicExpressionTranslation Translation { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => ReturnType + " " + Name + "(" + FromType + " e)");
    }

    public enum DynamicExpressionTranslation
    {
        TranslateExpressionName,
        ReuseTranslationOfReturnType,
        NoTranslation,
    }

    [AutoInit]
    public static class DynamicExpressionOperation
    {
        public static readonly ConstructSymbol<DynamicExpressionEntity>.From<DynamicExpressionEntity> Clone;
        public static readonly ExecuteSymbol<DynamicExpressionEntity> Save;
        public static readonly DeleteSymbol<DynamicExpressionEntity> Delete;
    }

    public interface IDynamicExpressionEvaluator
    {
        object EvaluateUntyped(Entity entity);
    }
}
