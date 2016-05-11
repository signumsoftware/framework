import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { Dic, } from './Globals';
import { ajaxGet, ajaxPost } from './Services';
import { openModal } from './Modals';
import { Lite, Entity, ModifiableEntity, EmbeddedEntity, LiteMessage, EntityPack, isEntity, isLite, isEntityPack } from './Signum.Entities';
import { IUserEntity } from './Signum.Entities.Basics';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, getTypeName, isEmbedded  } from './Reflection';
import { TypeContext } from './TypeContext';
import * as Finder from './Finder';
import { needsCanExecute } from './Operations/EntityOperations';
import ModalFrame from './Frames/ModalFrame';
import { ViewReplacer } from  './Frames/ReactVisitor'

export let NotFound: __React.ComponentClass<any>;

export let currentUser: IUserEntity;
export let currentHistory: HistoryModule.History;


export var getExpanded : () => boolean;
export var setExpanded : (isExpanded: boolean) => void;

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<Route path="view/:type/:id" getComponent={(loc, cb) => require(["./Frames/PageFrame"], (Comp) => cb(null, Comp.default)) } ></Route>);
    options.routes.push(<Route path="create/:type" getComponent={(loc, cb) => require(["./Frames/PageFrame"], (Comp) => cb(null, Comp.default))} ></Route>);
}


export function getTypeTitle(entity: ModifiableEntity, pr: PropertyRoute) {

    const typeInfo = getTypeInfo(entity.Type)

    if (!typeInfo) {
        return pr.typeReference().typeNiceName;
    }

    else {
        if (entity.isNew)
            return LiteMessage.New_G.niceToString().forGenderAndNumber(typeInfo.gender) + " " + typeInfo.niceName;

        return typeInfo.niceName + " " + (entity as Entity).id;
    }
}


export function navigateRoute(entity: Entity);
export function navigateRoute(lite: Lite<Entity>);
export function navigateRoute(type: PseudoType, id: any);
export function navigateRoute(typeOrEntity: Entity | Lite<Entity> | PseudoType, id: any = null) {
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

    return currentHistory.createHref("/view/" + typeName[0].toLowerCase() + typeName.substr(1) + "/" + id);
}

export function createRoute(type: PseudoType) {
    return currentHistory.createHref("/create/" + getTypeName(type));
}

export const entitySettings: { [type: string]: EntitySettings<ModifiableEntity> } = {};

export function addSettings(...settings: EntitySettings<any>[]) {
    settings.forEach(s=> Dic.addOrThrow(entitySettings, s.type.typeName, s));
}


export function getSettings<T extends ModifiableEntity>(type: Type<T>): EntitySettings<T>;
export function getSettings(type: PseudoType): EntitySettings<ModifiableEntity>;
export function getSettings(type: PseudoType): EntitySettings<ModifiableEntity> {
    const typeName = getTypeName(type);

    return entitySettings[typeName];
}

export let fallbackGetComponent: (entity: ModifiableEntity) => Promise<{ default: React.ComponentClass<{ ctx: TypeContext<ModifiableEntity> }> }> =
    e => new Promise(resolve => require(['./Lines/DynamicComponent'], resolve));

export function getComponent<T extends ModifiableEntity>(entity: T): Promise<React.ComponentClass<{ ctx: TypeContext<T> }>> {

    var settings = getSettings(entity.Type) as EntitySettings<T>;

    if (settings == null) {
        if (fallbackGetComponent)
            return fallbackGetComponent(entity).then(a => a.default);

        throw new Error(`No settings for '${entity.Type}'`);
    }

    if (settings.getComponent == null)
        throw new Error(`No getComponent set for settings for '${entity.Type}'`);

    return settings.getComponent(entity).then(a => applyViewOverrides(settings, a.default));
}

function applyViewOverrides<T extends ModifiableEntity>(setting: EntitySettings<T>, component: React.ComponentClass<{ ctx: TypeContext<T> }>) {

    if (!component.prototype.render)
        throw new Error("render function not defined in " + component);

    if (setting.viewOverrides == null || setting.viewOverrides.length == 0)
        return component;


    if (component.prototype.render.withViewOverrides)
        return component;

    var baseRender = component.prototype.render as () => void;

    component.prototype.render = function () {

        var ctx = this.props.ctx;

        var view = baseRender.call(this);

        var replacer = new ViewReplacer<T>(view, ctx);
        setting.viewOverrides.forEach(vo => vo(replacer));
        return replacer.result;
    };

    component.prototype.render.withViewOverrides = true;

    return component;
}

