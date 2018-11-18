using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Word
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
    public class SystemWordTemplateEntity : Entity
    {
        [StringLengthValidator(AllowNulls = false, Max = 200), UniqueIndex]
        public string FullClassName { get; set; }

        static readonly Expression<Func<SystemWordTemplateEntity, string>> ToStringExpression = e => e.FullClassName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

}
