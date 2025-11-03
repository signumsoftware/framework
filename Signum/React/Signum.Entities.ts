//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from './Reflection'
import * as Entities from './Signum.Entities'

import { getLambdaMembers } from './Reflection'

export interface ModifiableEntity {
  Type: string;
  toStr: string | undefined;
  modified: boolean;
  isNew: boolean | undefined; //required in embedded to remove and re-create in EntityJsonSerializer
  temporalId: string;
  error?: { [member: string]: string };
  propsMeta?: string[];
  mixins?: { [name: string]: MixinEntity }
}

export function liteKeyLong(lite: Lite<Entity>): string {
  return lite.EntityType + ";" + (lite.id == undefined ? "" : lite.id) + ";" + getToString(lite);
}

export interface Entity extends ModifiableEntity {
  id: number | string | undefined;
  ticks: string | undefined; //max value
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

export function translated<T extends ModifiableEntity, S extends string | null | undefined>(entity: T, field: (e: T) => S): S {
  var members = getLambdaMembers(field);

  if (members.length != 1 || members[0].type != 'Member')
    throw new Error("Invalid lambda");

  const prop = members[0].name;

  return (entity as any)[prop + "_translated"] as S ?? (entity as any)[prop] as S;
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
  partitionId?: number;

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


export const toStringDictionary: { [name: string]: ((entity: any) => string) | null } = {};

export function registerToString<T extends ModifiableEntity>(type: Type<T>, toStringFunc: ((e: T) => string) | null): void {
  toStringDictionary[type.typeName] = toStringFunc as ((e: ModifiableEntity) => string) | null;
}


export function registerCustomModelConsturctor<T extends Entity, M extends ModelEntity>(type: Type<T>, modelType: Type<T>, constructLiteModel: ((e: T) => M)): void {
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

export function newNiceName(ti: Reflection.TypeInfo): string {
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
    symbolNiceName: Reflection.symbolNiceName,
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

export function liteKey(lite: Lite<Entity>): string {
  return lite.EntityType + ";" + (lite.id == undefined ? "" : lite.id);
}

export function parseLite(lite: string): Lite<Entity> {

  const type = lite.before(";");
  const rest = lite.after(";");
  if (rest.contains(";")) {
    return {
      EntityType: type,
      id: rest.before(";"),
      model: rest.after(";")
    }
  }
  else {
    return {
      EntityType: type,
      id: rest,
    }
  }
}

export const liteKeyRegEx: RegExp = /^([a-zA-Z]+)[;]([0-9a-zA-Z-]+)$/;
export function parseLiteList(text: string): Lite<Entity>[] {
  const lines = text.split("|");
  const liteKeys = lines.map(l => liteKeyRegEx.test(l) ? l : null).notNull();
  const lites = liteKeys.map(lk => parseLite(lk)).filter(l => isLite(l));

  return lites;
}

export function is<T extends Entity>(a: Lite<T> | T | null | undefined, b: Lite<T> | T | null | undefined, compareTicks = false, assertTypesFound = true): boolean {

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

export function entityInfo(entity: ModifiableEntity | Lite<Entity> | null | undefined): string {
  if (!entity)
    return "undefined";

  const type = isLite(entity) ? entity.EntityType : entity.Type;
  const id = isLite(entity) ? entity.id : isEntity(entity) ? entity.id : "";
  const isNew = isLite(entity) ? entity.entity && entity.entity.isNew : entity.isNew;

  return `${type};${id || ""};${isNew || ""}`;
}

export const BigStringEmbedded: Type<BigStringEmbedded> = new Type<BigStringEmbedded>("BigStringEmbedded");
export interface BigStringEmbedded extends EmbeddedEntity {
  Type: "BigStringEmbedded";
  text: string | null;
}

export const BooleanEnum: EnumType<BooleanEnum> = new EnumType<BooleanEnum>("BooleanEnum");
export type BooleanEnum =
  "False" |
  "True";

export namespace CalendarMessage {
  export const Today: MessageKey = new MessageKey("CalendarMessage", "Today");
}

export namespace ConnectionMessage {
  export const VersionInfo: MessageKey = new MessageKey("ConnectionMessage", "VersionInfo");
  export const ANewVersionHasJustBeenDeployedSaveChangesAnd0: MessageKey = new MessageKey("ConnectionMessage", "ANewVersionHasJustBeenDeployedSaveChangesAnd0");
  export const OutdatedClientApplication: MessageKey = new MessageKey("ConnectionMessage", "OutdatedClientApplication");
  export const ANewVersionHasJustBeenDeployedConsiderReload: MessageKey = new MessageKey("ConnectionMessage", "ANewVersionHasJustBeenDeployedConsiderReload");
  export const Refresh: MessageKey = new MessageKey("ConnectionMessage", "Refresh");
}

export namespace ContainerToggleMessage {
  export const Compress: MessageKey = new MessageKey("ContainerToggleMessage", "Compress");
  export const Expand: MessageKey = new MessageKey("ContainerToggleMessage", "Expand");
}

export const CorruptMixin: Type<CorruptMixin> = new Type<CorruptMixin>("CorruptMixin");
export interface CorruptMixin extends MixinEntity {
  Type: "CorruptMixin";
  corrupt: boolean;
}

export interface EmbeddedEntity extends ModifiableEntity {
}

export namespace EngineMessage {
  export const ConcurrencyErrorOnDatabaseTable0Id1: MessageKey = new MessageKey("EngineMessage", "ConcurrencyErrorOnDatabaseTable0Id1");
  export const EntityWithType0AndId1NotFound: MessageKey = new MessageKey("EngineMessage", "EntityWithType0AndId1NotFound");
  export const NoWayOfMappingType0Found: MessageKey = new MessageKey("EngineMessage", "NoWayOfMappingType0Found");
  export const TheEntity0IsNew: MessageKey = new MessageKey("EngineMessage", "TheEntity0IsNew");
  export const ThereAre0ThatReferThisEntityByProperty1: MessageKey = new MessageKey("EngineMessage", "ThereAre0ThatReferThisEntityByProperty1");
  export const ThereAreRecordsIn0PointingToThisTableByColumn1: MessageKey = new MessageKey("EngineMessage", "ThereAreRecordsIn0PointingToThisTableByColumn1");
  export const UnauthorizedAccessTo0Because1: MessageKey = new MessageKey("EngineMessage", "UnauthorizedAccessTo0Because1");
  export const ThereIsAlreadyA0WithTheSame1_G: MessageKey = new MessageKey("EngineMessage", "ThereIsAlreadyA0WithTheSame1_G");
  export const ThereIsAlreadyA0With1EqualsTo2_G: MessageKey = new MessageKey("EngineMessage", "ThereIsAlreadyA0With1EqualsTo2_G");
}

export namespace EntityControlMessage {
  export const Create: MessageKey = new MessageKey("EntityControlMessage", "Create");
  export const Find: MessageKey = new MessageKey("EntityControlMessage", "Find");
  export const Detail: MessageKey = new MessageKey("EntityControlMessage", "Detail");
  export const MoveDown: MessageKey = new MessageKey("EntityControlMessage", "MoveDown");
  export const MoveUp: MessageKey = new MessageKey("EntityControlMessage", "MoveUp");
  export const MoveRight: MessageKey = new MessageKey("EntityControlMessage", "MoveRight");
  export const MoveLeft: MessageKey = new MessageKey("EntityControlMessage", "MoveLeft");
  export const Move: MessageKey = new MessageKey("EntityControlMessage", "Move");
  export const MoveWithDragAndDropOrCtrlUpDown: MessageKey = new MessageKey("EntityControlMessage", "MoveWithDragAndDropOrCtrlUpDown");
  export const MoveWithDragAndDropOrCtrlLeftRight: MessageKey = new MessageKey("EntityControlMessage", "MoveWithDragAndDropOrCtrlLeftRight");
  export const Navigate: MessageKey = new MessageKey("EntityControlMessage", "Navigate");
  export const Remove: MessageKey = new MessageKey("EntityControlMessage", "Remove");
  export const View: MessageKey = new MessageKey("EntityControlMessage", "View");
  export const Add: MessageKey = new MessageKey("EntityControlMessage", "Add");
  export const Paste: MessageKey = new MessageKey("EntityControlMessage", "Paste");
  export const PreviousValueWas0: MessageKey = new MessageKey("EntityControlMessage", "PreviousValueWas0");
  export const Moved: MessageKey = new MessageKey("EntityControlMessage", "Moved");
  export const Removed0: MessageKey = new MessageKey("EntityControlMessage", "Removed0");
  export const NoChanges: MessageKey = new MessageKey("EntityControlMessage", "NoChanges");
  export const Changed: MessageKey = new MessageKey("EntityControlMessage", "Changed");
  export const Added: MessageKey = new MessageKey("EntityControlMessage", "Added");
  export const RemovedAndSelectedAgain: MessageKey = new MessageKey("EntityControlMessage", "RemovedAndSelectedAgain");
  export const Selected: MessageKey = new MessageKey("EntityControlMessage", "Selected");
  export const Edit: MessageKey = new MessageKey("EntityControlMessage", "Edit");
  export const Reload: MessageKey = new MessageKey("EntityControlMessage", "Reload");
  export const Download: MessageKey = new MessageKey("EntityControlMessage", "Download");
  export const Expand: MessageKey = new MessageKey("EntityControlMessage", "Expand");
  export const Collapse: MessageKey = new MessageKey("EntityControlMessage", "Collapse");
  export const ToggleSideBar: MessageKey = new MessageKey("EntityControlMessage", "ToggleSideBar");
  export const Maximize: MessageKey = new MessageKey("EntityControlMessage", "Maximize");
  export const Minimize: MessageKey = new MessageKey("EntityControlMessage", "Minimize");
  export const _0Characters: MessageKey = new MessageKey("EntityControlMessage", "_0Characters");
  export const _0CharactersRemaining: MessageKey = new MessageKey("EntityControlMessage", "_0CharactersRemaining");
  export const Close: MessageKey = new MessageKey("EntityControlMessage", "Close");
}

export namespace FontSizeMessage {
  export const FontSize: MessageKey = new MessageKey("FontSizeMessage", "FontSize");
  export const ReduceFontSize: MessageKey = new MessageKey("FontSizeMessage", "ReduceFontSize");
  export const ResetFontSize: MessageKey = new MessageKey("FontSizeMessage", "ResetFontSize");
  export const IncreaseFontSize: MessageKey = new MessageKey("FontSizeMessage", "IncreaseFontSize");
}

export namespace FrameMessage {
  export const New0_G: MessageKey = new MessageKey("FrameMessage", "New0_G");
  export const Copied: MessageKey = new MessageKey("FrameMessage", "Copied");
  export const CopyToClipboard: MessageKey = new MessageKey("FrameMessage", "CopyToClipboard");
  export const Fullscreen: MessageKey = new MessageKey("FrameMessage", "Fullscreen");
  export const ThereAreErrors: MessageKey = new MessageKey("FrameMessage", "ThereAreErrors");
  export const Main: MessageKey = new MessageKey("FrameMessage", "Main");
}

export namespace HtmlEditorMessage {
  export const Hyperlink: MessageKey = new MessageKey("HtmlEditorMessage", "Hyperlink");
  export const EnterYourUrlHere: MessageKey = new MessageKey("HtmlEditorMessage", "EnterYourUrlHere");
  export const Bold: MessageKey = new MessageKey("HtmlEditorMessage", "Bold");
  export const Italic: MessageKey = new MessageKey("HtmlEditorMessage", "Italic");
  export const Underline: MessageKey = new MessageKey("HtmlEditorMessage", "Underline");
  export const Headings: MessageKey = new MessageKey("HtmlEditorMessage", "Headings");
  export const UnorderedList: MessageKey = new MessageKey("HtmlEditorMessage", "UnorderedList");
  export const OrderedList: MessageKey = new MessageKey("HtmlEditorMessage", "OrderedList");
  export const Quote: MessageKey = new MessageKey("HtmlEditorMessage", "Quote");
  export const CodeBlock: MessageKey = new MessageKey("HtmlEditorMessage", "CodeBlock");
  export const Code: MessageKey = new MessageKey("HtmlEditorMessage", "Code");
}

export interface ImmutableEntity extends Entity {
  allowChange: boolean;
}

export namespace JavascriptMessage {
  export const chooseAType: MessageKey = new MessageKey("JavascriptMessage", "chooseAType");
  export const chooseAValue: MessageKey = new MessageKey("JavascriptMessage", "chooseAValue");
  export const addFilter: MessageKey = new MessageKey("JavascriptMessage", "addFilter");
  export const openTab: MessageKey = new MessageKey("JavascriptMessage", "openTab");
  export const error: MessageKey = new MessageKey("JavascriptMessage", "error");
  export const executed: MessageKey = new MessageKey("JavascriptMessage", "executed");
  export const hideFilters: MessageKey = new MessageKey("JavascriptMessage", "hideFilters");
  export const showFilters: MessageKey = new MessageKey("JavascriptMessage", "showFilters");
  export const groupResults: MessageKey = new MessageKey("JavascriptMessage", "groupResults");
  export const ungroupResults: MessageKey = new MessageKey("JavascriptMessage", "ungroupResults");
  export const ShowGroup: MessageKey = new MessageKey("JavascriptMessage", "ShowGroup");
  export const activateTimeMachine: MessageKey = new MessageKey("JavascriptMessage", "activateTimeMachine");
  export const deactivateTimeMachine: MessageKey = new MessageKey("JavascriptMessage", "deactivateTimeMachine");
  export const showRecords: MessageKey = new MessageKey("JavascriptMessage", "showRecords");
  export const joinMode: MessageKey = new MessageKey("JavascriptMessage", "joinMode");
  export const loading: MessageKey = new MessageKey("JavascriptMessage", "loading");
  export const noActionsFound: MessageKey = new MessageKey("JavascriptMessage", "noActionsFound");
  export const saveChangesBeforeOrPressCancel: MessageKey = new MessageKey("JavascriptMessage", "saveChangesBeforeOrPressCancel");
  export const loseCurrentChanges: MessageKey = new MessageKey("JavascriptMessage", "loseCurrentChanges");
  export const noElementsSelected: MessageKey = new MessageKey("JavascriptMessage", "noElementsSelected");
  export const searchForResults: MessageKey = new MessageKey("JavascriptMessage", "searchForResults");
  export const selectOnlyOneElement: MessageKey = new MessageKey("JavascriptMessage", "selectOnlyOneElement");
  export const popupErrors: MessageKey = new MessageKey("JavascriptMessage", "popupErrors");
  export const popupErrorsStop: MessageKey = new MessageKey("JavascriptMessage", "popupErrorsStop");
  export const insertColumn: MessageKey = new MessageKey("JavascriptMessage", "insertColumn");
  export const editColumn: MessageKey = new MessageKey("JavascriptMessage", "editColumn");
  export const removeColumn: MessageKey = new MessageKey("JavascriptMessage", "removeColumn");
  export const groupByThisColumn: MessageKey = new MessageKey("JavascriptMessage", "groupByThisColumn");
  export const removeOtherColumns: MessageKey = new MessageKey("JavascriptMessage", "removeOtherColumns");
  export const restoreDefaultColumns: MessageKey = new MessageKey("JavascriptMessage", "restoreDefaultColumns");
  export const saved: MessageKey = new MessageKey("JavascriptMessage", "saved");
  export const search: MessageKey = new MessageKey("JavascriptMessage", "search");
  export const Selected: MessageKey = new MessageKey("JavascriptMessage", "Selected");
  export const selectToken: MessageKey = new MessageKey("JavascriptMessage", "selectToken");
  export const find: MessageKey = new MessageKey("JavascriptMessage", "find");
  export const remove: MessageKey = new MessageKey("JavascriptMessage", "remove");
  export const view: MessageKey = new MessageKey("JavascriptMessage", "view");
  export const create: MessageKey = new MessageKey("JavascriptMessage", "create");
  export const moveDown: MessageKey = new MessageKey("JavascriptMessage", "moveDown");
  export const moveUp: MessageKey = new MessageKey("JavascriptMessage", "moveUp");
  export const navigate: MessageKey = new MessageKey("JavascriptMessage", "navigate");
  export const newEntity: MessageKey = new MessageKey("JavascriptMessage", "newEntity");
  export const ok: MessageKey = new MessageKey("JavascriptMessage", "ok");
  export const cancel: MessageKey = new MessageKey("JavascriptMessage", "cancel");
  export const showPeriod: MessageKey = new MessageKey("JavascriptMessage", "showPeriod");
  export const showPreviousOperation: MessageKey = new MessageKey("JavascriptMessage", "showPreviousOperation");
  export const Date: MessageKey = new MessageKey("JavascriptMessage", "Date");
}

export namespace LiteMessage {
  export const IdNotValid: MessageKey = new MessageKey("LiteMessage", "IdNotValid");
  export const InvalidFormat: MessageKey = new MessageKey("LiteMessage", "InvalidFormat");
  export const Type0NotFound: MessageKey = new MessageKey("LiteMessage", "Type0NotFound");
  export const ToStr: MessageKey = new MessageKey("LiteMessage", "ToStr");
}

export interface ModelEntity extends ModifiableEntity {
}

export namespace NormalControlMessage {
  export const ViewForType0IsNotAllowed: MessageKey = new MessageKey("NormalControlMessage", "ViewForType0IsNotAllowed");
  export const SaveChangesFirst: MessageKey = new MessageKey("NormalControlMessage", "SaveChangesFirst");
  export const CopyEntityTypeAndIdForAutocomplete: MessageKey = new MessageKey("NormalControlMessage", "CopyEntityTypeAndIdForAutocomplete");
  export const CopyEntityUrl: MessageKey = new MessageKey("NormalControlMessage", "CopyEntityUrl");
}

export namespace OperationMessage {
  export const Create: MessageKey = new MessageKey("OperationMessage", "Create");
  export const CreateFromRegex: MessageKey = new MessageKey("OperationMessage", "CreateFromRegex");
  export const Create0: MessageKey = new MessageKey("OperationMessage", "Create0");
  export const StateShouldBe0InsteadOf1: MessageKey = new MessageKey("OperationMessage", "StateShouldBe0InsteadOf1");
  export const TheStateOf0ShouldBe1InsteadOf2: MessageKey = new MessageKey("OperationMessage", "TheStateOf0ShouldBe1InsteadOf2");
  export const InUserInterface: MessageKey = new MessageKey("OperationMessage", "InUserInterface");
  export const Operation01IsNotAuthorized: MessageKey = new MessageKey("OperationMessage", "Operation01IsNotAuthorized");
  export const Confirm: MessageKey = new MessageKey("OperationMessage", "Confirm");
  export const PleaseConfirmYouWouldLikeToDelete0FromTheSystem: MessageKey = new MessageKey("OperationMessage", "PleaseConfirmYouWouldLikeToDelete0FromTheSystem");
  export const PleaseConfirmYouWouldLikeTo01: MessageKey = new MessageKey("OperationMessage", "PleaseConfirmYouWouldLikeTo01");
  export const TheOperation0DidNotReturnAnEntity: MessageKey = new MessageKey("OperationMessage", "TheOperation0DidNotReturnAnEntity");
  export const Logs: MessageKey = new MessageKey("OperationMessage", "Logs");
  export const PreviousOperationLog: MessageKey = new MessageKey("OperationMessage", "PreviousOperationLog");
  export const LastOperationLog: MessageKey = new MessageKey("OperationMessage", "LastOperationLog");
  export const _0AndClose: MessageKey = new MessageKey("OperationMessage", "_0AndClose");
  export const _0AndNew: MessageKey = new MessageKey("OperationMessage", "_0AndNew");
  export const BulkModifications: MessageKey = new MessageKey("OperationMessage", "BulkModifications");
  export const PleaseConfirmThatYouWouldLikeToApplyTheAboveChangesAndExecute0Over12: MessageKey = new MessageKey("OperationMessage", "PleaseConfirmThatYouWouldLikeToApplyTheAboveChangesAndExecute0Over12");
  export const Condition: MessageKey = new MessageKey("OperationMessage", "Condition");
  export const Setters: MessageKey = new MessageKey("OperationMessage", "Setters");
  export const AddSetter: MessageKey = new MessageKey("OperationMessage", "AddSetter");
  export const MultiSetter: MessageKey = new MessageKey("OperationMessage", "MultiSetter");
  export const Deleting: MessageKey = new MessageKey("OperationMessage", "Deleting");
  export const Executing0: MessageKey = new MessageKey("OperationMessage", "Executing0");
  export const _0Errors: MessageKey = new MessageKey("OperationMessage", "_0Errors");
  export const ClosingThisModalOrBrowserTabWillCancelTheOperation: MessageKey = new MessageKey("OperationMessage", "ClosingThisModalOrBrowserTabWillCancelTheOperation");
  export const CancelOperation: MessageKey = new MessageKey("OperationMessage", "CancelOperation");
  export const AreYouSureYouWantToCancelTheOperation: MessageKey = new MessageKey("OperationMessage", "AreYouSureYouWantToCancelTheOperation");
  export const Operation: MessageKey = new MessageKey("OperationMessage", "Operation");
}

export namespace PaginationMessage {
  export const All: MessageKey = new MessageKey("PaginationMessage", "All");
}

export namespace QuickLinkMessage {
  export const Quicklinks: MessageKey = new MessageKey("QuickLinkMessage", "Quicklinks");
  export const No0Found: MessageKey = new MessageKey("QuickLinkMessage", "No0Found");
}

export namespace ReactWidgetsMessage {
  export const MoveToday: MessageKey = new MessageKey("ReactWidgetsMessage", "MoveToday");
  export const MoveBack: MessageKey = new MessageKey("ReactWidgetsMessage", "MoveBack");
  export const MoveForward: MessageKey = new MessageKey("ReactWidgetsMessage", "MoveForward");
  export const DateButton: MessageKey = new MessageKey("ReactWidgetsMessage", "DateButton");
  export const OpenCombobox: MessageKey = new MessageKey("ReactWidgetsMessage", "OpenCombobox");
  export const FilterPlaceholder: MessageKey = new MessageKey("ReactWidgetsMessage", "FilterPlaceholder");
  export const EmptyList: MessageKey = new MessageKey("ReactWidgetsMessage", "EmptyList");
  export const EmptyFilter: MessageKey = new MessageKey("ReactWidgetsMessage", "EmptyFilter");
  export const CreateOption: MessageKey = new MessageKey("ReactWidgetsMessage", "CreateOption");
  export const CreateOption0: MessageKey = new MessageKey("ReactWidgetsMessage", "CreateOption0");
  export const TagsLabel: MessageKey = new MessageKey("ReactWidgetsMessage", "TagsLabel");
  export const RemoveLabel: MessageKey = new MessageKey("ReactWidgetsMessage", "RemoveLabel");
  export const NoneSelected: MessageKey = new MessageKey("ReactWidgetsMessage", "NoneSelected");
  export const SelectedItems0: MessageKey = new MessageKey("ReactWidgetsMessage", "SelectedItems0");
  export const IncrementValue: MessageKey = new MessageKey("ReactWidgetsMessage", "IncrementValue");
  export const DecrementValue: MessageKey = new MessageKey("ReactWidgetsMessage", "DecrementValue");
}

export namespace SaveChangesMessage {
  export const ThereAreChanges: MessageKey = new MessageKey("SaveChangesMessage", "ThereAreChanges");
  export const YoureTryingToCloseAnEntityWithChanges: MessageKey = new MessageKey("SaveChangesMessage", "YoureTryingToCloseAnEntityWithChanges");
  export const LoseChanges: MessageKey = new MessageKey("SaveChangesMessage", "LoseChanges");
}

export namespace SearchHelpMessage {
  export const SearchHelp: MessageKey = new MessageKey("SearchHelpMessage", "SearchHelp");
  export const SearchControl: MessageKey = new MessageKey("SearchHelpMessage", "SearchControl");
  export const The0IsVeryPowerfulButCanBeIntimidatingTakeSomeTimeToLearnHowToUseItWillBeWorthIt: MessageKey = new MessageKey("SearchHelpMessage", "The0IsVeryPowerfulButCanBeIntimidatingTakeSomeTimeToLearnHowToUseItWillBeWorthIt");
  export const TheBasics: MessageKey = new MessageKey("SearchHelpMessage", "TheBasics");
  export const CurrentlyWeAreInTheQuery0YouCanOpenA1ByClickingThe2IconOrDoing3InTheRowButNotInALink: MessageKey = new MessageKey("SearchHelpMessage", "CurrentlyWeAreInTheQuery0YouCanOpenA1ByClickingThe2IconOrDoing3InTheRowButNotInALink");
  export const CurrentlyWeAreInTheQuery0GroupedBy1YouCanOpenAGroupByClickingThe2IconOrDoing3InTheRowButNotInALink: MessageKey = new MessageKey("SearchHelpMessage", "CurrentlyWeAreInTheQuery0GroupedBy1YouCanOpenAGroupByClickingThe2IconOrDoing3InTheRowButNotInALink");
  export const DoubleClick: MessageKey = new MessageKey("SearchHelpMessage", "DoubleClick");
  export const GroupedBy: MessageKey = new MessageKey("SearchHelpMessage", "GroupedBy");
  export const Doing0InTheRowWillSelectTheEntityAndCloseTheModalAutomaticallyAlternativelyYouCanSelectOneEntityAndClickOK: MessageKey = new MessageKey("SearchHelpMessage", "Doing0InTheRowWillSelectTheEntityAndCloseTheModalAutomaticallyAlternativelyYouCanSelectOneEntityAndClickOK");
  export const YouCanUseThePreparedFiltersOnTheTopToQuicklyFindThe0YouAreLookingFor: MessageKey = new MessageKey("SearchHelpMessage", "YouCanUseThePreparedFiltersOnTheTopToQuicklyFindThe0YouAreLookingFor");
  export const OrderingResults: MessageKey = new MessageKey("SearchHelpMessage", "OrderingResults");
  export const YouCanOrderResultsByClickingInAColumnHeaderDefaultOrderingIs0AndByClickingAgainItChangesTo1YouCanOrderByMoreThanOneColumnIfYouKeep2DownWhenClickingOnTheColumnsHeader: MessageKey = new MessageKey("SearchHelpMessage", "YouCanOrderResultsByClickingInAColumnHeaderDefaultOrderingIs0AndByClickingAgainItChangesTo1YouCanOrderByMoreThanOneColumnIfYouKeep2DownWhenClickingOnTheColumnsHeader");
  export const Ascending: MessageKey = new MessageKey("SearchHelpMessage", "Ascending");
  export const Descending: MessageKey = new MessageKey("SearchHelpMessage", "Descending");
  export const Shift: MessageKey = new MessageKey("SearchHelpMessage", "Shift");
  export const ChangeColumns: MessageKey = new MessageKey("SearchHelpMessage", "ChangeColumns");
  export const YouAreNotLimitedToTheColumnsYouSeeTheDefaultColumnsCanBeChangedBy0InAColumnHeaderAndThenSelect123: MessageKey = new MessageKey("SearchHelpMessage", "YouAreNotLimitedToTheColumnsYouSeeTheDefaultColumnsCanBeChangedBy0InAColumnHeaderAndThenSelect123");
  export const RightClicking: MessageKey = new MessageKey("SearchHelpMessage", "RightClicking");
  export const RightClick: MessageKey = new MessageKey("SearchHelpMessage", "RightClick");
  export const InsertColumn: MessageKey = new MessageKey("SearchHelpMessage", "InsertColumn");
  export const EditColumn: MessageKey = new MessageKey("SearchHelpMessage", "EditColumn");
  export const RemoveColumn: MessageKey = new MessageKey("SearchHelpMessage", "RemoveColumn");
  export const YouCanAlso0TheColumnsByDraggingAndDroppingThemToAnotherPosition: MessageKey = new MessageKey("SearchHelpMessage", "YouCanAlso0TheColumnsByDraggingAndDroppingThemToAnotherPosition");
  export const Rearrange: MessageKey = new MessageKey("SearchHelpMessage", "Rearrange");
  export const WhenInsertingTheNewColumnWillBeAddedBeforeOrAfterTheSelectedColumnDependingWhereYou0: MessageKey = new MessageKey("SearchHelpMessage", "WhenInsertingTheNewColumnWillBeAddedBeforeOrAfterTheSelectedColumnDependingWhereYou0");
  export const ClickOnThe0ButtonToOpenTheAdvancedFiltersThisWillAllowYouCreateComplexFiltersManuallyBySelectingThe1OfTheEntityOrARelatedEntitiesAComparison2AndA3ToCompare: MessageKey = new MessageKey("SearchHelpMessage", "ClickOnThe0ButtonToOpenTheAdvancedFiltersThisWillAllowYouCreateComplexFiltersManuallyBySelectingThe1OfTheEntityOrARelatedEntitiesAComparison2AndA3ToCompare");
  export const TrickYouCan0OnA1AndChoose2ToQuicklyFilterByThisColumnEvenMoreYouCan3ToFilterByThis4Directly: MessageKey = new MessageKey("SearchHelpMessage", "TrickYouCan0OnA1AndChoose2ToQuicklyFilterByThisColumnEvenMoreYouCan3ToFilterByThis4Directly");
  export const ColumnHeader: MessageKey = new MessageKey("SearchHelpMessage", "ColumnHeader");
  export const GroupingResultsByOneOrMoreColumn: MessageKey = new MessageKey("SearchHelpMessage", "GroupingResultsByOneOrMoreColumn");
  export const YouCanGroupResultsBy0InAColumnHeaderAndSelecting1AllTheColumnsWillDisappearExceptTheSelectedOneAndAnAggregationColumnTypically2: MessageKey = new MessageKey("SearchHelpMessage", "YouCanGroupResultsBy0InAColumnHeaderAndSelecting1AllTheColumnsWillDisappearExceptTheSelectedOneAndAnAggregationColumnTypically2");
  export const GroupByThisColumn: MessageKey = new MessageKey("SearchHelpMessage", "GroupByThisColumn");
  export const GroupHelp: MessageKey = new MessageKey("SearchHelpMessage", "GroupHelp");
  export const AnyNewColumnShouldEitherBeAnAggregate0OrItWillBeConsideredANewGroupKey1: MessageKey = new MessageKey("SearchHelpMessage", "AnyNewColumnShouldEitherBeAnAggregate0OrItWillBeConsideredANewGroupKey1");
  export const OnceGroupingYouCanFilterNormallyOrUsingAggregatesAsTheField0: MessageKey = new MessageKey("SearchHelpMessage", "OnceGroupingYouCanFilterNormallyOrUsingAggregatesAsTheField0");
  export const InSql: MessageKey = new MessageKey("SearchHelpMessage", "InSql");
  export const FinallyYouCanStopGroupingBy0InAColumnHeaderAndSelect1: MessageKey = new MessageKey("SearchHelpMessage", "FinallyYouCanStopGroupingBy0InAColumnHeaderAndSelect1");
  export const RestoreDefaultColumns: MessageKey = new MessageKey("SearchHelpMessage", "RestoreDefaultColumns");
  export const AQueryExpressionCouldBeAnyFieldOfThe: MessageKey = new MessageKey("SearchHelpMessage", "AQueryExpressionCouldBeAnyFieldOfThe");
  export const Like: MessageKey = new MessageKey("SearchHelpMessage", "Like");
  export const OrAnyOtherFieldThatYouSeeInThe: MessageKey = new MessageKey("SearchHelpMessage", "OrAnyOtherFieldThatYouSeeInThe");
  export const WhenYouClick: MessageKey = new MessageKey("SearchHelpMessage", "WhenYouClick");
  export const IconOrAnyRelatedEntity: MessageKey = new MessageKey("SearchHelpMessage", "IconOrAnyRelatedEntity");
  export const AQueryExpressionCouldBeAnyColumnOfThe: MessageKey = new MessageKey("SearchHelpMessage", "AQueryExpressionCouldBeAnyColumnOfThe");
  export const OrAnyOtherFieldThatYouSeeInTheProjectWhenYouClick: MessageKey = new MessageKey("SearchHelpMessage", "OrAnyOtherFieldThatYouSeeInTheProjectWhenYouClick");
  export const TheOperationThatWillBeUsedToCompareThe: MessageKey = new MessageKey("SearchHelpMessage", "TheOperationThatWillBeUsedToCompareThe");
  export const WithThe: MessageKey = new MessageKey("SearchHelpMessage", "WithThe");
  export const EqualsDistinctGreaterThan: MessageKey = new MessageKey("SearchHelpMessage", "EqualsDistinctGreaterThan");
  export const Etc: MessageKey = new MessageKey("SearchHelpMessage", "Etc");
  export const TheValueThatWillBeComparedWithThe: MessageKey = new MessageKey("SearchHelpMessage", "TheValueThatWillBeComparedWithThe");
  export const TypicallyHasTheSameTypeAsTheFieldButSomeOperatorsLike: MessageKey = new MessageKey("SearchHelpMessage", "TypicallyHasTheSameTypeAsTheFieldButSomeOperatorsLike");
  export const AllowToSelectMultipleValues: MessageKey = new MessageKey("SearchHelpMessage", "AllowToSelectMultipleValues");
  export const YouAreEditingAColumnLetMeExplainWhatEachFieldDoes: MessageKey = new MessageKey("SearchHelpMessage", "YouAreEditingAColumnLetMeExplainWhatEachFieldDoes");
  export const CanBeUsedAsTheFirstItemCountsTheNumberOfRowsOnEachGroup: MessageKey = new MessageKey("SearchHelpMessage", "CanBeUsedAsTheFirstItemCountsTheNumberOfRowsOnEachGroup");
}

export namespace SearchMessage {
  export const ChooseTheDisplayNameOfTheNewColumn: MessageKey = new MessageKey("SearchMessage", "ChooseTheDisplayNameOfTheNewColumn");
  export const Field: MessageKey = new MessageKey("SearchMessage", "Field");
  export const ColumnField: MessageKey = new MessageKey("SearchMessage", "ColumnField");
  export const AddColumn: MessageKey = new MessageKey("SearchMessage", "AddColumn");
  export const CollectionsCanNotBeAddedAsColumns: MessageKey = new MessageKey("SearchMessage", "CollectionsCanNotBeAddedAsColumns");
  export const InvalidColumnExpression: MessageKey = new MessageKey("SearchMessage", "InvalidColumnExpression");
  export const AddFilter: MessageKey = new MessageKey("SearchMessage", "AddFilter");
  export const AddOrGroup: MessageKey = new MessageKey("SearchMessage", "AddOrGroup");
  export const AddAndGroup: MessageKey = new MessageKey("SearchMessage", "AddAndGroup");
  export const OrGroup: MessageKey = new MessageKey("SearchMessage", "OrGroup");
  export const AndGroup: MessageKey = new MessageKey("SearchMessage", "AndGroup");
  export const GroupPrefix: MessageKey = new MessageKey("SearchMessage", "GroupPrefix");
  export const AddValue: MessageKey = new MessageKey("SearchMessage", "AddValue");
  export const DeleteFilter: MessageKey = new MessageKey("SearchMessage", "DeleteFilter");
  export const DeleteAllFilter: MessageKey = new MessageKey("SearchMessage", "DeleteAllFilter");
  export const Filters: MessageKey = new MessageKey("SearchMessage", "Filters");
  export const Columns: MessageKey = new MessageKey("SearchMessage", "Columns");
  export const Find: MessageKey = new MessageKey("SearchMessage", "Find");
  export const FinderOf0: MessageKey = new MessageKey("SearchMessage", "FinderOf0");
  export const Name: MessageKey = new MessageKey("SearchMessage", "Name");
  export const NewColumnSName: MessageKey = new MessageKey("SearchMessage", "NewColumnSName");
  export const NoActionsFound: MessageKey = new MessageKey("SearchMessage", "NoActionsFound");
  export const NoColumnSelected: MessageKey = new MessageKey("SearchMessage", "NoColumnSelected");
  export const NoFiltersSpecified: MessageKey = new MessageKey("SearchMessage", "NoFiltersSpecified");
  export const Of: MessageKey = new MessageKey("SearchMessage", "Of");
  export const Operator: MessageKey = new MessageKey("SearchMessage", "Operator");
  export const Query0IsNotAllowed: MessageKey = new MessageKey("SearchMessage", "Query0IsNotAllowed");
  export const Query0NotAllowed: MessageKey = new MessageKey("SearchMessage", "Query0NotAllowed");
  export const Query0NotRegistered: MessageKey = new MessageKey("SearchMessage", "Query0NotRegistered");
  export const Rename: MessageKey = new MessageKey("SearchMessage", "Rename");
  export const _0Results_N: MessageKey = new MessageKey("SearchMessage", "_0Results_N");
  export const First0Results_N: MessageKey = new MessageKey("SearchMessage", "First0Results_N");
  export const _01of2Results_N: MessageKey = new MessageKey("SearchMessage", "_01of2Results_N");
  export const _0Rows_N: MessageKey = new MessageKey("SearchMessage", "_0Rows_N");
  export const _0GroupWith1_N: MessageKey = new MessageKey("SearchMessage", "_0GroupWith1_N");
  export const Search: MessageKey = new MessageKey("SearchMessage", "Search");
  export const Refresh: MessageKey = new MessageKey("SearchMessage", "Refresh");
  export const Create: MessageKey = new MessageKey("SearchMessage", "Create");
  export const CreateNew0_G: MessageKey = new MessageKey("SearchMessage", "CreateNew0_G");
  export const ThereIsNo0: MessageKey = new MessageKey("SearchMessage", "ThereIsNo0");
  export const Value: MessageKey = new MessageKey("SearchMessage", "Value");
  export const View: MessageKey = new MessageKey("SearchMessage", "View");
  export const ViewSelected: MessageKey = new MessageKey("SearchMessage", "ViewSelected");
  export const Operations: MessageKey = new MessageKey("SearchMessage", "Operations");
  export const NoResultsFound: MessageKey = new MessageKey("SearchMessage", "NoResultsFound");
  export const NoResultsInThisPage: MessageKey = new MessageKey("SearchMessage", "NoResultsInThisPage");
  export const NoResultsFoundInPage01: MessageKey = new MessageKey("SearchMessage", "NoResultsFoundInPage01");
  export const GoBackToPageOne: MessageKey = new MessageKey("SearchMessage", "GoBackToPageOne");
  export const PinnedFilter: MessageKey = new MessageKey("SearchMessage", "PinnedFilter");
  export const Label: MessageKey = new MessageKey("SearchMessage", "Label");
  export const Column: MessageKey = new MessageKey("SearchMessage", "Column");
  export const ColSpan: MessageKey = new MessageKey("SearchMessage", "ColSpan");
  export const Row: MessageKey = new MessageKey("SearchMessage", "Row");
  export const WhenPressedTheFilterWillTakeNoEffectIfTheValueIsNull: MessageKey = new MessageKey("SearchMessage", "WhenPressedTheFilterWillTakeNoEffectIfTheValueIsNull");
  export const WhenPressedTheFilterValueWillBeSplittedAndAllTheWordsHaveToBeFound: MessageKey = new MessageKey("SearchMessage", "WhenPressedTheFilterValueWillBeSplittedAndAllTheWordsHaveToBeFound");
  export const ParentValue: MessageKey = new MessageKey("SearchMessage", "ParentValue");
  export const PleaseSelectA0_G: MessageKey = new MessageKey("SearchMessage", "PleaseSelectA0_G");
  export const PleaseSelectOneOrMore0_G: MessageKey = new MessageKey("SearchMessage", "PleaseSelectOneOrMore0_G");
  export const PleaseSelectAnEntity: MessageKey = new MessageKey("SearchMessage", "PleaseSelectAnEntity");
  export const PleaseSelectOneOrSeveralEntities: MessageKey = new MessageKey("SearchMessage", "PleaseSelectOneOrSeveralEntities");
  export const _0FiltersCollapsed: MessageKey = new MessageKey("SearchMessage", "_0FiltersCollapsed");
  export const DisplayName: MessageKey = new MessageKey("SearchMessage", "DisplayName");
  export const ToPreventPerformanceIssuesAutomaticSearchIsDisabledCheckYourFiltersAndThenClickSearchButton: MessageKey = new MessageKey("SearchMessage", "ToPreventPerformanceIssuesAutomaticSearchIsDisabledCheckYourFiltersAndThenClickSearchButton");
  export const PaginationAll_0Elements: MessageKey = new MessageKey("SearchMessage", "PaginationAll_0Elements");
  export const PaginationPages_0Of01lements: MessageKey = new MessageKey("SearchMessage", "PaginationPages_0Of01lements");
  export const PaginationFirst_01Elements: MessageKey = new MessageKey("SearchMessage", "PaginationFirst_01Elements");
  export const ReturnNewEntity: MessageKey = new MessageKey("SearchMessage", "ReturnNewEntity");
  export const DoYouWantToSelectTheNew01_G: MessageKey = new MessageKey("SearchMessage", "DoYouWantToSelectTheNew01_G");
  export const EditPinnedFilters: MessageKey = new MessageKey("SearchMessage", "EditPinnedFilters");
  export const PinFilter: MessageKey = new MessageKey("SearchMessage", "PinFilter");
  export const UnpinFilter: MessageKey = new MessageKey("SearchMessage", "UnpinFilter");
  export const IsActive: MessageKey = new MessageKey("SearchMessage", "IsActive");
  export const Split: MessageKey = new MessageKey("SearchMessage", "Split");
  export const SummaryHeader: MessageKey = new MessageKey("SearchMessage", "SummaryHeader");
  export const SummaryHeaderMustBeAnAggregate: MessageKey = new MessageKey("SearchMessage", "SummaryHeaderMustBeAnAggregate");
  export const HiddenColumn: MessageKey = new MessageKey("SearchMessage", "HiddenColumn");
  export const ShowHiddenColumns: MessageKey = new MessageKey("SearchMessage", "ShowHiddenColumns");
  export const HideHiddenColumns: MessageKey = new MessageKey("SearchMessage", "HideHiddenColumns");
  export const ShowMore: MessageKey = new MessageKey("SearchMessage", "ShowMore");
  export const GroupKey: MessageKey = new MessageKey("SearchMessage", "GroupKey");
  export const DerivedGroupKey: MessageKey = new MessageKey("SearchMessage", "DerivedGroupKey");
  export const Copy: MessageKey = new MessageKey("SearchMessage", "Copy");
  export const MoreThanOne0Selected: MessageKey = new MessageKey("SearchMessage", "MoreThanOne0Selected");
  export const CombineRowsWith: MessageKey = new MessageKey("SearchMessage", "CombineRowsWith");
  export const Equal0: MessageKey = new MessageKey("SearchMessage", "Equal0");
  export const SwitchViewMode: MessageKey = new MessageKey("SearchMessage", "SwitchViewMode");
  export const SplitsTheStringValueBySpaceAndSearchesEachPartIndependentlyInAnANDGroup: MessageKey = new MessageKey("SearchMessage", "SplitsTheStringValueBySpaceAndSearchesEachPartIndependentlyInAnANDGroup");
  export const SplitsTheValuesAndSearchesEachOneIndependentlyInAnANDGroup: MessageKey = new MessageKey("SearchMessage", "SplitsTheValuesAndSearchesEachOneIndependentlyInAnANDGroup");
  export const NoResultsFoundBecauseTheRule0DoesNotAllowedToExplore1WithoutFilteringFirst: MessageKey = new MessageKey("SearchMessage", "NoResultsFoundBecauseTheRule0DoesNotAllowedToExplore1WithoutFilteringFirst");
  export const NoResultsFoundBecauseYouAreNotAllowedToExplore0WithoutFilteringBy1First: MessageKey = new MessageKey("SearchMessage", "NoResultsFoundBecauseYouAreNotAllowedToExplore0WithoutFilteringBy1First");
  export const SimpleFilters: MessageKey = new MessageKey("SearchMessage", "SimpleFilters");
  export const AdvancedFilters: MessageKey = new MessageKey("SearchMessage", "AdvancedFilters");
  export const FilterDesigner: MessageKey = new MessageKey("SearchMessage", "FilterDesigner");
  export const TimeMachine: MessageKey = new MessageKey("SearchMessage", "TimeMachine");
  export const Options: MessageKey = new MessageKey("SearchMessage", "Options");
  export const YouHaveSelectedAllRowsOnThisPageDoYouWantTo0OnlyTheseRowsOrToAllRowsAcrossAllPages: MessageKey = new MessageKey("SearchMessage", "YouHaveSelectedAllRowsOnThisPageDoYouWantTo0OnlyTheseRowsOrToAllRowsAcrossAllPages");
  export const CurrentPage: MessageKey = new MessageKey("SearchMessage", "CurrentPage");
  export const AllPages: MessageKey = new MessageKey("SearchMessage", "AllPages");
  export const FilterTypeSelection: MessageKey = new MessageKey("SearchMessage", "FilterTypeSelection");
  export const FilterMenu: MessageKey = new MessageKey("SearchMessage", "FilterMenu");
  export const OperationsForSelectedElements: MessageKey = new MessageKey("SearchMessage", "OperationsForSelectedElements");
  export const PaginationMode: MessageKey = new MessageKey("SearchMessage", "PaginationMode");
  export const NumberOfElementsForPagination: MessageKey = new MessageKey("SearchMessage", "NumberOfElementsForPagination");
  export const SelectAllResults: MessageKey = new MessageKey("SearchMessage", "SelectAllResults");
  export const _0ResultTable: MessageKey = new MessageKey("SearchMessage", "_0ResultTable");
  export const SelectRow0_: MessageKey = new MessageKey("SearchMessage", "SelectRow0_");
  export const Enter: MessageKey = new MessageKey("SearchMessage", "Enter");
}

export namespace SelectorMessage {
  export const ConstructorSelector: MessageKey = new MessageKey("SelectorMessage", "ConstructorSelector");
  export const PleaseChooseAValueToContinue: MessageKey = new MessageKey("SelectorMessage", "PleaseChooseAValueToContinue");
  export const PleaseSelectAConstructor: MessageKey = new MessageKey("SelectorMessage", "PleaseSelectAConstructor");
  export const PleaseSelectAType: MessageKey = new MessageKey("SelectorMessage", "PleaseSelectAType");
  export const TypeSelector: MessageKey = new MessageKey("SelectorMessage", "TypeSelector");
  export const ValueMustBeSpecifiedFor0: MessageKey = new MessageKey("SelectorMessage", "ValueMustBeSpecifiedFor0");
  export const ChooseAValue: MessageKey = new MessageKey("SelectorMessage", "ChooseAValue");
  export const SelectAnElement: MessageKey = new MessageKey("SelectorMessage", "SelectAnElement");
  export const PleaseSelectAnElement: MessageKey = new MessageKey("SelectorMessage", "PleaseSelectAnElement");
  export const _0Selector: MessageKey = new MessageKey("SelectorMessage", "_0Selector");
  export const PleaseChooseA0ToContinue: MessageKey = new MessageKey("SelectorMessage", "PleaseChooseA0ToContinue");
  export const CreationOf0Cancelled: MessageKey = new MessageKey("SelectorMessage", "CreationOf0Cancelled");
  export const ChooseValues: MessageKey = new MessageKey("SelectorMessage", "ChooseValues");
  export const PleaseSelectAtLeastOneValueToContinue: MessageKey = new MessageKey("SelectorMessage", "PleaseSelectAtLeastOneValueToContinue");
}

export namespace SynchronizerMessage {
  export const EndOfSyncScript: MessageKey = new MessageKey("SynchronizerMessage", "EndOfSyncScript");
  export const StartOfSyncScriptGeneratedOn0: MessageKey = new MessageKey("SynchronizerMessage", "StartOfSyncScriptGeneratedOn0");
}

export namespace VoidEnumMessage {
  export const Instance: MessageKey = new MessageKey("VoidEnumMessage", "Instance");
}

