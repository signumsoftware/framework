

using System.ComponentModel;

namespace Signum.Entities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false)]
public sealed class AllowUnauthenticatedAttribute : Attribute
{

}

public enum OperationMessage
{
    [Description("Create...")]
    Create,
    [Description("^Create (.*) from .*$")]
    CreateFromRegex,
    [Description("Create {0}")]
    Create0,
    [Description("State should be {0} (instead of {1})")]
    StateShouldBe0InsteadOf1,
    [Description("The state of {0} should be {1} (instead of {2})")]
    TheStateOf0ShouldBe1InsteadOf2,
    [Description("(in user interface)")]
    InUserInterface,
    [Description("Operation {0} ({1}) is not Authorized")]
    Operation01IsNotAuthorized,
    Confirm,
    [Description("Please confirm you would like to delete {0} from the system")]
    PleaseConfirmYouWouldLikeToDelete0FromTheSystem,

    [Description("Please confirm you would like to {0} {1}")]
    PleaseConfirmYouWouldLikeTo01,

    [Description("{0} didn't return an entity")]
    TheOperation0DidNotReturnAnEntity,
    Logs,
    PreviousOperationLog,
    LastOperationLog,
    [Description("{0} & Close")]
    _0AndClose,
    [Description("{0} & New")]
    _0AndNew,

    BulkModifications, 
    [Description("Please confirm that you would like to apply the above changes and execute {0} over {1} {2}")]
    PleaseConfirmThatYouWouldLikeToApplyTheAboveChangesAndExecute0Over12,

    Condition, 
    Setters,
    [Description("Add setter")]
    AddSetter,
    [Description("multi setter")]
    MultiSetter,

    [Description("Deleting")]
    Deleting,

    [Description("Executing {0}")]
    Executing0,

    [Description("{0} error[s]")]
    _0Errors,

    [Description("Closing this modal (or browser tab!) will cancel the operation on the server")]
    ClosingThisModalOrBrowserTabWillCancelTheOperation,

    [Description("Cancel Operation?")]
    CancelOperation,

    [Description("Are you sure you want to cancel the operation?")]
    AreYouSureYouWantToCancelTheOperation,

    Operation
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
    [Description("There are '{0}' that refer to this entity by property '{1}'")]
    ThereAre0ThatReferThisEntityByProperty1,
    [Description("There are records in '{0}' referring to this table by column '{1}'")]
    ThereAreRecordsIn0PointingToThisTableByColumn1,
    [Description("Unauthorized access to {0} because {1}")]
    UnauthorizedAccessTo0Because1,


    [Description("There is already a {0} with the same {1}")]
    ThereIsAlreadyA0WithTheSame1_G,
    [Description("There is already a {0} with {1} equals to {2}")]
    ThereIsAlreadyA0With1EqualsTo2_G
}

public enum FrameMessage
{
    [Description("New {0}")]
    New0_G,
    Copied,
    CopyToClipboard,
    Fullscreen,
    ThereAreErrors,
    Main,
}

public enum EntityControlMessage
{
    Create,
    Find,
    Detail,
    MoveDown,
    MoveUp,
    MoveRight,
    MoveLeft,
    Move,
    [Description("Move with Drag and Drop or Ctrl + Up / Down")]
    MoveWithDragAndDropOrCtrlUpDown,
    [Description("Move with Drag and Drop or Ctrl + Left / Right")]
    MoveWithDragAndDropOrCtrlLeftRight,
    Navigate,
    Remove,
    View,
    [Description("Add…")]
    Add,
    Paste,
    [Description("Previous value was: {0}")]
    PreviousValueWas0,
    Moved,
    [Description("Removed {0}")]
    Removed0,
    NoChanges,
    Changed,
    Added,
    RemovedAndSelectedAgain,
    Selected,
    Edit,
    Reload,
    Download,
    Expand,
    Collapse,
    ToggleSideBar,
    Maximize,
    Minimize,
    [Description("{0} character[s]")]
    _0Characters,
    [Description("{0} character[s] remaining")]
    _0CharactersRemaining,
    Close
}

