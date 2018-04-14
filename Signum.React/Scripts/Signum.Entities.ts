//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'

export interface ModifiableEntity {
    Type: string;
    toStr: string;	
    modified : boolean;
    isNew: boolean;
    error?: { [member: string]: string };
}

export interface Entity extends ModifiableEntity {
    id: number | string;
    ticks: string; //max value
    mixins?: { [name: string]: MixinEntity }
}

export interface EnumEntity<T> extends Entity {

}

export interface MixinEntity extends ModifiableEntity {
}

export function getMixin<M extends MixinEntity>(entity: Entity, type: Type<M>): M {

    var mixin = tryGetMixin(entity, type);
    if (!mixin)
        throw new Error("Entity " + entity + " does not contain mixin " + type.typeName);
    return mixin;
}

export function tryGetMixin<M extends MixinEntity>(entity: Entity, type: Type<M>) : M | undefined  {
    return entity.mixins && entity.mixins[type.typeName] as M;
}

export type MList<T> = Array<MListElement<T>>;

export interface MListElement<T> {
    rowId: number | string | null;
    element: T;
}

export function newMListElement<T>(element: T): MListElement<T> {
    return { rowId: null, element };
}

export function toMList<T>(array: T[]): MList<T> {
    return array.map(newMListElement);
}

export interface Lite<T extends Entity> {
    entity?: T;
    EntityType: string;
    id?: number | string;
    toStr?: string;
}

export interface ModelState {
    [field: string]: string[];
}

export interface EntityPack<T extends ModifiableEntity> {
    readonly entity: T
    readonly canExecute: { [key: string]: string };
}

//The interfaces add no real members, they are there just to force TS structural typing

export interface ExecuteSymbol<T extends Entity> extends OperationSymbol { _execute_: T /*TRICK*/ };
export interface DeleteSymbol<T extends Entity> extends OperationSymbol { _delete_: T /*TRICK*/ };
export interface ConstructSymbol_Simple<T extends Entity> extends OperationSymbol { _construct_: T /*TRICK*/ };
export interface ConstructSymbol_From<T extends Entity, F extends Entity> extends OperationSymbol { _constructFrom_: T, _from_?: F /*TRICK*/ };
export interface ConstructSymbol_FromMany<T extends Entity, F extends Entity> extends OperationSymbol {  _constructFromMany_: T, _from_?: F /*TRICK*/ };

export const toStringDictionary: { [name: string]: ((entity: any) => string) | null } = {};

export function registerToString<T extends ModifiableEntity>(type: Type<T>, toStringFunc: ((e: T) => string) | null) {
    toStringDictionary[type.typeName] = toStringFunc as ((e: ModifiableEntity) => string) | null;
}

import * as Reflection from './Reflection' 

function getOrCreateToStringFunction(type: string)  {
    let f = toStringDictionary[type];
    if (f || f === null)
        return f; 

    const ti = Reflection.getTypeInfo(type);

    const getToString2 = getToString;

    try {
        const getToString = getToString2;
        const valToString = Reflection.valToString;
        const numberToString = Reflection.numberToString;
        const dateToString = Reflection.dateToString;
        const durationToString = Reflection.durationToString;

        f = ti && ti.toStringFunction ? eval("(" + ti.toStringFunction + ")") : null;
    } catch (e) {
        f = null;
    }

    toStringDictionary[type] = f;

    return f;
}

export function getToString(entityOrLite: ModifiableEntity | Lite<Entity> | undefined): string {
    if (entityOrLite == null)
        return "";

    const lite = entityOrLite as Lite<Entity>;
    if (lite.EntityType)
        return lite.entity ? getToString(lite.entity) : (lite.toStr || lite.EntityType);

    const entity = entityOrLite as ModifiableEntity;
    const toStr = getOrCreateToStringFunction(entity.Type);
    if (toStr)
        return toStr(entity);

    return entity.toStr || entity.Type;
}

