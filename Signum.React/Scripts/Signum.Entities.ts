//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
export interface ModifiableEntity {
    ToString?: string;
}

export interface IEntity {
    Type?: string;
    id?: any;
    isNew?: boolean;
    ticks?: number;
    ToString?: string;
}

export interface Entity extends ModifiableEntity, IEntity {
    mixins?: { [name: string]: MixinEntity }
}

export interface MixinEntity extends ModifiableEntity {
}

export type MList<T> = Array<MListElement<T>>;

export interface MListElement<T> {
    rowId?: any;
    element?: T;
}

export interface Lite<T> {
    entity?: T;
    _type?: string;
    id?: any;
    ToString?: string;
}

export type Type<T extends ModifiableEntity> = string;

export type ExecuteSymbol<T extends IEntity> = OperationSymbol;
export type DeleteSymbol<T extends IEntity> = OperationSymbol;
export type ConstructSymbol_Simple<T extends Entity> = OperationSymbol;
export type ConstructSymbol_From<T extends Entity, F extends IEntity> = OperationSymbol;
export type ConstructSymbol_FromMany<T extends Entity, F extends IEntity> = OperationSymbol; 
export module CalendarMessage {
    export const Today = "CalendarMessage.Today"
}

export module ConnectionMessage {
    export const AConnectionWithTheServerIsNecessaryToContinue = "ConnectionMessage.AConnectionWithTheServerIsNecessaryToContinue"
    export const SessionExpired = "ConnectionMessage.SessionExpired"
}

export interface CorruptMixin extends MixinEntity {
    corrupt?: boolean;
}

export interface EmbeddedEntity extends ModifiableEntity {
}

export module EngineMessage {
    export const ConcurrencyErrorOnDatabaseTable0Id1 = "EngineMessage.ConcurrencyErrorOnDatabaseTable0Id1"
    export const EntityWithType0AndId1NotFound = "EngineMessage.EntityWithType0AndId1NotFound"
    export const NoWayOfMappingType0Found = "EngineMessage.NoWayOfMappingType0Found"
    export const TheEntity0IsNew = "EngineMessage.TheEntity0IsNew"
    export const ThereAre0ThatReferThisEntity = "EngineMessage.ThereAre0ThatReferThisEntity"
    export const ThereAreRecordsIn0PointingToThisTableByColumn1 = "EngineMessage.ThereAreRecordsIn0PointingToThisTableByColumn1"
    export const UnauthorizedAccessTo0Because1 = "EngineMessage.UnauthorizedAccessTo0Because1"
    export const TheresAlreadyA0With1EqualsTo2_G = "EngineMessage.TheresAlreadyA0With1EqualsTo2_G"
}

export module EntityControlMessage {
    export const Create = "EntityControlMessage.Create"
    export const Find = "EntityControlMessage.Find"
    export const Detail = "EntityControlMessage.Detail"
    export const MoveDown = "EntityControlMessage.MoveDown"
    export const MoveUp = "EntityControlMessage.MoveUp"
    export const Navigate = "EntityControlMessage.Navigate"
    export const NullValueNotAllowed = "EntityControlMessage.NullValueNotAllowed"
    export const Remove = "EntityControlMessage.Remove"
    export const View = "EntityControlMessage.View"
}

export interface ImmutableEntity extends Entity {
    allowChange?: boolean;
}

export module JavascriptMessage {
    export const chooseAType = "JavascriptMessage.chooseAType"
    export const chooseAValue = "JavascriptMessage.chooseAValue"
    export const addFilter = "JavascriptMessage.addFilter"
    export const openTab = "JavascriptMessage.openTab"
    export const renameColumn = "JavascriptMessage.renameColumn"
    export const enterTheNewColumnName = "JavascriptMessage.enterTheNewColumnName"
    export const error = "JavascriptMessage.error"
    export const executed = "JavascriptMessage.executed"
    export const hideFilters = "JavascriptMessage.hideFilters"
    export const loading = "JavascriptMessage.loading"
    export const noActionsFound = "JavascriptMessage.noActionsFound"
    export const saveChangesBeforeOrPressCancel = "JavascriptMessage.saveChangesBeforeOrPressCancel"
    export const loseCurrentChanges = "JavascriptMessage.loseCurrentChanges"
    export const noElementsSelected = "JavascriptMessage.noElementsSelected"
    export const searchForResults = "JavascriptMessage.searchForResults"
    export const selectOnlyOneElement = "JavascriptMessage.selectOnlyOneElement"
    export const popupErrors = "JavascriptMessage.popupErrors"
    export const popupErrorsStop = "JavascriptMessage.popupErrorsStop"
    export const removeColumn = "JavascriptMessage.removeColumn"
    export const reorderColumn_MoveLeft = "JavascriptMessage.reorderColumn_MoveLeft"
    export const reorderColumn_MoveRight = "JavascriptMessage.reorderColumn_MoveRight"
    export const saved = "JavascriptMessage.saved"
    export const search = "JavascriptMessage.search"
    export const Selected = "JavascriptMessage.Selected"
    export const selectToken = "JavascriptMessage.selectToken"
    export const showFilters = "JavascriptMessage.showFilters"
    export const find = "JavascriptMessage.find"
    export const remove = "JavascriptMessage.remove"
    export const view = "JavascriptMessage.view"
    export const create = "JavascriptMessage.create"
    export const moveDown = "JavascriptMessage.moveDown"
    export const moveUp = "JavascriptMessage.moveUp"
    export const navigate = "JavascriptMessage.navigate"
    export const newEntity = "JavascriptMessage.newEntity"
    export const ok = "JavascriptMessage.ok"
    export const cancel = "JavascriptMessage.cancel"
}

