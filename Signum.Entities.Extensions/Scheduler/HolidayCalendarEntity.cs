using System;
using System.Linq;
using Signum.Utilities;
using System.Reflection;
using System.Linq.Expressions;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class HolidayCalendarEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        
        public MList<HolidayEmbedded> Holidays { get; set; } = new MList<HolidayEmbedded>();

        public bool IsHoliday(DateTime date)
        {
            return Holidays.Any(h => h.Date == date);
        }

        protected override string? PropertyValidation(PropertyInfo pi)
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

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);
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
        [DaysPrecisionValidator]
        public DateTime Date { get; set; } = DateTime.Today;

        [StringLengthValidator(Min = 3, Max = 100)]
        public string? Name { get; set; }

        public override string ToString()
        {
            return "{0:d} {1}".FormatWith(Date, Name);
        }
    }
}
