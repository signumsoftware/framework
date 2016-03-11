import * as React from "react"
import * as moment from "moment"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { Dic } from './Globals'
import { ajaxGet, ajaxPost } from './Services';

import { QueryDescription, CountQueryRequest, QueryRequest, FindOptions, FilterOption, FilterType, FilterOperation,
    QueryToken, ColumnDescription, ColumnOptionsMode, ColumnOption, Pagination, PaginationMode, ResultColumn,
    ResultTable, ResultRow, OrderOption, OrderType, SubTokensOptions, toQueryToken, isList, expandParentColumn } from './FindOptions';

import { Entity, IEntity, Lite, toLite, liteKey, parseLite, EntityControlMessage  } from './Signum.Entities';

import { Type, IType, EntityKind, QueryKey, getQueryNiceName, getQueryKey, TypeReference,
    getTypeInfo, getTypeInfos, getEnumInfo, toMomentFormat, PseudoType } from './Reflection';

import {navigateRoute, isNavigable, currentHistory, API as NavAPI } from './Navigator';
import SearchModal from './SearchControl/SearchModal';
import EntityLink from './SearchControl/EntityLink';
import SearchControl from './SearchControl/SearchControl';


export const querySettings: { [queryKey: string]: QuerySettings } = {};

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<Route path="find">
        <Route path=":queryName" getComponent={ (loc, cb) => require(["./SearchControl/SearchPage"], (Comp) => cb(null, Comp.default)) } />
        </Route>);
}

export function addSettings(...settings: QuerySettings[]) {
    settings.forEach(s=> Dic.addOrThrow(querySettings, getQueryKey(s.queryName), s));
}

export function getQuerySettings(queryName: PseudoType | QueryKey): QuerySettings {
    return querySettings[getQueryKey(queryName)];
}

export const isFindableEvent: Array<(queryKey: string) => boolean> = [];

export function isFindable(queryName: any): boolean {

    const queryKey = getQueryKey(queryName);

    return isFindableEvent.every(f=> f(queryKey));
}

export function find<T extends Entity>(type: Type<T>): Promise<Lite<T>>;
export function find(findOptions: FindOptions): Promise<Lite<IEntity>>;
export function find(findOptions: FindOptions | Type<any> ): Promise<Lite<IEntity>> {

    const fo = (findOptions as FindOptions).queryName ? findOptions as FindOptions :
        { queryName: findOptions as Type<any> } as FindOptions;
    
    return new Promise<Lite<IEntity>>((resolve, reject) => {
        require(["./SearchControl/SearchModal"], function (SP: { default: typeof SearchModal }) {
            SP.default.open(fo).then(resolve, reject);
        });
    });
}

export function findMany<T extends Entity>(type: Type<T>): Promise<Lite<T>[]>;
export function findMany(findOptions: FindOptions): Promise<Lite<IEntity>[]>;
export function findMany(findOptions: FindOptions | Type<any>): Promise<Lite<IEntity>[]> {

    const fo = (findOptions as FindOptions).queryName ? findOptions as FindOptions :
        { queryName: findOptions as Type<any> } as FindOptions;

    return new Promise<Lite<IEntity>[]>((resolve, reject) => {
        require(["./SearchControl/SearchModal"], function (SP: { default: typeof SearchModal }) {
            SP.default.openMany(fo).then(resolve, reject);
        });
    });
}

export function exploreWindowsOpen(findOptions: FindOptions, e: React.MouseEvent) {
    if (e.ctrlKey || e.button == 2)
        window.open(findOptionsPath(findOptions));
    else
        explore(findOptions).done();
}

export function explore(findOptions: FindOptions): Promise<void> {

    return new Promise<void>((resolve, reject) => {
        require(["./SearchControl/SearchModal"], function (SP: { default: typeof SearchModal }) {
            SP.default.explore(findOptions).then(resolve, reject);
        });
    });
}

