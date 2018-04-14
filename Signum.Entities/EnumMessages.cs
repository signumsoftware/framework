

using Signum.Utilities;
using System.ComponentModel;

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
        Confirm,
        [Description("Please confirm you'd like to delete {0} from the system")]
        PleaseConfirmYouDLikeToDelete0FromTheSystem,
        [Description("Please confirm you'd like to delete the entity from the system")]
        PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem,
        [Description("Please confirm you'd like to delete the selected entities from the system")]
        PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem,

        [Description("{0} didn't return an entity")]
        TheOperation0DidNotReturnAnEntity,
        Logs,
        PreviousOperationLog
    }

    public enum SynchronizerMessage
    {
        [Description("--- END OF SYNC SCRIPT")]
        EndOfSyncScript,
        [Description("--- START OF SYNC SCRIPT GENERATED ON {0}")]
        StartOfSyncScriptGeneratedOn0
    }

    public enum EngineMessage
    {
        [Description("Concurrency error on the database, Table = {0}, Id = {1}")]
        ConcurrencyErrorOnDatabaseTable0Id1,
        [Description("Entity with type {0} and Id {1} not found")]
        EntityWithType0AndId1NotFound,
        [Description("No way of mapping type {0} found")]
        NoWayOfMappingType0Found,
        [Description("The entity {0} is new")]
        TheEntity0IsNew,
        [Description("There are '{0}' that refer to this entity")]
        ThereAre0ThatReferThisEntity,
        [Description("There are records in '{0}' referring to this table by column '{1}'")]
        ThereAreRecordsIn0PointingToThisTableByColumn1,
        [Description("Unautorized access to {0} because {1}")]
        UnauthorizedAccessTo0Because1,
        [Description("There's already a {0} with {1} equals to '{2}'")]
        TheresAlreadyA0With1EqualsTo2_G
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
        [Description(@"Impossible to Save, integrity check failed:

")]
        ImpossibleToSaveIntegrityCheckFailed,
        [Description("Loading {0}...")]
        Loading0,
        [Description(@"There are changes that haven't been saved. 
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
        ThereAreErrors,
        Message,
        [Description(@"{0} and Close")]
        _0AndClose,
        [Description("New {0}")]
        New0_G,
        [Description("{0} {1}")]
        Type0Id1
    }

    public enum EntityControlMessage
    {
        Create,
        Find,
        Detail,
        MoveDown,
        MoveUp,
        Move,
        Navigate,
        NullValueNotAllowed,
        Remove,
        View,
    }

    [DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
    public enum BooleanEnum
    {
        [Description("No")]
        False = 0,
        [Description("Yes")]
        True = 1,
    }

    public enum SearchMessage
    {
        ChooseTheDisplayNameOfTheNewColumn,
        Field,
        [Description("Add column")]
        AddColumn,
        CollectionsCanNotBeAddedAsColumns,
        [Description("Add filter")]
        AddFilter,
        [Description("Add value")]
        AddValue,
        [Description("Delete filter")]
        DeleteFilter,
        Filters,
        Find,
        [Description("Finder of {0}")]
        FinderOf0,
        Name,
        [Description("New column's Name")]
        NewColumnSName,
        NoActionsFound,
        NoColumnSelected,
        NoFiltersSpecified,
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
        _0Results_N,
        [Description("first {0} result[s].")]
        First0Results_N,
        [Description("{0} - {1} of {2} result[s].")]
        _01of2Results_N,
        Search,
        Create,
        [Description("Create new {0}")]
        CreateNew0_G,
        [Description("All")]
        SearchControl_Pagination_All,
        [Description("There is no {0}")]
        ThereIsNo0,
        Value,
        View,
        [Description("View Selected")]
        ViewSelected,
        Operations,
        NoResultsFound,
        Explore,
    }

    public enum SelectorMessage
    {
        [Description("Constructor Selector")]
        ConstructorSelector,
        [Description("Please choose a value to continue:")]
        PleaseChooseAValueToContinue,
        [Description("Please select a Constructor")]
        PleaseSelectAConstructor,
        [Description("Please select one of the following types: ")]
        PleaseSelectAType,
        [Description("Type Selector")]
        TypeSelector,
        [Description("A value must be specified for {0}")]
        ValueMustBeSpecifiedFor0,
        ChooseAValue,
        SelectAnElement,
        PleaseSelectAnElement
    }

    public enum ConnectionMessage
    {
        AConnectionWithTheServerIsNecessaryToContinue,
        [Description("Sesion Expired")]
        SessionExpired,
        [Description("A new version has just been deployed! Save changes and {0}")]
        ANewVersionHasJustBeenDeployedSaveChangesAnd0,
        Refresh,
    }


    public enum PaginationMessage
    {
        All
    }

    public enum NormalControlMessage
    {
        Save,
        [Description("View for type {0} is not allowed")]
        ViewForType0IsNotAllowed,

        SaveChangesFirst
    }


    public enum CalendarMessage
    {
        [Description("Today")]
        Today,
    }

    public enum JavascriptMessage
    {
        [Description("Choose a type")]
        chooseAType,
        [Description("Choose a value")]
        chooseAValue,
        [Description("Add filter")]
        addFilter,
        [Description("Open tab")]
        openTab,
        [Description("Rename column")]
        renameColumn,
        [Description("Edit column")]
        editColumn,
        [Description("Enter the new column name")]
        enterTheNewColumnName,
        [Description("Error")]
        error,
        [Description("Executed")]
        executed,
        [Description("Hide filters")]
        hideFilters,
        [Description("Show filters")]
        showFilters,
        [Description("Group results")]
        groupResults,
        [Description("Ungroup results")]
        ungroupResults,
        [Description("Acivate Time Machine")]
        activateTimeMachine,
        [Description("Deactivate Time Machine")]
        deactivateTimeMachine,
        [Description("Show Records")]
        showRecords,
        [Description("Loading...")]
        loading,
        [Description("No actions found")]
        noActionsFound,
        [Description("Save changes before or press cancel")]
        saveChangesBeforeOrPressCancel,
        [Description("Lose current changes?")]
        loseCurrentChanges,
        [Description("No elements selected")]
        noElementsSelected,
        [Description("Search for results")]
        searchForResults,
        [Description("Select only one element")]
        selectOnlyOneElement,
        [Description("There are errors in the entity, do you want to continue?")]
        popupErrors,
        [Description("There are errors in the entity")]
        popupErrorsStop,
        [Description("Insert column")]
        insertColumn,
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
        [Description("Selected")]
        Selected,
        [Description("Select a token")]
        selectToken,

        [Description("Find")]
        find,
        [Description("Remove")]
        remove,
        [Description("View")]
        view,
        [Description("Create")]
        create,
        [Description("Move down")]
        moveDown,
        [Description("Move up")]
        moveUp,
        [Description("Navigate")]
        navigate,
        [Description("New entity")]
        newEntity,
        [Description("Ok")]
        ok,
        [Description("Cancel")]
        cancel,
        [Description("Show Period")]
        showPeriod,
        [Description("Show Previous Operation")]
        showPreviousOperation
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