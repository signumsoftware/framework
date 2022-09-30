using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using System.Xml.Linq;
using Signum.Utilities.Reflection;
using System.ComponentModel;

namespace Signum.Entities.Toolbar;

public interface IToolbarEntity: IEntity
{
    MList<ToolbarElementEmbedded> Elements { get; }
}

[EntityKind(EntityKind.Main, EntityData.Master)]
public class ToolbarEntity : Entity, IUserAssetEntity, IToolbarEntity
{
    [ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
    public Lite<IEntity>? Owner { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public ToolbarLocation Location { get; set; }

    public int? Priority { get; set; }

    [PreserveOrder]
    [NoRepeatValidator]
    public MList<ToolbarElementEmbedded> Elements { get; set; } = new MList<ToolbarElementEmbedded>();

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Toolbar",
            new XAttribute("Guid", Guid),
            new XAttribute("Name", Name),
            new XAttribute("Location", Location),
            Owner == null ? null! : new XAttribute("Owner", Owner.KeyLong()),
            Priority == null ? null! : new XAttribute("Priority", Priority.Value.ToString()),
            new XElement("Elements", Elements.Select(p => p.ToXml(ctx))));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Name = element.Attribute("Name")!.Value;
        Location = element.Attribute("Location")?.Value.ToEnum<ToolbarLocation>() ?? ToolbarLocation.Side;
        Owner = element.Attribute("Owner")?.Let(a => ctx.ParseLite(a.Value, this, tb => tb.Owner));
        Priority = element.Attribute("Priority")?.Let(a => int.Parse(a.Value));
        Elements.Synchronize(element.Element("Elements")!.Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));
    }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}

public enum ToolbarLocation
{
    Side,
    Top,
    Main,
}

[AutoInit]
public static class ToolbarOperation
{
    public static readonly ExecuteSymbol<ToolbarEntity> Save;
    public static readonly DeleteSymbol<ToolbarEntity> Delete;
}

public class ToolbarElementEmbedded : EmbeddedEntity
{
    public ToolbarElementType Type { get; set; }

    [StringLengthValidator(Min = 1, Max = 100)]
    public string? Label { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? IconName { get; set; }
  
    public ShowCount? ShowCount { get; set; } 

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? IconColor { get; set; }

    [ImplementedBy(typeof(ToolbarMenuEntity), typeof(ToolbarEntity), typeof(QueryEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(DashboardEntity), typeof(PermissionSymbol))]
    public Lite<Entity>? Content { get; set; }

    [StringLengthValidator(Min = 1, Max = int.MaxValue), URLValidator(absolute: true, aspNetSiteRelative: true)]
    public string? Url { get; set; }

    public bool OpenInPopup { get; set; }


    [Unit("s"), NumberIsValidator(Entities.ComparisonType.GreaterThanOrEqualTo, 10)]
    public int? AutoRefreshPeriod { get; set; }

    internal XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ToolbarElement",
            new XAttribute("Type", Type),
            string.IsNullOrEmpty(Label) ? null! : new XAttribute("Label", Label),
            string.IsNullOrEmpty(IconName) ? null! : new XAttribute("IconName", IconName),
            string.IsNullOrEmpty(IconColor) ? null! :  new XAttribute("IconColor", IconColor),
            ShowCount != null ? new XAttribute("ShowCount", ShowCount) : null!,
            OpenInPopup ? new XAttribute("OpenInPopup", OpenInPopup) : null!,
            AutoRefreshPeriod == null ? null! : new XAttribute("AutoRefreshPeriod", AutoRefreshPeriod),
            Content == null ? null! : new XAttribute("Content",
            Content is Lite<QueryEntity> query ?  ctx.RetrieveLite(query).Key :
            Content is Lite<PermissionSymbol> perm ?  ctx.RetrieveLite(perm).Key :
            (object)ctx.Include((Lite<IUserAssetEntity>)Content)),
            string.IsNullOrEmpty(Url) ? null! : new XAttribute("Url", Url));
    }

