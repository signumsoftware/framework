import * as React from "react"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { baseUrl, ajaxGet, ajaxPost } from 'Framework/Signum.React/Scripts/Services';
import { QuerySettings } from 'Framework/Signum.React/Scripts/QuerySettings';
import { FindOptions, FilterOptions, OrderOption, ColumnOptions, QueryToken, FilterOperation, OrderType, ColumnOptionsMode, QueryDescription, ColumnDescription, FilterType } from 'Framework/Signum.React/Scripts/FindOptions';
import { IEntity, Lite, toLite, liteKey, parseLite } from 'Framework/Signum.React/Scripts/Signum.Entities';
import { Type, IType, EntityKind, QueryKey, queryNiceName, queryKey, TypeReference } from 'Framework/Signum.React/Scripts/Reflection';
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


export var querySettings: { [queryKey: string]: QuerySettings } = {};

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<Route path="find">
        <Route path=":queryName" getComponent={Navigator.asyncLoad("Southwind.React/Templates/SearchControl/SearchPage") } />
        </Route>);
}

export function addSettings(...settings: QuerySettings[]) {
    settings.forEach(s=> Dic.addOrThrow(querySettings, queryKey(s.queryName), s));
}

export function getQuerySettings(queryName: any): QuerySettings {
    return querySettings[queryKey(queryName)];
}


var queryDescriptionCache: { [queryKey: string]: QueryDescription } = {};

export function getQueryDescription(queryName: any): Promise<QueryDescription>{

    var key = queryKey(queryName);

    if (queryDescriptionCache[key])
        return Promise.resolve(queryDescriptionCache[key]);

    return ajaxGet<QueryDescription>({ url: "api/query/description/" + queryKey(queryName) })
        .then(qd => {
            queryDescriptionCache[key] = qd;
            return qd;
        });
}



export function isFindable(queryName: any): boolean {
    throw new Error("not implemented");
}

export function findOptionsPath(findOptions: FindOptions): string;
export function findOptionsPath(queryName: any): string
{
    var fo = queryName as FindOptions;
    if (!fo.queryName)
        return Navigator.currentHistory.createPath("/Find/" + queryKey(queryName)); 
    
    var base = findOptionsPath(fo.queryName);

    var query = {
        filters: Encoder.encodeFilters(fo.filterOptions),
        orders: Encoder.encodeOrders(fo.orderOptions),
        columns: Encoder.encodeColumns(fo.columnOptions),
        columnOptions: !fo.columnOptionsMode || fo.columnOptionsMode == ColumnOptionsMode.Add ? null : ColumnOptionsMode[fo.columnOptionsMode],
        create: fo.create,
        navigate: fo.navigate,
        searchOnLoad: fo.searchOnLoad,
        showFilterButton: fo.showFilterButton,
        showFilters: fo.showFilters,
        showFooter: fo.showFooter,
        showHeader: fo.showHeader
    };

    return Navigator.currentHistory.createPath("/Find/" + queryKey(fo.queryName), query);
}

export function parseFindOptionsPath(queryName: string, query: any): Promise<FindOptions> {

    var result = {
        queryName: queryName,
        filterOptions: Decoder.decodeFilters(query.filters),
        orderOptions: Decoder.decodeOrders(query.orders),
        columnOptions: Decoder.decodeColumns(query.columns),
        columnOptionsMode: query.columnOptions || ColumnOptionsMode.Add,
        create: parseBoolean(query.create),
        navigate: parseBoolean(query.navigate),
        searchOnLoad: parseBoolean(query.searchOnLoad),
        showFilterButton: parseBoolean(query.showFilterButton),
        showFilters: parseBoolean(query.showFilters),
        showFooter: parseBoolean(query.showFooter),
        showHeader: parseBoolean(query.showHeader)
    };

    return parseTokens(result);
}




export function parseTokens(findOptions: FindOptions): Promise<FindOptions> {

    var completer = new TokenCompleter();

    if (findOptions.filterOptions)
        findOptions.filterOptions.forEach(fo=> completer.complete(fo).then(_=> parseValue(fo)));

    if (findOptions.orderOptions)
        findOptions.orderOptions.forEach(fo=> completer.complete(fo));

    if (findOptions.columnOptions)
        findOptions.columnOptions.forEach(fo=> completer.complete(fo));

    return completer.finish().then(a=> findOptions);
}

function parseValue(fo: FilterOptions) {
    switch (filterType(fo.token)) {
        case FilterType.Boolean: fo.value = parseBoolean(fo.value);
        case FilterType.Integer: fo.value = parseInt(fo.value) || null;
        case FilterType.Decimal: fo.value = parseFloat(fo.value) || null;
        case FilterType.Lite:
            {
                if (typeof fo.value == "string")
                    fo.value = parseLite(fo.value);
            }
    }
}

