using Signum.Entities.Authorization;
using Signum.Entities.Processes;
using Signum.Entities.Basics;
using System.ComponentModel;
using System.Net.Mail;
using Signum.Entities.Files;
using System.Security.Cryptography;

namespace Signum.Entities.Mailing;

[EntityKind(EntityKind.Main, EntityData.Transactional)]
public class EmailMessageEntity : Entity, IProcessLineDataEntity
{
    public EmailMessageEntity()
    {
        this.UniqueIdentifier = Guid.NewGuid();
        this.BindParent();
    }

    [CountIsValidator(ComparisonType.GreaterThan, 0)]
    public MList<EmailRecipientEmbedded> Recipients { get; set; } = new MList<EmailRecipientEmbedded>();

    [ImplementedByAll]
    public Lite<Entity>? Target { get; set; }

    public EmailFromEmbedded From { get; set; }

    public Lite<EmailTemplateEntity>? Template { get; set; }

    public DateTime CreationDate { get; private set; } = Clock.Now;

    public DateTime? Sent { get; set; }

    public Lite<EmailSenderConfigurationEntity>? SentBy { get; set; }

    public DateTime? ReceptionNotified { get; set; }

    [DbType(Size = int.MaxValue)]
    string? subject;
    [StringLengthValidator(AllowLeadingSpaces = true, AllowTrailingSpaces = true)]
    public string? Subject
    {
        get { return subject; }
        set { if (Set(ref subject, value)) SetCalculateHash(); }
    }

    [DbType(Size = int.MaxValue)]
    BigStringEmbedded body = new BigStringEmbedded();
    [BindParent]
    public BigStringEmbedded Body
    {
        get { return body; }
        set { if (Set(ref body, value)) SetCalculateHash(); }
    }

    static readonly char[] spaceChars = new[] { '\r', '\n', ' ' };


    public string CalculateHash()
    {
        var str = subject + body.Text;

        return Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(str.Trim(spaceChars))));
    }

    public void SetCalculateHash()
    {
        BodyHash = CalculateHash();
    }

    [StringLengthValidator(Min = 1, Max = 150)]
    public string? BodyHash { get; set; }

    public bool IsBodyHtml { get; set; } = false;

    public Lite<ExceptionEntity>? Exception { get; set; }

    public EmailMessageState State { get; set; }

    public Guid? UniqueIdentifier { get; set; }

    public bool EditableMessage { get; set; } = true;

    public Lite<EmailPackageEntity>? Package { get; set; }

    public Guid? ProcessIdentifier { get; set; }

    public int SendRetries { get; set; }

    [NoRepeatValidator]
    public MList<EmailAttachmentEmbedded> Attachments { get; set; } = new MList<EmailAttachmentEmbedded>();

    static StateValidator<EmailMessageEntity, EmailMessageState> validator = new StateValidator<EmailMessageEntity, EmailMessageState>(
        m => m.State, m => m.Exception, m => m.Sent, m => m.ReceptionNotified, m => m.Package)
        {
{EmailMessageState.Created,             false,         false,         false,                    null },
{EmailMessageState.Draft,               false,         false,         false,                    null },
{EmailMessageState.ReadyToSend,         false,         false,         false,                    null },
{EmailMessageState.RecruitedForSending, false,         false,         false,                    null },
{EmailMessageState.Sent,                false,         true,          false,                    null },
{EmailMessageState.SentException,       true,          null,          false,                    null },
{EmailMessageState.ReceptionNotified,   true,          true,          true,                     null },
{EmailMessageState.Received,            false,         false,         false,                    false },
{EmailMessageState.Outdated,            false,         false,         false,                    null },
        };

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Subject!);

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        return validator.Validate(this, pi);
    }


    protected override void PreSaving(PreSavingContext ctx)
    {
        SetCalculateHash();
        base.PreSaving(ctx);    
    }
}


public class EmailReceptionMixin : MixinEntity
{
    protected EmailReceptionMixin(ModifiableEntity mainEntity, MixinEntity next) : base(mainEntity, next)
    {
        this.BindParent();
    }

