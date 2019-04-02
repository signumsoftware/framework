using System;
using System.Linq;
using System.Globalization;
using Signum.Utilities;
using System.ComponentModel;
using System.Reflection;

namespace Signum.Entities.Scheduler
{
    public interface IScheduleRuleEntity : IEntity
    {
        DateTime StartingOn { get; }

        DateTime Next(DateTime now);
        IScheduleRuleEntity Clone();
    }


    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ScheduleRuleMinutelyEntity : Entity, IScheduleRuleEntity
    {
        public DateTime StartingOn { get; set; } = TimeZoneManager.Now.Date;

        public DateTime Next(DateTime now)
        {
            DateTime candidate = DateTimeExtensions.Max(now, StartingOn).TrimToMinutes();

            if (this.IsAligned)
                candidate = candidate.AddMinutes(-(candidate.Minute % EachMinutes));

            if (candidate < now)
                candidate = candidate.AddMinutes(EachMinutes);

            return candidate;
        }

        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int EachMinutes { get; set; }

        public bool IsAligned => EachMinutes > 0 && EachMinutes < 60 && (60 % EachMinutes == 0);

        public override string ToString()
        {
            return SchedulerMessage.Each0Minutes.NiceToString().FormatWith(EachMinutes.ToString());
        }

        public IScheduleRuleEntity Clone()
        {
            return new ScheduleRuleMinutelyEntity
            {
                EachMinutes = EachMinutes,
            };
        }
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ScheduleRuleWeekDaysEntity :  Entity, IScheduleRuleEntity
    {
        public DateTime StartingOn { get; set; } = TimeZoneManager.Now.Date;

        public bool Monday { get; set; }

        public bool Tuesday { get; set; }

        public bool Wednesday { get; set; }

        public bool Thursday { get; set; }

        public bool Friday { get; set; }

        public bool Saturday { get; set; }

        public bool Sunday { get; set; }

        public HolidayCalendarEntity? Calendar { get; set; }

        public bool Holiday { get; set; }

        public DateTime Next(DateTime now)
        {
            DateTime result = DateTimeExtensions.Max(now, StartingOn).Date.Add(StartingOn.TimeOfDay);

            if (result < now)
                result = result.AddDays(1);

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

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Monday) && !(Monday || Tuesday || Wednesday || Thursday || Friday || Saturday || Sunday || Holiday))
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

            return base.PropertyValidation(pi);
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
                StartingOn.ToUserInterface().ToShortTimeString());
        }

        public IScheduleRuleEntity Clone()
        {
            return new ScheduleRuleWeekDaysEntity
            {
                Calendar = Calendar,
                Holiday = Holiday,

                Monday = Monday,
                Tuesday = Tuesday,
                Wednesday = Wednesday,
                Thursday = Thursday,
                Friday = Friday,
                Saturday = Saturday,
                Sunday = Sunday,
                StartingOn = StartingOn
            };
        }
    }


    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ScheduleRuleMonthsEntity : Entity, IScheduleRuleEntity
    {
        public DateTime StartingOn { get; set; } = TimeZoneManager.Now.Date;

        public bool January { get; set; }
        public bool February { get; set; }
        public bool March { get; set; }
        public bool April { get; set; }
        public bool May { get; set; }
        public bool June { get; set; }
        public bool July { get; set; }
        public bool August{ get; set; }
        public bool September { get; set; }
        public bool October { get; set; }
        public bool November { get; set; }
        public bool December { get; set; }

        public DateTime Next(DateTime now)
        {
            DateTime result = DateTimeExtensions.Max(now, StartingOn).MonthStart().AddDays(StartingOn.Day - 1).Add(StartingOn.TimeOfDay);

            if (result < now)
                result = result.AddMonths(1);

            while (!IsAllowed(result.Month))
                result = result.AddMonths(1);

            return result;
        }

        bool IsAllowed(int month)
        {
            switch (month)
            {
                case 1: return January;
                case 2: return February;
                case 3: return March;
                case 4: return April;
                case 5: return May;
                case 6: return June;
                case 7: return July;
                case 8: return August;
                case 9: return September;
                case 10: return October;
                case 11: return November;
                case 12: return December;
                default: throw new InvalidOperationException();
            }
        }

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(January) && !(0.To(12).Any(i => IsAllowed(i + 1))))
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

            return base.PropertyValidation(pi);
        }

        public override string ToString()
        {
            var monthNames = 0.To(12).Where(i => IsAllowed(i + 1)).CommaAnd(i => CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames[i]);

            return SchedulerMessage.Day0At1In2.NiceToString(StartingOn.Day, StartingOn.ToUserInterface().ToShortTimeString(), monthNames);
        }

        public IScheduleRuleEntity Clone()
        {
            return new ScheduleRuleMonthsEntity
            {
                January = January,
                February = February,
                March = March,
                April = April,
                May = May,
                June = June,
                July = July,
                August = August,
                September = September,
                October = October,
                November = November,
                December = December,
                StartingOn = StartingOn,
            };
        }
    }

    public enum SchedulerMessage
    {
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
        ScheduleRuleWeeklyDN_DayOfTheWeek,
        [Description("Day {0} at {1} in {2}")]
        Day0At1In2,
        TaskIsNotRunning
    }

}
