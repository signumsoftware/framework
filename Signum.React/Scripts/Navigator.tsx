import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { ajaxGet, ajaxPost } from 'Framework/Signum.React/Scripts/Services';
import { IEntity, Lite, Entity, ModifiableEntity } from 'Framework/Signum.React/Scripts/Signum.Entities';
import { PseudoType, EntityKind, TypeInfo, getTypeInfo, IType, Type } from 'Framework/Signum.React/Scripts/Reflection';
import * as Finder from 'Framework/Signum.React/Scripts/Finder';


export var NotFound: __React.ComponentClass<any>;

export var currentUser: IEntity;
export var currentHistory: HistoryModule.History & HistoryModule.HistoryQueries;



export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<Route path="view/:type/:id" getComponent={asyncLoad("Southwind.React/Templates/NormalPage") } ></Route>);
    options.routes.push(<Route path="create/:type" getComponent={asyncLoad("Southwind.React/Templates/NormalPage") } ></Route>);
}

export function navigateRoute(entity: IEntity);
export function navigateRoute(lite: Lite<IEntity>);
export function navigateRoute(type: PseudoType, id: any);
export function navigateRoute(typeOfEntity: any, id: any = null) {
    var typeName: string;
    if ((typeOfEntity as IEntity).Type) {
        typeName = (typeOfEntity as IEntity).Type;
        id = (typeOfEntity as IEntity).id;
    }
    else if ((typeOfEntity as Lite<IEntity>).EntityType) {
        typeName = (typeOfEntity as Lite<IEntity>).EntityType;
        id = (typeOfEntity as Lite<IEntity>).id;
    }
    else {
        typeName = getTypeInfo(typeOfEntity as PseudoType).name;
    }

    return "/view/" + typeName[0].toLowerCase() + typeName.substr(1) + "/" + id;
}

export var entitySettings: { [type: string]: EntitySettingsBase } = {};

export function addSettings(...settings: EntitySettingsBase[]) {
    settings.forEach(s=> Dic.addOrThrow(entitySettings, s.type.typeName, s));
}


export function getSettings(type: PseudoType): EntitySettingsBase {
    var typeName = getTypeInfo(type).name;

    return entitySettings[typeName];
}


export var isCreableEvent: Array<(t: TypeInfo) => boolean> = [];

export function isCreable(type: PseudoType, isSearch?: boolean) {

    var ti = getTypeInfo(type);

    var es = entitySettings[ti.name];
    if (!es)
        return true;

    if (isSearch != null && !es.onIsCreable(isSearch))
        return false;

    return isCreableEvent.every(f=> f(ti));
}

export var isFindableEvent: Array<(t: TypeInfo) => boolean> = []; 

export function isFindable(type: PseudoType, isSearch?: boolean) {

    var ti = getTypeInfo(type)

    if (!Finder.isFindable(type))
        return false;

    var es = entitySettings[ti.name];
    if (es && !es.onIsFindable())
        return false;

    return true;
}

export var isViewableEvent: Array<(t: TypeInfo, entity?: ModifiableEntity) => boolean> = []; 

export function isViewable(typeOrEntity: PseudoType | ModifiableEntity, partialViewName: string): boolean{
    var entity = (typeOrEntity as ModifiableEntity).Type ? typeOrEntity as ModifiableEntity : null;

    var typeInfo = getTypeInfo(entity ? entity.Type : typeOrEntity as PseudoType);

    var es = entitySettings[typeInfo.name];

    return es != null && es.onIsViewable(partialViewName) && isViewableEvent.every(f=> f(typeInfo, entity));
}

export function isNavigable(typeOrEntity: PseudoType | ModifiableEntity, partialViewName: string, isSearch: boolean = false): boolean {

    var entity = (typeOrEntity as ModifiableEntity).Type ? typeOrEntity as Entity : null;

    var typeInfo = getTypeInfo(entity ? entity.Type : typeOrEntity as PseudoType);

    var es = entitySettings[typeInfo.name];

    return es != null && es.onIsNavigable(partialViewName, isSearch) && isViewableEvent.every(f=> f(typeInfo, entity));
}

export function asyncLoad(path: string | ((loc: HistoryModule.Location) => string)):
    (location: HistoryModule.Location, cb: (error: any, component?: ReactRouter.RouteComponent) => void) => void {
    return (location, callback) => {

        var finalPath = typeof path == "string" ? path as string :
            (path as ((loc: HistoryModule.Location) => string))(location);

        require([finalPath], Mod => {
            callback(null, (Mod as any)["default"]);
        });
    };
}


export interface WidgetsContext {
    entity?: Entity;
    lite?: Lite<Entity>;
}

export function renderWidgets(ctx: WidgetsContext): React.ReactFragment {
    return null;
}

export function renderEmbeddedWidgets(ctx: WidgetsContext, position: EmbeddedWidgetPosition): React.ReactFragment {
    return null;
}

export enum EmbeddedWidgetPosition {
    Top, 
    Bottom,
}

export interface ButtonsContext {
    entity?: Entity;
    lite?: Lite<Entity>;
    canExecute: { [key: string]: string }
}

export function renderButtons(ctx: ButtonsContext): React.ReactFragment {
    return null;
}