export function toLite<T extends Entity>(entity: T, fat?: boolean, toStr?: string): Lite<T>;
export function toLite<T extends Entity>(entity: T | null | undefined, fat?: boolean, toStr?: string): Lite<T> | null;
export function toLite<T extends Entity>(entity: T | null | undefined, fat?: boolean, toStr?: string): Lite<T> | null {

    if(!entity)
        return null;
    if(fat)
       return toLiteFat(entity, toStr);

    if(entity.id == undefined)
        throw new Error(`The ${entity.Type} has no Id`);

    return {
       EntityType : entity.Type,
       id: entity.id,
       toStr: toStr || getToString(entity),
    }
}

export function toLiteFat<T extends Entity>(entity: T, toStr?:string) : Lite<T> {
    
    return {
       entity : entity,
       EntityType  :entity.Type,
       id: entity.id,
       toStr: toStr || getToString(entity),
    }
}

export function liteKey(lite: Lite<Entity>) {
    return lite.EntityType + ";" + (lite.id == undefined ? "" : lite.id);
}

export function parseLite(lite: string) : Lite<Entity> {
    return {
        EntityType: lite.before(";"),
        id :  lite.after(";"),
    };
}

export function is<T extends Entity>(a: Lite<T> | T | null | undefined, b: Lite<T> | T | null | undefined, compareTicks = false) {

    if(a == undefined && b == undefined)
        return true;
        
    if(a == undefined || b == undefined)
        return false;

    const aType = (a as T).Type || (a as Lite<T>).EntityType;
    const bType = (b as T).Type || (b as Lite<T>).EntityType;

    if(!aType || !bType)
        throw new Error("No Type found");

    if (aType != bType)
        return false;

    if (a.id != undefined || b.id != undefined)
        return a.id == b.id && (!compareTicks || (a as T).ticks == (b as T).ticks);

    const aEntity = (a as T).Type ? a as T : (a as Lite<T>).entity;
    const bEntity = (b as T).Type ? b as T : (b as Lite<T>).entity;
    
    return aEntity == bEntity;
}

export function isLite(obj: any): obj is Lite<Entity> {
    return (obj as Lite<Entity>).EntityType != undefined;
}

export function isModifiableEntity(obj: any): obj is ModifiableEntity {
    return (obj as ModifiableEntity).Type != undefined;
}

export function isEntity(obj: any): obj is Entity {
    return (obj as Entity).Type != undefined;
}

export function isEntityPack(obj: any): obj is EntityPack<ModifiableEntity>{
    return (obj as EntityPack<ModifiableEntity>).entity != undefined &&
        (obj as EntityPack<ModifiableEntity>).canExecute !== undefined;
}

export function entityInfo(entity: ModifiableEntity | Lite<Entity> | null | undefined)
{
    if (!entity)
        return "undefined";

    const type = isLite(entity) ? entity.EntityType : entity.Type;
    const id = isLite(entity) ? entity.id : isEntity(entity) ? entity.id : "";
    const isNew = isLite(entity) ? entity.entity && entity.entity.isNew : entity.isNew;

    return  `${type};${id || ""};${isNew || ""}`;
}

export const BooleanEnum = new EnumType<BooleanEnum>("BooleanEnum");
export type BooleanEnum =
    "False" |
    "True";

export module CalendarMessage {
    export const Today = new MessageKey("CalendarMessage", "Today");
}

export module ConnectionMessage {
    export const AConnectionWithTheServerIsNecessaryToContinue = new MessageKey("ConnectionMessage", "AConnectionWithTheServerIsNecessaryToContinue");
    export const SessionExpired = new MessageKey("ConnectionMessage", "SessionExpired");
    export const ANewVersionHasJustBeenDeployedSaveChangesAnd0 = new MessageKey("ConnectionMessage", "ANewVersionHasJustBeenDeployedSaveChangesAnd0");
    export const Refresh = new MessageKey("ConnectionMessage", "Refresh");
}

