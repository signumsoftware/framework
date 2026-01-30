import * as React from "react";
import { RouteObject } from 'react-router'
import { DateTime, Duration } from 'luxon'
import * as AppContext from "./AppContext"
import { Navigator, ViewPromise } from "./Navigator"
import { Dic, classes, isNumber, isPromise, softCast } from './Globals'
import { ajaxGet, ajaxPost } from './Services';

import {
  QueryDescription, QueryValueRequest, QueryRequest, QueryEntitiesRequest, FindOptions,
  FindOptionsParsed, FilterOption, FilterOptionParsed, OrderOptionParsed,
  QueryToken, ColumnOption, ColumnOptionParsed, Pagination,
  ResultTable, ResultRow, OrderOption, SubTokensOptions, isList, ColumnOptionsMode, FilterRequest, ModalFindOptions, OrderRequest,
  FilterGroupOptionParsed, FilterConditionOptionParsed, FilterGroupOption,
  FilterConditionOption, FilterGroupRequest, FilterConditionRequest, PinnedFilter, SystemTime, hasAnyOrAll, hasAggregate, hasElement,
  toPinnedFilterParsed, isActive, hasOperation, hasToArray, ModalFindOptionsMany, canSplitValue, getFilterOperations, isFilterGroup, hasManual,
  hasSnippet,
  hasNested,
  hasTimeSeries,
  QueryDescriptionDTO,
  QueryTokenWithoutParent
} from './FindOptions';

import { FilterOperation, FilterGroupOperation, PinnedFilterActive } from './Signum.DynamicQuery';

import { Entity, Lite, toLite, liteKey, parseLite, isLite, isEntity, SearchMessage, ModifiableEntity, is, JavascriptMessage, isMListElement, MListElement, getToString, EmbeddedEntity, ModelEntity, isModifiableEntity } from './Signum.Entities';
import { TypeEntity, QueryEntity } from './Signum.Basics';

import {
  Type, QueryKey, getQueryKey, isQueryDefined, TypeReference,
  getTypeInfo, tryGetTypeInfos, getEnumInfo, toLuxonFormat, toNumberFormat, PseudoType,
  TypeInfo, PropertyRoute, QueryTokenString, getTypeInfos, tryGetTypeInfo, onReloadTypesActions,
  Anonymous, toLuxonDurationFormat, toFormatWithFixes, isTypeModel,
  isNumberType,
  isDecimalType,
  numberLimits
} from './Reflection';

import EntityLink from './SearchControl/EntityLink';
import SearchControlLoaded, { SearchControlMobileOptions, ColumnParsed } from './SearchControl/SearchControlLoaded';
import { ImportComponent } from './ImportComponent'
import { ButtonBarElement } from "./TypeContext";
import { EntityBaseController, TypeContext } from "./Lines";
import { clearContextualItems } from "./SearchControl/ContextualItems";
import { clearManualSubTokens } from "./SearchControl/QueryTokenBuilder";
import { APIHookOptions, useAPI } from "./Hooks";
import { QueryString } from "./QueryString";
import { similarToken } from "./Search";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { BsSize } from "./Components";
import { CollectionMessage } from "./Signum.External";
import { QueryTokenMessage } from "./Signum.DynamicQuery.Tokens";
import { TextHighlighter } from "./Components/Typeahead";
import * as FinderRules from "./FinderRules";
import { Operations } from "./Operations";
import ProgressBar from "./Components/ProgressBar";
import Notify, { NotifyOptions } from "./Frames/Notify";
import Exception from "./Exceptions/Exception";


export namespace Finder {


  export const querySettings: { [queryKey: string]: QuerySettings } = {};

  export function clearQuerySettings(): void {
    Dic.clear(querySettings);
  }

  export function start(options: { routes: RouteObject[] }): void {
    options.routes.push({ path: "/find/:queryName", element: <ImportComponent onImport={() => Options.getSearchPage()} /> });
    AppContext.clearSettingsActions.push(clearContextualItems);
    AppContext.clearSettingsActions.push(clearQuerySettings);
    AppContext.clearSettingsActions.push(clearQueryDescriptionCache);
    AppContext.clearSettingsActions.push(clearManualSubTokens);
    AppContext.clearSettingsActions.push(ButtonBarQuery.clearButtonBarElements);
    AppContext.clearSettingsActions.push(resetFormatRules);
    onReloadTypesActions.push(clearQueryDescriptionCache);
  }

  export function addSettings(...settings: QuerySettings[]): void {
    settings.forEach(s => Dic.addOrThrow(querySettings, getQueryKey(s.queryName), s));
  }

  export function pinnedSearchFilter(): FilterGroupOption;
  export function pinnedSearchFilter<T extends Entity>(type: Type<T>, ...tokens: ((t: QueryTokenString<Anonymous<T>>) => (QueryTokenString<any> | FilterConditionOption))[]): FilterGroupOption;
  export function pinnedSearchFilter<T extends Entity>(type?: Type<T>, ...tokens: ((t: QueryTokenString<Anonymous<T>>) => (QueryTokenString<any> | FilterConditionOption))[]): FilterGroupOption {
    if (type == null) {
      return {
        groupOperation: "Or",
        pinned: { label: SearchMessage.Search.niceToString(), splitValue: true, active: "WhenHasValue" },
        filters: [
          { token: "Entity.Id", operation: "EqualTo" },
          { token: "Entity.ToString", operation: "Contains" },
        ]
      };
    }

    return {
      groupOperation: "Or",
      pinned: { splitValue: true },
      filters: tokens.map(t => {
        var res = t(type!.token());

        if (res instanceof QueryTokenString)
          return { token: res, operation: "Contains" } as FilterConditionOption;

        return res;
      })
    };
  }

  export function getSettings(queryName: PseudoType | QueryKey): QuerySettings | undefined {
    return querySettings[getQueryKey(queryName)];
  }

  export function getOrAddSettings(queryName: PseudoType | QueryKey): QuerySettings {
    return querySettings[getQueryKey(queryName)] ?? (querySettings[getQueryKey(queryName)] = { queryName: queryName });
  }

  export const isFindableEvent: Array<(queryKey: string, fullScreen: boolean, context?: Lite<Entity>) => boolean> = [];

  export function isFindable(queryName: PseudoType | QueryKey, fullScreen: boolean, context?: Lite<Entity>): boolean {

    if (!isQueryDefined(queryName))
      return false;

    const queryKey = getQueryKey(queryName);

    return isFindableEvent.every(f => f(queryKey, fullScreen, context));
  }

  export function find<T extends Entity = Entity>(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<Lite<T> | undefined>;
  export function find<T extends Entity>(type: Type<T>, modalOptions?: ModalFindOptions): Promise<Lite<T> | undefined>;
  export function find(obj: FindOptions | Type<any>, modalOptions?: ModalFindOptions): Promise<Lite<Entity> | undefined> {

    const fo = (obj as FindOptions).queryName ? obj as FindOptions :
      { queryName: obj as Type<any> } as FindOptions;

    if (fo.groupResults)
      throw new Error("Use findRow instead");

    var qs = getSettings(fo.queryName);
    if (qs?.onFind && !(modalOptions?.useDefaultBehaviour))
      return qs.onFind(fo, modalOptions);

    return defaultFind(fo, modalOptions);
  }

  export function defaultFind(fo: FindOptions, modalOptions?: ModalFindOptions): Promise<Lite<Entity> | undefined> {
    let getPromiseSearchModal: () => Promise<Lite<Entity> | undefined> = () => Options.getSearchModal()
      .then(a => a.default.open(fo, modalOptions))
      .then(a => a?.row.entity);

    if (modalOptions?.autoSelectIfOne || modalOptions?.autoSkipIfZero)
      return fetchLites({ queryName: fo.queryName, filterOptions: fo.filterOptions ?? [], orderOptions: fo.orderOptions ?? [], count: 2 })
        .then(data => {
          if (data.length == 1 && modalOptions?.autoSelectIfOne)
            return Promise.resolve(data[0]);

          if (data.length == 0 && modalOptions?.autoSkipIfZero)
            return Promise.resolve(undefined);

          return getPromiseSearchModal();
        });

    return getPromiseSearchModal();
  }

  export namespace Options {
    export function getSearchPage(): Promise<typeof import("./SearchControl/SearchPage")> {
      return import("./SearchControl/SearchPage");
    }
    export function getSearchModal(): Promise<typeof import("./SearchControl/SearchModal")> {
      return import("./SearchControl/SearchModal");
    }

    export let entityColumnHeader: () => React.ReactElement | string | null | undefined = () => "";

    export let tokenCanSetPropery = (qt: QueryToken): boolean =>
      qt.filterType == "Lite" && qt.key != "Entity" ||
      qt.filterType == "Enum" && !isState(qt.type) ||
      qt.filterType == "DateTime" && qt.propertyRoute != null && PropertyRoute.tryParseFull(qt.propertyRoute)?.member?.type.name == "DateOnly";

    export let isState = (ti: TypeReference): boolean => ti.name.endsWith("State");

    export let defaultPagination: Pagination = {
      mode: "Paginate",
      elementsPerPage: 20,
      currentPage: 1,
    };

  }

  export function findRow(fo: FindOptions, modalOptions?: ModalFindOptions): Promise<{ row: ResultRow, searchControl: SearchControlLoaded } | undefined> {

    var qs = getSettings(fo.queryName);

    return Options.getSearchModal()
      .then(a => a.default.open(fo, modalOptions));
  }


  export function findMany<T extends Entity>(findOptions: FindOptions, modalOptions?: ModalFindOptionsMany): Promise<Lite<T>[] | undefined>;
  export function findMany<T extends Entity>(type: Type<T>, modalOptions?: ModalFindOptionsMany): Promise<Lite<T>[] | undefined>;
  export function findMany(findOptions: FindOptions | Type<any>, modalOptions?: ModalFindOptionsMany): Promise<Lite<Entity>[] | undefined> {

    const fo = (findOptions as FindOptions).queryName ? findOptions as FindOptions :
      { queryName: findOptions as Type<any> } as FindOptions;


    var qs = getSettings(fo.queryName);
    if (qs?.onFindMany && !(modalOptions?.useDefaultBehaviour))
      return qs.onFindMany(fo, modalOptions);

    return defaultFindMany(fo, modalOptions);
  }

  export function defaultFindMany(fo: FindOptions, modalOptions?: ModalFindOptionsMany): Promise<Lite<Entity>[] | undefined> {
    let getPromiseSearchModal: () => Promise<Lite<Entity>[] | undefined> = () => Options.getSearchModal()
      .then(SearchModal => SearchModal.default.openMany(fo, modalOptions))
      .then(pair => {
        if (!pair)
          return undefined;

        const sc = pair.searchControl!;

        if (sc.props.findOptions.groupResults)
          return sc.getGroupedSelectedEntities();

        return pair.rows.map(a => a.entity!);
      });

    if (modalOptions?.autoSelectIfOne || modalOptions?.autoSkipIfZero)
      return fetchLites({ queryName: fo.queryName, filterOptions: fo.filterOptions || [], orderOptions: fo.orderOptions || [], count: 2 })
        .then(data => {
          if (data.length == 1 && modalOptions?.autoSelectIfOne)
            return Promise.resolve(data);

          if (data.length == 0 && modalOptions?.autoSkipIfZero)
            return Promise.resolve(data);

          return getPromiseSearchModal();
        });

    return getPromiseSearchModal();
  }

  export function findManyRows(fo: FindOptions, modalOptions?: ModalFindOptionsMany): Promise<{ rows: ResultRow[], searchControl: SearchControlLoaded } | undefined> {

    var qs = getSettings(fo.queryName);

    return Options.getSearchModal()
      .then(a => a.default.openMany(fo, modalOptions));
  }

  export function exploreWindowsOpen(findOptions: FindOptions, e: React.MouseEvent<any>): void {
    e.preventDefault();
    if (e.ctrlKey || e.button == 1)
      window.open(AppContext.toAbsoluteUrl(findOptionsPath(findOptions)));
    else
      explore(findOptions);
  }

  export function explore(findOptions: FindOptions, modalOptions?: ModalFindOptions): Promise<void> {

    var qs = getSettings(findOptions.queryName);
    if (qs?.onExplore && !(modalOptions?.useDefaultBehaviour))
      return qs.onExplore(findOptions, modalOptions);

    return Options.getSearchModal()
      .then(a => a.default.explore(findOptions, modalOptions));
  }

  export function findOptionsPath(fo: FindOptions, extra?: any): string {

    const query = findOptionsPathQuery(fo, extra);
    var strQuery = QueryString.stringify(query);

    return "/find/" + getQueryKey(fo.queryName) + (strQuery ? ("?" + strQuery) : "");
  }

  export function findOptionsPathQuery(fo: FindOptions, extra?: any): any {
    fo = autoRemoveTrivialColumns(fo);

    const query = {
      groupResults: fo.groupResults || undefined,
      idf: fo.includeDefaultFilters,
      columnMode: (!fo.columnOptionsMode || fo.columnOptionsMode == "Add" as ColumnOptionsMode) ? undefined : fo.columnOptionsMode,
      paginationMode: fo.pagination && fo.pagination.mode,
      elementsPerPage: fo.pagination && fo.pagination.elementsPerPage,
      currentPage: fo.pagination && fo.pagination.currentPage,
      systemTimeMode: fo.systemTime?.mode,
      systemTimeJoinMode: fo.systemTime?.joinMode,
      systemTimeStartDate: fo.systemTime?.startDate,
      systemTimeEndDate: fo.systemTime?.endDate,
      timeSeriesStep: fo.systemTime?.timeSeriesStep,
      timeSeriesUnit: fo.systemTime?.timeSeriesUnit,
      timeSeriesMaxRowsPerStep: fo.systemTime?.timeSeriesMaxRowsPerStep,
      splitQueries: fo.systemTime?.splitQueries,
      ...extra
    };

    Encoder.encodeFilters(query, fo.filterOptions?.notNull());
    Encoder.encodeOrders(query, fo.orderOptions?.notNull());
    Encoder.encodeColumns(query, fo.columnOptions?.notNull());

    return query;
  }

  export function getTypeNiceName(tr: TypeReference): string {

    const niceName = tr.typeNiceName ??
      tryGetTypeInfos(tr)
        .map(ti => ti == undefined ? getSimpleTypeNiceName(tr.name) : (ti.niceName ?? ti.name))
        .joinComma(CollectionMessage.Or.niceToString());

    return tr.isCollection ? QueryTokenMessage.ListOf0.niceToString(niceName) : niceName;
  }

  export function getSimpleTypeNiceName(name: string): string {

    if (isDecimalType(name))
      return QueryTokenMessage.DecimalNumber.niceToString();

    if (isNumberType(name))
      return QueryTokenMessage.Number.niceToString();

    switch (name) {
      case "string":
      case "Guid":
        return QueryTokenMessage.Text.niceToString();
      case "Date": return QueryTokenMessage.Date.niceToString();
      case "DateTime": return QueryTokenMessage.DateTime.niceToString();
      case "DateTimeOffset": return QueryTokenMessage.DateTimeOffset.niceToString();
      case "boolean": return QueryTokenMessage.Check.niceToString();
    }

    return name;
  }


  export function parseFindOptionsPath(queryName: PseudoType | QueryKey, query: any): FindOptions {

    const result: FindOptions = {
      queryName: queryName,
      groupResults: parseBoolean(query.groupResults),
      includeDefaultFilters: parseBoolean(query.idf),
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
        joinMode: query.systemTimeJoinMode,
        startDate: query.systemTimeStartDate,
        endDate: query.systemTimeEndDate,
        timeSeriesUnit: query.timeSeriesUnit,
        timeSeriesStep: query.timeSeriesStep && parseInt(query.timeSeriesStep),
        timeSeriesMaxRowsPerStep: query.timeSeriesMaxRowsPerStep && parseInt(query.timeSeriesMaxRowsPerStep),
        splitQueries: Boolean(query.splitQueries),
      }
    };

    return Dic.simplify(result)!;
  }

