import { baseUrl } from 'Framework/Signum.React/Scripts/Services';
import { IEntity, Lite } from 'Framework/Signum.React/Scripts/Signum.Entities';
import { Type, IType, EntityKind } from 'Framework/Signum.React/Scripts/Reflection';
import { EntitySettingsBase, EntitySettings, EmbeddedEntitySettings} from 'Framework/Signum.React/Scripts/EntitySettings';
import * as Finder from 'Framework/Signum.React/Scripts/Finder';

export function navigateRoute(entity: IEntity);
export function navigateRoute(lite: Lite<IEntity>);
export function navigateRoute(type: IType, id: any);
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
        typeName = (typeOfEntity as IType).typeName;
    }
    
    return baseUrl + "/View/" + typeName + "/" + id;
}


export var entitySettings: { [type: string]: EntitySettingsBase } = {};

export function addSettings(...settings: EntitySettingsBase[])
{
    settings.forEach(s=> Dic.addOrThrow(entitySettings, s.type.typeName, s));
}


export var isCreableEvent: Array<(t: IType) => boolean> = [];

export function isCreable(type: IType, isSearch?: boolean) {
    var es = entitySettings[type.typeName];
    if (!es)
        return true;

    if (isSearch != null && !es.onIsCreable(isSearch))
        return false;

    return isCreableEvent.every(f=> f(type));
}

export var isFindableEvent: Array<(t: IType) => boolean> = []; 

export function isFindable(type: IType, isSearch?: boolean) {
    if (!Finder.isFindable(type))
        return false;

    var es = entitySettings[type.typeName];
    if (es && !es.onIsFindable())
        return false;

    return true;
}

export var isViewableEvent: Array<(t: IType | IEntity) => boolean> = []; 

export function isViewable(typeOrEntity: IType | IEntity, partialViewName: string): boolean{
    var typeName = (typeOrEntity as IType).typeName || (typeOrEntity as IEntity).Type;

    var es = entitySettings[typeName];

    return es != null && es.onIsViewable(partialViewName)
    isViewableEvent.every(f=> f(typeOrEntity));
}

export function isNavigable(typeOrEntity: IType | IEntity, partialViewName: string, isSearch: boolean = false): boolean {
    var typeName = (typeOrEntity as IType).typeName || (typeOrEntity as IEntity).Type;

    var es = entitySettings[typeName];

    return es != null && es.onIsNavigable(partialViewName, isSearch) &&
        isViewableEvent.every(f=> f(typeOrEntity));
}