export function findOptionsPath(findOptions: FindOptions): string;
export function findOptionsPath(queryName: PseudoType | QueryKey): string;
export function findOptionsPath(queryNameOrFindOptions: any): string {
    let fo = queryNameOrFindOptions as FindOptions;
    if (!fo.queryName)
        return currentHistory.createPath("/find/" + getQueryKey(queryNameOrFindOptions)); 

    fo = expandParentColumn(fo);
    
    const query = {
        filters: Encoder.encodeFilters(fo.filterOptions),
        orders: Encoder.encodeOrders(fo.orderOptions),
        columns: Encoder.encodeColumns(fo.columnOptions),
        columnMode: !fo.columnOptionsMode || fo.columnOptionsMode == ColumnOptionsMode.Add ? undefined : ColumnOptionsMode[fo.columnOptionsMode],
        create: fo.create,
        navigate: fo.navigate,
        searchOnLoad: fo.searchOnLoad,
        showFilterButton: fo.showFilterButton,
        showFilters: fo.showFilters,
        showFooter: fo.showFooter,
        showHeader: fo.showHeader,
        allowChangeColumns: fo.allowChangeColumns,
    };

    return currentHistory.createPath({ pathname: "/find/" + getQueryKey(fo.queryName), query: query });
}

export function parseFindOptionsPath(queryName: PseudoType | QueryKey, query: any): FindOptions {
    
    const result = {
        queryName: queryName,
        filterOptions: Decoder.decodeFilters(query.filters),
        orderOptions: Decoder.decodeOrders(query.orders),
        columnOptions: Decoder.decodeColumns(query.columns),
        columnOptionsMode: query.columnMode == null ? ColumnOptionsMode.Add : query.columnMode,
        create: parseBoolean(query.create),
        navigate: parseBoolean(query.navigate),
        searchOnLoad: parseBoolean(query.searchOnLoad),
        showFilterButton: parseBoolean(query.showFilterButton),
        showFilters: parseBoolean(query.showFilters),
        showFooter: parseBoolean(query.showFooter),
        showHeader: parseBoolean(query.showHeader),
    };

    return result;
}

export function mergeColumns(columns: ColumnDescription[], mode: ColumnOptionsMode, columnOptions: ColumnOption[]): ColumnOption[] {

    switch (mode) {
        case ColumnOptionsMode.Add:
            return columns.filter(cd => cd.name != "Entity").map(cd => ({ columnName: cd.name, token: toQueryToken(cd), displayName: cd.displayName }) as ColumnOption)
                .concat(columnOptions);

        case ColumnOptionsMode.Remove:
            return columns.filter(cd => cd.name != "Entity" && !columnOptions.some(a => (a.token ? a.token.fullKey : a.columnName) == cd.name))
                .map(cd => ({ columnName: cd.name, token: toQueryToken(cd), displayName: cd.displayName }) as ColumnOption);

        case ColumnOptionsMode.Replace:
            return columnOptions;
    }
}

export function smartColumns(current: ColumnOption[], ideal: ColumnDescription[]): { mode: ColumnOptionsMode; columns: ColumnOption[] } {
    
    var similar = (a: ColumnOption, b: ColumnDescription) =>
        a.token.fullKey == b.name && (a.displayName == b.displayName || a.displayName == null);

    current = current.map(co => ({
        token: co.token,
        columnName: co.columnName,
        displayName: co.displayName == co.token.niceName ? null : co.displayName
    } as ColumnOption));

    ideal = ideal.filter(a => a.name != "Entity");

    if (current.length < ideal.length) {
        var toRemove: ColumnOption[] = [];

        var j = 0;
        for (var i = 0; i < ideal.length; i++) {
            if (j < current.length && similar(current[j], ideal[i]))
                j++;
            else
                toRemove.push({ token: null, columnName: ideal[i].name, displayName: null });
        }
        if (toRemove.length + current.length == ideal.length) {
            return {
                mode: ColumnOptionsMode.Remove,
                columns: toRemove
            };
        }
    }
    else if (current.every((a, i) => i >= ideal.length || similar(a, ideal[i]))) {
        return {
            mode: ColumnOptionsMode.Add,
            columns: current.slice(ideal.length)
        };
    }
    
    return {
        mode: ColumnOptionsMode.Replace,
        columns: current,
    };
}

function parseBoolean(value: any): boolean {
    if (value === "true" || value === true)
        return true;

    if (value === "false" || value === false)
        return false;

    return undefined;
}

export function parseTokens(findOptions: FindOptions): Promise<FindOptions> {

    const completer = new TokenCompleter(findOptions.queryName);

    var promises: Promise<any>[] = [];

    if (findOptions.filterOptions)
        promises.push(...findOptions.filterOptions.map(fo => completer.complete(fo, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll)));

    if (findOptions.orderOptions)
        promises.push(...findOptions.orderOptions.map(fo => completer.complete(fo, SubTokensOptions.CanElement)));

    if (findOptions.columnOptions)
        promises.push(...findOptions.columnOptions.map(fo => completer.complete(fo, SubTokensOptions.CanElement)));

    completer.finished();

    return Promise.all(promises)
        .then(() => parseFilterValues(findOptions.filterOptions).then(() => findOptions));
}

