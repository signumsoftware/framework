using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Signum.Utilities;
using System.ComponentModel;
using System.Reflection;

namespace Signum.Entities.Scheduler
{
    public interface IScheduleRuleEntity : IEntity
    {
        DateTime Next(DateTime now);
    }


    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ScheduleRuleMinutelyEntity : Entity, IScheduleRuleEntity
    {
        public DateTime Next(DateTime now)
        {
            DateTime candidate = now.TrimToMinutes();

            candidate = candidate.AddMinutes(-(candidate.Minute % eachMinutes));

            if (candidate < now)
                candidate = candidate.AddMinutes(eachMinutes);

            return candidate; 
        }

        int eachMinutes;
        [NumberBetweenValidator(1, 60)]
        public int EachMinutes
        {
            get { return eachMinutes; }
            set { Set(ref eachMinutes, value); }
        }

        public override string ToString()
        {
            return SchedulerMessage.Each0Minutes.NiceToString().FormatWith(eachMinutes.ToString());
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => EachMinutes))
            {
                if ((60 % EachMinutes) != 0)
                    return SchedulerMessage._0IsNotMultiple1.NiceToString().FormatWith(pi.NiceName(), 60);
            }

            return base.PropertyValidation(pi);
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ScheduleRuleHourlyEntity : Entity, IScheduleRuleEntity
    {
        public DateTime Next(DateTime now)
        {
            DateTime candidate = now.TrimToHours();

            candidate = candidate.AddHours(-(candidate.Hour % eachHours));

            if (candidate < now)
                candidate = candidate.AddHours(eachHours);

            return candidate;
        }

        int eachHours;
        [NumberBetweenValidator(1, 24)]
        public int EachHours
        {
            get { return eachHours; }
            set { Set(ref eachHours, value); }
        }

        public override string ToString()
        {
            return SchedulerMessage.Each0Hours.NiceToString().FormatWith(eachHours.ToString());
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => EachHours))
            {
                if ((24 % EachHours) != 0)
                    return SchedulerMessage._0IsNotMultiple1.NiceToString().FormatWith(pi.NiceName(), 24);
            }

            return base.PropertyValidation(pi);
        }
    }




    [Serializable]
    public abstract class ScheduleRuleDayEntity : Entity, IScheduleRuleEntity
    {
        DateTime startingOn = TimeZoneManager.Now.Date;
        public DateTime StartingOn
        {
            get { return startingOn; }
            set { Set(ref startingOn, value); }
        }

        public abstract DateTime Next(DateTime now);

        protected DateTime BaseNext(DateTime now)
        {
            DateTime result = DateTimeExtensions.Max(now.Date, startingOn.Date).Add(startingOn.TimeOfDay);

            if (result < now)
                result = result.AddDays(1);

            return result;
        }

        public override string ToString()
        {
            return startingOn.ToUserInterface().ToShortTimeString();
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ScheduleRuleDailyEntity : ScheduleRuleDayEntity
    {
        public override string ToString()
        {
            return SchedulerMessage.ScheduleRuleDailyDN_Everydayat.NiceToString() + base.ToString();
        }

        public override DateTime Next(DateTime now)
        {
            return BaseNext(now);
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ScheduleRuleWeeklyEntity : ScheduleRuleDayEntity
    {
        DayOfWeek dayOfTheWeek;
        public DayOfWeek DayOfTheWeek
        {
            get { return dayOfTheWeek; }
            set { SetToStr(ref dayOfTheWeek, value); }
        }

        public override string ToString()
        {
            return "{0} {1} {2}".FormatWith(
                CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)dayOfTheWeek],
                SchedulerMessage.ScheduleRuleWeekDaysDN_At.NiceToString(),
                base.ToString());
        }


        public override DateTime Next(DateTime now)
        {
            DateTime result = BaseNext(now);

            while (result.DayOfWeek != dayOfTheWeek)
                result = result.AddDays(1);

            return result;
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ScheduleRuleWeekDaysEntity : ScheduleRuleDayEntity
    {
        bool monday;
        public bool Monday
        {
            get { return monday; }
            set { SetToStr(ref monday, value); }
        }

        bool tuesday;
        public bool Tuesday
        {
            get { return tuesday; }
            set { SetToStr(ref tuesday, value); }
        }

        bool wednesday;
        public bool Wednesday
        {
            get { return wednesday; }
            set { SetToStr(ref wednesday, value); }
        }

        bool thursday;
        public bool Thursday
        {

            get { return thursday; }
            set { SetToStr(ref thursday, value); }
        }

        bool friday;
        public bool Friday
        {
            get { return friday; }
            set { SetToStr(ref friday, value); }
        }

        bool saturday;
        public bool Saturday
        {
            get { return saturday; }
            set { SetToStr(ref saturday, value); }
        }

        bool sunday;
        public bool Sunday
        {
            get { return sunday; }
            set { SetToStr(ref sunday, value); }
        }

        HolidayCalendarEntity calendar;
        public HolidayCalendarEntity Calendar
        {
            get { return calendar; }
            set { Set(ref calendar, value); }
        }

        bool holiday;
        public bool Holiday
        {
            get { return holiday; }
            set { SetToStr(ref holiday, value); }
        }

        public override DateTime Next(DateTime now)
        {
            DateTime result = BaseNext(now);

            while (!IsAllowed(result.Date))
                result = result.AddDays(1);

            return result;
        }

        bool IsAllowed(DateTime dateTime)
        {
            if (calendar != null && calendar.IsHoliday(dateTime))
                return holiday;

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
            return "{0} {1} {2} {3}".FormatWith(
                (monday ? SchedulerMessage.ScheduleRuleWeekDaysDN_M.NiceToString() : "") +
                (tuesday ? SchedulerMessage.ScheduleRuleWeekDaysDN_T.NiceToString() : "") +
                (wednesday ? SchedulerMessage.ScheduleRuleWeekDaysDN_W.NiceToString() : "") +
                (thursday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Th.NiceToString() : "") +
                (friday ? SchedulerMessage.ScheduleRuleWeekDaysDN_F.NiceToString() : "") +
                (saturday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Sa.NiceToString() : "") +
                (sunday ? SchedulerMessage.ScheduleRuleWeekDaysDN_S.NiceToString() : ""),
                (calendar != null ? (holiday ? SchedulerMessage.ScheduleRuleWeekDaysDN_AndHoliday.NiceToString() : SchedulerMessage.ScheduleRuleWeekDaysDN_ButHoliday.NiceToString()) : null),
                SchedulerMessage.ScheduleRuleWeekDaysDN_At.NiceToString(),
                base.ToString());
        }
    }

    public enum SchedulerMessage
    {
        [Description("{0} is not multiple of {1}")]
        _0IsNotMultiple1,
        [Description("Each {0} hours")]
        Each0Hours,
        [Description("Each {0} minutes")]
        Each0Minutes,
        [Description("Dialy")]
        ScheduleRuleDailyEntity,
        [Description("Every day at ")]
        ScheduleRuleDailyDN_Everydayat,
        [Description("Starting On")]
        ScheduleRuleDayDN_StartingOn,
        [Description("Each X Hours")]
        ScheduleRuleHourlyEntity,
        [Description("Each X Minutes")]
        ScheduleRuleMinutelyEntity,
        [Description("Days of week")]
        ScheduleRuleWeekDaysEntity,
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
        ScheduleRuleWeeklyEntity,
        [Description("Day of the week")]
        ScheduleRuleWeeklyDN_DayOfTheWeek
    }

}
