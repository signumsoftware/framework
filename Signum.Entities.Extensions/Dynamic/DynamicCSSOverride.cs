using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    [Mixin(typeof(DisabledMixin))]
    public class DynamicCSSOverrideEntity : Entity
    {
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100), UniqueIndex]
        public string Name { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, MultiLine = true)]
        public string Script { get; set; }

        static Expression<Func<DynamicCSSOverrideEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class DynamicCSSOverrideOperation
    {
        public static readonly ExecuteSymbol<DynamicCSSOverrideEntity> Save;
        public static readonly DeleteSymbol<DynamicCSSOverrideEntity> Delete;
    }
}