    [BindParent]
    public EmailReceptionInfoEmbedded? ReceptionInfo { get; set; }
}

public class EmailReceptionInfoEmbedded : EmbeddedEntity
{
    public EmailReceptionInfoEmbedded()
    {
        this.BindParent();
    }

    [UniqueIndex(AllowMultipleNulls = true)]
    [StringLengthValidator(Min = 1, Max = 100)]
    public string UniqueId { get; set; }


    public Lite<Pop3ReceptionEntity> Reception { get; set; }

    [BindParent]
    public BigStringEmbedded RawContent { get; set; } = new BigStringEmbedded();


    public DateTime SentDate { get; set; }

    public DateTime ReceivedDate { get; set; }

    public DateTime? DeletionDate { get; set; }
}

public class EmailAttachmentEmbedded : EmbeddedEntity
{
    public EmailAttachmentType Type { get; set; }

    FilePathEmbedded file;
    //[DefaultFileType(nameof(EmailFileType.Attachment), nameof(EmailFileType))] is optional to register it
    public FilePathEmbedded File
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

    [StringLengthValidator(Min = 1, Max = 300)]
    public string ContentId { get; set; }

    public EmailAttachmentEmbedded Clone()
    {
        return new EmailAttachmentEmbedded
        {
            ContentId = ContentId,
            File = file.Clone(),
            Type = Type,
        };
    }

    internal bool Similar(EmailAttachmentEmbedded a)
    {
        return ContentId == a.ContentId || File.FileName == a.File.FileName;
    }

    public override string ToString()
    {
        return file?.ToString() ?? "";
    }
}

public enum EmailAttachmentType
{
    Attachment,
    LinkedResource
}

public class EmailRecipientEmbedded : EmailAddressEmbedded, IEquatable<EmailRecipientEmbedded>
{
    public EmailRecipientEmbedded() { }

    public EmailRecipientEmbedded(EmailOwnerData data)
        : base(data)
    {
        Kind = EmailRecipientKind.To;
    }

    public EmailRecipientEmbedded(MailAddress ma, EmailRecipientKind kind) : base(ma)
    {
        this.Kind = kind;
    }

    public EmailRecipientKind Kind { get; set; }

    public EmailRecipientEmbedded Clone()
    {
        return new EmailRecipientEmbedded
        {
            DisplayName = DisplayName,
            EmailAddress = EmailAddress,
            EmailOwner = EmailOwner,
            Kind = Kind,
        };
    }

    public override bool Equals(object? obj) => obj is EmailAddressEmbedded eae && Equals(eae);
    public bool Equals(EmailRecipientEmbedded? other) => other != null && base.Equals(other) && Kind == other.Kind;
    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Kind.GetHashCode());

    public string BaseToString()
    {
        return base.ToString();
    }

    public override string ToString()
    {
        return "{0}: {1}".FormatWith(Kind.NiceToString(), base.ToString());
    }
}

public enum EmailRecipientKind
{
    To,
    Cc,
    Bcc
}

public abstract class EmailAddressEmbedded : EmbeddedEntity, IEquatable<EmailAddressEmbedded>
{
    public EmailAddressEmbedded() { }

    public EmailAddressEmbedded(EmailOwnerData data)
    {
        EmailOwner = data.Owner;
        EmailAddress = data.Email!;
        DisplayName = data.DisplayName;
    }

    public EmailAddressEmbedded(MailAddress mailAddress)
    {
        DisplayName = mailAddress.DisplayName;
        EmailAddress = mailAddress.Address;
    }

    public Lite<IEmailOwnerEntity>? EmailOwner { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string EmailAddress { get; set; }

    public bool InvalidEmail { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(EmailAddress) && !InvalidEmail && !EMailValidatorAttribute.EmailRegex.IsMatch(EmailAddress))
            return ValidationMessage._0DoesNotHaveAValid1Format.NiceToString().FormatWith("{0}", pi.NiceName());


        return base.PropertyValidation(pi);
    }

