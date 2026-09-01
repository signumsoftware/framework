using Signum.UserAssets;
using System.Xml.Linq;
using Signum.Dashboard;
using Signum.UserQueries;


namespace Signum.Tour;

[EntityKind(EntityKind.Main, EntityData.Master), PrimaryKey(typeof(Guid))]
public class TourEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    [ImplementedBy(
        typeof(TypeEntity),
        typeof(TourTriggerSymbol),
        typeof(DashboardEntity),
        typeof(UserQueryEntity))]
    public Lite<Entity> Trigger { get; set; }

    [QueryableProperty, Ignore, NoRepeatValidator, PreserveOrder]
    public MList<TourStepEntity> Steps { get; set; } = new MList<TourStepEntity>();

    public bool ShowProgress { get; set; }

    public bool Animate { get; set; } = true;

    public bool ShowCloseButton { get; set; } = true;

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => IsNew ? this.BaseToString() : Trigger.ToString()!);

    public XElement ToXml(IToXmlContext ctx)
    {
        var triggerValue =
            Trigger is Lite<TypeEntity> typeEntity ? typeEntity.RetrieveFromCache().CleanName :
            Trigger is Lite<TourTriggerSymbol> symbol ? ctx.RetrieveLite(symbol).Key :
            Trigger.KeyLong(); //A LiteKey for the user assets (Dashboard, UserQuery), see IFromXmlContext.ParseLite

        return new XElement("Tour",
            new XAttribute("Guid", (Guid)Id),
            new XAttribute("Trigger", triggerValue),
            new XAttribute("ShowProgress", ShowProgress),
            new XAttribute("Animate", Animate),
            new XAttribute("ShowCloseButton", ShowCloseButton),
            Steps.Select(s => s.ToXml(ctx)));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        var triggerValue = element.Attribute("Trigger")!.Value;

        //A LiteKey (or the legacy cleanName|guid) means a user asset, a plain name means a TypeEntity or a TourTriggerSymbol
        Trigger = (triggerValue.Contains(';') || triggerValue.Contains('|')
            ? ctx.ParseLite(triggerValue, this, t => t.Trigger)
            : (Lite<Entity>?)ctx.TryGetTypeLite(triggerValue)
              ?? ctx.TryGetSymbol<TourTriggerSymbol>(triggerValue)?.ToLite())
              ?? throw new InvalidOperationException($"Trigger '{triggerValue}' not found");

        Steps.Synchronize(element.Elements("TourStep").ToList(), (s, x) => s.FromXml(x, ctx, this));
        ShowProgress = bool.Parse(element.Attribute("ShowProgress")!.Value);
        Animate = element.Attribute("Animate")?.Value.Let(bool.Parse) ?? true;
        ShowCloseButton = element.Attribute("ShowCloseButton")?.Value.Let(bool.Parse) ?? true;
    }
}

[AutoInit]
public static class TourOperation
{
    public static readonly ExecuteSymbol<TourEntity> Save;
    public static readonly DeleteSymbol<TourEntity> Delete;
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
            Title == null ? null! : new XAttribute("Title", Title),
            Side == null ? null! : new XAttribute("Side", Side.ToString()!),
            Align == null ? null! : new XAttribute("Align", Align.ToString()!),
            Click == null ? null! : new XAttribute("Click", Click.ToString()!),
            Description == null ? null! : new XElement("Description", Description),
            CssSteps.Select(s => s.ToXml(ctx)));
    }

    public void FromXml(XElement element, IFromXmlContext ctx, TourEntity tour)
    {
        Title = element.Attribute("Title")!.Value;
        Side = element.Attribute("Side")?.Value.ToEnum<PopoverSide>();
        Align = element.Attribute("Align")?.Value.ToEnum<PopoverAlign>();
        Click = element.Attribute("Click")?.Value.ToEnum<ClickTrigger>();
        Description = element.Element("Description")!.Value;
        CssSteps.Synchronize(element.Elements("CssStep").ToList(), (s, x) => s.FromXml(x, ctx, tour));
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

    static readonly PropertyRoute toolbarContentRoute = PropertyRoute.Construct((TourStepEntity ts) => ts.CssSteps.First().ToolbarContent);

    public Guid? DashboardPart { get; set; }

    [StringLengthValidator(Max = 400)]
    public string? TableColumn { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(CssSelector))
            return (pi, CssSelector).IsSetOnlyWhen(Type == CssStepType.CSSSelector);

        if (pi.Name == nameof(Property))
            return (pi, Property).IsSetOnlyWhen(Type == CssStepType.Property);

        if (pi.Name == nameof(ToolbarContent))
            return (pi, ToolbarContent).IsSetOnlyWhen(Type == CssStepType.ToolbarContent);

        if (pi.Name == nameof(DashboardPart))
            return (pi, DashboardPart).IsSetOnlyWhen(Type == CssStepType.DashboardPart);

        if (pi.Name == nameof(TableColumn))
            return (pi, TableColumn).IsSetOnlyWhen(Type == CssStepType.TableColumn);

        return base.PropertyValidation(pi);
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        var toolbarContent =
            ToolbarContent is Lite<QueryEntity> query ? ctx.RetrieveLite(query).Key :
            ToolbarContent is Lite<PermissionSymbol> perm ? ctx.RetrieveLite(perm).Key :
            ToolbarContent?.KeyLong(); //A LiteKey for anything else, see IFromXmlContext.ParseLite

        return new XElement("CssStep",
            new XAttribute("Type", Type.ToString()),
            CssSelector == null ? null! : new XAttribute("CssSelector", CssSelector),
            Property == null ? null! : new XAttribute("Property", Property.ToString()),
            toolbarContent == null ? null! : new XAttribute("ToolbarContent", toolbarContent),
            DashboardPart == null ? null! : new XAttribute("DashboardPart", DashboardPart),
            TableColumn == null ? null! : new XAttribute("TableColumn", TableColumn));
    }

    public void FromXml(XElement element, IFromXmlContext ctx, TourEntity userAsset)
    {
        Type = element.Attribute("Type")!.Value.ToEnum<CssStepType>();
        CssSelector = element.Attribute("CssSelector")?.Value;
        Property = element.Attribute("Property")?.Let(a =>
        {
            var typeEntity =
                userAsset.Trigger is Lite<TypeEntity> te ? te.RetrieveFromCache() :
                userAsset.Trigger is Lite<TourTriggerSymbol> sym ? TourTriggerLogic.GetTriggerType(SymbolLogic<TourTriggerSymbol>.ToSymbol(sym.ToString()!))?.ToTypeEntity() :
                null;

            return typeEntity == null ? null : ctx.GetPropertyRoute(typeEntity, a.Value);
        });
        var content = element.Attribute("ToolbarContent")?.Value;
        //A LiteKey (or the legacy cleanName|guid) means a user asset, a plain name means a QueryEntity or a PermissionSymbol
        ToolbarContent = !content.HasText() ? null :
           content.Contains(';') || content.Contains('|') ? ctx.ParseLite(content, userAsset, toolbarContentRoute) :
           (Lite<Entity>?)ctx.TryGetQuery(content)?.ToLite() ??
           (Lite<Entity>?)SymbolLogic<PermissionSymbol>.TryToSymbol(content)?.ToLite() ??
           throw new InvalidOperationException($"ToolbarContent '{content}' not found");
        DashboardPart = element.Attribute("DashboardPart")?.Value.Let(Guid.Parse);
        TableColumn = element.Attribute("TableColumn")?.Value;
    }
}

public enum CssStepType
{
    CSSSelector,
    Property,
    ToolbarContent,
    DashboardPart,
    TableColumn,
}
