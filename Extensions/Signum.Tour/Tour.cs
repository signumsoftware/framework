using Signum.UserAssets;
using System.Xml.Linq;
using Signum.Basics;

namespace Signum.Tour;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class TourEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    [ImplementedBy(typeof(TypeEntity), typeof(TourTriggerSymbol))]
    public Lite<Entity> Trigger { get; set; }

    [QueryableProperty, Ignore, NoRepeatValidator, PreserveOrder]
    public MList<TourStepEntity> Steps { get; set; } = new MList<TourStepEntity>();

    public bool ShowProgress { get; set; }

    public bool Animate { get; set; } = true;

    public bool ShowCloseButton { get; set; } = true;

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => IsNew ? this.BaseToString() : Trigger.ToString()!);

    public XElement ToXml(IToXmlContext ctx)
    {
        var forEntityName = 
            Trigger.Entity is TypeEntity ? ((Lite<TypeEntity>)(object)Trigger).RetrieveFromCache().CleanName :
            Trigger.Entity is TourTriggerSymbol symbol ? symbol.Key :
            Trigger.ToString();

        return new XElement("Tour",
            new XAttribute("Guid", Guid),
            new XElement("ForEntity", forEntityName),
            new XElement("Steps", Steps.Select(s => s.ToXml(ctx))),
            new XElement("ShowProgress", ShowProgress),
            new XElement("Animate", Animate),
            new XElement("ShowCloseButton", ShowCloseButton));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        var forEntityKey = element.Element("ForEntity")!.Value;
        Trigger = 
            (Lite<Entity>)ctx.GetTypeLite(forEntityKey) ?? 
            ctx.GetSymbol<TourTriggerSymbol>(forEntityKey)?.ToLite() ?? 
            throw new InvalidOperationException($"ForEntity '{forEntityKey}' not found");
        Steps.Synchronize(element.Element("Steps")!.Elements().ToList(), (s, x) => s.FromXml(x, ctx, this));
        ShowProgress = bool.Parse(element.Element("ShowProgress")!.Value);
        Animate = element.Element("Animate") != null ? bool.Parse(element.Element("Animate")!.Value) : true;
        ShowCloseButton = element.Element("ShowCloseButton") != null ? bool.Parse(element.Element("ShowCloseButton")!.Value) : true;
    }
}

[AutoInit]
public static class TourOperation
{
    public static readonly ExecuteSymbol<TourEntity> Save;
    public static readonly DeleteSymbol<TourEntity> Delete;
}

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class TourTriggerSymbol : Symbol
{
    private TourTriggerSymbol() { }

    public TourTriggerSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}


[EntityKind(EntityKind.Part, EntityData.Master)]
public class TourStepEntity : Entity, ICanBeOrdered
{
    [NotNullValidator(Disabled = true)]
    public Lite<TourEntity> Tour { get; set; }

    [StringLengthValidator(Max = 200), Translatable]
    public string Title { get; set; }

    [PreserveOrder]
    [NoRepeatValidator]
    public MList<CssStepEmbedded> CssSteps { get; set; } = new MList<CssStepEmbedded>();


    [StringLengthValidator(MultiLine = true), Translatable]
    public string Description { get; set; }

    public PopoverSide? Side { get; set; }

    public PopoverAlign? Align { get; set; }
    public ClickTrigger? Click { get; set; }

    public int Order { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Title ?? "Step");

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("TourStep",
            new XElement("CssSteps", CssSteps.Select(s => s.ToXml(ctx))),
            Title == null ? null! : new XElement("Title", Title),
            Description == null ? null! : new XElement("Description", Description),
            Side == null ? null! : new XElement("Side", Side.ToString()),
            Align == null ? null! : new XElement("Align", Align.ToString()),
            Click == null ? null! : new XElement("Click", Click.ToString()));
    }

    public void FromXml(XElement element, IFromXmlContext ctx, TourEntity tour)
    {
        CssSteps.Synchronize(element.Element("CssSteps")?.Elements().ToList() ?? new List<XElement>(), (s, x) => s.FromXml(x, ctx, tour));
        Title = element.Element("Title")!.Value;
        Description = element.Element("Description")!.Value;
        Side = element.Element("Side") != null ? element.Element("Side")!.Value.ToEnum<PopoverSide>() : null;
        Align = element.Element("Align") != null ? element.Element("Align")!.Value.ToEnum<PopoverAlign>() : null;
        Click = element.Element("Click") != null ? element.Element("Click")!.Value.ToEnum<ClickTrigger>() : null;
    }
}

public enum ClickTrigger
{
    OnLoad,
    OnNext,
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

public class CssStepEmbedded : EmbeddedEntity
{
    public CssStepType Type { get; set; }

    [StringLengthValidator(Max = 200)]
    public string? CssSelector { get; set; }

    public PropertyRouteEntity? Property { get; set; }

    [ImplementedBy(typeof(QueryEntity))]
    public Lite<Entity>? ToolbarContent { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(CssSelector))
            return (pi, CssSelector).IsSetOnlyWhen(Type == CssStepType.CSSSelector);

        if (pi.Name == nameof(Property))
            return (pi, Property).IsSetOnlyWhen(Type == CssStepType.Property);

        if (pi.Name == nameof(ToolbarContent))
            return (pi, ToolbarContent).IsSetOnlyWhen(Type == CssStepType.ToolbarContent);

        return base.PropertyValidation(pi);
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("CssStep",
            new XElement("Type", Type.ToString()),
            CssSelector == null ? null! : new XElement("CssSelector", CssSelector),
            Property == null ? null! : new XElement("Property", Property),
            ToolbarContent == null ? null! : new XElement("ToolbarContent", ctx.RetrieveLite(ToolbarContent))    
            );
    }

    public void FromXml(XElement element, IFromXmlContext ctx, TourEntity userAsset)
    {
        Type = element.Element("Type")!.Value.ToEnum<CssStepType>();
        CssSelector = element.Element("CssSelector")?.Value;
        Property = element.Element("Property")?.Let(e => userAsset.Trigger is Lite<TypeEntity> typeEntity 
            ? ctx.GetPropertyRoute(typeEntity.RetrieveFromCache(), e.Value)
            : null);
        var content = element.Element("Content")?.Value;
        ToolbarContent = !content.HasText() ? null :
           (Lite<Entity>?)ctx.TryGetQuery(content)?.ToLite() ??
           (Lite<Entity>?)SymbolLogic<PermissionSymbol>.TryToSymbol(content)?.ToLite() ??
           (Lite<Entity>?)ctx.ParseLite(content, userAsset, PropertyRoute.Construct((TourStepEntity e) => e.CssSteps.First().ToolbarContent)) ??
           throw new InvalidOperationException($"Content '{content}' not found");
    }
}

public enum CssStepType
{
    CSSSelector,
    Property,
    ToolbarContent
}