  export function getDefaultColumns(qd: QueryDescription): QueryToken[] {
    return Dic.getValues(qd.columns)
      .filter(a => a.fullKey != "Entity" && a.queryTokenType != "Aggregate" && a.queryTokenType != "TimeSeries");

  }

  export function mergeColumns(qd: QueryDescription, mode: ColumnOptionsMode, columnOptions: ColumnOption[]): ColumnOption[] {

    var columns = getDefaultColumns(qd);

    switch (mode) {
      case "Add":
        return columns.map(cd => softCast<ColumnOption>({ token: cd.fullKey }))
          .concat(columnOptions);

      case "InsertStart":
        return columnOptions
          .concat(columns.map(cd => softCast<ColumnOption>({ token: cd.fullKey })));

      case "Remove":
        return columns.filter(cd => !columnOptions.some(a => a.token == cd.fullKey))
          .map(cd => softCast<ColumnOption>({ token: cd.fullKey }));

      case "ReplaceAll":
        return columnOptions;

      case "ReplaceOrAdd": {
        var original = columns.map(cd => softCast<ColumnOption>({ token: cd.fullKey }));
        columnOptions.forEach(toReplaceOrAdd => {
          var index = original.findIndex(co => co.token.toString() == toReplaceOrAdd.token.toString());
          if (index != -1)
            original[index] = toReplaceOrAdd;
          else
            original.push(toReplaceOrAdd);
        });
        return original;
      }
      default: throw new Error("Unexpected column mode");
    }
  }

  export function smartColumns(current: ColumnOptionParsed[], qd: QueryDescription): { mode: ColumnOptionsMode; columns: ColumnOption[] } {

    const similar = (c: ColumnOptionParsed, d: QueryToken) =>
      c.token!.fullKey == d.fullKey && (c.displayName == d.niceName) && c.summaryToken == null && c.combineRows == null && !c.hiddenColumn;

    const toColumnOption = (c: ColumnOptionParsed) => ({
      token: c.token!.fullKey,
      displayName: c.token!.niceName == c.displayName ? undefined : c.displayName,
      summaryToken: c.summaryToken?.fullKey,
      combineRows: c.combineRows,
      hiddenColumn: c.hiddenColumn,
    }) as ColumnOption;

    var ideal = Finder.getDefaultColumns(qd);

    current = current.filter(a => a.token != null);

    if (ideal.every((idl, i) => i < current.length && similar(current[i], idl))) {
      return {
        mode: "Add",
        columns: current.slice(ideal.length).map(c => toColumnOption(c))
      };
    }

    if (ideal.every((idl, i) => i < current.length && current[i].token!.fullKey == idl.fullKey)) {

      var replacements = current.filter((curr, i) => i < ideal.length && !similar(curr, ideal[i])).map(c => toColumnOption(c));
      var additions = current.slice(ideal.length).map(c => toColumnOption(c));

      return {
        mode: "ReplaceOrAdd",
        columns: [...replacements, ...additions]
      };
    }

    if (current.length < ideal.length) {
      const toRemove: ColumnOption[] = [];

      let j = 0;
      for (let i = 0; i < ideal.length; i++) {
        if (j < current.length && similar(current[j], ideal[i]))
          j++;
        else
          toRemove.push({ token: ideal[i].fullKey, });
      }

      if (toRemove.length + current.length == ideal.length && toRemove.length < current.length) {
        return {
          mode: "Remove",
          columns: toRemove
        };
      }
    }

    return {
      mode: "ReplaceAll",
      columns: current.map(c => toColumnOption(c)),
    };
  }

  function parseBoolean(value: any): boolean | undefined {
    if (value === "true" || value === true)
      return true;

    if (value === "false" || value === false)
      return false;

    return undefined;
  }

  export function parseFilterOptions(fos: (FilterOption | null | undefined)[], groupResults: boolean, qd: QueryDescription): Promise<FilterOptionParsed[]> {

    const completer = new TokenCompleter(qd);
    var sto = SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | (groupResults ? SubTokensOptions.CanAggregate : 0);

    fos.notNull().forEach(fo => completer.requestFilter(fo));

    return completer.finished()
      .then(() => fos.notNull().map(fo => completer.toFilterOptionParsed(fo, sto)))
      .then(filters => parseFilterValues(filters).then(() => filters));
  }



  export function parseOrderOptions(orderOptions: (OrderOption | null | undefined)[], groupResults: boolean, qd: QueryDescription): Promise<OrderOptionParsed[]> {

    const completer = new TokenCompleter(qd);
    var sto = SubTokensOptions.CanElement | SubTokensOptions.CanSnippet | (groupResults ? SubTokensOptions.CanAggregate : 0);
    orderOptions.notNull().forEach(a => completer.request(a.token.toString()));

    return completer.finished()
      .then(() => orderOptions.notNull().map(oo => ({
        token: completer.get(oo.token.toString(), sto),
        orderType: oo.orderType ?? "Ascending",
      }) as OrderOptionParsed));
  }

  export function parseColumnOptions(columnOptions: ColumnOption[], groupResults: boolean, qd: QueryDescription): Promise<ColumnOptionParsed[]> {

    const completer = new TokenCompleter(qd);
    var sto = SubTokensOptions.CanElement | SubTokensOptions.CanToArray | SubTokensOptions.CanSnippet | (groupResults ? SubTokensOptions.CanAggregate : SubTokensOptions.CanOperation | SubTokensOptions.CanManual);
    columnOptions.forEach(a => completer.request(a.token.toString()));
    columnOptions.filter(a => a.summaryToken != null).forEach(a => completer.request(a.summaryToken!.toString()));

    return completer.finished()
      .then(() => columnOptions.map(co => {

        const token = completer.get(co.token.toString(), sto);

        return ({
          token: token,
          displayName: (typeof co.displayName == "function" ? co.displayName() : co.displayName) ?? token.niceName,
          summaryToken: co.summaryToken && completer.get(co.summaryToken.toString(), SubTokensOptions.CanAggregate),
          combineEquals: co.combineRows,
          hiddenColumn: co.hiddenColumn,
        }) as ColumnOptionParsed
      }));
  }



  export async function getPropsFromFilters(type: PseudoType, filterOptionsParsed: FilterOptionParsed[], options? : { avoidCustom?: boolean}): Promise<any> {

    const ti = getTypeInfo(type);
    if (!(options?.avoidCustom) && querySettings[ti.name]?.customGetPropsFromFilter) {
      return querySettings[ti.name].customGetPropsFromFilter!([...filterOptionsParsed]);
    }

    var result = getPropsFromFiltersSync(type, filterOptionsParsed, true);

    var promiseMembers = Object.entries(result).filter(([key, value]) => isPromise(value));

    await Promise.all(promiseMembers.map(async ([key, value]) => result[key] = await value));

    return result;
  }

