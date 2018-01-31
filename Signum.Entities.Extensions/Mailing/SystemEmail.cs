using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
    public class SystemEmailEntity : Entity
    {
        [NotNullValidator, UniqueIndex]
        public string FullClassName { get; set; }

        static Expression<Func<SystemEmailEntity, string>> ToStringExpression = e => e.FullClassName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
