import * as React from "react";
import * as moment from "moment"
import * as numbro from "numbro"
import { Router, Route, Redirect } from "react-router"
import * as QueryString from "query-string"
import * as Navigator from "./Navigator"
import { Dic } from './Globals'
import { ajaxGet, ajaxPost } from './Services';

import {
    QueryDescription, QueryCountRequest, QueryRequest, QueryEntitiesRequest, FindOptions,
    FindOptionsParsed, FilterOption, FilterOptionParsed, OrderOptionParsed, CountOptionsParsed,
    QueryToken, ColumnDescription, ColumnOption, ColumnOptionParsed, Pagination, ResultColumn,
    ResultTable, ResultRow, OrderOption, SubTokensOptions, toQueryToken, isList, ColumnOptionsMode, FilterRequest, ModalFindOptions, OrderRequest
} from './FindOptions';

import { PaginationMode, OrderType, FilterOperation, FilterType, UniqueType, QueryTokenMessage } from './Signum.Entities.DynamicQuery';

import { Entity, Lite, toLite, liteKey, parseLite, EntityControlMessage, isLite, isEntityPack, isEntity, External } from './Signum.Entities';
import { TypeEntity, QueryEntity } from './Signum.Entities.Basics';

import {
    Type, IType, EntityKind, QueryKey, getQueryNiceName, getQueryKey, isQueryDefined, TypeReference,
    getTypeInfo, getTypeInfos, getEnumInfo, toMomentFormat, toNumbroFormat, PseudoType, EntityData,
    TypeInfo, PropertyRoute
} from './Reflection';

import SearchModal from './SearchControl/SearchModal';
import EntityLink from './SearchControl/EntityLink';
import SearchControlLoaded from './SearchControl/SearchControlLoaded';
import { ImportRoute } from "./AsyncImport";
import { SearchControl } from "./Search";


export const querySettings: { [queryKey: string]: QuerySettings } = {};


export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<ImportRoute path="~/find/:queryName" onImportModule={() => import("./SearchControl/SearchPage")} />);
}

export function addSettings(...settings: QuerySettings[]) {
    settings.forEach(s => Dic.addOrThrow(querySettings, getQueryKey(s.queryName), s));
}

export function getSettings(queryName: PseudoType | QueryKey): QuerySettings {
    return querySettings[getQueryKey(queryName)];
}

export const isFindableEvent: Array<(queryKey: string, fullScreen: boolean) => boolean> = [];

export function isFindable(queryName: PseudoType | QueryKey, fullScreen: boolean): boolean {

    if (!isQueryDefined(queryName))
        return false;

    const queryKey = getQueryKey(queryName);

    return isFindableEvent.every(f => f(queryKey, fullScreen));
}

