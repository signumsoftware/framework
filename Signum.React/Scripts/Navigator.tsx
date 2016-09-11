import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { Dic, } from './Globals';
import { ajaxGet, ajaxPost } from './Services';
import { openModal } from './Modals';
import { Lite, Entity, ModifiableEntity, EmbeddedEntity, ModelEntity, LiteMessage, EntityPack, isEntity, isLite, isEntityPack, toLite } from './Signum.Entities';
import { IUserEntity, TypeEntity } from './Signum.Entities.Basics';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, getTypeName, isTypeEmbeddedOrValue, isTypeModel, KindOfType, OperationType, TypeReference } from './Reflection';
import { TypeContext } from './TypeContext';
import * as Finder from './Finder';
import { needsCanExecute } from './Operations/EntityOperations';
import * as Operations from './Operations';
import ModalFrame from './Frames/ModalFrame';
import { ViewReplacer } from './Frames/ReactVisitor'


Dic.skipClasses.push(React.Component);
React.Component.prototype.changeState = function (this: React.Component<any, any>, func: (state: any) => void) {
    func(this.state);
    this.forceUpdate();
}

export let currentUser: IUserEntity | undefined;
export function setCurrentUser(user: IUserEntity | undefined) {
    currentUser = user;
}

export let currentHistory: HistoryModule.History;
export function setCurrentHistory(history: HistoryModule.History) {
    currentHistory = history;
}

export namespace Expander {
    export let getExpanded: () => boolean;
    export let setExpanded: (isExpanded: boolean) => void;
}

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<Route path="view/:type/:id" getComponent={(loc, cb) => require(["./Frames/PageFrame"], (Comp) => cb(undefined, Comp.default))} ></Route>);
    options.routes.push(<Route path="create/:type" getComponent={(loc, cb) => require(["./Frames/PageFrame"], (Comp) => cb(undefined, Comp.default))} ></Route>);
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

    return currentHistory.createHref("~/view/" + typeName[0].toLowerCase() + typeName.substr(1) + "/" + id);
}

export function createRoute(type: PseudoType) {
    return currentHistory.createHref("~/create/" + getTypeName(type));
}

export const entitySettings: { [type: string]: EntitySettings<ModifiableEntity> } = {};

export function addSettings(...settings: EntitySettings<any>[]) {
    settings.forEach(s => Dic.addOrThrow(entitySettings, s.type.typeName, s));
}


export function getSettings<T extends ModifiableEntity>(type: Type<T>): EntitySettings<T>;
export function getSettings(type: PseudoType): EntitySettings<ModifiableEntity>;
export function getSettings(type: PseudoType): EntitySettings<ModifiableEntity> {
    const typeName = getTypeName(type);

    return entitySettings[typeName];
}

export function setFallbackViewPromise(newFallback: (entity: ModifiableEntity) => ViewPromise<ModifiableEntity>) {
    fallbackViewPromise = newFallback;
}

export let fallbackViewPromise: (entity: ModifiableEntity) => ViewPromise<ModifiableEntity> =
    e => new ViewPromise<ModifiableEntity>(resolve => require(['./Lines/DynamicComponent'], resolve));

export function getViewPromise<T extends ModifiableEntity>(entity: T): ViewPromise<ModifiableEntity> {

    const settings = getSettings(entity.Type) as EntitySettings<T>;

    if (settings == undefined) {
        if (fallbackViewPromise)
            return fallbackViewPromise(entity);

        throw new Error(`No settings for '${entity.Type}'`);
    }

    if (settings.getViewPromise == undefined) {
        if (fallbackViewPromise)
            return fallbackViewPromise(entity);

        throw new Error(`No getComponent set for settings for '${entity.Type}'`);
    }

    return settings.getViewPromise(entity).applyViewOverrides(settings);
}



export const isCreableEvent: Array<(typeName: string) => boolean> = [];