    public string? DisplayName { get; set; }

    public override string ToString()
    {
        return "{0} <{1}>".FormatWith(DisplayName, EmailAddress);
    }

    public override bool Equals(object? obj) => obj is EmailAddressEmbedded eae && Equals(eae);
    public bool Equals(EmailAddressEmbedded? other) => other != null && other.EmailAddress == EmailAddress && other.DisplayName == DisplayName;
    public override int GetHashCode() => HashCode.Combine((EmailAddress ?? "").GetHashCode(), (DisplayName ?? "").GetHashCode());
}

public class EmailFromEmbedded : EmailAddressEmbedded, IEquatable<EmailFromEmbedded>
{
    public EmailFromEmbedded() { }
    public EmailFromEmbedded(EmailOwnerData data) : base(data)
    {
        AzureUserId = data.AzureUserId;
    }

    public Guid? AzureUserId { get; set; }

    public EmailFromEmbedded Clone()
    {
        return new EmailFromEmbedded
        {
            DisplayName = DisplayName,
            EmailAddress = EmailAddress,
            EmailOwner = EmailOwner,
            AzureUserId = AzureUserId,
        };
    }

    public override bool Equals(object? obj) => obj is EmailFromEmbedded eae && Equals(eae);
    public bool Equals(EmailFromEmbedded? other) => other != null && base.Equals(other) && other.AzureUserId == AzureUserId;
    public override int GetHashCode() => base.GetHashCode();
}

public enum EmailMessageState
{
    [Ignore]
    Created,
    Draft,
    ReadyToSend,
    RecruitedForSending,
    Sent,
    SentException,
    ReceptionNotified,
    Received,
    Outdated
}

public interface IEmailOwnerEntity : IEntity
{
}

[DescriptionOptions(DescriptionOptions.Description | DescriptionOptions.Members)]
public class EmailOwnerData : IEquatable<EmailOwnerData>
{
    public Lite<IEmailOwnerEntity>? Owner { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public CultureInfoEntity? CultureInfo { get; set; }
    public Guid? AzureUserId { get; set; }

    public override bool Equals(object? obj) => obj is EmailOwnerData eod && Equals(eod);
    public bool Equals(EmailOwnerData? other)
    {
        return Owner != null && other != null && other.Owner != null && Owner.Equals(other.Owner);
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

[AutoInit]
public static class EmailMessageProcess
{
    public static readonly ProcessAlgorithmSymbol CreateEmailsSendAsync;
    public static ProcessAlgorithmSymbol SendEmails;
}

[AutoInit]
public static class EmailMessageOperation
{
    public static ExecuteSymbol<EmailMessageEntity> Save;
    public static ExecuteSymbol<EmailMessageEntity> ReadyToSend;
    public static ExecuteSymbol<EmailMessageEntity> Send;
    public static ConstructSymbol<EmailMessageEntity>.From<EmailMessageEntity> ReSend;
    public static ConstructSymbol<ProcessEntity>.FromMany<EmailMessageEntity> ReSendEmails;
    public static ConstructSymbol<EmailMessageEntity>.Simple CreateMail;
    public static ConstructSymbol<EmailMessageEntity>.From<EmailTemplateEntity> CreateEmailFromTemplate;
    public static DeleteSymbol<EmailMessageEntity> Delete;
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
    [Description("{0} {1} requires extra parameters")]
    _01requiresExtraParameters
}

[EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
public class EmailPackageEntity : Entity, IProcessDataEntity
{
    [StringLengthValidator(Max = 200)]
    public string? Name { get; set; }

    public override string ToString()
    {
        return "EmailPackage {0}".FormatWith(Name);
    }
}

[AutoInit]
public static class EmailFileType
{
    public static FileTypeSymbol Attachment;
}

[AutoInit]
public static class AsyncEmailSenderPermission
{
    public static PermissionSymbol ViewAsyncEmailSenderPanel;
}