public enum HtmlEditorMessage
{
    [Description("Hyperlink")]
    Hyperlink,
    [Description("Enter your url here...")]
    EnterYourUrlHere,
    [Description("Bold (Ctrl + B)")]
    Bold,
    [Description("Italic (Ctrl + I)")]
    Italic,
    [Description("Underline (Ctrl + U)")]
    Underline,
    Headings,
    UnorderedList,
    OrderedList,
    Quote,
    CodeBlock,
    Code,
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
    ColumnField,
    [Description("Add column")]
    AddColumn,
    CollectionsCanNotBeAddedAsColumns,
    InvalidColumnExpression,
    [Description("Add filter")]
    AddFilter,


    [Description("Add OR group")]
    AddOrGroup,
    [Description("Add AND group")]
    AddAndGroup,

    [Description("OR group")]
    OrGroup,
    [Description("AND group")]
    AndGroup,

    [Description("Group Prefix")]
    GroupPrefix,

    [Description("Add value")]
    AddValue,
    [Description("Delete filter")]
    DeleteFilter,
    [Description("Delete all filter")]
    DeleteAllFilter,
    Filters,
    Columns,
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
    Operator,
    [Description("Query {0} is not allowed")]
    Query0IsNotAllowed,
    [Description("Query {0} is not allowed")]
    Query0NotAllowed,
    [Description("Query {0} is not registered in the QueryLogic.Queries")]
    Query0NotRegistered,
    Rename,
    [Description("{0} result[s].")]
    _0Results_N,
    [Description("first {0} result[s].")]
    First0Results_N,
    [Description("{0} - {1} of {2} result[s].")]
    _01of2Results_N,
    [Description("{0} row[s]")]
    _0Rows_N,
    [Description("{0} group[s] with {1}")]
    _0GroupWith1_N,
    Search,
    Refresh,
    Create,
    [Description("Create new {0}")]
    CreateNew0_G,
    [Description("There is no {0}")]
    ThereIsNo0,
    Value,
    View,
    [Description("View Selected")]
    ViewSelected,
    Operations,
    NoResultsFound,
    NoResultsInThisPage,
    [Description("No results found in page {0}, {1}")]
    NoResultsFoundInPage01,
    [Description("go back to page one")]
    GoBackToPageOne,
    PinnedFilter,
    Label,
    Column,
    [Description("ColSpan")]
    ColSpan,
    Row,
    [Description("When pressed, the filter will take no effect if the value is null")]
    WhenPressedTheFilterWillTakeNoEffectIfTheValueIsNull,
    [Description("When pressed, the filter value will be splited and all the words have to be found")]
    WhenPressedTheFilterValueWillBeSplittedAndAllTheWordsHaveToBeFound,
    ParentValue,
    [Description("Please select a {0}")]
    PleaseSelectA0_G,
    [Description("Please select one or several {0}")]
    PleaseSelectOneOrMore0_G,
    [Description("Please select an Entity")]
    PleaseSelectAnEntity,
    [Description("Please select one or several Entities")]
    PleaseSelectOneOrSeveralEntities,
    [Description("{0} filters collapsed")]
    _0FiltersCollapsed,
    DisplayName,
    [Description("To prevent performance issues automatic search is disabled, check your filters first and then click [Search] button.")]
    ToPreventPerformanceIssuesAutomaticSearchIsDisabledCheckYourFiltersAndThenClickSearchButton,
    [Description("{0} elements")]
    PaginationAll_0Elements,
    [Description("{0} of {1} elements")]
    PaginationPages_0Of01lements,
    [Description("{0} {1} elements")]
    PaginationFirst_01Elements,
    [Description("Return new entity?")]
    ReturnNewEntity,
    [Description("Do you want to return the new {0} ({1})?")]
    DoYouWantToSelectTheNew01_G,
    [Description("Edit pinned filters")]
    EditPinnedFilters,

    [Description("Pin filter")]
    PinFilter,
    [Description("Unpin filter")]
    UnpinFilter,

    [Description("Is Active")]
    IsActive,

    [Description("Split")]
    Split,

    [Description("Summary header")]
    SummaryHeader,
    [Description("Summary header must be an aggregate (like Sum, Count, etc..)")]
    SummaryHeaderMustBeAnAggregate,