export module LiteMessage {
    export const IdNotValid = "LiteMessage.IdNotValid"
    export const InvalidFormat = "LiteMessage.InvalidFormat"
    export const New_G = "LiteMessage.New_G"
    export const Type0NotFound = "LiteMessage.Type0NotFound"
    export const ToStr = "LiteMessage.ToStr"
}

export interface ModelEntity extends EmbeddedEntity {
}

export module NormalControlMessage {
    export const Save = "NormalControlMessage.Save"
    export const ViewForType0IsNotAllowed = "NormalControlMessage.ViewForType0IsNotAllowed"
}

export module NormalWindowMessage {
    export const _0Errors1 = "NormalWindowMessage._0Errors1"
    export const _1Error = "NormalWindowMessage._1Error"
    export const Cancel = "NormalWindowMessage.Cancel"
    export const ContinueAnyway = "NormalWindowMessage.ContinueAnyway"
    export const ContinueWithErrors = "NormalWindowMessage.ContinueWithErrors"
    export const FixErrors = "NormalWindowMessage.FixErrors"
    export const ImpossibleToSaveIntegrityCheckFailed = "NormalWindowMessage.ImpossibleToSaveIntegrityCheckFailed"
    export const Loading0 = "NormalWindowMessage.Loading0"
    export const LoseChanges = "NormalWindowMessage.LoseChanges"
    export const NoDirectErrors = "NormalWindowMessage.NoDirectErrors"
    export const Ok = "NormalWindowMessage.Ok"
    export const Reload = "NormalWindowMessage.Reload"
    export const The0HasErrors1 = "NormalWindowMessage.The0HasErrors1"
    export const ThereAreChanges = "NormalWindowMessage.ThereAreChanges"
    export const ThereAreChangesContinue = "NormalWindowMessage.ThereAreChangesContinue"
    export const ThereAreErrors = "NormalWindowMessage.ThereAreErrors"
    export const Message = "NormalWindowMessage.Message"
}

export module OperationMessage {
    export const Create = "OperationMessage.Create"
    export const CreateFromRegex = "OperationMessage.CreateFromRegex"
    export const StateShouldBe0InsteadOf1 = "OperationMessage.StateShouldBe0InsteadOf1"
    export const InUserInterface = "OperationMessage.InUserInterface"
    export const Operation01IsNotAuthorized = "OperationMessage.Operation01IsNotAuthorized"
    export const PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem = "OperationMessage.PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem"
    export const PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem = "OperationMessage.PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem"
    export const TheOperation0DidNotReturnAnEntity = "OperationMessage.TheOperation0DidNotReturnAnEntity"
    export const Logs = "OperationMessage.Logs"
}

export const OperationSymbol: Type<OperationSymbol> = "OperationSymbol";
export interface OperationSymbol extends Symbol {
}

export module PaginationMessage {
    export const All = "PaginationMessage.All"
}

export module QuickLinkMessage {
    export const Quicklinks = "QuickLinkMessage.Quicklinks"
    export const No0Found = "QuickLinkMessage.No0Found"
}

