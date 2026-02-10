using Signum.UserAssets;
using System.Xml.Linq;

namespace Signum.Tour;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class TourEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    [PreserveOrder]
    [NoRepeatValidator]
    public MList<TourStepEmbedded> Steps { get; set; } = new MList<TourStepEmbedded>();

    public bool ShowProgress { get; set; }

    public bool Animate { get; set; } = true;

    public bool ShowCloseButton { get; set; } = true;

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Tour",
            new XAttribute("Guid", Guid),
            new XElement("Name", Name),
            new XElement("Steps", Steps.Select(s => s.ToXml(ctx))),
            new XElement("ShowProgress", ShowProgress),
            new XElement("Animate", Animate),
            new XElement("ShowCloseButton", ShowCloseButton));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Name = element.Element("Name")!.Value;
        Steps.Synchronize(element.Element("Steps")!.Elements().ToList(), (s, x) => s.FromXml(x, ctx));
        ShowProgress = bool.Parse(element.Element("ShowProgress")!.Value);
        Animate = element.Element("Animate") != null ? bool.Parse(element.Element("Animate")!.Value) : true;
        ShowCloseButton = element.Element("ShowCloseButton") != null ? bool.Parse(element.Element("ShowCloseButton")!.Value) : true;
    }
}

[AutoInit]
public static class TourOperation
{
    public static readonly ExecuteSymbol<TourEntity> Save;
}

public class TourStepEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 200)]
    public string? Element { get; set; }

    [StringLengthValidator(Max = 200), Translatable]
    public string? Title { get; set; }

    [StringLengthValidator(MultiLine = true), Translatable]
    public string? Description { get; set; }

    public PopoverSide? Side { get; set; }

    public PopoverAlign? Align { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Title ?? Element ?? "Step");

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("TourStep",
            Element == null ? null! : new XElement("Element", Element),
            Title == null ? null! : new XElement("Title", Title),
            Description == null ? null! : new XElement("Description", Description),
            Side == null ? null! : new XElement("Side", Side.ToString()),
            Align == null ? null! : new XElement("Align", Align.ToString()));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Element = element.Element("Element")?.Value;
        Title = element.Element("Title")?.Value;
        Description = element.Element("Description")?.Value;
        Side = element.Element("Side") != null ? element.Element("Side")!.Value.ToEnum<PopoverSide>() : null;
        Align = element.Element("Align") != null ? element.Element("Align")!.Value.ToEnum<PopoverAlign>() : null;
    }
}

public enum PopoverSide
{
    Top,
    Right,
    Bottom,
    Left
}

public enum PopoverAlign
{
    Start,
    Center,
    End
}