    HiddenColumn,
    ShowHiddenColumns,
    HideHiddenColumns,

    ShowMore,

    GroupKey,
    DerivedGroupKey,

    Copy,

    [Description("More than one {0} selected")]
    MoreThanOne0Selected,
    CombineRowsWith,

    [Description("Equal {0}")]
    Equal0,

    SwitchViewMode,

    [Description("Splits the string value by space and searches each part independently in an AND group")]
    SplitsTheStringValueBySpaceAndSearchesEachPartIndependentlyInAnANDGroup,

    [Description("Splits the values and searches each one independently in an AND group")]
    SplitsTheValuesAndSearchesEachOneIndependentlyInAnANDGroup,

    [Description("No results found because the rule {0} does not allowed to explore {1} without filtering first")]
    NoResultsFoundBecauseTheRule0DoesNotAllowedToExplore1WithoutFilteringFirst,

    [Description("No results found because you are not allowed to explore {0} without filtering by {1} first")]
    NoResultsFoundBecauseYouAreNotAllowedToExplore0WithoutFilteringBy1First,

    SimpleFilters,
    AdvancedFilters,
    FilterDesigner,
    TimeMachine,
    Options,

    [Description("You have selected all rows on this page. Do you want to {0} only these rows, or to all rows across all pages?")]
    YouHaveSelectedAllRowsOnThisPageDoYouWantTo0OnlyTheseRowsOrToAllRowsAcrossAllPages,

    [Description("Current Page")]
    CurrentPage,

    [Description("All Pages")]
    AllPages,

    [Description("Filter-type Selection")]
    FilterTypeSelection,
    FilterMenu,
    OperationsForSelectedElements,

    PaginationMode,
    NumberOfElementsForPagination,

    SelectAllResults,
    [Description("{0} Result Table")]
    _0ResultTable,
    [Description("Select row {0}")]
    SelectRow0_,
    Enter,
}

