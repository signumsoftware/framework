

using System.ComponentModel;
using Signum.Utilities;

namespace Signum.Entities
{
    public enum OperationMessage
    {
        [Description("Create...")]
        Create,
        [Description("^Create (.*) from .*$")]
        CreateFromRegex,
        [Description("State should be {0} (instead of {1})")]
        StateShouldBe0InsteadOf1,
        [Description("(in user interface)")]
        InUserInterface,
        [Description("Operation {0} ({1}) is not Authorized")]
        Operation01IsNotAuthorized,
        [Description("Delete?")]
        Delete,
        [Description("Please confirm you'd like to delete the entity from the system")]
        PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem,
        [Description("Please confirm you'd like to delete the selected entities from the system")]
        PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem,

        [Description("{0} didn't return an entity")]
        TheOperation0DidNotReturnAnEntity,
        Logs
    }

    public enum SynchronizerMessage
    {
        [Description("     '{0}' has been renamed in {1}?")]
        _0HasBeenRenamedIn1,
        [Description("--- END OF SYNC SCRIPT")]
        EndOfSyncScript,
        [Description("- n: None")]
        NNone,
        [Description("--- START OF SYNC SCRIPT GENERATED ON {0}")]
        StartOfSyncScriptGeneratedOn0
    }

    public enum EngineMessage
    {
        [Description("Concurrency error on the databe, Table = {0}, Id = {1}")]
        ConcurrencyErrorOnDatabaseTable0Id1,
        [Description("Entity with type {0} and Id {1} not found")]
        EntityWithType0AndId1NotFound,
        [Description("No way of mapping type {0} found")]
        NoWayOfMappingType0Found,
        [Description("The entity {0} is new")]
        TheEntity0IsNew,
        [Description("There are '{0}' that refer to this entity")]
        ThereAre0ThatReferThisEntity,
        [Description("There are records in '{0}' refearing to this table by column '{1}'")]
        ThereAreRecordsIn0PointingToThisTableByColumn1,
        [Description("Unautorized access to {0} because {1}")]
        UnauthorizedAccessTo0Because1,
        [Description("There's already a '{0}' with '{1}' {2}")]
        TheresAlreadyA0With12
    }

