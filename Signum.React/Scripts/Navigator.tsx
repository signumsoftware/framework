import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { Dic, } from './Globals';
import { ajaxGet, ajaxPost } from './Services';
import { openModal } from './Modals';
import { IEntity, Lite, Entity, ModifiableEntity, EmbeddedEntity, LiteMessage, EntityPack } from './Signum.Entities';
import { PropertyRoute, PseudoType, EntityKind, TypeInfo, IType, Type, getTypeInfo, getTypeName  } from './Reflection';
import { TypeContext } from './TypeContext';
import { EntityComponent, EntityComponentProps, EntityFrame } from './Lines';
import * as Finder from './Finder';
import { needsCanExecute } from './Operations/EntityOperations';
import ModalFrame from './Frames/ModalFrame';
import { ViewReplacer } from  './Frames/ReactVisitor'

export let NotFound: __React.ComponentClass<any>;

export let currentUser: IEntity;
export let currentHistory: HistoryModule.History;



export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<Route path="view/:type/:id" getComponent={(loc, cb) => require(["./Frames/PageFrame"], (Comp) => cb(null, Comp.default)) } ></Route>);
    options.routes.push(<Route path="create/:type" getComponent={(loc, cb) => require(["./Frames/PageFrame"], (Comp) => cb(null, Comp.default))} ></Route>);
}


export function getTypeTitle(entity: Entity, pr: PropertyRoute) {

    const typeInfo = getTypeInfo(entity.Type)

    if (!typeInfo) {
        return pr.typeReference().typeNiceName;
    }

    else {
        if (entity.isNew)
            return LiteMessage.New_G.niceToString().forGenderAndNumber(typeInfo.gender) + " " + typeInfo.niceName;

        return typeInfo.niceName + " " + entity.id;
    }
}


export function navigateRoute(entity: IEntity);
export function navigateRoute(lite: Lite<IEntity>);
export function navigateRoute(type: PseudoType, id: any);
export function navigateRoute(typeOfEntity: any, id: any = null) {
    let typeName: string;
    if ((typeOfEntity as IEntity).Type) {
        typeName = (typeOfEntity as IEntity).Type;
        id = (typeOfEntity as IEntity).id;
    }
    else if ((typeOfEntity as Lite<IEntity>).EntityType) {
        typeName = (typeOfEntity as Lite<IEntity>).EntityType;
        id = (typeOfEntity as Lite<IEntity>).id;
    }
    else {
        typeName = getTypeName(typeOfEntity as PseudoType);
    }

    return currentHistory.createHref("/view/" + typeName[0].toLowerCase() + typeName.substr(1) + "/" + id);
}

export const entitySettings: { [type: string]: EntitySettingsBase<ModifiableEntity> } = {};

export function addSettings(...settings: EntitySettingsBase<any>[]) {
    settings.forEach(s=> Dic.addOrThrow(entitySettings, s.type.typeName, s));
}


export function getSettings<T extends ModifiableEntity>(type: Type<T>): EntitySettingsBase<T>;
export function getSettings(type: PseudoType): EntitySettingsBase<ModifiableEntity>;
export function getSettings(type: PseudoType): EntitySettingsBase<ModifiableEntity> {
    const typeName = getTypeName(type);

    return entitySettings[typeName];
}


export function getComponent<T extends ModifiableEntity>(entity: T): Promise<React.ComponentClass<EntityComponentProps<T>>> {

    var settings = getSettings(entity.Type);

    if (settings == null)
        throw new Error(`No settings for '${entity.Type}'`);

    if (settings.getComponent == null)
        throw new Error(`No getComponent set for settings for '${entity.Type}'`);

    return settings.getComponent(entity).then(a => a.default);
}

export const isCreableEvent: Array<(typeName: string) => boolean> = [];

export function isCreable(type: PseudoType, isSearch?: boolean) {

    const typeName = getTypeName(type);

    const es = entitySettings[typeName];
    
    return (es == null || isSearch == null || es.onIsCreable(isSearch)) &&  isCreableEvent.every(f => f(typeName));
}

export const isReadonlyEvent: Array<(typeName: string) => boolean> = [];

export function isReadOnly(type: PseudoType, isSearch?: boolean) {

    const typeName = getTypeName(type);

    const es = entitySettings[typeName];

    return (es == null || es.onIsReadonly()) && isReadonlyEvent.every(f => f(typeName));
}

