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
using System.Linq.Expressions;
using Signum.Entities.Files;
using System.Security.Cryptography;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class EmailMessageEntity : Entity
    {   
        public EmailMessageEntity()
        {
            this.UniqueIdentifier = Guid.NewGuid();
        }

        [NotNullable]
        MList<EmailRecipientEntity> recipients = new MList<EmailRecipientEntity>();
        [CountIsValidator(ComparisonType.GreaterThan, 0)]
        public MList<EmailRecipientEntity> Recipients
        {
            get { return recipients; }
            set { Set(ref recipients, value); }
        }

        [ImplementedByAll]
        Lite<Entity> target;
        public Lite<Entity> Target
        {
            get { return target; }
            set { Set(ref target, value); }
        }

        [NotNullable]
        EmailAddressEntity from;
        [NotNullValidator]
        public EmailAddressEntity From
        {
            get { return from; }
            set { Set(ref from, value); }
        }

        Lite<SmtpConfigurationEntity> smtpConfiguration;
        public Lite<SmtpConfigurationEntity> SmtpConfiguration
        {
            get { return smtpConfiguration; }
            set { Set(ref smtpConfiguration, value); }
        }

        Lite<EmailTemplateEntity> template;
        public Lite<EmailTemplateEntity> Template
        {
            get { return template; }
            set { Set(ref template, value); }
        }

        DateTime creationTime = TimeZoneManager.Now;
        public DateTime CreationTime
        {
            get { return creationTime; }
            private set { Set(ref creationTime, value); }
        }

        DateTime? sent;
        public DateTime? Sent
        {
            get { return sent; }
            set { SetToStr(ref sent, value); }
        }

        DateTime? receptionNotified;
        public DateTime? ReceptionNotified
        {
            get { return receptionNotified; }
            set { Set(ref receptionNotified, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string subject;
        [StringLengthValidator(AllowNulls = true)]
        public string Subject
        {
            get { return subject; }
            set { if (Set(ref subject, value))CalculateHash(); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string body;
        [StringLengthValidator(AllowNulls = true)]
        public string Body
        {
            get { return body; }
            set { if (Set(ref body, value))CalculateHash(); }
        }

        static readonly char[] spaceChars = new[] { '\r', '\n', ' ' };

        void CalculateHash()
        {
            var str = subject + body;

            BodyHash = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(str.Trim(spaceChars))));
        }

        [NotNullable, SqlDbType(Size = 150)]
        string bodyHash;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 150)]
        public string BodyHash
        {
            get { return bodyHash; }
            set { Set(ref bodyHash, value); }
        }

        bool isBodyHtml = false;
        public bool IsBodyHtml
        {
            get { return isBodyHtml; }
            set { Set(ref isBodyHtml, value); }
        }

        Lite<ExceptionEntity> exception;
        public Lite<ExceptionEntity> Exception
        {
            get { return exception; }
            set { Set(ref exception, value); }
        }

        EmailMessageState state;
        public EmailMessageState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        Guid? uniqueIdentifier;
        public Guid? UniqueIdentifier
        {
            get { return uniqueIdentifier; }
            set { Set(ref uniqueIdentifier, value); }
        }

        bool editableMessage = true;
        public bool EditableMessage
        {
            get { return editableMessage; }
            set { Set(ref editableMessage, value); }
        }

        Lite<EmailPackageEntity> package;
        public Lite<EmailPackageEntity> Package
        {
            get { return package; }
            set { Set(ref package, value); }
        }

        [NotNullable]
        MList<EmailAttachmentEntity> attachments = new MList<EmailAttachmentEntity>();
        [NotNullValidator, NoRepeatValidator]
        public MList<EmailAttachmentEntity> Attachments
        {
            get { return attachments; }
            set { Set(ref attachments, value); }
        }

        static StateValidator<EmailMessageEntity, EmailMessageState> validator = new StateValidator<EmailMessageEntity, EmailMessageState>(
            m => m.State, m => m.Exception, m => m.Sent, m => m.ReceptionNotified, m => m.Package)
            {
{EmailMessageState.Created,      false,         false,        false,                    null },
{EmailMessageState.Sent,         false,         true,         false,                    null },
{EmailMessageState.SentException,true,          true,         false,                    null },
{EmailMessageState.ReceptionNotified,true,      true,         true,                     null },
{EmailMessageState.Received,     false,         false,         false,                    false },
            };

        static Expression<Func<EmailMessageEntity, string>> ToStringExpression = e => e.Subject;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }


    [Serializable]
    public class EmailReceptionMixin : MixinEntity
    {
        protected EmailReceptionMixin(Entity mainEntity, MixinEntity next) : base(mainEntity, next)
        {
        }

        EmailReceptionInfoEntity receptionInfo;
        public EmailReceptionInfoEntity ReceptionInfo
        {
            get { return receptionInfo; }
            set { Set(ref receptionInfo, value); }
        }
    }

    [Serializable]
    public class EmailReceptionInfoEntity : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex(AllowMultipleNulls = true)]
        string uniqueId;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string UniqueId
        {
            get { return uniqueId; }
            set { Set(ref uniqueId, value); }
        }

        [NotNullable]
        Lite<Pop3ReceptionEntity> reception;
        [NotNullValidator]
        public Lite<Pop3ReceptionEntity> Reception
        {
            get { return reception; }
            set { Set(ref reception, value); }
        }

        [SqlDbType(Size = int.MaxValue), NotNullable]
        string rawContent;
        public string RawContent
        {
            get { return rawContent; }
            set { Set(ref rawContent, value); }
        }

        DateTime sentDate;
        public DateTime SentDate
        {
            get { return sentDate; }
            set { Set(ref sentDate, value); }
        }

        DateTime receivedDate;
        public DateTime ReceivedDate
        {
            get { return receivedDate; }
            set { Set(ref receivedDate, value); }
        }

        DateTime? deletionDate;
        public DateTime? DeletionDate
        {
            get { return deletionDate; }
            set { Set(ref deletionDate, value); }
        }
    }

    [Serializable]
    public class EmailAttachmentEntity : EmbeddedEntity
    {
        EmailAttachmentType type;
        public EmailAttachmentType Type
        {
            get { return type; }
            set { Set(ref type, value); }
        }

        [NotNullable]
        FilePathEntity file;
        [NotNullValidator]
        public FilePathEntity File
        {
            get { return file; }
            set
            {
                if (Set(ref file, value))
                {
                    if (ContentId == null && File != null)
                        ContentId = Guid.NewGuid() + File.FileName;
                }
            }
        }

        [NotNullable, SqlDbType(Size = 300)]
        string contentId;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 300)]
        public string ContentId
        {
            get { return contentId; }
            set { Set(ref contentId, value); }
        }

        public EmailAttachmentEntity Clone()
        {
            return new EmailAttachmentEntity
            {
                ContentId = contentId,
                File = file,
                Type = type,
            }; 
        }

        internal bool Similar(EmailAttachmentEntity a)
        {
            return ContentId == a.ContentId || File.FileName == a.File.FileName;
        }

        public override string ToString()
        {
            return file.TryToString();
        }
    }

    public enum EmailAttachmentType
    {
        Attachment,
        LinkedResource
    }

    [Serializable]
    public class EmailRecipientEntity : EmailAddressEntity, IEquatable<EmailRecipientEntity>
    {
        public EmailRecipientEntity() { }

        public EmailRecipientEntity(EmailOwnerData data)
            : base(data)
        {
            kind = EmailRecipientKind.To;
        }

        public EmailRecipientEntity(MailAddress ma, EmailRecipientKind kind) : base(ma)
        {
            this.kind = kind;
        }

        EmailRecipientKind kind;
        public EmailRecipientKind Kind
        {
            get { return kind; }
            set { Set(ref kind, value); }
        }

        public new EmailRecipientEntity Clone()
        {
            return new EmailRecipientEntity
            {
                 DisplayName = DisplayName,
                 EmailAddress = EmailAddress, 
                 EmailOwner = EmailOwner,
                 Kind = Kind,
            };
        }

        public bool Equals(EmailRecipientEntity other)
        {
            return base.Equals((EmailAddressEntity)other) && kind == other.kind;
        }

        public override bool Equals(object obj)
        {
            return obj is EmailAddressEntity && Equals((EmailAddressEntity)obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ kind.GetHashCode();
        }

        public string BaseToString()
        {
            return base.ToString();
        }

        public override string ToString()
        {
            return "{0}: {1}".FormatWith(kind.NiceToString(), base.ToString());
        }
    }

    public enum EmailRecipientKind
    { 
        To,
        Cc,
        Bcc
    }

    [Serializable]
    public class EmailAddressEntity : EmbeddedEntity, IEquatable<EmailAddressEntity>
    {
        public EmailAddressEntity() { }

        public EmailAddressEntity(EmailOwnerData data)
        {
            emailOwner = data.Owner;
            emailAddress = data.Email;
            displayName = data.DisplayName;
        }

        public EmailAddressEntity(MailAddress mailAddress)
        {
            displayName = mailAddress.DisplayName;
            emailAddress = mailAddress.Address;
        }

        Lite<IEmailOwnerEntity> emailOwner;
        public Lite<IEmailOwnerEntity> EmailOwner
        {
            get { return emailOwner; }
            set { Set(ref emailOwner, value); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string emailAddress;
        [EMailValidator, StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
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

        public EmailAddressEntity Clone()
        {
            return new EmailAddressEntity
            {
                DisplayName = DisplayName,
                EmailAddress = EmailAddress,
                EmailOwner = EmailOwner
            }; 
        }

        public bool Equals(EmailAddressEntity other)
        {
            return other.emailAddress == emailAddress && other.displayName == displayName;
        }

        public override bool Equals(object obj)
        {
            return obj is EmailAddressEntity && Equals((EmailAddressEntity)obj);
        }

        public override int GetHashCode()
        {
            return (emailAddress ?? "").GetHashCode() ^ (displayName ?? "").GetHashCode();
        }
    }

    public enum EmailMessageState
    {
        Created,
        Sent,
        SentException,
        ReceptionNotified,
        Received
    }

    public interface IEmailOwnerEntity : IEntity
    {
        EmailOwnerData EmailOwnerData { get; }
    }

    [DescriptionOptions(DescriptionOptions.Description | DescriptionOptions.Members)]
    public class EmailOwnerData : IEquatable<EmailOwnerData>
    {
        public Lite<IEmailOwnerEntity> Owner { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public CultureInfoEntity CultureInfo { get; set; }

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

        public override string ToString()
        {
            return "{0} <{1}> ({2})".FormatWith(DisplayName, Email, Owner);
        }
    }

    public static class EmailMessageProcess
    {
        public static readonly ProcessAlgorithmSymbol SendEmails = new ProcessAlgorithmSymbol();
    }

    public static class EmailMessageOperation
    {
        public static readonly ExecuteSymbol<EmailMessageEntity> Send = OperationSymbol.Execute<EmailMessageEntity>();
        public static readonly ConstructSymbol<EmailMessageEntity>.From<EmailMessageEntity> ReSend = OperationSymbol.Construct<EmailMessageEntity>.From<EmailMessageEntity>();
        public static readonly ConstructSymbol<ProcessEntity>.FromMany<EmailMessageEntity> ReSendEmails = OperationSymbol.Construct<ProcessEntity>.FromMany<EmailMessageEntity>();
        public static readonly ConstructSymbol<EmailMessageEntity>.Simple CreateMail = OperationSymbol.Construct<EmailMessageEntity>.Simple();
        public static readonly ConstructSymbol<EmailMessageEntity>.From<EmailTemplateEntity> CreateMailFromTemplate = OperationSymbol.Construct<EmailMessageEntity>.From<EmailTemplateEntity>();
        public static readonly DeleteSymbol<EmailMessageEntity> Delete = OperationSymbol.Delete<EmailMessageEntity>();
    }

    public enum EmailMessageMessage
    {
        [Description("The email message cannot be sent from state {0}")]
        TheEmailMessageCannotBeSentFromState0,
        [Description("Message")]
        Message,
        Messages,
        RemainingMessages,
        ExceptionMessages,
        DefaultFromIsMandatory,
        From,
        To,
        Attachments
    }

    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class EmailPackageEntity : Entity, IProcessDataEntity
    {
        [SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value); }
        }

        public override string ToString()
        {
            return "EmailPackage {0}".FormatWith(Name);
        }
    }

    public static class EmailFileType
    {
        public static readonly FileTypeSymbol Attachment = new FileTypeSymbol();
    }
}

