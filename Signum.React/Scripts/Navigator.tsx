import * as React from "react"
import { Dic, classes, } from './Globals';
import { ajaxGet, ajaxPost, clearContextHeaders } from './Services';
import { Lite, Entity, ModifiableEntity, EntityPack, isEntity, isLite, isEntityPack, toLite, liteKey } from './Signum.Entities';
import { IUserEntity, TypeEntity, ExceptionEntity } from './Signum.Entities.Basics';
import { PropertyRoute, PseudoType, Type, getTypeInfo, tryGetTypeInfos, getTypeName, isTypeModel, OperationType, TypeReference, IsByAll, isTypeEntity, tryGetTypeInfo, getTypeInfos, newLite, TypeInfo } from './Reflection';
import { TypeContext } from './TypeContext';
import * as AppContext from './AppContext';
import * as Finder from './Finder';
import * as Operations from './Operations';
import { ViewReplacer } from './Frames/ReactVisitor'
import { AutocompleteConfig, FindOptionsAutocompleteConfig, LiteAutocompleteConfig } from './Lines/AutoCompleteConfig'
import { FindOptions } from './FindOptions'
import { ImportRoute } from "./AsyncImport";
import { NormalWindowMessage } from "./Signum.Entities";
import { BsSize } from "./Components/Basic";
import { ButtonBarManager } from "./Frames/ButtonBar";
import { clearWidgets } from "./Frames/Widgets";
import { clearCustomConstructors } from "./Constructor";
import { toAbsoluteUrl, currentUser } from "./AppContext";
import { useForceUpdate, useAPI, useAPIWithReload } from "./Hooks";
import { ErrorModalOptions, RenderServiceMessageDefault, RenderValidationMessageDefault, RenderMessageDefault } from "./Modals/ErrorModal";


if (!window.__allowNavigatorWithoutUser && (currentUser == null || currentUser.toStr == "Anonymous"))
  throw new Error("To improve intial performance, no dependency to any module that depends on Navigator should be taken for anonymous user. Review your dependencies or write var __allowNavigatorWithoutUser = true in Index.cshtml to disable this check.");

export function start(options: { routes: JSX.Element[] }) {
  options.routes.push(<ImportRoute path="~/view/:type/:id" onImportModule={() => NavigatorManager.getFramePage() } />);
  options.routes.push(<ImportRoute path="~/create/:type" onImportModule={() => NavigatorManager.getFramePage()} />);

  AppContext.clearSettingsActions.push(clearEntitySettings);
  AppContext.clearSettingsActions.push(clearWidgets)
  AppContext.clearSettingsActions.push(ButtonBarManager.clearButtonBarRenderer);
  AppContext.clearSettingsActions.push(clearCustomConstructors);

  ErrorModalOptions.getExceptionUrl = exceptionId => navigateRoute(newLite(ExceptionEntity, exceptionId));
  ErrorModalOptions.isExceptionViewable = () => isViewable(ExceptionEntity);
  ErrorModalOptions.renderServiceMessage = se => <RenderServiceMessageDefault error={se} />;
  ErrorModalOptions.renderValidationMessage = ve => <RenderValidationMessageDefault error={ve} />;
  ErrorModalOptions.renderMessage = e => <RenderMessageDefault error={e} />;
}

export namespace NavigatorManager {
  export function getFramePage() {
    return import("./Frames/FramePage");
  }

  export function getFrameModal() {
    return import("./Frames/FrameModal");
  }
}

export function getTypeTitle(entity: ModifiableEntity, pr: PropertyRoute | undefined) {

  if (isTypeEntity(entity.Type)) {

    const typeInfo = getTypeInfo(entity.Type);

    if (entity.isNew)
      return NormalWindowMessage.New0_G.niceToString().forGenderAndNumber(typeInfo.gender).formatWith(typeInfo.niceName);

    return renderTitle(typeInfo, entity);

  }
  else if (isTypeModel(entity.Type)) {

    const typeInfo = getTypeInfo(entity.Type);
    return typeInfo.niceName;

  } else {

    return pr!.typeReference().typeNiceName;
  }
}

let renderId = (entity: Entity): React.ReactChild => <span className={classes(getTypeInfo(entity.Type).members["Id"].type!.name == "Guid" ? "sf-guid-id" : "")}>{entity.id}</span>;

export function setRenderIdFunction(newFunction: (entity: Entity) => React.ReactChild) {
  renderId = newFunction;
}


let renderTitle = (typeInfo: TypeInfo, entity: ModifiableEntity) => {
  return NormalWindowMessage.Type0Id1.niceToString().formatHtml(typeInfo.niceName, renderId(entity as Entity));
  return null;
}