export const isFindableEvent: Array<(typeName: string) => boolean> = []; 

export function isFindable(type: PseudoType, isSearch?: boolean) {

    const typeName = getTypeName(type);

    if (!Finder.isFindable(typeName))
        return false;

    const es = entitySettings[typeName];
    if (es && !es.onIsFindable())
        return false;

    return true;
}

export const isViewableEvent: Array<(typeName: string, entity?: ModifiableEntity) => boolean> = []; 

export function isViewable(typeOrEntity: PseudoType | ModifiableEntity, customView = false): boolean{
    const entity = (typeOrEntity as ModifiableEntity).Type ? typeOrEntity as ModifiableEntity : null;

    const typeName = entity ? entity.Type : getTypeName(typeOrEntity as PseudoType);

    const es = entitySettings[typeName];

    return (es == null ? customView : es.onIsViewable(customView)) && isViewableEvent.every(f => f(typeName, entity));
}

export function isNavigable(typeOrEntity: PseudoType | ModifiableEntity, customView = false, isSearch = false): boolean {

    const entity = (typeOrEntity as ModifiableEntity).Type ? typeOrEntity as Entity : null;

    const typeName = entity ? entity.Type : getTypeName(typeOrEntity as PseudoType);

    const es = entitySettings[typeName];

    return (es == null ? customView : es.onIsNavigable(customView, isSearch)) && isViewableEvent.every(f => f(typeName, entity));
}



export interface ViewOptions {
    entity: Lite<IEntity> | ModifiableEntity | EntityPack<ModifiableEntity>;
    propertyRoute?: PropertyRoute;
    readOnly?: boolean;
    showOperations?: boolean;
    requiresSaveOperation?: boolean;
    getComponent?: (ctx: TypeContext<ModifiableEntity>, frame: EntityFrame<ModifiableEntity>) => React.ReactElement<any>;
}

export function view(options: ViewOptions): Promise<ModifiableEntity>;
export function view<T extends ModifiableEntity>(entity: T, propertyRoute?: PropertyRoute): Promise<T>;
export function view<T extends IEntity>(entity: Lite<T>): Promise<T>
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
    entity: Lite<IEntity> | ModifiableEntity | EntityPack<ModifiableEntity>;
    readOnly?: boolean;
    getComponent?: (ctx: TypeContext<ModifiableEntity>, frame: EntityFrame<ModifiableEntity>) => React.ReactElement<any>;
}

