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
    public class DynamicExpressionEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100), IdentifierValidator(IdentifierType.PascalAscii)]
        public string Name { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string FromType { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ReturnType { get; set; }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        [StringLengthValidator(AllowNulls = false, Min = 1, MultiLine = true)]
        public string Body { get; set; }

        [SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = true, Min = 1, Max = 100)]
        public string Format { get; set; }

        [SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = true, Min = 1, Max = 100)]
        public string Unit { get; set; }

        public DynamicExpressionTranslation Translation { get; set; }

        static Expression<Func<DynamicExpressionEntity, string>> ToStringExpression = @this => @this.ReturnType + " " + @this.Name + "(" + @this.FromType + " e)";
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
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