export const isCreableEvent: Array<(typeName: string) => boolean> = [];

export function isCreable(type: PseudoType, customView = false, isSearch = false) {

    const typeName = getTypeName(type);
    
    const baseIsCreable = checkFlag(typeIsCreable(typeName), isSearch);

    const hasView = customView || hasRegisteredView(typeName);

    return baseIsCreable && hasView && isCreableEvent.every(f => f(typeName));
}


function typeIsCreable(typeName: string): EntityWhen {

    const es = entitySettings[typeName];
    if (es != null && es.isCreable != null)
        return es.isCreable;
    
    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == null)
        return EntityWhen.IsLine;

    switch (typeInfo.entityKind) {
        case EntityKind.SystemString: return EntityWhen.Never;
        case EntityKind.System: return EntityWhen.Never;
        case EntityKind.Relational: return EntityWhen.Never;
        case EntityKind.String: return EntityWhen.IsSearch;
        case EntityKind.Shared: return EntityWhen.Always;
        case EntityKind.Main: return EntityWhen.IsSearch;
        case EntityKind.Part: return EntityWhen.IsLine;
        case EntityKind.SharedPart: return EntityWhen.IsLine;
        default: throw new Error("Unexpected kind");
    }
}


export const isReadonlyEvent: Array<(typeName: string, entity?: EntityPack<ModifiableEntity>) => boolean> = [];

export function isReadOnly(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>) {

    const entityPack = isEntityPack(typeOrEntity) ? typeOrEntity : null;

    const typeName = isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type : getTypeName(typeOrEntity as PseudoType);

    const baseIsReadOnly = typeIsReadOnly(typeName);

    return baseIsReadOnly || isReadonlyEvent.some(f => f(typeName, entityPack));
}


function typeIsReadOnly(typeName: string): boolean {

    const es = entitySettings[typeName];
    if (es != null && es.isReadOnly != null)
        return es.isReadOnly;
    
    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == null)
        return false;

    switch (typeInfo.entityKind) {
        case EntityKind.SystemString: return true;
        case EntityKind.System: return true;
        case EntityKind.Relational: return true;
        case EntityKind.String: return false;
        case EntityKind.Shared: return false;
        case EntityKind.Main: return false;
        case EntityKind.Part: return false;
        case EntityKind.SharedPart: return false;
        default: throw new Error("Unexpected kind");
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

    if (es != null && es.isFindable != null)
        return es.isFindable;

    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == null)
        return false;

    switch (typeInfo.entityKind) {
        case EntityKind.SystemString: return true;
        case EntityKind.System: return true;
        case EntityKind.Relational: return false;
        case EntityKind.String: return true;
        case EntityKind.Shared: return true;
        case EntityKind.Main: return true;
        case EntityKind.Part: return false;
        case EntityKind.SharedPart: return true;
        default: throw new Error("Unexpected kind");
    }
}

export const isViewableEvent: Array<(typeName: string, entityPack?: EntityPack<ModifiableEntity>) => boolean> = []; 

export function isViewable(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>, customView = false): boolean{

    const entityPack = isEntityPack(typeOrEntity) ? typeOrEntity : null;

    const typeName = isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type : getTypeName(typeOrEntity as PseudoType);

    const baseIsViewable = typeIsViewable(typeName);
    
    const hasView = customView || hasRegisteredView(typeName);

    return baseIsViewable && hasView && isViewableEvent.every(f => f(typeName, entityPack));
}

function hasRegisteredView(typeName: string) {
        
    const es = entitySettings[typeName];
    if (es)
        return !!es.getComponent;

    return !!fallbackGetComponent;
}

function typeIsViewable(typeName: string): boolean {

    const es = entitySettings[typeName];

    if (es != null && es.isViewable != null)
        return es.isViewable;

    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == null)
        return true;

    switch (typeInfo.entityKind) {
        case EntityKind.SystemString: return false;
        case EntityKind.System: return true;
        case EntityKind.Relational: return false;
        case EntityKind.String: return false;
        case EntityKind.Shared: return true;
        case EntityKind.Main: return true;
        case EntityKind.Part: return true;
        case EntityKind.SharedPart: return true;
        default: throw new Error("Unexpected kind");
    }
}