export const CorruptMixin = new Type<CorruptMixin>("CorruptMixin");
export interface CorruptMixin extends MixinEntity {
    Type: "CorruptMixin";
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
    export const Move = new MessageKey("EntityControlMessage", "Move");
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
    export const editColumn = new MessageKey("JavascriptMessage", "editColumn");
    export const enterTheNewColumnName = new MessageKey("JavascriptMessage", "enterTheNewColumnName");
    export const error = new MessageKey("JavascriptMessage", "error");
    export const executed = new MessageKey("JavascriptMessage", "executed");
    export const hideFilters = new MessageKey("JavascriptMessage", "hideFilters");
    export const showFilters = new MessageKey("JavascriptMessage", "showFilters");
    export const groupResults = new MessageKey("JavascriptMessage", "groupResults");
    export const ungroupResults = new MessageKey("JavascriptMessage", "ungroupResults");
    export const activateTimeMachine = new MessageKey("JavascriptMessage", "activateTimeMachine");
    export const deactivateTimeMachine = new MessageKey("JavascriptMessage", "deactivateTimeMachine");
    export const showRecords = new MessageKey("JavascriptMessage", "showRecords");
    export const loading = new MessageKey("JavascriptMessage", "loading");
    export const noActionsFound = new MessageKey("JavascriptMessage", "noActionsFound");
    export const saveChangesBeforeOrPressCancel = new MessageKey("JavascriptMessage", "saveChangesBeforeOrPressCancel");
    export const loseCurrentChanges = new MessageKey("JavascriptMessage", "loseCurrentChanges");
    export const noElementsSelected = new MessageKey("JavascriptMessage", "noElementsSelected");
    export const searchForResults = new MessageKey("JavascriptMessage", "searchForResults");
    export const selectOnlyOneElement = new MessageKey("JavascriptMessage", "selectOnlyOneElement");
    export const popupErrors = new MessageKey("JavascriptMessage", "popupErrors");
    export const popupErrorsStop = new MessageKey("JavascriptMessage", "popupErrorsStop");
    export const insertColumn = new MessageKey("JavascriptMessage", "insertColumn");
    export const removeColumn = new MessageKey("JavascriptMessage", "removeColumn");
    export const reorderColumn_MoveLeft = new MessageKey("JavascriptMessage", "reorderColumn_MoveLeft");
    export const reorderColumn_MoveRight = new MessageKey("JavascriptMessage", "reorderColumn_MoveRight");
    export const saved = new MessageKey("JavascriptMessage", "saved");
    export const search = new MessageKey("JavascriptMessage", "search");
    export const Selected = new MessageKey("JavascriptMessage", "Selected");
    export const selectToken = new MessageKey("JavascriptMessage", "selectToken");
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
    export const showPeriod = new MessageKey("JavascriptMessage", "showPeriod");
    export const showPreviousOperation = new MessageKey("JavascriptMessage", "showPreviousOperation");
}

export module LiteMessage {
    export const IdNotValid = new MessageKey("LiteMessage", "IdNotValid");
    export const InvalidFormat = new MessageKey("LiteMessage", "InvalidFormat");
    export const New_G = new MessageKey("LiteMessage", "New_G");
    export const Type0NotFound = new MessageKey("LiteMessage", "Type0NotFound");
    export const ToStr = new MessageKey("LiteMessage", "ToStr");
}

export interface ModelEntity extends ModifiableEntity {
}

export module NormalControlMessage {
    export const Save = new MessageKey("NormalControlMessage", "Save");
    export const ViewForType0IsNotAllowed = new MessageKey("NormalControlMessage", "ViewForType0IsNotAllowed");
    export const SaveChangesFirst = new MessageKey("NormalControlMessage", "SaveChangesFirst");
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
    export const _0AndClose = new MessageKey("NormalWindowMessage", "_0AndClose");
    export const New0_G = new MessageKey("NormalWindowMessage", "New0_G");
    export const Type0Id1 = new MessageKey("NormalWindowMessage", "Type0Id1");
}