export module SearchMessage {
    export const ChooseTheDisplayNameOfTheNewColumn = "SearchMessage.ChooseTheDisplayNameOfTheNewColumn"
    export const Field = "SearchMessage.Field"
    export const AddColumn = "SearchMessage.AddColumn"
    export const AddFilter = "SearchMessage.AddFilter"
    export const DeleteFilter = "SearchMessage.DeleteFilter"
    export const Filters = "SearchMessage.Filters"
    export const Find = "SearchMessage.Find"
    export const FinderOf0 = "SearchMessage.FinderOf0"
    export const Name = "SearchMessage.Name"
    export const NewColumnSName = "SearchMessage.NewColumnSName"
    export const NoActionsFound = "SearchMessage.NoActionsFound"
    export const NoColumnSelected = "SearchMessage.NoColumnSelected"
    export const NoFiltersSpecified = "SearchMessage.NoFiltersSpecified"
    export const Of = "SearchMessage.Of"
    export const Operation = "SearchMessage.Operation"
    export const Query0IsNotAllowed = "SearchMessage.Query0IsNotAllowed"
    export const Query0NotAllowed = "SearchMessage.Query0NotAllowed"
    export const Query0NotRegistered = "SearchMessage.Query0NotRegistered"
    export const Rename = "SearchMessage.Rename"
    export const _0Results_N = "SearchMessage._0Results_N"
    export const First0Results_N = "SearchMessage.First0Results_N"
    export const _01of2Results_N = "SearchMessage._01of2Results_N"
    export const Search = "SearchMessage.Search"
    export const Create = "SearchMessage.Create"
    export const CreateNew0_G = "SearchMessage.CreateNew0_G"
    export const SearchControl_Pagination_All = "SearchMessage.SearchControl_Pagination_All"
    export const ThereIsNo0 = "SearchMessage.ThereIsNo0"
    export const Value = "SearchMessage.Value"
    export const View = "SearchMessage.View"
    export const ViewSelected = "SearchMessage.ViewSelected"
    export const Operations = "SearchMessage.Operations"
    export const NoResultsFound = "SearchMessage.NoResultsFound"
}

export module SelectorMessage {
    export const ConstructorSelector = "SelectorMessage.ConstructorSelector"
    export const PleaseChooseAValueToContinue = "SelectorMessage.PleaseChooseAValueToContinue"
    export const PleaseSelectAConstructor = "SelectorMessage.PleaseSelectAConstructor"
    export const PleaseSelectAType = "SelectorMessage.PleaseSelectAType"
    export const TypeSelector = "SelectorMessage.TypeSelector"
    export const ValueMustBeSpecifiedFor0 = "SelectorMessage.ValueMustBeSpecifiedFor0"
    export const ChooseAValue = "SelectorMessage.ChooseAValue"
    export const SelectAnElement = "SelectorMessage.SelectAnElement"
    export const PleaseSelectAnElement = "SelectorMessage.PleaseSelectAnElement"
}

export interface Symbol extends Entity {
    key?: string;
}

export module SynchronizerMessage {
    export const _0HasBeenRenamedIn1 = "SynchronizerMessage._0HasBeenRenamedIn1"
    export const EndOfSyncScript = "SynchronizerMessage.EndOfSyncScript"
    export const NNone = "SynchronizerMessage.NNone"
    export const StartOfSyncScriptGeneratedOn0 = "SynchronizerMessage.StartOfSyncScriptGeneratedOn0"
}