function filterType(queryToken: QueryToken) {
    if ((queryToken as any).filterType)
        return (queryToken as any).filterType;

    else (queryToken as any).filterType = calculateFilterType(queryToken.type);
}


function calculateFilterType(typeRef: TypeReference): FilterType {
    
    if (typeRef.name == "boolean")
        return FilterType.Boolean;

    return FilterType.Boolean;
}



class TokenCompleter
{
    queryName: any;

    tokensToRequest: { [fullKey: string]: ({ promise: Promise<QueryToken>, resolve: (action: QueryToken) => void }) };


    complete(tokenContainer: { columnName: string, token?: QueryToken }) : Promise<void> {
        if (tokenContainer.token)
            return;

        return this.request(tokenContainer.columnName)
            .then(token => { tokenContainer.token = token; });
    }

    simpleQueryToken(queryColumn: ColumnDescription): QueryToken{

        return {
            type: queryColumn.type,
            format: queryColumn.format,
            niceName: queryColumn.displayName,
            fullKey: queryColumn.name,
            key: queryColumn.name,
            unit: queryColumn.unit,
            toString: queryColumn.displayName,
            filterType: queryColumn.filterType,
        };
    }

    request(fullKey: string): Promise<QueryToken> {

        if (!fullKey.contains("."))
            return getQueryDescription(this.queryName).then(qd=> this.simpleQueryToken(qd.columns[fullKey]));
        
        var bucket = this.tokensToRequest[fullKey];

        if (bucket)
            return bucket.promise;

        bucket = { promise: null, resolve: null };

        bucket.promise = new Promise<QueryToken>((resolve, reject) => {
            bucket.resolve = resolve;
        });

        return bucket.promise;
    }


    finish(): Promise<void> {
        var request = {
            queryKey: queryKey(this.queryName),
            tokens: Dic.getKeys(this.tokensToRequest)
        }; 
        
        return ajaxPost<QueryToken[]>({ url: "api/query/parseTokens" }, request).then(tokens=> {
            tokens.forEach(t=> this.tokensToRequest[t.fullKey].resolve(t));
        });
    }
}


function parseBoolean(value: any): boolean
{
    if (value === "true" || value === true)
        return true;


    if (value === "false" || value === false)
        return false;

    return null;
}

module Encoder {

    export function encodeFilters(filterOptions: FilterOptions[]) {
        return !filterOptions ? null : filterOptions.map(fo=> getTokenString(fo) + "," + FilterOperation[fo.operation] + "," + stringValue(fo.value));
    }

    export function encodeOrders(orderOptions: OrderOption[]) {
        return !orderOptions ? null : orderOptions.map(oo=> (oo.orderType == OrderType.Descending ? "-" : "") + getTokenString(oo));
    }

    export function encodeColumns(columnOptions: ColumnOptions[]) {
        return !columnOptions ? null : columnOptions.map(co=> getTokenString(co) + (co.displayName ? ("," + co.displayName) : ""));
    }

    export function stringValue(value: any): string {

        if (!value)
            return value;

        if (value.Type)
            value = toLite(value as IEntity);

        if (value.EntityType)
            return liteKey(value as Lite<IEntity>);

        return value.toString();
    }
}

module Decoder {

    export function asArray(queryPosition: string | string[]) {

        if (typeof queryPosition == "string")
            return [queryPosition as string];

        return queryPosition as string[]
    }


    export function decodeFilters(filters: string | string[]): FilterOptions[] {

        if (!filters)
            return null;
        
        return asArray(filters).map(val=> val.split(","))
            .map(vals=> ({
                columnName: vals[0],
                operation: vals[1] as any,
                value: vals[2]
            }) as FilterOptions);
    }

    export function decodeOrders(orders: string | string[]): OrderOption[] {
        
        if (!orders)
            return null;

        return asArray(orders).map(val=> ({
            orderType: val[0] == "-" ? OrderType.Descending : OrderType.Ascending,
            columnName: val.tryAfter("-") || val
        }));
    }

    export function decodeColumns(columns: string | string[]): ColumnOptions[]{

        if (!columns)
            return null;

        return asArray(columns).map(val=> ({
            columnName: val[0].tryBefore(",") || val[0],
            displayName: val[0].tryAfter(",")
        }) as ColumnOptions);
    }

    export function getEntity(value: any): string {

        if (!value)
            return value;

        if (value.Type)
            value = toLite(value as IEntity);

        if (value.EntityType)
            return liteKey(value as Lite<IEntity>);

        return value.toString();
    }
}

function getTokenString(tokenContainer: { columnName: string, token?: QueryToken }) {
    return tokenContainer.token ? tokenContainer.token.fullKey : tokenContainer.columnName;
}




