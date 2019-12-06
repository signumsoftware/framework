using System;
using System.Linq;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using System.ComponentModel;
using System.Reflection;
using Signum.Entities.UserAssets;
using Signum.Entities.Templating;
using System.Xml.Linq;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EmailTemplateEntity : Entity, IUserAssetEntity
    {
        public EmailTemplateEntity()
        {
            RebindEvents();
        }

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        public EmailTemplateEntity(object queryName) : this()
        {
            this.queryName = queryName;
        }

        [Ignore]
        internal object queryName;

        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        public bool EditableMessage { get; set; } = true;

        public bool DisableAuthorization { get; set; }
        
        public QueryEntity Query { get; set; }

        public EmailModelEntity? Model { get; set; }

        public bool SendDifferentMessages { get; set; }

        public EmailTemplateContactEmbedded? From { get; set; }

        [NoRepeatValidator]
        public MList<EmailTemplateRecipientEmbedded> Recipients { get; set; } = new MList<EmailTemplateRecipientEmbedded>();

        [PreserveOrder]
        [NoRepeatValidator, ImplementedBy(typeof(ImageAttachmentEntity)), NotifyChildProperty]
        public MList<IAttachmentGeneratorEntity> Attachments { get; set; } = new MList<IAttachmentGeneratorEntity>();

        public Lite<EmailMasterTemplateEntity>? MasterTemplate { get; set; }

        public bool IsBodyHtml { get; set; } = true;

        [NotifyCollectionChanged, NotifyChildProperty]
        public MList<EmailTemplateMessageEmbedded> Messages { get; set; } = new MList<EmailTemplateMessageEmbedded>();

        [NotifyChildProperty]
        public TemplateApplicableEval? Applicable { get; set; }


        protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Name == nameof(Messages))
            {
                if (Messages == null || !Messages.Any())
                    return EmailTemplateMessage.ThereAreNoMessagesForTheTemplate.NiceToString();

                if (Messages.GroupCount(m => m.CultureInfo).Any(c => c.Value > 1))
                    return EmailTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);

        internal void ParseData(QueryDescription queryDescription)
        {
            if (Recipients != null)
                foreach (var r in Recipients.Where(r => r.Token != null))
                    r.Token!.ParseData(this, queryDescription, SubTokensOptions.CanElement);

            if (From != null && From.Token != null)
                From.Token.ParseData(this, queryDescription, SubTokensOptions.CanElement);
        }

        public bool IsApplicable(Entity? entity)
        {
            if (Applicable == null)
                return true;

            try
            {
                return Applicable.Algorithm!.ApplicableUntyped(entity);
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Error evaluating Applicable for EmailTemplate '{Name}' with entity '{entity}': " + e.Message, e);
            }
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            if(this.Attachments != null && this.Attachments.Count() > 0)
            {
                throw new NotImplementedException("Attachments are not yet exportable");
            }

            return new XElement("EmailTemplate",
                new XAttribute("Name", Name),
                new XAttribute("Guid", Guid),
                new XAttribute("DisableAuthorization", DisableAuthorization),
                new XAttribute("Query", Query.Key),
                new XAttribute("EditableMessage", EditableMessage),
                Model == null ? null : new XAttribute("SystemEmail", Model.FullClassName),
                new XAttribute("SendDifferentMessages", SendDifferentMessages),
                MasterTemplate == null ? null : new XAttribute("MasterTemplate", MasterTemplate.IdOrNull),
                new XAttribute("IsBodyHtml", IsBodyHtml),
                From == null ? null : new XElement("From",
                    From.DisplayName != null ? new XAttribute("DisplayName", From.DisplayName) : null,
                    From.EmailAddress != null ? new XAttribute("EmailAddress", From.EmailAddress) : null,
                    From.Token != null ? new XAttribute("Token", From.Token.Token.FullKey()) : null),
                new XElement("Recipients", Recipients.Select(rec =>
                    new XElement("Recipient", new XAttribute("DisplayName", rec.DisplayName ?? ""),
                         new XAttribute("EmailAddress", rec.EmailAddress ?? ""),
                         new XAttribute("Kind", rec.Kind),
                         rec.Token != null ? new XAttribute("Token", rec.Token?.Token.FullKey()) : null
                        )
                    )),
                new XElement("Messages", Messages.Select(x =>
                    new XElement("Message",
                        new XAttribute("CultureInfo", x.CultureInfo.Name),
                        new XAttribute("Subject", x.Subject),
                        new XCData(x.Text)
                    ))),

                this.Applicable?.Let(app => new XElement("Applicable", new XCData(app.Script)))
                );
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Guid = Guid.Parse(element.Attribute("Guid").Value);
            Name = element.Attribute("Name").Value;
            DisableAuthorization = element.Attribute("DisableAuthorization")?.Let(a => bool.Parse(a.Value)) ?? false;

            Query = ctx.GetQuery(element.Attribute("Query").Value);
            EditableMessage = bool.Parse(element.Attribute("EditableMessage").Value);
            Model = ctx.GetEmailModel(element.Attribute("SystemEmail").Value);
            SendDifferentMessages = bool.Parse(element.Attribute("SendDifferentMessages").Value);

            MasterTemplate = Lite.ParsePrimaryKey<EmailMasterTemplateEntity>(element.Attribute("MasterTemplate").Value);
            IsBodyHtml = bool.Parse(element.Attribute("IsBodyHtml").Value);

            From = element.Element("From")?.Let(from =>  new EmailTemplateContactEmbedded
            {
                DisplayName = from.Attribute("DisplayName").Value,
                EmailAddress = from.Attribute("EmailAddress").Value,
                Token = from.Attribute("Token")?.Let(t => new QueryTokenEmbedded(t.Value)),
            });

            Recipients = element.Element("Recipients").Elements("Recipient").Select(rep => new EmailTemplateRecipientEmbedded
            {
                DisplayName = rep.Attribute("DisplayName").Value,
                EmailAddress = rep.Attribute("EmailAddress").Value,
                Kind = rep.Attribute("Kind").Value.ToEnum<EmailRecipientKind>(),
                Token = rep.Attribute("Token") != null ? new QueryTokenEmbedded(rep.Attribute("Token").Value) : null
            }).ToMList();

            Messages = element.Element("Messages").Elements("Message").Select(elem => new EmailTemplateMessageEmbedded(ctx.GetCultureInfoEntity(elem.Attribute("CultureInfo").Value))
            {
                Subject = elem.Attribute("Subject").Value,
                Text = elem.Value
            }).ToMList();

            Applicable = element.Element("Applicable")?.Let(app => new TemplateApplicableEval { Script =  app.Value});
            ParseData(ctx.GetQueryDescription(Query));
        }

    }

    [Serializable]
    public class EmailTemplateContactEmbedded : EmbeddedEntity
    {
        public QueryTokenEmbedded? Token { get; set; }

        public string? EmailAddress { get; set; }

        public string? DisplayName { get; set; }

        public override string ToString()
        {
            return "{0} <{1}>".FormatWith(DisplayName, EmailAddress);
        }

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Token))
            {
                if (Token == null && EmailAddress.IsNullOrEmpty())
                    return EmailTemplateMessage.TokenOrEmailAddressMustBeSet.NiceToString();

                if (Token != null && !EmailAddress.IsNullOrEmpty())
                    return EmailTemplateMessage.TokenAndEmailAddressCanNotBeSetAtTheSameTime.NiceToString();

                if (Token != null && Token.Token.Type != typeof(EmailOwnerData))
                    return EmailTemplateMessage.TokenMustBeA0.NiceToString(typeof(EmailOwnerData).NiceName());
            }

            return null;
        }
    }

    [Serializable]
    public class EmailTemplateRecipientEmbedded : EmailTemplateContactEmbedded
    {
        public EmailRecipientKind Kind { get; set; }

        public override string ToString()
        {
            return "{0} {1} <{2}>".FormatWith(Kind.NiceToString(), DisplayName, EmailAddress);
        }
    }

    [Serializable]
    public class EmailTemplateMessageEmbedded : EmbeddedEntity
    {
        private EmailTemplateMessageEmbedded() { }

        public EmailTemplateMessageEmbedded(CultureInfoEntity culture)
        {
            this.CultureInfo = culture;
        }

        
        public CultureInfoEntity CultureInfo { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(MultiLine=true)]
        public string Text
        {
            get { return text; }
            set
            {
                if (Set(ref text, value))
                    TextParsedNode = null;
            }
        }

        [Ignore]
        internal object? TextParsedNode;

        string subject;
        [StringLengthValidator(Min = 3, Max = 200)]
        public string Subject
        {
            get { return subject; }
            set
            {
                if (Set(ref subject, value))
                    SubjectParsedNode = null;
            }
        }

        [Ignore]
        internal object? SubjectParsedNode;

        public override string ToString()
        {
            return CultureInfo?.ToString() ?? EmailTemplateMessage.NewCulture.NiceToString();
        }
     }


    public interface IAttachmentGeneratorEntity : IEntity
    {
    }

    [AutoInit]
    public static class EmailTemplateOperation
    {
        public static ConstructSymbol<EmailTemplateEntity>.From<EmailModelEntity> CreateEmailTemplateFromModel;
        public static ConstructSymbol<EmailTemplateEntity>.Simple Create;
        public static ExecuteSymbol<EmailTemplateEntity> Save;
        public static DeleteSymbol<EmailTemplateEntity> Delete;
    }

    public enum EmailTemplateMessage
    {
        [Description("End date must be higher than start date")]
        EndDateMustBeHigherThanStartDate,
        [Description("There are no messages for the template")]
        ThereAreNoMessagesForTheTemplate,
        [Description("There must be a message for {0}")]
        ThereMustBeAMessageFor0,
        [Description("There's more than one message for the same language")]
        TheresMoreThanOneMessageForTheSameLanguage,
        [Description("The text must contain {0} indicating replacement point")]
        TheTextMustContain0IndicatingReplacementPoint,
        [Description("Impossible to access {0} because the template has no {1}")]
        ImpossibleToAccess0BecauseTheTemplateHAsNo1,
        NewCulture,
        TokenOrEmailAddressMustBeSet,
        TokenAndEmailAddressCanNotBeSetAtTheSameTime,
        [Description("Token must be a {0}")]
        TokenMustBeA0,
        ShowPreview,
        HidePreview
    }

    public enum EmailTemplateViewMessage
    {
        [Description("Insert message content")]
        InsertMessageContent,
        [Description("Insert")]
        Insert,
        [Description("Language")]
        Language
    }

    [InTypeScript(true)]
    public enum EmailTemplateVisibleOn
    {
        Single = 1,
        Multiple = 2,
        Query = 4
    }
}