  export function getPropsFromFiltersSync(type: PseudoType, filterOptionsParsed: FilterOptionParsed[], retrieveEntityInPromise: boolean = false): any {

    const ti = getTypeInfo(type);

    function getMemberForToken(ti: TypeInfo, fullKey: string) {
      var token = fullKey.tryAfter("Entity.") ?? fullKey;

      if (token.contains("."))
        return null;

      return ti.members[token];
    }

    const result: any = {};

    filterOptionsParsed.forEach(fo => {

      if (isFilterGroup(fo) ||
        fo.token == null ||
        !Options.tokenCanSetPropery(fo.token) ||
        fo.operation != "EqualTo" ||
        !isActive(fo))
        return;

      const mi = getMemberForToken(ti, fo.token!.fullKey);

      if (!mi)
        return;

      const valueOrPromise = tryConvert(fo.value, mi.type, retrieveEntityInPromise);

      result[mi.name.firstLower()] = valueOrPromise;
    });

    return result;
  }

  export function tryConvert(value: any, type: TypeReference, retrieveEntityInPromise: boolean): any | undefined {

    if (value == null)
      return null;

    if (type.isLite) {

      if (isLite(value))
        return value;

      if (isEntity(value))
        return toLite(value);

      return undefined;
    }

    const ti = tryGetTypeInfo(type.name);

    if (ti?.kind == "Entity") {

      if (isLite(value))
        return retrieveEntityInPromise ? Navigator.API.fetch(value) : value;

      if (isEntity(value))
        return value;

      return undefined;
    }

    if (type.name == "string" || type.name == "Guid" || type.name == "DateOnly" || ti?.kind == "Enum") {
      if (typeof value === "string")
        return value;

      return undefined;
    }

    if (type.name == "boolean") {
      if (typeof value === "boolean")
        return value;
    }

    if (isNumberType(type.name)) {
      if (typeof value === "number")
        return value;
    }

    return undefined;
  }


  export function getPropsFromFindOptions(type: PseudoType, fo: FindOptions | undefined): Promise<any> {
    if (fo == null)
      return Promise.resolve(undefined);

    return getQueryDescription(fo.queryName)
      .then(qd => parseFindOptions(fo, qd, true))
      .then(fop => getPropsFromFilters(type, fop.filterOptions));
  }

  export function toFindOptions(fo: FindOptionsParsed, qd: QueryDescription, defaultIncludeDefaultFilters: boolean): FindOptions {

    const pair = smartColumns(fo.columnOptions, qd);

    const qs = getSettings(fo.queryKey);

    const defPagination = qs?.pagination ?? Options.defaultPagination;

    function equalsPagination(p1: Pagination, p2: Pagination) {
      return p1.mode == p2.mode && p1.elementsPerPage == p2.elementsPerPage && p1.currentPage == p2.currentPage;
    }

    var findOptions = {
      queryName: fo.queryKey,
      groupResults: fo.groupResults ? true : undefined,
      filterOptions: toFilterOptions(fo.filterOptions),
      orderOptions: fo.orderOptions.filter(a => !!a.token).map(o => ({ token: o.token.fullKey, orderType: o.orderType }) as OrderOption),
      columnOptions: pair.columns,
      columnOptionsMode: pair.mode,
      pagination: fo.pagination && !equalsPagination(fo.pagination, defPagination) ? fo.pagination : undefined,
      systemTime: fo.systemTime,
    } as FindOptions;

    if (!findOptions.groupResults && findOptions.orderOptions) {
      var defaultOrder = getDefaultOrder(qd, qs);

      if (equalOrders(defaultOrder, findOptions.orderOptions.notNull()))
        findOptions.orderOptions = undefined;
    }

    if (findOptions.filterOptions) {
      var defaultFilters = getDefaultFilter(qd, qs);
      var filterOptions = findOptions.filterOptions.notNull();
      if (defaultFilters && defaultFilters.length <= filterOptions.length) {
        if (equalFilters(defaultFilters, filterOptions.slice(0, defaultFilters.length))) {
          findOptions.filterOptions = filterOptions.slice(defaultFilters.length);
          findOptions.includeDefaultFilters = true;
        }
      }
    }
    if (!findOptions.includeDefaultFilters)
      findOptions.includeDefaultFilters = false;

    if (findOptions.includeDefaultFilters == defaultIncludeDefaultFilters)
      delete findOptions.includeDefaultFilters;

    return findOptions;
  }

  function equalOrders(as: OrderOption[] | undefined, bs: OrderOption[] | undefined): boolean {
    if (as == undefined && bs == undefined)
      return true;

    if (as == undefined || bs == undefined)
      return false;

    return as.length == bs.length && as.every((a, i) => {
      var b = bs![i];

      return (a.token && a.token.toString()) == (b.token && b.token.toString()) &&
        a.orderType == b.orderType;
    });
  }

  function equalFilters(as: FilterOption[] | undefined, bs: FilterOption[] | undefined): boolean {

    if (as == undefined && bs == undefined)
      return true;

    if (as == undefined || bs == undefined)
      return false;

    return as.length == bs.length && as.every((a, i) => {
      var b = bs![i];

      return (a.token && a.token.toString()) == (b.token && b.token.toString()) &&
        (a as FilterGroupOption).groupOperation == (b as FilterGroupOption).groupOperation &&
        ((a as FilterConditionOption).operation ?? "EqualTo") == ((b as FilterConditionOption).operation ?? "EqualTo") &&
        (a.value == b.value || is(a.value, b.value, false, false)) &&
        Dic.equals(a.pinned, b.pinned, true) &&
        equalFilters((a as FilterGroupOption).filters?.notNull(), (b as FilterGroupOption).filters?.notNull());
    });
  }

  export const defaultOrderColumn: string = "Id";

  export function getDefaultOrder(qd: QueryDescription, qs: QuerySettings | undefined): OrderOption[] | undefined {
    if (qs?.defaultOrders)
      return qs.defaultOrders;

    const tis = getTypeInfos(qd.columns["Entity"].type);

    if (!qd.columns[defaultOrderColumn])
      return undefined;

    return [{
      token: defaultOrderColumn,
      orderType: tis.some(a => a.entityData == "Transactional") ? "Descending" : "Ascending"
    }];
  }

  export function getDefaultFilter(qd: QueryDescription | undefined, qs: QuerySettings | undefined): FilterOption[] | undefined {
    if (qs?.defaultFilters)
      return qs.defaultFilters;

    if (qs?.simpleFilterBuilder)
      return undefined;

    if (qd == null || qd.columns["Entity"]) {
      return [
        {
          groupOperation: "Or",
          pinned: { label: SearchMessage.Search.niceToString(), splitValue: true, active: "WhenHasValue" },
          filters: [
            { token: "Entity.ToString", operation: "Contains" },
            { token: "Entity.Id", operation: "EqualTo" },
          ]
        }
      ];
    }
    else {
      return undefined;
    }
  }

  export function isAggregate(fop: FilterOptionParsed): boolean {
    if (isFilterGroup(fop))
      return fop.filters.some(f => isAggregate(f));

    return fop.token != null && fop.token.queryTokenType == "Aggregate";
  }

  export function toFilterOptions(filterOptionsParsed: FilterOptionParsed[]): FilterOption[] {

    function toFilterOption(fop: FilterOptionParsed): FilterOption | null {

      var pinned = fop.pinned && Dic.simplify({ ...fop.pinned }) as PinnedFilter;
      if (isFilterGroup(fop))
        return ({
          token: fop.token && fop.token.fullKey,
          groupOperation: fop.groupOperation,
          value: fop.value === "" ? undefined : fop.value,
          pinned: pinned,
          dashboardBehaviour: fop.dashboardBehaviour,
          filters: fop.filters.map(fp => toFilterOption(fp)).filter(fo => !!fo),
        }) as FilterGroupOption;
      else {
        if (fop.token == null)
          return null;

        return ({
          token: fop.token && fop.token.fullKey,
          operation: fop.operation,
          value: fop.value === "" ? undefined : fop.value,
          frozen: fop.frozen ? true : undefined,
          pinned: pinned,
          dashboardBehaviour: fop.dashboardBehaviour,
        }) as FilterConditionOption;
      }
    }

    return filterOptionsParsed.map(fop => toFilterOption(fop)).filter(fo => fo != null) as FilterOption[];
  }

  export function parseFindOptions(findOptions: FindOptions, qd: QueryDescription, defaultIncludeDefaultFilters: boolean): Promise<FindOptionsParsed> {
    const fo = autoRemoveTrivialColumns(findOptions);

    fo.columnOptions = mergeColumns(qd, fo.columnOptionsMode ?? "Add", fo.columnOptions?.notNull() ?? []);

    var qs: QuerySettings | undefined = querySettings[qd.queryKey];
    const tis = tryGetTypeInfos(qd.columns["Entity"].type);

    if (!fo.groupResults && (!fo.orderOptions || fo.orderOptions.length == 0)) {
      var defaultOrder = getDefaultOrder(qd, qs);

      if (defaultOrder)
        fo.orderOptions = defaultOrder;
    }

    if (fo.includeDefaultFilters == null ? defaultIncludeDefaultFilters : fo.includeDefaultFilters) {
      var defaultFilters = getDefaultFilter(qd, qs);
      if (defaultFilters)
        fo.filterOptions = [...defaultFilters, ...fo.filterOptions ?? []];
    }

    if (fo.filterOptions)
      fo.filterOptions = simplifyPinnedFilters(fo.filterOptions.notNull());

    const canAggregate = (findOptions.groupResults ? SubTokensOptions.CanAggregate : 0);
    const canAggregateXorOperation = (canAggregate != 0 ? canAggregate : SubTokensOptions.CanOperation | SubTokensOptions.CanManual);
    const canTimeSeries = (fo.systemTime?.mode == QueryTokenString.timeSeries.token ? SubTokensOptions.CanTimeSeries : 0);

    const completer = new TokenCompleter(qd);


    if (fo.filterOptions)
      fo.filterOptions.notNull().forEach(fo => completer.requestFilter(fo));

    if (fo.orderOptions)
      fo.orderOptions.notNull().forEach(oo => completer.request(oo.token.toString()));

    if (fo.columnOptions) {
      fo.columnOptions.notNull().forEach(co => completer.request(co.token.toString()));
      fo.columnOptions.notNull().filter(a => a.summaryToken).forEach(co => completer.request(co.summaryToken!.toString()));
    }

    return completer.finished().then(() => {

      var result: FindOptionsParsed = {
        queryKey: qd.queryKey,
        groupResults: fo.groupResults == true,
        pagination: fixPagination(fo.pagination != null ? fo.pagination : qs?.pagination ?? Options.defaultPagination),
        systemTime: fo.systemTime && fixSystemTime(fo.systemTime),

        columnOptions: (fo.columnOptions?.notNull() ?? []).map(co => {

          const token = completer.get(co.token.toString(), SubTokensOptions.CanElement | SubTokensOptions.CanToArray | SubTokensOptions.CanSnippet | canAggregateXorOperation | canTimeSeries);

          return softCast<ColumnOptionParsed>({
            token: token,
            displayName: (typeof co.displayName == "function" ? co.displayName() : co.displayName) ?? token.niceName,
            summaryToken: co.summaryToken ? completer.get(co.summaryToken.toString(), SubTokensOptions.CanElement | SubTokensOptions.CanAggregate) : undefined,
            hiddenColumn: co.hiddenColumn,
            combineRows: co.combineRows,
          });
        }),

        orderOptions: (fo.orderOptions?.notNull() ?? []).map(oo => ({
          token: completer.get(oo.token.toString(), SubTokensOptions.CanElement | SubTokensOptions.CanSnippet | canAggregate | canTimeSeries),
          orderType: oo.orderType,
        }) as OrderOptionParsed),

        filterOptions: (fo.filterOptions?.notNull() ?? []).map(fo => completer.toFilterOptionParsed(fo, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | canAggregate | canTimeSeries)),
      };

      return parseFilterValues(result.filterOptions)
        .then(() => result)
    });
  }