export function setRenderTitleFunction(newFunction: (typeInfo: TypeInfo, entity: ModifiableEntity) => React.ReactElement | null) {
  renderTitle = newFunction;
}

export function navigateRoute(entity: Entity, viewName?: string): string;
export function navigateRoute(lite: Lite<Entity>, viewName?: string): string;
export function navigateRoute(entityOrLite: Entity | Lite<Entity>, viewName?: string): string {
  let typeName: string;
  let id: number | string | undefined;
  if (isEntity(entityOrLite)) {

    typeName = entityOrLite.Type;
    id = entityOrLite.id;
  }
  else if (isLite(entityOrLite)) {
    typeName = entityOrLite.EntityType;
    id = entityOrLite.id;
  }
  else
    throw new Error("Entity or Lite expected");

  if (id == null)
    throw new Error("No Id");

  const es = getSettings(typeName);
  if (es?.onNavigateRoute)
    return es.onNavigateRoute(typeName, id!, viewName);
  else
    return navigateRouteDefault(typeName, id!, viewName);

}

export function navigateRouteDefault(typeName: string, id: number | string, viewName?: string) {
  return toAbsoluteUrl("~/view/" + typeName.firstLower() + "/" + id + (viewName ? "?viewName=" + viewName : ""));

}

export function createRoute(type: PseudoType, viewName?: string) {
  return toAbsoluteUrl("~/create/" + getTypeName(type) + (viewName ? "?viewName=" + viewName : ""));
}




export function clearEntitySettings() {
  Dic.clear(entitySettings);
}

export const entitySettings: { [type: string]: EntitySettings<ModifiableEntity> } = {};
export function addSettings(...settings: EntitySettings<any>[]) {
  settings.forEach(s => Dic.addOrThrow(entitySettings, s.typeName, s));
}

export function getOrAddSettings<T extends ModifiableEntity>(type: Type<T>): EntitySettings<T>;
export function getOrAddSettings(type: PseudoType): EntitySettings<ModifiableEntity>;
export function getOrAddSettings(type: PseudoType): EntitySettings<ModifiableEntity> {
  const typeName = getTypeName(type);

  return entitySettings[typeName] || (entitySettings[typeName] = new EntitySettings(typeName));
}

export function getSettings<T extends ModifiableEntity>(type: Type<T>): EntitySettings<T> | undefined;
export function getSettings(type: PseudoType): EntitySettings<ModifiableEntity> | undefined;
export function getSettings(type: PseudoType): EntitySettings<ModifiableEntity> | undefined {
  const typeName = getTypeName(type);

  return entitySettings[typeName];
}

export function setViewDispatcher(newDispatcher: ViewDispatcher) {
  viewDispatcher = newDispatcher;
}

export interface ViewDispatcher {
  hasDefaultView(typeName: string): boolean;
  getViewNames(typeName: string): Promise<string[]>;
  getViewPromise(entity: ModifiableEntity, viewName?: string): ViewPromise<ModifiableEntity>;
  getViewOverrides(typeName: string, viewName?: string): Promise<ViewOverride<ModifiableEntity>[]>;
}

export class BasicViewDispatcher implements ViewDispatcher {
  hasDefaultView(typeName: string) {
    const es = getSettings(typeName);
    return (es?.getViewPromise) != null;
  }

  getViewNames(typeName: string) {
    const es = getSettings(typeName);
    return Promise.resolve((es?.namedViews && Dic.getKeys(es.namedViews)) ?? []);
  }

  getViewOverrides(typeName: string, viewName?: string) {
    const es = getSettings(typeName);
    return Promise.resolve(es?.viewOverrides?.filter(a => a.viewName == viewName) ?? []);
  }


  getViewPromise(entity: ModifiableEntity, viewName?: string) {
    const es = getSettings(entity.Type);

    if (!es)
      throw new Error(`No EntitySettings registered for '${entity.Type}'`);

    if (viewName == undefined) {

      if (!es.getViewPromise)
        throw new Error(`The EntitySettings registered for '${entity.Type}' has not getViewPromise`);

      return es.getViewPromise(entity).applyViewOverrides(entity.Type);
    } else {
      var nv = es.namedViews && es.namedViews[viewName];

      if (!nv || !nv.getViewPromise)
        throw new Error(`The EntitySettings registered for '${entity.Type}' has not namedView '${viewName}'`);

      return nv.getViewPromise(entity).applyViewOverrides(entity.Type, viewName);
    }
  }


}

