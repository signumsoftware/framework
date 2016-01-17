//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////
import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from 'Framework/Signum.React/Scripts/Reflection' 

export interface ModifiableEntity {
    Type: string;
    toStr?: string;
}

export interface IEntity {
    Type: string;
    id?: any;
    isNew?: boolean;
    ticks?: number;
    toStr?: string;
}

export interface Entity extends ModifiableEntity, IEntity {
    mixins?: { [name: string]: MixinEntity }
}

export interface MixinEntity extends ModifiableEntity {
}

export function getMixin<M extends MixinEntity>(entity: Entity, type: Type<M>) {
    return entity.mixins[type.typeName] as M;
}

export type MList<T> = Array<MListElement<T>>;

export interface MListElement<T> {
    rowId?: any;
    element?: T;
}

export interface Lite<T extends IEntity> {
    entity?: T;
    EntityType: string;
    id?: any;
    toStr?: string;
}

export type Type<T extends ModifiableEntity> = string;

export type ExecuteSymbol<T extends IEntity> = OperationSymbol;
export type DeleteSymbol<T extends IEntity> = OperationSymbol;
export type ConstructSymbol_Simple<T extends Entity> = OperationSymbol;
export type ConstructSymbol_From<T extends Entity, F extends IEntity> = OperationSymbol;
export type ConstructSymbol_FromMany<T extends Entity, F extends IEntity> = OperationSymbol; 

export function toLite<T extends IEntity>(entity: T, fat?: boolean) : Lite<T> {
    if(fat)
       return toLiteFat(entity);


    return {
       EntityType : entity.Type,
       id :entity.id,
       toStr :entity.toStr,
    }
}

export function toLiteFat<T extends IEntity>(entity: T) : Lite<T> {
    return {
       entity : entity,
       EntityType  :entity.Type,
       id :entity.id,
       toStr :entity.toStr,
    }
}

export function liteKey(lite: Lite<IEntity>) {
    return lite.EntityType + ";" + (lite.id || "");
}

export function parseLite(lite: string) : Lite<IEntity> {
    return {
        EntityType: lite.before(";"),
        id :  lite.after(";"),
    };
}

import { getTypeInfo } from 'Framework/Signum.React/Scripts/Reflection' 
export function is<T extends IEntity>(a: Lite<T> | T, b: Lite<T> | T) {

    if (a.id != b.id)
        return false;

    var aType = getTypeInfo((a as T).Type || (a as Lite<T>).EntityType);
    var bType = getTypeInfo((a as T).Type || (a as Lite<T>).EntityType);

    return aType == bType;
}


export enum BooleanEnum {
    False = "False" as any,
    True = "True" as any,
}
export const BooleanEnum_Type = new EnumType<BooleanEnum>("BooleanEnum", BooleanEnum);

export module CalendarMessage {
    export const Today = new MessageKey("CalendarMessage", "Today");
}

export module ConnectionMessage {
    export const AConnectionWithTheServerIsNecessaryToContinue = new MessageKey("ConnectionMessage", "AConnectionWithTheServerIsNecessaryToContinue");
    export const SessionExpired = new MessageKey("ConnectionMessage", "SessionExpired");
}

export const CorruptMixin_Type = new Type<CorruptMixin>("CorruptMixin");
export interface CorruptMixin extends MixinEntity {
    corrupt?: boolean;
}

export interface EmbeddedEntity extends ModifiableEntity {
}

export module EngineMessage {
    export const ConcurrencyErrorOnDatabaseTable0Id1 = new MessageKey("EngineMessage", "ConcurrencyErrorOnDatabaseTable0Id1");
    export const EntityWithType0AndId1NotFound = new MessageKey("EngineMessage", "EntityWithType0AndId1NotFound");
    export const NoWayOfMappingType0Found = new MessageKey("EngineMessage", "NoWayOfMappingType0Found");
    export const TheEntity0IsNew = new MessageKey("EngineMessage", "TheEntity0IsNew");
    export const ThereAre0ThatReferThisEntity = new MessageKey("EngineMessage", "ThereAre0ThatReferThisEntity");
    export const ThereAreRecordsIn0PointingToThisTableByColumn1 = new MessageKey("EngineMessage", "ThereAreRecordsIn0PointingToThisTableByColumn1");
    export const UnauthorizedAccessTo0Because1 = new MessageKey("EngineMessage", "UnauthorizedAccessTo0Because1");
    export const TheresAlreadyA0With1EqualsTo2_G = new MessageKey("EngineMessage", "TheresAlreadyA0With1EqualsTo2_G");
}