  function fixPagination(p: Pagination): Pagination {
    return {
      mode: p.mode,
      elementsPerPage: p.mode == "All" ? undefined : p.elementsPerPage == null || p.elementsPerPage < 0 ? 20 : p.elementsPerPage,
      currentPage: p.mode != "Paginate" ? undefined : p.currentPage == null || p.currentPage < 0 ? 1 : p.currentPage,
    };
  }

  function fixSystemTime(p: SystemTime): SystemTime {
    return {
      ...p
    };
  }

  function simplifyPinnedFilters(fos: FilterOption[]): FilterOption[] {

    const toRemove: FilterOption[] = [];
    const result = fos.map(fo => {
      if (fo.pinned != null &&
        (fo.pinned?.active == "Always" || fo.pinned?.active == "WhenHasValue") &&
        !isFilterGroup(fo)) {

        var fo2 = fos.firstOrNull(fo2 =>
          fo2.pinned == null &&
          !isFilterGroup(fo2) &&
          similarToken(fo.token?.toString(), fo2.token?.toString()) &&
          (fo.operation ?? FilterOperation.value("EqualTo")) == (fo2.operation ?? FilterOperation.value("EqualTo")) &&
          (fo.pinned?.active == "Always" || fo2.value != null));

        if (fo2 != null) {
          toRemove.push(fo2);
          return { ...fo, value: fo2.value } as FilterConditionOption;
        }
      }
      return fo;

    });

    return result.filter(fo => fo && !toRemove.contains(fo));
  }

  export function getQueryRequest(fo: FindOptionsParsed, qs?: QuerySettings, avoidHiddenColumns?: boolean): QueryRequest {

    return {
      queryKey: fo.queryKey,
      groupResults: fo.groupResults,
      filters: toFilterRequests(fo.filterOptions),
      columns: fo.columnOptions.filter(a => a.token != undefined).map(co => ({ token: co.token!.fullKey, displayName: co.displayName! }))
        .concat((!fo.groupResults && !avoidHiddenColumns && qs?.hiddenColumns || []).map(co => ({ token: co.token.toString(), displayName: "" }))),
      orders: fo.orderOptions.filter(a => a.token != undefined).map(oo => ({ token: oo.token.fullKey, orderType: oo.orderType })),
      pagination: fo.pagination,
      systemTime: fo.systemTime,
    };
  }

  export function getSummaryQueryRequest(fo: FindOptionsParsed): QueryRequest | null {

    var summaryTokens = fo.columnOptions.filter(a => a.summaryToken != undefined).map(a => a.summaryToken!)
      .filter(a => a.queryTokenType == "Aggregate");

    if (summaryTokens.length == 0)
      return null;

    return {
      queryKey: fo.queryKey,
      groupResults: true,
      filters: toFilterRequests(fo.filterOptions),
      columns: summaryTokens.map(sqt => ({ token: sqt.fullKey, displayName: sqt.niceName! })),
      orders: [],
      pagination: { mode: "All" }, //Should be 1 result anyway
      systemTime: fo.systemTime,
    };
  }

