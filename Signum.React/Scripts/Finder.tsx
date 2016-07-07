import * as React from "react"
import * as moment from "moment"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { Dic } from './Globals'
import { ajaxGet, ajaxPost } from './Services';

import { QueryDescription, CountQueryRequest, QueryRequest, FindOptions, FilterOption,
    QueryToken, ColumnDescription, ColumnOption, Pagination, ResultColumn,
    ResultTable, ResultRow, OrderOption, SubTokensOptions, toQueryToken, isList, expandParentColumn, ColumnOptionsMode } from './FindOptions';

import { PaginationMode, OrderType, FilterOperation, FilterType, UniqueType } from './Signum.Entities.DynamicQuery';

import { Entity, Lite, toLite, liteKey, parseLite, EntityControlMessage, isLite, isEntityPack, isEntity } from './Signum.Entities';
import { TypeEntity, QueryEntity } from './Signum.Entities.Basics';

import { Type, IType, EntityKind, QueryKey, getQueryNiceName, getQueryKey, isQueryDefined, TypeReference,
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

export function isFindable(queryName: PseudoType | QueryKey): boolean {

    if (!isQueryDefined(queryName))
        return false;

    const queryKey = getQueryKey(queryName);

    return isFindableEvent.every(f=> f(queryKey));
}

export function find(findOptions: FindOptions): Promise<Lite<Entity>>;
export function find<T extends Entity>(type: Type<T>): Promise<Lite<T>>;
export function find(obj: FindOptions | Type<any>): Promise<Lite<Entity>> {

    const fo = (obj as FindOptions).queryName ? obj as FindOptions :
        { queryName: obj as Type<any> } as FindOptions;
    
    return new Promise<Lite<Entity>>((resolve, reject) => {
        require(["./SearchControl/SearchModal"], function (SP: { default: typeof SearchModal }) {
            SP.default.open(fo).then(resolve, reject);
        });
    });
}

export function findMany(findOptions: FindOptions): Promise<Lite<Entity>[]>;
export function findMany<T extends Entity>(type: Type<T>): Promise<Lite<T>[]>;
export function findMany(findOptions: FindOptions | Type<any>): Promise<Lite<Entity>[]> {

    const fo = (findOptions as FindOptions).queryName ? findOptions as FindOptions :
        { queryName: findOptions as Type<any> } as FindOptions;

    return new Promise<Lite<Entity>[]>((resolve, reject) => {
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

export function findOptionsPath(fo: FindOptions, extra?: any): string {

    fo = expandParentColumn(fo);
    
    const query = {
        columnMode: !fo.columnOptionsMode || fo.columnOptionsMode == "Add" as ColumnOptionsMode ? undefined : fo.columnOptionsMode,
        create: fo.create,
        navigate: fo.navigate,
        searchOnLoad: fo.searchOnLoad,
        showFilterButton: fo.showFilterButton,
        showFilters: fo.showFilters,
        showFooter: fo.showFooter,
        showHeader: fo.showHeader,
        allowChangeColumns: fo.allowChangeColumns,
    };
    
    Encoder.encodeFilters(query, fo.filterOptions);
    Encoder.encodeOrders(query, fo.orderOptions);
    Encoder.encodeColumns(query, fo.columnOptions);

    Dic.extend(query, extra);

    return currentHistory.createPath({ pathname: "~/find/" + getQueryKey(fo.queryName), query: query });
}



export function parseFindOptionsPath(queryName: PseudoType | QueryKey, query: any): FindOptions {
    
    const result = {
        queryName: queryName,
        filterOptions: Decoder.decodeFilters(query),
        orderOptions: Decoder.decodeOrders(query),
        columnOptions: Decoder.decodeColumns(query),
        columnOptionsMode: query.columnMode == null ? "Add" : query.columnMode,
        create: parseBoolean(query.create),
        navigate: parseBoolean(query.navigate),
        searchOnLoad: parseBoolean(query.searchOnLoad),
        showFilterButton: parseBoolean(query.showFilterButton),
        showFilters: parseBoolean(query.showFilters),
        showFooter: parseBoolean(query.showFooter),
        showHeader: parseBoolean(query.showHeader),
    } as FindOptions;

    return result;
}

export function mergeColumns(columns: ColumnDescription[], mode: ColumnOptionsMode, columnOptions: ColumnOption[]): ColumnOption[] {

    switch (mode) {
        case "Add":
            return columns.filter(cd => cd.name != "Entity").map(cd => ({ columnName: cd.name, token: toQueryToken(cd), displayName: cd.displayName }) as ColumnOption)
                .concat(columnOptions);

        case "Remove":
            return columns.filter(cd => cd.name != "Entity" && !columnOptions.some(a => (a.token ? a.token.fullKey : a.columnName) == cd.name))
                .map(cd => ({ columnName: cd.name, token: toQueryToken(cd), displayName: cd.displayName }) as ColumnOption);

        case "Replace":
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
                mode: "Remove",
                columns: toRemove
            };
        }
    }
    else if (current.every((a, i) => i >= ideal.length || similar(a, ideal[i]))) {
        return {
            mode: "Add",
            columns: current.slice(ideal.length)
        };
    }
    
    return {
        mode: "Replace",
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

    if (findOptions.filterOptions) {
        findOptions.filterOptions.filter(fo => !fo.operation).forEach(fo => fo.operation = "EqualTo");
        promises.push(...findOptions.filterOptions.map(fo => completer.complete(fo, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll)));
    }

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

        if (!fullKey.contains(".") && fullKey != "Count"){
            return getQueryDescription(this.queryName).then(qd=> {
                
                var colDesc = qd.columns[fullKey];  
                
                if(colDesc == null)
                    throw new Error(`Column '${fullKey}' not found in '${getQueryKey(this.queryName)}'`);

                return toQueryToken(colDesc);
            });
        }

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
    switch (token.filterType) {
        case "Boolean": return parseBoolean(val);
        case "Integer": return nanToNull(parseInt(val));
        case "Decimal": return nanToNull(parseFloat(val));
        case "Lite":
            {
                var lite = convertToLite(val);

                if (lite && !lite.toStr)
                    needToStr.push(lite);

                return lite;
            }
    }

    return val;
}

function nanToNull(n: number) {
    if (isNaN(n))
        return null;

    return n;
}

function convertToLite(val: string | Lite<Entity> | Entity): Lite<Entity> {
    if (val == null || val == "")
        return null; 

    if (isLite(val)) {
        if (val.entity != null && val.entity.id != null)
            return toLite(val.entity, false);

        return val;
    }

    if (isEntity(val))
        return toLite(val);

    if (typeof val === "string")
        return parseLite(val);

    throw new Error(`Impossible to convert ${val} to Lite`); 
}

export function clearQueryDescriptionCache() {
    queryDescriptionCache = {};
}

let queryDescriptionCache: { [queryKey: string]: QueryDescription } = {};
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
        return ajaxGet<QueryDescription>({ url: "~/api/query/description/" + queryKey });
    }

    export function fetchQueryEntity(queryKey: string): Promise<QueryEntity> {
        return ajaxGet<QueryEntity>({ url: "~/api/query/entity/" + queryKey });
    }

    export function search(request: QueryRequest): Promise<ResultTable> {
        return ajaxPost<ResultTable>({ url: "~/api/query/search" }, request);
    }

    export function queryCount(request: CountQueryRequest): Promise<number> {
        return ajaxPost<number>({ url: "~/api/query/queryCount" }, request);
    }

    export function findLiteLike(request: { types: string, subString: string, count: number }): Promise<Lite<Entity>[]> {
        return ajaxGet<Lite<Entity>[]>({
            url: currentHistory.createHref({ pathname: "~/api/query/findLiteLike", query: request })
        });
    }

    export function findTypeLike(request: { subString: string, count: number }): Promise<Lite<TypeEntity>[]> {
        return ajaxGet<Lite<TypeEntity>[]>({
            url: currentHistory.createHref({ pathname: "~/api/query/findLiteLike", query: request })
        });
    }

    export function findAllLites(request: { types: string }): Promise<Lite<Entity>[]> {
        return ajaxGet<Lite<Entity>[]>({
            url: currentHistory.createHref({ pathname: "~/api/query/findAllLites", query: request })
        });
    }

    export function findAllEntities(request: { types: string }): Promise<Entity[]> {
        return ajaxGet<Entity[]>({
            url: currentHistory.createHref({ pathname: "~/api/query/findAllEntities", query: request })
        });
    }

    export function parseTokens(queryKey: string, tokens: { token: string, options: SubTokensOptions }[]): Promise<QueryToken[]> {
        return ajaxPost<QueryToken[]>({ url: "~/api/query/parseTokens" }, { queryKey, tokens });
    }

    export function subTokens(queryKey: string, token: QueryToken, options: SubTokensOptions): Promise<QueryToken[]>{
        return ajaxPost<QueryToken[]>({ url: "~/api/query/subTokens" }, { queryKey, token: token == null ? null:  token.fullKey, options }).then(list=> {

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

    export function encodeFilters(query: any, filterOptions: FilterOption[]) {
        if (filterOptions)
            filterOptions.forEach((fo, i) => query["filter" + i] = getTokenString(fo) + "~" + (fo.operation || "EqualTo") + "~" + stringValue(fo.value));
    }

    export function encodeOrders(query: any, orderOptions: OrderOption[]) {
        if (orderOptions)
            orderOptions.forEach((oo, i) => query["order" + i] = (oo.orderType == "Descending" ? "-" : "") + getTokenString(oo));
    }

    export function encodeColumns(query: any, columnOptions: ColumnOption[]) {
        if (columnOptions)
            columnOptions.forEach((co, i) => query["column" + i] = getTokenString(co) + (co.displayName ? ("~" + scapeTilde(co.displayName)) : ""));
    }

    export function stringValue(value: any): string {

        if (value == null)
            return "";

        if (Array.isArray(value))
            return (value as any[]).map(a => stringValue(a)).join("~");

        if (value.Type)
            value = toLite(value as Entity);

        if (value.EntityType)
            return liteKey(value as Lite<Entity>);
        
        return scapeTilde(value.toString());
    }

    export function scapeTilde(str: string) {
        if (str == null)
            return "";

        return str.replace("~", "#|#");
    }

    export function getTokenString(tokenContainer: { columnName: string, token?: QueryToken }) {
        return tokenContainer.token ? tokenContainer.token.fullKey : tokenContainer.columnName;
    }
}

export module Decoder {
    export function valuesInOrder(query: any, prefix: string): string[] {
        var regex = new RegExp("^" + prefix + "(\\d*)$");

        return Dic.getKeys(query).map(s => regex.exec(s))
            .filter(r => !!r).orderBy(a => parseInt(a[1])).map(s => query[s[0]]);
    }


    export function decodeFilters(query: any): FilterOption[] {
        return valuesInOrder(query, "filter").map(val => {
            var parts = val.split("~");

            return {
                columnName: parts[0],
                operation: parts[1] as FilterOperation,
                value: parts.length == 3 ? unscapeTildes(parts[2]) :
                    parts.slice(2).map(a => unscapeTildes(a))
            } as FilterOption;
        });
    }

    export function unscapeTildes(str: string) {
        if (!str)
            return null;

        return str.replace("#|#", "~");
    }

    export function decodeOrders(query: any): OrderOption[] {

        return valuesInOrder(query, "order").map(val => ({
            orderType: val[0] == "-" ? "Descending" : "Ascending",
            columnName: val.tryAfter("-") || val
        } as OrderOption));
    }

    export function decodeColumns(query: any): ColumnOption[]{

        return valuesInOrder(query, "column").map(val => ({
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
    mode: "Paginate",
    elementsPerPage: 20,
    currentPage: 1,
};


export const defaultOrderColumn: string = "Id";

export interface QuerySettings {
    queryName: PseudoType | QueryKey;
    pagination?: Pagination;
    defaultOrderColumn?: string;
    hiddenColumns?: ColumnOption[];
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
        formatter: col => new CellFormatter(cell => cell ? <span>{cell.toStr || cell.toString() }</span> : null)
    },
    {
        name: "Enum",
        isApplicable: col => col.token.filterType == "Enum",
        formatter: col => new CellFormatter(cell => cell == null ? null : <span>{getEnumInfo(col.token.type.name, cell).niceName}</span>)
    },
    {
        name: "Lite",
        isApplicable: col => col.token.filterType == "Lite",
        formatter: col => new CellFormatter((cell: Lite<Entity>) => !cell ? null : <EntityLink lite={cell}/>)
    },

    {
        name: "Guid",
        isApplicable: col => col.token.filterType == "Guid",
        formatter: col => new CellFormatter((cell: string) => cell && <span className="guid">{cell.substr(0, 4) + "…" + cell.substring(cell.length - 4)}</span>)
    },
    {
        name: "DateTime",
        isApplicable: col => col.token.filterType == "DateTime",
        formatter: col=> {
            const momentFormat = toMomentFormat(col.token.format);
            return new CellFormatter((cell: string) => cell == null || cell == "" ? "" : <span>{moment(cell).format(momentFormat) }</span>)
        }
    },
    {
        name: "Number",
        isApplicable: col => col.token.filterType == "Integer" || col.token.filterType == "Decimal",
        formatter: col => new CellFormatter((cell: number) => cell && <span>{cell.toString() }</span>, "right")
    },
    {
        name: "Number with Unit",
        isApplicable: col => (col.token.filterType == "Integer" || col.token.filterType == "Decimal") && !!col.token.unit,
        formatter: col => new CellFormatter((cell: number) => cell && <span>{cell.toString() + " " + col.token.unit}</span>, "right")
    },
    {
        name: "Bool",
        isApplicable: col => col.token.filterType == "Boolean",
        formatter: col=> new CellFormatter((cell: boolean) => cell == null ? null : <input type="checkbox" disabled={true} checked={cell}/>, "center")
    },
];

export interface EntityFormatRule {
    name: string;
    formatter: EntityFormatter;
    isApplicable: (row: ResultRow) => boolean;
}


export type EntityFormatter = (row: ResultRow, columns: string[]) => React.ReactChild;

export const entityFormatRules: EntityFormatRule[] = [
    {
        name: "View",
        isApplicable: row=> true,
        formatter: row => !row.entity || !isNavigable(row.entity.EntityType, null, true) ? null :
            <EntityLink lite={row.entity} inSearch={true}>{EntityControlMessage.View.niceToString() }</EntityLink>
    },
];