export module OperationMessage {
    export const Create = new MessageKey("OperationMessage", "Create");
    export const CreateFromRegex = new MessageKey("OperationMessage", "CreateFromRegex");
    export const StateShouldBe0InsteadOf1 = new MessageKey("OperationMessage", "StateShouldBe0InsteadOf1");
    export const InUserInterface = new MessageKey("OperationMessage", "InUserInterface");
    export const Operation01IsNotAuthorized = new MessageKey("OperationMessage", "Operation01IsNotAuthorized");
    export const Confirm = new MessageKey("OperationMessage", "Confirm");
    export const PleaseConfirmYouDLikeToDelete0FromTheSystem = new MessageKey("OperationMessage", "PleaseConfirmYouDLikeToDelete0FromTheSystem");
    export const PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem = new MessageKey("OperationMessage", "PleaseConfirmYouDLikeToDeleteTheEntityFromTheSystem");
    export const PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem = new MessageKey("OperationMessage", "PleaseConfirmYouDLikeToDeleteTheSelectedEntitiesFromTheSystem");
    export const TheOperation0DidNotReturnAnEntity = new MessageKey("OperationMessage", "TheOperation0DidNotReturnAnEntity");
    export const Logs = new MessageKey("OperationMessage", "Logs");
    export const PreviousOperationLog = new MessageKey("OperationMessage", "PreviousOperationLog");
}

export const OperationSymbol = new Type<OperationSymbol>("Operation");
export interface OperationSymbol extends Symbol {
    Type: "Operation";
}

export const OperationType = new EnumType<OperationType>("OperationType");
export type OperationType =
    "Execute" |
    "Delete" |
    "Constructor" |
    "ConstructorFrom" |
    "ConstructorFromMany";

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
    export const CollectionsCanNotBeAddedAsColumns = new MessageKey("SearchMessage", "CollectionsCanNotBeAddedAsColumns");
    export const AddFilter = new MessageKey("SearchMessage", "AddFilter");
    export const AddValue = new MessageKey("SearchMessage", "AddValue");
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
    export const Explore = new MessageKey("SearchMessage", "Explore");
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
    key: string;
}

export module SynchronizerMessage {
    export const EndOfSyncScript = new MessageKey("SynchronizerMessage", "EndOfSyncScript");
    export const StartOfSyncScriptGeneratedOn0 = new MessageKey("SynchronizerMessage", "StartOfSyncScriptGeneratedOn0");
}