export module EntityControlMessage {
    export const Create = new MessageKey("EntityControlMessage", "Create");
    export const Find = new MessageKey("EntityControlMessage", "Find");
    export const Detail = new MessageKey("EntityControlMessage", "Detail");
    export const MoveDown = new MessageKey("EntityControlMessage", "MoveDown");
    export const MoveUp = new MessageKey("EntityControlMessage", "MoveUp");
    export const Navigate = new MessageKey("EntityControlMessage", "Navigate");
    export const NullValueNotAllowed = new MessageKey("EntityControlMessage", "NullValueNotAllowed");
    export const Remove = new MessageKey("EntityControlMessage", "Remove");
    export const View = new MessageKey("EntityControlMessage", "View");
}

export interface ImmutableEntity extends Entity {
    allowChange?: boolean;
}

export module JavascriptMessage {
    export const chooseAType = new MessageKey("JavascriptMessage", "chooseAType");
    export const chooseAValue = new MessageKey("JavascriptMessage", "chooseAValue");
    export const addFilter = new MessageKey("JavascriptMessage", "addFilter");
    export const openTab = new MessageKey("JavascriptMessage", "openTab");
    export const renameColumn = new MessageKey("JavascriptMessage", "renameColumn");
    export const enterTheNewColumnName = new MessageKey("JavascriptMessage", "enterTheNewColumnName");
    export const error = new MessageKey("JavascriptMessage", "error");
    export const executed = new MessageKey("JavascriptMessage", "executed");
    export const hideFilters = new MessageKey("JavascriptMessage", "hideFilters");
    export const loading = new MessageKey("JavascriptMessage", "loading");
    export const noActionsFound = new MessageKey("JavascriptMessage", "noActionsFound");
    export const saveChangesBeforeOrPressCancel = new MessageKey("JavascriptMessage", "saveChangesBeforeOrPressCancel");
    export const loseCurrentChanges = new MessageKey("JavascriptMessage", "loseCurrentChanges");
    export const noElementsSelected = new MessageKey("JavascriptMessage", "noElementsSelected");
    export const searchForResults = new MessageKey("JavascriptMessage", "searchForResults");
    export const selectOnlyOneElement = new MessageKey("JavascriptMessage", "selectOnlyOneElement");
    export const popupErrors = new MessageKey("JavascriptMessage", "popupErrors");
    export const popupErrorsStop = new MessageKey("JavascriptMessage", "popupErrorsStop");
    export const removeColumn = new MessageKey("JavascriptMessage", "removeColumn");
    export const reorderColumn_MoveLeft = new MessageKey("JavascriptMessage", "reorderColumn_MoveLeft");
    export const reorderColumn_MoveRight = new MessageKey("JavascriptMessage", "reorderColumn_MoveRight");
    export const saved = new MessageKey("JavascriptMessage", "saved");
    export const search = new MessageKey("JavascriptMessage", "search");
    export const Selected = new MessageKey("JavascriptMessage", "Selected");
    export const selectToken = new MessageKey("JavascriptMessage", "selectToken");
    export const showFilters = new MessageKey("JavascriptMessage", "showFilters");
    export const find = new MessageKey("JavascriptMessage", "find");
    export const remove = new MessageKey("JavascriptMessage", "remove");
    export const view = new MessageKey("JavascriptMessage", "view");
    export const create = new MessageKey("JavascriptMessage", "create");
    export const moveDown = new MessageKey("JavascriptMessage", "moveDown");
    export const moveUp = new MessageKey("JavascriptMessage", "moveUp");
    export const navigate = new MessageKey("JavascriptMessage", "navigate");
    export const newEntity = new MessageKey("JavascriptMessage", "newEntity");
    export const ok = new MessageKey("JavascriptMessage", "ok");
    export const cancel = new MessageKey("JavascriptMessage", "cancel");
}

