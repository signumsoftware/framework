using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Authorization;
using Signum.Entities.Processes;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Mailing;
using Signum.Entities.Basics;
using System.Globalization;
using System.ComponentModel;
using Signum.Entities.DynamicQuery;
using System.Net.Mail;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main)]
    public class EmailMessageDN : Entity
    {   
        public EmailMessageDN()
        {
            this.UniqueIdentifier = Guid.NewGuid();
        }

        MList<EmailRecipientDN> recipients = new MList<EmailRecipientDN>();
        [CountIsValidator(ComparisonType.GreaterThan, 0)]
        public MList<EmailRecipientDN> Recipients
        {
            get { return recipients; }
            set { Set(ref recipients, value, () => Recipients); }
        }

        [ImplementedByAll]
        Lite<IdentifiableEntity> target;
        public Lite<IdentifiableEntity> Target
        {
            get { return target; }
            set { Set(ref target, value, () => Target); }
        }

        [NotNullable]
        EmailAddressDN from;
        [NotNullValidator]
        public EmailAddressDN From
        {
            get { return from; }
            set { Set(ref from, value, () => From); }
        }

        Lite<EmailTemplateDN> template;
        public Lite<EmailTemplateDN> Template
        {
            get { return template; }
            set { Set(ref template, value, () => Template); }
        }

        DateTime creationTime = TimeZoneManager.Now;
        public DateTime CreationTime
        {
            get { return creationTime; }
            private set { Set(ref creationTime, value, () => CreationTime); }
        }

        DateTime? sent;
        public DateTime? Sent
        {
            get { return sent; }
            set { SetToStr(ref sent, value, () => Sent); }
        }

        DateTime? receptionNotified;
        public DateTime? ReceptionNotified
        {
            get { return receptionNotified; }
            set { Set(ref receptionNotified, value, () => ReceptionNotified); }
        }

        DateTime? received;
        public DateTime? Received
        {
            get { return received; }
            set { Set(ref received, value, () => Received); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string subject;
        [StringLengthValidator(AllowNulls = true, Min = 3)]
        public string Subject
        {
            get { return subject; }
            set { Set(ref subject, value, () => Subject); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string body;
        [StringLengthValidator(AllowNulls = true, Min = 3)]
        public string Body
        {
            get { return body; }
            set { Set(ref body, value, () => Body); }
        }

        bool isBodyHtml = false;
        public bool IsBodyHtml
        {
            get { return isBodyHtml; }
            set { Set(ref isBodyHtml, value, () => IsBodyHtml); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }

        EmailMessageState state;
        public EmailMessageState State
        {
            get { return state; }
            set { Set(ref state, value, () => State); }
        }

        Guid? uniqueIdentifier;
        public Guid? UniqueIdentifier
        {
            get { return uniqueIdentifier; }
            set { Set(ref uniqueIdentifier, value, () => UniqueIdentifier); }
        }

        bool editableMessage = true;
        public bool EditableMessage
        {
            get { return editableMessage; }
            set { Set(ref editableMessage, value, () => EditableMessage); }
        }

        Lite<EmailPackageDN> package;
        public Lite<EmailPackageDN> Package
        {
            get { return package; }
            set { Set(ref package, value, () => Package); }
        }

        [Ignore]
        Lite<Pop3ReceptionDN> reception;
        public Lite<Pop3ReceptionDN> Reception
        {
            get { return reception; }
            set { Set(ref reception, value, () => Reception); }
        }

        static StateValidator<EmailMessageDN, EmailMessageState> validator = new StateValidator<EmailMessageDN, EmailMessageState>(
            m => m.State, m => m.Exception, m => m.Sent, m => m.Received, m => m.ReceptionNotified, m => m.Package)
            {
{EmailMessageState.Created,      false,         false,        false,           false,                    null },
{EmailMessageState.Sent,         false,         true,         false,           false,                    null },
{EmailMessageState.SentException,true,          true,         false,           false,                    null },
{EmailMessageState.ReceptionNotified,true,      true,         false,           true,                     null },
{EmailMessageState.Received,     false,         false,        true,            false,                    false },
            };
    }

    [Serializable]
    public class EmailRecipientDN : EmailAddressDN
    {
        public EmailRecipientDN() { }

        public EmailRecipientDN(EmailOwnerData data)
            : base(data)
        {
            kind = EmailRecipientKind.To;
        }

        public EmailRecipientDN(MailAddress ma, EmailRecipientKind kind) : base(ma)
        {
            this.kind = kind;
        }

        EmailRecipientKind kind;
        public EmailRecipientKind Kind
        {
            get { return kind; }
            set { Set(ref kind, value, () => Kind); }
        }

        public override string ToString()
        {
            return "{0}: {1}".Formato(kind.NiceToString(), base.ToString());
        }

        internal EmailRecipientDN Clone()
        {
            return new EmailRecipientDN
            {
                 DisplayName = DisplayName,
                 EmailAddress = EmailAddress, 
                 EmailOwner = EmailOwner,
                 Kind = Kind,
            };
        }
    }

    public enum EmailRecipientKind
    { 
        To,
        CC,
        Bcc
    }

    [Serializable]
    public class EmailAddressDN : EmbeddedEntity
    {
        public EmailAddressDN() { }

        public EmailAddressDN(EmailOwnerData data)
        {
            emailOwner = data.Owner;
            emailAddress = data.Email;
            displayName = data.DisplayName;
        }

        public EmailAddressDN(MailAddress mailAddress)
        {
            displayName = mailAddress.DisplayName;
            emailAddress = mailAddress.Address;
        }

        Lite<IEmailOwnerDN> emailOwner;
        public Lite<IEmailOwnerDN> EmailOwner
        {
            get { return emailOwner; }
            set { Set(ref emailOwner, value, () => EmailOwner); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string emailAddress;
        [EMailValidator, StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string EmailAddress
        {
            get { return emailAddress; }
            set { Set(ref emailAddress, value, () => EmailAddress); }
        }

        string displayName;
        public string DisplayName
        {
            get { return displayName; }
            set { Set(ref displayName, value, () => DisplayName); }
        }

        public override string ToString()
        {
            return "{0} <{1}>".Formato(displayName, emailAddress);
        }

        public EmailAddressDN Clone()
        {
            return new EmailAddressDN
            {
                DisplayName = DisplayName,
                EmailAddress = EmailAddress,
                EmailOwner = EmailOwner
            }; 
        }
    }

    public enum EmailSenderOperation
    {
        Save
    }

    public enum EmailRecipientOperation
    {
        Save
    }

    public enum EmailMessageState
    {
        Created,
        Sent,
        SentException,
        ReceptionNotified,
        Received
    }

    public interface IEmailOwnerDN : IIdentifiable
    {
        EmailOwnerData EmailOwnerData { get; }
    }

    public class EmailOwnerData : IQueryTokenBag, IEquatable<EmailOwnerData>
    {
        public Lite<IEmailOwnerDN> Owner { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public CultureInfo CultureInfo { get; set; }

        public bool Equals(EmailOwnerData other)
        {
            return Owner != null && other != null && other.Owner != null && Owner.Equals(other.Owner);
        }

        public override bool Equals(object obj)
        {
            return obj is EmailOwnerData && Equals((EmailOwnerData)obj);
        }

        public override int GetHashCode()
        {
            return Owner == null ? base.GetHashCode() : Owner.GetHashCode();
        }
    }

    public enum EmailMessageProcesses
    {
        SendEmails
    }

    public enum EmailMessageOperation
    {
        Send,
        ReSend,
        ReSendEmails,
        CreateMail,
        CreateMailFromTemplate
    }

    public enum EmailMessageMessage
    {
        [Description("The email message cannot be sent from state {0}")]
        TheEmailMessageCannotBeSentFromState0,
        [Description("Message")]
        Message,
    }

    [Serializable, EntityKind(EntityKind.System)]
    public class EmailPackageDN : IdentifiableEntity, IProcessDataDN
    {
        [SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        public override string ToString()
        {
            return "EmailPackage {0}".Formato(Name);
        }
    }
}

