using System.Globalization;
using System.ComponentModel;
using Signum.UserAssets;
using System.Xml.Linq;

namespace Signum.Scheduler;

public interface IScheduleRuleEntity : IEntity, IUserAssetEntity
{
    DateTime StartingOn { get; }

    DateTime Next(DateTime now);
    IScheduleRuleEntity Clone();
}


[EntityKind(EntityKind.Part, EntityData.Master)]
public class ScheduleRuleMinutelyEntity : Entity, IScheduleRuleEntity
{
    public Guid Guid { get; set; } = Guid.NewGuid();

    public DateTime StartingOn { get; set; } = Clock.Now.Date;

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

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ScheduleRuleMinutely",
                       new XAttribute("Guid", this.Guid),
                       new XAttribute("StartingOn", this.StartingOn.ToString("o", CultureInfo.InvariantCulture)),
                       new XAttribute("EachMinutes", this.EachMinutes));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        this.StartingOn = DateTime.ParseExact(element.Attribute("StartingOn")!.Value, "o", CultureInfo.InvariantCulture);
        this.EachMinutes = Convert.ToInt32(element.Attribute("EachMinutes")!.Value);
    }
}

[EntityKind(EntityKind.Part, EntityData.Master)]
public class ScheduleRuleWeekDaysEntity : Entity, IScheduleRuleEntity
{
    public Guid Guid { get; set; } = Guid.NewGuid();

    public DateTime StartingOn { get; set; } = Clock.Now.Date;

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
        if (Calendar != null && Calendar.IsHoliday(dateTime.ToDateOnly()))
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
            (Monday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Mo.NiceToString() : "") +
            (Tuesday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Tu.NiceToString() : "") +
            (Wednesday ? SchedulerMessage.ScheduleRuleWeekDaysDN_We.NiceToString() : "") +
            (Thursday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Th.NiceToString() : "") +
            (Friday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Fr.NiceToString() : "") +
            (Saturday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Sa.NiceToString() : "") +
            (Sunday ? SchedulerMessage.ScheduleRuleWeekDaysDN_Su.NiceToString() : ""),
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

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ScheduleRuleWeekDays",
                       new XAttribute("Guid", this.Guid),
                       new XAttribute("StartingOn", this.StartingOn.ToString("o", CultureInfo.InvariantCulture)),
                       new XAttribute("Monday", this.Monday),
                       new XAttribute("Tuesday", this.Monday),
                       new XAttribute("Wednesday", this.Monday),
                       new XAttribute("Thursday", this.Monday),
                       new XAttribute("Friday", this.Monday),
                       new XAttribute("Saturday", this.Monday),
                       new XAttribute("Sunday", this.Monday),
                       new XAttribute("Holiday", this.Holiday),
                       this.Calendar == null ? null! : new XAttribute("HolidayCalendar", ctx.Include(this.Calendar)));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        this.StartingOn = DateTime.ParseExact(element.Attribute("StartingOn")!.Value, "o", CultureInfo.InvariantCulture);
        this.Monday = element.Attribute("Monday")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.Tuesday = element.Attribute("Tuesday")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.Wednesday = element.Attribute("Wednesday")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.Thursday = element.Attribute("Thursday")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.Friday = element.Attribute("Friday")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.Saturday = element.Attribute("Saturday")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.Sunday = element.Attribute("Sunday")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.Holiday = element.Attribute("Holiday")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.Calendar = element.Attribute("HolidayCalendar")?.Let(a => (HolidayCalendarEntity)ctx.GetEntity(Guid.Parse(a.Value)));
    }
}


[EntityKind(EntityKind.Part, EntityData.Master)]
public class ScheduleRuleMonthsEntity : Entity, IScheduleRuleEntity
{
    public Guid Guid { get; set; } = Guid.NewGuid();

    public DateTime StartingOn { get; set; } = Clock.Now.Date;

    public bool January { get; set; }
    public bool February { get; set; }
    public bool March { get; set; }
    public bool April { get; set; }
    public bool May { get; set; }
    public bool June { get; set; }
    public bool July { get; set; }
    public bool August { get; set; }
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

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ScheduleRuleMonths",
                       new XAttribute("Guid", this.Guid),
                       new XAttribute("StartingOn", this.StartingOn.ToString("o", CultureInfo.InvariantCulture)),
                       new XAttribute("January", this.January),
                       new XAttribute("February", this.February),
                       new XAttribute("March", this.March),
                       new XAttribute("April", this.April),
                       new XAttribute("May", this.May),
                       new XAttribute("June", this.June),
                       new XAttribute("July", this.July),
                       new XAttribute("August", this.August),
                       new XAttribute("September", this.September),
                       new XAttribute("October", this.October),
                       new XAttribute("November", this.November),
                       new XAttribute("December", this.December));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        this.StartingOn = DateTime.ParseExact(element.Attribute("StartingOn")!.Value, "o", CultureInfo.InvariantCulture);
        this.January = element.Attribute("January")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.February = element.Attribute("February")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.March = element.Attribute("March")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.April = element.Attribute("April")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.May = element.Attribute("May")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.June = element.Attribute("June")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.July = element.Attribute("July")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.August = element.Attribute("August")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.September = element.Attribute("September")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.October = element.Attribute("October")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.November = element.Attribute("November")?.Let(a => bool.Parse(a.Value)) ?? false;
        this.December = element.Attribute("December")?.Let(a => bool.Parse(a.Value)) ?? false;
    }
}

public enum SchedulerMessage
{
    [Description("Each {0} minutes")]
    Each0Minutes,

    [Description("and holidays")]
    ScheduleRuleWeekDaysDN_AndHoliday,
    [Description("at")]
    ScheduleRuleWeekDaysDN_At,
    [Description("but holidays")]
    ScheduleRuleWeekDaysDN_ButHoliday,


    [Description("Mo")]
    ScheduleRuleWeekDaysDN_Mo,
    [Description("Tu")]
    ScheduleRuleWeekDaysDN_Tu,
    [Description("We")]
    ScheduleRuleWeekDaysDN_We,
    [Description("Th")]
    ScheduleRuleWeekDaysDN_Th,
    [Description("Fr")]
    ScheduleRuleWeekDaysDN_Fr,
    [Description("Sa")]
    ScheduleRuleWeekDaysDN_Sa,
    [Description("Su")]
    ScheduleRuleWeekDaysDN_Su,


    [Description("Day {0} at {1} in {2}")]
    Day0At1In2,
    TaskIsNotRunning
}