export module LiteMessage {
    export const IdNotValid = new MessageKey("LiteMessage", "IdNotValid");
    export const InvalidFormat = new MessageKey("LiteMessage", "InvalidFormat");
    export const New_G = new MessageKey("LiteMessage", "New_G");
    export const Type0NotFound = new MessageKey("LiteMessage", "Type0NotFound");
    export const ToStr = new MessageKey("LiteMessage", "ToStr");
}

export interface ModelEntity extends EmbeddedEntity {
}

export module NormalControlMessage {
    export const Save = new MessageKey("NormalControlMessage", "Save");
    export const ViewForType0IsNotAllowed = new MessageKey("NormalControlMessage", "ViewForType0IsNotAllowed");
}

export module NormalWindowMessage {
    export const _0Errors1 = new MessageKey("NormalWindowMessage", "_0Errors1");
    export const _1Error = new MessageKey("NormalWindowMessage", "_1Error");
    export const Cancel = new MessageKey("NormalWindowMessage", "Cancel");
    export const ContinueAnyway = new MessageKey("NormalWindowMessage", "ContinueAnyway");
    export const ContinueWithErrors = new MessageKey("NormalWindowMessage", "ContinueWithErrors");
    export const FixErrors = new MessageKey("NormalWindowMessage", "FixErrors");
    export const ImpossibleToSaveIntegrityCheckFailed = new MessageKey("NormalWindowMessage", "ImpossibleToSaveIntegrityCheckFailed");
    export const Loading0 = new MessageKey("NormalWindowMessage", "Loading0");
    export const LoseChanges = new MessageKey("NormalWindowMessage", "LoseChanges");
    export const NoDirectErrors = new MessageKey("NormalWindowMessage", "NoDirectErrors");
    export const Ok = new MessageKey("NormalWindowMessage", "Ok");
    export const Reload = new MessageKey("NormalWindowMessage", "Reload");
    export const The0HasErrors1 = new MessageKey("NormalWindowMessage", "The0HasErrors1");
    export const ThereAreChanges = new MessageKey("NormalWindowMessage", "ThereAreChanges");
    export const ThereAreChangesContinue = new MessageKey("NormalWindowMessage", "ThereAreChangesContinue");
    export const ThereAreErrors = new MessageKey("NormalWindowMessage", "ThereAreErrors");
    export const Message = new MessageKey("NormalWindowMessage", "Message");
}

export module OperationMessage {
    export const Create = new MessageKey("OperationMessage", "Create");
    export const CreateFromRegex = new MessageKey("OperationMessage", "CreateFromRegex");
    export const StateShouldBe0InsteadOf1 = new MessageKey("OperationMessage", "StateShouldBe0InsteadOf1");
    export const InUserInterface = new MessageKey("OperationMessage", "InUserInterface");
    export const Operation01IsNotAuthorized = new MessageKey("OperationMessage", "Operation01IsNotAuthorized");
    export const PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem = new MessageKey("OperationMessage", "PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem");
    export const PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem = new MessageKey("OperationMessage", "PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem");
    export const TheOperation0DidNotReturnAnEntity = new MessageKey("OperationMessage", "TheOperation0DidNotReturnAnEntity");
    export const Logs = new MessageKey("OperationMessage", "Logs");
}

export const OperationSymbol_Type = new Type<OperationSymbol>("Operation");
export interface OperationSymbol extends Symbol {
}

export module PaginationMessage {
    export const All = new MessageKey("PaginationMessage", "All");
}

export module QuickLinkMessage {
    export const Quicklinks = new MessageKey("QuickLinkMessage", "Quicklinks");
    export const No0Found = new MessageKey("QuickLinkMessage", "No0Found");
}

