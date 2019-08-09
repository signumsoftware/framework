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

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => FullClassName);
    }
}
