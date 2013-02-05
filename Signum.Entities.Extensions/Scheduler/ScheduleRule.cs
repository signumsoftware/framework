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
            return SchedulerMessage.Each0MinutesFrom1.NiceToString().Formato(eachMinute.ToString(), startingOn.ToUserInterface().ToShortDateString());
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => EachMinute))
            {
                if ((60 % EachMinute) != 0)
                    return SchedulerMessage._0IsNotMultiple1.NiceToString().Formato(pi.NiceName(), 60);
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
            return SchedulerMessage.Each0HoursFrom1.NiceToString().Formato(eachHour.ToString(), startingOn.ToUserInterface().ToShortDateString());
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => EachHour))
            {
                if ((24 % EachHour) != 0)
                    return SchedulerMessage._0IsNotMultiple1.NiceToString().Formato(pi.NiceName(), 24);
            }

            return base.PropertyValidation(pi);
        }
    }

    [Serializable, EntityKind(EntityKind.Part)]
    public class ScheduleRuleDailyDN : ScheduleRuleDayDN
    {
        public override string ToString()
        {
            return SchedulerMessage.ScheduleRuleDailyDN_Everydayat.NiceToString() + base.ToString();
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
                SchedulerMessage.ScheduleRuleWeekDaysDN_At.NiceToString(),
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
                (monday ? SchedulerMessage.ScheduleRuleWeekDaysDN_M.NiceToString() : "") +
                (tuesday ? SchedulerMessage.ScheduleRuleWeekDaysDN_T.NiceToString() : "") +
                (wednesday ? SchedulerMessage.ScheduleRuleWeekDaysDN_W.NiceToString() : "") +
                (thursday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Th.NiceToString() : "") +
                (friday ? SchedulerMessage.ScheduleRuleWeekDaysDN_F.NiceToString() : "") +
                (saturday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Sa.NiceToString() : "") +
                (sunday ? SchedulerMessage.ScheduleRuleWeekDaysDN_S.NiceToString() : ""),
                (holiday.HasValue ? (holiday.Value ? SchedulerMessage.ScheduleRuleWeekDaysDN_AndHoliday.NiceToString() : SchedulerMessage.ScheduleRuleWeekDaysDN_ButHoliday.NiceToString()) : null),
                SchedulerMessage.ScheduleRuleWeekDaysDN_At.NiceToString(),
                base.ToString());
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(()=>Holiday))
            {
                if (calendar != null && holiday == null)
                    return SchedulerMessage.Holidayhavetobetrueorfalsewhenacalendarisset.NiceToString();

                if (calendar == null && holiday != null)
                    return SchedulerMessage.Holidayhavetobenullwhennocalendarisset.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

    }

    public enum SchedulerMessage
    {
        [Description("{0} is not multiple of {1}")]
        _0IsNotMultiple1,
        [Description("Each {0} hours from {1}")]
        Each0HoursFrom1,
        [Description("Each {0} minutes from {1}")]
        Each0MinutesFrom1,
        [Description("Holiday have to be null when no calendar is set")]
        Holidayhavetobenullwhennocalendarisset,
        [Description("Holiday have to be true or false when a calendar is set")]
        Holidayhavetobetrueorfalsewhenacalendarisset,
        [Description("Dialy")]
        ScheduleRuleDailyDN,
        [Description("Every day at ")]
        ScheduleRuleDailyDN_Everydayat,
        [Description("Starting On")]
        ScheduleRuleDayDN_StartingOn,
        [Description("Each X Hours")]
        ScheduleRuleHourlyDN,
        [Description("Each X Minutes")]
        ScheduleRuleMinutelyDN,
        [Description("Days of week")]
        ScheduleRuleWeekDaysDN,
        [Description("and holidays")]
        ScheduleRuleWeekDaysDN_AndHoliday,
        [Description("at")]
        ScheduleRuleWeekDaysDN_At,
        [Description("but holidays")]
        ScheduleRuleWeekDaysDN_ButHoliday,
        [Description("Calendar")]
        ScheduleRuleWeekDaysDN_Calendar,
        [Description("F")]
        ScheduleRuleWeekDaysDN_F,
        [Description("Friday")]
        ScheduleRuleWeekDaysDN_Friday,
        [Description("Holiday")]
        ScheduleRuleWeekDaysDN_Holiday,
        [Description("M")]
        ScheduleRuleWeekDaysDN_M,
        [Description("Monday")]
        ScheduleRuleWeekDaysDN_Monday,
        [Description("S")]
        ScheduleRuleWeekDaysDN_S,
        [Description("Sa")]
        ScheduleRuleWeekDaysDN_Sa,
        [Description("Saturday")]
        ScheduleRuleWeekDaysDN_Saturday,
        [Description("Sunday")]
        ScheduleRuleWeekDaysDN_Sunday,
        [Description("T")]
        ScheduleRuleWeekDaysDN_T,
        [Description("Th")]
        ScheduleRuleWeekDaysDN_Th,
        [Description("Thursday")]
        ScheduleRuleWeekDaysDN_Thursday,
        [Description("Tuesday")]
        ScheduleRuleWeekDaysDN_Tuesday,
        [Description("W")]
        ScheduleRuleWeekDaysDN_W,
        [Description("Wednesday")]
        ScheduleRuleWeekDaysDN_Wednesday,
        [Description("Weekly")]
        ScheduleRuleWeeklyDN,
        [Description("Day of the week")]
        ScheduleRuleWeeklyDN_DayOfTheWeek
    }

}
