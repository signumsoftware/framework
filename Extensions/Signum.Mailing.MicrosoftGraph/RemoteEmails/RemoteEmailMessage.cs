using Signum.Authorization;
using System.ComponentModel;

namespace Signum.Mailing.MicrosoftGraph.RemoteEmails;

public class RemoteEmailMessageModel : ModelEntity
{
    public string Id { get; set; }

    public Lite<UserEntity> User { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }
    public bool IsBodyHtml { get; set; }
    public bool IsDraft { get; set; }
    public bool IsRead { get; internal set; }
    public bool HasAttachments { get; internal set; }

    public RecipientEmbedded From { get; set; }
    public MList<RecipientEmbedded> ToRecipients { get; set; } = new MList<RecipientEmbedded>();
    public MList<RecipientEmbedded> CcRecipients { get; set; } = new MList<RecipientEmbedded>();
    public MList<RecipientEmbedded> BccRecipients { get; set; } = new MList<RecipientEmbedded>();

    public MList<RemoteAttachmentEmbedded> Attachments { get; set; } = new MList<RemoteAttachmentEmbedded>();

    public RemoteEmailFolderModel? Folder { get; set; }
    public MList<string> Categories { get; set; } = new MList<string>();

    public DateTimeOffset? CreatedDateTime { get; set; }
    public DateTimeOffset? LastModifiedDateTime { get; set; }
    public DateTimeOffset? ReceivedDateTime { get; set; }
    public DateTimeOffset? SentDateTime { get; set; }

    public string? WebLink { get; internal set; }

    public string? Extension0 { get; internal set; }
    public string? Extension1 { get; internal set; }
    public string? Extension2 { get; internal set; }
    public string? Extension3 { get; internal set; }

    protected override void PreSaving(PreSavingContext ctx)
    {
        throw new InvalidOperationException("RemoteEmails can not be saved");
    }

    protected override void PostRetrieving(PostRetrievingContext ctx)
    {
        throw new InvalidOperationException("RemoteEmails can not be retrieved");
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Subject);
}

public enum RemoteEmailMessageMessage
{
    UserFilterNotFound,
    [Description("User {0} has not mailbox")]
    User0HasNoMailbox,
    Deleting,
    Delete,
    Moving,
    Move,
    AddCategory,
    RemoveCategory,
    ChangingCategories,
    Messages,
    Message,
    SelectAFolder,
    [Description("Please confirm you would like to delete {0} from Outlook")]
    PleaseConfirmYouWouldLikeToDelete0FromOutlook,
}

public enum RemoteEmailMessageQuery
{
    RemoteEmailMessages
}

public class RemoteEmailFolderModel : ModelEntity
{
    public string FolderId { get; set; }

    public string DisplayName { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => DisplayName);
}

public class RecipientEmbedded : EmbeddedEntity
{

    public RecipientEmbedded()
    {

    }

    public string? EmailAddress { get; set; }
    
    public string? Name { get; set; }

    public override string ToString()
    {
        return $"{Name ?? ""} <{EmailAddress?.Etc(35) ?? ""}>";
    }
}

public class RemoteAttachmentEmbedded : EmbeddedEntity
{
    public string Id { get; set; }

    public string Name { get; set; }
    
    public long Size { get; set; }

    public DateTimeOffset LastModifiedDateTime { get; set; }

    public bool IsInline { get; set; }
    public string? ContentId { get; set; }

    public override string ToString()
    {
        return $"{Name} {Size.ToComputerSize()}";
    }
}