export function parseSingleToken(queryName: PseudoType | QueryKey, token: string, subTokenOptions: SubTokensOptions): Promise<QueryToken> {

    const completer = new TokenCompleter(queryName);
    const result = completer.request(token, subTokenOptions);
    completer.finished();

    return result;
}

export class TokenCompleter {
    constructor(public queryName: PseudoType | QueryKey) { }

    tokensToRequest: {
        [fullKey: string]: (
            {
                options: SubTokensOptions, promise: Promise<QueryToken>,
                resolve: (action: QueryToken) => void
            })
    } = {};


    complete(tokenContainer: { columnName: string, token?: QueryToken }, options: SubTokensOptions): Promise<void> {
        if (tokenContainer.token)
            return;

        return this.request(tokenContainer.columnName, options)
            .then(token => { tokenContainer.token = token; });
    }


    request(fullKey: string, options: SubTokensOptions): Promise<QueryToken> {

        if (fullKey == null)
            return Promise.resolve(null);

        if (!fullKey.contains("."))
            return getQueryDescription(this.queryName).then(qd=> toQueryToken(qd.columns[fullKey]));

        var bucket = this.tokensToRequest[fullKey];

        if (bucket)
            return bucket.promise;

        this.tokensToRequest[fullKey] = bucket = { promise: null, resolve: null, options: options };

        bucket.promise = new Promise<QueryToken>((resolve, reject) => {
            bucket.resolve = resolve;
        });

        return bucket.promise;
    }


    finished(): Promise<void> {

        const queryKey = getQueryKey(this.queryName);
        const tokens = Dic.map(this.tokensToRequest, (token, val) => ({ token: token, options: val.options }));
        
        if (tokens.length == 0)
            return Promise.resolve(null);
        
        return API.parseTokens(queryKey, tokens).then(parsedTokens=> {
            parsedTokens.forEach(t=> this.tokensToRequest[t.fullKey].resolve(t));
        });
    }
}



export function parseFilterValues(filterOptions: FilterOption[]): Promise<void> {

    var needToStr: Lite<any>[] = [];
    filterOptions.forEach(fo => {
        if (isList(fo.operation)) {
            if (!Array.isArray(fo.value))
                fo.value = [fo.value];

            fo.value = (fo.value as any[]).map(v => parseValue(fo.token, v, needToStr));
        }

        else {
            if (Array.isArray(fo.value))
                throw new Error("Unespected array for operation " + fo.operation);

            fo.value = parseValue(fo.token, fo.value, needToStr);
        }
    });

    if (needToStr.length == 0)
        return Promise.resolve(null);

    return NavAPI.fillToStrings(needToStr)
}


function parseValue(token: QueryToken, val: any, needToStr: Array<any>): any {
    switch (filterType(token)) {
        case FilterType.Boolean: return parseBoolean(val);
        case FilterType.Integer: return parseInt(val) || null;
        case FilterType.Decimal: return parseFloat(val) || null;
        case FilterType.Lite:
            {
                var lite = convertToLite(val);

                if (lite && !lite.toStr)
                    needToStr.push(lite);

                return lite;
            }
    }

    return val;
}

