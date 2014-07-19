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

namespace Signum.Entities.Mailing
{
    public static class EmailTemplateOperation
    {
        public static readonly ConstructSymbol<EmailTemplateDN>.From<SystemEmailDN> CreateEmailTemplateFromSystemEmail = OperationSymbol.Construct<EmailTemplateDN>.From<SystemEmailDN>();
        public static readonly ConstructSymbol<EmailTemplateDN>.Simple Create = OperationSymbol.Construct<EmailTemplateDN>.Simple();
        public static readonly ExecuteSymbol<EmailTemplateDN> Save = OperationSymbol.Execute<EmailTemplateDN>();
        public static readonly ExecuteSymbol<EmailTemplateDN> Enable = OperationSymbol.Execute<EmailTemplateDN>();
        public static readonly ExecuteSymbol<EmailTemplateDN> Disable = OperationSymbol.Execute<EmailTemplateDN>();
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EmailTemplateDN : Entity
    {
        public EmailTemplateDN() { }

        public EmailTemplateDN(object queryName)
        {
            this.queryName = queryName;
        }

        [Ignore]
        internal object queryName;

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        bool editableMessage = true;
        public bool EditableMessage
        {
            get { return editableMessage; }
            set { Set(ref editableMessage, value); }
        }

        bool disableAuthorization;
        public bool DisableAuthorization
        {
            get { return disableAuthorization; }
            set { Set(ref disableAuthorization, value); }
        }

        [NotNullable]
        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }

        SystemEmailDN systemEmail;
        public SystemEmailDN SystemEmail
        {
            get { return systemEmail; }
            set { Set(ref systemEmail, value); }
        }

        bool sendDifferentMessages;
        public bool SendDifferentMessages
        {
            get { return sendDifferentMessages; }
            set { Set(ref sendDifferentMessages, value); }
        }

        EmailTemplateContactDN from;
        public EmailTemplateContactDN From
        {
            get { return from; }
            set { Set(ref from, value); }
        }

        [NotNullable]
        MList<EmailTemplateRecipientDN> recipients = new MList<EmailTemplateRecipientDN>();
        [NotNullValidator, NoRepeatValidator]
        public MList<EmailTemplateRecipientDN> Recipients
        {
            get { return recipients; }
            set { Set(ref recipients, value); }
        }

       
        Lite<EmailMasterTemplateDN> masterTemplate;
        public Lite<EmailMasterTemplateDN> MasterTemplate
        {
            get { return masterTemplate; }
            set { Set(ref masterTemplate, value); }
        }

        Lite<SmtpConfigurationDN> smtpConfiguration;
        public Lite<SmtpConfigurationDN> SmtpConfiguration
        {
            get { return smtpConfiguration; }
            set { Set(ref smtpConfiguration, value); }
        }

        bool isBodyHtml = true;
        public bool IsBodyHtml
        {
            get { return isBodyHtml; }
            set { Set(ref isBodyHtml, value); }
        }

        [NotifyCollectionChanged]
        MList<EmailTemplateMessageDN> messages = new MList<EmailTemplateMessageDN>();
        public MList<EmailTemplateMessageDN> Messages
        {
            get { return messages; }
            set { Set(ref messages, value); }
        }

        bool active;
        public bool Active
        {
            get { return active; }
            set { Set(ref active, value); }
        }

        DateTime startDate = TimeZoneManager.Now.TrimToMinutes();
        [MinutesPrecissionValidator]
        public DateTime StartDate
        {
            get { return startDate; }
            set { Set(ref startDate, value); }
        }

        DateTime? endDate;
        [MinutesPrecissionValidator]
        public DateTime? EndDate
        {
            get { return endDate; }
            set { Set(ref endDate, value); }
        }

        static Expression<Func<EmailTemplateDN, bool>> IsActiveNowExpression =
            (mt) => mt.active && TimeZoneManager.Now.IsInInterval(mt.StartDate, mt.EndDate);
        public bool IsActiveNow()
        {
            return IsActiveNowExpression.Evaluate(this);
        }

        protected override void ChildCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (sender == messages)
            {
                if (args.OldItems != null)
                    foreach (var item in args.OldItems.Cast<EmailTemplateMessageDN>())
                        item.Template = null;

                if (args.NewItems != null)
                    foreach (var item in args.NewItems.Cast<EmailTemplateMessageDN>())
                        item.Template = this;
            }
        }

