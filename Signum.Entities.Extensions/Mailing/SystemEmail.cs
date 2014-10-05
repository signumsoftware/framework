using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksField(false)]
    public class SystemEmailDN : Entity
    {
        [NotNullable, UniqueIndex]
        string fullClassName;
        public string FullClassName
        {
            get { return fullClassName; }
            set { Set(ref fullClassName, value); }
        }

        static readonly Expression<Func<SystemEmailDN, string>> ToStringExpression = e => e.fullClassName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