export module API {

    export function fetchEntity<T extends Entity>(lite: Lite<T>): Promise<T>;
    export function fetchEntity<T extends Entity>(type: Type<T>, id: any): Promise<T>;
    export function fetchEntity<T extends Entity>(type: PseudoType, id: any): Promise<Entity>;
    export function fetchEntity(typeOrLite: PseudoType | Lite<any>, id?: any): Promise<Entity> {

        var typeInfo = getTypeInfo((typeOrLite as Lite<any>).EntityType || typeOrLite as PseudoType);
        var id = (typeOrLite as Lite<any>).id || id;

        return ajaxGet<Entity>({ url: "/api/entity/" + typeInfo.name + "/" + id });
    }


    export function fetchEntityPack<T extends Entity>(lite: Lite<T>): Promise<EntityPack<T>>;
    export function fetchEntityPack<T extends Entity>(type: Type<T>, id: any): Promise<EntityPack<T>>;
    export function fetchEntityPack(type: PseudoType, id: any): Promise<EntityPack<Entity>>;
    export function fetchEntityPack(typeOrLite: PseudoType | Lite<any>, id?: any): Promise<EntityPack<Entity>> {

        var typeInfo = getTypeInfo((typeOrLite as Lite<any>).EntityType || typeOrLite as PseudoType);
        var id = (typeOrLite as Lite<any>).id || id;

        return ajaxGet<EntityPack<Entity>>({ url: "/api/entityPack/" + typeInfo.name + "/" + id });
    }


    export function fetchOperationInfos(type: PseudoType): Promise<Array<OperationInfo>> {

        var typeName = getTypeInfo(type).name;

        return ajaxGet<Array<OperationInfo>>({ url: "/api/operations/" + typeName });

    }

}

export interface OperationInfo {
    key: string;
    operationType: OperationType,
}

export enum OperationType {
    Execute,
    Delete,
    Constructor,
    ConstructorFrom,
    ConstructorFromMany
}

export interface EntityPack<T extends Entity> {
    entity: T
    canExecute: { [key: string]: string };
}


export abstract class EntitySettingsBase {
    public type: IType;

    public avoidPopup: boolean;

    abstract onIsCreable(isSearch: boolean): boolean;
    abstract onIsFindable(): boolean;
    abstract onIsViewable(partialViewName: string): boolean;
    abstract onIsNavigable(partialViewName: string, isSearch: boolean): boolean;
    abstract onIsReadonly(): boolean;

    abstract onPartialView(entity: ModifiableEntity): string;


    constructor(type: IType) {
        this.type = type;
    }
}

export class EntitySettings<T extends Entity> extends EntitySettingsBase {
    public type: Type<T>;

    isCreable: EntityWhen;
    isFindable: boolean;
    isViewable: boolean;
    isNavigable: EntityWhen;
    isReadOnly: boolean;

    partialViewName: (entity: T) => string;

    constructor(type: Type<T>, partialViewName: (entity: T) => string,
        options?: { isCreable?: EntityWhen, isFindable?: boolean; isViewable?: boolean; isNavigable?: EntityWhen; isReadOnly?: boolean }) {
        super(type);

        this.partialViewName = partialViewName;

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
        return hasFlag(this.isCreable, isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
    }


    onIsFindable(): boolean {
        return this.isFindable;
    }

    onIsViewable(partialViewName: string): boolean {
        if (!this.partialViewName && !partialViewName)
            return false;

        return this.isViewable;
    }

    onIsNavigable(partialViewName: string, isSearch: boolean): boolean {

        if (!this.partialViewName && !partialViewName)
            return false;

        return hasFlag(this.isNavigable, isSearch ? EntityWhen.IsSearch : EntityWhen.IsLine);
    }

    onIsReadonly(): boolean {
        return this.isReadOnly;
    }


    onPartialView(entity: ModifiableEntity): string{
        return this.partialViewName(entity as T);
    }
    
}

export class EmbeddedEntitySettings<T extends ModifiableEntity> extends EntitySettingsBase {
    public type: Type<T>;

    partialViewName: (entity: T) => string;

    isCreable: boolean;
    isViewable: boolean;
    isReadOnly: boolean;

    constructor(type: Type<T>, partialViewName: (entity: T) => string,
        options?: { isCreable?: boolean; isViewable?: boolean; isReadOnly?: boolean }) {
        super(type);

        Dic.extend(this, options);
    }

    onIsCreable(isSearch: boolean) {
        if (isSearch)
            throw new Error("EmbeddedEntitySettigs are not compatible with isSearch");

        return this.isCreable;
    }

    onIsFindable(): boolean {
        return false;
    }

    onIsViewable(partialViewName: string): boolean {
        if (!partialViewName && !partialViewName)
            return false;

        return this.isViewable;
    }

    onIsNavigable(partialViewName: string, isSearch: boolean): boolean {
        return false;
    }

    onIsReadonly(): boolean {
        return this.isReadOnly;
    }

    onPartialView(entity: ModifiableEntity): string {
        return this.partialViewName(entity as T);
    }
}


export enum EntityWhen {
    Always = 3,
    IsSearch = 2,
    IsLine = 1,
    Never = 0,
}