export class DynamicComponentViewDispatcher implements ViewDispatcher {

  hasDefaultView(typeName: string) {
    return true;
  }

  getViewNames(typeName: string) {
    const es = getSettings(typeName);
    return Promise.resolve((es?.namedViews && Dic.getKeys(es.namedViews)) ?? []);
  }

  getViewOverrides(typeName: string, viewName?: string) {
    const es = getSettings(typeName);
    return Promise.resolve(es?.viewOverrides?.filter(a => a.viewName == viewName) ?? []);
  }

  getViewPromise(entity: ModifiableEntity, viewName?: string) {
    const es = getSettings(entity.Type);

    if (viewName == undefined) {

      if (es?.getViewPromise == null)
        return new ViewPromise<ModifiableEntity>(import('./Lines/DynamicComponent'));

      return es.getViewPromise(entity).applyViewOverrides(entity.Type);
    } else {
      if (!es)
        throw new Error(`No EntitySettings registered for '${entity.Type}'`);

      var nv = es.namedViews && es.namedViews[viewName];

      if (!nv || !nv.getViewPromise)
        throw new Error(`The EntitySettings registered for '${entity.Type}' has not namedView '${viewName}'`);

      return nv.getViewPromise(entity).applyViewOverrides(entity.Type, viewName);
    }
  }
}

export let viewDispatcher: ViewDispatcher = new DynamicComponentViewDispatcher();

export function getViewPromise<T extends ModifiableEntity>(entity: T, viewName?: string): ViewPromise<T> {
  return viewDispatcher.getViewPromise(entity, viewName);
}

export const isCreableEvent: Array<(typeName: string, options: IsCreableOptions | undefined) => boolean> = [];

export interface IsCreableOptions {
  customComponent?: boolean;
  isSearch?: boolean;
  isEmbedded?: boolean;
}

export function isCreable(type: PseudoType, options?: IsCreableOptions) {

  const typeName = getTypeName(type);

  const baseIsCreable = checkFlag(typeIsCreable(typeName, options?.isEmbedded), options?.isSearch);

  const hasView = options?.customComponent || viewDispatcher.hasDefaultView(typeName);

  const hasConstructor = hasAllowedConstructor(typeName);

  return baseIsCreable && hasView && hasConstructor && isCreableEvent.every(f => f(typeName, options));
}

function hasAllowedConstructor(typeName: string) {
  const ti = tryGetTypeInfo(typeName);

  if (ti == undefined || ti.operations == undefined)
    return true;

  if (!ti.hasConstructorOperation)
    return true;

  const allowed = Dic.getValues(ti.operations).some(oi => oi.operationType == "Constructor");

  return allowed;
}

function typeIsCreable(typeName: string, isEmbedded?: boolean): EntityWhen {

  const es = entitySettings[typeName];
  if (es != undefined && es.isCreable != undefined)
    return es.isCreable;

  if (isEmbedded)
    return "IsLine";

  const typeInfo = tryGetTypeInfo(typeName);
  if (typeInfo == null)
    return "Never";

  if (typeInfo.kind == "Enum")
    return "Never";

  switch (typeInfo.entityKind) {
    case "SystemString": return "Never";
    case "System": return "Never";
    case "Relational": return "Never";
    case "String": return "IsSearch";
    case "Shared": return "Always";
    case "Main": return "IsSearch";
    case "Part": return "IsLine";
    case "SharedPart": return "IsLine";
    default: return "Never";
  }
}


export const isReadonlyEvent: Array<(typeName: string, entity?: EntityPack<ModifiableEntity>, options?: IsReadonlyOptions) => boolean> = [];

export interface IsReadonlyOptions {
  ignoreTypeIsReadonly?: boolean;
  isEmbedded?: boolean;
}

export function isReadOnly(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>, options?: IsReadonlyOptions) {

  const entityPack = isEntityPack(typeOrEntity) ? typeOrEntity : undefined;

  const typeName = isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type : getTypeName(typeOrEntity as PseudoType);

  const baseIsReadOnly = options?.ignoreTypeIsReadonly ? false : typeIsReadOnly(typeName, options?.isEmbedded);

  return baseIsReadOnly || isReadonlyEvent.some(f => f(typeName, entityPack, options));
}

function typeIsReadOnly(typeName: string, isEmbedded: boolean | undefined): boolean {

  const es = entitySettings[typeName];
  if (es != undefined && es.isReadOnly != undefined)
    return es.isReadOnly;

  if (isEmbedded)
    return false;

  const typeInfo = tryGetTypeInfo(typeName);
  if (typeInfo == undefined)
    return true;

  if (typeInfo.kind == "Enum")
    return true;

  switch (typeInfo.entityKind) {
    case "SystemString": return true;
    case "System": return true;
    case "Relational": return true;
    case "String": return false;
    case "Shared": return false;
    case "Main": return false;
    case "Part": return false;
    case "SharedPart": return false;
    default: return false;
  }
}

