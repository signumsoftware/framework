using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
    public class EmailModelEntity : Entity
    {
        [UniqueIndex]
        public string FullClassName { get; set; }

        static Expression<Func<EmailModelEntity, string>> ToStringExpression = e => e.FullClassName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
