using Signum.UserAssets;
using System.Xml.Linq;
using System.Globalization;

namespace Signum.Scheduler;


[EntityKind(EntityKind.Shared, EntityData.Master)]
public class HolidayCalendarEntity : Entity, IUserAssetEntity
{
    public Guid Guid { get; set; } = Guid.NewGuid();

    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }


    public MList<HolidayEmbedded> Holidays { get; set; } = new MList<HolidayEmbedded>();

    public bool IsHoliday(DateOnly date)
    {
        return Holidays.Any(h => h.Date == date);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Holidays) && Holidays != null)
        {
            string rep = (from h in Holidays
                          group 1 by h.Date into g
                          where g.Count() > 2
                          select new { Date = g.Key, Num = g.Count() }).ToString(g => "{0} ({1})".FormatWith(g.Date, g.Num), ", ");

            if (rep.HasText())
                return "Some dates have been repeated: " + rep;
        }

        return base.PropertyValidation(pi);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("HolidayCalendar",
            new XAttribute("Guid", this.Guid),
            new XAttribute("Name", this.Name),
            new XElement("Holidays", this.Holidays.Select(p => p.ToXml(ctx))));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        this.Name = element.Attribute("Name")!.Value;
        this.Holidays.Synchronize(element.Element("Holidays")!.Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));
    }
}

[AutoInit]
public static class HolidayCalendarOperation
{
    public static ExecuteSymbol<HolidayCalendarEntity> Save;
    public static DeleteSymbol<HolidayCalendarEntity> Delete;
}

public class HolidayEmbedded : EmbeddedEntity
{
    public DateOnly Date { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? Name { get; set; }

    public override string ToString()
    {
        return "{0:d} {1}".FormatWith(Date, Name);
    }

    internal XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("HolidayElement",
            new XAttribute("Date", this.Date.ToString("o", CultureInfo.InvariantCulture)),
            this.Name == null ? null! : new XAttribute("Name", this.Name));
    }

    internal void FromXml(XElement x, IFromXmlContext ctx)
    {
        this.Date = DateOnly.ParseExact(x.Attribute("Date")!.Value, "o", CultureInfo.InvariantCulture);
        this.Name = x.Attribute("Name")?.Value;
    }
}