        protected override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);

            messages.ForEach(e => e.Template = this);
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => StartDate) || pi.Is(() => EndDate))
            {
                if (EndDate != null && EndDate < StartDate)
                    return EmailTemplateMessage.EndDateMustBeHigherThanStartDate.NiceToString();
            }

            if (pi.Is(() => Messages) && Active)
            {
                if (Messages == null || !Messages.Any())
                    return EmailTemplateMessage.ThereAreNoMessagesForTheTemplate.NiceToString();

                if (Messages.GroupCount(m => m.CultureInfo).Any(c => c.Value > 1))
                    return EmailTemplateMessage.TheresMoreThanOneMessageForTheSameLanguage.NiceToString();
            }

            return base.PropertyValidation(pi);
        }

        static readonly Expression<Func<EmailTemplateDN, string>> ToStringExpression = e => e.Name;
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
    }

    [Serializable]
    public class EmailTemplateContactDN : EmbeddedEntity
    {
        QueryTokenDN token;
        public QueryTokenDN Token
        {
            get { return token; }
            set { Set(ref token, value); }
        }

        string emailAddress;
        public string EmailAddress
        {
            get { return emailAddress; }
            set { Set(ref emailAddress, value); }
        }

        string displayName;
        public string DisplayName
        {
            get { return displayName; }
            set { Set(ref displayName, value); }
        }

        public override string ToString()
        {
            return "{0} <{1}>".Formato(displayName, emailAddress);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Token))
            {
                if (Token == null && emailAddress.IsNullOrEmpty())
                    return "Token or Email Address must be set";

                if (Token != null && !emailAddress.IsNullOrEmpty())
                    return "Token and Email Address can not be set at the same time";
            }
            
            return null;
        }
    }

    [Serializable]
    public class EmailTemplateRecipientDN : EmailTemplateContactDN
    {
        EmailRecipientKind kind;
        public EmailRecipientKind Kind
        {
            get { return kind; }
            set { Set(ref kind, value); }
        }

        public override string ToString()
        {
            return "{0} {1} <{2}>".Formato(kind.NiceToString(), DisplayName, EmailAddress);
        }
    }

    [Serializable]
    public class EmailTemplateMessageDN : EmbeddedEntity
    {
        private EmailTemplateMessageDN() { }

        public EmailTemplateMessageDN(CultureInfoDN culture)
        {
            this.CultureInfo = culture;
        }

        [Ignore]
        internal EmailTemplateDN template;
        public EmailTemplateDN Template
        {
            get { return template; }
            set { template = value; }
        }

        [NotNullable]
        CultureInfoDN cultureInfo;
        [NotNullValidator]
        public CultureInfoDN CultureInfo
        {
            get { return cultureInfo; }
            set { Set(ref cultureInfo, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string text;
        [StringLengthValidator(AllowNulls = false, Max = int.MaxValue)]
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

        [SqlDbType(Size = 200)]
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
            return cultureInfo.TryToString() ?? EmailTemplateMessage.NewCulture.NiceToString();
        }
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
        [Description("The template is already active")]
        TheTemplateIsAlreadyActive,
        [Description("The template is already inactive")]
        TheTemplateIsAlreadyInactive,
        [Description("Type {0} does not have a property with name {1}")]
        Type0DoesNotHaveAPropertyWithName1,
        [Description("SystemEmail should be set to access model {0}")]
        SystemEmailShouldBeSetToAccessModel0,
        NewCulture,
    }

    public enum EmailTemplateCanAddTokenMessage
    { 
        [Description("No column selected")]
        NoColumnSelected,
        [Description("You cannot add If blocks on collection fields")]
        YouCannotAddIfBlocksOnCollectionFields,
        [Description("You have to add the Element token to use Foreach on collection fields")]
        YouHaveToAddTheElementTokenToUseForeachOnCollectionFields,
        [Description("You can only add Foreach blocks with collection fields")]
        YouCanOnlyAddForeachBlocksWithCollectionFields,
        [Description("You cannot add Blocks with All or Any")]
        YouCannotAddBlocksWithAllOrAny
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
}