public enum SearchHelpMessage
{
    SearchHelp,
    SearchControl,
    [Description("The {0} is very powerful, but can be intimidating. Take some time to learn how to use it... will be worth it!")]
    The0IsVeryPowerfulButCanBeIntimidatingTakeSomeTimeToLearnHowToUseItWillBeWorthIt,
    TheBasics,
    [Description("Currently we are in the query {0}, you can open a {1} by clicking the {2} icon, or doing {3} in the row (but not in a link!).")]
    CurrentlyWeAreInTheQuery0YouCanOpenA1ByClickingThe2IconOrDoing3InTheRowButNotInALink,
    [Description("Currently we are in the query {0}, grouped by {1}, you can open a group by clicking the {2} icon, or doing {3} in the row (but not in a link!).")]
    CurrentlyWeAreInTheQuery0GroupedBy1YouCanOpenAGroupByClickingThe2IconOrDoing3InTheRowButNotInALink,
    [Description("double-click")]
    DoubleClick,
    GroupedBy,
    [Description("Doing {0} in the row will select the entity and close the modal automatically, alternatively you can select one entity and click OK.")]
    Doing0InTheRowWillSelectTheEntityAndCloseTheModalAutomaticallyAlternativelyYouCanSelectOneEntityAndClickOK,
    [Description("You can use the prepared filters on the top to quickly find the {0} you are looking for.")]
    YouCanUseThePreparedFiltersOnTheTopToQuicklyFindThe0YouAreLookingFor,
    [Description("Ordering results")]
    OrderingResults,
    [Description("You can order results by clicking in a column header, default ordering is {0} and by clicking again it changes to {1}. You can order by more than one column if you keep {2} down when clicking on the columns header.")]
    YouCanOrderResultsByClickingInAColumnHeaderDefaultOrderingIs0AndByClickingAgainItChangesTo1YouCanOrderByMoreThanOneColumnIfYouKeep2DownWhenClickingOnTheColumnsHeader,
    Ascending,
    Descending,
    Shift,
    [Description("Change columns")]
    ChangeColumns,
    [Description("You are not limited to the columns you see! The default columns can be changed by {0} in a column header and then select {1}, {2} or {3}.")]
    YouAreNotLimitedToTheColumnsYouSeeTheDefaultColumnsCanBeChangedBy0InAColumnHeaderAndThenSelect123,
    [Description("right-clicking")]
    RightClicking,
    [Description("right-click")]
    RightClick,
    [Description("Insert Column")]
    InsertColumn,
    [Description("Edit Column")]
    EditColumn,
    [Description("Remove Column")]
    RemoveColumn,
    [Description("You can also {0} the columns by dragging and dropping them to another position.")]
    YouCanAlso0TheColumnsByDraggingAndDroppingThemToAnotherPosition,
    [Description("rearrange")]
    Rearrange,
    [Description("When inserting, the new column will be added before or after the selected column, depending where you {0}.")]
    WhenInsertingTheNewColumnWillBeAddedBeforeOrAfterTheSelectedColumnDependingWhereYou0,
    [Description("Click on the {0} button to open the Advanced filters, this will allow you create complex filters manually by selecting the {1} of the entity (or a related entities), a comparison {2} and a {3} to compare.")]
    ClickOnThe0ButtonToOpenTheAdvancedFiltersThisWillAllowYouCreateComplexFiltersManuallyBySelectingThe1OfTheEntityOrARelatedEntitiesAComparison2AndA3ToCompare,
    [Description("Trick: You can {0} on a {1} and choose {2} to quickly filter by this column. Even more, you can {3} to filter by this {4} directly.")]
    TrickYouCan0OnA1AndChoose2ToQuicklyFilterByThisColumnEvenMoreYouCan3ToFilterByThis4Directly,
    [Description("column header")]
    ColumnHeader,
    [Description("Grouping results by one (or more) column")]
    GroupingResultsByOneOrMoreColumn,
    [Description("You can group results by {0} in a column header and selecting {1}. All the columns will disapear except the selected one and an agregation column (typically {2}).")]
    YouCanGroupResultsBy0InAColumnHeaderAndSelecting1AllTheColumnsWillDisappearExceptTheSelectedOneAndAnAggregationColumnTypically2,
    [Description("Group by this column")]
    GroupByThisColumn,
    [Description("Group help")]
    GroupHelp,
    [Description("Any new column should either be an aggregate {0} or it will be considered a new group key {1}.")]
    AnyNewColumnShouldEitherBeAnAggregate0OrItWillBeConsideredANewGroupKey1,
    [Description("Once grouping you can filter normally or using aggregates as the field ({0}).")]
    OnceGroupingYouCanFilterNormallyOrUsingAggregatesAsTheField0,
    [Description("in SQL")]
    InSql,
    [Description("Finally you can stop grouping by {0} in a column header and select {1}")]
    FinallyYouCanStopGroupingBy0InAColumnHeaderAndSelect1,
    [Description("Restore default columns")]
    RestoreDefaultColumns,
    [Description("A query expression could be any field of the")]
    AQueryExpressionCouldBeAnyFieldOfThe,
    [Description("like")]
    Like,
    [Description("or any other field that you see in the")]
    OrAnyOtherFieldThatYouSeeInThe,
    [Description("when you click")]
    WhenYouClick,
    [Description("icon) or any related entity.")]
    IconOrAnyRelatedEntity,
    [Description("A query expression could be any column of the")]
    AQueryExpressionCouldBeAnyColumnOfThe,
    [Description("or any other field that you see in the Project when you click")]
    OrAnyOtherFieldThatYouSeeInTheProjectWhenYouClick,
    [Description("The operation that will be used to compare the")]
    TheOperationThatWillBeUsedToCompareThe,
    [Description("with the")]
    WithThe,
    [Description("Equals, Distinct, GreaterThan")]
    EqualsDistinctGreaterThan,
    [Description("etc...")]
    Etc,
    [Description("The value that will be compared with the")]
    TheValueThatWillBeComparedWithThe,
    [Description("typically has the same type as the field, but some operators like")]
    TypicallyHasTheSameTypeAsTheFieldButSomeOperatorsLike,
    [Description("allow to select multiple values.")]
    AllowToSelectMultipleValues,
    [Description("You are editing a column, let me explain what each field does:")]
    YouAreEditingAColumnLetMeExplainWhatEachFieldDoes,
    [Description("Can be used as the first item, counts the number of rows on each group.")]
    CanBeUsedAsTheFirstItemCountsTheNumberOfRowsOnEachGroup,

}

