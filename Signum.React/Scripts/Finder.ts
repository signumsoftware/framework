import { baseUrl } from 'Framework/Signum.React/Scripts/Services';
import { QuerySettings } from 'Framework/Signum.React/Scripts/QuerySettings';
import { IEntity, Lite } from 'Framework/Signum.React/Scripts/Signum.Entities';
import { Type, IType, EntityKind } from 'Framework/Signum.React/Scripts/Reflection';
import * as Navigator from 'Framework/Signum.React/Scripts/Navigator';


//export function navigateRoute(typeOfEntity: any, id: any = null) {
//    var typeName: string;
//    if ((typeOfEntity as IEntity).Type) {
//        typeName = (typeOfEntity as IEntity).Type;
//        id = (typeOfEntity as IEntity).id;
//    }
//    else if ((typeOfEntity as Lite<IEntity>).EntityType) {
//        typeName = (typeOfEntity as Lite<IEntity>).EntityType;
//        id = (typeOfEntity as Lite<IEntity>).id;
//    }
//    else {
//        typeName = (typeOfEntity as IType).typeName;
//    }

//    return baseUrl + "/View/" + typeName + "/" + id;
//}


export var querySettings: { [type: string]: QuerySettings } = {};

export function addSettings(...settings: QuerySettings[]) {
    settings.forEach(s=> Dic.addOrThrow(querySettings, uniqueQueryName(s.queryName), s));
}

export function uniqueQueryName(queryName: any): string{
    throw "Error";
}


export function isFindable(queryName: any): boolean {
    throw new Error("not implemented");
}