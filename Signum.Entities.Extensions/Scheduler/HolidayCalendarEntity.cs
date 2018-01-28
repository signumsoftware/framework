using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class HolidayCalendarEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullValidator]
        public MList<HolidayEmbedded> Holidays { get; set; } = new MList<HolidayEmbedded>();

        public bool IsHoliday(DateTime date)
        {
            return Holidays.Any(h => h.Date == date);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Holidays) && Holidays != null)
            {
                string rep = (from h in Holidays
                              group 1 by h.Date into g
                              where g.Count() > 2
                              select new { Date = g.Key, Num = g.Count() }).ToString(g => "{0} ({1})".FormatWith(g.Date, g.Num), ", ");

                if (rep.HasText())
                    return "Some dates have been repeated: " + rep;
            }

            return base.PropertyValidation(pi);
        }

        static readonly Expression<Func<HolidayCalendarEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class HolidayCalendarOperation
    {
        public static ExecuteSymbol<HolidayCalendarEntity> Save;
        public static DeleteSymbol<HolidayCalendarEntity> Delete;
    }

    [Serializable]
    public class HolidayEmbedded : EmbeddedEntity
    {
        [DaysPrecissionValidator]
        public DateTime Date { get; set; } = DateTime.Today;

        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Name { get; set; }

        public override string ToString()
        {
            return "{0:d} {1}".FormatWith(Date, Name);
        }
    }
}