export module ValidationMessage {
    export const _0DoesNotHaveAValid1Format = new MessageKey("ValidationMessage", "_0DoesNotHaveAValid1Format");
    export const _0DoesNotHaveAValid1IdentifierFormat = new MessageKey("ValidationMessage", "_0DoesNotHaveAValid1IdentifierFormat");
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
    export const _0ShouldHaveJustOneLine = new MessageKey("ValidationMessage", "_0ShouldHaveJustOneLine");
    export const _0ShouldNotHaveInitialSpaces = new MessageKey("ValidationMessage", "_0ShouldNotHaveInitialSpaces");
    export const _0ShouldNotHaveFinalSpaces = new MessageKey("ValidationMessage", "_0ShouldNotHaveFinalSpaces");
    export const TheLenghtOf0HasToBeEqualTo1 = new MessageKey("ValidationMessage", "TheLenghtOf0HasToBeEqualTo1");
    export const TheLengthOf0HasToBeGreaterOrEqualTo1 = new MessageKey("ValidationMessage", "TheLengthOf0HasToBeGreaterOrEqualTo1");
    export const TheLengthOf0HasToBeLesserOrEqualTo1 = new MessageKey("ValidationMessage", "TheLengthOf0HasToBeLesserOrEqualTo1");
    export const TheNumberOf0IsBeingMultipliedBy1 = new MessageKey("ValidationMessage", "TheNumberOf0IsBeingMultipliedBy1");
    export const TheRowsAreBeingGroupedBy0 = new MessageKey("ValidationMessage", "TheRowsAreBeingGroupedBy0");
    export const TheNumberOfElementsOf0HasToBe12 = new MessageKey("ValidationMessage", "TheNumberOfElementsOf0HasToBe12");
    export const Type0NotAllowed = new MessageKey("ValidationMessage", "Type0NotAllowed");
    export const _0IsMandatoryWhen1IsNotSet = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsNotSet");
    export const _0IsMandatoryWhen1IsNotSetTo2 = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsNotSetTo2");
    export const _0IsMandatoryWhen1IsSet = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsSet");
    export const _0IsMandatoryWhen1IsSetTo2 = new MessageKey("ValidationMessage", "_0IsMandatoryWhen1IsSetTo2");
    export const _0ShouldBeNullWhen1IsNotSet = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsNotSet");
    export const _0ShouldBeNullWhen1IsNotSetTo2 = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsNotSetTo2");
    export const _0ShouldBeNullWhen1IsSet = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsSet");
    export const _0ShouldBeNullWhen1IsSetTo2 = new MessageKey("ValidationMessage", "_0ShouldBeNullWhen1IsSetTo2");
    export const _0ShouldBeNull = new MessageKey("ValidationMessage", "_0ShouldBeNull");
    export const _0ShouldBeADateInThePast = new MessageKey("ValidationMessage", "_0ShouldBeADateInThePast");
    export const BeInThePast = new MessageKey("ValidationMessage", "BeInThePast");
    export const _0ShouldBeGreaterThan1 = new MessageKey("ValidationMessage", "_0ShouldBeGreaterThan1");
    export const _0ShouldBeGreaterThanOrEqual1 = new MessageKey("ValidationMessage", "_0ShouldBeGreaterThanOrEqual1");
    export const _0ShouldBeLessThan1 = new MessageKey("ValidationMessage", "_0ShouldBeLessThan1");
    export const _0ShouldBeLessThanOrEqual1 = new MessageKey("ValidationMessage", "_0ShouldBeLessThanOrEqual1");
    export const _0HasAPrecissionOf1InsteadOf2 = new MessageKey("ValidationMessage", "_0HasAPrecissionOf1InsteadOf2");
    export const _0ShouldBeOfType1 = new MessageKey("ValidationMessage", "_0ShouldBeOfType1");
    export const _0ShouldNotBeOfType1 = new MessageKey("ValidationMessage", "_0ShouldNotBeOfType1");
    export const _0And1CanNotBeSetAtTheSameTime = new MessageKey("ValidationMessage", "_0And1CanNotBeSetAtTheSameTime");
    export const _0And1And2CanNotBeSetAtTheSameTime = new MessageKey("ValidationMessage", "_0And1And2CanNotBeSetAtTheSameTime");
    export const _0Have1ElementsButAllowedOnly2 = new MessageKey("ValidationMessage", "_0Have1ElementsButAllowedOnly2");
}

export module VoidEnumMessage {
    export const Instance = new MessageKey("VoidEnumMessage", "Instance");
}

export namespace External {

    export module CollectionMessage {
        export const And = new MessageKey("CollectionMessage", "And");
        export const Or = new MessageKey("CollectionMessage", "Or");
        export const No0Found = new MessageKey("CollectionMessage", "No0Found");
        export const MoreThanOne0Found = new MessageKey("CollectionMessage", "MoreThanOne0Found");
    }
    
    export const DayOfWeek = new EnumType<DayOfWeek>("DayOfWeek");
    export type DayOfWeek =
        "Sunday" |
        "Monday" |
        "Tuesday" |
        "Wednesday" |
        "Thursday" |
        "Friday" |
        "Saturday";
    
}