export module SearchMessage {
    export const ChooseTheDisplayNameOfTheNewColumn = new MessageKey("SearchMessage", "ChooseTheDisplayNameOfTheNewColumn");
    export const Field = new MessageKey("SearchMessage", "Field");
    export const AddColumn = new MessageKey("SearchMessage", "AddColumn");
    export const AddFilter = new MessageKey("SearchMessage", "AddFilter");
    export const DeleteFilter = new MessageKey("SearchMessage", "DeleteFilter");
    export const Filters = new MessageKey("SearchMessage", "Filters");
    export const Find = new MessageKey("SearchMessage", "Find");
    export const FinderOf0 = new MessageKey("SearchMessage", "FinderOf0");
    export const Name = new MessageKey("SearchMessage", "Name");
    export const NewColumnSName = new MessageKey("SearchMessage", "NewColumnSName");
    export const NoActionsFound = new MessageKey("SearchMessage", "NoActionsFound");
    export const NoColumnSelected = new MessageKey("SearchMessage", "NoColumnSelected");
    export const NoFiltersSpecified = new MessageKey("SearchMessage", "NoFiltersSpecified");
    export const Of = new MessageKey("SearchMessage", "Of");
    export const Operation = new MessageKey("SearchMessage", "Operation");
    export const Query0IsNotAllowed = new MessageKey("SearchMessage", "Query0IsNotAllowed");
    export const Query0NotAllowed = new MessageKey("SearchMessage", "Query0NotAllowed");
    export const Query0NotRegistered = new MessageKey("SearchMessage", "Query0NotRegistered");
    export const Rename = new MessageKey("SearchMessage", "Rename");
    export const _0Results_N = new MessageKey("SearchMessage", "_0Results_N");
    export const First0Results_N = new MessageKey("SearchMessage", "First0Results_N");
    export const _01of2Results_N = new MessageKey("SearchMessage", "_01of2Results_N");
    export const Search = new MessageKey("SearchMessage", "Search");
    export const Create = new MessageKey("SearchMessage", "Create");
    export const CreateNew0_G = new MessageKey("SearchMessage", "CreateNew0_G");
    export const SearchControl_Pagination_All = new MessageKey("SearchMessage", "SearchControl_Pagination_All");
    export const ThereIsNo0 = new MessageKey("SearchMessage", "ThereIsNo0");
    export const Value = new MessageKey("SearchMessage", "Value");
    export const View = new MessageKey("SearchMessage", "View");
    export const ViewSelected = new MessageKey("SearchMessage", "ViewSelected");
    export const Operations = new MessageKey("SearchMessage", "Operations");
    export const NoResultsFound = new MessageKey("SearchMessage", "NoResultsFound");
}

export module SelectorMessage {
    export const ConstructorSelector = new MessageKey("SelectorMessage", "ConstructorSelector");
    export const PleaseChooseAValueToContinue = new MessageKey("SelectorMessage", "PleaseChooseAValueToContinue");
    export const PleaseSelectAConstructor = new MessageKey("SelectorMessage", "PleaseSelectAConstructor");
    export const PleaseSelectAType = new MessageKey("SelectorMessage", "PleaseSelectAType");
    export const TypeSelector = new MessageKey("SelectorMessage", "TypeSelector");
    export const ValueMustBeSpecifiedFor0 = new MessageKey("SelectorMessage", "ValueMustBeSpecifiedFor0");
    export const ChooseAValue = new MessageKey("SelectorMessage", "ChooseAValue");
    export const SelectAnElement = new MessageKey("SelectorMessage", "SelectAnElement");
    export const PleaseSelectAnElement = new MessageKey("SelectorMessage", "PleaseSelectAnElement");
}

export interface Symbol extends Entity {
    key?: string;
}

export module SynchronizerMessage {
    export const _0HasBeenRenamedIn1 = new MessageKey("SynchronizerMessage", "_0HasBeenRenamedIn1");
    export const EndOfSyncScript = new MessageKey("SynchronizerMessage", "EndOfSyncScript");
    export const NNone = new MessageKey("SynchronizerMessage", "NNone");
    export const StartOfSyncScriptGeneratedOn0 = new MessageKey("SynchronizerMessage", "StartOfSyncScriptGeneratedOn0");
}