export function typeRequiresSaveOperation(typeName: string): boolean {

  const typeInfo = tryGetTypeInfo(typeName);
  if (typeInfo == undefined)
    return false;

  switch (typeInfo.entityKind) {
    case "SystemString": return true;
    case "System": return true;
    case "Relational": return true;
    case "String": return true;
    case "Shared": return true;
    case "Main": return true;
    case "Part": return false;
    case "SharedPart": return false;
    default: return false;
  }
}

export interface IsFindableOptions {
    isSearch?: boolean;
    isEmbedded?: boolean;
}

export function isFindable(type: PseudoType, options?: IsFindableOptions) {

  const typeName = getTypeName(type);

  const baseIsReadOnly = typeIsFindable(typeName, options?.isEmbedded);

  return baseIsReadOnly && Finder.isFindable(typeName, true);
}

function typeIsFindable(typeName: string, isEmbedded: boolean | undefined) {

  const es = entitySettings[typeName];

  if (es != undefined && es.isFindable != undefined)
    return es.isFindable;

  if (isEmbedded)
    return false;

  const typeInfo = tryGetTypeInfo(typeName);
  if (typeInfo == null)
    return false;

  if (typeInfo.kind == "Enum")
    return true;

  switch (typeInfo.entityKind) {
    case "SystemString": return true;
    case "System": return true;
    case "Relational": return false;
    case "String": return true;
    case "Shared": return true;
    case "Main": return true;
    case "Part": return false;
    case "SharedPart": return true;
    default: return false;
  }
}

export const isViewableEvent: Array<(typeName: string, entityPack: EntityPack<ModifiableEntity> | undefined, options: IsViewableOptions | undefined) => boolean> = [];

export interface IsViewableOptions {
  customComponent?: boolean;
  isSearch?: boolean;
  isEmbedded?: boolean;
  buttons?: ViewButtons;
}

export type ViewButtons = "ok_cancel" | "close" | undefined;

export function typeDefaultButtons(typeName: string, isEmbedded: boolean | undefined): ViewButtons {
  if (isEmbedded)
    return "ok_cancel";

  const ti = tryGetTypeInfo(typeName);
  if (ti != null) {
    if (
      ti.entityKind == undefined ||
      ti.entityKind == "Part" ||
      ti.entityKind == "SharedPart")
      return "ok_cancel";
  }

  return "close";
}

export function isViewable(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>, options?: IsViewableOptions): boolean {

  const entityPack = isEntityPack(typeOrEntity) ? typeOrEntity : undefined;

  const typeName = isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type : getTypeName(typeOrEntity as PseudoType);

  const baseTypeName = checkFlag(typeIsViewable(typeName, options?.isEmbedded), options?.isSearch);

  const hasView = options?.customComponent || viewDispatcher.hasDefaultView(typeName);

  return baseTypeName && hasView && isViewableEvent.every(f => f(typeName, entityPack, options));
}

function typeIsViewable(typeName: string, isEmbedded: boolean | undefined): EntityWhen {

  const es = entitySettings[typeName];

  if (es != undefined && es.isViewable != undefined)
    return es.isViewable;

  if (isEmbedded)
    return "IsLine";

  const typeInfo = tryGetTypeInfo(typeName);
  if (typeInfo == null)
    return "Never";

  if (typeInfo.kind == "Enum")
    return "Never";

  switch (typeInfo.entityKind) {
    case "SystemString": return "Never";
    case "System": return "Always";
    case "Relational": return "Never";
    case "String": return "IsSearch";
    case "Shared": return "Always";
    case "Main": return "Always";
    case "Part": return "Always";
    case "SharedPart": return "Always";
    default: return "Never";
  }
}

export function defaultFindOptions(type: TypeReference): FindOptions | undefined {
  if (type.isEmbedded || type.name == IsByAll)
    return undefined;

  const types = tryGetTypeInfos(type);

  if (types.length == 1 && types[0] != null) {
    var s = getSettings(types[0]);

    if (s?.findOptions) {
      return s.findOptions;
    }
  }

  return undefined;
}