public enum SelectorMessage
{
    [Description("Constructor Selector")]
    ConstructorSelector,
    [Description("Please choose a value to continue:")]
    PleaseChooseAValueToContinue,
    [Description("Please select a constructor")]
    PleaseSelectAConstructor,
    [Description("Please select one of the following types: ")]
    PleaseSelectAType,
    [Description("Type Selector")]
    TypeSelector,
    [Description("A value must be specified for {0}")]
    ValueMustBeSpecifiedFor0,
    ChooseAValue,
    SelectAnElement,
    PleaseSelectAnElement,
    [Description("{0} selector")]
    _0Selector,
    [Description("Please choose a {0} to continue:")]
    PleaseChooseA0ToContinue,

    [Description("Creation of {0} cancelled")]
    CreationOf0Cancelled,

    ChooseValues,

    [Description("Please select at least one value to continue:")]
    PleaseSelectAtLeastOneValueToContinue
}

[AllowUnauthenticated]
public enum ConnectionMessage
{
    VersionInfo,
    [Description("A new version has just been deployed! Save changes and {0}")]
    ANewVersionHasJustBeenDeployedSaveChangesAnd0,
    OutdatedClientApplication,
    [Description("Looks like a new version has just been deployed! If you don't have changes that need to be saved, consider reloading")]
    ANewVersionHasJustBeenDeployedConsiderReload,
    Refresh,
}


public enum PaginationMessage
{
    All
}

public enum NormalControlMessage
{
    [Description("View for type {0} is not allowed")]
    ViewForType0IsNotAllowed,
    SaveChangesFirst,
    [Description("Copy Entity Type and Id (for autocomplete)")]
    CopyEntityTypeAndIdForAutocomplete,
    [Description("Copy Entity URL")]
    CopyEntityUrl
}

public enum SaveChangesMessage
{
    ThereAreChanges,
    YoureTryingToCloseAnEntityWithChanges,
    LoseChanges,
}

public enum CalendarMessage
{
    [Description("Today")]
    Today,
}

[AllowUnauthenticated]
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
    [Description("Show group")]
    ShowGroup,
    [Description("Acivate Time Machine")]
    activateTimeMachine,
    [Description("Deactivate Time Machine")]
    deactivateTimeMachine,
    [Description("Show Records")]
    showRecords,
    [Description("Join mode")]
    joinMode,
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
    [Description("Edit column")]
    editColumn,
    [Description("Remove column")]
    removeColumn,
    [Description("Group by this column")]
    groupByThisColumn,
    [Description("Remove other columns")]
    removeOtherColumns,
    [Description("Restore default columns")]
    restoreDefaultColumns,
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
    showPreviousOperation,

    [Description("Date")]
    Date,
}

//https://github.com/jquense/react-widgets/blob/5d4985c6dac0df34b86c7d8ad311ff97066977ab/packages/react-widgets/src/messages.tsx#L35
[AllowUnauthenticated]
public enum ReactWidgetsMessage
{
    [Description("Today")]
    MoveToday,

    [Description("Navigate back")]
    MoveBack,
    [Description("Navigate forward")]
    MoveForward,
    [Description("Select date")]
    DateButton,
    [Description("open combobox")]
    OpenCombobox,
    [Description("")]
    FilterPlaceholder,
    [Description("There are no items in this list")]
    EmptyList,
    [Description("The filter returned no results")]
    EmptyFilter,
    [Description("Create option")]
    CreateOption,
    [Description("Create option {0}")]
    CreateOption0,
    [Description("Selected items")]
    TagsLabel,
    [Description("Remove selected item")]
    RemoveLabel,
    [Description("no selected items")]
    NoneSelected,
    [Description("Selected items: {0}")]
    SelectedItems0,
    [Description("Increment value")]
    IncrementValue,
    [Description("Decrement value")]
    DecrementValue,
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

public enum ContainerToggleMessage
{
    Compress,
    Expand,
}


[AllowUnauthenticated]
public enum FontSizeMessage
{
    FontSize,
    ReduceFontSize,
    ResetFontSize,
    IncreaseFontSize,
}