    internal void FromXml(XElement x, IFromXmlContext ctx)
    {
        Type = x.Attribute("Type")!.Value.ToEnum<ToolbarElementType>();
        Label = x.Attribute("Label")?.Value;
        ShowCount = x.Attribute("ShowCount")?.Value.ToEnum<ShowCount>();
        IconName = x.Attribute("IconName")?.Value;
        IconColor = x.Attribute("IconColor")?.Value;
        OpenInPopup = x.Attribute("OpenInPopup")?.Value.ToBool() ?? false;
        AutoRefreshPeriod = x.Attribute("AutoRefreshPeriod")?.Value.ToInt() ?? null;

        var content = x.Attribute("Content")?.Value;

        Content = !content.HasText() ? null :
            Guid.TryParse(content, out Guid guid) ? (Lite<Entity>)ctx.GetEntity(guid).ToLiteFat() :
            (Lite<Entity>?)ctx.TryGetQuery(content)?.ToLite() ??
            (Lite<Entity>?)ctx.TryPermission(content)?.ToLite() ??
            throw new InvalidOperationException($"Content '{content}' not found");

        Url = x.Attribute("Url")?.Value;
    }

    static StateValidator<ToolbarElementEmbedded, ToolbarElementType> stateValidator = new StateValidator<ToolbarElementEmbedded, ToolbarElementType>
            (n => n.Type,                   n => n.Content, n=> n.Url, n => n.IconName, n => n.Label)
        {
            { ToolbarElementType.Divider,   false,          false,     false,          false  },
            { ToolbarElementType.Header,    null,           null,       null,           null  },
            { ToolbarElementType.Item,      null,           null,      null,           null },
        };

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if(Type == ToolbarElementType.Item || Type == ToolbarElementType.Header)
        {
            if (pi.Name == nameof(Label))
            {
                if (string.IsNullOrEmpty(Label) && Content == null)
                    return ValidationMessage._0IsMandatoryWhen1IsNotSet.NiceToString(pi.NiceName(), ReflectionTools.GetPropertyInfo(() => Content).NiceName());
            }

            if(pi.Name == nameof(Url))
            { 
                if (string.IsNullOrEmpty(Url) && Content == null && Type == ToolbarElementType.Item)
                    return ValidationMessage._0IsMandatoryWhen1IsNotSet.NiceToString(pi.NiceName(), ReflectionTools.GetPropertyInfo(() => Content).NiceName());
            }
        }

        return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{Type}: {(Label ?? (Content == null ? "Null" : Content.ToString()))}");
}

public enum ShowCount
{
    [Description("More than 0")]
    MoreThan0 = 1,
    Always
}

public enum ToolbarElementType
{
    Header = 2,
    Divider,
    Item,
}

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class ToolbarMenuEntity : Entity, IUserAssetEntity, IToolbarEntity
{
    [ImplementedBy(typeof(UserEntity), typeof(RoleEntity))]
    public Lite<IEntity>? Owner { get; set; }

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    [PreserveOrder]
    [NoRepeatValidator]
    public MList<ToolbarElementEmbedded> Elements { get; set; } = new MList<ToolbarElementEmbedded>();

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ToolbarMenu",
            new XAttribute("Guid", Guid),
            new XAttribute("Name", Name),
            Owner == null ? null! : new XAttribute("Owner", Owner.KeyLong()),
            new XElement("Elements", Elements.Select(p => p.ToXml(ctx))));
    }


    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Name = element.Attribute("Name")!.Value;
        Elements.Synchronize(element.Element("Elements")!.Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));
        Owner = element.Attribute("Owner")?.Let(a => ctx.ParseLite(a.Value, this, tm =>tm.Owner));
    }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}

[AutoInit]
public static class ToolbarMenuOperation
{
    public static readonly ExecuteSymbol<ToolbarMenuEntity> Save;
    public static readonly DeleteSymbol<ToolbarMenuEntity> Delete;
}

public enum ToolbarMessage
{
    RecursionDetected,
    [Description(@"{0} cycles have been found in the Toolbar due to the relationships:")]
    _0CyclesHaveBeenFoundInTheToolbarDueToTheRelationships
}