export function find<T extends Entity = Entity>(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<Lite<T> | undefined>;
export function find<T extends Entity>(type: Type<T>, modalOptions?: ModalFindOptions): Promise<Lite<T> | undefined>;
export function find(obj: FindOptions | Type<any>, modalOptions?: ModalFindOptions): Promise<Lite<Entity> | undefined> {

    const fo = (obj as FindOptions).queryName ? obj as FindOptions :
        { queryName: obj as Type<any> } as FindOptions;

    if (fo.groupResults)
        throw new Error("Use findRow instead");

    var qs = getSettings(fo.queryName);
    if (qs && qs.onFind && !(modalOptions && modalOptions.useDefaultBehaviour))
        return qs.onFind(fo, modalOptions);

    let getPromiseSearchModal: () => Promise<Lite<Entity> | undefined> = () => import("./SearchControl/SearchModal")
        .then(a => a.default.open(fo, modalOptions))
        .then(rr => rr && rr.entity);

    if (modalOptions && modalOptions.autoSelectIfOne)
        return fetchEntitiesWithFilters(fo.queryName, fo.filterOptions || [], fo.orderOptions || [], 2)
            .then(data => {
                if (data.length == 1)
                    return Promise.resolve(data[0]);

                return getPromiseSearchModal();
            });

    return getPromiseSearchModal();
}

export function findRow(fo: FindOptions, modalOptions?: ModalFindOptions): Promise<ResultRow | undefined> {
    
    var qs = getSettings(fo.queryName);

    return import("./SearchControl/SearchModal")
        .then(a => a.default.open(fo, modalOptions));
}


export function findMany<T extends Entity = Entity>(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<Lite<T>[] | undefined>;
export function findMany<T extends Entity>(type: Type<T>, modalOptions?: ModalFindOptions): Promise<Lite<T>[] | undefined>;
export function findMany(findOptions: FindOptions | Type<any>, modalOptions?: ModalFindOptions): Promise<Lite<Entity>[] | undefined> {

    const fo = (findOptions as FindOptions).queryName ? findOptions as FindOptions :
        { queryName: findOptions as Type<any> } as FindOptions;

    if (fo.groupResults)
        throw new Error("Use findManyRows instead");

    var qs = getSettings(fo.queryName);
    if (qs && qs.onFindMany && !(modalOptions && modalOptions.useDefaultBehaviour))
        return qs.onFindMany(fo, modalOptions);

    let getPromiseSearchModal: () => Promise<Lite<Entity>[] | undefined> = () => import("./SearchControl/SearchModal")
        .then(a => a.default.openMany(fo, modalOptions))
        .then(rows => rows && rows.map(a => a.entity!));

    if (modalOptions && modalOptions.autoSelectIfOne)
        return fetchEntitiesWithFilters(fo.queryName, fo.filterOptions || [], fo.orderOptions || [], 2)
            .then(data => {
                if (data.length == 1)
                    return Promise.resolve(data);

                return getPromiseSearchModal();
            });

    return getPromiseSearchModal();
}

export function findManyRows(fo: FindOptions, modalOptions?: ModalFindOptions): Promise<ResultRow[] | undefined> {
    
    var qs = getSettings(fo.queryName);
  
    return import("./SearchControl/SearchModal")
        .then(a => a.default.openMany(fo, modalOptions));
}

export function exploreWindowsOpen(findOptions: FindOptions, e: React.MouseEvent<any>) {
    e.preventDefault();
    if (e.ctrlKey || e.button == 1)
        window.open(findOptionsPath(findOptions));
    else
        explore(findOptions).done();
}

export function explore(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<void> {

    var qs = getSettings(findOptions.queryName);
    if (qs && qs.onExplore && !(modalOptions && modalOptions.useDefaultBehaviour))
        return qs.onExplore(findOptions, modalOptions);

    return import("./SearchControl/SearchModal")
        .then(a => a.default.explore(findOptions, modalOptions));
}

export function findOptionsPath(fo: FindOptions, extra?: any): string {

    const query = findOptionsPathQuery(fo, extra);

    return Navigator.history.createHref({ pathname: "~/find/" + getQueryKey(fo.queryName), search: QueryString.stringify(query) });
}

export function findOptionsPathQuery(fo: FindOptions, extra?: any): any {
    fo = expandParentColumn(fo);

    const query = {
        groupResults: fo.groupResults || undefined,
        columnMode: (!fo.columnOptionsMode || fo.columnOptionsMode == "Add" as ColumnOptionsMode) ? undefined : fo.columnOptionsMode,
        paginationMode: fo.pagination && fo.pagination.mode,
        elementsPerPage: fo.pagination && fo.pagination.elementsPerPage,
        currentPage: fo.pagination && fo.pagination.currentPage,
        systemTimeMode: fo.systemTime && fo.systemTime.mode,
        systemTimeStartDate: fo.systemTime && fo.systemTime.startDate,
        systemTimeEndDate: fo.systemTime && fo.systemTime.endDate,
        ...extra
    };

    Encoder.encodeFilters(query, fo.filterOptions);
    Encoder.encodeOrders(query, fo.orderOptions);
    Encoder.encodeColumns(query, fo.columnOptions);

    return query;
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

    const result: FindOptions = {
        queryName: queryName,
        groupResults: parseBoolean(query.groupResults),
        filterOptions: Decoder.decodeFilters(query),
        orderOptions: Decoder.decodeOrders(query),
        columnOptions: Decoder.decodeColumns(query),
        columnOptionsMode: query.columnMode == undefined ? "Add" : query.columnMode,
        pagination: query.paginationMode && {
            mode: query.paginationMode,
            elementsPerPage: query.elementsPerPage,
            currentPage: query.currentPage,
        } as Pagination,
        systemTime: query.systemTimeMode && {
            mode: query.systemTimeMode,
            startDate: query.systemTimeStartDate,
            endDate: query.systemTimeEndDate,
        }
    };
    
    return Dic.simplify(result);
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

export function parseFilterOptions(fos: FilterOption[], groupResults: boolean, qd: QueryDescription): Promise<FilterOptionParsed[]> {

    const completer = new TokenCompleter(qd);
    var sto = SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | (groupResults ? SubTokensOptions.CanAggregate : 0);
    fos.forEach(a => completer.request(a.columnName, sto));

    return completer.finished()
        .then(() => fos.map(fo => ({
            token: completer.get(fo.columnName),
            operation: fo.operation || "EqualTo",
            value: fo.value,
            frozen: fo.frozen || false,
        }) as FilterOptionParsed))
        .then(filters => parseFilterValues(filters).then(() => filters));
}

export function parseOrderOptions(orderOptions: OrderOption[], groupResults: boolean, qd: QueryDescription): Promise<OrderOptionParsed[]> {

    const completer = new TokenCompleter(qd);
    var sto = SubTokensOptions.CanElement | (groupResults ? SubTokensOptions.CanAggregate : 0);
    orderOptions.forEach(a => completer.request(a.columnName, sto));

    return completer.finished()
        .then(() => orderOptions.map(oo => ({
            token: completer.get(oo.columnName),
            orderType: oo.orderType || "Ascending",
        }) as OrderOptionParsed));
}

export function parseColumnOptions(columnOptions: ColumnOption[], groupResults: boolean, qd: QueryDescription): Promise<ColumnOptionParsed[]> {

    const completer = new TokenCompleter(qd);
    var sto = SubTokensOptions.CanElement | (groupResults ? SubTokensOptions.CanAggregate : 0);
    columnOptions.forEach(a => completer.request(a.columnName, sto));

    return completer.finished()
        .then(() => columnOptions.map(co => ({
            token: completer.get(co.columnName),
            displayName: co.displayName || completer.get(co.columnName).niceName,
        }) as ColumnOptionParsed));
}

export function setFilters(e: Entity, filterOptionsParsed: FilterOptionParsed[]): Promise<Entity> {

    function getMemberForToken(ti: TypeInfo, fullKey: string) {
        var token = fullKey.tryAfter("Entity.") || fullKey;

        if (token.contains("."))
            return null;

        return ti.members[token];
    }

    const ti = getTypeInfo(e.Type);

    return Promise.all(filterOptionsParsed.filter(fo => fo.token && fo.operation == "EqualTo").map(fo => {

        const mi = getMemberForToken(ti, fo.token!.fullKey);

        if (mi && (e as any)[mi.name.firstLower()] == null) {
            const promise = Navigator.tryConvert(fo.value, mi.type);

            if (promise == null)
                return null;

            return promise.then(v => (e as any)[mi.name.firstLower()] = v);
        }

        return null;
    }).filter(p => !!p)).then(() => e);
}

export function toFindOptions(fo: FindOptionsParsed, qd: QueryDescription): FindOptions {

    const pair = smartColumns(fo.columnOptions, Dic.getValues(qd.columns));

    const qs = getSettings(fo.queryKey);

    const defPagination = qs && qs.pagination || defaultPagination;

    function equalsPagination(p1: Pagination, p2: Pagination) {
        return p1.mode == p2.mode && p1.elementsPerPage == p2.elementsPerPage && p1.currentPage == p2.currentPage;
    }

    var findOptions = {
        queryName: fo.queryKey,
        groupResults: fo.groupResults ? true : undefined,
        filterOptions: fo.filterOptions.filter(a => !!a.token).map(f => ({ columnName: f.token!.fullKey, operation: f.operation, value: f.value, frozen: f.frozen }) as FilterOption),
        orderOptions: fo.orderOptions.filter(a => !!a.token).map(o => ({ columnName: o.token.fullKey, orderType: o.orderType }) as OrderOption),
        columnOptions: pair.columns,
        columnOptionsMode: pair.mode,
        pagination: fo.pagination && !equalsPagination(fo.pagination, defPagination) ? fo.pagination : undefined,
        systemTime: fo.systemTime,
    } as FindOptions;

    if (!findOptions.groupResults && findOptions.orderOptions && findOptions.orderOptions.length == 1) {
        var onlyOrder = findOptions.orderOptions[0]
        var defaultOrder = getDefaultOrder(qd, qs);

        if (defaultOrder && onlyOrder.columnName == defaultOrder.columnName && onlyOrder.orderType == defaultOrder.orderType)
            findOptions.orderOptions.remove(onlyOrder);
    }

    return findOptions;
}

export const defaultOrderColumn: string = "Id";

export function getDefaultOrder(qd: QueryDescription, qs: QuerySettings): OrderOption | undefined {
    const defaultOrder = qs && qs.defaultOrderColumn || defaultOrderColumn;
    const tis = getTypeInfos(qd.columns["Entity"].type);

    if (defaultOrder == defaultOrderColumn && !qd.columns[defaultOrderColumn])
        return undefined;

    return {
        columnName: defaultOrder,
        orderType: qs && qs.defaultOrderType || (tis.some(a => a.entityData == "Transactional") ? "Descending" as OrderType : "Ascending" as OrderType)
    } as OrderOption;
}

export function toFilterOptions(filterOptionsParsed: FilterOptionParsed[]) {
    return filterOptionsParsed
        .filter(f => !!f.token)
        .map(f => ({ columnName: f.token!.fullKey, operation: f.operation, value: f.value, frozen: f.frozen }) as FilterOption);
}

export function parseFindOptions(findOptions: FindOptions, qd: QueryDescription): Promise<FindOptionsParsed> {

    const fo: FindOptions = { ...findOptions };

    expandParentColumn(fo);

    fo.columnOptions = mergeColumns(Dic.getValues(qd.columns), fo.columnOptionsMode || "Add", fo.columnOptions || []);

    var qs = querySettings[qd.queryKey];
    const tis = getTypeInfos(qd.columns["Entity"].type);


    if (!fo.groupResults && (!fo.orderOptions || fo.orderOptions.length == 0)) {
        var defaultOrder = getDefaultOrder(qd, qs);

        if (defaultOrder)
            fo.orderOptions = [defaultOrder];
    }

    var canAggregate = (findOptions ? SubTokensOptions.CanAggregate : 0);
    const completer = new TokenCompleter(qd);
    if (fo.filterOptions)
        fo.filterOptions.forEach(a => completer.request(a.columnName, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | canAggregate));

    if (fo.orderOptions)
        fo.orderOptions.forEach(a => completer.request(a.columnName, SubTokensOptions.CanElement | canAggregate));

    if (fo.columnOptions)
        fo.columnOptions.forEach(a => completer.request(a.columnName, SubTokensOptions.CanElement | canAggregate));

    return completer.finished().then(() => {

        var result: FindOptionsParsed = {
            queryKey: qd.queryKey,
            groupResults: fo.groupResults == true,
            pagination: fo.pagination != null ? fo.pagination : qs && qs.pagination || defaultPagination,
            systemTime: fo.systemTime,

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

export function validateNewEntities(fo: FindOptions): string | undefined {

    var types = [fo.parentValue, ...(fo.filterOptions || []).map(a => a.value)].flatMap(a => getTypeIfNew(a));

    if (types.length == 0)
        return undefined;

    return `Filtering by new ${types.joinComma(" and ")}. Consider hiding the control for new entities.`;
}

function getTypeIfNew(val: any): string[] {
    if (!val)
        return [];

    if (isEntity(val) && val.isNew)
        return [val.Type];

    if (isLite(val) && val.id == null)
        return [val.EntityType];

    if (Array.isArray(val))
        return val.flatMap(v => getTypeIfNew(v));

    return [];
}



export function exploreOrNavigate(findOptions: FindOptions): Promise<void> {
    return fetchEntitiesWithFilters(findOptions.queryName, findOptions.filterOptions || [], [], 2).then(list => {
        if (list.length == 1)
            return Navigator.navigate(list[0]);
        else
            return explore(findOptions);
    });
}

export function getCount(queryName: PseudoType | QueryKey, filterOptions: FilterOption[], valueToken?: string): Promise<number> {
    return getQueryDescription(queryName).then(qd => {
        return parseFilterOptions(filterOptions, false, qd).then(fop => {

            let filters = fop.map(fo => ({
                token: fo.token!.fullKey,
                operation: fo.operation,
                value: fo.value,
            } as FilterRequest));

            return API.queryCount({ queryKey: qd.queryKey, filters, valueToken });
        });
    });
}

export function fetchEntitiesWithFilters<T extends Entity>(queryName: Type<T>, filterOptions: FilterOption[], orderOptions: OrderOption[], count: number): Promise<Lite<T>[]>;
export function fetchEntitiesWithFilters(queryName: PseudoType | QueryKey, filterOptions: FilterOption[], orderOptions: OrderOption[], count: number): Promise<Lite<Entity>[]>;
export function fetchEntitiesWithFilters(queryName: PseudoType | QueryKey, filterOptions: FilterOption[], orderOptions: OrderOption[], count: number): Promise<Lite<Entity>[]> {
    return getQueryDescription(queryName).then(qd =>
        parseFilterOptions(filterOptions, false, qd)
            .then(fop =>
                parseOrderOptions(orderOptions, false, qd).then(oop =>
                    API.fetchEntitiesWithFilters({

                        queryKey: qd.queryKey,

                        filters: fop.map(fo => ({
                            token: fo.token!.fullKey,
                            operation: fo.operation,
                            value: fo.value,
                        } as FilterRequest)),

                        orders: oop.map(oo => ({
                            token: oo.token!.fullKey,
                            orderType: oo.orderType
                        }) as OrderRequest),

                        count: count
                    })
                )
            )
    );
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


    get(fullKey: string): QueryToken {
        if (this.isSimple(fullKey)) {
            const cd = this.queryDescription.columns[fullKey];

            if (cd == undefined)
                throw new Error(`Column '${fullKey}' is not a column of query '${this.queryDescription.queryKey}'. Maybe use 'Entity.${fullKey}' instead?`);

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

    return Navigator.API.fillToStringsArray(needToStr)
}


function parseValue(token: QueryToken, val: any, needToStr: Array<any>): any {
    switch (token.filterType) {
        case "Boolean": return parseBoolean(val);
        case "Integer": return nanToNull(parseInt(val));
        case "Decimal": return nanToNull(parseFloat(val));
        case "DateTime": return (val == null ? null : moment(val).format());
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

    export function executeQuery(request: QueryRequest, abortController?: FetchAbortController): Promise<ResultTable> {
        return ajaxPost<ResultTable>({ url: "~/api/query/executeQuery", abortController }, request);
    }

    export function queryCount(request: QueryCountRequest, avoidNotifyPendingRequest: boolean | undefined = undefined, abortController?: FetchAbortController): Promise<number> {
        return ajaxPost<number>({ url: "~/api/query/queryCount", avoidNotifyPendingRequests: avoidNotifyPendingRequest, abortController }, request);
    }

    export function fetchEntitiesWithFilters(request: QueryEntitiesRequest): Promise<Lite<Entity>[]> {
        return ajaxPost<Lite<Entity>[]>({ url: "~/api/query/entitiesWithFilter" }, request);
    }

    export function fetchAllLites(request: { types: string }): Promise<Lite<Entity>[]> {
        return ajaxGet<Lite<Entity>[]>({
            url: "~/api/query/allLites?" + QueryString.stringify(request)
        });
    }

    export function findTypeLike(request: { subString: string, count: number }): Promise<Lite<TypeEntity>[]> {
        return ajaxGet<Lite<TypeEntity>[]>({
            url: "~/api/query/findTypeLike?" + QueryString.stringify(request)
        });
    }

    export function findLiteLike(request: AutocompleteRequest, abortController?: FetchAbortController): Promise<Lite<Entity>[]> {
        return ajaxGet<Lite<Entity>[]>({ url: "~/api/query/findLiteLike?" + QueryString.stringify(request), abortController });
    }

    export interface AutocompleteRequest {
        types: string;
        subString: string;
        count: number;
    }

    export function findLiteLikeWithFilters(request: AutocompleteQueryRequest, abortController?: FetchAbortController): Promise<Lite<Entity>[]> {
        return ajaxPost<Lite<Entity>[]>({ url: "~/api/query/findLiteLikeWithFilters", abortController }, request);
    }

    export function parseTokens(queryKey: string, tokens: { token: string, options: SubTokensOptions }[]): Promise<QueryToken[]> {
        return ajaxPost<QueryToken[]>({ url: "~/api/query/parseTokens" }, { queryKey, tokens });
    }

    export function getSubTokens(queryKey: string, token: QueryToken | undefined, options: SubTokensOptions): Promise<QueryToken[]> {
        return ajaxPost<QueryToken[]>({ url: "~/api/query/subTokens" }, { queryKey, token: token == undefined ? undefined : token.fullKey, options }).then(list => {

            if (token == undefined) {
                const entity = list.filter(a => a.key == "Entity").single();

                list.filter(a => a.fullKey.startsWith("Entity.")).forEach(t => t.parent = entity);
            } else {
                list.forEach(t => t.parent = token);
            }
            return list;
        });
    }

    export interface AutocompleteQueryRequest {
        queryKey: string;
        filters: FilterRequest[];
        orders: OrderRequest[];
        subString: string;
        count: number;
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

        if (isEntity(value))
            value = toLite(value, value.isNew);

        if (isLite(value))
            return liteKey(value);

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
            columnName: val[0] == "-" ? val.tryAfter("-") : val
        } as OrderOption));
    }

    export function decodeColumns(query: any): ColumnOption[] {

        return valuesInOrder(query, "column").map(val => ({
            columnName: val.tryBefore("~") || val,
            displayName: unscapeTildes(val.tryAfter("~"))
        }) as ColumnOption);
    }
}


export module ButtonBarQuery {

    interface ButtonBarQueryContext {
        searchControl: SearchControlLoaded;
        findOptions: FindOptionsParsed;
    }

    export const onButtonBarElements: ((ctx: ButtonBarQueryContext) => React.ReactElement<any> | undefined)[] = [];

    export function getButtonBarElements(ctx: ButtonBarQueryContext): React.ReactElement<any>[] {
        return onButtonBarElements.map(f => f(ctx)).filter(a => a != undefined).map(a => a!);
    }
}


export let defaultPagination: Pagination = {
    mode: "Paginate",
    elementsPerPage: 20,
    currentPage: 1,
};



export interface QuerySettings {
    queryName: PseudoType | QueryKey;
    pagination?: Pagination;
    allowSystemTime?: boolean;
    defaultOrderColumn?: string;
    defaultOrderType?: OrderType;
    hiddenColumns?: ColumnOption[];
    formatters?: { [columnName: string]: CellFormatter };
    rowAttributes?: (row: ResultRow, columns: string[]) => React.HTMLAttributes<HTMLTableRowElement> | undefined;
    entityFormatter?: EntityFormatter;
    onDoubleClick?: (e: React.MouseEvent<any>, row: ResultRow) => void;
    simpleFilterBuilder?: (qd: QueryDescription, initialFilterOptions: FilterOptionParsed[]) => React.ReactElement<any> | undefined;
    onFind?: (fo: FindOptions, mo?: ModalFindOptions) => Promise<Lite<Entity> | undefined>;
    onFindMany?: (fo: FindOptions, mo?: ModalFindOptions) => Promise<Lite<Entity>[] | undefined>;
    onExplore?: (fo: FindOptions, mo?: ModalFindOptions) => Promise<void>;
}

export interface FormatRule {
    name: string;
    formatter: (column: ColumnOptionParsed) => CellFormatter;
    isApplicable: (column: ColumnOptionParsed, sc: SearchControlLoaded | undefined) => boolean;
}

export class CellFormatter {
    constructor(
        public formatter: (cell: any, ctx: CellFormatterContext) => React.ReactChild | undefined,
        public cellClass?: string) {
    }
}

export interface CellFormatterContext {
    refresh?: () => void;
}


export function getCellFormatter(qs: QuerySettings, co: ColumnOptionParsed, sc: SearchControlLoaded | undefined): CellFormatter | undefined {
    if (!co.token)
        return undefined;

    const result = qs && qs.formatters && qs.formatters[co.token.fullKey];

    if (result)
        return result;

    const prRoute = registeredPropertyFormatters[co.token.propertyRoute!];
    if (prRoute)
        return prRoute;

    const rule = formatRules.filter(a => a.isApplicable(co, sc)).last("FormatRules");

    return rule.formatter(co);
}

export const registeredPropertyFormatters: { [typeAndProperty: string]: CellFormatter } = {};

export function registerPropertyFormatter(pr: PropertyRoute, formater: CellFormatter) {
    registeredPropertyFormatters[pr.toString()] = formater;
}


export const formatRules: FormatRule[] = [
    {
        name: "Object",
        isApplicable: col => true,
        formatter: col => new CellFormatter(cell => cell ? <span>{cell.toStr || cell.toString()}</span> : undefined)
    },
    {
        name: "Enum",
        isApplicable: col => col.token!.filterType == "Enum",
        formatter: col => new CellFormatter(cell => cell == undefined ? undefined : <span>{getEnumInfo(col.token!.type.name, cell).niceName}</span>)
    },
    {
        name: "Lite",
        isApplicable: col => col.token!.filterType == "Lite",
        formatter: col => new CellFormatter((cell: Lite<Entity>, ctx) => !cell ? undefined : <EntityLink lite={cell} onNavigated={ctx.refresh} />)
    },

    {
        name: "Guid",
        isApplicable: col => col.token!.filterType == "Guid",
        formatter: col => new CellFormatter((cell: string) => cell && <span className="guid">{cell.substr(0, 4) + "…" + cell.substring(cell.length - 4)}</span>)
    },
    {
        name: "DateTime",
        isApplicable: col => col.token!.filterType == "DateTime",
        formatter: col => {
            const momentFormat = toMomentFormat(col.token!.format);
            return new CellFormatter((cell: string) => cell == undefined || cell == "" ? "" : <bdi>{moment(cell).format(momentFormat)}</bdi>) //To avoid flippig hour and date (L LT) in RTL cultures 
        }
    },
    {
        name: "Number",
        isApplicable: col => col.token!.filterType == "Integer" || col.token!.filterType == "Decimal",
        formatter: col => {
            const numbroFormat = toNumbroFormat(col.token!.format);
            return new CellFormatter((cell: number) => cell == undefined ? "" : <span>{numbro(cell).format(numbroFormat)}</span>, "numeric-cell");
        }
    },
    {
        name: "Number with Unit",
        isApplicable: col => (col.token!.filterType == "Integer" || col.token!.filterType == "Decimal") && !!col.token!.unit,
        formatter: col => {
            const numbroFormat = toNumbroFormat(col.token!.format);
            return new CellFormatter((cell: number) => cell == undefined ? "" : <span>{numbro(cell).format(numbroFormat) + "\u00a0" + col.token!.unit}</span>, "numeric-cell");
        }
    },
    {
        name: "Bool",
        isApplicable: col => col.token!.filterType == "Boolean",
        formatter: col => new CellFormatter((cell: boolean) => cell == undefined ? undefined : <input type="checkbox" disabled={true} checked={cell} />, "centered-cell")
    },
];

export interface EntityFormatRule {
    name: string;
    formatter: EntityFormatter;
    isApplicable: (row: ResultRow, sc: SearchControlLoaded | undefined) => boolean;
}


export type EntityFormatter = (row: ResultRow, columns: string[], sc?: SearchControlLoaded) => React.ReactChild | undefined;

export const entityFormatRules: EntityFormatRule[] = [
    {
        name: "View",
        isApplicable: row => true,
        formatter: (row, columns, sc) => !row.entity || !Navigator.isNavigable(row.entity.EntityType, undefined, true) ? undefined :
            <EntityLink lite={row.entity}
                inSearch={true}
                onNavigated={sc && sc.handleOnNavigated}
                getViewPromise={sc && sc.props.getViewPromise}>
                {EntityControlMessage.View.niceToString()}
            </EntityLink>
    },
];