export module ValidationMessage {
    export const _0DoesNotHaveAValid1Format = new MessageKey("ValidationMessage", "_0DoesNotHaveAValid1Format");
    export const _0HasAnInvalidFormat = new MessageKey("ValidationMessage", "_0HasAnInvalidFormat");
    export const _0HasMoreThan1DecimalPlaces = new MessageKey("ValidationMessage", "_0HasMoreThan1DecimalPlaces");
    export const _0HasSomeRepeatedElements1 = new MessageKey("ValidationMessage", "_0HasSomeRepeatedElements1");
    export const _0ShouldBe12 = new MessageKey("ValidationMessage", "_0ShouldBe12");
    export const _0HasToBeBetween1And2 = new MessageKey("ValidationMessage", "_0HasToBeBetween1And2");
    export const _0HasToBeLowercase = new MessageKey("ValidationMessage", "_0HasToBeLowercase");
    export const _0HasToBeUppercase = new MessageKey("ValidationMessage", "_0HasToBeUppercase");
    export const _0IsNecessary = new MessageKey("ValidationMessage", "_0IsNecessary");
    export const _0IsNecessaryOnState1 = new MessageKey("ValidationMessage", "_0IsNecessaryOnState1");
    export const _0IsNotAllowed = new MessageKey("ValidationMessage", "_0IsNotAllowed");
    export const _0IsNotAllowedOnState1 = new MessageKey("ValidationMessage", "_0IsNotAllowedOnState1");
    export const _0IsNotSet = new MessageKey("ValidationMessage", "_0IsNotSet");
    export const _0IsSet = new MessageKey("ValidationMessage", "_0IsSet");
    export const _0IsNotA1_G = new MessageKey("ValidationMessage", "_0IsNotA1_G");
    export const BeA0_G = new MessageKey("ValidationMessage", "BeA0_G");
    export const Be = new MessageKey("ValidationMessage", "Be");
    export const BeBetween0And1 = new MessageKey("ValidationMessage", "BeBetween0And1");
    export const BeNotNull = new MessageKey("ValidationMessage", "BeNotNull");
    export const FileName = new MessageKey("ValidationMessage", "FileName");
    export const Have0Decimals = new MessageKey("ValidationMessage", "Have0Decimals");
    export const HaveANumberOfElements01 = new MessageKey("ValidationMessage", "HaveANumberOfElements01");
    export const HaveAPrecisionOf = new MessageKey("ValidationMessage", "HaveAPrecisionOf");
    export const HaveBetween0And1Characters = new MessageKey("ValidationMessage", "HaveBetween0And1Characters");
    export const HaveMaximum0Characters = new MessageKey("ValidationMessage", "HaveMaximum0Characters");
    export const HaveMinimum0Characters = new MessageKey("ValidationMessage", "HaveMinimum0Characters");
    export const HaveNoRepeatedElements = new MessageKey("ValidationMessage", "HaveNoRepeatedElements");
    export const HaveValid0Format = new MessageKey("ValidationMessage", "HaveValid0Format");
    export const InvalidDateFormat = new MessageKey("ValidationMessage", "InvalidDateFormat");
    export const InvalidFormat = new MessageKey("ValidationMessage", "InvalidFormat");
    export const NotPossibleToaAssign0 = new MessageKey("ValidationMessage", "NotPossibleToaAssign0");
    export const Numeric = new MessageKey("ValidationMessage", "Numeric");
    export const OrBeNull = new MessageKey("ValidationMessage", "OrBeNull");
    export const Telephone = new MessageKey("ValidationMessage", "Telephone");
    export const _0ShouldNotHaveBreakLines = new MessageKey("ValidationMessage", "_0ShouldNotHaveBreakLines");
    export const _0ShouldNotHaveInitialSpaces = new MessageKey("ValidationMessage", "_0ShouldNotHaveInitialSpaces");
    export const _0ShouldNotHaveFinalSpaces = new MessageKey("ValidationMessage", "_0ShouldNotHaveFinalSpaces");
    export const TheLenghtOf0HasToBeEqualTo1 = new MessageKey("ValidationMessage", "TheLenghtOf0HasToBeEqualTo1");
    export const TheLengthOf0HasToBeGreaterOrEqualTo1 = new MessageKey("ValidationMessage", "TheLengthOf0HasToBeGreaterOrEqualTo1");
    export const TheLengthOf0HasToBeLesserOrEqualTo1 = new MessageKey("ValidationMessage", "TheLengthOf0HasToBeLesserOrEqualTo1");
    export const TheNumberOf0IsBeingMultipliedBy1 = new MessageKey("ValidationMessage", "TheNumberOf0IsBeingMultipliedBy1");
    export const TheNumberOfElementsOf0HasToBe12 = new MessageKey("ValidationMessage", "TheNumberOfElementsOf0HasToBe12");
    export const Type0NotAllowed = new MessageKey("ValidationMessage", "Type0NotAllowed");
    export const _0IsMandatoryWhen1IsNotSet = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsNotSet");
    export const _0IsMandatoryWhen1IsSet = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsSet");
    export const _0ShouldBeNullWhen1IsNotSet = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsNotSet");
    export const _0ShouldBeNullWhen1IsSet = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsSet");
    export const _0ShouldBeNull = new MessageKey("ValidationMessage", "_0ShouldBeNull");
    export const _0ShouldBeADateInThePast = new MessageKey("ValidationMessage", "_0ShouldBeADateInThePast");
    export const BeInThePast = new MessageKey("ValidationMessage", "BeInThePast");
    export const _0ShouldBeGreaterThan1 = new MessageKey("ValidationMessage", "_0ShouldBeGreaterThan1");
    export const _0HasAPrecissionOf1InsteadOf2 = new MessageKey("ValidationMessage", "_0HasAPrecissionOf1InsteadOf2");
}