  export function validateNewEntities(fo: FindOptions): string | undefined {

    function getValues(fo: FilterOption): any[] {
      if (isFilterGroup(fo))
        return fo.filters.notNull().flatMap(f => getValues(f));

      return [fo.value];
    }

    var allValues = (fo.filterOptions?.notNull() ?? []).flatMap(fo => getValues(fo));

    var allNewTypes = allValues.flatMap(a => getTypeIfNew(a));

    if (allNewTypes.length == 0)
      return undefined;

    return `Filtering by new ${allNewTypes.joinComma(" and ")}. Consider hiding the control for new entities.`;
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



  export function exploreOrView(findOptions: FindOptions): Promise<void> {
    return fetchLites({ queryName: findOptions.queryName, filterOptions: findOptions.filterOptions ?? [], orderOptions: [], count: 2 }).then(list => {
      if (list.length == 1)
        return Navigator.view(list[0], { buttons: "close" }).then(() => undefined);
      else
        return explore(findOptions);
    });
  }

  export function useQueryValue<T = number>(queryName: PseudoType | QueryKey | null, filterOptions: (FilterOption | null | undefined)[], valueToken?: QueryTokenString<T> | string, multipleValues?: boolean, extraDeps?: React.DependencyList): T | null | undefined {

    var query = {};
    Encoder.encodeFilters(filterOptions);

    return useAPI(() => !queryName ? null : getQueryValue(queryName, filterOptions, valueToken, multipleValues),
      [
        queryName && getQueryKey(queryName),
        QueryString.stringify(query),
        valueToken?.toString(),
        multipleValues,
        ...(extraDeps ?? [])
      ]);
  }

  export function getQueryValue<T = number>(queryName: PseudoType | QueryKey, filterOptions: (FilterOption | null | undefined)[], valueToken?: QueryTokenString<T> | string, multipleValues?: boolean): Promise<T> {
    return getQueryDescription(queryName).then(qd => {
      return parseFilterOptions(filterOptions ?? [], false, qd).then(fops => {

        let filters = toFilterRequests(fops);

        return API.queryValue({ queryKey: qd.queryKey, filters, valueToken: valueToken?.toString(), multipleValues });
      });
    });
  }

  export function toFilterRequests(fops: FilterOptionParsed[], overridenValue?: OverridenValue): FilterRequest[] {
    return fops.map(fop => toFilterRequest(fop, overridenValue)).filter(a => a != null) as FilterRequest[];
  }

  interface OverridenValue {
    value: any;
  }


  export function toFilterRequest(fop: FilterOptionParsed, overridenValue?: OverridenValue): FilterRequest | undefined {

    if (fop.pinned && (fop.pinned.active == "Checkbox_Unchecked" || fop.pinned.active == "NotCheckbox_Checked"))
      return undefined;

    if (fop.dashboardBehaviour == "UseAsInitialSelection")
      return undefined;

    const operation = (fop as FilterConditionOptionParsed).operation;

    if (fop.pinned && overridenValue == null) {
      if (fop.pinned.splitValue) {

        if (!fop.value)
          return undefined;

        if (!canSplitValue(fop))
          throw new Error("Split text only works with string");


        if (typeof fop.value == "string") {
          const parts = fop.value.split(/\s+/);

          return ({
            groupOperation: "And",
            token: fop.token?.fullKey,
            filters: parts.filter(a => a.length > 0).map(part => toFilterRequest(fop, { value: part })),
          }) as FilterGroupRequest;
        }
        if (operation && isList(operation)) {

          const parts = (fop.value as unknown[]);

          var newOperation: FilterOperation = operation == "IsIn" ? "EqualTo" : "DistinctTo";

          return ({
            groupOperation: "And",
            //token: fop.token?.fullKey,
            filters: parts.map(part => toFilterRequest({ ...fop, operation: newOperation }, { value: part })),
          }) as FilterGroupRequest;
        }
      }
      else if (isFilterGroup(fop)) {

        if (fop.pinned.active == "WhenHasValue" && (fop.value == null || fop.value == "")) {
          return undefined;
        }

        if (fop.pinned.active == "Checkbox_Checked") {

        } else {
          return toFilterRequest(fop, { value: fop.value });
        }

      }
    }

    if (isFilterGroup(fop))
      return ({
        groupOperation: fop.groupOperation,
        token: fop.token && fop.token.fullKey,
        filters: toFilterRequests(fop.filters, overridenValue)
      } as FilterGroupRequest);
    else {
      if (fop.token == null || fop.token.filterType == null || fop.operation == null)
        return undefined;

      if (overridenValue == null && fop.pinned && fop.pinned.active == "WhenHasValue" && (fop.value == null || fop.value === ""))
        return undefined;

      const value = overridenValue ? overridenValue.value : fop.value;

      if (fop.token && typeof value == "string") {
        if (isNumberType(fop.token.type.name)) {

          const numVal = parseInt(value);

          const limits = numberLimits[fop.token.type.name]

          if (isNaN(numVal) || numVal < limits.min || limits.max < numVal) {
            if (overridenValue)
              return undefined;

            return ({
              token: fop.token.fullKey,
              operation: fop.operation,
              value: undefined,
            } as FilterConditionRequest);
          }

          return ({
            token: fop.token.fullKey,
            operation: fop.operation,
            value: numVal,
          } as FilterConditionRequest);
        }

        if (fop.token.type.name == "Guid") {
          if (!isValidGuid(value)) {
            if (overridenValue)
              return undefined;

            return ({
              token: fop.token.fullKey,
              operation: fop.operation,
              value: undefined,
            } as FilterConditionRequest);
          }

          return ({
            token: fop.token.fullKey,
            operation: fop.operation,
            value: value,
          } as FilterConditionRequest);
        }
      }

      if (Array.isArray(value)) {
        return ({
          token: fop.token.fullKey,
          operation: fop.operation,
          value: value.notNull(),
        } as FilterConditionRequest);
      }

      return ({
        token: fop.token.fullKey,
        operation: fop.operation,
        value: value,
      } as FilterConditionRequest);
    }
  }


  function isValidGuid(str: string) {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(str);
  }

  export async function fetchLites<T extends Entity>(fo: FetchEntitiesOptions<T>): Promise<Lite<T>[]> {

    var qd = await getQueryDescription(fo.queryName);
    var filters = await parseFilterOptions(fo.filterOptions ?? [], false, qd);
    var orders = await parseOrderOptions(fo.orderOptions ?? [], false, qd);

    var result = await API.fetchLites({

      queryKey: qd.queryKey,

      filters: toFilterRequests(filters),

      orders: orders.map(oo => ({
        token: oo.token!.fullKey,
        orderType: oo.orderType
      }) as OrderRequest),

      count: fo.count ?? null
    });

    return result as Lite<T>[];
  }

  export async function fetchEntities<T extends Entity>(fo: FetchEntitiesOptions<T>): Promise<T[]> {
    const qd = await getQueryDescription(fo.queryName);
    const filters = await parseFilterOptions(fo.filterOptions ?? [], false, qd);
    const orders = await parseOrderOptions(fo.orderOptions ?? [], false, qd);

    const entities = await API.fetchEntities({

      queryKey: qd.queryKey,

      filters: toFilterRequests(filters),

      orders: orders.map(oo => ({
        token: oo.token!.fullKey,
        orderType: oo.orderType
      }) as OrderRequest),

      count: fo.count ?? null,
    });

    return entities as T[];
  }

  export function defaultNoColumnsAllRows(fo: FindOptions, count: number | undefined): FindOptions {

    const newFO = { ...fo };

    if (newFO.columnOptions == undefined && newFO.columnOptionsMode == undefined) {

      newFO.columnOptions = [];
      newFO.columnOptionsMode = "ReplaceAll";
    }

    if (newFO.pagination == undefined) {
      newFO.pagination = count == undefined ? { mode: "All" } : { mode: "Firsts", elementsPerPage: count };
    }

    return newFO;
  }

  export function autoRemoveTrivialColumns(fo: FindOptions): FindOptions {

    var newFO = { ...fo };

    if (newFO.columnOptions == undefined && newFO.columnOptionsMode == undefined && newFO.filterOptions) {
      var trivialColumns = getTrivialColumns(newFO.filterOptions.notNull());

      if (trivialColumns.length) {
        newFO.columnOptions = trivialColumns;
        newFO.columnOptionsMode = "Remove";
      }
    }

    return newFO;
  }

  export function getTrivialColumns(fos: FilterOption[]): ColumnOption[] {
    return fos
      .filter(fo => !isFilterGroup(fo) && (fo.operation == null || fo.operation == "EqualTo") && !fo.token.toString().contains(".") && fo.pinned == null && fo.value != null)
      .map(fo => ({ token: fo.token }) as ColumnOption);
  }
  export async function parseSingleToken(queryName: PseudoType | QueryKey, token: string | QueryTokenString<any>, subTokenOptions: SubTokensOptions): Promise<QueryToken> {

    var qd = await getQueryDescription(getQueryKey(queryName));
    const completer = new TokenCompleter(qd);
    const result = completer.request(token.toString());
    await completer.finished();
    return completer.get(token.toString(), subTokenOptions);
  }

  export async function parseTokens(queryName: PseudoType | QueryKey, tokens: (string | QueryTokenString<any>)[], subTokenOptions: SubTokensOptions): Promise<QueryToken[]> {
    var qd = await getQueryDescription(getQueryKey(queryName));
    const completer = new TokenCompleter(qd);
    tokens.forEach(token => completer.request(token.toString()));
    await completer.finished();
    return tokens.map(token => completer.get(token.toString(), subTokenOptions));
  }

  export class TokenCompleter {


    static globalCache: Map<string, Map<string, { token: QueryToken, subTokens?: string[] }>>
      = new Map<string, Map<string, { token: QueryToken, subTokens?: string[] }>>();

    queryCache: Map<string, { token: QueryToken, subTokens?: string[] }>;

    tokensToRequest: Set<string> = new Set<string>();

    constructor(public qd: QueryDescription) {

      var cache = TokenCompleter.globalCache.get(qd.queryKey);
      if (cache == null) {
        cache = new Map<string, { token: QueryToken, subTokens?: string[] }>();
        TokenCompleter.globalCache.set(qd.queryKey, cache);
      }

      this.queryCache = cache;
    }

    requestFilter(fo: FilterOption): void {

      if (isFilterGroup(fo)) {
        fo.token && this.request(fo.token.toString());

        fo.filters.notNull().forEach(f => this.requestFilter(f));
      } else {

        this.request(fo.token.toString());
      }
    }

    request(fullKey: string): void {

      var token = this.queryCache.get(fullKey);
      if (token)
        return;

      this.tokensToRequest.add(fullKey);
    }

    finished(): Promise<void> {

      if (this.tokensToRequest.size == 0)
        return Promise.resolve(undefined);

      return TokenCompleter.API_parseTokens(this.qd.queryKey, Array.from(this.tokensToRequest)).then(parsedTokens => {
        parsedTokens.forEach(t => {
          this.addToCache(t);
        });
      });
    }

    addToCache(dto: QueryTokenWithoutParent | QueryToken, parentToken?: QueryToken): QueryToken {

      let cached = this.queryCache.get(dto.fullKey);
      if (cached == null) {

        parentToken ??= (dto.parent != null ? this.addToCache(dto.parent as QueryToken) : undefined)

        function getParent(token: string) {
          if (token.endsWith("]"))
            return token.beforeLast("[").beforeLast(".");

          return token.tryBeforeLast(".");
        }


        if (parentToken == null) {
          if (getParent(dto.fullKey) != null)
            throw new Error(`Token with key '${dto.fullKey}' on query '${this.qd.queryKey}' has no parent, but it is not a root token`);
        } else {
          if (parentToken.fullKey != getParent(dto.fullKey))
            throw new Error("Invalid parent");
        }

        const { subTokens, parent, ...rest } = dto as QueryTokenWithoutParent;
        const token = rest as Writable<QueryToken>;
        token.parent = parentToken;
        token.__isCached__ = true;
        //if (token.fullKey == "Locked")
        //  console.log(token);
        Object.freeze(token);
        cached = { token: token as QueryToken, subTokens: undefined };
        this.queryCache.set(dto.fullKey, cached);
      }

      if (cached.token.parent == null && dto.parent != null)
        (cached.token as any).parent = this.addToCache(dto.parent as QueryToken);

      const subTokens = (dto as QueryTokenWithoutParent).subTokens;
      if (subTokens != null && (cached.subTokens == null)) {
        cached.subTokens = Dic.map(subTokens, (key, st) => this.addToCache(st, cached!.token).fullKey);
      }

      return cached.token;
    }


    get(fullKey: string, options: SubTokensOptions): QueryToken {
      var token = this.queryCache.get(fullKey)?.token;

      if (!token)
        throw new Error(`Token with key '${fullKey}' not found on query '${this.qd.queryKey}`);

      if ((options & SubTokensOptions.CanAggregate) == 0 && hasAggregate(token))
        throw new Error(`Token with key '${fullKey}' on query '${this.qd.queryKey} not valid (aggregates not allowed)`);

      if ((options & SubTokensOptions.CanAnyAll) == 0 && hasAnyOrAll(token))
        throw new Error(`Token with key '${fullKey}' on query '${this.qd.queryKey} not valid (Any/All not allowed)`);

      if ((options & SubTokensOptions.CanElement) == 0 && hasElement(token))
        throw new Error(`Token with key '${fullKey}' on query '${this.qd.queryKey} not valid (Element not allowed)`);

      if ((options & SubTokensOptions.CanOperation) == 0 && hasOperation(token))
        throw new Error(`Token with key '${fullKey}' on query '${this.qd.queryKey} not valid (Operation not allowed)`);

      if ((options & SubTokensOptions.CanToArray) == 0 && hasToArray(token))
        throw new Error(`Token with key '${fullKey}' on query '${this.qd.queryKey} not valid (ToArray not allowed)`);

      if ((options & SubTokensOptions.CanSnippet) == 0 && hasSnippet(token))
        throw new Error(`Token with key '${fullKey}' on query '${this.qd.queryKey} not valid (Snippet not allowed)`);

      if ((options & SubTokensOptions.CanManual) == 0 && hasManual(token))
        throw new Error(`Token with key '${fullKey}' on query '${this.qd.queryKey} not valid (Manual not allowed)`);

      if ((options & SubTokensOptions.CanNested) == 0 && hasNested(token))
        throw new Error(`Token with key '${fullKey}' on query '${this.qd.queryKey} not valid (Nested not allowed)`);

      if ((options & SubTokensOptions.CanTimeSeries) == 0 && hasTimeSeries(token))
        throw new Error(`Token with key '${fullKey}' on query '${this.qd.queryKey} not valid (TimeSeries not allowed)`);

      return token;
    }

    async getSubTokens(parentToken: QueryToken | undefined, options: SubTokensOptions, autoExpand: boolean): Promise<QueryToken[]> {

      let candidates = parentToken == null ? Dic.getValues(this.qd.columns) :
        await this.getSubTokensInternal(parentToken);

      if (autoExpand) {

        const autoExpandToken = (cached: { token: QueryToken, subTokens?: string[] }): QueryToken[] => {
          if (cached.token.autoExpand) {

            return [cached.token, ...cached.subTokens!.flatMap(fullKey => autoExpandToken(this.queryCache.get(fullKey)!))];
          }

          return [cached.token];
        }

        candidates = candidates.flatMap(t => autoExpandToken(this.queryCache.get(t.fullKey)!))
          .orderBy(a => a.parent == parentToken ? 0 : 1);
      }

      candidates = candidates.filter(t => {

        if (t.hideInAutoExpand && t.parent?.fullKey != parentToken?.fullKey)
          return false;

        if ((options & SubTokensOptions.CanAggregate) == 0 && hasAggregate(t))
          return false;

        if ((options & SubTokensOptions.CanAnyAll) == 0 && hasAnyOrAll(t))
          return false;

        if ((options & SubTokensOptions.CanElement) == 0 && hasElement(t))
          return false;

        if ((options & SubTokensOptions.CanOperation) == 0 && hasOperation(t))
          return false;

        if ((options & SubTokensOptions.CanToArray) == 0 && hasToArray(t))
          return false;

        if ((options & SubTokensOptions.CanSnippet) == 0 && hasSnippet(t))
          return false;

        if ((options & SubTokensOptions.CanManual) == 0 && hasManual(t))
          return false;

        if ((options & SubTokensOptions.CanNested) == 0 && hasNested(t))
          return false;

        if ((options & SubTokensOptions.CanTimeSeries) == 0 && hasTimeSeries(t))
          return false;

        return true;
      });

      return candidates;

    }

    async getSubTokensInternal(parentToken: QueryToken): Promise<QueryToken[]> {

      var cached = this.queryCache.get(parentToken.fullKey);

      if (cached == null) {
        this.addToCache(parentToken);

        cached = this.queryCache.get(parentToken.fullKey)!;
      }

      //if (cached?.token != parentToken)
      //  throw new Error(`Token '${parentToken.fullKey}' is not the same instance as in the cache`);

      if (cached.subTokens != null)
        return cached.subTokens.map(fullKey => this.queryCache.get(fullKey)?.token!);

      var st = await TokenCompleter.API_getSubTokens(this.qd.queryKey, parentToken.fullKey);

      var subTokens = st.map(st => this.addToCache(st, cached?.token));

      cached.subTokens = subTokens.map(a => a.fullKey);

      return subTokens;
    }

    toFilterOptionParsed(fo: FilterOption, options: SubTokensOptions): FilterOptionParsed {
      if (isFilterGroup(fo)) {
        const token = fo.token && this.get(fo.token.toString(), options)

        return ({
          token: token,
          groupOperation: fo.groupOperation,
          value: fo.value,
          pinned: fo.pinned && toPinnedFilterParsed(fo.pinned),
          dashboardBehaviour: fo.dashboardBehaviour,
          filters: fo.filters.notNull().map(f => this.toFilterOptionParsed(f, options)),
          frozen: fo.frozen || false,
          expanded: false,
        } as FilterGroupOptionParsed);
      }
      else {

        const token = this.get(fo.token.toString(), options);

        return ({
          token: token,
          operation: fo.operation ?? "EqualTo",
          value: fo.value,
          frozen: fo.frozen || false,
          removeElementWarning: fo.removeElementWarning,
          pinned: fo.pinned && toPinnedFilterParsed(fo.pinned),
          dashboardBehaviour: fo.dashboardBehaviour,
        } as FilterConditionOptionParsed);
      }
    }

    private static API_parseTokens(queryKey: string, tokens: string[]): Promise<QueryToken[]> {
      return ajaxPost({ url: "/api/query/parseTokens" }, { queryKey, tokens });
    }

    private static API_getSubTokens(queryKey: string, token: string): Promise<QueryTokenWithoutParent[]> {
      return ajaxPost({ url: "/api/query/subTokens" }, { queryKey, token: token });
    }
  }

  export function parseFilterValues(filterOptions: FilterOptionParsed[]): Promise<void> {

    const needsModel: Lite<any>[] = [];

    function parseFilterValue(fo: FilterOptionParsed) {
      if (isFilterGroup(fo))
        fo.filters.forEach(f => parseFilterValue(f));
      else {
        if (isList(fo.operation!)) {
          if (!Array.isArray(fo.value))
            fo.value = [fo.value].notNull();

          fo.value = (fo.value as any[]).map(v => parseValue(fo.token!, v, needsModel));
        }
        else {
          if (Array.isArray(fo.value))
            throw new Error("Unespected array for operation " + fo.operation);

          fo.value = parseValue(fo.token!, fo.value, needsModel);
        }
      }
    }

    filterOptions.forEach(fo => parseFilterValue(fo));

    if (needsModel.length == 0)
      return Promise.resolve(undefined);

    return Navigator.API.fillLiteModelsArray(needsModel)
  }


  function parseValue(token: QueryToken, val: any, needModel: Array<any>): any {
    switch (token.filterType) {
      case "Boolean": return parseBoolean(val);
      case "Integer": return nanToNull(parseInt(val));
      case "Decimal": return nanToNull(parseFloat(val));
      case "DateTime": {

        if (val == null)
          return null;

        if (typeof val == "string") {

          const dt = val.endsWith("Z") ? DateTime.fromISO(val, { zone: "utc" }) : DateTime.fromISO(val);

          if (val.length == 10 && token.type.name == "DateTime") //Date -> DateTime
            return dt.toISO()!;

          if (val.length > 10 && token.type.name == "DateOnly") //DateTime -> Date
            return dt.toISODate();

          return val;
        }

        if (val instanceof DateTime) {
          if (token.type.name == "DateOnly") //DateTime -> Date
            return val.toISODate();
          return val.toISO()!;
        }

        if (val instanceof Date) {
          if (token.type.name == "DateOnly") //DateTime -> Date
            return DateTime.fromJSDate(val).toISODate();
          return DateTime.fromJSDate(val).toISO()!;
        }

        return val;
      }

      case "Lite":
        {
          const lite = convertToLite(val);

          if (lite && !lite.model)
            needModel.push(lite);

          return lite;
        }
      case "Model":
        {
          return Decoder.decodeModel[token.type.name](val);
        }
    }

    return val;
  }



  function nanToNull(n: number) {
    if (isNaN(n))
      return undefined;

    return n;
  }

  function convertToLite(val: string | Lite<Entity> | Entity | MListElement<Entity> | MListElement<Lite<Entity>> | undefined): Lite<Entity> | undefined {
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

    if (isMListElement(val))
      return convertToLite(val.element);

    throw new Error(`Impossible to convert ${val} to Lite`);
  }

  function clearQueryDescriptionCache() {
    queryDescriptionCache.clear();
    TokenCompleter.globalCache.clear();
  }

  let queryDescriptionCache = new Map<string, Promise<QueryDescription>>;
  export function getQueryDescription(queryName: PseudoType | QueryKey): Promise<QueryDescription> {
    const queryKey = getQueryKey(queryName);

    if (!queryDescriptionCache.has(queryKey)) {
      queryDescriptionCache.set(queryKey, API.fetchQueryDescription(queryKey).then(dto => {

        const qd: QueryDescription = {
          queryKey: dto.queryKey,
          columns: {}
        };

        const completed = new TokenCompleter(qd);

        qd.columns = Dic.mapObject(dto.columns, (key, token) => completed.addToCache(token));

        return Object.freeze(qd);
      }));
    }

    return queryDescriptionCache.get(queryKey)!;
  }

  export function inDB<R>(entity: Entity | Lite<Entity>, token: QueryTokenString<R> | string): Promise<AddToLite<R> | null> {

    var fo: FindOptions = {
      queryName: isEntity(entity) ? entity.Type : entity.EntityType,
      filterOptions: [{ token: "Entity", value: entity }],
      pagination: { mode: "Firsts", elementsPerPage: 1 },
      columnOptions: [{ token: token }],
      columnOptionsMode: "ReplaceAll",
    };

    return getQueryDescription(fo.queryName)
      .then(qd => parseFindOptions(fo!, qd, false))
      .then(fop => API.executeQuery(getQueryRequest(fop)))
      .then(rt => rt.rows[0].columns[0]);
  }

  export function inDBMany<TO extends { [name: string]: QueryTokenString<any> | string }>(entity: Entity | Lite<Entity>, tokensObject: TO): Promise<ExtractTokensObject<TO>> {

    var fo: FindOptions = {
      queryName: isEntity(entity) ? entity.Type : entity.EntityType,
      filterOptions: [{ token: "Entity", value: entity }],
      pagination: { mode: "Firsts", elementsPerPage: 1 },
      columnOptions: Dic.getValues(tokensObject).map(a => ({ token: a })),
      columnOptionsMode: "ReplaceAll",
    };

    return getQueryDescription(fo.queryName)
      .then(qd => parseFindOptions(fo!, qd, false))
      .then(fop => API.executeQuery(getQueryRequest(fop)))
      .then(rt => {
        var firstRow = rt.rows[0];
        return firstRow && Dic.mapObject(tokensObject, (key, value, index) => firstRow.columns[index]) as ExtractTokensObject<TO>;
      });
  }

  export function inDBList<R>(entity: Entity | Lite<Entity>, token: QueryTokenString<R> | string): Promise<AddToLite<R>[]> {

    var fo: FindOptions = {
      queryName: isEntity(entity) ? entity.Type : entity.EntityType,
      filterOptions: [{ token: "Entity", value: entity }],
      pagination: { mode: "All" },
      columnOptions: [{ token: token }],
      columnOptionsMode: "ReplaceAll",
    };

    return getQueryDescription(fo.queryName)
      .then(qd => parseFindOptions(fo!, qd, false))
      .then(fop => API.executeQuery(getQueryRequest(fop)))
      .then(rt => rt.rows.map(r => r.columns[0]).notNull());
  }

  export type AddToLite<T> = T extends Entity ? Lite<T> : T;
  export type ExtractQueryToken<T> =
    T extends QueryTokenString<infer S> ? AddToLite<S> :
    T extends TokenObject ? ExtractTokensObject<T>[] :
    T extends undefined ? undefined :
    any;

  export type ExtractTokensObject<T> = {
    [P in keyof T]: ExtractQueryToken<T[P]>;
  };

  export function useQuery(fo: FindOptions, additionalDeps?: any[], options?: APIHookOptions): ResultTable | undefined;
  export function useQuery(fo: FindOptions | null, additionalDeps?: any[], options?: APIHookOptions): ResultTable | undefined | null;
  export function useQuery(fo: FindOptions | null, additionalDeps?: any[], options?: APIHookOptions): ResultTable | undefined | null {
    return useAPI(
      signal => fo == null ? null : getResultTable(fo, signal),
      [fo && findOptionsPath(fo), ...(additionalDeps || [])],
      options);

  }

  interface FetchEntitiesOptions<T extends Entity = any> {
    queryName: Type<T> | QueryKey | PseudoType;
    filterOptions?: (FilterOption | null | undefined)[];
    orderOptions?: (OrderOption | null | undefined)[];
    count?: number | null;
  }




  export function useFetchLites<T extends Entity>(fo: FetchEntitiesOptions<T>, additionalDeps?: React.DependencyList, options?: APIHookOptions): Lite<T>[] | undefined;
  export function useFetchLites<T extends Entity>(fo: FetchEntitiesOptions<T> | null, additionalDeps?: React.DependencyList, options?: APIHookOptions): Lite<T>[] | null | undefined;
  export function useFetchLites<T extends Entity>(fo: FetchEntitiesOptions<T> | null, additionalDeps?: React.DependencyList, options?: APIHookOptions): Lite<T>[] | null | undefined {
    return useAPI(() => fo && fetchLites(fo),
      [
        fo && findOptionsPath({
          queryName: fo.queryName,
          filterOptions: fo.filterOptions,
          orderOptions: fo.orderOptions,
          pagination: fo.count == null ? { mode: "All" } : { mode: "Firsts", elementsPerPage: fo.count }
        }),
        ...additionalDeps ?? []
      ],
      options,
    );
  }

  export function useFetchEntities<T extends Entity>(fo: FetchEntitiesOptions<T>, additionalDeps?: React.DependencyList, options?: APIHookOptions): T[] | undefined;
  export function useFetchEntities<T extends Entity>(fo: FetchEntitiesOptions<T> | null, additionalDeps?: React.DependencyList, options?: APIHookOptions): T[] | null | undefined;
  export function useFetchEntities<T extends Entity>(fo: FetchEntitiesOptions<T> | null, additionalDeps?: React.DependencyList, options?: APIHookOptions): T[] | null | undefined {
    return useAPI(() => fo && fetchEntities(fo),
      [
        fo && findOptionsPath({
          queryName: fo.queryName,
          filterOptions: fo.filterOptions,
          orderOptions: fo.orderOptions,
          pagination: fo.count == null ? { mode: "All" } : { mode: "Firsts", elementsPerPage: fo.count }
        }),
        ...additionalDeps ?? []
      ],
      options,
    );
  }


  export function useResultTableTyped<TO extends { [name: string]: QueryTokenString<any> | string }>(fo: FindOptions, tokensObject: TO, additionalDeps?: React.DependencyList, options?: APIHookOptions): ExtractTokensObject<TO>[] | undefined;
  export function useResultTableTyped<TO extends { [name: string]: QueryTokenString<any> | string }>(fo: FindOptions | null, tokensObject: TO, additionalDeps?: React.DependencyList, options?: APIHookOptions): ExtractTokensObject<TO>[] | null | undefined;
  export function useResultTableTyped<TO extends { [name: string]: QueryTokenString<any> | string }>(fo: FindOptions | null, tokensObject: TO, additionalDeps?: React.DependencyList, options?: APIHookOptions): ExtractTokensObject<TO>[] | null | undefined {
    var fo2: FindOptions | null = fo && {
      pagination: { mode: "All" },
      ...fo,
      columnOptions: getAllColumns(tokensObject),
      columnOptionsMode: "ReplaceAll",
    };

    var result = useAPI(signal => fo2 && getResultTable(fo2, signal), [fo2 && findOptionsPath(fo2), ...(additionalDeps || [])], options);

    return result && result.rows.map(row => toTypedRow(tokensObject, result!.columns, row));
  }

  function getAllColumns(tokensObject: TokenObject): ColumnOption[] {
    return Dic.getValues(tokensObject).flatMap(a => {
      if (a == undefined)
        return [];

      if (typeof a == "string" || a instanceof QueryTokenString)
        return [{ token: a }];

      return getAllColumns(a);
    });
  }

  export interface TokenObject {
    [name: string]: QueryTokenString<any> | string | TokenObject | undefined;
  }

  export function toTypedRow<TO extends TokenObject>(tokensObject: TO, columns: string[], row: ResultRow): ExtractTokensObject<TO> {
    return Dic.mapObject(tokensObject, (key, value) => {

      if (value == undefined)
        return undefined;

      var token = value.toString();

      if (token == "Entity" && row.entity)
        return row.entity;

      const index = columns.indexOf(token);

      return row.columns[index];
    }) as ExtractTokensObject<TO>;
  }

  export async function getResultTableTyped<TO extends TokenObject>(fo: FindOptions, tokensObject: TO, signal?: AbortSignal): Promise<ExtractTokensObject<TO>[]> {
    var fo2: FindOptions = {
      pagination: { mode: "All" },
      ...fo,
      columnOptions: getAllColumns(tokensObject),
      columnOptionsMode: "ReplaceAll",
    };

    const rt = await getResultTable(fo2);

    return rt.rows.map(row => toTypedRow(tokensObject, rt.columns, row));
  }

  export async function getResultTableTypedWithPagination<TO extends TokenObject>(fo: FindOptions, tokensObject: TO, signal?: AbortSignal): Promise<{ totalElements?: number, rows: ExtractTokensObject<TO>[] }> {
    var fo2: FindOptions = {
      ...fo,
      columnOptions: getAllColumns(tokensObject),
      columnOptionsMode: "ReplaceAll",
    };

    const rt = await getResultTable(fo2);

    return ({
      totalElements: rt.totalElements,
      rows: rt.rows.map(row => toTypedRow(tokensObject, rt.columns, row))
    });
  }

  export function getResultTable(fo: FindOptions, signal?: AbortSignal): Promise<ResultTable> {

    fo = defaultNoColumnsAllRows(fo, undefined);

    return getQueryDescription(fo.queryName)
      .then(qd => parseFindOptions(fo!, qd, false))
      .then(fop => API.executeQuery(getQueryRequest(fop), signal));
  }

  export function useInDB<R>(entity: Entity | Lite<Entity> | null, token: QueryTokenString<R> | string, additionalDeps?: any[], options?: APIHookOptions): AddToLite<R> | null | undefined {
    var resultTable = useQuery(entity == null || isEntity(entity) && entity.isNew ? null : {
      queryName: isEntity(entity) ? entity.Type : entity.EntityType,
      filterOptions: [{ token: "Entity", value: entity }],
      pagination: { mode: "Firsts", elementsPerPage: 1 },
      columnOptions: [{ token: token }],
      columnOptionsMode: "ReplaceAll",
    }, additionalDeps, options);

    if (entity == null)
      return null;

    if (resultTable == null)
      return undefined;

    return resultTable.rows[0]?.columns[0] ?? null;
  }



  export function useInDBMany<TO extends { [name: string]: QueryTokenString<any> | string }>(entity: Entity | Lite<Entity>, tokensObject: TO, additionalDeps?: any[], options?: APIHookOptions): ExtractTokensObject<TO> | undefined;
  export function useInDBMany<TO extends { [name: string]: QueryTokenString<any> | string }>(entity: Entity | Lite<Entity> | null, tokensObject: TO, additionalDeps?: any[], options?: APIHookOptions): ExtractTokensObject<TO> | null | undefined;
  export function useInDBMany<TO extends { [name: string]: QueryTokenString<any> | string }>(entity: Entity | Lite<Entity> | null, tokensObject: TO, additionalDeps?: any[], options?: APIHookOptions): ExtractTokensObject<TO> | null | undefined {
    var resultTable = useQuery(entity == null || isEntity(entity) && entity.isNew ? null : {
      queryName: isEntity(entity) ? entity.Type : entity.EntityType,
      filterOptions: [{ token: "Entity", value: entity }],
      pagination: { mode: "Firsts", elementsPerPage: 1 },
      columnOptions: Dic.getValues(tokensObject).map(a => ({ token: a })),
      columnOptionsMode: "ReplaceAll",
    }, additionalDeps, options);

    return React.useMemo(() => {

      if (entity == null)
        return null;

      if (resultTable == null)
        return undefined;

      var firstRow = resultTable.rows[0];

      return firstRow && Dic.mapObject(tokensObject, (key, value, index) => firstRow.columns[index]) as ExtractTokensObject<TO>;
    }, [entity, resultTable]);
  }


  export function useInDBList<R>(entity: Entity | Lite<Entity> | null, token: QueryTokenString<R> | string, additionalDeps?: any[], options?: APIHookOptions): AddToLite<R>[] | null | undefined {
    var resultTable = useQuery(entity == null || isEntity(entity) && entity.isNew ? null : {
      queryName: isEntity(entity) ? entity.Type : entity.EntityType,
      filterOptions: [{ token: "Entity", value: entity }],
      pagination: { mode: "All" },
      columnOptions: [{ token: token }],
      columnOptionsMode: "ReplaceAll",
    }, additionalDeps, options);

    return React.useMemo(() => {

      if (entity == null)
        return null;

      if (resultTable == null)
        return undefined;

      return resultTable.rows.map(r => r.columns[0]).notNull();

    }, [entity, resultTable]);
  }

  export function useFetchAllLite<T extends Entity>(type: Type<T>, deps?: any[]): Lite<T>[] | undefined {
    return useAPI(() => API.fetchAllLites({ types: type.typeName }), deps ?? []) as Lite<T>[] | undefined;
  }

  export function decompress(rt: ResultTable): ResultTable {
    var rows = rt.rows;
    var columns = rt.columns;

    for (var i = 0; i < columns.length; i++) {
      var uniqueValues = rt.uniqueValues[columns[i]];

      if (uniqueValues != null) {
        for (var j = 0; j < rows.length; j++) {
          var row = rows[j];
          var index = row.columns[i] as number | null;
          if (index != null)
            row.columns[i] = uniqueValues[index];
        }
      }
    }
    return rt;
  }

  export namespace API {

    export function fetchQueryDescription(queryKey: string): Promise<QueryDescriptionDTO> {
      return ajaxGet({ url: "/api/query/description/" + queryKey });
    }

    export function fetchQueryEntity(queryKey: string): Promise<QueryEntity> {
      return ajaxGet({ url: "/api/query/queryEntity/" + queryKey });
    }


    export async function executeQuerySplitTimeSeries(request: QueryRequest, signal?: AbortSignal): Promise<ResultTable> {
      const st = request.systemTime!;
      const endDate = DateTime.fromISO(st.endDate!);

      const resultTables: { timeSerie: string, rt: ResultTable }[] = [];
      const timeSeries = QueryTokenString.timeSeries.toString();

      var dates = new Array<string>();
      {
        let dt = DateTime.fromISO(st.startDate!);
        while (dt < endDate) {
          dates.push(dt.toISO()!);
          dt = dt.plus({ [st.timeSeriesUnit!.toLowerCase()]: st.timeSeriesStep });
        }
      }

      var notifyOptions: NotifyOptions = {
        text: "",
        type: "loading",
        priority: 10,
      };


      for (var i = 0; i < dates.length; i++) {

        const dt = dates[i];
        const newRequest: QueryRequest = {
          queryKey: request.queryKey,
          filters: request.filters,
          columns: request.columns.filter(a => a.token != timeSeries.toString()),
          orders: request.orders.filter(a => a.token != timeSeries.toString()),
          systemTime: {
            mode: "AsOf",
            startDate: dt,
          },
          groupResults: request.groupResults,
          pagination: { mode: "Firsts", elementsPerPage: st.timeSeriesMaxRowsPerStep }
        };
        const rt = await executeQuery(newRequest, signal);
        resultTables.push({ timeSerie: dt, rt: decompress(rt) });

        notifyOptions.text = JavascriptMessage.loading.niceToString() + `[${i + 1}/${dates.length}]`;
        Notify.singleton?.notifyTimeout(notifyOptions);
      }

      Notify.singleton?.remove(notifyOptions);

      const index = request.columns.findIndex(a => a.token == timeSeries);

      var combined: ResultTable = {
        columns: [timeSeries, ...resultTables.first().rt.columns],
        uniqueValues: {},
        rows: index == -1 ? resultTables.flatMap(a => a.rt.rows) :
          resultTables.flatMap(a => a.rt.rows.map(row => ({
            entity: row.entity,
            columns: [
              a.timeSerie,
              ...row.columns
            ]
          }))),
        totalElements: resultTables.sum(a => a.rt.totalElements! ?? 0),
        pagination: { mode: "All" }
      };

      return combined;
    }


    export function executeQuery(request: QueryRequest, signal?: AbortSignal): Promise<ResultTable> {
      if (request.systemTime?.mode == "TimeSeries" && request.systemTime.splitQueries) {
        return executeQuerySplitTimeSeries(request, signal);
      }

      return ajaxPost<ResultTable>({ url: "/api/query/executeQuery/" + request.queryKey, signal }, request)
        .then(rt => decompress(rt));
    }

    export function queryValue(request: QueryValueRequest, avoidNotifyPendingRequest: boolean | undefined = undefined, signal?: AbortSignal): Promise<any> {
      return ajaxPost({ url: "/api/query/queryValue/" + request.queryKey, avoidNotifyPendingRequests: avoidNotifyPendingRequest, signal }, request);
    }

    export function fetchLites(request: QueryEntitiesRequest): Promise<Lite<Entity>[]> {
      return ajaxPost({ url: "/api/query/lites/" + request.queryKey }, request);
    }

    export function fetchEntities(request: QueryEntitiesRequest): Promise<Entity[]> {
      return ajaxPost({ url: "/api/query/entities/" + request.queryKey }, request);
    }

    export function fetchAllLites(request: { types: string }): Promise<Lite<Entity>[]> {
      return ajaxGet({
        url: "/api/query/allLites?" + QueryString.stringify(request)
      });
    }

    export function findTypeLike(request: { subString: string, count: number }): Promise<Lite<TypeEntity>[]> {
      return ajaxGet({
        url: "/api/query/findTypeLike?" + QueryString.stringify(request)
      });
    }

    export function findLiteLike(request: AutocompleteRequest, signal?: AbortSignal): Promise<Lite<Entity>[]> {
      return ajaxGet({ url: "/api/query/findLiteLike?" + QueryString.stringify({ ...request }), signal });
    }


    export interface AutocompleteRequest {
      types: string;
      subString: string;
      count: number;
    }
  }



  function shouldIgnoreValues(pinned?: PinnedFilter | null) {
    return pinned != null && (pinned.active == "Always" || pinned.active == "WhenHasValue");
  }

  export namespace Encoder {



    export function encodeFilters(query: any, filterOptions?: FilterOption[], prefix?: string): void {

      var i: number = 0;

      function encodeFilter(fo: FilterOption, identation: number, ignoreValues: boolean) {
        var identSuffix = identation == 0 ? "" : ("_" + identation);

        var index = i++;

        if (fo.pinned) {
          var p = fo.pinned;
          query[(prefix ?? "") + "filterPinned" + index + identSuffix] = scapeTilde(typeof p.label == "function" ? p.label() : p.label ?? "") +
            "~" + (p.column == null ? "" : p.column) + (p.colSpan == null ? "" : ("." + p.colSpan)) +
            "~" + (p.row == null ? "" : p.row) +
            "~" + PinnedFilterActive.values().indexOf(p.active ?? "Always") +
            "~" + (p.splitValue ? 1 : 0);
        }


        if (isFilterGroup(fo)) {
          query[(prefix ?? "") + "filter" + index + identSuffix] = (fo.token ?? "") + "~" + (fo.groupOperation) + "~" + (ignoreValues ? "" : stringValue(fo.value));

          fo.filters.notNull().forEach(f => encodeFilter(f, identation + 1, ignoreValues || shouldIgnoreValues(fo.pinned)));
        } else {
          query[(prefix ?? "") + "filter" + index + identSuffix] = fo.token + "~" + (fo.operation ?? "EqualTo") + "~" + (ignoreValues ? "" : stringValue(fo.value));
        }

      }

      if (filterOptions)
        filterOptions.forEach(fo => encodeFilter(fo, 0, false));
    }

    export function encodeOrders(query: any, orderOptions?: OrderOption[], prefix?: string): void {
      if (orderOptions)
        orderOptions.forEach((oo, i) => query[(prefix ?? "") + "order" + i] = (oo.orderType == "Descending" ? "-" : "") + oo.token);
    }

    export function encodeColumns(query: any, columnOptions?: ColumnOption[], prefix?: string): void {
      if (columnOptions) {
        columnOptions.forEach((co, i) => {

          var displayName = co.hiddenColumn ? HIDDEN :
            co.displayName ? scapeTilde(typeof co.displayName == "function" ? co.displayName() : co.displayName) :
              undefined;

          query[(prefix ?? "") + "column" + i] = co.token + (displayName ? ("~" + displayName) : "");
          if (co.summaryToken)
            query[(prefix ?? "") + "summary" + i] = co.summaryToken.toString();
          if (co.combineRows)
            query[(prefix ?? "") + "combine" + i] = co.combineRows == "EqualValue" ? "V" : "E";
        });
      }
    }

    export var encodeModel: { [typeName: string]: (model: any) => string } = {};

    export function stringValue(value: any): string {

      if (value == undefined)
        return "";

      if (Array.isArray(value))
        return value.notNull().map(a => stringValue(a)).join("~");

      if (isEntity(value))
        value = toLite(value, value.isNew);

      if (isLite(value))
        return liteKey(value);

      if (isModifiableEntity(value) && isTypeModel(value.Type)) {
        return encodeModel[value.Type](value);
      }

      return scapeTilde(value.toString());
    }

    export function scapeTilde(str: string): string {
      if (str == undefined)
        return "";

      return str.replace("~", "#|#");
    }
  }




  const HIDDEN = "__";

  export namespace Decoder {

    export const decodeModel: { [typeName: string]: (string: any) => ModelEntity | null } = {};


    interface FilterPart {
      order: number;
      identation: number;
      value: string;
    };

    export function filterInOrder(query: any, prefix: string): FilterPart[] {
      const regex = new RegExp("^" + prefix + "(\\d*)(_(\\d*))?$");

      return Dic.getKeys(query)
        .map(s => regex.exec(s))
        .filter(r => !!r)
        .map(m => ({ order: parseInt(m![1]), identation: parseInt(m![3] ?? "0"), value: query[m![0]] }))
        .orderBy(a => a.order);
    }

    export function decodeFilters(query: any, prefix?: string): FilterOption[] {

      function parsePinnedFilter(str: string): PinnedFilter {
        var parts = str.split("~");
        var col = parts[1];
        return ({
          label: unscapeTildes(parts[0]),
          column: col.length ? (col.contains(".") ? parseInt(col.before(".")) : parseInt(col)) : undefined,
          colSpan: col.length && col.contains(".") ? parseInt(col.after(".")) : undefined,
          row: parts[2].length ? parseInt(parts[2]) : undefined,
          active: parseInt(parts[3]) == 0 ? undefined : PinnedFilterActive.values()[parseInt(parts[3])],
          splitValue: parseInt(parts[4]) == 0 ? undefined : Boolean(parseInt(parts[4])),
        });
      }


      function toFilterList(filters: FilterPart[], identation: number, ignoreValues: boolean): FilterOption[] {

        return filters.groupWhen(a => a.identation == identation).map(gr => {

          var identSuffix = identation == 0 ? "" : ("_" + identation);

          var pinnedText = query[(prefix ?? "") + "filterPinned" + gr.key.order + identSuffix] as string;

          var pinned = pinnedText == undefined ? null : parsePinnedFilter(pinnedText);

          const parts = gr.key.value.split("~");

          if (FilterOperation.isDefined(parts[1])) {
            var operation = FilterOperation.assertDefined(parts[1]);
            return ({
              token: parts[0],
              operation: operation,
              value: ignoreValues ? null :
                !isList(operation) ? unscapeTildes(parts[2]) :
                  parts.slice(2).map(a => unscapeTildes(a)).notNull(),
              pinned: pinned,
            }) as FilterConditionOption
          } else {
            return ({
              token: parts[0] == null || parts[0].length == 0 ? null : parts[0],
              groupOperation: FilterGroupOperation.assertDefined(parts[1]),
              value: ignoreValues ? null : unscapeTildes(parts[2]),
              pinned: pinned,
              filters: toFilterList(gr.elements, identation + 1, ignoreValues || shouldIgnoreValues(pinned)),
            }) as FilterGroupOption;
          }
        });
      }

      return toFilterList(filterInOrder(query, (prefix ?? "") + "filter"), 0, false)
    }

    export function unscapeTildes(str: string | undefined): string | undefined {
      if (!str)
        return undefined;

      return str.replace("#|#", "~");
    }

    export function valuesInOrder(query: any, prefix: string): { index: number, value: string }[] {
      const regex = new RegExp("^" + prefix + "(\\d*)$");

      return Dic.getKeys(query).map(s => regex.exec(s))
        .filter(r => !!r)
        .map(r => ({ index: parseInt(r![1]), value: query[r![0]] }))
        .orderBy(a => a.index);
    }

    export function decodeOrders(query: any, prefix?: string): OrderOption[] {
      return valuesInOrder(query, (prefix ?? "") + "order").map(p => ({
        orderType: p.value[0] == "-" ? "Descending" : "Ascending",
        token: p.value[0] == "-" ? p.value.tryAfter("-") : p.value
      } as OrderOption));
    }


    export function decodeColumns(query: any, prefix?: string): ColumnOption[] {
      var summary = valuesInOrder(query, (prefix ?? "") + "summary");
      var combine = valuesInOrder(query, (prefix ?? "") + "combine");

      return valuesInOrder(query, (prefix ?? "") + "column").map(p => {

        var displayName = unscapeTildes(p.value.tryAfter("~"));

        var token = p.value.tryBefore("~") ?? p.value;
        var comb = combine.firstOrNull(a => a.index == p.index)?.value;
        return softCast<ColumnOption>({
          token: token,
          displayName: displayName == HIDDEN ? undefined : displayName,
          hiddenColumn: displayName == HIDDEN ? true : undefined,
          summaryToken: summary.firstOrNull(a => a.index == p.index)?.value,
          combineRows: comb == "V" ? "EqualValue" : comb == "E" ? "EqualEntity" : undefined,
        });
      });
    }
  }


  export namespace ButtonBarQuery {

    interface ButtonBarQueryContext {
      searchControl: SearchControlLoaded;
      findOptions: FindOptionsParsed;
    }

    export const onButtonBarElements: ((ctx: ButtonBarQueryContext) => ButtonBarElement | undefined)[] = [];

    export function getButtonBarElements(ctx: ButtonBarQueryContext): ButtonBarElement[] {
      return onButtonBarElements.map(f => f(ctx)).filter(a => a != undefined).map(a => a as ButtonBarElement);
    }

    export function clearButtonBarElements(): void {
      ButtonBarQuery.onButtonBarElements.clear();
    }

  }



  export interface QuerySettings {
    queryName: PseudoType | QueryKey;
    pagination?: Pagination;
    allowSystemTime?: boolean;
    defaultOrders?: OrderOption[];
    defaultFilters?: FilterOption[];
    defaultAggregates?: ColumnOption[];
    hiddenColumns?: ColumnOption[];
    formatters?: { [token: string]: CellFormatter };
    rowAttributes?: (row: ResultRow, searchControl: SearchControlLoaded) => React.HTMLAttributes<HTMLTableRowElement> | undefined;
    entityFormatter?: EntityFormatter;
    inPlaceNavigation?: boolean;
    modalSize?: BsSize;
    showContextMenu?: (fop: FindOptionsParsed) => boolean | "Basic";
    allowCreate?: boolean;
    allowSelection?: boolean;
    getViewPromise?: (e: ModifiableEntity | null) => (undefined | string | ViewPromise<ModifiableEntity>);
    onDoubleClick?: (e: React.MouseEvent<any>, row: ResultRow, columns: string[], sc?: SearchControlLoaded) => void;
    simpleFilterBuilder?: (sfbc: SimpleFilterBuilderContext) => React.ReactElement | undefined;
    onFind?: (fo: FindOptions, mo?: ModalFindOptions) => Promise<Lite<Entity> | undefined>;
    onFindMany?: (fo: FindOptions, mo?: ModalFindOptions) => Promise<Lite<Entity>[] | undefined>;
    onExplore?: (fo: FindOptions, mo?: ModalFindOptions) => Promise<void>;
    extraButtons?: (searchControl: SearchControlLoaded) => (ButtonBarElement | null | undefined | false)[];
    noResultMessage?: (searchControl: SearchControlLoaded) => React.ReactElement | string | undefined;
    customGetPropsFromFilter?: (filters: FilterOptionParsed[]) => Promise<any>;
    mobileOptions?: (fop: FindOptionsParsed) => SearchControlMobileOptions;
    markRowsColumn?: string;
  }


  export interface SimpleFilterBuilderContext {
    queryDescription: QueryDescription;
    initialFilterOptions: FilterOptionParsed[];
    search: () => void;
    searchControl?: SearchControlLoaded
  }



  export function isSystemVersioned(tr?: TypeReference): boolean {
    return tr != null && getTypeInfos(tr).some(ti => ti.isSystemVersioned == true)
  }

  export function getCellFormatter(qs: QuerySettings | undefined, qt: QueryToken, sc: SearchControlLoaded | undefined): CellFormatter {

    const result = qs?.formatters && qs.formatters[qt.fullKey];

    if (result)
      return result;

    const prRoute = registeredPropertyFormatters[qt.propertyRoute!];
    if (prRoute)
      return prRoute;

    const rule = formatRules.filter(a => a.isApplicable(qt, sc)).last("FormatRules");

    return rule.formatter(qt, sc);
  }

  export function resetFormatRules(): void {
    Dic.clear(registeredPropertyFormatters);

    formatRules.clear();
    formatRules.push(...FinderRules.initFormatRules());

    entityFormatRules.clear();
    entityFormatRules.push(...FinderRules.initEntityFormatRules());

    quickFilterRules.clear();
    quickFilterRules.push(...FinderRules.initQuickFilterRules());

    filterValueFormatRules.clear();
    filterValueFormatRules.push(...FinderRules.initFilterValueFormatRules());
  }

  export interface FormatRule {
    name: string;
    formatter: (column: QueryToken, sc: SearchControlLoaded | undefined) => CellFormatter;
    isApplicable: (column: QueryToken, sc: SearchControlLoaded | undefined) => boolean;
  }

  export class CellFormatter {
    constructor(
      public formatter: (cell: any, ctx: CellFormatterContext, column: ColumnParsed) => React.ReactElement | string | null | undefined,
      public fillWidth: boolean,
      public cellClass?: string) {
    }
  }

  export interface CellFormatterContext {
    refresh?: () => void;
    systemTime?: SystemTime;
    columns: string[];
    row: ResultRow;
    rowIndex: number;
    searchControl?: SearchControlLoaded
  }

  export const registeredPropertyFormatters: { [typeAndProperty: string]: CellFormatter } = {};

  export function registerPropertyFormatter(pr: PropertyRoute | string/*For expressions*/ | undefined, formater: CellFormatter): void {
    if (pr == null)
      return;
    registeredPropertyFormatters[pr.toString()] = formater;
  }

  export const formatRules: FormatRule[] = FinderRules.initFormatRules();

  export interface EntityFormatRule {
    name: string;
    formatter: EntityFormatter;
    isApplicable: (sc: SearchControlLoaded | undefined) => boolean;
  }

  export class EntityFormatter {
    constructor(
      public formatter: (ctx: CellFormatterContext) => React.ReactElement | string | null | undefined,
      public cellClass?: string) {
    }
  }

  export const entityFormatRules: EntityFormatRule[] = FinderRules.initEntityFormatRules();

  export interface QuickFilterRule {
    name: string
    applicable: (qt: QueryToken, cellValue: unknown, sc: SearchControlLoaded) => boolean;
    execute: (qt: QueryToken, cellValue: unknown, sc: SearchControlLoaded) => Promise<boolean>;
  }

  export const quickFilterRules: QuickFilterRule[] = FinderRules.initQuickFilterRules();

  export interface FilterFormatterContext {
    ctx: TypeContext<any>;
    label?: string;
    mandatory?: boolean;
    forceNullable?: boolean;
    filterOptions: FilterOptionParsed[];
    handleValueChange: (f: FilterOptionParsed, avoidSearch?: boolean) => void;
  }


  export interface FilterValueFormatter {
    name: string
    applicable: (f: FilterOptionParsed, ffc: FilterFormatterContext) => boolean;
    renderValue: (f: FilterOptionParsed, ffc: FilterFormatterContext) => React.ReactElement;
  }

  export const filterValueFormatRules: FilterValueFormatter[] = FinderRules.initFilterValueFormatRules();

  export function renderFilterValue(f: FilterOptionParsed, ffc: FilterFormatterContext): React.ReactElement<any, string | React.JSXElementConstructor<any>> {
    var rule = filterValueFormatRules.filter(r => r.applicable(f, ffc)).last();
    return rule.renderValue(f, ffc);
  }

}

type Writable<T> = {
  -readonly [P in keyof T]: T[P];
};
