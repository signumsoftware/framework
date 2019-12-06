using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Word
{
    [Serializable, EntityKind(EntityKind.SystemString, EntityData.Master), TicksColumn(false)]
    public class WordModelEntity : Entity
    {
        [StringLengthValidator(Max = 200), UniqueIndex]
        public string FullClassName { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => FullClassName);
    }

}
