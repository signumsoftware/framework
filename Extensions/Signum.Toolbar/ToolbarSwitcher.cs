using Signum.UserAssets;
using System;
using System.Xml.Linq;

namespace  Signum.Toolbar;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class ToolbarSwitcherEntity : Entity, IToolbarEntity, IUserAssetEntity
{
    [UniqueIndex]
    [StringLengthValidator(Max = 100)]
    public string Name { get; set; }

    [PreserveOrder, NoRepeatValidator]
    public MList<ToolbarSwitcherOptionEmbedded> Options { get; set; } = new MList<ToolbarSwitcherOptionEmbedded>();

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();


    public IEnumerable<Lite<IToolbarEntity>> GetSubToolbars() => Options.Select(a => a.ToolbarMenu).OfType<Lite<IToolbarEntity>>();


    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ToolbarSwitcher",
            new XAttribute("Guid", Guid),
            new XAttribute("Name", Name),
            new XElement("Options", Options.Select(p => p.ToXml(ctx))));
    }
        
    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Name = element.Attribute("Name")!.Value;
        Options.Synchronize(element.Element("Options")!.Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));
    }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

   
}

public class ToolbarSwitcherOptionEmbedded : EmbeddedEntity
{
    public Lite<ToolbarMenuEntity> ToolbarMenu { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? IconName { get; set; }

    [StringLengthValidator(Min = 3, Max = 20)]
    [Format(FormatAttribute.Color)]
    public string? IconColor { get; set; }

    internal XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ToolbarSwitcherOption",
            new XAttribute("ToolbarMenu", ctx.Include(ToolbarMenu)),
            string.IsNullOrEmpty(IconName) ? null! : new XAttribute("IconName", IconName),
            string.IsNullOrEmpty(IconColor) ? null! : new XAttribute("IconColor", IconColor)
            );
    }

    internal void FromXml(XElement x, IFromXmlContext ctx)
    {
        IconName = x.Attribute("IconName")?.Value;
        IconColor = x.Attribute("IconColor")?.Value;

        var guid = Guid.Parse(x.Attribute("ToolbarMenu")!.Value);
        ToolbarMenu = (Lite<ToolbarMenuEntity>)ctx.GetEntity(guid).ToLiteFat();
    }

}

[AutoInit]
public static class ToolbarSwitcherOperation
{
    public static readonly ExecuteSymbol<ToolbarSwitcherEntity> Save;
    public static readonly DeleteSymbol<ToolbarSwitcherEntity> Delete;
}
