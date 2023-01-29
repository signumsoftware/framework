using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Linq;

namespace Signum.Entities.Discovery;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class DiscoveryEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    [StringLengthValidator(Max = 100)]
    public string Name { get; set; }

    [ImplementedBy(typeof(PermissionSymbol), typeof(TypeEntity),typeof(QueryEntity))]
    public Lite<Entity>? Related { get; set; }

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    public DiscoveryType Type { get; set; }

    [PreserveOrder, NoRepeatValidator]
    public MList<DiscoveryMessageEmbedded> Messages { get; set; } = new MList<DiscoveryMessageEmbedded>();

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        var related = element.Attribute("Related")?.Value;

        Name = element.Attribute("Name")!.Value;
        Type = element.Attribute("Type")!.Value.ToEnum<DiscoveryType>();
        Related = !related.HasText() ? null :
            Guid.TryParse(related, out Guid guid) ? (Lite<Entity>)ctx.GetEntity(guid).ToLiteFat() :
            (Lite<Entity>?)ctx.TryGetQuery(related)?.ToLite() ??
            (Lite<Entity>?)ctx.TryPermission(related)?.ToLite() ??
            throw new InvalidOperationException($"Related '{related}' not found");
        Messages = element.Element("Messages")!.Elements("Message").Select(elem => new DiscoveryMessageEmbedded
        {
            Culture = ctx.GetCultureInfoEntity(elem.Attribute("Culture")!.Value),
            Title = elem.Attribute("Title")!.Value!,
            Content = elem.Value,
        }).ToMList();
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Discovery",
           new XAttribute("Name", Name),
           new XAttribute("Type", Type),
           Related == null ? null! : new XAttribute("Related",
           Related is Lite<QueryEntity> query ? ctx.RetrieveLite(query).Key :
           Related is Lite<PermissionSymbol> per ? ctx.RetrieveLite(per).Key :
           (object)ctx.Include((Lite<IUserAssetEntity>)Related)),
            new XElement("Messages", Messages.Select(x =>
                new XElement("Message",
                    new XAttribute("Culture", x.Culture.Name),
                    new XAttribute("Title", x.Title),
                    new XCData(x.Content)
                )))
           );
    }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}

public enum DiscoveryType
{
    [Description("What's new")]
    WhatsNew,

    [Description("Did you know?")]
    DidYouKnow, 
}

public class DiscoveryMessageEmbedded : EmbeddedEntity
{
    public CultureInfoEntity Culture { get; set; }

    [StringLengthValidator(Max = 100)]
    public string Title { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string Content { get; set; }
}

[AutoInit]
public static class DiscoveryOperation
{
    public static readonly ExecuteSymbol<DiscoveryEntity> Save;
    public static readonly DeleteSymbol<DiscoveryEntity> Delete;
}

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class DiscoveryLogEntity : Entity
{
    public Lite<DiscoveryEntity> Discovery { get; set; }

    public DateTime CreationDate { get; private set; } = Clock.Now;

    public Lite<UserEntity> User { get; set; }
}

