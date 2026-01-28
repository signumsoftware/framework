import * as React from "react"
import { RouteObject } from 'react-router'
import { Dic, classes, softCast, } from './Globals';
import { ajaxGet, ajaxPost, clearContextHeaders } from './Services';
import { Lite, Entity, ModifiableEntity, EntityPack, isEntity, isLite, isEntityPack, toLite, liteKey, FrameMessage, ModelEntity, getToString, isModifiableEntity, EnumEntity, SearchMessage } from './Signum.Entities';
import { TypeEntity, ExceptionEntity } from './Signum.Basics';
import { PropertyRoute, PseudoType, Type, getTypeInfo, tryGetTypeInfos, getTypeName, isTypeModel, OperationType, TypeReference, IsByAll, isTypeEntity, tryGetTypeInfo, getTypeInfos, newLite, TypeInfo, EnumType } from './Reflection';
import { ButtonBarElement, ButtonsContext, EntityFrame, TypeContext } from './TypeContext';
import * as AppContext from './AppContext';
import { Finder } from './Finder';
import * as Operations from './Operations';
import { Constructor } from './Constructor';
import { ViewReplacer } from './Frames/ReactVisitor'
import { AutocompleteConfig, FindOptionsAutocompleteConfig, getLitesWithSubStr, LiteAutocompleteConfig, MultiAutoCompleteConfig } from './Lines/AutoCompleteConfig'
import { FindOptions } from './FindOptions'
import { ImportComponent } from './ImportComponent'
import { BsSize } from "./Components/Basic";
import { ButtonBarManager } from "./Frames/ButtonBar";
import { clearWidgets } from "./Frames/Widgets";
import { toAbsoluteUrl, currentUser } from "./AppContext";
import { useForceUpdate, useAPI, useAPIWithReload, APIHookOptions } from "./Hooks";
import { ErrorModalOptions, RenderServiceMessageDefault, RenderValidationMessageDefault, RenderMessageDefault } from "./Modals/ErrorModal";
import CopyLiteButton from "./Components/CopyLiteButton";
import { Typeahead } from "./Components";
import { TextHighlighter, TypeaheadOptions } from "./Components/Typeahead";
import CopyLinkButton from "./Components/CopyLinkButton";
import { object } from "prop-types";
import { clearSpecialActions } from "./OmniboxSpecialAction";
import { ContextualItemsContext, MenuItemBlock } from "./SearchControl/ContextualItems";

if (!window.__allowNavigatorWithoutUser && (currentUser == null || getToString(currentUser) == "Anonymous"))
  throw new Error("To improve intial performance, no dependency to any module that depends on Navigator should be taken for anonymous user. Review your dependencies or write var __allowNavigatorWithoutUser = true in Index.cshtml to disable this check.");

export namespace Navigator {

  export function start(options: { routes: RouteObject[] }): void {
    options.routes.push({ path: "/view/:type/:id", element: <ImportComponent onImport={() => getFramePage()} /> });
    options.routes.push({ path: "/create/:type", element: <ImportComponent onImport={() => getFramePage()} /> });

    AppContext.clearSettingsActions.push(clearEntitySettings);
    AppContext.clearSettingsActions.push(clearWidgets)
    AppContext.clearSettingsActions.push(ButtonBarManager.clearButtonBarRenderer);
    AppContext.clearSettingsActions.push(Constructor.clearCustomConstructors);
    AppContext.clearSettingsActions.push(clearEntityChanged);
    AppContext.clearSettingsActions.push(clearSpecialActions);
    AppContext.clearSettingsActions.push(clearEvents);

    ErrorModalOptions.getExceptionUrl = exceptionId => navigateRoute(newLite(ExceptionEntity, exceptionId));
    ErrorModalOptions.isExceptionViewable = () => isViewable(ExceptionEntity);
  }

  export const entityChanged: { [typeName: string]: Array<(cleanName: string, entity: Entity | undefined, isRedirect: boolean) => void> } = {};

  export function registerEntityChanged<T extends Entity>(type: Type<T>, callback: (cleanName: string, entity: T | undefined, isRedirect: boolean) => void): void {
    var cleanName = type.typeName;
    (entityChanged[cleanName] ??= []).push(callback as any);
  }




  export function useEntityChanged<T extends Entity>(type: Type<T>, callback: (cleanName: string, entity: T | undefined, isRedirect: boolean) => void, deps: any[]): void;
  export function useEntityChanged(types: string[], callback: (cleanName: string, entity: Entity | undefined, isRedirect: boolean) => void, deps: any[]): void;
  export function useEntityChanged<T extends Entity>(typeOrTypes: Type<any> | string | string[], callback: (cleanName: string, entity: Entity | undefined, isRedirect: boolean) => void, deps: any[]): void {

    var types = Array.isArray(typeOrTypes) ? typeOrTypes : [typeOrTypes.toString()];

    React.useEffect(() => {

      types.forEach(cleanName => {
        (entityChanged[cleanName] ??= []).push(callback);
      });

      return () => {
        types.forEach(cleanName => {
          entityChanged[cleanName]?.remove(callback);

          if (entityChanged[cleanName]?.length == 0)
            delete entityChanged[cleanName];
        });
      }
    }, [types.join(","), ...deps]);
  }

