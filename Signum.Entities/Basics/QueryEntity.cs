using System;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false), InTypeScript(Undefined = false)]
    public class QueryEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Key { get; set; }

        public static Func<QueryEntity, Implementations> GetEntityImplementations;

        static Expression<Func<QueryEntity, string>> ToStringExpression = e => e.Key;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
