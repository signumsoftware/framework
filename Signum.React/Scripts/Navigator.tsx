import * as React from "react"
import * as H from "history"
import { Route, Switch } from "react-router"
import { Dic, } from './Globals';
import { ajaxGet, ajaxPost } from './Services';
import { Lite, Entity, ModifiableEntity, EntityPack, isEntity, isLite, isEntityPack, toLite } from './Signum.Entities';
import { IUserEntity, TypeEntity } from './Signum.Entities.Basics';
import { PropertyRoute, PseudoType, Type, getTypeInfo, getTypeInfos, getTypeName, isTypeEmbeddedOrValue, isTypeModel, OperationType, TypeReference, IsByAll } from './Reflection';
import { TypeContext } from './TypeContext';
import * as Finder from './Finder';
import * as Operations from './Operations';
import { ViewReplacer } from './Frames/ReactVisitor'
import { AutocompleteConfig, FindOptionsAutocompleteConfig, LiteAutocompleteConfig } from './Lines/AutoCompleteConfig'
import { FindOptions } from './FindOptions'
import { ImportRoute } from "./AsyncImport";
import * as AppRelativeRoutes from "./AppRelativeRoutes";
import { NormalWindowMessage } from "./Signum.Entities";
import { BsSize } from "./Components/Basic";


Dic.skipClasses.push(React.Component);

export let currentUser: IUserEntity | undefined;
export function setCurrentUser(user: IUserEntity | undefined) {
    currentUser = user;
}

export let history: H.History;
export function setCurrentHistory(h: H.History) {
    history = h;
}

export let setTitle: (pageTitle?: string) => void;
export function setTitleFunction(titleFunction: (pageTitle?: string) => void) {
    setTitle = titleFunction;
}

export function createAppRelativeHistory(): H.History {
    var h = H.createBrowserHistory({});
    AppRelativeRoutes.useAppRelativeBasename(h);
    AppRelativeRoutes.useAppRelativeComputeMatch(Route);
    AppRelativeRoutes.useAppRelativeComputeMatch(ImportRoute as any);
    AppRelativeRoutes.useAppRelativeSwitch(Switch);
    setCurrentHistory(h);
    return h;

}

export let resetUI: () => void = () => { };
export function setResetUI(reset: () => void) {
    resetUI = reset;
}

export namespace Expander {
    export let onGetExpanded: () => boolean;
    export let onSetExpanded: (isExpanded: boolean) => void;

    export function setExpanded(expanded: boolean): boolean {
        let wasExpanded = onGetExpanded != null &&onGetExpanded();;
        if (onSetExpanded)
            onSetExpanded(expanded);

        return wasExpanded;
    }
}

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<ImportRoute path="~/view/:type/:id" onImportModule={() => import("./Frames/FramePage")} />);
    options.routes.push(<ImportRoute path="~/create/:type" onImportModule={() => import("./Frames/FramePage")} />);
}

export function getTypeTitle(entity: ModifiableEntity, pr: PropertyRoute | undefined) {

    if (isTypeEmbeddedOrValue(entity.Type)) {

        return pr!.typeReference().typeNiceName;

    } else if (isTypeModel(entity.Type)) {

        const typeInfo = getTypeInfo(entity.Type);

        return typeInfo.niceName;

    }
    else {

        const typeInfo = getTypeInfo(entity.Type);

        if (entity.isNew)
            return NormalWindowMessage.New0_G.niceToString().forGenderAndNumber(typeInfo.gender).formatWith(typeInfo.niceName);

        return NormalWindowMessage.Type0Id1.niceToString().formatWith(typeInfo.niceName, (entity as Entity).id);
    }
}


export function navigateRoute(entity: Entity): string;
export function navigateRoute(lite: Lite<Entity>): string;
export function navigateRoute(type: PseudoType, id: number | string): string;
export function navigateRoute(typeOrEntity: Entity | Lite<Entity> | PseudoType, id: number | string | undefined = undefined): string {
    let typeName: string;
    if (isEntity(typeOrEntity)) {
        typeName = typeOrEntity.Type;
        id = typeOrEntity.id;
    }
    else if (isLite(typeOrEntity)) {
        typeName = typeOrEntity.EntityType;
        id = typeOrEntity.id;
    }
    else {
        typeName = getTypeName(typeOrEntity as PseudoType);
    }

    const es = getSettings(typeName);
    if (es && es.onNavigateRoute)
        return es.onNavigateRoute(typeName, id!);
    else
        return navigateRouteDefault(typeName, id!);

}