export function isNavigable(typeOrEntity: PseudoType | EntityPack<ModifiableEntity>, customView = false, isSearch = false): boolean {

    const entityPack = isEntityPack(typeOrEntity) ? typeOrEntity : null;

    const typeName = isEntityPack(typeOrEntity) ? typeOrEntity.entity.Type : getTypeName(typeOrEntity as PseudoType);

    const baseTypeName = checkFlag(typeIsNavigable(typeName), isSearch);

    const hasView = customView || hasRegisteredView(typeName);

    return baseTypeName && hasView && isViewableEvent.every(f => f(typeName, entityPack));
}



function typeIsNavigable(typeName: string): EntityWhen {

    const es = entitySettings[typeName];

    if (es != null && es.isViewable != null)
        return es.isNavigable;

    const typeInfo = getTypeInfo(typeName);
    if (typeInfo == null)
        return EntityWhen.Never;

    switch (typeInfo.entityKind) {
        case EntityKind.SystemString: return EntityWhen.Never;
        case EntityKind.System: return EntityWhen.Always;
        case EntityKind.Relational: return EntityWhen.Never;
        case EntityKind.String: return EntityWhen.IsSearch;
        case EntityKind.Shared: return EntityWhen.Always;
        case EntityKind.Main: return EntityWhen.Always;
        case EntityKind.Part: return EntityWhen.Always;
        case EntityKind.SharedPart: return EntityWhen.Always;
        default: throw new Error("Unexpected kind");
    }
}




export interface ViewOptions {
    entity: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>;
    propertyRoute?: PropertyRoute;
    readOnly?: boolean;
    showOperations?: boolean;
    requiresSaveOperation?: boolean;
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
}

export function view(options: ViewOptions): Promise<ModifiableEntity>;
export function view<T extends ModifiableEntity>(entity: T, propertyRoute?: PropertyRoute): Promise<T>;
export function view<T extends Entity>(entity: Lite<T>): Promise<T>
export function view(entityOrOptions: ViewOptions | ModifiableEntity | Lite<Entity>, propertyRoute?: PropertyRoute): Promise<ModifiableEntity>
{
    const options = (entityOrOptions as ModifiableEntity).Type ? { entity: entityOrOptions as ModifiableEntity, propertyRoute: propertyRoute } as ViewOptions :
        (entityOrOptions as Lite<Entity>).EntityType ? { entity: entityOrOptions as Lite<Entity> } as ViewOptions :
            entityOrOptions as ViewOptions;

    return new Promise<ModifiableEntity>((resolve, reject) => {
        require(["./Frames/ModalFrame"], function (NP: { default: typeof ModalFrame }) {
            NP.default.openView(options).then(resolve, reject);
        });
    });
}


export interface NavigateOptions {
    entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>;
    readOnly?: boolean;
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
}

export function navigate(options: NavigateOptions): Promise<void>;
export function navigate<T extends ModifiableEntity>(entity: T | EntityPack<T>, propertyRoute?: PropertyRoute): Promise<void>;
export function navigate<T extends Entity>(entity: Lite<T>): Promise<void>
export function navigate(entityOrOptions: NavigateOptions | Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>): Promise<void> {

    const options = (entityOrOptions as ModifiableEntity).Type ? { entityOrPack: entityOrOptions as ModifiableEntity } as NavigateOptions :
        (entityOrOptions as Lite<Entity>).EntityType ? { entityOrPack: entityOrOptions as Lite<Entity> } as NavigateOptions :
            (entityOrOptions as EntityPack<ModifiableEntity>).entity ? { entityOrPack: entityOrOptions as EntityPack<ModifiableEntity> } as NavigateOptions :
                entityOrOptions as NavigateOptions;

    return new Promise<void>((resolve, reject) => {
        require(["./Frames/ModalFrame"], function (NP: { default: typeof ModalFrame }) {
            NP.default.openNavigate(options).then(resolve, reject);
        });
    });
} 


