using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.UserQueries;
using Signum.Entities.DynamicQuery;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.ComponentModel;
using Signum.Entities.Translation;
using System.Reflection;
using Signum.Entities.UserAssets;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Entities.Templating;
using System.Xml.Linq;
using Signum.Entities.Mailing;
using Signum.Engine.Basics;
using Signum.Engine;

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
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        public bool EditableMessage { get; set; } = true;

        public bool DisableAuthorization { get; set; }

        [NotNullValidator]
        public QueryEntity Query { get; set; }

        public SystemEmailEntity SystemEmail { get; set; }

        public bool SendDifferentMessages { get; set; }

        public EmailTemplateContactEmbedded From { get; set; }

        [NotNullValidator, NoRepeatValidator]
        public MList<EmailTemplateRecipientEntity> Recipients { get; set; } = new MList<EmailTemplateRecipientEntity>();

        [PreserveOrder]
        [NotNullValidator, NoRepeatValidator, ImplementedBy(typeof(ImageAttachmentEntity)), NotifyChildProperty]
        public MList<IAttachmentGeneratorEntity> Attachments { get; set; } = new MList<IAttachmentGeneratorEntity>();

        public Lite<EmailMasterTemplateEntity> MasterTemplate { get; set; }

        public bool IsBodyHtml { get; set; } = true;

        [NotNullValidator, NotifyCollectionChanged, NotifyChildProperty]
        public MList<EmailTemplateMessageEmbedded> Messages { get; set; } = new MList<EmailTemplateMessageEmbedded>();

        [NotifyChildProperty]
        public TemplateApplicableEval Applicable { get; set; }
        

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
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

        static readonly Expression<Func<EmailTemplateEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        internal void ParseData(QueryDescription queryDescription)
        {
            if (Recipients != null)
                foreach (var r in Recipients.Where(r => r.Token != null))
                    r.Token.ParseData(this, queryDescription, SubTokensOptions.CanElement);

            if (From != null && From.Token != null)
                From.Token.ParseData(this, queryDescription, SubTokensOptions.CanElement);
        }

        public bool IsApplicable(Entity entity)
        {
            if (Applicable == null)
                return true;

            try
            {
                return Applicable.Algorithm.ApplicableUntyped(entity);
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Error evaluating Applicable for EmailTemplate '{Name}' with entity '{entity}': " + e.Message, e);
            }
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            if(this.Attachments != null)
            {
                throw new NotImplementedException("Attachments are not yet exportable");
            }
            XElement recipients = new XElement("Recipients", Recipients.Select(i => 
                new XElement("Recipient", new XAttribute("DisplayName", i.DisplayName),
                     new XAttribute("EmailAddress", i.EmailAddress),
                     new XAttribute("Kind", i.Kind),
                     i.Token != null ? new XAttribute("Token",   i.Token?.Token.FullKey()): null
                    )
                ));

            XElement messages = new XElement("Messages", Messages.Select(x => new XElement("Message",
                 new XAttribute("Subject", x.Subject),
                    new XAttribute("Text", x.Text),
                    new XAttribute("CultureInfo", x.CultureInfo.Id)
                )));
            
            return new XElement("EmailTemplate",
                new XAttribute("Guid", Guid),
                new XAttribute("Query", Query.Key),
                new XAttribute("EditableMessage", EditableMessage),
                new XAttribute("SystemEmail", SystemEmail.FullClassName),
                new XAttribute("SendDifferentMessages", SendDifferentMessages),
                new XElement("From",
                    From.DisplayName != null ? new XAttribute("DisplayName", From.DisplayName) : null,
                    From.EmailAddress != null ? new XAttribute("EmailAddress", From.EmailAddress) : null,
                    From.Token != null ? new XAttribute("Token", From.Token.Token.FullKey()) : null),
                recipients,
                messages,
                new XAttribute("MasterTemplate", MasterTemplate.IdOrNull),
                new XAttribute("IsBodyHtml", IsBodyHtml),
                this.Applicable.Script != null ? new XAttribute("Applicable", this.Applicable.Script) : null
                );

        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Recipients = element.Elements("Recipients").Select(x =>
            {
                var elem = x.Element("Recipient");
                return new EmailTemplateRecipientEntity
                {
                    DisplayName = elem.Attribute("DisplayName").Value,
                    EmailAddress = elem.Attribute("EmailAddress").Value,
                    Kind = (EmailRecipientKind)Enum.Parse(typeof(EmailRecipientKind),elem.Attribute("Kind").Value),
                    Token = elem.Attribute("Token") != null ? new QueryTokenEmbedded(elem.Attribute("Token").Value) : null
                };
            }).ToMList();

            Messages = element.Elements("Messages").Select(x =>
                {
                    var elem = x.Element("Message");
                    return new EmailTemplateMessageEmbedded(elem.Attribute("CultureInfo")?.Let(a => Lite.ParsePrimaryKey<CultureInfoEntity>(a.Value).RetrieveAndForget()))
                    {
                        Subject = elem.Attribute("Subject").Value,
                        Text = elem.Attribute("Text").Value
                    };
                }).ToMList();

            Guid = Guid.Parse(element.Attribute("Guid").Value);
            Query = ctx.GetQuery(element.Attribute("Query").Value);
            EditableMessage = bool.Parse(element.Attribute("EditableMessage").Value);
            SystemEmail = ctx.GetSystemEmail(element.Attribute("SystemEmail").Value);
            SendDifferentMessages = bool.Parse(element.Attribute("SendDifferentMessages").Value);
            var from = element.Element("From");
            From = new EmailTemplateContactEmbedded
            {
                DisplayName = from.Attribute("DisplayName").Value,
                EmailAddress = from.Attribute("EmailAddress").Value,
                Token = from.Attribute("Token") != null ? new QueryTokenEmbedded(from.Attribute("Token").Value) : null,
            };
            MasterTemplate = Lite.ParsePrimaryKey<EmailMasterTemplateEntity>(element.Attribute("MasterTemplate").Value);
            IsBodyHtml = bool.Parse(element.Attribute("IsBodyHtml").Value);
            Applicable = new TemplateApplicableEval { Script = element.Attribute("Applicable") != null ? element.Attribute("Applicable").Value : null };
            ParseData(ctx.GetQueryDescription(Query));
        }

    } 

    [Serializable]
    public class EmailTemplateContactEmbedded : EmbeddedEntity
    {
        public QueryTokenEmbedded Token { get; set; }

        public string EmailAddress { get; set; }

        public string DisplayName { get; set; }

        public override string ToString()
        {
            return "{0} <{1}>".FormatWith(DisplayName, EmailAddress);
        }

        protected override string PropertyValidation(PropertyInfo pi)
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
    public class EmailTemplateRecipientEntity : EmailTemplateContactEmbedded
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

        [NotNullValidator]
        public CultureInfoEntity CultureInfo { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, MultiLine=true)]
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
        internal object TextParsedNode;

        string subject;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 200)]
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
        internal object SubjectParsedNode;

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
        public static ConstructSymbol<EmailTemplateEntity>.From<SystemEmailEntity> CreateEmailTemplateFromSystemEmail;
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
