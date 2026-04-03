using Signum.Basics;
using Signum.Files;
using System.ComponentModel;

namespace Signum.WhatsNew;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class WhatsNewEntity : Entity
{
    [StringLengthValidator(Max = 30)]
    public string Name { get; set; }

    [CountIsValidator(ComparisonType.GreaterThan, 0)]
    public MList<WhatsNewMessageEmbedded> Messages { get; set; } = new MList<WhatsNewMessageEmbedded>();

    [DefaultFileType(nameof(WhatsNewFileType.WhatsNewPreviewFileType))]
    public FilePathEmbedded? PreviewPicture { get; set; }

    [DefaultFileType(nameof(WhatsNewFileType.WhatsNewAttachmentFileType))]
    public MList<FilePathEmbedded> Attachment { get; set; } = new MList<FilePathEmbedded>();

    public DateTime CreationDate { get; private set; } = Clock.Now;

    public WhatsNewState Status { get; set; } = WhatsNewState.Draft;

    [ImplementedBy(typeof(QueryEntity), typeof(PermissionSymbol))]
    public Lite<Entity>? Related { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}

[AutoInit]
public static class WhatsNewOperation
{
    public static readonly ExecuteSymbol<WhatsNewEntity> Save;
    public static readonly DeleteSymbol<WhatsNewEntity> Delete;
    public static readonly ExecuteSymbol<WhatsNewEntity> Publish;
    public static readonly ExecuteSymbol<WhatsNewEntity> Unpublish;
}

[AutoInit]
public static class WhatsNewFileType
{
    public static FileTypeSymbol WhatsNewAttachmentFileType;
    public static FileTypeSymbol WhatsNewPreviewFileType;
}

public class WhatsNewMessageEmbedded : EmbeddedEntity
{
    public CultureInfoEntity Culture { get; set; }

    public string Title { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string Description { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Title);
}

public enum WhatsNewMessage
{
    News,
    NewNews,
    YourNews,
    MyActiveNews,
    YouDoNotHaveAnyUnreadNews,
    ViewMore,
    CloseAll,
    AllMyNews,
    NewUnreadNews,
    ReadFurther,
    Downloads,
    [Description("{0} contains no version for culture '{1}'")]
    _0ContiansNoVersionForCulture1,
    Language,
    ThisNewIsNoLongerAvailable,
    BackToOverview,
    NewsPage,
    Preview,
    IsRead,
    [Description("Close {0} WhatsNew")]
    Close0WhatsNew,
    [Description("NEW")]
    New,
}

public enum WhatsNewState
{
    Draft,
    Publish
}
