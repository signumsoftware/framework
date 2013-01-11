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
    public interface IScheduleRuleDN : IIdentifiable
    {
        DateTime Next();
    }

    [Serializable]
    public abstract class ScheduleRuleDayDN : Entity, IScheduleRuleDN
    {
        DateTime startingOn = TimeZoneManager.Now.Date;
        public DateTime StartingOn
        {
            get { return startingOn; }
            set { Set(ref startingOn, value, () => StartingOn); }
        }

        public abstract DateTime Next();

        protected DateTime BaseNext()
        {
            DateTime result = DateTimeExtensions.Max(TimeZoneManager.Now.Date, startingOn.Date).Add(startingOn.TimeOfDay); 

            if (result < TimeZoneManager.Now)
                result = result.AddDays(1);

            return result;
        }

        public override string ToString()
        {
            return startingOn.ToUserInterface().ToShortTimeString();
        }
    }

    [Serializable, EntityKind(EntityKind.Part)]
    public class ScheduleRuleMinutelyDN : Entity, IScheduleRuleDN
    {
        DateTime startingOn = TimeZoneManager.Now.Date;
        public DateTime StartingOn
        {
            get { return startingOn; }
            set { Set(ref startingOn, value, () => StartingOn); }
        }

        public DateTime Next()
        {
            DateTime next = DateTimeExtensions.Max(TimeZoneManager.Now, startingOn);

            DateTime candidate = next.TrimToMinutes();

            candidate = candidate.AddMinutes(-(candidate.Minute % eachMinute));

            if (candidate < next)
                candidate = candidate.AddMinutes(eachMinute);

            return candidate; 
        }

        int eachMinute;
        [NumberBetweenValidator(1, 60)]
        public int EachMinute
        {
            get { return eachMinute; }
            set { Set(ref eachMinute, value, () => EachMinute); }
        }

        public override string ToString()
        {
            return Resources.Each0MinutesFrom1.Formato(eachMinute.ToString(), startingOn.ToUserInterface().ToShortDateString());
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => EachMinute))
            {
                if ((60 % EachMinute) != 0)
                    return Resources._0IsNotMultiple1.Formato(pi.NiceName(), 60);
            }

            return base.PropertyValidation(pi);
        }
    }

    [Serializable, EntityKind(EntityKind.Part)]
    public class ScheduleRuleHourlyDN : Entity, IScheduleRuleDN
    {
        DateTime startingOn = TimeZoneManager.Now.Date;
        public DateTime StartingOn
        {
            get { return startingOn; }
            set { Set(ref startingOn, value, () => StartingOn); }
        }

        public DateTime Next()
        {
            DateTime next = DateTimeExtensions.Max(TimeZoneManager.Now, startingOn);

            DateTime candidate = next.TrimToHours();

            candidate = candidate.AddHours(-(candidate.Hour % eachHour));

            if (candidate < next)
                candidate = candidate.AddHours(eachHour);

            return candidate;
        }

        public static bool isValid(int time)
        {
            return (24 % time) == 0;
        }

        int eachHour;
        [NumberBetweenValidator(1, 24)]
        public int EachHour
        {
            get { return eachHour; }
            set { Set(ref eachHour, value, () => EachHour); }
        }

        public override string ToString()
        {
            return Resources.Each0HoursFrom1.Formato(eachHour.ToString(), startingOn.ToUserInterface().ToShortDateString());
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => EachHour))
            {
                if ((24 % EachHour) != 0)
                    return Resources._0IsNotMultiple1.Formato(pi.NiceName(), 24);
            }

            return base.PropertyValidation(pi);
        }
    }

    [Serializable, EntityKind(EntityKind.Part)]
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

    [Serializable, EntityKind(EntityKind.Part)]
    public class ScheduleRuleWeeklyDN : ScheduleRuleDayDN
    {
        DayOfWeek dayOfTheWeek;
        public DayOfWeek DayOfTheWeek
        {
            get { return dayOfTheWeek; }
            set { SetToStr(ref dayOfTheWeek, value, () => DayOfTheWeek); }
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

    [Serializable, EntityKind(EntityKind.Part)]
    public class ScheduleRuleWeekDaysDN : ScheduleRuleDayDN
    {
        bool monday;
        public bool Monday
        {
            get { return monday; }
            set { SetToStr(ref monday, value, () => Monday); }
        }

        bool tuesday;
        public bool Tuesday
        {
            get { return tuesday; }
            set { SetToStr(ref tuesday, value, () => Tuesday); }
        }

        bool wednesday;
        public bool Wednesday
        {
            get { return wednesday; }
            set { SetToStr(ref wednesday, value, () => Wednesday); }
        }

        bool thursday;
        public bool Thursday
        {

            get { return thursday; }
            set { SetToStr(ref thursday, value, () => Thursday); }
        }

        bool friday;
        public bool Friday
        {
            get { return friday; }
            set { SetToStr(ref friday, value, () => Friday); }
        }

        bool saturday;
        public bool Saturday
        {
            get { return saturday; }
            set { SetToStr(ref saturday, value, () => Saturday); }
        }

        bool sunday;
        public bool Sunday
        {
            get { return sunday; }
            set { SetToStr(ref sunday, value, () => Sunday); }
        }

        CalendarDN calendar;
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

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(()=>Holiday))
            {
                if (calendar != null && holiday == null)
                    return Resources.Holidayhavetobetrueorfalsewhenacalendarisset;

                if (calendar == null && holiday != null)
                    return Resources.Holidayhavetobenullwhennocalendarisset;
            }

            return base.PropertyValidation(pi);
        }

    }
}
