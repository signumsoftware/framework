using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Signum.Utilities;

namespace Signum.Entities.Scheduler
{
    [ImplementedBy(typeof(ScheduleRuleDailyDN), typeof(ScheduleRuleWeeklyDN), typeof(ScheduleRuleWeekDaysDN))]
    public interface IScheduleRule: IIdentifiable
    {
        DateTime Next(DateTime dt);
    }

    [Serializable]
    public abstract class ScheduleRuleDayDN : Entity, IScheduleRule
    {
        int hour;
        [NumberBetweenValidator(0, 23)]
        public int Hour
        {
            get { return hour; }
            set { Set(ref hour, value, "Hour"); }
        }

        int minute;
        [NumberBetweenValidator(0, 59)]
        public int Minute
        {
            get { return minute; }
            set { Set(ref minute, value, "Minute"); }
        }

        public abstract DateTime Next(DateTime dt);

        protected DateTime HourOnDate(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, hour, minute, 0); 
        }
    }

    [Serializable]
    public class ScheduleRuleDailyDN : ScheduleRuleDayDN
    {
        public override string ToString()
        {
            return "Every day at {1}:{2}".Formato(Hour, Minute);
        }     

        public override DateTime Next(DateTime dt)
        {
            DateTime result = HourOnDate(dt);

            if (result < dt)
                result = result.AddDays(1);

            return result; 
        }
    }

    [Serializable]
    public class ScheduleRuleWeeklyDN : ScheduleRuleDayDN
    {
        DayOfWeek dayOfTheWeek;
        public DayOfWeek DayOfTheWeek
        {
            get { return dayOfTheWeek; }
            set { Set(ref dayOfTheWeek, value, "DayOfTheWeek"); }
        }

        public override string ToString()
        {
            return "{0} at {1}:{2}".Formato(CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)dayOfTheWeek], Hour, Minute);
        }


        public override DateTime Next(DateTime dt)
        {
            DateTime result = HourOnDate(dt);

            if(result < dt)
                result = result.AddDays(1);

            while (result.DayOfWeek != dayOfTheWeek)
                result = result.AddDays(1);

            return result;
        }
    }

    [Serializable]
    public class ScheduleRuleWeekDaysDN : ScheduleRuleDayDN
    {
        bool sunday;
        public bool Sunday
        {
            get { return sunday; }
            set { SetToStr(ref sunday, value, "Sunday"); }
        }

        bool monday;
        public bool Monday
        {
            get { return monday; }
            set { SetToStr(ref monday, value, "Monday"); }
        }

        bool tuesday;
        public bool Tuesday
        {
            get { return tuesday; }
            set { SetToStr(ref tuesday, value, "Tuesday"); }
        }

        bool wednesday;
        public bool Wednesday
        {
            get { return wednesday; }
            set { SetToStr(ref wednesday, value, "Wednesday"); }
        }

        bool thursday;
        public bool Thursday
        {

            get { return thursday; }
            set { SetToStr(ref thursday, value, "Thursday"); }
        }

        bool friday;
        public bool Friday
        {
            get { return friday; }
            set { SetToStr(ref friday, value, "Friday"); }
        }

        bool saturday;
        public bool Saturday
        {
            get { return saturday; }
            set { SetToStr(ref saturday, value, "Saturday"); }
        }

        bool? holiday;
        public bool? Holiday
        {
            get { return holiday; }
            set
            {
                if (value == null || calendar != null)
                    Set(ref holiday, value, "Holiday");
            }
        }

        CalendarDN calendar;
        public CalendarDN Calendar
        {
            get { return calendar; }
            set { if (Set(ref calendar, value, "Calendar")) holiday = calendar == null ? (bool?)null : false; }
        }

        public override DateTime Next(DateTime dt)
        {
            DateTime result = HourOnDate(dt);

            if (result < dt)
                result = result.AddDays(1);

            while (!IsAllowed(result.Date))
                result = result.AddDays(1);

            return result;
        }

        bool IsAllowed(DateTime dateTime)
        {
            if (calendar != null && calendar.IsHoliday(dateTime))
                return holiday.Value;

            switch (dateTime.DayOfWeek)
            {
                case DayOfWeek.Sunday: return sunday;
                case DayOfWeek.Monday: return monday;
                case DayOfWeek.Tuesday: return tuesday;
                case DayOfWeek.Wednesday: return wednesday;
                case DayOfWeek.Thursday: return thursday;
                case DayOfWeek.Friday: return friday;
                case DayOfWeek.Saturday: return saturday;
                default: throw new InvalidOperationException(); 
            }            
        }

        public override string ToString()
        {
            return
                (sunday ? "S" : "") +
                (monday ? "M" : "") +
                (tuesday ? "T" : "") +
                (wednesday ? "W" : "") +
                (thursday ? "Th" : "") +
                (friday ? "F" : "") +
                (sunday ? "S" : "") +
                (holiday.HasValue ? (holiday.Value ? "and Holidays" : "but Holidays") : null)
                + " at {0}:{1}".Formato(Hour, Minute);
        }

        public override string this[string columnName]
        {
            get
            {
                string result = base[columnName];

                if (columnName == "Holiday")
                {
                    if (calendar != null && holiday == null)
                        result = result.AddLine("Holiday have to be true or false when a calendar is set");

                    if (calendar == null && holiday != null)
                        result = result.AddLine("Holiday have to be null when no calendar is set"); 
                }

                return result;
            }
        }
    }
}