  function clearEntityChanged() {
    Dic.clear(entityChanged);
  }

  export function raiseEntityChanged(typeOrEntity: Type<any> | string | Entity, isRedirect = false): void {
    var cleanName = isEntity(typeOrEntity) ? typeOrEntity.Type : typeOrEntity.toString();
    var entity = isEntity(typeOrEntity) ? typeOrEntity : undefined;

    entityChanged[cleanName]?.forEach(func => func(cleanName, entity, isRedirect));
  }

  export function getTypeSubTitle(entity: ModifiableEntity, pr: PropertyRoute | undefined): React.ReactNode | undefined {

    var settings = entitySettings[entity.Type];

    if (settings?.renderSubTitle)
      return settings.renderSubTitle(entity);

    if (isTypeEntity(entity.Type)) {

      const typeInfo = getTypeInfo(entity.Type);

      if (entity.isNew)
        return null;

      return defaultRenderSubTitle(typeInfo, entity);
    }
    else if (isTypeModel(entity.Type)) {
      return undefined;

    } else {
      return pr!.typeReference().typeNiceName;
    }
  }

  let defaultRenderSubTitle = (typeInfo: TypeInfo, entity: ModifiableEntity): React.ReactElement | null => {
    return <span>{typeInfo.niceName} {renderId(entity as Entity)}</span>;
  }

  export function setDefaultRenderTitleFunction(newFunction: (typeInfo: TypeInfo, entity: ModifiableEntity) => React.ReactElement | null): void {
    defaultRenderSubTitle = newFunction;
  }


  let renderId = (entity: Entity): React.ReactElement | string | number => {
    var idType = getTypeInfo(entity.Type).members["Id"].type;

    const hideId = getSettings(entity.Type)?.hideId ?? idType!.name == "Guid";
    return (
      <>
        <span className={hideId ? "sf-hide-id" : ""}>
          {entity.id}
        </span>
        <CopyLiteButton className={"sf-hide-id"} entity={entity} />
        <CopyLinkButton className={"sf-hide-id"} entity={entity} />
      </>
    );
  }

