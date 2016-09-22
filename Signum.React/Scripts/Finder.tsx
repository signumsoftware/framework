import * as React from "react"
import * as moment from "moment"
import * as numbro from "numbro"
import { Router, Route, Redirect, IndexRoute } from "react-router"
import { Dic } from './Globals'
import { ajaxGet, ajaxPost } from './Services';

import {
    QueryDescription, CountQueryRequest, QueryRequest, QueryEntitiesRequest, FindOptions, 
    FindOptionsParsed, FilterOption, FilterOptionParsed, OrderOptionParsed, CountOptionsParsed,
    QueryToken, ColumnDescription, ColumnOption, ColumnOptionParsed, Pagination, ResultColumn,
    ResultTable, ResultRow, OrderOption, SubTokensOptions, toQueryToken, isList, ColumnOptionsMode, FilterRequest
} from './FindOptions';

import { PaginationMode, OrderType, FilterOperation, FilterType, UniqueType, QueryTokenMessage } from './Signum.Entities.DynamicQuery';

import { Entity, Lite, toLite, liteKey, parseLite, EntityControlMessage, isLite, isEntityPack, isEntity, External } from './Signum.Entities';
import { TypeEntity, QueryEntity } from './Signum.Entities.Basics';

import {
    Type, IType, EntityKind, QueryKey, getQueryNiceName, getQueryKey, isQueryDefined, TypeReference,
    getTypeInfo, getTypeInfos, getEnumInfo, toMomentFormat, toNumbroFormat, PseudoType, EntityData,
    TypeInfo, PropertyRoute
} from './Reflection';

import { navigateRoute, isNavigable, currentHistory, API as NavAPI, isCreable, tryConvert } from './Navigator';
import SearchModal from './SearchControl/SearchModal';
import EntityLink from './SearchControl/EntityLink';
import SearchControl from './SearchControl/SearchControlLoaded';


export const querySettings: { [queryKey: string]: QuerySettings } = {};

export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<Route path="find">
        <Route path=":queryName" getComponent={ (loc, cb) => require(["./SearchControl/SearchPage"], (Comp) => cb(undefined, Comp.default)) } />
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

export function find(findOptions: FindOptions): Promise<Lite<Entity> | undefined>;
export function find<T extends Entity>(type: Type<T>): Promise<Lite<T> | undefined>;
export function find(obj: FindOptions | Type<any>): Promise<Lite<Entity> | undefined> {

    const fo = (obj as FindOptions).queryName ? obj as FindOptions :
        { queryName: obj as Type<any> } as FindOptions;
    
    return new Promise<Lite<Entity>>((resolve, reject) => {
        require(["./SearchControl/SearchModal"], function (SP: { default: typeof SearchModal }) {
            SP.default.open(fo).then(resolve, reject);
        });
    });
}

