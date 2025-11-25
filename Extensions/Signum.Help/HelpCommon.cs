using System.ComponentModel;
using System.Globalization;

namespace Signum.Help;

public interface IHelpEntity : IEntity
{
    bool ForeachHtmlField(Func<string, string> processHtml);
}

public abstract class HelpImportLineBase : EmbeddedEntity
{
    public TypeEntity Type { get; set; }

    public string Key { get; set; }

    public CultureInfoEntity Culture { get; set; }

    public string? Text { get; set; }

    public ImportAction Action { get; set; }

    [ImplementedByAll()]
    public Lite<IHelpEntity>? ExitingEntity { get; set; }

}

public class HelpImportPreviewLineEmbedded : HelpImportLineBase
{
    [Description("Apply?")]
    public bool? Apply { get; set; }

    public bool ApplyVisible
    {
        get { return Action != ImportAction.NoChange; }
    }

    public override string ToString() => $"{Type} {Key} {Action}";
}

public class HelpImportReportLineEmbedded : HelpImportLineBase
{
    public ImportStatus Status { get; set; }
    public string? ActionError { get; set; }
}

public class HelpImportPreviewModel : ModelEntity
{
    public MList<HelpImportPreviewLineEmbedded> Lines { get; set; } = [];
}

public class HelpImportReportModel : ModelEntity
{
    public MList<HelpImportReportLineEmbedded> Lines { get; set; } = [];
}

public enum ImportStatus
{ 
    NoChange,
    Applied,
    Failed,
    Skipped
}

public enum ImportAction
{
    NoChange,
    Create,
    Override,
//    Remove
}

public enum HelpMessage
{
    [Description("{0} is a {1}")]
    _0IsA1_G,
    [Description("An embedded entity of type {0}")]
    AnEmbeddedEntityOfType0,
    [Description("A reference ({1}) to a {2}")]
    AReference1ToA2_G,
    [Description("lite")]
    lite,
    [Description("full")]
    full,
    [Description("{0} is a {1} and shows {2}")]
    _0IsA1AndShows2,
    [Description("{0} is a calculated {1}")]
    _0IsACalculated1,
    [Description("{0} is a collection of elements {1}")]
    _0IsACollectionOfElements1,
    [Description("amount")]
    Amount,
    [Description("any")]
    Any,
    Appendices,
    Buscador,
    [Description("Call {0} over {1} of the {2}")]
    Call0Over1OfThe2,
    [Description("character")]
    Character,
    [Description("boolean value (yes or no)")]
    BooleanValue,
    [Description("Constructs a new {0}")]
    ConstructsANew0,
    [Description("date")]
    Date,
    [Description("date and time")]
    DateTime,
    [Description("expressed in ")]
    ExpressedIn,
    [Description("from {0} of the {1}")]
    From0OfThe1,
    [Description("from many {0}")]
    FromMany0,
    Help,
    HelpNotLoaded,
    [Description("integer")]
    Integer,
    [Description("Key {0} not found")]
    Key0NotFound,
    [Description(" (optional)")]
    Optional,
    [Description("Property {0} does not exist in type {1}")]
    Property0NotExistsInType1,
    [Description("Query of {0}")]
    QueryOf0,
    [Description("Removes the {0} from the database")]
    RemovesThe0FromTheDatabase,
    [Description(". Should  ")]
    Should,
    [Description("string")]
    String,
    [Description("the database version")]
    TheDatabaseVersion,
    [Description("the property {0}")]
    TheProperty0,
    [Description("value")]
    Value,
    [Description("value like {0}")]
    ValueLike0,
    [Description("your version")]
    YourVersion,
    [Description("{0} is the primary key of {1}, of type {2}")]
    _0IsThePrimaryKeyOf1OfType2,
    [Description("(in {0})")]
    In0,
    Entities,
    SearchText,
    Previous,
    Next,
    Edit,
    Close,
    ViewMore,
    JumpToViewMore,
    ExportAsZip,
    Import,
    ImportCompletedSuccessfully,
    ImportCompletedWithErrors,
    ImportReport,
    ImportError,
    ImportHelpContentsFromZipFile,
    SelectTheZIPFileWithTheHelpContentsThatYouWantToImport,
    ChooseZIPFile,
    SelectedFile,
    HelpZipContents,
    NewKey,
    ActionStatus
}

public enum HelpKindMessage
{
    [Description("His main function is to {0}")]
    HisMainFunctionIsTo0,
    [Description("relate other entities")]
    RelateOtherEntities,
    [Description("classify other entities")]
    ClassifyOtherEntities,
    [Description("store information shared by other entities")]
    StoreInformationSharedByOtherEntities,
    [Description("store information on its own")]
    StoreInformationOnItsOwn,
    [Description("store part of the information of other entity")]
    StorePartOfTheInformationOfAnotherEntity,
    [Description("store parts of information shared by different entities")]
    StorePartsOfInformationSharedByDifferentEntities,

    [Description(" automatically by the system")]
    AutomaticallyByTheSystem,
    [Description(" and is Master Data (rarely changes)")]
    AndIsMasterDataRarelyChanges,
    [Description(" and is Transactional Data (created regularly)")]
    andIsTransactionalDataCreatedRegularly,

}

public enum HelpSyntaxMessage
{
    BoldText,
    ItalicText,
    UnderlineText,
    StriketroughText,
    LinkToEntity,
    LinkToProperty,
    LinkToQuery,
    LinkToOperation,
    LinkToNamespace,
    ExernalLink,
    LinksAllowAnExtraParameterForTheText,
    Example,
    UnorderedListItem,
    OtherItem,
    OrderedListItem,
    TitleLevel,
    Title,
    Images,
    Texts,
    Links,
    Lists,
    InsertImage,
    Options,
    Edit,
    Save,
    Syntax,
    [Description("Translate from...")]
    TranslateFrom

}

public enum HelpSearchMessage
{
    Search,
    [Description("{0} result[s] for {1} (in {2} ms)")]
    _0ResultsFor1In2,
    Results
}

[DescriptionOptions(DescriptionOptions.Members)]
public enum TypeSearchResult
{
    Appendix,
    Namespace,
    Type,
    Property,
    Query,
    Operation,
}