  export function setRenderIdFunction(newFunction: (entity: Entity) => React.ReactElement | string | number): void {
    renderId = newFunction;
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


  export function navigateRouteDefault(typeName: string, id: number | string, viewName?: string): string {
    return "/view/" + typeName.firstLower() + "/" + id + (viewName ? "?viewName=" + viewName : "");

  }

  export function createRoute(type: PseudoType, viewName?: string): string {
    return "/create/" + getTypeName(type) + (viewName ? "?viewName=" + viewName : "");
  }



  export function renderLiteOrEntity(entity: Lite<Entity> | Entity | ModifiableEntity, modelType?: string): string | React.ReactElement<any, string | React.JSXElementConstructor<any>> | undefined {
    if (isLite(entity))
      return renderLite(entity);

    if (isEntity(entity)) {
      var es = entitySettings[entity.Type];

      if (es.renderEntity)
        return es.renderEntity(entity, new TextHighlighter(undefined));

      if (es.renderLite) {
        var lite = toLite(entity, entity.isNew);
        return es.renderLite(lite, new TextHighlighter(undefined));
      }

      return getToString(entity);
    }
  }

  export function renderLite(lite: Lite<Entity>, hl?: TextHighlighter): React.ReactElement | string {
    var es = entitySettings[lite.EntityType];
    if (es != null && es.renderLite != null) {
      return es.renderLite(lite, hl ?? new TextHighlighter(undefined));
    }

    var toStr = getToString(lite);
    return hl == null ? toStr : hl.highlight(toStr);
  }

  export function renderEntity(entity: ModifiableEntity): React.ReactElement | string {
    var es = entitySettings[entity.Type];
    if (es != null && es.renderEntity != null) {
      return es.renderEntity(entity, new TextHighlighter(undefined));
    }

    if (entity.isNew) {
      var ti = tryGetTypeInfo(entity.Type);

      if (ti) {
        if (isTypeModel(entity.Type))
          return ti.niceName!;

        return FrameMessage.New0_G.niceToString().forGenderAndNumber(ti.gender).formatWith(ti.niceName);
      }
    }

    return getToString(entity);
  }

  export function clearEntitySettings(): void {
    Dic.clear(entitySettings);
  }

  export function clearEvents(): void {

    isCreableEvent.clear();
    isReadonlyEvent.clear();
    isViewableEvent.clear();
    Finder.isFindableEvent.clear();
  }

  export const entitySettings: { [type: string]: EntitySettings<ModifiableEntity> } = {};
  export function addSettings(...settings: EntitySettings<any>[]): void {
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

  export function setViewDispatcher(newDispatcher: ViewDispatcher): void {
    viewDispatcher = newDispatcher;
  }

  export function getFramePage(): Promise<typeof import("./Frames/FramePage")> {
    return import("./Frames/FramePage");
  }

  export function getFrameModal(): Promise<typeof import("./Frames/FrameModal")> {
    return import("./Frames/FrameModal");
  }

  export function onFramePageCreationCancelled(): void {
    AppContext.navigate("/", { replace: true });
  }

  export interface ViewDispatcher {
    hasDefaultView(typeName: string): boolean;
    getViewNames(typeName: string): Promise<string[]>;
    getViewPromise(entity: ModifiableEntity, viewName?: string): ViewPromise<ModifiableEntity>;
    getViewOverrides(typeName: string, viewName?: string): Promise<ViewOverride<ModifiableEntity>[]>;
  }

  export class BasicViewDispatcher implements ViewDispatcher {
    hasDefaultView(typeName: string): boolean {
      const es = getSettings(typeName);
      return (es?.getViewPromise) != null;
    }

    getViewNames(typeName: string): Promise<string[]> {
      const es = getSettings(typeName);
      return Promise.resolve((es?.namedViews && Dic.getKeys(es.namedViews)) ?? []);
    }

    getViewOverrides(typeName: string, viewName?: string): Promise<ViewOverride<ModifiableEntity>[]> {
      const es = getSettings(typeName);
      return Promise.resolve(es?.viewOverrides?.filter(a => a.viewName == viewName) ?? []);
    }


    getViewPromise(entity: ModifiableEntity, viewName?: string): ViewPromise<ModifiableEntity> {
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


  export class AutoViewDispatcher implements ViewDispatcher {

    hasDefaultView(typeName: string) {
      return true;
    }

    getViewNames(typeName: string): Promise<string[]> {
      const es = getSettings(typeName);
      return Promise.resolve((es?.namedViews && Dic.getKeys(es.namedViews)) ?? []);
    }

    getViewOverrides(typeName: string, viewName?: string): Promise<ViewOverride<ModifiableEntity>[]> {
      const es = getSettings(typeName);
      return Promise.resolve(es?.viewOverrides?.filter(a => a.viewName == viewName) ?? []);
    }

    getViewPromise(entity: ModifiableEntity, viewName?: string): ViewPromise<ModifiableEntity> {
      const es = getSettings(entity.Type);

      if (viewName == undefined) {

        if (es?.getViewPromise == null)
          return new ViewPromise<ModifiableEntity>(import('./AutoComponent'));

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

  export let viewDispatcher: ViewDispatcher = new AutoViewDispatcher();

  export function getViewPromise<T extends ModifiableEntity>(entity: T, viewName?: string): ViewPromise<T> {
    return viewDispatcher.getViewPromise(entity, viewName);
  }

  export const isCreableEvent: Array<(typeName: string, options: IsCreableOptions | undefined) => boolean> = [];

  export interface IsCreableOptions {
    customComponent?: boolean;
    isSearch?: boolean;
    isEmbedded?: boolean;
  }

  export function isCreable(type: PseudoType, options?: IsCreableOptions): boolean {

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

  export function isReadOnly(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>, options?: IsReadonlyOptions): boolean {

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
  export function checkFlag(entityWhen: EntityWhen, isSearchMainEntity: boolean | undefined): boolean {
    return entityWhen == "Always" ||
      entityWhen == (isSearchMainEntity ? "IsSearch" : "IsLine");
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
    fullScreenSearch?: boolean;
    isEmbeddedEntity?: boolean;
  }

  export function isFindable(type: PseudoType, options?: IsFindableOptions): boolean {

    const typeName = getTypeName(type);

    const baseIsReadOnly = typeIsFindable(typeName, options?.isEmbeddedEntity);

    return baseIsReadOnly && Finder.isFindable(typeName, options?.fullScreenSearch ?? true);
  }

  function typeIsFindable(typeName: string, isEmbeddedEntity: boolean | undefined) {

    const es = entitySettings[typeName];

    if (es != undefined && es.isFindable != undefined)
      return es.isFindable;

    if (isEmbeddedEntity)
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

  export const isViewableEvent: Array<(typeName: string, entityPack: EntityPack<ModifiableEntity> | Lite<Entity> | undefined, options: IsViewableOptions | undefined) => boolean> = [];

  export interface IsViewableOptions {
    customComponent?: boolean;
    isSearch?: "main" | "related";
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

  export function isViewable(typeOrEntity: PseudoType | EntityPack<ModifiableEntity> | Lite<Entity>, options?: IsViewableOptions): boolean {

    const entity =
      isEntityPack(typeOrEntity) ? typeOrEntity :
        isLite(typeOrEntity) ? typeOrEntity :
          undefined;

    const typeName =
      isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type :
        isLite(typeOrEntity) ? typeOrEntity.EntityType :
          getTypeName(typeOrEntity as PseudoType);

    const typeViewable = checkFlag(typeIsViewable(typeName, options?.isEmbedded), options?.isSearch == "main");
    if (!typeViewable)
      return false;

    const hasView = options?.customComponent || viewDispatcher.hasDefaultView(typeName);
    if (!hasView)
      return false;

    if (entity) {
      const es = entitySettings[typeName];

      if (es != null && isLite(entity) && es.isViewableLite && !es.isViewableLite(entity, options))
        return false;

      if (es != null && isEntityPack(entity) && es.isViewableEntityPack && !es.isViewableEntityPack(entity, options))
        return false;
    }

    if (!isViewableEvent.every(f => f(typeName, entity, options)))
      return false;

    return true;
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

      if (s?.defaultFindOptions) {
        return s.defaultFindOptions;
      }
    }

    return undefined;
  }

  export function getAutoComplete(type: TypeReference, findOptions: FindOptions | undefined, findOptionsDictionary: { [typeName: string]: FindOptions } | undefined, ctx: TypeContext<any>, create: boolean, showType?: boolean): AutocompleteConfig<any> | null {
    if (type.isEmbedded || type.name == IsByAll)
      return null;

    let types = tryGetTypeInfos(type).notNull();
    showType ??= types.length > 1;

    types = types.filter(t => isFindable(t, { fullScreenSearch: false }));

    if (types.length == 0)
      return null;

    if (types.length == 1 || findOptions != null)
      return getAutoCompleteBasic(types[0]!, findOptions, ctx, create, showType);

    return new MultiAutoCompleteConfig(types.toObject(t => t!.name,
      t => getAutoCompleteBasic(t!, (findOptionsDictionary && findOptionsDictionary[t!.name]), ctx, create, showType!)
    ));
  }


  export function getAutoCompleteBasic(type: TypeInfo, findOptions: FindOptions | undefined, ctx: TypeContext<any>, create: boolean, showType: boolean): AutocompleteConfig<any> {

    var s = getSettings(type);

    if (s?.autocomplete != null) {
      var acc = s.autocomplete(findOptions, showType);

      if (acc != null)
        return acc;
    }

    var fo = findOptions ?? s?.defaultFindOptions ?? { queryName: type.name };

    return new FindOptionsAutocompleteConfig(fo, {
      showType: showType,
      itemsDelay: s?.autocompleteDelay,
      getAutocompleteConstructor: (subStr, rows) => getAutocompleteConstructors(type, subStr, { ctx, foundLites: rows.map(a => a.entity!), findOptions, create: create }) as AutocompleteConstructor<Entity>[]
    });
  }


  export interface ViewOptions<T extends ModifiableEntity> {
    title?: React.ReactNode | null;
    subTitle?: React.ReactNode | null;
    propertyRoute?: PropertyRoute;
    readOnly?: boolean;
    modalSize?: BsSize;
    isOperationVisible?: (eoc: Operations.EntityOperationContext<T & Entity>) => boolean;
    validate?: boolean;
    requiresSaveOperation?: boolean;
    avoidPromptLoseChange?: boolean;
    buttons?: ViewButtons;
    getViewPromise?: (entity: T) => undefined | string | ViewPromise<T>;
    createNew?: () => Promise<EntityPack<T> | undefined>;
    allowExchangeEntity?: boolean;
    extraProps?: {};
  }


  export function view<T extends ModifiableEntity>(entityOrPack: Lite<T & Entity> | T | EntityPack<T>, viewOptions?: ViewOptions<T>): Promise<T | undefined> {

    const typeName = isEntityPack(entityOrPack) ? entityOrPack.entity.Type : getTypeName(entityOrPack);

    const es = getSettings(typeName) as EntitySettings<T> | undefined;

    if (es?.onView)
      return es.onView(entityOrPack, viewOptions);
    else
      return viewDefault(entityOrPack, viewOptions);
  }

  export function viewDefault<T extends ModifiableEntity>(entityOrPack: Lite<T & Entity> | T | EntityPack<T>, viewOptions?: ViewOptions<T>): Promise<T | undefined> {
    return getFrameModal()
      .then(NP => NP.FrameModalManager.openView(entityOrPack, viewOptions ?? {}));
  }

  export function createInNewTab(pack: EntityPack<ModifiableEntity>, viewName?: string): void {
    var url = createRoute(pack.entity.Type, viewName) + "?waitOpenerData=true";
    window.dataForChildWindow = pack;
    var win = window.open(toAbsoluteUrl(url));
  }

  export function createInCurrentTab(pack: EntityPack<ModifiableEntity>, viewName?: string): void {
    var url = createRoute(pack.entity.Type, viewName) + "?waitCurrentData=true";
    window.dataForCurrentWindow = pack;
    AppContext.navigate(url);
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
      return view(pack, { buttons: "close" }).then(() => undefined);
    }
  }

  export function toEntityPack<T extends ModifiableEntity>(entityOrEntityPack: Lite<T & Entity> | T | EntityPack<T>): Promise<EntityPack<T>> {
    if ((entityOrEntityPack as EntityPack<T>).canExecute)
      return Promise.resolve(entityOrEntityPack as EntityPack<T>);

    const entity = (entityOrEntityPack as T).Type ?
      entityOrEntityPack as T :
      (entityOrEntityPack as Lite<Entity> | EntityPack<T>).entity;

    if (entity == undefined)
      return API.fetchEntityPack(entityOrEntityPack as Lite<T & Entity>);

    if (!isEntity(entity))
      return Promise.resolve({ entity: cloneEntity(entity), canExecute: {} });

    return API.fetchEntityPackEntity(entity as T & Entity).then(ep => ({ ...ep, entity: cloneEntity(entity) }));
  }

  export async function reloadFrameIfNecessary(frame: EntityFrame): Promise<void> {

    var entity = frame.pack.entity;
    if (isEntity(entity) && entity.id && entity.ticks != null) {
      var newPack = await API.fetchEntityPack(toLite(entity));
      if (newPack.entity.ticks != entity.ticks)
        frame.onReload(newPack);
    }
  }

  function cloneEntity(obj: any) {
    return JSON.parse(JSON.stringify(obj));
  }


  export function useFetchInState<T extends Entity>(lite: Lite<T> | null | undefined, options?: APIHookOptions): T | null | undefined {
    return useAPI(signal =>
      lite == null ? Promise.resolve<T | null | undefined>(lite) :
        API.fetch(lite),
      [lite && liteKey(lite)], options);
  }

  export function useFetchInStateWithReload<T extends Entity>(lite: Lite<T> | null | undefined, options?: APIHookOptions): [T | null | undefined, () => void] {
    return useAPIWithReload(signal =>
      lite == null ? Promise.resolve<T | null | undefined>(lite) :
        API.fetch(lite),
      [lite && liteKey(lite)], options);
  }

  export function useFetchAndRemember<T extends Entity>(lite: Lite<T> | null, onLoaded?: () => void): T | null | undefined {

    const forceUpdate = useForceUpdate();
    React.useEffect(() => {
      if (lite && !lite.entity)
        API.fetchAndRemember(lite)
          .then(() => {
            onLoaded && onLoaded();
            forceUpdate();
          });
    }, [lite]);


    if (lite == null)
      return null;

    if (lite.entity == null)
      return undefined;

    return lite.entity;
  }

  export function useFetchEntity<T extends Entity>(type: Type<T>, id: any, partitionId?: number, deps?: React.DependencyList, options?: APIHookOptions): T | undefined {
    return useAPI(signal => API.fetchEntity(type, id, partitionId), [type, id, partitionId, ...(deps ?? [])], options);
  }

  export function useFetchAll<T extends Entity>(type: Type<T>, deps?: React.DependencyList): T[] | undefined {
    return useAPI(signal => API.fetchAll(type), [type, ...(deps ?? [])]);
  }

  export function useLiteToString<T extends Entity>(type: Type<T>, id: number | string, deps?: React.DependencyList, options?: APIHookOptions): Lite<T> {

    var lite = React.useMemo(() => newLite(type, id), [type, id, ...(deps ?? [])]);

    useAPI(() => API.fillLiteModels(lite), [lite, ...(deps ?? [])], options);

    return lite;
  }

  export function useFillToString<T extends Entity>(lite: Lite<T> | null | undefined, force: boolean = false, deps?: React.DependencyList): void {
    useAPI(() => {
      return lite == null || ((lite.model != null || lite.entity != null) && !force) ? Promise.resolve() : API.fillLiteModels(lite);
    }, [lite, ...(deps ?? [])]);
  }


  export function getAutocompleteConstructors(tr: TypeReference, str: string, aac: AutocompleteConstructorContext): AutocompleteConstructor<ModifiableEntity>[] {
    return getTypeInfos(tr.name).map(ti => {
      var es = getSettings(ti);

      if (es == null || es.autocompleteConstructor == null)
        return null;

      if (typeof es.autocompleteConstructor == "string")
        return softCast<AutocompleteConstructor<ModifiableEntity>>({
          type: ti.name,
          onClick: () => Constructor.construct(ti.name, { [es!.autocompleteConstructor as string]: str }).then(a => a && view(a))
        });

      return es.autocompleteConstructor(str, aac);
    }).notNull();
  }

  export function someNonViewable(lites: Lite<Entity>[]) : boolean {
    return lites.groupBy(a => a.EntityType).some(gr => {
      var isViewable = Navigator.entitySettings[gr.key]?.isViewableLite;
      return isViewable && gr.elements.some(lite => !isViewable!(lite, { isSearch: "main" }))
    });
  }

  export namespace API {

    export function fillLiteModels(...lites: (Lite<Entity> | null | undefined)[]): Promise<void> {
      return fillLiteModelsArray(lites.filter(l => l != null) as Lite<Entity>[]);
    }

    export function fillLiteModelsArray(lites: Lite<Entity>[], force?: boolean): Promise<void> {

      if (force) {
        lites.forEach(a => a.ModelType = a.ModelType ?? (isModifiableEntity(a.model) ? a.model.Type : "string"));
      }

      const realLites = force ? lites : lites.filter(a => a.model == undefined && a.entity == undefined);

      if (!realLites.length)
        return Promise.resolve();

      return ajaxPost<unknown[]>({ url: "/api/liteModels" }, realLites).then(models => {
        realLites.forEach((l, i) => l.model = models[i]);
      });
    }

    export function fetchAll<T extends Entity>(type: Type<T>): Promise<Array<T>> {
      return ajaxGet({ url: "/api/fetchAll/" + type.typeName });
    }


    export function fetchAndRemember<T extends Entity>(lite: Lite<T>): Promise<T> {
      if (lite.entity)
        return Promise.resolve(lite.entity);

      if (lite.id == null)
        throw new Error("Lite has no Id");

      return fetchEntity(lite.EntityType, lite.id).then(e => lite.entity = e as T);
    }

    export function fetch<T extends Entity>(lite: Lite<T>): Promise<T> {

      if (lite.id == null)
        throw new Error("Lite has no Id");

      return fetchEntity(lite.EntityType, lite.id, lite.partitionId) as Promise<T>;
    }

    export function fetchEntity<T extends Entity>(type: Type<T>, id: any, partitionId?: number): Promise<T>;
    export function fetchEntity(type: PseudoType, id: number | string, partitionId?: number): Promise<Entity>;
    export function fetchEntity(type: PseudoType, id?: number | string, partitionId?: number): Promise<Entity> {

      const typeName = getTypeName(type);
      let idVal = id;

      return ajaxGet({ url: "/api/entity/" + typeName + "/" + id + (partitionId ? "?partitionId=" + partitionId : "") });
    }

    export function exists<T extends Entity>(lite: Lite<T>): Promise<boolean>;
    export function exists<T extends Entity>(entity: T): Promise<boolean>;
    export function exists<T extends Entity>(type: Type<T>, id: any): Promise<boolean>;
    export function exists(type: PseudoType, id: number | string): Promise<boolean>;
    export function exists(typeOrEntity: PseudoType | Lite<Entity> | Entity, idOrNull?: number | string): Promise<boolean> {

      const typeName =
        isEntity(typeOrEntity) ? typeOrEntity.Type :
          isLite(typeOrEntity) ? typeOrEntity.EntityType :
            getTypeName(typeOrEntity);

      let id = isEntity(typeOrEntity) ? typeOrEntity.id :
        isLite(typeOrEntity) ? typeOrEntity.id :
          idOrNull;

      if (id == null)
        throw new Error("No id found");

      return ajaxGet({ url: "/api/exists/" + typeName + "/" + id });
    }


    export function fetchEntityPack<T extends Entity>(lite: Lite<T>): Promise<EntityPack<T>>;
    export function fetchEntityPack<T extends Entity>(type: Type<T>, id: number | string, partitionId?: number): Promise<EntityPack<T>>;
    export function fetchEntityPack(type: PseudoType, id: number | string, partitionId?: number): Promise<EntityPack<Entity>>;
    export function fetchEntityPack(typeOrLite: PseudoType | Lite<any>, id?: any, partitionId?: number): Promise<EntityPack<Entity>> {

      const typeName = (typeOrLite as Lite<any>).EntityType ?? getTypeName(typeOrLite as PseudoType);
      let idVal = (typeOrLite as Lite<any>).id != null ? (typeOrLite as Lite<any>).id : id;
      let pId = (typeOrLite as Lite<any>)?.partitionId ?? partitionId;
      return ajaxGet({ url: "/api/entityPack/" + typeName + "/" + idVal + (pId ? "?partitionId=" + pId : "") });
    }

    export function fetchEntityPackEntity<T extends Entity>(entity: T): Promise<EntityPack<T>> {
      return ajaxPost<EntityPack<T>>({ url: "/api/entityPackEntity" }, entity)
        .then(ep => ({ ...ep, entity }));
    }

    export function validateEntity(entity: ModifiableEntity): Promise<void> {
      return ajaxPost({ url: "/api/validateEntity" }, entity);
    }

    export function getType(typeName: string): Promise<TypeEntity | null> {

      return ajaxGet({ url: `/api/reflection/typeEntity/${typeName}` });
    }

    export function getEnumEntities<T extends string>(type: EnumType<T>): Promise<EnumConverter<T>>;
    export function getEnumEntities(typeName: string): Promise<EnumConverter<string>>;
    export function getEnumEntities(type: string | EnumType<string>): Promise<EnumConverter<string>> {

      var typeName = typeof type == "string" ? type : type.typeName;

      return ajaxGet<{ [enumValue: string]: Entity }>({ url: `/api/reflection/enumEntities/${typeName}` })
        .then(enumToEntity => softCast<EnumConverter<string>>({
          enumToEntity: enumToEntity,
          idToEnum: Object.entries(enumToEntity).toObject(a => a[1].id!.toString(), a => a[0])
        }));
    }
  }
}


export interface EnumConverter<T> {
  enumToEntity: { [enumValue: string]: EnumEntity<T> };
  idToEnum: { [id: string]: T };
}


export interface EntitySettingsOptions<T extends ModifiableEntity> {
  isCreable?: EntityWhen;
  isFindable?: boolean;
  isViewable?: EntityWhen;
  isViewableLite?: (lite: Lite<T & Entity>, options: Navigator.IsViewableOptions | undefined) => boolean;
  isViewableEntityPack?: (entityPack: EntityPack<T>, options: Navigator.IsViewableOptions | undefined) => boolean;
  isReadOnly?: boolean;
  avoidPopup?: boolean;
  supportsAdditionalTabs?: boolean;

  hideId?: boolean;

  allowWrapEntityLink?: boolean;
  avoidFillSearchColumnWidth?: boolean;

  modalSize?: BsSize;
  modalMaxWidth?: boolean;
  modalDialogClass?: string;
  modalFullScreen?: boolean;

  stickyHeader?: boolean;

  onAssignServerChanges?: (local: T, server: T) => void;

  renderSubTitle?: (entity: T) => React.ReactNode;

  autocomplete?: (fo: FindOptions | undefined, showType: boolean) => AutocompleteConfig<any> | undefined | null;
  autocompleteDelay?: number;
  autocompleteConstructor?: (keyof T) | ((str: string, aac: AutocompleteConstructorContext) => AutocompleteConstructor<T> | null);
  defaultFindOptions?: FindOptions;

  getViewPromise?: (entity: T) => ViewPromise<T>;
  onNavigateRoute?: (typeName: string, id: string | number) => string;
  onView?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, viewOptions?: Navigator.ViewOptions<T>) => Promise<T | undefined>;
  onCreateNew?: (oldEntity: EntityPack<T>) => (Promise<EntityPack<T> | undefined>) | undefined; /*Save An New*/

  renderLite?: (lite: Lite<T & Entity>, hl: TextHighlighter) => React.ReactElement | string;
  renderEntity?: (entity: T, hl: TextHighlighter) => React.ReactElement | string; 
  extraToolbarButtons?: (ctx: ButtonsContext) => (ButtonBarElement | undefined)[];
  enforceFocusInModal?: boolean;

  namedViews?: NamedViewSettings<T>[];

  showContextualSearchBox?: (ctx: ContextualItemsContext<Entity>, blocks?: MenuItemBlock[]) => boolean
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

export class EntitySettings<T extends ModifiableEntity> {
  typeName: string;

  getViewPromise?: (entity: T) => ViewPromise<T>;

  viewOverrides?: Array<ViewOverride<T>>;

  isCreable?: EntityWhen;
  isFindable?: boolean;
  isViewable?: EntityWhen;
  isViewableLite?: (lite: Lite<T & Entity>, options: Navigator.IsViewableOptions | undefined) => boolean;
  isViewableEntityPack?: (entityPack: EntityPack<T>, options: Navigator.IsViewableOptions | undefined) => boolean;
  isReadOnly?: boolean;
  avoidPopup!: boolean;
  supportsAdditionalTabs?: boolean;

  hideId?: boolean;

  allowWrapEntityLink?: boolean;
  avoidFillSearchColumnWidth?: boolean;

  modalSize?: BsSize;
  modalMaxWidth?: boolean;
  modalDialogClass?: string;
  modalFullScreen?: boolean;

  stickyHeader?: boolean;

  onAssignServerChanges?: (local: T, server: T) => void;

  renderSubTitle?: (entity: T) => React.ReactNode;

  autocomplete?: (fo: FindOptions | undefined, showType: boolean) => AutocompleteConfig<any> | undefined | null;
  autocompleteDelay?: number;
  autocompleteConstructor?: (keyof T) | ((str: string, aac: AutocompleteConstructorContext) => AutocompleteConstructor<T> | null);
  defaultFindOptions?: FindOptions;

  onView?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, viewOptions?: Navigator.ViewOptions<T>) => Promise<T | undefined>;
  onNavigateRoute?: (typeName: string, id: string | number, viewName?: string) => string;

  namedViews?: { [viewName: string]: NamedViewSettings<T> };
  overrideView(override: (replacer: ViewReplacer<T>) => void, viewName?: string): void {
    if (this.viewOverrides == undefined)
      this.viewOverrides = [];

    this.viewOverrides.push({ override, viewName });
  }

  renderLite?: (lite: Lite<T & Entity>, hl: TextHighlighter) => React.ReactElement | string; 
  renderEntity?: (entity: T, hl: TextHighlighter) => React.ReactElement | string; 
  extraToolbarButtons?: (ctx: ButtonsContext) => (ButtonBarElement | undefined)[];
  enforceFocusInModal?: boolean;

  showContextualSearchBox = (ctx: any, blocks?: MenuItemBlock[]) : boolean => Boolean(blocks && blocks.notNull().sum(b => b.menuItems?.length) > 20);

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

  registerNamedView(settings: NamedViewSettings<T>): void {
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
  promise!: Promise<(ctx: TypeContext<T>) => React.ReactElement>;

  constructor(promise?: Promise<ViewModule<T>>) {
    if (promise)
      this.promise = promise
        .then(mod => {
          return (ctx: TypeContext<T>): React.ReactElement => React.createElement(mod.default, { ctx });
        });
  }

  static resolve<T extends ModifiableEntity>(getComponent: (ctx: TypeContext<T>) => React.ReactElement): ViewPromise<T> {
    var result = new ViewPromise<T>();
    result.promise = Promise.resolve(getComponent);
    return result;
  }

  withProps<P>(props: Partial<P>): ViewPromise<T> {

    var result = new ViewPromise<T>();

    result.promise = this.promise.then(func => {
      return (ctx: TypeContext<T>): React.ReactElement => {
        var result = func(ctx);
        return React.cloneElement(result, { ...props });
      };
    });

    return result;
  }

  applyViewOverrides(typeName: string, viewName?: string): ViewPromise<T> {
    this.promise = this.promise.then(func =>
      Navigator.viewDispatcher.getViewOverrides(typeName, viewName).then(vos => {

        if (vos.length == 0)
          return func;

        return (ctx: TypeContext<T>) => {
          var result = func(ctx);
          var component = result.type as React.ComponentClass<{ ctx: TypeContext<T> }> | React.FunctionComponent<{ ctx: TypeContext<T> }>;
          if (component.prototype.render) {
            monkeyPatchClassComponent<T>(component as React.ComponentClass<{ ctx: TypeContext<T> }>, vos!);
            return result;
          } else {
            var newFunc = ViewPromise.surroundFunctionComponent(component as React.FunctionComponent<{ ctx: TypeContext<T> }>, vos)
            return React.createElement(newFunc, result.props as any);
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

  static surroundFunctionComponent<T extends ModifiableEntity>(functionComponent: React.FunctionComponent<{ ctx: TypeContext<T> }>, viewOverrides: ViewOverride<T>[]): React.FunctionComponent<{ ctx: TypeContext<T> }> {

    var cache = (functionComponent as any).cache as FunctionCache<T>;

    if (cache) {
      if (cache.viewOverrides.every((vo, i) => viewOverrides[i] == vo))
        return cache.overridenView;
      else {
        (functionComponent as any).cache = null;
      }
    }

    var result = function NewComponent(props: { ctx: TypeContext<T> }) {
      var view = functionComponent(props);

      const replacer = new ViewReplacer<T>(view! as React.ReactElement, props.ctx, functionComponent);
      viewOverrides.forEach(vo => vo.override(replacer));
      return replacer.result;
    };

    Object.defineProperty(result, "name", { value: functionComponent.name + "VO" });

    (functionComponent as any).cache = softCast<FunctionCache<T>>({
      overridenView: result,
      viewOverrides: viewOverrides,
    });

    return result;
  }

}

function monkeyPatchClassComponent<T extends ModifiableEntity>(component: React.ComponentClass<{ ctx: TypeContext<T> }>, viewOverrides: ViewOverride<T>[]) {

  if (!component.prototype.render)
    throw new Error("render function not defined in " + component);

  if (component.prototype.render.withViewOverrides)
    return;

  const baseRender = component.prototype.render as (this: React.Component<any>) => React.ReactElement;

  component.prototype.render = function (this: React.Component<any, any>) {

    const ctx = this.props.ctx;

    const view = baseRender.call(this);

    const replacer = new ViewReplacer<T>(view!, ctx, component);
    viewOverrides.forEach(vo => vo.override(replacer));
    return replacer.result;
  };

  component.prototype.render.withViewOverrides = true;
}

interface FunctionCache<T extends ModifiableEntity>  {
  overridenView: React.FunctionComponent<{ ctx: TypeContext<T> }>,
  viewOverrides: ViewOverride<T>[]
}


export type EntityWhen = "Always" | "IsSearch" | "IsLine" | "Never";