export function navigateRouteDefault(typeName: string, id: number | string) {
    return toAbsoluteUrl("~/view/" + typeName.firstLower() + "/" + id);

}

export function createRoute(type: PseudoType) {
    return toAbsoluteUrl("~/create/" + getTypeName(type));
}

export const entitySettings: { [type: string]: EntitySettings<ModifiableEntity> } = {};

export function clearEntitySettings() {
    Dic.clear(entitySettings);
}

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
        return (es && es.getViewPromise) != null;
    }

    getViewNames(typeName: string) {
        const es = getSettings(typeName);
        return Promise.resolve(es && es.namedViews && Dic.getKeys(es.namedViews) || []);
    }

    getViewOverrides(typeName: string, viewName?: string) {
        const es = getSettings(typeName);
        return Promise.resolve(es && es.viewOverrides && es.viewOverrides.filter(a => a.viewName == viewName) || []);
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
        return Promise.resolve(es && es.namedViews && Dic.getKeys(es.namedViews) || [] );
    }

    getViewOverrides(typeName: string, viewName?: string) {
        const es = getSettings(typeName);
        return Promise.resolve(es && es.viewOverrides && es.viewOverrides.filter(a => a.viewName == viewName) || []);
    }

    getViewPromise(entity: ModifiableEntity, viewName?: string) {
        const es = getSettings(entity.Type);

        if (viewName == undefined) {

            if (!es || !es.getViewPromise)
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

export const isCreableEvent: Array<(typeName: string) => boolean> = [];

export function isCreable(type: PseudoType, customView = false, isSearch = false) {

    const typeName = getTypeName(type);

    const baseIsCreable = checkFlag(typeIsCreable(typeName), isSearch);

    const hasView = customView || viewDispatcher.hasDefaultView(typeName);

    const hasConstructor = hasAllowedConstructor(typeName);

    return baseIsCreable && hasView && hasConstructor && isCreableEvent.every(f => f(typeName));
}

function hasAllowedConstructor(typeName: string) {
    const ti = getTypeInfo(typeName);

    if (ti == undefined || ti.operations == undefined)
        return true;

    const constructOperations = Dic.getValues(ti.operations).filter(a => a.operationType == OperationType.Constructor);

    if (!constructOperations.length)
        return true;

    const allowed = constructOperations.filter(oi => Operations.isOperationInfoAllowed(oi));

    return allowed.length > 0;
}

function typeIsCreable(typeName: string): EntityWhen {

    const es = entitySettings[typeName];
    if (es != undefined && es.isCreable != undefined)
        return es.isCreable;

    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == undefined)
        return "IsLine";

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


export const isReadonlyEvent: Array<(typeName: string, entity?: EntityPack<ModifiableEntity>) => boolean> = [];

export function isReadOnly(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>) {

    const entityPack = isEntityPack(typeOrEntity) ? typeOrEntity : undefined;

    const typeName = isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type : getTypeName(typeOrEntity as PseudoType);

    const baseIsReadOnly = typeIsReadOnly(typeName);

    return baseIsReadOnly || isReadonlyEvent.some(f => f(typeName, entityPack));
}


function typeIsReadOnly(typeName: string): boolean {

    const es = entitySettings[typeName];
    if (es != undefined && es.isReadOnly != undefined)
        return es.isReadOnly;

    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == undefined)
        return false;

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

    const typeInfo = getTypeInfo(typeName);
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

export const isFindableEvent: Array<(typeName: string) => boolean> = [];

export function isFindable(type: PseudoType, isSearch?: boolean) {

    const typeName = getTypeName(type);

    const baseIsReadOnly = typeIsFindable(typeName);

    return baseIsReadOnly && Finder.isFindable(typeName, true);
}

function typeIsFindable(typeName: string) {

    const es = entitySettings[typeName];

    if (es != undefined && es.isFindable != undefined)
        return es.isFindable;

    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == undefined)
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

export const isViewableEvent: Array<(typeName: string, entityPack?: EntityPack<ModifiableEntity>) => boolean> = [];

export function isViewable(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>, customView = false): boolean {

    const entityPack = isEntityPack(typeOrEntity) ? typeOrEntity : undefined;

    const typeName = isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type : getTypeName(typeOrEntity as PseudoType);

    const baseIsViewable = typeIsViewable(typeName);

    const hasView = customView || viewDispatcher.hasDefaultView(typeName);

    return baseIsViewable && hasView && isViewableEvent.every(f => f(typeName, entityPack));
}


function typeIsViewable(typeName: string): boolean {

    const es = entitySettings[typeName];

    if (es != undefined && es.isViewable != undefined)
        return es.isViewable;

    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == undefined)
        return true;

    if (typeInfo.kind == "Enum")
        return false;

    switch (typeInfo.entityKind) {
        case "SystemString": return false;
        case "System": return true;
        case "Relational": return false;
        case "String": return false;
        case "Shared": return true;
        case "Main": return true;
        case "Part": return true;
        case "SharedPart": return true;
        default: return true;
    }
}

export function isNavigable(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>, customComponent = false, isSearch = false): boolean {

    const entityPack = isEntityPack(typeOrEntity) ? typeOrEntity : undefined;

    const typeName = isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type : getTypeName(typeOrEntity as PseudoType);

    const baseTypeName = checkFlag(typeIsNavigable(typeName), isSearch);

    const hasView = customComponent || viewDispatcher.hasDefaultView(typeName);

    return baseTypeName && hasView && isViewableEvent.every(f => f(typeName, entityPack));
}

function typeIsNavigable(typeName: string): EntityWhen {

    const es = entitySettings[typeName];

    if (es != undefined && es.isNavigable != undefined)
        return es.isNavigable;

    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == undefined)
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

    const types = getTypeInfos(type);

    if (types.length == 1) {
        var s = getSettings(types[0]);

        if (s && s.findOptions) {
            return s.findOptions;
        }
    }

    return undefined;
}