export function toEntityPack(entityOrEntityPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, showOperations: boolean): Promise<EntityPack<ModifiableEntity>> {
    if ((entityOrEntityPack as EntityPack<ModifiableEntity>).canExecute)
        return Promise.resolve(entityOrEntityPack);

    const entity = (entityOrEntityPack as ModifiableEntity).Type ?
        entityOrEntityPack as ModifiableEntity :
        (entityOrEntityPack as Lite<Entity> | EntityPack<ModifiableEntity>).entity;

    if (entity == null)
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

        var realLites = lites.filter(a => a.toStr == null && a.entity == null);

        if (!realLites.length)
            return Promise.resolve<void>();

        return ajaxPost<string[]>({ url: "/api/entityToStrings" }, realLites).then(strs => {
            realLites.forEach((l, i) => l.toStr = strs[i]);
        });
    }

    export function fetchAll<T extends Entity>(type: Type<T>): Promise<Array<T>> {
        return ajaxGet<Array<Entity>>({ url: "/api/fetchAll/" + type.typeName });
    }

    export function fetchAndRemember<T extends Entity>(lite: Lite<T>): Promise<T> {
        if (lite.entity)
            return Promise.resolve(lite.entity);

        return fetchEntity(lite.EntityType, lite.id).then(e => lite.entity = e as T);
    }

    export function fetchAndForget<T extends Entity>(lite: Lite<T>): Promise<T> {
        return fetchEntity(lite.EntityType, lite.id);
    }
    
    export function fetchEntity<T extends Entity>(type: Type<T>, id: any): Promise<T>;
    export function fetchEntity(type: PseudoType, id: any): Promise<Entity>;
    export function fetchEntity(type: PseudoType, id?: any): Promise<Entity> {

        const typeName = getTypeName(type);
        let idVal = id;

        return ajaxGet<Entity>({ url: "/api/entity/" + typeName + "/" + id });
    }


    export function fetchEntityPack<T extends Entity>(lite: Lite<T>): Promise<EntityPack<T>>;
    export function fetchEntityPack<T extends Entity>(type: Type<T>, id: any): Promise<EntityPack<T>>;
    export function fetchEntityPack(type: PseudoType, id: any): Promise<EntityPack<Entity>>;
    export function fetchEntityPack(typeOrLite: PseudoType | Lite<any>, id?: any): Promise<EntityPack<Entity>> {

        const typeName = (typeOrLite as Lite<any>).EntityType || getTypeName(typeOrLite as PseudoType);
        let idVal = (typeOrLite as Lite<any>).id || id;

        return ajaxGet<EntityPack<Entity>>({ url: "/api/entityPack/" + typeName + "/" + idVal });
    }


    export function fetchCanExecute<T extends Entity>(entity: T): Promise<EntityPack<T>> {

        return ajaxPost<EntityPack<Entity>>({ url: "/api/entityPackEntity" }, entity);
    }
}


export class EntitySettings<T extends ModifiableEntity> {
    type: Type<T>;

    avoidPopup: boolean;

    getToString: (entity: T) => string;

    getComponent: (entity: T) => Promise<{ default: React.ComponentClass<{ ctx: TypeContext<T> }> }>;

    viewOverrides: Array<(replacer: ViewReplacer<T>) => void>;

    isCreable: EntityWhen;
    isFindable: boolean;
    isViewable: boolean;
    isNavigable: EntityWhen;
    isReadOnly: boolean;

    overrideView(override: (replacer: ViewReplacer<T>) => void) {
        if (this.viewOverrides == null)
            this.viewOverrides = [];

        this.viewOverrides.push(override);
    }

    constructor(type: Type<T>, getComponent: (entity: T) => Promise<any>,
        options?: { isCreable?: EntityWhen, isFindable?: boolean; isViewable?: boolean; isNavigable?: EntityWhen; isReadOnly?: boolean }) {

        this.type = type;
        this.getComponent = getComponent;

        Dic.extend(this, options);
    }
}

export function checkFlag(entityWhen: EntityWhen, isSearch: boolean) {
    return entityWhen == EntityWhen.Always ||
        entityWhen == (isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
}

export enum EntityWhen {
    Always = "Always" as any,
    IsSearch = "IsSearch" as any,
    IsLine = "IsLine" as any,
    Never = "Never" as any,
}

declare global {
    interface String {
        formatHtml(...parameters: any[]): React.ReactElement<any>;
    }
}

String.prototype.formatHtml = function () {
    const regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    const args = arguments;

    const parts = this.split(regex);

    const result = [];
    for (let i = 0; i < parts.length - 4; i += 4) {
        result.push(parts[i]);
        result.push(args[parts[i + 1]]);
    }
    result.push(parts[parts.length - 1]);

    return React.createElement("span", null, ...result);
};