export function getAutoComplete(type: TypeReference, findOptions: FindOptions | undefined, ctx: TypeContext<any>, create: boolean, showType?: boolean): AutocompleteConfig<any> | null {
  if (type.isEmbedded || type.name == IsByAll)
    return null;

  var result: AutocompleteConfig<any> | null | undefined = null;

  const types = tryGetTypeInfos(type);

  var s = types.length == 1 && types[0] != null ? getSettings(types[0]) : null;

  if (s && s.autocomplete) {
    result = s.autocomplete(findOptions)
  }

  if (!result) {
    if (findOptions)
      result = new FindOptionsAutocompleteConfig(findOptions, {
        getAutocompleteConstructor: (subStr, rows) => getAutocompleteConstructors(type, subStr, { ctx, foundLites: rows.map(a => a.entity!), findOptions, create: create }) as AutocompleteConstructor<Entity>[]
      });
    else
      result = new LiteAutocompleteConfig((signal, subStr: string) => Finder.API.findLiteLike({
        types: type.name,
        subString: subStr,
        count: 5
      }, signal)
        .then(lites => [...lites, ...(getAutocompleteConstructors(type, subStr, { ctx, foundLites: lites, create: create }) as AutocompleteConstructor<Entity>[])]),
        { showType: showType ?? type.name.contains(",") });
  }

  if (!result.getItemsDelay && s?.autocompleteDelay) {
    result.getItemsDelay = s.autocompleteDelay;
  }

  return result;
}

export interface ViewOptions {
  title?: string;
  propertyRoute?: PropertyRoute;
  readOnly?: boolean;
  modalSize?: BsSize;
  isOperationVisible?: (eoc: Operations.EntityOperationContext<any /*Entity*/>) => boolean;
  validate?: boolean;
  requiresSaveOperation?: boolean;
  avoidPromptLoseChange?: boolean;
  buttons?: ViewButtons;
  getViewPromise?: (entity: ModifiableEntity) => undefined | string | ViewPromise<ModifiableEntity>;
  createNew?: () => Promise<EntityPack<ModifiableEntity> | undefined>;
  allowExchangeEntity?: boolean;
  extraProps?: {};
}

export function view<T extends ModifiableEntity>(options: EntityPack<T>, viewOptions?: ViewOptions): Promise<T | undefined>;
export function view<T extends ModifiableEntity>(entity: T, viewOptions?: ViewOptions): Promise<T | undefined>;
export function view<T extends Entity>(entity: Lite<T>, viewOptions?: ViewOptions): Promise<T | undefined>
export function view(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, viewOptions?: ViewOptions): Promise<ModifiableEntity | undefined>;
export function view(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, viewOptions?: ViewOptions): Promise<ModifiableEntity | undefined> {

  const typeName = isEntityPack(entityOrPack) ? entityOrPack.entity.Type : getTypeName(entityOrPack);

  const es = getSettings(typeName);

  if (es?.onView)
    return es.onView(entityOrPack, viewOptions);
  else
    return viewDefault(entityOrPack, viewOptions);
}

export function viewDefault(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, viewOptions?: ViewOptions) {
  return NavigatorManager.getFrameModal()
    .then(NP => NP.FrameModalManager.openView(entityOrPack, viewOptions ?? {}));
}

export function createInNewTab(pack: EntityPack<ModifiableEntity>) {
  var url = createRoute(pack.entity.Type) + "?waitData=true";
  window.dataForChildWindow = pack;
  var win = window.open(url);
}

export function createNavigateOrTab(pack: EntityPack<Entity> | undefined, event: React.MouseEvent<any>): Promise<void> {
  if (!pack || !pack.entity)
    return Promise.resolve();

  const es = getSettings(pack.entity.Type);
  if (es?.avoidPopup || event.ctrlKey || event.button == 1) {
    createInNewTab(pack);
    return Promise.resolve();
  }
  else {
    return view(pack).then(() => undefined);
  }
}


export function toEntityPack(entityOrEntityPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>): Promise<EntityPack<ModifiableEntity>> {
  if ((entityOrEntityPack as EntityPack<ModifiableEntity>).canExecute)
    return Promise.resolve(entityOrEntityPack as EntityPack<ModifiableEntity>);

  const entity = (entityOrEntityPack as ModifiableEntity).Type ?
    entityOrEntityPack as ModifiableEntity :
    (entityOrEntityPack as Lite<Entity> | EntityPack<ModifiableEntity>).entity;

  if (entity == undefined)
    return API.fetchEntityPack(entityOrEntityPack as Lite<Entity>);

  let ti = tryGetTypeInfo(entity.Type);
  if (ti == null || !ti.requiresEntityPack)
    return Promise.resolve({ entity: cloneEntity(entity), canExecute: {} });

  return API.fetchEntityPackEntity(entity as Entity).then(ep => ({ ...ep, entity: cloneEntity(entity)}));
}

