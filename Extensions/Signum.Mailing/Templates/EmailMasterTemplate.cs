using System.Text.RegularExpressions;
using Signum.Basics;
using System.Xml.Linq;
using Signum.UserAssets;
using Signum.Basics;

namespace Signum.Mailing.Templates;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class EmailMasterTemplateEntity : Entity, IUserAssetEntity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public bool IsDefault { get; set; }

    [BindParent, PreserveOrder]
    public MList<EmailMasterTemplateMessageEmbedded> Messages { get; set; } = new MList<EmailMasterTemplateMessageEmbedded>();

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    public static readonly Regex MasterTemplateContentRegex = new Regex(@"\@\[content\]");

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    [PreserveOrder]
    [NoRepeatValidator, ImplementedBy(typeof(ImageAttachmentEntity)), BindParent]
    public MList<IAttachmentGeneratorEntity> Attachments { get; set; } = new MList<IAttachmentGeneratorEntity>();

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Messages))
        {
            if (Messages == null || !Messages.Any())
                return EmailTemplateMessage.ThereAreNoMessagesForTheTemplate.NiceToString();

            if (Messages.GroupBy(m => m.CultureInfo).Any(g => g.Count() > 1))
                return EmailTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage.NiceToString();
        }

        return base.PropertyValidation(pi);
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("EmailMasterTemplate",
            new XAttribute("Name", Name ?? ""),
            new XAttribute("Guid", Guid),

            new XElement("Messages", Messages.Select(x =>
                new XElement("Message",
                    new XAttribute("CultureInfo", x.CultureInfo.Name),
                    new XCData(x.Text)
            ))),

            Attachments.IsEmpty() ? null : new XElement("Attachments", Attachments.Select(x => x.ToXml(ctx)))
        );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Name = element.Attribute("Name")!.Value;
        Messages.Synchronize(element.Element("Messages")!.Elements("Message").ToList(), (et, xml) =>
        {
            et.CultureInfo = CultureInfoLogic.GetCultureInfoEntity(xml.Attribute("CultureInfo")!.Value);
            et.Text = xml.Value;
        });

        Attachments.SynchronizeAttachments(element.Element("Attachments")?.Elements().ToList(), ctx, this);
    }
}

[AutoInit]
public static class EmailMasterTemplateOperation
{
    public static ConstructSymbol<EmailMasterTemplateEntity>.From<EmailMasterTemplateEntity> Clone;
    public static ConstructSymbol<EmailMasterTemplateEntity>.Simple Create;
    public static ExecuteSymbol<EmailMasterTemplateEntity> Save;
    public static DeleteSymbol<EmailMasterTemplateEntity> Delete;
}

public class EmailMasterTemplateMessageEmbedded : EmbeddedEntity
{
    public EmailMasterTemplateMessageEmbedded() { }

    public EmailMasterTemplateMessageEmbedded(CultureInfoEntity culture)
    {
        CultureInfo = culture;
    }

    public CultureInfoEntity CultureInfo { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string Text { get; set; }

    public override string ToString()
    {
        return CultureInfo?.ToString()!;
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Text) && !EmailMasterTemplateEntity.MasterTemplateContentRegex.IsMatch(Text))
        {
            return EmailTemplateMessage.TheTextMustContain0IndicatingReplacementPoint.NiceToString().FormatWith("@[content]");
        }

        return base.PropertyValidation(pi);
    }

    public EmailMasterTemplateMessageEmbedded Clone()
    {
        return new EmailMasterTemplateMessageEmbedded()
        {
            Text = this.Text,
            CultureInfo = this.CultureInfo,
        };
    }
}
