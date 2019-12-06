using Signum.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Signum.Entities.Basics;
using Signum.Entities.UserAssets;
using System.Xml.Linq;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EmailMasterTemplateEntity : Entity , IUserAssetEntity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotifyCollectionChanged, NotifyChildProperty]
        public MList<EmailMasterTemplateMessageEmbedded> Messages { get; set; } = new MList<EmailMasterTemplateMessageEmbedded>();

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();


        public static readonly Regex MasterTemplateContentRegex = new Regex(@"\@\[content\]");

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);

        protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
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
                new XAttribute("Name", this.Name ?? ""),
                new XAttribute("Guid", this.Guid),

                new XElement("Messages", this.Messages.Select(x =>
                    new XElement("Message",
                        new XAttribute("CultureInfo", x.CultureInfo.Name),
                        new XCData(x.Text)
                ))));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Name = element.Attribute("Name").Value;
            Guid = Guid.Parse(element.Attribute("Guid").Value);
            Messages = new MList<EmailMasterTemplateMessageEmbedded>();
            Messages = element.Element("Messages").Elements("Message").Select(elem => new EmailMasterTemplateMessageEmbedded(ctx.GetCultureInfoEntity(elem.Attribute("CultureInfo").Value))
            {
                Text = elem.Value
            }).ToMList();

        }
    }

    [AutoInit]
    public static class EmailMasterTemplateOperation
    {
        public static ConstructSymbol<EmailMasterTemplateEntity>.Simple Create;
        public static ExecuteSymbol<EmailMasterTemplateEntity> Save;
    }

    [Serializable]
    public class EmailMasterTemplateMessageEmbedded : EmbeddedEntity
    {
        private EmailMasterTemplateMessageEmbedded() { }

        public EmailMasterTemplateMessageEmbedded(CultureInfoEntity culture)
        {
            this.CultureInfo = culture;
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
    }
}