function cloneEntity(obj: any) {
  return JSON.parse(JSON.stringify(obj));
}


export function useFetchInState<T extends Entity>(lite: Lite<T> | null | undefined): T | null | undefined {
  return useAPI(signal =>
    lite == null ? Promise.resolve<T | null | undefined>(lite) :
      API.fetchAndForget(lite),
    [lite && liteKey(lite)]);
}

export function useFetchInStateWithReload<T extends Entity>(lite: Lite<T> | null | undefined): [T | null | undefined, () => void] {
  return useAPIWithReload(signal =>
    lite == null ? Promise.resolve<T | null | undefined>(lite) :
      API.fetchAndForget(lite),
    [lite && liteKey(lite)]);
}

export function useFetchAndRemember<T extends Entity>(lite: Lite<T> | null, onLoaded?: () => void): T | null | undefined {

  const forceUpdate = useForceUpdate();
  React.useEffect(() => {
    if (lite && !lite.entity)
      API.fetchAndRemember(lite)
        .then(() => {
          onLoaded && onLoaded();
          forceUpdate();
        })
        .done();
  }, [lite]);


  if (lite == null)
    return null;

  if (lite.entity == null)
    return undefined;

  return lite.entity;
}

export function useFetchAll<T extends Entity>(type: Type<T>): T[] | undefined {
  return useAPI(signal => API.fetchAll(type), []);
}

export function useLiteToString<T extends Entity>(type: Type<T>, id: number | string): Lite<T> {

  var lite = React.useMemo(() => newLite(type, id), [type, id]);

  useAPI(() => API.fillToStrings(lite), [lite]);

  return lite;
}

export module API {

  export function fillToStrings(...lites: (Lite<Entity> | null | undefined)[]): Promise<void> {
    return fillToStringsArray(lites.filter(l => l != null) as Lite<Entity>[]);
  }

  export function fillToStringsArray(lites: Lite<Entity>[]): Promise<void> {

    const realLites = lites.filter(a => a.toStr == undefined && a.entity == undefined);

    if (!realLites.length)
      return Promise.resolve();

    return ajaxPost<string[]>({ url: "~/api/entityToStrings" }, realLites).then(strs => {
      realLites.forEach((l, i) => l.toStr = strs[i]);
    });
  }

  export function fetchAll<T extends Entity>(type: Type<T>): Promise<Array<T>> {
    return ajaxGet({ url: "~/api/fetchAll/" + type.typeName });
  }


  export function fetchAndRemember<T extends Entity>(lite: Lite<T>): Promise<T> {
    if (lite.entity)
      return Promise.resolve(lite.entity);

    if (lite.id == null)
      throw new Error("Lite has no Id");

    return fetchEntity(lite.EntityType, lite.id).then(e => lite.entity = e as T);
  }

  export function fetchAndForget<T extends Entity>(lite: Lite<T>): Promise<T> {

    if (lite.id == null)
      throw new Error("Lite has no Id");

    return fetchEntity(lite.EntityType, lite.id) as Promise<T>;
  }

  export function fetchEntity<T extends Entity>(type: Type<T>, id: any): Promise<T>;
  export function fetchEntity(type: PseudoType, id: number | string): Promise<Entity>;
  export function fetchEntity(type: PseudoType, id?: number | string): Promise<Entity> {

    const typeName = getTypeName(type);
    let idVal = id;

    return ajaxGet({ url: "~/api/entity/" + typeName + "/" + id });
  }


  export function fetchEntityPack<T extends Entity>(lite: Lite<T>): Promise<EntityPack<T>>;
  export function fetchEntityPack<T extends Entity>(type: Type<T>, id: number | string): Promise<EntityPack<T>>;
  export function fetchEntityPack(type: PseudoType, id: number | string): Promise<EntityPack<Entity>>;
  export function fetchEntityPack(typeOrLite: PseudoType | Lite<any>, id?: any): Promise<EntityPack<Entity>> {

    const typeName = (typeOrLite as Lite<any>).EntityType ?? getTypeName(typeOrLite as PseudoType);
    let idVal = (typeOrLite as Lite<any>).id != null ? (typeOrLite as Lite<any>).id : id;

    return ajaxGet({ url: "~/api/entityPack/" + typeName + "/" + idVal });
  }

  export function fetchEntityPackEntity<T extends Entity>(entity: T): Promise<EntityPack<T>> {
    return ajaxPost({ url: "~/api/entityPackEntity" }, entity);
  }