export module VoidEnumMessage {
    export const Instance = new MessageKey("VoidEnumMessage", "Instance");
}

export namespace Basics {

    export const ColorEntity_Type = new Type<ColorEntity>("Color");
    export interface ColorEntity extends EmbeddedEntity {
        argb?: number;
    }
    
    export const DeleteLogParametersEntity_Type = new Type<DeleteLogParametersEntity>("DeleteLogParameters");
    export interface DeleteLogParametersEntity extends EmbeddedEntity {
        deleteLogsWithMoreThan?: number;
        dateLimit?: string;
        chunkSize?: number;
        maxChunks?: number;
    }
    
    export const ExceptionEntity_Type = new Type<ExceptionEntity>("Exception");
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
    
    export const OperationLogEntity_Type = new Type<OperationLogEntity>("OperationLog");
    export interface OperationLogEntity extends Entity {
        target?: Lite<IEntity>;
        origin?: Lite<IEntity>;
        operation?: OperationSymbol;
        user?: Lite<IUserEntity>;
        start?: string;
        end?: string;
        exception?: Lite<ExceptionEntity>;
    }
    
    export const PropertyRouteEntity_Type = new Type<PropertyRouteEntity>("PropertyRoute");
    export interface PropertyRouteEntity extends Entity {
        path?: string;
        rootType?: TypeEntity;
    }
    
    export const QueryEntity_Type = new Type<QueryEntity>("Query");
    export interface QueryEntity extends Entity {
        key?: string;
    }
    
    export interface SemiSymbol extends Entity {
        key?: string;
        name?: string;
    }
    
