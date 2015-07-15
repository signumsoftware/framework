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
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EmailTemplateEntity : Entity
    {
        public EmailTemplateEntity() { }

        public EmailTemplateEntity(object queryName)
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
        QueryEntity query;
        [NotNullValidator]
        public QueryEntity Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }

        SystemEmailEntity systemEmail;
        public SystemEmailEntity SystemEmail
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

        EmailTemplateContactEntity from;
        public EmailTemplateContactEntity From
        {
            get { return from; }
            set { Set(ref from, value); }
        }

        [NotNullable]
        MList<EmailTemplateRecipientEntity> recipients = new MList<EmailTemplateRecipientEntity>();
        [NotNullValidator, NoRepeatValidator]
        public MList<EmailTemplateRecipientEntity> Recipients
        {
            get { return recipients; }
            set { Set(ref recipients, value); }
        }

       
        Lite<EmailMasterTemplateEntity> masterTemplate;
        public Lite<EmailMasterTemplateEntity> MasterTemplate
        {
            get { return masterTemplate; }
            set { Set(ref masterTemplate, value); }
        }

        bool isBodyHtml = true;
        public bool IsBodyHtml
        {
            get { return isBodyHtml; }
            set { Set(ref isBodyHtml, value); }
        }

        [NotifyCollectionChanged]
        MList<EmailTemplateMessageEntity> messages = new MList<EmailTemplateMessageEntity>();
        public MList<EmailTemplateMessageEntity> Messages
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

        DateTime? startDate;
        [MinutesPrecissionValidator]
        public DateTime? StartDate
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

        static Expression<Func<EmailTemplateEntity, bool>> IsActiveNowExpression =
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
                    foreach (var item in args.OldItems.Cast<EmailTemplateMessageEntity>())
                        item.Template = null;

                if (args.NewItems != null)
                    foreach (var item in args.NewItems.Cast<EmailTemplateMessageEntity>())
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

        static readonly Expression<Func<EmailTemplateEntity, string>> ToStringExpression = e => e.Name;
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
    public class EmailTemplateContactEntity : EmbeddedEntity
    {
        QueryTokenEntity token;
        public QueryTokenEntity Token
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
            return "{0} <{1}>".FormatWith(displayName, emailAddress);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Token))
            {
                if (Token == null && emailAddress.IsNullOrEmpty())
                    return EmailTemplateMessage.TokenOrEmailAddressMustBeSet.NiceToString();

                if (Token != null && !emailAddress.IsNullOrEmpty())
                    return EmailTemplateMessage.TokenAndEmailAddressCanNotBeSetAtTheSameTime.NiceToString();

                if (Token != null && Token.Token.Type != typeof(EmailOwnerData))
                    return EmailTemplateMessage.TokenMustBeA0.NiceToString(typeof(EmailOwnerData).NiceName());
            }
            
            return null;
        }
    }

    [Serializable]
    public class EmailTemplateRecipientEntity : EmailTemplateContactEntity
    {
        EmailRecipientKind kind;
        public EmailRecipientKind Kind
        {
            get { return kind; }
            set { Set(ref kind, value); }
        }

        public override string ToString()
        {
            return "{0} {1} <{2}>".FormatWith(kind.NiceToString(), DisplayName, EmailAddress);
        }
    }

    [Serializable]
    public class EmailTemplateMessageEntity : EmbeddedEntity
    {
        private EmailTemplateMessageEntity() { }

        public EmailTemplateMessageEntity(CultureInfoEntity culture)
        {
            this.CultureInfo = culture;
        }

        [Ignore]
        internal EmailTemplateEntity template;
        public EmailTemplateEntity Template
        {
            get { return template; }
            set { template = value; }
        }

        [NotNullable]
        CultureInfoEntity cultureInfo;
        [NotNullValidator]
        public CultureInfoEntity CultureInfo
        {
            get { return cultureInfo; }
            set { Set(ref cultureInfo, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
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

    public static class EmailTemplateOperation
    {
        public static readonly ConstructSymbol<EmailTemplateEntity>.From<SystemEmailEntity> CreateEmailTemplateFromSystemEmail = OperationSymbol.Construct<EmailTemplateEntity>.From<SystemEmailEntity>();
        public static readonly ConstructSymbol<EmailTemplateEntity>.Simple Create = OperationSymbol.Construct<EmailTemplateEntity>.Simple();
        public static readonly ExecuteSymbol<EmailTemplateEntity> Save = OperationSymbol.Execute<EmailTemplateEntity>();
        public static readonly ExecuteSymbol<EmailTemplateEntity> Enable = OperationSymbol.Execute<EmailTemplateEntity>();
        public static readonly ExecuteSymbol<EmailTemplateEntity> Disable = OperationSymbol.Execute<EmailTemplateEntity>();
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
        [Description("SystemEmail should be set to access model {0}")]
        SystemEmailShouldBeSetToAccessModel0,
        NewCulture,
        TokenOrEmailAddressMustBeSet,
        TokenAndEmailAddressCanNotBeSetAtTheSameTime,
        [Description("Token must be a {0}")]
        TokenMustBeA0,
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
