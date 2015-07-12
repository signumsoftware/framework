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

            candidate = candidate.AddMinutes(-(candidate.Minute % EachMinutes));

            if (candidate < now)
                candidate = candidate.AddMinutes(EachMinutes);

            return candidate;
        }
        
        [NumberBetweenValidator(1, 60)]
        public int EachMinutes { get; set; }

        public override string ToString()
        {
            return SchedulerMessage.Each0Minutes.NiceToString().FormatWith(EachMinutes.ToString());
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(EachMinutes))
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

            candidate = candidate.AddHours(-(candidate.Hour % EachHours));

            if (candidate < now)
                candidate = candidate.AddHours(EachHours);

            return candidate;
        }

        [NumberBetweenValidator(1, 24)]
        public int EachHours { get; set; }

        public override string ToString()
        {
            return SchedulerMessage.Each0Hours.NiceToString().FormatWith(EachHours.ToString());
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(EachHours))
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
        public DateTime StartingOn { get; set; } = TimeZoneManager.Now.Date;

        public abstract DateTime Next(DateTime now);

        protected DateTime BaseNext(DateTime now)
        {
            DateTime result = DateTimeExtensions.Max(now.Date, StartingOn.Date).Add(StartingOn.TimeOfDay);

            if (result < now)
                result = result.AddDays(1);

            return result;
        }

        public override string ToString()
        {
            return StartingOn.ToUserInterface().ToShortTimeString();
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
        public DayOfWeek DayOfTheWeek { get; set; }

        public override string ToString()
        {
            return "{0} {1} {2}".FormatWith(
                CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)DayOfTheWeek],
                SchedulerMessage.ScheduleRuleWeekDaysDN_At.NiceToString(),
                base.ToString());
        }


        public override DateTime Next(DateTime now)
        {
            DateTime result = BaseNext(now);

            while (result.DayOfWeek != DayOfTheWeek)
                result = result.AddDays(1);

            return result;
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ScheduleRuleWeekDaysEntity : ScheduleRuleDayEntity
    {
        public bool Monday { get; set; }

        public bool Tuesday { get; set; }

        public bool Wednesday { get; set; }

        public bool Thursday { get; set; }

        public bool Friday { get; set; }

        public bool Saturday { get; set; }

        public bool Sunday { get; set; }

        public HolidayCalendarEntity Calendar { get; set; }

        public bool Holiday { get; set; }

        public override DateTime Next(DateTime now)
        {
            DateTime result = BaseNext(now);

            while (!IsAllowed(result.Date))
                result = result.AddDays(1);

            return result;
        }

        bool IsAllowed(DateTime dateTime)
        {
            if (Calendar != null && Calendar.IsHoliday(dateTime))
                return Holiday;

            switch (dateTime.DayOfWeek)
            {
                case DayOfWeek.Sunday: return Sunday;
                case DayOfWeek.Monday: return Monday;
                case DayOfWeek.Tuesday: return Tuesday;
                case DayOfWeek.Wednesday: return Wednesday;
                case DayOfWeek.Thursday: return Thursday;
                case DayOfWeek.Friday: return Friday;
                case DayOfWeek.Saturday: return Saturday;
                default: throw new InvalidOperationException();
            }
        }

        public override string ToString()
        {
            return "{0} {1} {2} {3}".FormatWith(
                (Monday ? SchedulerMessage.ScheduleRuleWeekDaysDN_M.NiceToString() : "") +
                (Tuesday ? SchedulerMessage.ScheduleRuleWeekDaysDN_T.NiceToString() : "") +
                (Wednesday ? SchedulerMessage.ScheduleRuleWeekDaysDN_W.NiceToString() : "") +
                (Thursday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Th.NiceToString() : "") +
                (Friday ? SchedulerMessage.ScheduleRuleWeekDaysDN_F.NiceToString() : "") +
                (Saturday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Sa.NiceToString() : "") +
                (Sunday ? SchedulerMessage.ScheduleRuleWeekDaysDN_S.NiceToString() : ""),
                (Calendar != null ? (Holiday ? SchedulerMessage.ScheduleRuleWeekDaysDN_AndHoliday.NiceToString() : SchedulerMessage.ScheduleRuleWeekDaysDN_ButHoliday.NiceToString()) : null),
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