    public enum NormalWindowMessage
    {
        [Description("{0} Errors: {1}")]
        _0Errors1,
        [Description("1 Error: {0}")]
        _1Error,
        Cancel,
        [Description("Continue anyway?")]
        ContinueAnyway,
        [Description("Continue with errors?")]
        ContinueWithErrors,
        [Description("Fix Errors")]
        FixErrors,
        [Description(@"Imposisible to Save, integrity check failed:

")]
        ImpossibleToSaveIntegrityCheckFailed,
        [Description("Loading {0}...")]
        Loading0,
        [Description(@"There are changes that hasn't been saved. 
Lose changes?")]
        LoseChanges,
        NoDirectErrors,
        Ok,
        Reload,
        [Description(@"The {0} has errors: 
{1}")]
        The0HasErrors1,
        ThereAreChanges,
        [Description(@"There are new changes that will be lost.

	Continue?")]
        ThereAreChangesContinue,
        ThereAreErrors
    }

    public enum EntityControlMessage
    {
        Create,
        Find,
        Detail,
        MoveDown,
        MoveUp,
        Navigate,
        New,
        [Description("no")]
        No,
        NullValueNotAllowed,
        Remove,
        View,
        [Description("yes")]
        Yes
    }

    public enum SearchMessage
    {
        ChooseTheDisplayNameOfTheNewColumn,
        CreateNew,
        Field,
        [Description("Add column")]
        FilterBuilder_AddColumn,
        [Description("Add filter")]
        FilterBuilder_AddFilter,
        [Description("Delete Filter")]
        FilterBuilder_DeleteFilter,
        Filters,
        Find,
        [Description("Finder of {0}")]
        FinderOf0,
        Name,
        [Description("New Column")]
        NewColumn,
        [Description("New Column's Name")]
        NewColumnSName,
        [Description("New Filter")]
        NewFilter,
        NoActionsFound,
        NoColumnSelected,
        NoFiltersSpecified,
        NoResults,
        [Description("of")]
        Of,
        Operation,
        [Description("Query {0} is not allowed")]
        Query0IsNotAllowed,
        [Description("Query {0} is not allowed")]
        Query0NotAllowed,
        [Description("Query {0} is not registered in the DynamicQueryManager")]
        Query0NotRegistered,
        Rename,
        [Description("{0} result[s].")]
        _0Results,
        [Description("first {0} result[s].")]
        First0Results,
        [Description("{0} - {1} of {2} result[s].")]
        _01of2Results,
        Search,
        [Description("Create")]
        Search_Create,
        [Description("Operations")]
        Search_CtxMenuItem_Operations,
        [Description("All")]
        SearchControl_Pagination_All,
        SelectAnElement,
        [Description("There is no {0}")]
        ThereIsNo0,
        Value,
        View,
        [Description("View Selected")]
        ViewSelected
    }

    public enum SelectorMessage
    {
        ChooseAType,
        ChooseAValue,
        [Description("Constructor Selector")]
        ConstructorSelector,
        [Description("Please, choose a value to continue:")]
        PleaseChooseAValueToContinue,
        [Description("Please Select a Constructor")]
        PleaseSelectAConstructor,
        [Description("Please select one of the following types: ")]
        PleaseSelectAType,
        [Description("Type Selector")]
        TypeSelector,
        [Description("A value must be specified for {0}")]
        ValueMustBeSpecifiedFor0
    }

    public enum ConnectionMessage
    {
        AConnectionWithTheServerIsNecessaryToContinue,
        [Description("Sesion Expired")]
        SessionExpired
    }


    public enum PaginationMessage
    {
        All
    }

    public enum NormalControlMessage
    {
        New,
        Save,
        [Description("View for type {0} is not allowed")]
        ViewForType0IsNotAllowed
    }


    public enum CalendarMessage
    {
        [Description("Done")]
        CalendarClose,
        [Description("Next")]
        CalendarNext,
        [Description("Prev")]
        CalendarPrevious,
        [Description("Today")]
        CalendarToday,
        ShowCalendar
    }

    public enum JavascriptMessage
    {
        [Description("Add filter")]
        addFilter,
        [Description("Edit column name")]
        editColumnName,
        [Description("Enter the new column name")]
        enterTheNewColumnName,
        [Description("Error")]
        error,
        [Description("Executed")]
        executed,
        [Description("Hide filters")]
        hideFilters,
        [Description("Loading...")]
        loading,
        [Description(@"There are changes that haven't been saved. 
Lose changes?")]
        loseChanges,
        [Description("No elements selected")]
        noElementsSelected,
        [Description("No results found")]
        noResults,
        [Description("You can select only one element")]
        onlyOneElement,
        [Description("There are errors in the entity, you want to continue?")]
        popupErrors,
        [Description("There are errors in the entity")]
        popupErrorsStop,
        [Description("Remove column")]
        removeColumn,
        [Description("Move left")]
        reorderColumn_MoveLeft,
        [Description("Move right")]
        reorderColumn_MoveRight,
        [Description("Saved")]
        saved,
        [Description("Search")]
        search,
        [Description("selected")]
        searchControlMenuSelected,
        [Description("Select a token")]
        selectToken,
        [Description("Show filters")]
        showFilters,
        [Description("Find")]
        find,
        [Description("Remove")]
        remove,
        [Description("View")]
        view,
        [Description("View")]
        create,
        [Description("Move down")]
        moveDown,
        [Description("Move up")]
        moveUp,
        [Description("Navigate")]
        navigate,
    }

    public enum QuickLinkMessage
    {
        [Description("Quick links")]
        Quicklinks,
        [Description("No {0} found")]
        No0Found
    }

    public enum VoidEnumMessage
    {
        [Description("-")]
        Instance
    }
}