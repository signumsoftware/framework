//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'

export interface ModifiableEntity {
  Type: string;
  toStr: string;
  modified: boolean;
  isNew: boolean;
  error?: { [member: string]: string };
  readonlyProperties?: string[];
  mixins?: { [name: string]: MixinEntity }
}

export function liteKeyLong(lite: Lite<Entity>) {
  return lite.EntityType + ";" + (lite.id == undefined ? "" : lite.id) + ";" + getToString(lite);
}

export interface Entity extends ModifiableEntity {
  id: number | string | undefined;
  ticks: string; //max value
}

export interface EnumEntity<T> extends Entity {

}

export interface MixinEntity extends ModifiableEntity {
}

export function getMixin<M extends MixinEntity>(entity: ModifiableEntity, type: Type<M>): M {

  var mixin = tryGetMixin(entity, type);
  if (!mixin)
    throw new Error("Entity " + entity + " does not contain mixin " + type.typeName);
  return mixin;
}

export function tryGetMixin<M extends MixinEntity>(entity: ModifiableEntity, type: Type<M>): M | undefined {
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

export function isMListElement(obj: unknown): obj is MListElement<unknown> {
  return obj != null && (obj as MListElement<unknown>).rowId !== undefined;
}


export function toMList<T>(array: T[]): MList<T> {
  return array.map(newMListElement);
}

export interface Lite<T extends Entity> {
  EntityType: string;
  id?: number | string;
  model?: unknown;

  ModelType?: string;
  entity?: T;
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
export interface ConstructSymbol_FromMany<T extends Entity, F extends Entity> extends OperationSymbol { _constructFromMany_: T, _from_?: F /*TRICK*/ };

export const toStringDictionary: { [name: string]: ((entity: any) => string) | null } = {};

export function registerToString<T extends ModifiableEntity>(type: Type<T>, toStringFunc: ((e: T) => string) | null) {
  toStringDictionary[type.typeName] = toStringFunc as ((e: ModifiableEntity) => string) | null;
}


export function registerCustomModelConsturctor<T extends Entity, M extends ModelEntity>(type: Type<T>, modelType: Type<T>, constructLiteModel: ((e: T) => M)) {
  var ti = Reflection.tryGetTypeInfo(type.typeName);

  if (ti) {
    var clm = ti.customLiteModels?.[modelType.typeName];

    if (clm == null)
      throw new Error(`Type ${type.typeName} has no registered Lite Model '${modelType}'`); 

    clm.constructorFunction = constructLiteModel as any as (e: Entity) => ModelEntity;
  }
}

import * as Reflection from './Reflection'
import { object } from 'prop-types';

export function newNiceName(ti: Reflection.TypeInfo) {
  return FrameMessage.New0_G.niceToString().forGenderAndNumber(ti.gender).formatWith(ti.niceName);
}

function createLiteModel(e: Entity, modelType?: string): ModelEntity | string {

  var ti = Reflection.tryGetTypeInfo(e.Type);

  if (ti == null)
    return getToString(e);

  modelType ??= getDefaultLiteModelType(ti);

  if (modelType == "string")
    return getToString(e);

  var clm = ti.customLiteModels?.[modelType];

  if (clm == null)
    throw new Error(`Type ${e.Type} has no registered Lite Model '${modelType}'`); 
  
  if (clm.constructorFunction)
    return clm.constructorFunction(e);

  if (clm.constructorFunctionString == null)
    throw new Error(`No constructor function for '${modelType}' provided`);

  clm.constructorFunction = compileFunction(clm.constructorFunctionString);

  return clm.constructorFunction!(e);
}

function getDefaultLiteModelType(ti: Reflection.TypeInfo) {
  if (!ti.customLiteModels)
    return "string";

  return Object.keys(ti.customLiteModels).singleOrNull(modelType => ti.customLiteModels![modelType].isDefault) ?? "string"
}

function getOrCreateToStringFunction(type: string) {
  let f = toStringDictionary[type];
  if (f || f === null)
    return f;

  const ti = Reflection.tryGetTypeInfo(type);

  f = toStringDictionary[type] = ti?.toStringFunction ? compileFunction(ti.toStringFunction) : null;

  return f;
}

function compileFunction(functionString: string): (e: any) => any {

  var func = new Function("e", "fd", functionString);

  var funcDeps = {
    getToString: getToString,
    valToString: Reflection.valToString,
    numberToString: Reflection.numberToString,
    dateToString: Reflection.dateToString,
    timeToString: Reflection.timeToString,
    getTypeInfo: Reflection.getTypeInfo,
    newNiceName: newNiceName,
    New : Reflection.New,
    toLite: toLite,
  };

  return e => func(e, funcDeps);
}


export function getToString(entityOrLite: ModifiableEntity | Lite<Entity> | undefined | null, toStringLite?: (e : Entity) => string): string {
  if (entityOrLite == null)
    return "";

  const lite = entityOrLite as Lite<Entity>;
  if (lite.EntityType) {
    if (lite.entity)
      return (toStringLite || getToString)(lite.entity);

    if (Reflection.isLowPopulationSymbol(lite.EntityType))
      return Reflection.symbolNiceName(lite as Lite<Entity & Reflection.ISymbol>);

    if (typeof lite.model == "string")
      return lite.model;

    if (isModifiableEntity(lite.model))
      return getToString(lite.model);

    return lite.EntityType;
  }

  const entity = entityOrLite as ModifiableEntity;
  const toStrFunc = getOrCreateToStringFunction(entity.Type);
  if (toStrFunc)
    return toStrFunc(entity);

  if (Reflection.isLowPopulationSymbol(entity.Type))
    return Reflection.symbolNiceName(entity as Entity & Reflection.ISymbol);

  return entity.toStr || entity.Type;
}

export function toLite<T extends Entity>(entity: T, fat?: boolean, model?: unknown): Lite<T>;
export function toLite<T extends Entity>(entity: T | null | undefined, fat?: boolean, model?: unknown): Lite<T> | null;
export function toLite<T extends Entity>(entity: T | null | undefined, fat?: boolean, model?: unknown): Lite<T> | null {

  if (!entity)
    return null;
  if (fat)
    return toLiteFat(entity, model);

  if (entity.id == undefined)
    throw new Error(`The ${entity.Type} has no Id`);

  return {
    EntityType: entity.Type,
    id: entity.id,
    model: model || createLiteModel(entity),
  }
}

export function toLiteFat<T extends Entity>(entity: T, model?: unknown): Lite<T> {

  return {
    entity: entity,
    EntityType: entity.Type,
    id: entity.id,
    model: model || createLiteModel(entity),
  }
}

export function liteKey(lite: Lite<Entity>) {
  return lite.EntityType + ";" + (lite.id == undefined ? "" : lite.id);
}

export function parseLite(lite: string): Lite<Entity> {
  return {
    EntityType: lite.before(";"),
    id: lite.after(";"),
  };
}

export const liteKeyRegEx = /^([a-zA-Z]+)[;]([0-9a-zA-Z-]+)$/;
export function parseLiteList(text: string): Lite<Entity>[] {
  const lines = text.split("|");
  const liteKeys = lines.map(l => liteKeyRegEx.test(l) ? l : null).notNull();
  const lites = liteKeys.map(lk => parseLite(lk)).filter(l => isLite(l));

  return lites;
}

export function is<T extends Entity>(a: Lite<T> | T | null | undefined, b: Lite<T> | T | null | undefined, compareTicks = false, assertTypesFound = true) {

  if (a == undefined && b == undefined)
    return true;

  if (a == undefined || b == undefined)
    return false;

  const aType = (a as T).Type || (a as Lite<T>).EntityType;
  const bType = (b as T).Type || (b as Lite<T>).EntityType;

  if (!aType || !bType) {
    if (assertTypesFound)
      throw new Error("No Type found");
    else
      return false;
  }

  if (aType != bType)
    return false;

  if (a.id != undefined || b.id != undefined)
    return a.id == b.id && (!compareTicks || (a as T).ticks == (b as T).ticks);

  const aEntity = isEntity(a) ? a as unknown as T: a.entity;
  const bEntity = isEntity(b) ? b as unknown as T : b.entity;

  return aEntity == bEntity;
}

export function isLite(obj: any): obj is Lite<Entity> {
  return obj != null && (obj as Lite<Entity>).EntityType != undefined;
}

export function isModifiableEntity(obj: any): obj is ModifiableEntity {
return obj != null && (obj as ModifiableEntity).Type != undefined;
}

export function isEntity(obj: any): obj is Entity {
  if(!isModifiableEntity(obj))
    return false;
  const ti = Reflection.tryGetTypeInfo(obj.Type);
  return ti != null && ti.entityKind != null;
}

export function isEntityPack(obj: any): obj is EntityPack<ModifiableEntity> {
  return obj != null && (obj as EntityPack<ModifiableEntity>).entity != undefined &&
    (obj as EntityPack<ModifiableEntity>).canExecute !== undefined;
}

export function entityInfo(entity: ModifiableEntity | Lite<Entity> | null | undefined) {
  if (!entity)
    return "undefined";

  const type = isLite(entity) ? entity.EntityType : entity.Type;
  const id = isLite(entity) ? entity.id : isEntity(entity) ? entity.id : "";
  const isNew = isLite(entity) ? entity.entity && entity.entity.isNew : entity.isNew;

  return `${type};${id || ""};${isNew || ""}`;
}

export const BooleanEnum = new EnumType<BooleanEnum>("BooleanEnum");
export type BooleanEnum =
  "False" |
  "True";

export module CalendarMessage {
  export const Today = new MessageKey("CalendarMessage", "Today");
}

export const ComparisonType = new EnumType<ComparisonType>("ComparisonType");
export type ComparisonType =
  "EqualTo" |
  "DistinctTo" |
  "GreaterThan" |
  "GreaterThanOrEqualTo" |
  "LessThan" |
  "LessThanOrEqualTo";

export module ConnectionMessage {
  export const AConnectionWithTheServerIsNecessaryToContinue = new MessageKey("ConnectionMessage", "AConnectionWithTheServerIsNecessaryToContinue");
  export const SessionExpired = new MessageKey("ConnectionMessage", "SessionExpired");
  export const ANewVersionHasJustBeenDeployedSaveChangesAnd0 = new MessageKey("ConnectionMessage", "ANewVersionHasJustBeenDeployedSaveChangesAnd0");
  export const OutdatedClientApplication = new MessageKey("ConnectionMessage", "OutdatedClientApplication");
  export const ANewVersionHasJustBeenDeployedConsiderReload = new MessageKey("ConnectionMessage", "ANewVersionHasJustBeenDeployedConsiderReload");
  export const Refresh = new MessageKey("ConnectionMessage", "Refresh");
}

export const CorruptMixin = new Type<CorruptMixin>("CorruptMixin");
export interface CorruptMixin extends MixinEntity {
  Type: "CorruptMixin";
  corrupt: boolean;
}

export interface EmbeddedEntity extends ModifiableEntity {
}

export module EngineMessage {
  export const ConcurrencyErrorOnDatabaseTable0Id1 = new MessageKey("EngineMessage", "ConcurrencyErrorOnDatabaseTable0Id1");
  export const EntityWithType0AndId1NotFound = new MessageKey("EngineMessage", "EntityWithType0AndId1NotFound");
  export const NoWayOfMappingType0Found = new MessageKey("EngineMessage", "NoWayOfMappingType0Found");
  export const TheEntity0IsNew = new MessageKey("EngineMessage", "TheEntity0IsNew");
  export const ThereAre0ThatReferThisEntityByProperty1 = new MessageKey("EngineMessage", "ThereAre0ThatReferThisEntityByProperty1");
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
  export const MoveRight = new MessageKey("EntityControlMessage", "MoveRight");
  export const MoveLeft = new MessageKey("EntityControlMessage", "MoveLeft");
  export const Move = new MessageKey("EntityControlMessage", "Move");
  export const MoveWithDragAndDropOrCtrlUpDown = new MessageKey("EntityControlMessage", "MoveWithDragAndDropOrCtrlUpDown");
  export const MoveWithDragAndDropOrCtrlLeftRight = new MessageKey("EntityControlMessage", "MoveWithDragAndDropOrCtrlLeftRight");
  export const Navigate = new MessageKey("EntityControlMessage", "Navigate");
  export const Remove = new MessageKey("EntityControlMessage", "Remove");
  export const View = new MessageKey("EntityControlMessage", "View");
  export const Add = new MessageKey("EntityControlMessage", "Add");
  export const Paste = new MessageKey("EntityControlMessage", "Paste");
}

export module FrameMessage {
  export const New0_G = new MessageKey("FrameMessage", "New0_G");
  export const Copied = new MessageKey("FrameMessage", "Copied");
  export const Fullscreen = new MessageKey("FrameMessage", "Fullscreen");
  export const ThereAreErrors = new MessageKey("FrameMessage", "ThereAreErrors");
  export const Main = new MessageKey("FrameMessage", "Main");
}

export interface ImmutableEntity extends Entity {
  allowChange: boolean;
}

export module JavascriptMessage {
  export const chooseAType = new MessageKey("JavascriptMessage", "chooseAType");
  export const chooseAValue = new MessageKey("JavascriptMessage", "chooseAValue");
  export const addFilter = new MessageKey("JavascriptMessage", "addFilter");
  export const openTab = new MessageKey("JavascriptMessage", "openTab");
  export const error = new MessageKey("JavascriptMessage", "error");
  export const executed = new MessageKey("JavascriptMessage", "executed");
  export const hideFilters = new MessageKey("JavascriptMessage", "hideFilters");
  export const showFilters = new MessageKey("JavascriptMessage", "showFilters");
  export const groupResults = new MessageKey("JavascriptMessage", "groupResults");
  export const ungroupResults = new MessageKey("JavascriptMessage", "ungroupResults");
  export const ShowGroup = new MessageKey("JavascriptMessage", "ShowGroup");
  export const activateTimeMachine = new MessageKey("JavascriptMessage", "activateTimeMachine");
  export const deactivateTimeMachine = new MessageKey("JavascriptMessage", "deactivateTimeMachine");
  export const showRecords = new MessageKey("JavascriptMessage", "showRecords");
  export const joinMode = new MessageKey("JavascriptMessage", "joinMode");
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
  export const editColumn = new MessageKey("JavascriptMessage", "editColumn");
  export const removeColumn = new MessageKey("JavascriptMessage", "removeColumn");
  export const groupByThisColumn = new MessageKey("JavascriptMessage", "groupByThisColumn");
  export const restoreDefaultColumns = new MessageKey("JavascriptMessage", "restoreDefaultColumns");
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
  export const Date = new MessageKey("JavascriptMessage", "Date");
}

export module LiteMessage {
  export const IdNotValid = new MessageKey("LiteMessage", "IdNotValid");
  export const InvalidFormat = new MessageKey("LiteMessage", "InvalidFormat");
  export const Type0NotFound = new MessageKey("LiteMessage", "Type0NotFound");
  export const ToStr = new MessageKey("LiteMessage", "ToStr");
}

export interface ModelEntity extends ModifiableEntity {
}

export module NormalControlMessage {
  export const ViewForType0IsNotAllowed = new MessageKey("NormalControlMessage", "ViewForType0IsNotAllowed");
  export const SaveChangesFirst = new MessageKey("NormalControlMessage", "SaveChangesFirst");
  export const CopyEntityTypeAndIdForAutocomplete = new MessageKey("NormalControlMessage", "CopyEntityTypeAndIdForAutocomplete");
  export const CopyEntityUrl = new MessageKey("NormalControlMessage", "CopyEntityUrl");
}

export module OperationMessage {
  export const Create = new MessageKey("OperationMessage", "Create");
  export const CreateFromRegex = new MessageKey("OperationMessage", "CreateFromRegex");
  export const Create0 = new MessageKey("OperationMessage", "Create0");
  export const StateShouldBe0InsteadOf1 = new MessageKey("OperationMessage", "StateShouldBe0InsteadOf1");
  export const TheStateOf0ShouldBe1InsteadOf2 = new MessageKey("OperationMessage", "TheStateOf0ShouldBe1InsteadOf2");
  export const InUserInterface = new MessageKey("OperationMessage", "InUserInterface");
  export const Operation01IsNotAuthorized = new MessageKey("OperationMessage", "Operation01IsNotAuthorized");
  export const Confirm = new MessageKey("OperationMessage", "Confirm");
  export const PleaseConfirmYouWouldLikeToDelete0FromTheSystem = new MessageKey("OperationMessage", "PleaseConfirmYouWouldLikeToDelete0FromTheSystem");
  export const PleaseConfirmYouWouldLikeTo01 = new MessageKey("OperationMessage", "PleaseConfirmYouWouldLikeTo01");
  export const TheOperation0DidNotReturnAnEntity = new MessageKey("OperationMessage", "TheOperation0DidNotReturnAnEntity");
  export const Logs = new MessageKey("OperationMessage", "Logs");
  export const PreviousOperationLog = new MessageKey("OperationMessage", "PreviousOperationLog");
  export const _0AndClose = new MessageKey("OperationMessage", "_0AndClose");
  export const _0AndNew = new MessageKey("OperationMessage", "_0AndNew");
  export const BulkModifications = new MessageKey("OperationMessage", "BulkModifications");
  export const PleaseConfirmThatYouWouldLikeToApplyTheAboveChangesAndExecute0Over12 = new MessageKey("OperationMessage", "PleaseConfirmThatYouWouldLikeToApplyTheAboveChangesAndExecute0Over12");
  export const Condition = new MessageKey("OperationMessage", "Condition");
  export const Setters = new MessageKey("OperationMessage", "Setters");
  export const AddSetter = new MessageKey("OperationMessage", "AddSetter");
  export const MultiSetter = new MessageKey("OperationMessage", "MultiSetter");
  export const Deleting = new MessageKey("OperationMessage", "Deleting");
  export const Executing0 = new MessageKey("OperationMessage", "Executing0");
  export const _0Errors = new MessageKey("OperationMessage", "_0Errors");
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

export const PropertyOperation = new EnumType<PropertyOperation>("PropertyOperation");
export type PropertyOperation =
  "Set" |
  "AddElement" |
  "AddNewElement" |
  "ChangeElements" |
  "RemoveElement" |
  "RemoveElementsWhere" |
  "ModifyEntity" |
  "CreateNewEntity";

export module QuickLinkMessage {
  export const Quicklinks = new MessageKey("QuickLinkMessage", "Quicklinks");
  export const No0Found = new MessageKey("QuickLinkMessage", "No0Found");
}

export module ReactWidgetsMessage {
  export const MoveToday = new MessageKey("ReactWidgetsMessage", "MoveToday");
  export const MoveBack = new MessageKey("ReactWidgetsMessage", "MoveBack");
  export const MoveForward = new MessageKey("ReactWidgetsMessage", "MoveForward");
  export const DateButton = new MessageKey("ReactWidgetsMessage", "DateButton");
  export const OpenCombobox = new MessageKey("ReactWidgetsMessage", "OpenCombobox");
  export const FilterPlaceholder = new MessageKey("ReactWidgetsMessage", "FilterPlaceholder");
  export const EmptyList = new MessageKey("ReactWidgetsMessage", "EmptyList");
  export const EmptyFilter = new MessageKey("ReactWidgetsMessage", "EmptyFilter");
  export const CreateOption = new MessageKey("ReactWidgetsMessage", "CreateOption");
  export const CreateOption0 = new MessageKey("ReactWidgetsMessage", "CreateOption0");
  export const TagsLabel = new MessageKey("ReactWidgetsMessage", "TagsLabel");
  export const RemoveLabel = new MessageKey("ReactWidgetsMessage", "RemoveLabel");
  export const NoneSelected = new MessageKey("ReactWidgetsMessage", "NoneSelected");
  export const SelectedItems0 = new MessageKey("ReactWidgetsMessage", "SelectedItems0");
  export const IncrementValue = new MessageKey("ReactWidgetsMessage", "IncrementValue");
  export const DecrementValue = new MessageKey("ReactWidgetsMessage", "DecrementValue");
}

export module SaveChangesMessage {
  export const ThereAreChanges = new MessageKey("SaveChangesMessage", "ThereAreChanges");
  export const YoureTryingToCloseAnEntityWithChanges = new MessageKey("SaveChangesMessage", "YoureTryingToCloseAnEntityWithChanges");
  export const LoseChanges = new MessageKey("SaveChangesMessage", "LoseChanges");
}

export module SearchMessage {
  export const ChooseTheDisplayNameOfTheNewColumn = new MessageKey("SearchMessage", "ChooseTheDisplayNameOfTheNewColumn");
  export const Field = new MessageKey("SearchMessage", "Field");
  export const AddColumn = new MessageKey("SearchMessage", "AddColumn");
  export const CollectionsCanNotBeAddedAsColumns = new MessageKey("SearchMessage", "CollectionsCanNotBeAddedAsColumns");
  export const InvalidColumnExpression = new MessageKey("SearchMessage", "InvalidColumnExpression");
  export const AddFilter = new MessageKey("SearchMessage", "AddFilter");
  export const AddGroup = new MessageKey("SearchMessage", "AddGroup");
  export const AddValue = new MessageKey("SearchMessage", "AddValue");
  export const DeleteFilter = new MessageKey("SearchMessage", "DeleteFilter");
  export const DeleteAllFilter = new MessageKey("SearchMessage", "DeleteAllFilter");
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
  export const Refresh = new MessageKey("SearchMessage", "Refresh");
  export const Create = new MessageKey("SearchMessage", "Create");
  export const CreateNew0_G = new MessageKey("SearchMessage", "CreateNew0_G");
  export const ThereIsNo0 = new MessageKey("SearchMessage", "ThereIsNo0");
  export const Value = new MessageKey("SearchMessage", "Value");
  export const View = new MessageKey("SearchMessage", "View");
  export const ViewSelected = new MessageKey("SearchMessage", "ViewSelected");
  export const Operations = new MessageKey("SearchMessage", "Operations");
  export const NoResultsFound = new MessageKey("SearchMessage", "NoResultsFound");
  export const NoResultsInThisPage = new MessageKey("SearchMessage", "NoResultsInThisPage");
  export const NoResultsFoundInPage01 = new MessageKey("SearchMessage", "NoResultsFoundInPage01");
  export const GoBackToPageOne = new MessageKey("SearchMessage", "GoBackToPageOne");
  export const Explore = new MessageKey("SearchMessage", "Explore");
  export const PinnedFilter = new MessageKey("SearchMessage", "PinnedFilter");
  export const Label = new MessageKey("SearchMessage", "Label");
  export const Column = new MessageKey("SearchMessage", "Column");
  export const Row = new MessageKey("SearchMessage", "Row");
  export const SplitText = new MessageKey("SearchMessage", "SplitText");
  export const WhenPressedTheFilterWillTakeNoEffectIfTheValueIsNull = new MessageKey("SearchMessage", "WhenPressedTheFilterWillTakeNoEffectIfTheValueIsNull");
  export const WhenPressedTheFilterValueWillBeSplittedAndAllTheWordsHaveToBeFound = new MessageKey("SearchMessage", "WhenPressedTheFilterValueWillBeSplittedAndAllTheWordsHaveToBeFound");
  export const ParentValue = new MessageKey("SearchMessage", "ParentValue");
  export const PleaseSelectA0_G = new MessageKey("SearchMessage", "PleaseSelectA0_G");
  export const PleaseSelectOneOrMore0_G = new MessageKey("SearchMessage", "PleaseSelectOneOrMore0_G");
  export const PleaseSelectAnEntity = new MessageKey("SearchMessage", "PleaseSelectAnEntity");
  export const PleaseSelectOneOrSeveralEntities = new MessageKey("SearchMessage", "PleaseSelectOneOrSeveralEntities");
  export const _0FiltersCollapsed = new MessageKey("SearchMessage", "_0FiltersCollapsed");
  export const DisplayName = new MessageKey("SearchMessage", "DisplayName");
  export const ToPreventPerformanceIssuesAutomaticSearchIsDisabledCheckYourFiltersAndThenClickSearchButton = new MessageKey("SearchMessage", "ToPreventPerformanceIssuesAutomaticSearchIsDisabledCheckYourFiltersAndThenClickSearchButton");
  export const PaginationAll_0Elements = new MessageKey("SearchMessage", "PaginationAll_0Elements");
  export const PaginationPages_0Of01lements = new MessageKey("SearchMessage", "PaginationPages_0Of01lements");
  export const PaginationFirst_01Elements = new MessageKey("SearchMessage", "PaginationFirst_01Elements");
  export const ReturnNewEntity = new MessageKey("SearchMessage", "ReturnNewEntity");
  export const DoYouWantToSelectTheNew01_G = new MessageKey("SearchMessage", "DoYouWantToSelectTheNew01_G");
  export const ShowPinnedFiltersOptions = new MessageKey("SearchMessage", "ShowPinnedFiltersOptions");
  export const HidePinnedFiltersOptions = new MessageKey("SearchMessage", "HidePinnedFiltersOptions");
  export const SummaryHeader = new MessageKey("SearchMessage", "SummaryHeader");
  export const SummaryHeaderMustBeAnAggregate = new MessageKey("SearchMessage", "SummaryHeaderMustBeAnAggregate");
  export const HiddenColumn = new MessageKey("SearchMessage", "HiddenColumn");
  export const ShowHiddenColumns = new MessageKey("SearchMessage", "ShowHiddenColumns");
  export const HideHiddenColumns = new MessageKey("SearchMessage", "HideHiddenColumns");
  export const GroupKey = new MessageKey("SearchMessage", "GroupKey");
  export const DerivedGroupKey = new MessageKey("SearchMessage", "DerivedGroupKey");
  export const Copy = new MessageKey("SearchMessage", "Copy");
  export const MoreThanOne0Selected = new MessageKey("SearchMessage", "MoreThanOne0Selected");
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
  export const _0Selector = new MessageKey("SelectorMessage", "_0Selector");
  export const PleaseChooseA0ToContinue = new MessageKey("SelectorMessage", "PleaseChooseA0ToContinue");
  export const CreationOf0Cancelled = new MessageKey("SelectorMessage", "CreationOf0Cancelled");
  export const ChooseValues = new MessageKey("SelectorMessage", "ChooseValues");
  export const PleaseSelectAtLeastOneValueToContinue = new MessageKey("SelectorMessage", "PleaseSelectAtLeastOneValueToContinue");
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
  export const _0ShouldBe1 = new MessageKey("ValidationMessage", "_0ShouldBe1");
  export const _0ShouldBe1InsteadOf2 = new MessageKey("ValidationMessage", "_0ShouldBe1InsteadOf2");
  export const _0HasToBeBetween1And2 = new MessageKey("ValidationMessage", "_0HasToBeBetween1And2");
  export const _0HasToBeLowercase = new MessageKey("ValidationMessage", "_0HasToBeLowercase");
  export const _0HasToBeUppercase = new MessageKey("ValidationMessage", "_0HasToBeUppercase");
  export const _0IsNecessary = new MessageKey("ValidationMessage", "_0IsNecessary");
  export const _0IsNecessaryOnState1 = new MessageKey("ValidationMessage", "_0IsNecessaryOnState1");
  export const _0IsNotAllowed = new MessageKey("ValidationMessage", "_0IsNotAllowed");
  export const _0IsNotAllowedOnState1 = new MessageKey("ValidationMessage", "_0IsNotAllowedOnState1");
  export const _0IsNotSet = new MessageKey("ValidationMessage", "_0IsNotSet");
  export const _0IsNotSetIn1 = new MessageKey("ValidationMessage", "_0IsNotSetIn1");
  export const _0AreNotSet = new MessageKey("ValidationMessage", "_0AreNotSet");
  export const _0IsSet = new MessageKey("ValidationMessage", "_0IsSet");
  export const _0IsNotA1_G = new MessageKey("ValidationMessage", "_0IsNotA1_G");
  export const BeA0_G = new MessageKey("ValidationMessage", "BeA0_G");
  export const Be0 = new MessageKey("ValidationMessage", "Be0");
  export const BeBetween0And1 = new MessageKey("ValidationMessage", "BeBetween0And1");
  export const BeNotNull = new MessageKey("ValidationMessage", "BeNotNull");
  export const FileName = new MessageKey("ValidationMessage", "FileName");
  export const Have0Decimals = new MessageKey("ValidationMessage", "Have0Decimals");
  export const HaveANumberOfElements01 = new MessageKey("ValidationMessage", "HaveANumberOfElements01");
  export const HaveAPrecisionOf0 = new MessageKey("ValidationMessage", "HaveAPrecisionOf0");
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
  export const _0HasAPrecisionOf1InsteadOf2 = new MessageKey("ValidationMessage", "_0HasAPrecisionOf1InsteadOf2");
  export const _0ShouldBeOfType1 = new MessageKey("ValidationMessage", "_0ShouldBeOfType1");
  export const _0ShouldNotBeOfType1 = new MessageKey("ValidationMessage", "_0ShouldNotBeOfType1");
  export const _0And1CanNotBeSetAtTheSameTime = new MessageKey("ValidationMessage", "_0And1CanNotBeSetAtTheSameTime");
  export const _0Or1ShouldBeSet = new MessageKey("ValidationMessage", "_0Or1ShouldBeSet");
  export const _0And1And2CanNotBeSetAtTheSameTime = new MessageKey("ValidationMessage", "_0And1And2CanNotBeSetAtTheSameTime");
  export const _0Have1ElementsButAllowedOnly2 = new MessageKey("ValidationMessage", "_0Have1ElementsButAllowedOnly2");
  export const _0IsEmpty = new MessageKey("ValidationMessage", "_0IsEmpty");
  export const _0ShouldBeEmpty = new MessageKey("ValidationMessage", "_0ShouldBeEmpty");
  export const _AtLeastOneValueIsNeeded = new MessageKey("ValidationMessage", "_AtLeastOneValueIsNeeded");
  export const PowerOf = new MessageKey("ValidationMessage", "PowerOf");
  export const BeAString = new MessageKey("ValidationMessage", "BeAString");
  export const BeAMultilineString = new MessageKey("ValidationMessage", "BeAMultilineString");
  export const IsATimeOfTheDay = new MessageKey("ValidationMessage", "IsATimeOfTheDay");
  export const ThereAre0InState1 = new MessageKey("ValidationMessage", "ThereAre0InState1");
  export const ThereAre0ThatReferenceThis1 = new MessageKey("ValidationMessage", "ThereAre0ThatReferenceThis1");
  export const _0IsNotCompatibleWith1 = new MessageKey("ValidationMessage", "_0IsNotCompatibleWith1");
  export const _0IsRepeated = new MessageKey("ValidationMessage", "_0IsRepeated");
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


