using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
    public class SystemEmailEntity : Entity
    {
        [NotNullable, UniqueIndex]
        public string FullClassName { get; set; }

        static readonly Expression<Func<SystemEmailEntity, string>> ToStringExpression = e => e.FullClassName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