export function getAutoComplete(type: TypeReference, findOptions: FindOptions | undefined, showType?: boolean): AutocompleteConfig<any> | null {
    if (type.isEmbedded || type.name == IsByAll)
        return null;

    var config: AutocompleteConfig<any> | null = null;

    if (findOptions)
        config = new FindOptionsAutocompleteConfig(findOptions);

    const types = getTypeInfos(type);
    var delay: number | undefined;

    if (types.length == 1) {
        var s = getSettings(types[0]);

        if (s) {
            if (s.autocomplete) {
                config = s.autocomplete;
            }

            delay = s.autocompleteDelay;
        }
    }

    if(!config) {
        config = new LiteAutocompleteConfig((ac, subStr: string) => Finder.API.findLiteLike({
            types: type.name,
            subString: subStr,
            count: 5
        }, ac), false, showType == null ? type.name.contains(",") : showType);
    }

    if (!config.getItemsDelay) {
        config.getItemsDelay = delay;
    }

    return config;
}

export interface ViewOptions {
    title?: string;
    propertyRoute?: PropertyRoute;
    readOnly?: boolean;
    modalSize?: BsSize;
    isOperationVisible?: (eoc: Operations.EntityOperationContext<any /*Entity*/>) => boolean;
    validate?: boolean;
    requiresSaveOperation?: boolean;
    avoidPromptLooseChange?: boolean;
    getViewPromise?: (entity: ModifiableEntity) => undefined | string | ViewPromise<ModifiableEntity>;
    extraComponentProps?: {};
}