  export function validateEntity(entity: ModifiableEntity): Promise<void> {
    return ajaxPost({ url: "~/api/validateEntity" }, entity);
  }

  export function getType(typeName: string): Promise<TypeEntity | null> {

    return ajaxGet({ url: `~/api/reflection/typeEntity/${typeName}` });
  }
}


export interface EntitySettingsOptions<T extends ModifiableEntity> {
  isCreable?: EntityWhen;
  isFindable?: boolean;
  isViewable?: EntityWhen;
  isReadOnly?: boolean;
  avoidPopup?: boolean;
  supportsAdditionalTabs?: boolean;

  modalSize?: BsSize;

  autocomplete?: (fo: FindOptions | undefined) => AutocompleteConfig<any> | undefined | null;
  autocompleteDelay?: number;
  autocompleteConstructor?: (str: string, aac: AutocompleteConstructorContext) => AutocompleteConstructor<T> | null;

  getViewPromise?: (entity: T) => ViewPromise<T>;
  onNavigateRoute?: (typeName: string, id: string | number) => string;
  onView?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, viewOptions?: ViewOptions) => Promise<T | undefined>;
  onCreateNew?: (oldEntity: EntityPack<T>) => (Promise<EntityPack<T> | undefined>) | undefined; /*Save An New*/

  namedViews?: NamedViewSettings<T>[];
}

export interface AutocompleteConstructorContext {
  ctx: TypeContext<any>;
  foundLites: Lite<Entity>[];
  findOptions?: FindOptions;
  create: boolean;
}

export interface ViewOverride<T extends ModifiableEntity> {
  viewName?: string;
  override: (replacer: ViewReplacer<T>) => void;
}

export interface AutocompleteConstructor<T extends ModifiableEntity> {
  type: PseudoType;
  onClick: () => Promise<T | Lite<T & Entity> | undefined>;
  customElement?: React.ReactNode;
}

export function getAutocompleteConstructors(tr: TypeReference, str: string, aac: AutocompleteConstructorContext): AutocompleteConstructor<ModifiableEntity>[]{
  return getTypeInfos(tr.name).map(ti => {
    var es = getSettings(ti);
    return es?.autocompleteConstructor && es.autocompleteConstructor(str, aac);
  }).notNull();
}

export class EntitySettings<T extends ModifiableEntity> {
  typeName: string;

  getViewPromise?: (entity: T) => ViewPromise<T>;

  viewOverrides?: Array<ViewOverride<T>>;

  isCreable?: EntityWhen;
  isFindable?: boolean;
  isViewable?: EntityWhen;
  isReadOnly?: boolean;
  avoidPopup!: boolean;
  supportsAdditionalTabs?: boolean;

  modalSize?: BsSize;

  autocomplete?: (fo: FindOptions | undefined) => AutocompleteConfig<any> | undefined | null;
  autocompleteDelay?: number;
  autocompleteConstructor?: (str: string, aac: AutocompleteConstructorContext) => AutocompleteConstructor<T> | null;

  findOptions?: FindOptions;
  onView?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, viewOptions?: ViewOptions) => Promise<T | undefined>;
  onNavigateRoute?: (typeName: string, id: string | number, viewName?: string) => string;

  namedViews?: { [viewName: string]: NamedViewSettings<T> };
  overrideView(override: (replacer: ViewReplacer<T>) => void, viewName?: string) {
    if (this.viewOverrides == undefined)
      this.viewOverrides = [];

    this.viewOverrides.push({ override, viewName });
  }

  constructor(type: Type<T> | string, getViewModule?: (entity: T) => Promise<ViewModule<T>>, options?: EntitySettingsOptions<T>) {

    this.typeName = (type as Type<T>).typeName ?? type as string;
    this.getViewPromise = getViewModule && (entity => new ViewPromise(getViewModule(entity)));

    if (options) {
      var { namedViews, ...rest } = options;
      Dic.assign(this, rest);

      if (namedViews != null)
        this.namedViews = namedViews.toObject(a => a.viewName);
    }
  }

  registerNamedView(settings: NamedViewSettings<T>) {
    if (!this.namedViews)
      this.namedViews = {};

    this.namedViews[settings.viewName] = settings;
  }
}

interface NamedViewSettingsOptions<T extends ModifiableEntity> {
  getViewPromise?: (entity: T) => ViewPromise<T>;
}

export class NamedViewSettings<T extends ModifiableEntity> {
  type: Type<T>

  viewName: string;

  getViewPromise: (entity: T) => ViewPromise<T>;

