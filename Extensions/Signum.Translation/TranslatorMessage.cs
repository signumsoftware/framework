using System.ComponentModel;

namespace Signum.Translation;

[AutoInit]
public static class TranslationPermission
{
    public static PermissionSymbol TranslateCode;
    public static PermissionSymbol TranslateInstances;
}

public enum TranslationMessage
{
    [Description("Repeated cultures {0}")]
    RepeatedCultures0,

    CodeTranslations,
    InstanceTranslations,

    [Description("Synchronize {0} in {1}")]
    Synchronize0In1,

    [Description("View {0} in {1}")]
    View0In1,

    [Description("all languages")]
    AllLanguages,

    [Description("{0} already synchronized")]
    _0AlreadySynchronized,

    NothingToTranslate,
    All,

    [Description("Nothing to translate in {0}")]
    NothingToTranslateIn0,

    [Description("sync")]
    Sync,

    [Description("view")]
    View,

    [Description("none")]
    None,

    [Description("edit")]
    Edit,

    [Description("auto-sync")]
    AutoSync,

    Member,
    Type,

    Instance,
    Property,
    Save,
    Search,
    [Description("Press search for results...")]
    PressSearchForResults,
    NoResultsFound,

    Namespace,
    NewTypes,
    NewTranslations,

    BackToTranslationStatus,
    [Description("Back to sync assembly {0}")]
    BackToSyncAssembly0,

    ThisFieldIsTranslatable,


    [Description("{0} outdated translatiosn for {1} have been deleted")]
    _0OutdatedTranslationsFor1HaveBeenDeleted,

    DownloadView,
    DownloadSync,
    Download,




    [Description("Are you sure to continue auto translation {0} for {1} without revision?")]
    AreYouSureToContinueAutoTranslation0For1WithoutRevision,

    [Description("Are you sure to continue auto translation all types for {0} without revision?")]
    AreYouSureToContinueAutoTranslationAllTypesFor0WithoutRevision,

    [Description("Are you sure to continue auto translation all assemblies for {0} without revision?")]
    AreYouSureToContinueAutoTranslationAllAssembliesFor0WithoutRevision,
}

public enum TranslationJavascriptMessage
{
    WrongTranslationToSubstitute,
    RightTranslation,
    RememberChange,
}