export function isCreable(type: PseudoType, customView = false, isSearch = false) {

    const typeName = getTypeName(type);

    const baseIsCreable = checkFlag(typeIsCreable(typeName), isSearch);

    const hasView = customView || hasRegisteredViewPromise(typeName);

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

    if (typeInfo.kind == KindOfType.Enum)
        return "Never";

    switch (typeInfo.entityKind) {
        case EntityKind.SystemString: return "Never";
        case EntityKind.System: return "Never";
        case EntityKind.Relational: return "Never";
        case EntityKind.String: return "IsSearch";
        case EntityKind.Shared: return "Always";
        case EntityKind.Main: return "IsSearch";
        case EntityKind.Part: return "IsLine";
        case EntityKind.SharedPart: return "IsLine";
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

    if (typeInfo.kind == KindOfType.Enum)
        return true;

    switch (typeInfo.entityKind) {
        case EntityKind.SystemString: return true;
        case EntityKind.System: return true;
        case EntityKind.Relational: return true;
        case EntityKind.String: return false;
        case EntityKind.Shared: return false;
        case EntityKind.Main: return false;
        case EntityKind.Part: return false;
        case EntityKind.SharedPart: return false;
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

    if (typeInfo.kind == KindOfType.Enum)
        return true;

    switch (typeInfo.entityKind) {
        case EntityKind.SystemString: return true;
        case EntityKind.System: return true;
        case EntityKind.Relational: return false;
        case EntityKind.String: return true;
        case EntityKind.Shared: return true;
        case EntityKind.Main: return true;
        case EntityKind.Part: return false;
        case EntityKind.SharedPart: return true;
        default: return false;
    }
}

export const isViewableEvent: Array<(typeName: string, entityPack?: EntityPack<ModifiableEntity>) => boolean> = [];

export function isViewable(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>, customView = false): boolean {

    const entityPack = isEntityPack(typeOrEntity) ? typeOrEntity : undefined;

    const typeName = isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type : getTypeName(typeOrEntity as PseudoType);

    const baseIsViewable = typeIsViewable(typeName);

    const hasView = customView || hasRegisteredViewPromise(typeName);

    return baseIsViewable && hasView && isViewableEvent.every(f => f(typeName, entityPack));
}

function hasRegisteredViewPromise(typeName: string) {

    const es = entitySettings[typeName];
    if (es)
        return !!es.getViewPromise;

    return !!fallbackViewPromise;
}

function typeIsViewable(typeName: string): boolean {

    const es = entitySettings[typeName];

    if (es != undefined && es.isViewable != undefined)
        return es.isViewable;

    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == undefined)
        return true;

    if (typeInfo.kind == KindOfType.Enum)
        return false;

    switch (typeInfo.entityKind) {
        case EntityKind.SystemString: return false;
        case EntityKind.System: return true;
        case EntityKind.Relational: return false;
        case EntityKind.String: return false;
        case EntityKind.Shared: return true;
        case EntityKind.Main: return true;
        case EntityKind.Part: return true;
        case EntityKind.SharedPart: return true;
        default: return true;
    }
}

export function isNavigable(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>, customComponent = false, isSearch = false): boolean {

    const entityPack = isEntityPack(typeOrEntity) ? typeOrEntity : undefined;

    const typeName = isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type : getTypeName(typeOrEntity as PseudoType);

    const baseTypeName = checkFlag(typeIsNavigable(typeName), isSearch);

    const hasView = customComponent || hasRegisteredViewPromise(typeName);

    return baseTypeName && hasView && isViewableEvent.every(f => f(typeName, entityPack));
}



function typeIsNavigable(typeName: string): EntityWhen {

    const es = entitySettings[typeName];

    if (es != undefined && es.isViewable != undefined)
        return es.isNavigable;

    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == undefined)
        return "Never";

    if (typeInfo.kind == KindOfType.Enum)
        return "Never";

    switch (typeInfo.entityKind) {
        case EntityKind.SystemString: return "Never";
        case EntityKind.System: return "Always";
        case EntityKind.Relational: return "Never";
        case EntityKind.String: return "IsSearch";
        case EntityKind.Shared: return "Always";
        case EntityKind.Main: return "Always";
        case EntityKind.Part: return "Always";
        case EntityKind.SharedPart: return "Always";
        default: return "Never";
    }
}


