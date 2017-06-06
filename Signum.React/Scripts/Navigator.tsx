import * as React from "react"
import * as H from "history"
import { Router, Route, Redirect, RouterChildContext, RouteProps, Switch, match, matchPath } from "react-router"
import { Dic, } from './Globals';
import { ajaxGet, ajaxPost } from './Services';
import { openModal } from './Modals';
import { Lite, Entity, ModifiableEntity, EmbeddedEntity, ModelEntity, LiteMessage, EntityPack, isEntity, isLite, isEntityPack, toLite } from './Signum.Entities';
import { IUserEntity, TypeEntity } from './Signum.Entities.Basics';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, getTypeInfos, getTypeName, isTypeEmbeddedOrValue, isTypeModel, KindOfType, OperationType, TypeReference, IsByAll } from './Reflection';
import { TypeContext } from './TypeContext';
import * as Finder from './Finder';
import { needsCanExecute } from './Operations/EntityOperations';
import * as Operations from './Operations';
import FrameModal from './Frames/FrameModal';
import { ViewReplacer } from './Frames/ReactVisitor'
import { AutocompleteConfig, FindOptionsAutocompleteConfig, LiteAutocompleteConfig } from './Lines/AutocompleteConfig'
import { FindOptions } from './FindOptions'
import { ImportRoute } from "./AsyncImport";
import * as AppRelativeRoutes from "./AppRelativeRoutes";


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
    export let getExpanded: () => boolean;
    export let setExpanded: (isExpanded: boolean) => void;
}

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<ImportRoute path="~/view/:type/:id" onImportModule={() => _import("./Frames/FramePage")} />);
    options.routes.push(<ImportRoute path="~/create/:type" onImportModule={() => _import("./Frames/FramePage")} />);
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
            return LiteMessage.New_G.niceToString().forGenderAndNumber(typeInfo.gender) + " " + typeInfo.niceName;

        return typeInfo.niceName + " " + (entity as Entity).id;
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

export function addSettings(...settings: EntitySettings<any>[]) {
    settings.forEach(s => Dic.addOrThrow(entitySettings, s.type.typeName, s));
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
    hasView(typeName: string): boolean;
    getView(entity: ModifiableEntity): ViewPromise<ModifiableEntity>;
}

export class BasicViewDispatcher implements ViewDispatcher {
    hasView(typeName: string) {
        const settings = getSettings(typeName) as EntitySettings<ModifiableEntity>;

        return (settings && settings.getViewPromise) != null;
    }
    getView(entity: ModifiableEntity) {
        const settings = getSettings(entity.Type) as EntitySettings<ModifiableEntity>;

        if (!settings)
            throw new Error(`No EntitySettings registered for ${entity.Type}`);

        if (!settings.getViewPromise)
            throw new Error(`The EntitySettings registered for ${entity.Type} has not getViewPromise`);

        return settings.getViewPromise(entity).applyViewOverrides(settings);
    }
}

export class DynamicComponentViewDispatcher implements ViewDispatcher {
    hasView(typeName: string) {
        return true;
    }
    getView(entity: ModifiableEntity) {
        const settings = getSettings(entity.Type) as EntitySettings<ModifiableEntity>;

        if (!settings || !settings.getViewPromise)
            return new ViewPromise<ModifiableEntity>(_import('./Lines/DynamicComponent'));

        return settings.getViewPromise(entity).applyViewOverrides(settings);
    }
}

export let viewDispatcher: ViewDispatcher = new DynamicComponentViewDispatcher();

export function getViewPromise<T extends ModifiableEntity>(entity: T): ViewPromise<T> {
    return viewDispatcher.getView(entity);
}

export const isCreableEvent: Array<(typeName: string) => boolean> = [];

export function isCreable(type: PseudoType, customView = false, isSearch = false) {

    const typeName = getTypeName(type);

    const baseIsCreable = checkFlag(typeIsCreable(typeName), isSearch);

    const hasView = customView || viewDispatcher.hasView(typeName);

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

    const allowed = constructOperations.filter(oi => Operations.isOperationAllowed(oi));

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

    return baseIsReadOnly && Finder.isFindable(typeName);
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

    const hasView = customView || viewDispatcher.hasView(typeName);

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

    const hasView = customComponent || viewDispatcher.hasView(typeName);

    return baseTypeName && hasView && isViewableEvent.every(f => f(typeName, entityPack));
}

function typeIsNavigable(typeName: string): EntityWhen {

    const es = entitySettings[typeName];

    if (es != undefined && es.isViewable != undefined)
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

export function getAutoComplete(type: TypeReference, findOptions: FindOptions | undefined): AutocompleteConfig<any> | null {
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
        }, ac), false);
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
    isOperationVisible?: (eoc: Operations.EntityOperationContext<Entity>) => boolean;
    validate?: boolean;
    requiresSaveOperation?: boolean;
    avoidPromptLooseChange?: boolean;
    viewPromise?: ViewPromise<ModifiableEntity>;
    getViewPromise?: (entity: ModifiableEntity) => ViewPromise<ModifiableEntity>;
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
    return _import<{ default: typeof FrameModal }>("./Frames/FrameModal")
        .then(NP => NP.default.openView(entityOrPack, viewOptions || {}));
}