export module ValidationMessage {
    export const _0DoesNotHaveAValid1Format = "ValidationMessage._0DoesNotHaveAValid1Format"
    export const _0HasAnInvalidFormat = "ValidationMessage._0HasAnInvalidFormat"
    export const _0HasMoreThan1DecimalPlaces = "ValidationMessage._0HasMoreThan1DecimalPlaces"
    export const _0HasSomeRepeatedElements1 = "ValidationMessage._0HasSomeRepeatedElements1"
    export const _0ShouldBe12 = "ValidationMessage._0ShouldBe12"
    export const _0HasToBeBetween1And2 = "ValidationMessage._0HasToBeBetween1And2"
    export const _0HasToBeLowercase = "ValidationMessage._0HasToBeLowercase"
    export const _0HasToBeUppercase = "ValidationMessage._0HasToBeUppercase"
    export const _0IsNecessary = "ValidationMessage._0IsNecessary"
    export const _0IsNecessaryOnState1 = "ValidationMessage._0IsNecessaryOnState1"
    export const _0IsNotAllowed = "ValidationMessage._0IsNotAllowed"
    export const _0IsNotAllowedOnState1 = "ValidationMessage._0IsNotAllowedOnState1"
    export const _0IsNotSet = "ValidationMessage._0IsNotSet"
    export const _0IsSet = "ValidationMessage._0IsSet"
    export const _0IsNotA1_G = "ValidationMessage._0IsNotA1_G"
    export const BeA0_G = "ValidationMessage.BeA0_G"
    export const Be = "ValidationMessage.Be"
    export const BeBetween0And1 = "ValidationMessage.BeBetween0And1"
    export const BeNotNull = "ValidationMessage.BeNotNull"
    export const FileName = "ValidationMessage.FileName"
    export const Have0Decimals = "ValidationMessage.Have0Decimals"
    export const HaveANumberOfElements01 = "ValidationMessage.HaveANumberOfElements01"
    export const HaveAPrecisionOf = "ValidationMessage.HaveAPrecisionOf"
    export const HaveBetween0And1Characters = "ValidationMessage.HaveBetween0And1Characters"
    export const HaveMaximum0Characters = "ValidationMessage.HaveMaximum0Characters"
    export const HaveMinimum0Characters = "ValidationMessage.HaveMinimum0Characters"
    export const HaveNoRepeatedElements = "ValidationMessage.HaveNoRepeatedElements"
    export const HaveValid0Format = "ValidationMessage.HaveValid0Format"
    export const InvalidDateFormat = "ValidationMessage.InvalidDateFormat"
    export const InvalidFormat = "ValidationMessage.InvalidFormat"
    export const NotPossibleToaAssign0 = "ValidationMessage.NotPossibleToaAssign0"
    export const Numeric = "ValidationMessage.Numeric"
    export const OrBeNull = "ValidationMessage.OrBeNull"
    export const Telephone = "ValidationMessage.Telephone"
    export const _0ShouldNotHaveBreakLines = "ValidationMessage._0ShouldNotHaveBreakLines"
    export const _0ShouldNotHaveInitialSpaces = "ValidationMessage._0ShouldNotHaveInitialSpaces"
    export const _0ShouldNotHaveFinalSpaces = "ValidationMessage._0ShouldNotHaveFinalSpaces"
    export const TheLenghtOf0HasToBeEqualTo1 = "ValidationMessage.TheLenghtOf0HasToBeEqualTo1"
    export const TheLengthOf0HasToBeGreaterOrEqualTo1 = "ValidationMessage.TheLengthOf0HasToBeGreaterOrEqualTo1"
    export const TheLengthOf0HasToBeLesserOrEqualTo1 = "ValidationMessage.TheLengthOf0HasToBeLesserOrEqualTo1"
    export const TheNumberOf0IsBeingMultipliedBy1 = "ValidationMessage.TheNumberOf0IsBeingMultipliedBy1"
    export const TheNumberOfElementsOf0HasToBe12 = "ValidationMessage.TheNumberOfElementsOf0HasToBe12"
    export const Type0NotAllowed = "ValidationMessage.Type0NotAllowed"
    export const _0IsMandatoryWhen1IsNotSet = "ValidationMessage._0IsMandatoryWhen1IsNotSet"
    export const _0IsMandatoryWhen1IsSet = "ValidationMessage._0IsMandatoryWhen1IsSet"
    export const _0ShouldBeNullWhen1IsNotSet = "ValidationMessage._0ShouldBeNullWhen1IsNotSet"
    export const _0ShouldBeNullWhen1IsSet = "ValidationMessage._0ShouldBeNullWhen1IsSet"
    export const _0ShouldBeNull = "ValidationMessage._0ShouldBeNull"
    export const _0ShouldBeADateInThePast = "ValidationMessage._0ShouldBeADateInThePast"
    export const BeInThePast = "ValidationMessage.BeInThePast"
    export const _0ShouldBeGreaterThan1 = "ValidationMessage._0ShouldBeGreaterThan1"
    export const _0HasAPrecissionOf1InsteadOf2 = "ValidationMessage._0HasAPrecissionOf1InsteadOf2"
}

export module VoidEnumMessage {
    export const Instance = "VoidEnumMessage.Instance"
}

export namespace Basics {

    export const ColorEntity: Type<ColorEntity> = "ColorEntity";
    export interface ColorEntity extends EmbeddedEntity {
        argb?: number;
    }
    
    export const DeleteLogParametersEntity: Type<DeleteLogParametersEntity> = "DeleteLogParametersEntity";
    export interface DeleteLogParametersEntity extends EmbeddedEntity {
        deleteLogsWithMoreThan?: number;
        dateLimit?: string;
        chunkSize?: number;
        maxChunks?: number;
    }
    