  constructor(type: Type<T>, viewName: string, getViewModule?: (entity: T) => Promise<ViewModule<T>>, options?: NamedViewSettingsOptions<T>) {
    this.type = type;
    this.viewName = viewName;
    var getViewPromise = (getViewModule && ((entity: T) => new ViewPromise(getViewModule(entity)))) || (options?.getViewPromise);
    if (!getViewPromise)
      throw new Error("setting getViewModule or options.getViewPromise arguments is mandatory");
    this.getViewPromise = getViewPromise;
    Dic.assign(this, options)
  }
}

export type ViewModule<T extends ModifiableEntity> = { default: React.ComponentClass<any /* { ctx: TypeContext<T> }*/> | React.FunctionComponent<any /*{ ctx: TypeContext<T> }*/> };

export class ViewPromise<T extends ModifiableEntity> {
  promise!: Promise<(ctx: TypeContext<T>) => React.ReactElement<any>>;

  constructor(promise?: Promise<ViewModule<T>>) {
    if (promise)
      this.promise = promise
        .then(mod => {
          return (ctx: TypeContext<T>): React.ReactElement<any> => React.createElement(mod.default, { ctx });
        });
  }

  static resolve<T extends ModifiableEntity>(getComponent: (ctx: TypeContext<T>) => React.ReactElement<any>) {
    var result = new ViewPromise<T>();
    result.promise = Promise.resolve(getComponent);
    return result;
  }

  withProps<P>(props: Partial<P>): ViewPromise<T> {

    var result = new ViewPromise<T>();

    result.promise = this.promise.then(func => {
      return (ctx: TypeContext<T>): React.ReactElement<any> => {
        var result = func(ctx);
        return React.cloneElement(result, { ...props });
      };
    });

    return result;
  }

  applyViewOverrides(typeName: string, viewName?: string): ViewPromise<T> {
    this.promise = this.promise.then(func =>
      viewDispatcher.getViewOverrides(typeName, viewName).then(vos => {

        if (vos.length == 0)
          return func;

        return (ctx: TypeContext<T>) => {
          var result = func(ctx);
          var component = result.type as React.ComponentClass<{ ctx: TypeContext<T> }> | React.FunctionComponent<{ ctx: TypeContext<T> }>;
          if (component.prototype.render) {
            monkeyPatchClassComponent<T>(component as React.ComponentClass<{ ctx: TypeContext<T> }>, vos!);
            return result;
          } else {
            var newFunc = surroundFunctionComponent(component as React.FunctionComponent<{ ctx: TypeContext<T> }>, vos)
            return React.createElement(newFunc, result.props);
          }
        };
      }));

    return this;
  }

  static flat<T extends ModifiableEntity>(promise: Promise<ViewPromise<T>>): ViewPromise<T> {
    var result = new ViewPromise<T>();
    result.promise = promise.then(vp => vp.promise);
    return result;
  }
}

function monkeyPatchClassComponent<T extends ModifiableEntity>(component: React.ComponentClass<{ ctx: TypeContext<T> }>, viewOverrides: ViewOverride<T>[]) {

  if (!component.prototype.render)
    throw new Error("render function not defined in " + component);

  if (component.prototype.render.withViewOverrides)
    return;

  const baseRender = component.prototype.render as (this: React.Component<any>) => React.ReactElement<any>;

  component.prototype.render = function (this: React.Component<any, any>) {

    const ctx = this.props.ctx;

    const view = baseRender.call(this);
    if (view == null)
      return null;

    const replacer = new ViewReplacer<T>(view, ctx);
    viewOverrides.forEach(vo => vo.override(replacer));
    return replacer.result;
  };

  component.prototype.render.withViewOverrides = true;
}

export function surroundFunctionComponent<T extends ModifiableEntity>(functionComponent: React.FunctionComponent<{ ctx: TypeContext<T> }>, viewOverrides: ViewOverride<T>[]) {
  var result = function NewComponent(props: { ctx: TypeContext<T> }) {
    var view = functionComponent(props);
    if (view == null)
      return null;

    const replacer = new ViewReplacer<T>(view, props.ctx);
    viewOverrides.forEach(vo => vo.override(replacer));
    return replacer.result;
  };

  Object.defineProperty(result, "name", { value: functionComponent.name + "VO" });

  return result;
}

export function checkFlag(entityWhen: EntityWhen, isSearch: boolean | undefined) {
  return entityWhen == "Always" ||
    entityWhen == (isSearch ? "IsSearch" : "IsLine");
}

export type EntityWhen = "Always" | "IsSearch" | "IsLine" | "Never";