function convertToLite(val: any): Lite<Entity> {
    if (val == null || val == "")
        return null; 

    if ((val as Lite<Entity>).EntityType) {
        let lite = val as Lite<Entity>;
        if (lite.entity != null && lite.entity.id != null)
            return toLite(lite.entity, false);

        return lite;
    }

    if ((val as Entity).Type)
        return toLite(val as Entity);

    if (typeof val == "string")
        return parseLite(val);

    throw new Error(`Impossible to convert ${val} to Lite`); 
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

    const queryDescriptionCache: { [queryKey: string]: QueryDescription } = {};
    export function getQueryDescription(queryName: PseudoType | QueryKey): Promise<QueryDescription> {
    const queryKey = getQueryKey(queryName);

    if (queryDescriptionCache[queryKey])
        return Promise.resolve(queryDescriptionCache[queryKey]);

    return API.fetchQueryDescription(queryKey).then(qd => {
                Object.freeze(qd.columns);
        queryDescriptionCache[queryKey] = Object.freeze(qd);
                return qd;
            });
    }

export module API {
    
    export function fetchQueryDescription(queryKey: string): Promise<QueryDescription> {
        return ajaxGet<QueryDescription>({ url: "/api/query/description/" + queryKey });
    }

    export function search(request: QueryRequest): Promise<ResultTable> {
        return ajaxPost<ResultTable>({ url: "/api/query/search" }, request);
    }

    export function queryCount(request: CountQueryRequest): Promise<number> {
        return ajaxPost<number>({ url: "/api/query/queryCount" }, request);
    }

    export function findLiteLike(request: { types: string, subString: string, count: number }): Promise<Lite<IEntity>[]> {
        return ajaxGet<Lite<IEntity>[]>({
            url: currentHistory.createHref({ pathname: "api/query/findLiteLike", query: request })
        });
    }

    export function findAllLites(request: { types: string }): Promise<Lite<IEntity>[]> {
        return ajaxGet<Lite<IEntity>[]>({
            url: currentHistory.createHref({ pathname: "api/query/findAllLites", query: request })
        });
    }

    export function findAllEntities(request: { types: string }): Promise<IEntity[]> {
        return ajaxGet<Lite<IEntity>[]>({
            url: currentHistory.createHref({ pathname: "api/query/findAllEntities", query: request })
        });
    }

    export function parseTokens(queryKey: string, tokens: { token: string, options: SubTokensOptions }[]): Promise<QueryToken[]> {
        return ajaxPost<QueryToken[]>({ url: "/api/query/parseTokens" }, { queryKey, tokens });
    }

    export function subTokens(queryKey: string, token: QueryToken, options: SubTokensOptions): Promise<QueryToken[]>{
        return ajaxPost<QueryToken[]>({ url: "/api/query/subTokens" }, { queryKey, token: token == null ? null:  token.fullKey, options }).then(list=> {

            if (token == null) {
                var entity = list.filter(a => a.key == "Entity").singleOrNull();

                list.filter(a => a.fullKey.startsWith("Entity.")).forEach(t => t.parent = entity);
            } else {
            list.forEach(t=> t.parent = token);
            }
            return list;
        });
    }
}



export module Encoder {

    export function encodeFilters(filterOptions: FilterOption[]): string[] {
        return !filterOptions ? undefined : filterOptions.map(fo => getTokenString(fo) + "~" + FilterOperation[fo.operation] + "~" + stringValue(fo.value));
    }

    export function encodeOrders(orderOptions: OrderOption[]): string {
        return !orderOptions ? undefined : orderOptions.map(oo => (oo.orderType == OrderType.Descending ? "-" : "") + getTokenString(oo)).join("~");
    }

    export function encodeColumns(columnOptions: ColumnOption[]): string[] {
        return !columnOptions ? undefined : columnOptions.map(co => getTokenString(co) + (co.displayName ? ("~" +  scapeTilde(co.displayName)) : ""));
    }

    export function stringValue(value: any): string {

        if (!value)
            return value;

        if (Array.isArray(value))
            return (value as any[]).map(a => stringValue(a)).join("~");

        if (value.Type)
            value = toLite(value as IEntity);

        if (value.EntityType)
            return liteKey(value as Lite<IEntity>);
        
        return scapeTilde(value.toString());
    }

    export function scapeTilde(str: string) {
        if (str == null)
            return null;

        return str.replace("~", "#|#");
    }

    export function getTokenString(tokenContainer: { columnName: string, token?: QueryToken }) {
        return tokenContainer.token ? tokenContainer.token.fullKey : tokenContainer.columnName;
    }
}

export module Decoder {

    export function asArray(queryPosition: string | string[]) {

        if (typeof queryPosition == "string")
            return [queryPosition as string];

        return queryPosition as string[]
    }


    export function decodeFilters(filters: string | string[]): FilterOption[] {

        if (!filters)
            return undefined;
        
        return asArray(filters).map(val => {
            var parts = val.split("~");

            return {
                columnName: parts[0],
                operation: parts[1] as any as FilterOperation,
                value: parts.length == 3 ? unscapeTildes(parts[2]) :
                    parts.slice(2).map(a => unscapeTildes(a))
            } as FilterOption;
        });
    }

    export function unscapeTildes(str: string) {
        if (!str)
            return str;

        return str.replace("#|#", "~");
    }

    export function decodeOrders(orders: string): OrderOption[] {
        
        if (!orders)
            return undefined;

        return orders.split("~").map(val => ({
            orderType: val[0] == "-" ? OrderType.Descending : OrderType.Ascending,
            columnName: val.tryAfter("-") || val
        }));
    }

    export function decodeColumns(columns: string | string[]): ColumnOption[]{

        if (!columns)
            return undefined;

        return asArray(columns).map(val=> ({
            columnName: val.tryBefore("~") || val,
            displayName: unscapeTildes(val.tryAfter("~"))
        }) as ColumnOption);
    }
}


export module ButtonBarQuery {

    interface ButtonBarQueryContext {
        searchControl: SearchControl;
        findOptions: FindOptions;
    }

    export var onButtonBarElements: ((ctx: ButtonBarQueryContext) => React.ReactElement<any>)[] = [];

    export function getButtonBarElements(ctx: ButtonBarQueryContext): React.ReactElement<any>[] {
        return onButtonBarElements.map(f => f(ctx)).filter(a => a != null);
    }
}


export const defaultPagination: Pagination = {
    mode: PaginationMode.Paginate,
    elementsPerPage: 20,
    currentPage: 1,
};


export const defaultOrderColumn: string = "Id";

export interface QuerySettings {
    queryName: PseudoType | QueryKey;
    pagination?: Pagination;
    defaultOrderColumn?: string;
    formatters?: { [columnName: string]: CellFormatter };
    rowAttributes?: (row: ResultRow, columns: string[]) => React.HTMLAttributes;
    entityFormatter?: EntityFormatter;
    simpleFilterBuilder?: (qd: QueryDescription, initialFindOptions: FindOptions) => React.ReactElement<any>;
}

export interface FormatRule {
    name: string;
    formatter: (column: ColumnOption) => CellFormatter;
    isApplicable: (column: ColumnOption) => boolean;
}

export class CellFormatter {
    constructor(
        public formatter: (cell: any) => React.ReactChild,
        public textAllign = "left") {
    }
}


export const formatRules: FormatRule[] = [
    {
        name: "Object",
        isApplicable: col=> true,
        formatter: col=> new CellFormatter(cell => cell ? (cell.toStr || cell.toString()) : null)
    },
    {
        name: "Enum",
        isApplicable: col=> col.token.filterType == FilterType.Enum,
        formatter: col=> new CellFormatter(cell => getEnumInfo(col.token.type.name, cell).niceName)
    },
    {
        name: "Lite",
        isApplicable: col => col.token.filterType == FilterType.Lite,
        formatter: col => new CellFormatter((cell: Lite<IEntity>) => !cell ? null : <EntityLink lite={cell}/>)
    },

    {
        name: "Guid",
        isApplicable: col=> col.token.filterType == FilterType.Guid,
        formatter: col => new CellFormatter((cell: string) => cell && <span className="guid">{(cell.substr(0, 4) + "…" + cell.substring(cell.length - 4)) }</span>)
    },
    {
        name: "DateTime",
        isApplicable: col=> col.token.filterType == FilterType.DateTime,
        formatter: col=> {
            const momentFormat = toMomentFormat(col.token.format);
            return new CellFormatter((cell: string) => cell == null || cell == "" ? "" : moment(cell).format(momentFormat))
        }
    },
    {
        name: "Number",
        isApplicable: col=> col.token.filterType == FilterType.Integer || col.token.filterType == FilterType.Decimal,
        formatter: col=> new CellFormatter((cell: number) => cell && cell.toString(), "right")
    },
    {
        name: "Number with Unit",
        isApplicable: col=> (col.token.filterType == FilterType.Integer || col.token.filterType == FilterType.Decimal) && !!col.token.unit,
        formatter: col => new CellFormatter((cell: number) => cell && cell.toString() + " " + col.token.unit, "right")
    },
    {
        name: "Bool",
        isApplicable: col=> col.token.filterType == FilterType.Boolean,
        formatter: col=> new CellFormatter((cell: boolean) => cell == null ? null : <input type="checkbox" disabled={true} checked={cell}/>, "center")
    },
];

export interface EntityFormatRule {
    name: string;
    formatter: EntityFormatter;
    isApplicable: (row: ResultRow) => boolean;
}


export type EntityFormatter = (row: ResultRow) => React.ReactChild;

export const entityFormatRules: EntityFormatRule[] = [
    {
        name: "View",
        isApplicable: row=> true,
        formatter: row => !row.entity || !isNavigable(row.entity.EntityType, null, true) ? null :
            <EntityLink lite={row.entity} inSearch={true}>{EntityControlMessage.View.niceToString() }</EntityLink>
    },
];