    export const ExceptionEntity: Type<ExceptionEntity> = "ExceptionEntity";
    export interface ExceptionEntity extends Entity {
        creationDate?: string;
        exceptionType?: string;
        exceptionMessage?: string;
        exceptionMessageHash?: number;
        stackTrace?: string;
        stackTraceHash?: number;
        threadId?: number;
        user?: Lite<IUserEntity>;
        environment?: string;
        version?: string;
        userAgent?: string;
        requestUrl?: string;
        controllerName?: string;
        actionName?: string;
        urlReferer?: string;
        machineName?: string;
        applicationName?: string;
        userHostAddress?: string;
        userHostName?: string;
        form?: string;
        queryString?: string;
        session?: string;
        data?: string;
        referenced?: boolean;
    }
    
    export interface IUserEntity extends IEntity {
    }
    
    export const OperationLogEntity: Type<OperationLogEntity> = "OperationLogEntity";
    export interface OperationLogEntity extends Entity {
        target?: Lite<IEntity>;
        origin?: Lite<IEntity>;
        operation?: OperationSymbol;
        user?: Lite<IUserEntity>;
        start?: string;
        end?: string;
        exception?: Lite<ExceptionEntity>;
    }
    
    export interface SemiSymbol extends Entity {
        key?: string;
        name?: string;
    }
    
    export const TypeEntity: Type<TypeEntity> = "TypeEntity";
    export interface TypeEntity extends Entity {
        fullClassName?: string;
        cleanName?: string;
        tableName?: string;
        namespace?: string;
        className?: string;
    }
    
}

export namespace DynamicQuery {

    export enum ColumnOptionsMode {
        Add,
        Remove,
        Replace,
    }
    
    export enum FilterOperation {
        EqualTo,
        DistinctTo,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        Like,
        NotContains,
        NotStartsWith,
        NotEndsWith,
        NotLike,
        IsIn,
    }
    
    export enum FilterType {
        Integer,
        Decimal,
        String,
        DateTime,
        Lite,
        Embedded,
        Boolean,
        Enum,
        Guid,
    }
    
    export enum OrderType {
        Ascending,
        Descending,
    }
    
    export enum PaginationMode {
        All,
        Firsts,
        Paginate,
    }
    
    export module QueryTokenMessage {
        export const _0As1 = "QueryTokenMessage._0As1"
        export const And = "QueryTokenMessage.And"
        export const AnyEntity = "QueryTokenMessage.AnyEntity"
        export const As0 = "QueryTokenMessage.As0"
        export const Check = "QueryTokenMessage.Check"
        export const Column0NotFound = "QueryTokenMessage.Column0NotFound"
        export const Count = "QueryTokenMessage.Count"
        export const Date = "QueryTokenMessage.Date"
        export const DateTime = "QueryTokenMessage.DateTime"
        export const Day = "QueryTokenMessage.Day"
        export const DayOfWeek = "QueryTokenMessage.DayOfWeek"
        export const DayOfYear = "QueryTokenMessage.DayOfYear"
        export const DecimalNumber = "QueryTokenMessage.DecimalNumber"
        export const Embedded0 = "QueryTokenMessage.Embedded0"
        export const GlobalUniqueIdentifier = "QueryTokenMessage.GlobalUniqueIdentifier"
        export const Hour = "QueryTokenMessage.Hour"
        export const ListOf0 = "QueryTokenMessage.ListOf0"
        export const Millisecond = "QueryTokenMessage.Millisecond"
        export const Minute = "QueryTokenMessage.Minute"
        export const Month = "QueryTokenMessage.Month"
        export const MonthStart = "QueryTokenMessage.MonthStart"
        export const MoreThanOneColumnNamed0 = "QueryTokenMessage.MoreThanOneColumnNamed0"
        export const Number = "QueryTokenMessage.Number"
        export const Of = "QueryTokenMessage.Of"
        export const Second = "QueryTokenMessage.Second"
        export const Text = "QueryTokenMessage.Text"
        export const Year = "QueryTokenMessage.Year"
        export const WeekNumber = "QueryTokenMessage.WeekNumber"
        export const _0Steps1 = "QueryTokenMessage._0Steps1"
        export const Step0 = "QueryTokenMessage.Step0"
    }
    
    export enum UniqueType {
        First,
        FirstOrDefault,
        Single,
        SingleOrDefault,
        SingleOrMany,
        Only,
    }
    
}

export namespace Patterns {

    export module EntityMessage {
        export const AttemptToSet0InLockedEntity1 = "EntityMessage.AttemptToSet0InLockedEntity1"
        export const AttemptToAddRemove0InLockedEntity1 = "EntityMessage.AttemptToAddRemove0InLockedEntity1"
    }
    
    export interface LockableEntity extends Entity {
        locked?: boolean;
    }
    
}