export function view<T extends ModifiableEntity>(options: EntityPack<T>, viewOptions?: ViewOptions): Promise<T | undefined>;
export function view<T extends ModifiableEntity>(entity: T, viewOptions?: ViewOptions): Promise<T | undefined>;
export function view<T extends Entity>(entity: Lite<T>, viewOptions?: ViewOptions): Promise<T | undefined>
export function view(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, viewOptions?: ViewOptions): Promise<ModifiableEntity | undefined>;
export function view(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, viewOptions?: ViewOptions): Promise<ModifiableEntity | undefined> {

    const typeName = isEntityPack(entityOrPack) ? entityOrPack.entity.Type : getTypeName(entityOrPack);

    const es = getSettings(typeName);

    if (es && es.onView)
        return es.onView(entityOrPack, viewOptions);
    else
        return viewDefault(entityOrPack, viewOptions);
}

export function viewDefault(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, viewOptions?: ViewOptions) {
    return import("./Frames/FrameModal")
        .then(NP => NP.default.openView(entityOrPack, viewOptions || {}));
}

export interface NavigateOptions {
    readOnly?: boolean;
    modalSize?: BsSize;
    avoidPromptLooseChange?: boolean;
    getViewPromise?: (entity: ModifiableEntity) => undefined | string | ViewPromise<ModifiableEntity>;
    extraComponentProps?: {};
}

export function navigate(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, navigateOptions?: NavigateOptions): Promise<void> {

    const typeName = isEntityPack(entityOrPack) ? entityOrPack.entity.Type : getTypeName(entityOrPack);

    const es = getSettings(typeName);

    if (es && es.onNavigate)
        return es.onNavigate(entityOrPack, navigateOptions);
    else
        return navigateDefault(entityOrPack, navigateOptions);
}

export function navigateDefault(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, navigateOptions?: NavigateOptions): Promise<void> {
    return import("./Frames/FrameModal")
        .then(NP => NP.default.openNavigate(entityOrPack, navigateOptions || {}));
}

export function createInNewTab(pack: EntityPack<ModifiableEntity>) {
    var url = createRoute(pack.entity.Type) + "?waitData=true";
    window.dataForChildWindow = pack;
    var win = window.open(url);
}

export function createNavigateOrTab(pack: EntityPack<Entity>, event: React.MouseEvent<any>) {
    if (!pack || !pack.entity)
        return;

    const es = getSettings(pack.entity.Type);
    if (es && es.avoidPopup || event.ctrlKey || event.button == 1) {
        createInNewTab(pack);
    }
    else {
        navigate(pack);
    }
}

export function pushOrOpenInTab(path: string, e: React.MouseEvent<any> | React.KeyboardEvent<any>) {
    if ((e as React.MouseEvent<any>).button == 2)
        return;

    e.preventDefault();
    if (e.ctrlKey || (e as React.MouseEvent<any>).button == 1)
        window.open(toAbsoluteUrl(path));
    else
        history.push(path);
}


export function toEntityPack(entityOrEntityPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>): Promise<EntityPack<ModifiableEntity>> {
    if ((entityOrEntityPack as EntityPack<ModifiableEntity>).canExecute)
        return Promise.resolve(entityOrEntityPack as EntityPack<ModifiableEntity>);

    const entity = (entityOrEntityPack as ModifiableEntity).Type ?
        entityOrEntityPack as ModifiableEntity :
        (entityOrEntityPack as Lite<Entity> | EntityPack<ModifiableEntity>).entity;

    if (entity == undefined)
        return API.fetchEntityPack(entityOrEntityPack as Lite<Entity>);

    let ti = getTypeInfo(entity.Type);
    if (ti  == null || !ti.requiresEntityPack)
        return Promise.resolve({ entity: cloneEntity(entity), canExecute: {} });

    return API.fetchEntityPackEntity(entity as Entity);
}

function cloneEntity(obj: any) {
    return JSON.parse(JSON.stringify(obj));
}

export module API {

    export function fillToStrings(...lites: (Lite<Entity> | null | undefined)[]) : Promise < void> {
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
        return ajaxGet<Array<T>>({ url: "~/api/fetchAll/" + type.typeName });
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

        return ajaxGet<Entity>({ url: "~/api/entity/" + typeName + "/" + id });
    }


