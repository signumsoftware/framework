using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Signum.Utilities;
using System.ComponentModel;
using Signum.Entities.Extensions.Properties;
using System.Reflection;

namespace Signum.Entities.Scheduler
{
    [ImplementedBy(typeof(ScheduleRuleDailyDN), typeof(ScheduleRuleWeeklyDN), typeof(ScheduleRuleWeekDaysDN))]
    public interface IScheduleRuleDN : IIdentifiable
    {
        DateTime Next();
    }

    [Serializable]
    public abstract class ScheduleRuleDayDN : Entity, IScheduleRuleDN
    {
        int hour;
        [NumberBetweenValidator(0, 23), Format("00"), LocDescription]
        public int Hour
        {
            get { return hour; }
            set { SetToStr(ref hour, value, () => Hour); }
        }

        int minute;
        [NumberBetweenValidator(0, 59), Format("00"), LocDescription]
        public int Minute
        {
            get { return minute; }
            set { SetToStr(ref minute, value, () => Minute); }
        }

        DateTime startingOn = DateTime.Today;
        [DateOnlyValidator, LocDescription]
        public DateTime StartingOn
        {
            get { return startingOn; }
            set { Set(ref startingOn, value, () => StartingOn); }
        }

        public abstract DateTime Next();

        protected DateTime BaseNext()
        {
            DateTime result = DateTimeExtensions.Max(DateTime.Today, startingOn).AddHours(hour).AddMinutes(minute);

            if (result < DateTime.Now)
                result = result.AddDays(1);

            return result;
        }

        public override string ToString()
        {
            return "{0:00}:{1:00}".Formato(Hour, Minute);
        }
    }

    [Serializable, LocDescription]
    public class ScheduleRuleDailyDN : ScheduleRuleDayDN
    {
        public override string ToString()
        {
            return Resources.ScheduleRuleDailyDN_Everydayat + base.ToString();
        }

        public override DateTime Next()
        {
            return BaseNext();
        }
    }

    [Serializable, LocDescription]
    public class ScheduleRuleWeeklyDN : ScheduleRuleDayDN
    {
        DayOfWeek dayOfTheWeek;
        [LocDescription]
        public DayOfWeek DayOfTheWeek
        {
            get { return dayOfTheWeek; }
            set { Set(ref dayOfTheWeek, value, () => DayOfTheWeek); }
        }

        public override string ToString()
        {
            return "{0} {1} {2}".Formato(
                CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)dayOfTheWeek],
                Resources.ScheduleRuleWeekDaysDN_At,
                base.ToString());
        }


        public override DateTime Next()
        {
            DateTime result = BaseNext();

            while (result.DayOfWeek != dayOfTheWeek)
                result = result.AddDays(1);

            return result;
        }

    }


    [Serializable, LocDescription]
    public class ScheduleRuleWeekDaysDN : ScheduleRuleDayDN
    {
        bool monday;
        [LocDescription]
        public bool Monday
        {
            get { return monday; }
            set { SetToStr(ref monday, value, () => Monday); }
        }

        bool tuesday;
        [LocDescription]
        public bool Tuesday
        {
            get { return tuesday; }
            set { SetToStr(ref tuesday, value, () => Tuesday); }
        }

        bool wednesday;
        [LocDescription]
        public bool Wednesday
        {
            get { return wednesday; }
            set { SetToStr(ref wednesday, value, () => Wednesday); }
        }

        bool thursday;
        [LocDescription]
        public bool Thursday
        {

            get { return thursday; }
            set { SetToStr(ref thursday, value, () => Thursday); }
        }

        bool friday;
        [LocDescription]
        public bool Friday
        {
            get { return friday; }
            set { SetToStr(ref friday, value, () => Friday); }
        }

        bool saturday;
        [LocDescription]
        public bool Saturday
        {
            get { return saturday; }
            set { SetToStr(ref saturday, value, () => Saturday); }
        }

        bool sunday;
        [LocDescription]
        public bool Sunday
        {
            get { return sunday; }
            set { SetToStr(ref sunday, value, () => Sunday); }
        }

        CalendarDN calendar;
        [LocDescription]
        public CalendarDN Calendar
        {
            get { return calendar; }
            set
            {
                if (Set(ref calendar, value, () => Calendar))
                    Holiday = calendar == null ? (bool?)null : false;
            }
        }

        bool? holiday;
        [LocDescription]
        public bool? Holiday
        {
            get { return holiday; }
            set { SetToStr(ref holiday, value, () => Holiday); }
        }

        public override DateTime Next()
        {
            DateTime result = BaseNext();

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
            return "{0} {1} {2} {3}".Formato(
                (monday ? Resources.ScheduleRuleWeekDaysDN_M : "") +
                (tuesday ? Resources.ScheduleRuleWeekDaysDN_T : "") +
                (wednesday ? Resources.ScheduleRuleWeekDaysDN_W : "") +
                (thursday ? Resources.ScheduleRuleWeekDaysDN_Th : "") +
                (friday ? Resources.ScheduleRuleWeekDaysDN_F : "") +
                (saturday ? Resources.ScheduleRuleWeekDaysDN_Sa : "") +
                (sunday ? Resources.ScheduleRuleWeekDaysDN_S : ""),
                (holiday.HasValue ? (holiday.Value ? Resources.ScheduleRuleWeekDaysDN_AndHoliday : Resources.ScheduleRuleWeekDaysDN_ButHoliday) : null),
                Resources.ScheduleRuleWeekDaysDN_At,
                base.ToString());
        }

        protected override string PropertyCheck(PropertyInfo pi)
        {
            if (pi.Is(()=>Holiday))
            {
                if (calendar != null && holiday == null)
                    return Resources.Holidayhavetobetrueorfalsewhenacalendarisset;

                if (calendar == null && holiday != null)
                    return Resources.Holidayhavetobenullwhennocalendarisset;
            }

            return base.PropertyCheck(pi);
        }

    }
}