export function navigate(options: NavigateOptions): Promise<void>;
export function navigate<T extends ModifiableEntity>(entity: T, propertyRoute?: PropertyRoute): Promise<void>;
export function navigate<T extends IEntity>(entity: Lite<T>): Promise<void>
export function navigate(entityOrOptions: NavigateOptions | ModifiableEntity | Lite<Entity>): Promise<void> {
    const options = (entityOrOptions as ModifiableEntity).Type ? { entity: entityOrOptions } as NavigateOptions :
        (entityOrOptions as Lite<Entity>).EntityType ? { entity: entityOrOptions } as NavigateOptions :
            (entityOrOptions as EntityPack<ModifiableEntity>).entity ? { entity: entityOrOptions } as NavigateOptions :
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
        (entityOrEntityPack as Lite<Entity>).entity;

    if (entity == null)
        return API.fetchEntityPack(entityOrEntityPack as Lite<Entity>);

    if (!showOperations || !needsCanExecute(entity))
        return Promise.resolve({ entity: cloneEntity(entity), canExecute: {} });

    return API.fetchCanExecute(entity);
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


export abstract class EntitySettingsBase<T extends ModifiableEntity> {
    type: Type<T>;

    avoidPopup: boolean;

    abstract onIsCreable(isSearch: boolean): boolean;
    abstract onIsFindable(): boolean;
    abstract onIsViewable(customView: boolean): boolean;
    abstract onIsNavigable(customView: boolean, isSearch: boolean): boolean;
    abstract onIsReadonly(): boolean;

    getToString: (entity: T) => string;

    getComponent: (entity: T) => Promise<{ default: React.ComponentClass<EntityComponentProps<T>> }>;

    viewOverrides: Array<(replacer: ViewReplacer<T>) => void>;

    overrideView(override: (replacer: ViewReplacer<T>) => void) {
        if (this.viewOverrides == null)
            this.viewOverrides = [];

        this.viewOverrides.push(override);
    }
    
    constructor(type: Type<T>, getComponent: (entity: T) => Promise<any>) {
        this.type = type;
        this.getComponent = getComponent;
    }
}

export class EntitySettings<T extends Entity> extends EntitySettingsBase<T> {    

    isCreable: EntityWhen;
    isFindable: boolean;
    isViewable: boolean;
    isNavigable: EntityWhen;
    isReadOnly: boolean;

    constructor(type: Type<T>, getComponent: (entity: T) => Promise<any>,
        options?: { isCreable?: EntityWhen, isFindable?: boolean; isViewable?: boolean; isNavigable?: EntityWhen; isReadOnly?: boolean }) {
        super(type, getComponent);

        switch (type.typeInfo().entityKind) {
            case EntityKind.SystemString:
                this.isCreable = EntityWhen.Never;
                this.isFindable = true;
                this.isViewable = false;
                this.isNavigable = EntityWhen.Never;
                this.isReadOnly = true;
                break;

            case EntityKind.System:
                this.isCreable = EntityWhen.Never;
                this.isFindable = true;
                this.isViewable = true;
                this.isNavigable = EntityWhen.Always;
                this.isReadOnly = true;
                break;

            case EntityKind.Relational:
                this.isCreable = EntityWhen.Never;
                this.isFindable = false;
                this.isViewable = false;
                this.isNavigable = EntityWhen.Never;
                this.isReadOnly = true;
                break;

            case EntityKind.String:
                this.isCreable = EntityWhen.IsSearch;
                this.isFindable = true;
                this.isViewable = false;
                this.isNavigable = EntityWhen.IsSearch;
                break;

            case EntityKind.Shared:
                this.isCreable = EntityWhen.Always;
                this.isFindable = true;
                this.isViewable = true;
                this.isNavigable = EntityWhen.Always;
                break;

            case EntityKind.Main:
                this.isCreable = EntityWhen.IsSearch;
                this.isFindable = true;
                this.isViewable = true;
                this.isNavigable = EntityWhen.Always;
                break;

            case EntityKind.Part:
                this.isCreable = EntityWhen.IsLine;
                this.isFindable = false;
                this.isViewable = true;
                this.isNavigable = EntityWhen.Always;
                break;

            case EntityKind.SharedPart:
                this.isCreable = EntityWhen.IsLine;
                this.isFindable = true;
                this.isViewable = true;
                this.isNavigable = EntityWhen.Always;
                break;

            default:
                break;

        }

        Dic.extend(this, options);
    }

    onIsCreable(isSearch: boolean): boolean {

        return this.isCreable == EntityWhen.Always ||
            this.isCreable == (isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
    }


    onIsFindable(): boolean {
        return this.isFindable;
    }

    onIsViewable(customView: boolean): boolean {
        if (!this.getComponent && !customView)
            return false;

        return this.isViewable;
    }

    onIsNavigable(customView: boolean, isSearch: boolean): boolean {

        if (!this.getComponent && !customView)
            return false;

        return this.isNavigable == EntityWhen.Always ||
            this.isNavigable == (isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
    }

    onIsReadonly(): boolean {
        return this.isReadOnly;
    }
}

export class EmbeddedEntitySettings<T extends ModifiableEntity> extends EntitySettingsBase<T> {

    isCreable: boolean;
    isViewable: boolean;
    isReadOnly: boolean;
    
    constructor(type: Type<T>, getComponent: (entity: T) => Promise<any>,
        options?: { isCreable?: boolean; isViewable?: boolean; isReadOnly?: boolean }) {
        super(type, getComponent);

        Dic.extend(this, options, { isCreable: true, isViewable: true });
    }

    onIsCreable(isSearch: boolean) {
        if (isSearch)
            throw new Error("EmbeddedEntitySettigs are not compatible with isSearch");

        return this.isCreable;
    }

    onIsFindable(): boolean {
        return false;
    }

    onIsViewable(customView: boolean): boolean {
        if (!this.getComponent && !customView)
            return false;

        return this.isViewable;
    }

    onIsNavigable(customView: boolean, isSearch: boolean): boolean {
        return false;
    }

    onIsReadonly(): boolean {
        return this.isReadOnly;
    }

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