export interface NavigateOptions {
    readOnly?: boolean;
    avoidPromptLooseChange?: boolean;
    viewPromise?: ViewPromise<ModifiableEntity>;
    getViewPromise?: (entity: ModifiableEntity) => ViewPromise<ModifiableEntity>;
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
    return _import<{ default: typeof FrameModal }>("./Frames/FrameModal")
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

export function pushOrOpen(path: string, e: React.MouseEvent<any> | React.KeyboardEvent<any>) {
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

    if (!needsCanExecute(entity))
        return Promise.resolve({ entity: cloneEntity(entity), canExecute: {} });

    return API.fetchCanExecute(entity as Entity);
}

function cloneEntity(obj: any) {
    return JSON.parse(JSON.stringify(obj));
}

export module API {

    export function fillToStrings<T extends Entity>(lites: Lite<T>[]): Promise<void> {

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


    export function fetchCanExecute<T extends Entity>(entity: T): Promise<EntityPack<T>> {

        return ajaxPost<EntityPack<T>>({ url: "~/api/entityPackEntity" }, entity);
    }

    export function validateEntity(entity: ModifiableEntity): Promise<void> {
        return ajaxPost<void>({ url: "~/api/validateEntity" }, entity);
    }

    export function getType(typeName: string): Promise<TypeEntity> {

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
    autocomplete?: AutocompleteConfig<T>;
    autocompleteDelay?: number;
    getViewPromise?: (entity: T) => ViewPromise<T>;
    onNavigateRoute?: (typeName: string, id: string | number) => string;
    onNavigate?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, navigateOptions?: NavigateOptions) => Promise<void>;
    onView?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, viewOptions?: ViewOptions) => Promise<T | undefined>;
}

export class EntitySettings<T extends ModifiableEntity> {
    type: Type<T>;

    avoidPopup: boolean;

    getToString: (entity: T) => string;

    getViewPromise?: (entity: T) => ViewPromise<T>;

    viewOverrides: Array<(replacer: ViewReplacer<T>) => void>;

    isCreable: EntityWhen;
    isFindable: boolean;
    isViewable: boolean;
    isNavigable: EntityWhen;
    isReadOnly: boolean;
    autocomplete?: AutocompleteConfig<T>;
    autocompleteDelay?: number;
    onNavigate?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, navigateOptions?: NavigateOptions) => Promise<void>;
    onView?: (entityOrPack: Lite<Entity & T> | T | EntityPack<T>, viewOptions?: ViewOptions) => Promise<T | undefined>;
    onNavigateRoute?: (typeName: string, id: string | number) => string;

    overrideView(override: (replacer: ViewReplacer<T>) => void) {
        if (this.viewOverrides == undefined)
            this.viewOverrides = [];

        this.viewOverrides.push(override);
    }

    constructor(type: Type<T>, getViewModule?: (entity: T) => Promise<ViewModule<any>>, options?: EntitySettingsOptions<T>) {

        this.type = type;
        this.getViewPromise = getViewModule && (entity => new ViewPromise(getViewModule(entity)));

        Dic.assign(this, options);
    }
}

export type ViewModule<T extends ModifiableEntity> = { default: React.ComponentClass<{ ctx: TypeContext<T> }> };

export class ViewPromise<T extends ModifiableEntity> {
    promise: Promise<(ctx: TypeContext<T>) => React.ReactElement<any>>;

    constructor(promise?: Promise<ViewModule<T>>) {
        if (promise)
            this.promise = promise
                .then(mod => {
                    return (ctx: TypeContext<T>) => React.createElement(mod.default, { ctx });
                });
    }

    static resolve<T extends ModifiableEntity>(getComponent: (ctx: TypeContext<T>) => React.ReactElement<any>) {
        var result = new ViewPromise();
        result.promise = Promise.resolve(getComponent);
        return result;
    }

    withProps(props: {}): ViewPromise<T> {

        var result = new ViewPromise<T>();

        result.promise = this.promise.then(func => {
            return (ctx: TypeContext<T>) => {
                var result = func(ctx);
                return React.cloneElement(result, props);
            };
        });

        return result;
    }

    applyViewOverrides(setting: EntitySettings<T>): ViewPromise<T> {
        this.promise = this.promise.then(func => {
            return (ctx: TypeContext<T>) => {
                var result = func(ctx);
                var component = result.type as React.ComponentClass<{ ctx: TypeContext<T> }>;
                monkeyPatchComponent(component, setting);
                return result;
            };
        });

        return this;
    }

    static flat<T extends ModifiableEntity>(promise: Promise<ViewPromise<T>>): ViewPromise<T> {
        var result = new ViewPromise<T>();
        result.promise = promise.then(vp => vp.promise);
        return result;
    }
}

function monkeyPatchComponent<T extends ModifiableEntity>(component: React.ComponentClass<{ ctx: TypeContext<T> }>, setting: EntitySettings<T>) {

    if (!component.prototype.render)
        throw new Error("render function not defined in " + component);

    if (setting.viewOverrides == undefined || setting.viewOverrides.length == 0)
        return;

    if (component.prototype.render.withViewOverrides)
        return;

    const baseRender = component.prototype.render as () => void;

    component.prototype.render = function (this: React.Component<any, any>) {

        const ctx = this.props.ctx;

        const view = baseRender.call(this);

        const replacer = new ViewReplacer<T>(view, ctx);
        setting.viewOverrides.forEach(vo => vo(replacer));
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