export function findMany(findOptions: FindOptions): Promise<Lite<Entity>[] | undefined>;
export function findMany<T extends Entity>(type: Type<T>): Promise<Lite<T>[] | undefined>;
export function findMany(findOptions: FindOptions | Type<any>): Promise<Lite<Entity>[] | undefined> {

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

export function getTypeNiceName(tr: TypeReference) {

    const niceName = tr.typeNiceName ||
        getTypeInfos(tr)
            .map(ti => ti == undefined ? getSimpleTypeNiceName(tr.name) : (ti.niceName || ti.name))
            .joinComma(External.CollectionMessage.Or.niceToString());

    return tr.isCollection ? QueryTokenMessage.ListOf0.niceToString(niceName) : niceName;
}

export function getSimpleTypeNiceName(name: string) {

    switch (name) {
        case "string":
        case "guid":
            return QueryTokenMessage.Text.niceToString();
        case "datetime": return QueryTokenMessage.DateTime.niceToString();
        case "number": return QueryTokenMessage.Number.niceToString();
        case "decimal": return QueryTokenMessage.DecimalNumber.niceToString();
        case "boolean": return QueryTokenMessage.Check.niceToString();
    }

    return name;
}


export function parseFindOptionsPath(queryName: PseudoType | QueryKey, query: any): FindOptions {
    
    const result = {
        queryName: queryName,
        filterOptions: Decoder.decodeFilters(query),
        orderOptions: Decoder.decodeOrders(query),
        columnOptions: Decoder.decodeColumns(query),
        columnOptionsMode: query.columnMode == undefined ? "Add" : query.columnMode,
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

export function mergeColumns(columnDescriptions: ColumnDescription[], mode: ColumnOptionsMode, columnOptions: ColumnOption[]): ColumnOption[] {

    switch (mode) {
        case "Add":
            return columnDescriptions.filter(cd => cd.name != "Entity").map(cd => ({ columnName: cd.name, displayName: cd.displayName }) as ColumnOption)
                .concat(columnOptions);

        case "Remove":
            return columnDescriptions.filter(cd => cd.name != "Entity" && !columnOptions.some(a => a.columnName == cd.name))
                .map(cd => ({ columnName: cd.name, displayName: cd.displayName }) as ColumnOption);

        case "Replace":
            return columnOptions;

        default: throw new Error("Unexpected column mode");
    }
}

export function smartColumns(current: ColumnOptionParsed[], ideal: ColumnDescription[]): { mode: ColumnOptionsMode; columns: ColumnOption[] } {

    const similar = (c: ColumnOptionParsed, d: ColumnDescription) =>
        c.token!.fullKey == d.name && (c.displayName == d.displayName);

    ideal = ideal.filter(a => a.name != "Entity");

    current = current.filter(a => a.token != null);

    if (current.length < ideal.length) {
        const toRemove: ColumnOption[] = [];

        let j = 0;
        for (let i = 0; i < ideal.length; i++) {
            if (j < current.length && similar(current[j], ideal[i]))
                j++;
            else
                toRemove.push({ columnName: ideal[i].name, });
        }
        if (toRemove.length + current.length == ideal.length) {
            return {
                mode: "Remove",
                columns: toRemove
            };
        }
    }
    else if (current.every((c, i) => i >= ideal.length || similar(c, ideal[i]))) {
        return {
            mode: "Add",
            columns: current.slice(ideal.length).map(c => ({ columnName: c.token!.fullKey, displayName: c.displayName }) as ColumnOption)
        };
    }
    
    return {
        mode: "Replace",
        columns: current.map(c => ({ columnName: c.token!.fullKey, displayName: c.displayName }) as ColumnOption),
    };
}

function parseBoolean(value: any): boolean | undefined {
    if (value === "true" || value === true)
        return true;

    if (value === "false" || value === false)
        return false;

    return undefined;
}

export function parseFilterOptions(filterOptions: FilterOption[], qd: QueryDescription): Promise<FilterOptionParsed[]> {

    const completer = new TokenCompleter(qd);
    filterOptions.forEach(a => completer.request(a.columnName, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll));

    return completer.finished()
        .then(() => filterOptions.map(fo => ({
            token: completer.get(fo.columnName),
            operation: fo.operation || "EqualTo",
            value: fo.value,
            frozen: fo.frozen || false,
        }) as FilterOptionParsed))
        .then(filters => parseFilterValues(filters).then(() => filters));
}

export function setFilters(e: Entity, filterOptionsParsed: FilterOptionParsed[]) : Promise<Entity> {

    function getMemberForToken(ti: TypeInfo, fullKey: string) {
        var token = fullKey.tryAfter("Entity.") || fullKey;

        if (token.contains("."))
            return null;

        return ti.members[token];
    }

    const ti = getTypeInfo(e.Type);

    return Promise.all(filterOptionsParsed.filter(fo => fo.token && fo.operation == "EqualTo").map(fo => {

        const mi = getMemberForToken(ti, fo.token!.fullKey);

        if (mi && (e as any)[mi.name] == null) {
            const promise = tryConvert(fo.value, mi.type);

            if (promise == null)
                return null;

            return promise.then(v => (e as any)[mi.name.firstLower()] = v);
        }

        return null;
    }).filter(p => !!p)).then(() => e);
}

export function parseFindOptions(findOptions: FindOptions, qd: QueryDescription): Promise<FindOptionsParsed> {

    const fo = Dic.extend({}, findOptions) as FindOptions;

    expandParentColumn(fo);

    fo.columnOptions = mergeColumns(Dic.getValues(qd.columns), fo.columnOptionsMode || "Add", fo.columnOptions || []);

    var qs = querySettings[qd.queryKey];

    const tis = getTypeInfos(qd.columns["Entity"].type);

    if (!fo.orderOptions || fo.orderOptions.length == 0) {
        const defaultOrder = qs && qs.defaultOrderColumn || defaultOrderColumn;

        if (qd.columns[defaultOrder]) {
            fo.orderOptions = [{
                columnName: defaultOrder,
                orderType: tis.some(a => a.entityData == "Transactional") ? "Descending" as OrderType : "Ascending" as OrderType
            }];
        }
    }
    
    const completer = new TokenCompleter(qd);
    if (fo.filterOptions)
        fo.filterOptions.forEach(a => completer.request(a.columnName, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll));

    if (fo.orderOptions)
        fo.orderOptions.forEach(a => completer.request(a.columnName, SubTokensOptions.CanElement));

    if (fo.columnOptions)
        fo.columnOptions.forEach(a => completer.request(a.columnName, SubTokensOptions.CanElement));

    return completer.finished().then(() => {
        
        var result: FindOptionsParsed = {
            queryKey: qd.queryKey,
            searchOnLoad: fo.searchOnLoad != null ? fo.searchOnLoad : true,
            showHeader: fo.showHeader != null ? fo.showHeader :true,
            showFilters: fo.showFilters != null ? fo.showFilters :false,
            showFilterButton: fo.showFilterButton != null ? fo.showFilterButton :true,
            showFooter: fo.showFooter != null ? fo.showFooter : true,
            allowChangeColumns: fo.allowChangeColumns != null ? fo.allowChangeColumns : true,
            create: fo.create != null ? fo.create :tis.some(ti => isCreable(ti, false, true)),
            navigate: fo.navigate != null ? fo.navigate :tis.some(ti => isNavigable(ti, undefined, true)),
            pagination: fo.pagination != null ? fo.pagination :qs && qs.pagination || defaultPagination,
            contextMenu: fo.contextMenu != null ? fo.contextMenu :true,


            columnOptions: (fo.columnOptions || []).map(co => ({
                token: completer.get(co.columnName),
                displayName: co.displayName || completer.get(co.columnName).niceName
            }) as ColumnOptionParsed),

            orderOptions: (fo.orderOptions || []).map(oo => ({
                token: completer.get(oo.columnName),
                orderType: oo.orderType,
            }) as OrderOptionParsed),

            filterOptions: (fo.filterOptions || []).map(fo => ({
                token: completer.get(fo.columnName),
                operation: fo.operation || "EqualTo",
                value: fo.value,
                frozen: fo.frozen || false,
            }) as FilterOptionParsed),
        };

        return parseFilterValues(result.filterOptions)
            .then(() => result)
    });
}

export function fetchEntitiesWithFilters(queryName: PseudoType | QueryKey, filterOptions: FilterOption[], count: number) : Promise<Lite<Entity>[]> {
    return getQueryDescription(queryName).then(qd => {
        return parseFilterOptions(filterOptions, qd).then(fop => {

            let filters = fop.map(fo => ({
                token: fo.token!.fullKey,
                operation: fo.operation,
                value: fo.value,
            } as FilterRequest));

            return API.fetchEntitiesWithFilters({
                queryKey: qd.queryKey,
                filters: filters,
                count: count
            });
        }); 
    });
}

export function expandParentColumn(fo: FindOptions): FindOptions {
    
    if (!fo.parentColumn)
        return fo;

    fo.filterOptions = [
        { columnName: fo.parentColumn, operation: "EqualTo", value: fo.parentValue, frozen: true },
        ...(fo.filterOptions || [])
    ];

    if (!fo.parentColumn.contains(".") && (fo.columnOptionsMode == undefined || fo.columnOptionsMode == "Remove")) {
        fo.columnOptions = [
            { columnName: fo.parentColumn },
            ...(fo.columnOptions || [])
        ];

        fo.columnOptionsMode = "Remove";
    }

    if (fo.searchOnLoad == undefined)
        fo.searchOnLoad = true;

    fo.parentColumn = undefined;
    fo.parentValue = undefined;

    return fo;
}

export function parseSingleToken(queryName: PseudoType | QueryKey, token: string, subTokenOptions: SubTokensOptions): Promise<QueryToken> {

    return getQueryDescription(getQueryKey(queryName)).then(qd => {
        const completer = new TokenCompleter(qd);
        const result = completer.request(token, subTokenOptions);
        return completer.finished().then(() => completer.get(token));
    });
}

export class TokenCompleter {
    constructor(public queryDescription: QueryDescription) { }

    tokensToRequest: {
        [fullKey: string]: (
            {
                options: SubTokensOptions,
                token?: QueryToken,
            })
    } = {};

    request(fullKey: string, options: SubTokensOptions): void {

        if (this.isSimple(fullKey)) 
            return;
        
        if (this.tokensToRequest[fullKey])
            return;

        this.tokensToRequest[fullKey] = {
            options: options
        };
    }

    isSimple(fullKey: string) {
        return !fullKey.contains(".") && fullKey != "Count";
    }

    finished(): Promise<void> {
        
        const tokens = Dic.map(this.tokensToRequest, (token, val) => ({ token: token, options: val.options }));
        
        if (tokens.length == 0)
            return Promise.resolve(undefined);

        return API.parseTokens(this.queryDescription.queryKey, tokens).then(parsedTokens => {
            parsedTokens.forEach(t => this.tokensToRequest[t.fullKey].token = t);
        });
    }


    get(fullKey: string): QueryToken{
        if (this.isSimple(fullKey)) {
            const cd = this.queryDescription.columns[fullKey];

            if (cd == undefined)
                throw new Error(`Column '${fullKey}' not found in '${this.queryDescription.queryKey}'`);

            return toQueryToken(cd);
        }

        return this.tokensToRequest[fullKey].token!;
    }
}



export function parseFilterValues(filterOptions: FilterOptionParsed[]): Promise<void> {

    const needToStr: Lite<any>[] = [];
    filterOptions.filter(fo => fo.token != null).forEach(fo => {
        if (isList(fo.operation!)) {
            if (!Array.isArray(fo.value))
                fo.value = [fo.value];

            fo.value = (fo.value as any[]).map(v => parseValue(fo.token!, v, needToStr));
        }

        else {
            if (Array.isArray(fo.value))
                throw new Error("Unespected array for operation " + fo.operation);

            fo.value = parseValue(fo.token!, fo.value, needToStr);
        }
    });

    if (needToStr.length == 0)
        return Promise.resolve(undefined);

    return NavAPI.fillToStrings(needToStr)
}


function parseValue(token: QueryToken, val: any, needToStr: Array<any>): any {
    switch (token.filterType) {
        case "Boolean": return parseBoolean(val);
        case "Integer": return nanToNull(parseInt(val));
        case "Decimal": return nanToNull(parseFloat(val));
        case "Lite":
            {
                const lite = convertToLite(val);

                if (lite && !lite.toStr)
                    needToStr.push(lite);

                return lite;
            }
    }

    return val;
}

function nanToNull(n: number) {
    if (isNaN(n))
        return undefined;

    return n;
}

function convertToLite(val: string | Lite<Entity> | Entity | undefined): Lite<Entity> | undefined {
    if (val == undefined || val == "")
        return undefined; 

    if (isLite(val)) {
        if (val.entity != undefined && val.entity.id != undefined)
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
        return ajaxGet<QueryEntity>({ url: "~/api/query/queryEntity/" + queryKey });
    }

    export function executeQuery(request: QueryRequest): Promise<ResultTable> {
        return ajaxPost<ResultTable>({ url: "~/api/query/executeQuery" }, request);
    }
    
    export function queryCount(request: CountQueryRequest): Promise<number> {
        return ajaxPost<number>({ url: "~/api/query/queryCount" }, request);
    }

    export function fetchEntitiesWithFilters(request: QueryEntitiesRequest): Promise<Lite<Entity>[]> {
        return ajaxPost<Lite<Entity>[]>({ url: "~/api/query/entitiesWithFilter" }, request);
    }
    
    export function fetchAllLites(request: { types: string }): Promise<Lite<Entity>[]> {
        return ajaxGet<Lite<Entity>[]>({
            url: currentHistory.createHref({ pathname: "~/api/query/allLites", query: request })
        });
    }

    export function findTypeLike(request: { subString: string, count: number }): Promise<Lite<TypeEntity>[]> {
        return ajaxGet<Lite<TypeEntity>[]>({
            url: currentHistory.createHref({ pathname: "~/api/query/findTypeLike", query: request })
        });
    }

    export function findLiteLike(request: { types: string, subString: string, count: number }): Promise<Lite<Entity>[]> {
        return ajaxGet<Lite<Entity>[]>({
            url: currentHistory.createHref({ pathname: "~/api/query/findLiteLike", query: request })
        });
    }


    export function findLiteLikeWithFilters(request: { queryKey: string, filters: FilterRequest[], subString: string, count: number }): Promise<Lite<Entity>[]> {
        return ajaxPost<Lite<Entity>[]>({ url: "~/api/query/findLiteLikeWithFilters" }, request);
    }

    export function parseTokens(queryKey: string, tokens: { token: string, options: SubTokensOptions }[]): Promise<QueryToken[]> {
        return ajaxPost<QueryToken[]>({ url: "~/api/query/parseTokens" }, { queryKey, tokens });
    }

    export function subTokens(queryKey: string, token: QueryToken | undefined, options: SubTokensOptions): Promise<QueryToken[]>{
        return ajaxPost<QueryToken[]>({ url: "~/api/query/subTokens" }, { queryKey, token: token == undefined ? undefined:  token.fullKey, options }).then(list=> {

            if (token == undefined) {
                const entity = list.filter(a => a.key == "Entity").single();

                list.filter(a => a.fullKey.startsWith("Entity.")).forEach(t => t.parent = entity);
            } else {
                list.forEach(t => t.parent = token);
            }
            return list;
        });
    }
}



export module Encoder {

    export function encodeFilters(query: any, filterOptions?: FilterOption[]) {
        if (filterOptions)
            filterOptions.forEach((fo, i) => query["filter" + i] = getTokenString(fo) + "~" + (fo.operation || "EqualTo") + "~" + stringValue(fo.value));
    }

    export function encodeOrders(query: any, orderOptions?: OrderOption[]) {
        if (orderOptions)
            orderOptions.forEach((oo, i) => query["order" + i] = (oo.orderType == "Descending" ? "-" : "") + getTokenString(oo));
    }

    export function encodeColumns(query: any, columnOptions?: ColumnOption[]) {
        if (columnOptions)
            columnOptions.forEach((co, i) => query["column" + i] = getTokenString(co) + (co.displayName ? ("~" + scapeTilde(co.displayName)) : ""));
    }

    export function stringValue(value: any): string {

        if (value == undefined)
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
        if (str == undefined)
            return "";

        return str.replace("~", "#|#");
    }

    export function getTokenString(tokenContainer: { columnName: string, token?: QueryToken }) {
        return tokenContainer.token ? tokenContainer.token.fullKey : tokenContainer.columnName;
    }
}



export module Decoder {
    export function valuesInOrder(query: any, prefix: string): string[] {
        const regex = new RegExp("^" + prefix + "(\\d*)$");

        return Dic.getKeys(query).map(s => regex.exec(s))
            .filter(r => !!r).map(r => r!).orderBy(a => parseInt(a[1])).map(s => query[s[0]]);
    }


    export function decodeFilters(query: any): FilterOption[] {
        return valuesInOrder(query, "filter").map(val => {
            const parts = val.split("~");

            return {
                columnName: parts[0],
                operation: parts[1] as FilterOperation,
                value: parts.length == 3 ? unscapeTildes(parts[2]) :
                    parts.slice(2).map(a => unscapeTildes(a))
            } as FilterOption;
        });
    }

    export function unscapeTildes(str: string | undefined): string | undefined {
        if (!str)
            return undefined;

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
        findOptions: FindOptionsParsed;
    }

    export const onButtonBarElements: ((ctx: ButtonBarQueryContext) => React.ReactElement<any> | undefined)[] = [];

    export function getButtonBarElements(ctx: ButtonBarQueryContext): React.ReactElement<any>[] {
        return onButtonBarElements.map(f => f(ctx)).filter(a => a != undefined).map(a => a!);
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
    rowAttributes?: (row: ResultRow, columns: string[]) => React.HTMLAttributes | undefined;
    entityFormatter?: EntityFormatter;
    simpleFilterBuilder?: (qd: QueryDescription, initialFindOptions: FindOptionsParsed) => React.ReactElement<any> | undefined;
}

export interface FormatRule {
    name: string;
    formatter: (column: ColumnOptionParsed) => CellFormatter;
    isApplicable: (column: ColumnOptionParsed) => boolean;
}

export class CellFormatter {
    constructor(
        public formatter: (cell: any) => React.ReactChild | undefined,
        public textAllign = "left") {
    }
}


export function getCellFormatter(qs: QuerySettings, co: ColumnOptionParsed): CellFormatter | undefined {
    if (!co.token)
        return undefined;

    const result = qs && qs.formatters && qs.formatters[co.token.fullKey];

    if (result)
        return result; 

    const prRoute = registeredPropertyFormatters[co.token.propertyRoute!];
    if (prRoute)
        return prRoute;

    const rule = formatRules.filter(a => a.isApplicable(co)).last("FormatRules");
    
    return rule.formatter(co)
}

export const registeredPropertyFormatters: { [typeAndProperty: string]: CellFormatter } = {};

export function registerPropertyFormatter(pr: PropertyRoute, formater: CellFormatter) {
    registeredPropertyFormatters[pr.toString()] = formater;
}


export const formatRules: FormatRule[] = [
    {
        name: "Object",
        isApplicable: col=> true,
        formatter: col => new CellFormatter(cell => cell ? <span>{cell.toStr || cell.toString() }</span> : undefined)
    },
    {
        name: "Enum",
        isApplicable: col => col.token!.filterType == "Enum",
        formatter: col => new CellFormatter(cell => cell == undefined ? undefined : <span>{getEnumInfo(col.token!.type.name, cell).niceName}</span>)
    },
    {
        name: "Lite",
        isApplicable: col => col.token!.filterType == "Lite",
        formatter: col => new CellFormatter((cell: Lite<Entity>) => !cell ? undefined : <EntityLink lite={cell}/>)
    },

    {
        name: "Guid",
        isApplicable: col => col.token!.filterType == "Guid",
        formatter: col => new CellFormatter((cell: string) => cell && <span className="guid">{cell.substr(0, 4) + "…" + cell.substring(cell.length - 4)}</span>)
    },
    {
        name: "DateTime",
        isApplicable: col => col.token!.filterType == "DateTime",
        formatter: col=> {
            const momentFormat = toMomentFormat(col.token!.format);
            return new CellFormatter((cell: string) => cell == undefined || cell == "" ? "" : <span>{moment(cell).format(momentFormat) }</span>)
        }
    },
    {
        name: "Number",
        isApplicable: col => col.token!.filterType == "Integer" || col.token!.filterType == "Decimal",
        formatter: col => {
            const numbroFormat = toNumbroFormat(col.token!.format);
            return new CellFormatter((cell: number) => cell == undefined ? "" : <span>{numbro(cell).format(numbroFormat)}</span>, "right");
        }
    },
    {
        name: "Number with Unit",
        isApplicable: col => (col.token!.filterType == "Integer" || col.token!.filterType == "Decimal") && !!col.token!.unit,
        formatter: col => {
            const numbroFormat = toNumbroFormat(col.token!.format);
            return new CellFormatter((cell: number) => cell == undefined ? "" : <span>{numbro(cell).format(numbroFormat) + " " + col.token!.unit}</span>, "right");
        }
    },
    {
        name: "Bool",
        isApplicable: col => col.token!.filterType == "Boolean",
        formatter: col=> new CellFormatter((cell: boolean) => cell == undefined ? undefined : <input type="checkbox" disabled={true} checked={cell}/>, "center")
    },
];

export interface EntityFormatRule {
    name: string;
    formatter: EntityFormatter;
    isApplicable: (row: ResultRow) => boolean;
}


export type EntityFormatter = (row: ResultRow, columns: string[]) => React.ReactChild | undefined;

export const entityFormatRules: EntityFormatRule[] = [
    {
        name: "View",
        isApplicable: row=> true,
        formatter: row => !row.entity || !isNavigable(row.entity.EntityType, undefined, true) ? undefined :
            <EntityLink lite={row.entity} inSearch={true}>{EntityControlMessage.View.niceToString() }</EntityLink>
    },
];