    export function fetchEntityPack<T extends Entity>(lite: Lite<T>): Promise<EntityPack<T>>;
    export function fetchEntityPack<T extends Entity>(type: Type<T>, id: number | string): Promise<EntityPack<T>>;
    export function fetchEntityPack(type: PseudoType, id: number | string): Promise<EntityPack<Entity>>;
    export function fetchEntityPack(typeOrLite: PseudoType | Lite<any>, id?: any): Promise<EntityPack<Entity>> {
        
        const typeName = (typeOrLite as Lite<any>).EntityType || getTypeName(typeOrLite as PseudoType);
        let idVal = (typeOrLite as Lite<any>).id != null ? (typeOrLite as Lite<any>).id : id;

        return ajaxGet<EntityPack<Entity>>({ url: "~/api/entityPack/" + typeName + "/" + idVal });
    }


    export function fetchEntityPackEntity<T extends Entity>(entity: T): Promise<EntityPack<T>> {

        return ajaxPost<EntityPack<T>>({ url: "~/api/entityPackEntity" }, entity);
    }

    export function validateEntity(entity: ModifiableEntity): Promise<void> {
        return ajaxPost<void>({ url: "~/api/validateEntity" }, entity);
    }

    export function getType(typeName: string): Promise<TypeEntity | null> {

        return ajaxGet<TypeEntity>({ url: `~/api/reflection/typeEntity/${typeName}` });
    }
}

export interface EntitySettingsOptions<T extends ModifiableEntity> {
    isCreable?: EntityWhen;
    isFindable?: boolean;
    isViewable?: boolean;
    isNavigable?: EntityWhen;
    isReadOnly?: boolean;
    avoidPopup?: boolean;
    autocomplete?: AutocompleteConfig<any>;
    autocompleteDelay?: number;
    getViewPromise?: (entity: T) => ViewPromise<T>;
    onNavigateRoute?: (typeName: string, id: string | number) => string;
    onNavigate?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, navigateOptions?: NavigateOptions) => Promise<void>;
    onView?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, viewOptions?: ViewOptions) => Promise<T | undefined>;
    namedViews?: NamedViewSettings<T>[];
}

export interface ViewOverride<T extends ModifiableEntity> {
    viewName?: string;
    override: (replacer: ViewReplacer<T>) => void;
}

export class EntitySettings<T extends ModifiableEntity> {
    typeName: string;

    avoidPopup!: boolean;

    getViewPromise?: (entity: T) => ViewPromise<T>;

    viewOverrides?: Array<ViewOverride<T>>;

    isCreable?: EntityWhen;
    isFindable?: boolean;
    isViewable?: boolean;
    isNavigable?: EntityWhen;
    isReadOnly?: boolean;
    autocomplete?: AutocompleteConfig<any>;
    autocompleteDelay?: number;
    findOptions?: FindOptions;
    onNavigate?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, navigateOptions?: NavigateOptions) => Promise<void>;
    onView?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, viewOptions?: ViewOptions) => Promise<T | undefined>;
    onNavigateRoute?: (typeName: string, id: string | number) => string;

    namedViews?: { [viewName: string]: NamedViewSettings<T> };

    overrideView(override: (replacer: ViewReplacer<T>) => void, viewName?: string) {
        if (this.viewOverrides == undefined)
            this.viewOverrides = [];

        this.viewOverrides.push({ override, viewName });
    }

    constructor(type: Type<T> | string, getViewModule?: (entity: T) => Promise<ViewModule<any>>, options?: EntitySettingsOptions<T>) {

        this.typeName = (type as Type<T>).typeName || type as string;
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
        var getViewPromise = (getViewModule && ((entity: T) => new ViewPromise(getViewModule(entity)))) || (options && options.getViewPromise);
        if (!getViewPromise)
            throw new Error("setting getViewModule or options.getViewPromise arguments is mandatory");
        this.getViewPromise = getViewPromise;
        Dic.assign(this, options)
    }
}

