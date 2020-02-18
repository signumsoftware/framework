using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Signum.Entities.Calendar
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class CalendarDayEntity : Entity
    {
        public Date Date { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Date.ToShortString());
    }
}
