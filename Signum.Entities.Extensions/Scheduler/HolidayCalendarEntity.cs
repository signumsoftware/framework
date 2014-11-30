using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;
using System.Reflection;
using System.Linq.Expressions;

namespace Signum.Entities.Scheduler
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class HolidayCalendarEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }
        
        [NotNullable]
        MList<HolidayEntity> holidays = new MList<HolidayEntity>();
        [NotNullValidator]
        public MList<HolidayEntity> Holidays
        {
            get { return holidays; }
            set { Set(ref holidays, value); }
        }

        public bool IsHoliday(DateTime date)
        {
            return holidays.Any(h => h.Date == date);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(()=>Holidays) && holidays != null)
            {
                string rep = (from h in holidays
                              group 1 by h.Date into g
                              where g.Count() > 2
                              select new { Date = g.Key, Num = g.Count() }).ToString(g => "{0} ({1})".FormatWith(g.Date, g.Num), ", ");

                if (rep.HasText())
                    return "Some dates have been repeated: " + rep;
            }

            return base.PropertyValidation(pi);
        }

        static readonly Expression<Func<HolidayCalendarEntity, string>> ToStringExpression = e => e.name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class HolidayCalendarOperation
    {
        public static readonly ExecuteSymbol<HolidayCalendarEntity> Save = OperationSymbol.Execute<HolidayCalendarEntity>();
        public static readonly DeleteSymbol<HolidayCalendarEntity> Delete = OperationSymbol.Delete<HolidayCalendarEntity>();
    }

    [Serializable]
    public class HolidayEntity : EmbeddedEntity
    {
        DateTime date = DateTime.Today;
        [DaysPrecissionValidator]
        public DateTime Date
        {
            get { return date; }
            set { SetToStr(ref date, value); }
        }

        [SqlDbType(Size = 100)]
        string name;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        public override string ToString()
        {
            return "{0:d} {1}".FormatWith(date, name);
        }
    }
}