    export const TypeEntity_Type = new Type<TypeEntity>("Type");
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
        Add = "Add" as any,
        Remove = "Remove" as any,
        Replace = "Replace" as any,
    }
    export const ColumnOptionsMode_Type = new EnumType<ColumnOptionsMode>("ColumnOptionsMode", ColumnOptionsMode);
    
    export enum FilterOperation {
        EqualTo = "EqualTo" as any,
        DistinctTo = "DistinctTo" as any,
        GreaterThan = "GreaterThan" as any,
        GreaterThanOrEqual = "GreaterThanOrEqual" as any,
        LessThan = "LessThan" as any,
        LessThanOrEqual = "LessThanOrEqual" as any,
        Contains = "Contains" as any,
        StartsWith = "StartsWith" as any,
        EndsWith = "EndsWith" as any,
        Like = "Like" as any,
        NotContains = "NotContains" as any,
        NotStartsWith = "NotStartsWith" as any,
        NotEndsWith = "NotEndsWith" as any,
        NotLike = "NotLike" as any,
        IsIn = "IsIn" as any,
    }
    export const FilterOperation_Type = new EnumType<FilterOperation>("FilterOperation", FilterOperation);
    
    export enum FilterType {
        Integer = "Integer" as any,
        Decimal = "Decimal" as any,
        String = "String" as any,
        DateTime = "DateTime" as any,
        Lite = "Lite" as any,
        Embedded = "Embedded" as any,
        Boolean = "Boolean" as any,
        Enum = "Enum" as any,
        Guid = "Guid" as any,
    }
    export const FilterType_Type = new EnumType<FilterType>("FilterType", FilterType);
    
    export enum OrderType {
        Ascending = "Ascending" as any,
        Descending = "Descending" as any,
    }
    export const OrderType_Type = new EnumType<OrderType>("OrderType", OrderType);
    
    export enum PaginationMode {
        All = "All" as any,
        Firsts = "Firsts" as any,
        Paginate = "Paginate" as any,
    }
    export const PaginationMode_Type = new EnumType<PaginationMode>("PaginationMode", PaginationMode);
    
    export module QueryTokenMessage {
        export const _0As1 = new MessageKey("QueryTokenMessage", "_0As1");
        export const And = new MessageKey("QueryTokenMessage", "And");
        export const AnyEntity = new MessageKey("QueryTokenMessage", "AnyEntity");
        export const As0 = new MessageKey("QueryTokenMessage", "As0");
        export const Check = new MessageKey("QueryTokenMessage", "Check");
        export const Column0NotFound = new MessageKey("QueryTokenMessage", "Column0NotFound");
        export const Count = new MessageKey("QueryTokenMessage", "Count");
        export const Date = new MessageKey("QueryTokenMessage", "Date");
        export const DateTime = new MessageKey("QueryTokenMessage", "DateTime");
        export const Day = new MessageKey("QueryTokenMessage", "Day");
        export const DayOfWeek = new MessageKey("QueryTokenMessage", "DayOfWeek");
        export const DayOfYear = new MessageKey("QueryTokenMessage", "DayOfYear");
        export const DecimalNumber = new MessageKey("QueryTokenMessage", "DecimalNumber");
        export const Embedded0 = new MessageKey("QueryTokenMessage", "Embedded0");
        export const GlobalUniqueIdentifier = new MessageKey("QueryTokenMessage", "GlobalUniqueIdentifier");
        export const Hour = new MessageKey("QueryTokenMessage", "Hour");
        export const ListOf0 = new MessageKey("QueryTokenMessage", "ListOf0");
        export const Millisecond = new MessageKey("QueryTokenMessage", "Millisecond");
        export const Minute = new MessageKey("QueryTokenMessage", "Minute");
        export const Month = new MessageKey("QueryTokenMessage", "Month");
        export const MonthStart = new MessageKey("QueryTokenMessage", "MonthStart");
        export const MoreThanOneColumnNamed0 = new MessageKey("QueryTokenMessage", "MoreThanOneColumnNamed0");
        export const Number = new MessageKey("QueryTokenMessage", "Number");
        export const Of = new MessageKey("QueryTokenMessage", "Of");
        export const Second = new MessageKey("QueryTokenMessage", "Second");
        export const Text = new MessageKey("QueryTokenMessage", "Text");
        export const Year = new MessageKey("QueryTokenMessage", "Year");
        export const WeekNumber = new MessageKey("QueryTokenMessage", "WeekNumber");
        export const _0Steps1 = new MessageKey("QueryTokenMessage", "_0Steps1");
        export const Step0 = new MessageKey("QueryTokenMessage", "Step0");
    }
    
    export enum UniqueType {
        First = "First" as any,
        FirstOrDefault = "FirstOrDefault" as any,
        Single = "Single" as any,
        SingleOrDefault = "SingleOrDefault" as any,
        SingleOrMany = "SingleOrMany" as any,
        Only = "Only" as any,
    }
    export const UniqueType_Type = new EnumType<UniqueType>("UniqueType", UniqueType);
    
}

export namespace External {

    export enum DayOfWeek {
        Sunday = "Sunday" as any,
        Monday = "Monday" as any,
        Tuesday = "Tuesday" as any,
        Wednesday = "Wednesday" as any,
        Thursday = "Thursday" as any,
        Friday = "Friday" as any,
        Saturday = "Saturday" as any,
    }
    export const DayOfWeek_Type = new EnumType<DayOfWeek>("DayOfWeek", DayOfWeek);
    
}

export namespace Patterns {

    export module EntityMessage {
        export const AttemptToSet0InLockedEntity1 = new MessageKey("EntityMessage", "AttemptToSet0InLockedEntity1");
        export const AttemptToAddRemove0InLockedEntity1 = new MessageKey("EntityMessage", "AttemptToAddRemove0InLockedEntity1");
    }
    
    export interface LockableEntity extends Entity {
        locked?: boolean;
    }
    
}

