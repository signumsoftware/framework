import { baseUrl } from 'Framework/Signum.React/Scripts/Services';
import { QuerySettings } from 'Framework/Signum.React/Scripts/QuerySettings';
import { FindOptions, QueryToken, FilterOperation, OrderType, ColumnOptionsMode } from 'Framework/Signum.React/Scripts/FindOptions';
import { IEntity, Lite } from 'Framework/Signum.React/Scripts/Signum.Entities';
import { Type, IType, EntityKind, QueryKey } from 'Framework/Signum.React/Scripts/Reflection';
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
    if (queryName instanceof Type)
        return (queryName as Type<any>).typeName;

    if (queryName instanceof QueryKey)
        return (queryName as QueryKey).name;

    if (typeof queryName == "string")
        return queryName as string;

    throw new Error("unexpected queryName type");
}

export function niceName(queryName: any): string {
    if (queryName instanceof Type)
        return (queryName as Type<any>).nicePluralName();

    if (queryName instanceof QueryKey)
        return (queryName as QueryKey).niceName();

    if (typeof queryName == "string")
        return queryName as string;

    throw new Error("unexpected queryName type");
}


export function isFindable(queryName: any): boolean {
    throw new Error("not implemented");
}

export function findRoute(findOptions: FindOptions): string;
export function findRoute(queryName: any): string
{
    var fo = queryName as FindOptions;
    if (!fo.queryName)
        return Navigator.currentHistory.createPath("/Find/" + uniqueQueryName(queryName)); 
    
    var base = findRoute(fo.queryName);

    var query = {
        filters: !fo.filterOptions ? null : fo.filterOptions.map(fo=> getTokenString(fo) + "," + FilterOperation[fo.operation] + "," + getValue(fo.value)).join(";"),
        orders: !fo.orderOptions ? null : fo.orderOptions.map(oo=> (oo.orderType == OrderType.Descending ? "-" : "") + getTokenString(oo)),
        columns: !fo.columnOptions ? null : fo.columnOptions.map(co=> getTokenString(co) + (co.displayName ? ("," + co.displayName) : "")),
        columnOptions: fo.columnOptionsMode == ColumnOptionsMode.Add ? null : ColumnOptionsMode[fo.columnOptionsMode],
        create: fo.create,
        navigate: fo.navigate,
        searchOnLoad: fo.searchOnLoad,
        showFilterButton: fo.showFilterButton,
        showFilters: fo.showFilters,
        showFooter: fo.showFooter,
        showHeader: fo.showHeader
    };

    return Navigator.currentHistory.createPath("/Find/" + uniqueQueryName(fo.queryName), query);
}


function getValue(value: any) {
    
}

function getTokenString(tokenContainer: { columnName: string, token: QueryToken }) {
    return tokenContainer.columnName;
}