export interface ViewOptions {
    title?: string;
    propertyRoute?: PropertyRoute;
    readOnly?: boolean;
    showOperations?: boolean;
    validate?: boolean;
    requiresSaveOperation?: boolean;
    avoidPromptLooseChange?: boolean;
    viewPromise?: ViewPromise<ModifiableEntity>;
    extraComponentProps?: {};
}

export function view<T extends ModifiableEntity>(options: EntityPack<T>, viewOptions?: ViewOptions): Promise<T>;
export function view<T extends ModifiableEntity>(entity: T, viewOptions?: ViewOptions): Promise<T>;
export function view<T extends Entity>(entity: Lite<T>, viewOptions?: ViewOptions): Promise<T>
export function view(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, viewOptions?: ViewOptions): Promise<ModifiableEntity>;
export function view(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, viewOptions?: ViewOptions): Promise<ModifiableEntity> {
    return new Promise<ModifiableEntity>((resolve, reject) => {
        require(["./Frames/ModalFrame"], function (NP: { default: typeof ModalFrame }) {
            NP.default.openView(entityOrPack, viewOptions || {}).then(resolve, reject);
        });
    });
}


export interface NavigateOptions {
    readOnly?: boolean;
    avoidPromptLooseChange?: boolean;
    viewPromise?: ViewPromise<ModifiableEntity>;
    extraComponentProps?: {};
}

export function navigate(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, navigateOptions?: NavigateOptions): Promise<void> {

    return new Promise<void>((resolve, reject) => {
        require(["./Frames/ModalFrame"], function (NP: { default: typeof ModalFrame }) {
            NP.default.openNavigate(entityOrPack, navigateOptions || {}).then(resolve, reject);
        });
    });
}

export function createInNewTab(pack: EntityPack<ModifiableEntity>) {
    var url = createRoute(pack.entity.Type) + "?waitData=true";
    var win = window.open(url);
    win.parentWindowData = pack;
}

export function createNavigateOrTab(pack: EntityPack<Entity>, event: React.MouseEvent) {
    if (!pack || !pack.entity)
        return;

    const es = getSettings(pack.entity.Type);
    if (es.avoidPopup || event.ctrlKey || event.button == 1) {
        createInNewTab(pack);
    }
    else {
        navigate(pack);
    }
}


export function toEntityPack(entityOrEntityPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, showOperations: boolean): Promise<EntityPack<ModifiableEntity>> {
    if ((entityOrEntityPack as EntityPack<ModifiableEntity>).canExecute)
        return Promise.resolve(entityOrEntityPack);

    const entity = (entityOrEntityPack as ModifiableEntity).Type ?
        entityOrEntityPack as ModifiableEntity :
        (entityOrEntityPack as Lite<Entity> | EntityPack<ModifiableEntity>).entity;

    if (entity == undefined)
        return API.fetchEntityPack(entityOrEntityPack as Lite<Entity>);

    if (!showOperations || !needsCanExecute(entity))
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
            return Promise.resolve<void>();

        return ajaxPost<string[]>({ url: "~/api/entityToStrings" }, realLites).then(strs => {
            realLites.forEach((l, i) => l.toStr = strs[i]);
        });
    }

    export function fetchAll<T extends Entity>(type: Type<T>): Promise<Array<T>> {
        return ajaxGet<Array<Entity>>({ url: "~/api/fetchAll/" + type.typeName });
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

        return fetchEntity(lite.EntityType, lite.id);
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
        let idVal = (typeOrLite as Lite<any>).id || id;

        return ajaxGet<EntityPack<Entity>>({ url: "~/api/entityPack/" + typeName + "/" + idVal });
    }


    export function fetchCanExecute<T extends Entity>(entity: T): Promise<EntityPack<T>> {

        return ajaxPost<EntityPack<Entity>>({ url: "~/api/entityPackEntity" }, entity);
    }

    export function validateEntity(entity: ModifiableEntity): Promise<void> {
        return ajaxPost<void>({ url: "~/api/validateEntity" }, entity);
    }

    export function getType(typeName: string): Promise<TypeEntity> {

        return ajaxGet<TypeEntity>({ url: `~/api/reflection/typeEntity/${typeName}` });
    }
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

    overrideView(override: (replacer: ViewReplacer<T>) => void) {
        if (this.viewOverrides == undefined)
            this.viewOverrides = [];

        this.viewOverrides.push(override);
    }

    constructor(type: Type<T>, getViewPromise?: (entity: T) => ViewPromise<any>,
        options?: { isCreable?: EntityWhen, isFindable?: boolean; isViewable?: boolean; isNavigable?: EntityWhen; isReadOnly?: boolean, avoidPopup?: boolean }) {

        this.type = type;
        this.getViewPromise = getViewPromise;

        Dic.extend(this, options);
    }
}