export type ViewModule<T extends ModifiableEntity> = { default: React.ComponentClass<any /* { ctx: TypeContext<T> }*/> };

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

    withProps(props: {}): ViewPromise<T> {

        var result = new ViewPromise<T>();

        result.promise = this.promise.then(func => {
            return (ctx: TypeContext<T>): React.ReactElement<any> => {
                var result = func(ctx);
                return React.cloneElement(result, props);
            };
        });

        return result;
    }

    applyViewOverrides(typeName: string, viewName?: string): ViewPromise<T> {
        this.promise = this.promise.then(func =>
            viewDispatcher.getViewOverrides(typeName, viewName).then(vos => {
                return (ctx: TypeContext<T>) => {
                    var result = func(ctx);
                    var component = result.type as React.ComponentClass<{ ctx: TypeContext<T> }>;
                    monkeyPatchComponent<T>(component, vos!);
                    return result;
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

function monkeyPatchComponent<T extends ModifiableEntity>(component: React.ComponentClass<{ ctx: TypeContext<T> }>, viewOverrides: ViewOverride<T>[]) {

    if (!component.prototype.render)
        throw new Error("render function not defined in " + component);

    if (component.prototype.render.withViewOverrides)
        return;

    const baseRender = component.prototype.render as () => void;

    component.prototype.render = function (this: React.Component<any, any>) {

        const ctx = this.props.ctx;

        const view = baseRender.call(this);

        const replacer = new ViewReplacer<T>(view, ctx);
        viewOverrides.forEach(vo => vo.override(replacer));
        return replacer.result;
    };

    component.prototype.render.withViewOverrides = true;
}


export function checkFlag(entityWhen: EntityWhen, isSearch: boolean) {
    return entityWhen == "Always" ||
        entityWhen == (isSearch ? "IsSearch" : "IsLine");
}

export type EntityWhen = "Always" | "IsSearch" | "IsLine" | "Never";

declare global {
    interface String {
        formatHtml(...parameters: any[]): React.ReactElement<any>;
    }

    interface Array<T> {
        joinCommaHtml(this: Array<T>, lastSeparator: string): React.ReactElement<any>;
    }
}

String.prototype.formatHtml = function (this: string) {
    const regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    const args = arguments;

    const parts = this.split(regex);

    const result: (string | React.ReactElement<any>)[] = [];
    for (let i = 0; i < parts.length - 4; i += 4) {
        result.push(parts[i]);
        result.push(args[parseInt(parts[i + 1])]);
    }
    result.push(parts[parts.length - 1]);

    return React.createElement("span", undefined, ...result);
};

Array.prototype.joinCommaHtml = function (this: any[], lastSeparator: string) {
    const args = arguments;

    const result: (string | React.ReactElement<any>)[] = [];
    for (let i = 0; i < this.length - 2; i++) {
        result.push(this[i]);
        result.push(", ");
    }

    if (this.length >= 2) {
        result.push(this[this.length - 2]);
        result.push(lastSeparator)
    }

    if (this.length >= 1) {
        result.push(this[this.length - 1]);
    }

    return React.createElement("span", undefined, ...result);
}

export function toAbsoluteUrl(appRelativeUrl: string): string {
    if (appRelativeUrl && appRelativeUrl.startsWith("~/"))
        return window.__baseUrl + appRelativeUrl.after("~/");

    if (appRelativeUrl.startsWith(window.__baseUrl) || appRelativeUrl.startsWith("http"))
        return appRelativeUrl;

    return appRelativeUrl;
}

export function tryConvert(value: any, type: TypeReference): Promise<any> | undefined {

    if (value == null)
        return Promise.resolve(null);

    if (type.isLite) {

        if (isLite(value))
            return Promise.resolve(value);

        if (isEntity(value))
            return Promise.resolve(toLite(value));

        return undefined;
    }

    const ti = getTypeInfo(type.name); 

    if (ti && ti.kind == "Entity") {

        if (isLite(value))
            return API.fetchAndForget(value);

        if (isEntity(value))
            return Promise.resolve(value);

        return undefined;
    }

    if (type.name == "string" || type.name == "Guid" || type.name == "Date" || ti && ti.kind == "Enum") {
        if (typeof value === "string")
            return Promise.resolve(value);

        return undefined;
    }

    if (type.name == "number") {
        if (typeof value === "number")
            return Promise.resolve(value);
    }

    return undefined;
}