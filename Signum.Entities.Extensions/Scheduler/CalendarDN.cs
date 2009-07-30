using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Entities.Scheduler
{
    [Serializable, LocDescription]
    public class CalendarDN : Entity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100), LocDescription]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, "Name"); }
        }

        MList<HolidayDN> holidays;
        [NotNullValidator, LocDescription]
        public MList<HolidayDN> Holidays
        {
            get { return holidays; }
            set { Set(ref holidays, value, "Holidays"); }
        }

        public void CleanOldHolidays()
        {
            holidays.RemoveAll(h => h.Date < DateTime.Now); 
        }

        public bool IsHoliday(DateTime date)
        {
            return holidays.Any(h => h.Date == date); 
        }

        public override string this[string columnName]
        {
            get
            {
                string result = base[columnName];

                if (columnName == "Holydais" && holidays != null)
                {
                    string rep = (from h in holidays
                                  group 1 by h.Date into g
                                  where g.Count() > 2
                                  select new { Date = g.Key, Num = g.Count() }).ToString(g => "{0} ({1})".Formato(g.Date, g.Num), ", ");

                    if (rep.HasText())
                        result = result.AddLine("Some dates have been repeated: " + rep); 
                }

                return result;
            }
        }

        public override string ToString()
        {
            return name;
        }
    }

    [Serializable]
    public class HolidayDN : EmbeddedEntity
    {
        DateTime date;
        [DateOnlyValidator]
        public DateTime Date
        {
            get { return date; }
            set { SetToStr(ref date, value, "Date"); }
        }

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, "Name"); }
        }

        public override string ToString()
        {
            return "{0:d} {1}".Formato(date, name);
        }
    }
}