export type ViewModule<T extends ModifiableEntity> = { default: React.ComponentClass<{ ctx: TypeContext<T> }> };

export class ViewPromise<T extends ModifiableEntity> {
    promise: Promise<(ctx: TypeContext<T>) => React.ReactElement<any>>;

    constructor(callback: (loadModule: (module: ViewModule<T>) => void) => void) {
        this.promise = new Promise<ViewModule<T>>(callback)
            .then(mod => {
                return (ctx: TypeContext<T>) => React.createElement(mod.default, { ctx });
            });
    }

    withProps(componentParams: {} | Promise<{}>): ViewPromise<T> {
        this.promise = this.promise.then(func =>
            Promise.resolve(componentParams).then(params => {
                return (ctx: TypeContext<T>) => {
                    var result = func(ctx);
                    return React.cloneElement(result, params);
                };
            }));

        return this;
    }

    applyViewOverrides(setting: EntitySettings<T>): ViewPromise<T> {
        this.promise = this.promise.then(func => {
            return (ctx: TypeContext<T>) => {
                var result = func(ctx);
                applyViewOverrides(setting, result.type as React.ComponentClass<{ ctx: TypeContext<T> }>);
                return result;
            };
        });

        return this;
    }
}

function applyViewOverrides<T extends ModifiableEntity>(setting: EntitySettings<T>, component: React.ComponentClass<{ ctx: TypeContext<T> }>) {

    if (!component.prototype.render)
        throw new Error("render function not defined in " + component);

    if (setting.viewOverrides == undefined || setting.viewOverrides.length == 0)
        return component;


    if (component.prototype.render.withViewOverrides)
        return component;

    const baseRender = component.prototype.render as () => void;

    component.prototype.render = function (this: React.Component<any, any>) {

        const ctx = this.props.ctx;

        const view = baseRender.call(this);

        const replacer = new ViewReplacer<T>(view, ctx);
        setting.viewOverrides.forEach(vo => vo(replacer));
        return replacer.result;
    };

    component.prototype.render.withViewOverrides = true;

    return component;
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

function fixBaseName<T>(baseFunction: (location?: HistoryModule.LocationDescriptorObject | string) => T, baseName: string): (location?: HistoryModule.LocationDescriptorObject | string) => T {

    function fixUrl(url: string): string {
        if (url && url.startsWith("~/"))
            return baseName + url.after("~/");

        if (url.startsWith(baseName) || url.startsWith("http"))
            return url;

        console.warn(url);
        return url;
    }

    return (location) => {
        if (typeof location === "string") {
            return baseFunction(fixUrl(location));
        } else {
            location!.pathname = fixUrl(location!.pathname!);
            return baseFunction(location);
        }
    };
}

export function useAppRelativeBasename(history: HistoryModule.History, baseName: string) {
    history.push = fixBaseName(history.push, baseName);
    history.replace = fixBaseName(history.replace, baseName);
    history.createHref = fixBaseName(history.createHref, baseName);
    history.createPath = fixBaseName(history.createPath, baseName);
    history.createLocation = fixBaseName(history.createLocation, baseName);
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

    if (getTypeInfo(type.name) && getTypeInfo(type.name).kind == KindOfType.Entity) {

        if (isLite(value))
            return API.fetchAndForget(value);

        if (isEntity(value))
            return Promise.resolve(value);

        return undefined;
    }

    if (type.name == "string" || type.name == "Guid" || type.name == "Date") {
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