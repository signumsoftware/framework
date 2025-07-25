import * as React from 'react'
import { Finder } from '../Finder'
import { AbortableRequest } from '../Services'
import { FindOptions, FilterOptionParsed, OrderOptionParsed, OrderRequest, ResultRow, ColumnOptionParsed, ColumnRequest, QueryDescription, QueryRequest, FilterOption, ResultTable } from '../FindOptions'
import { getTypeInfo, getQueryKey, QueryTokenString, getTypeName, getTypeInfos, TypeInfo } from '../Reflection'
import { ModifiableEntity, Lite, Entity, toLite, is, isLite, isEntity, getToString, liteKey, SearchMessage, parseLiteList } from '../Signum.Entities'
import { TextHighlighter, TypeaheadController, TypeaheadOptions } from '../Components/Typeahead'
import { Navigator, AutocompleteConstructor } from '../Navigator';
import { Dic } from '../Globals'

export interface AutocompleteConfig<T> {
  getItems: (subStr: string) => Promise<T[]>;
  getItemsDelay(): number | undefined;
  getMinLength(): number | undefined;
  renderItem(item: T, highlighter: TextHighlighter): React.ReactNode;
  renderList?(typeahead: TypeaheadController): React.ReactNode;
  getEntityFromItem(item: T): Promise<Lite<Entity> | ModifiableEntity | undefined>;
  getDataKeyFromItem(item: T): string | undefined;
  getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<T>;
  isCompatible(item: unknown, type: string): item is T;
  getSortByString(item: T): string
  abort(): void;
}

export interface AutocompleteConfigOptions {
  itemsDelay?: number;
  minLength?: number;
}

export interface LiteAutocomplateConfigOptions extends AutocompleteConfigOptions {
  requiresInitialLoad?: boolean,
  showType?: boolean
}

export function isAutocompleteConstructor<T extends ModifiableEntity>(a: any): a is AutocompleteConstructor<T> {
  return typeof a == "object" && (a as AutocompleteConstructor<T>).onClick != null;
}

export function isResultRow(a: any): a is ResultRow {
  return typeof a == "object" && (a as ResultRow).entity != null;
}

export class LiteAutocompleteConfig<T extends Entity> implements AutocompleteConfig<Lite<T> | AutocompleteConstructor<T>>{
  requiresInitialLoad?: boolean;
  showType?: boolean;

  constructor(
    public getItemsFunction: (signal: AbortSignal, subStr: string) => Promise<(Lite<T> | AutocompleteConstructor<T>)[]>,
    options?: LiteAutocomplateConfigOptions,
  ) {
    Dic.assign(this, options);
  }
  itemsDelay?: number | undefined;
  minLength?: number | undefined;

  abortableRequest: AbortableRequest<string, (Lite<T> | AutocompleteConstructor<T>)[]> = new AbortableRequest((signal, subStr: string) => this.getItemsFunction(signal, subStr));

  getItemsDelay(): number | undefined {
    return this.itemsDelay;
  }

  getMinLength(): number | undefined {
    return this.minLength;
  }

  abort(): void {
    this.abortableRequest.abort();
  }

  getItems(subStr: string): Promise<(Lite<T> | AutocompleteConstructor<T>)[]> {
    return this.abortableRequest.getData(subStr);
  }

  renderItem(item: Lite<T> | AutocompleteConstructor<T>, hl: TextHighlighter): React.ReactNode{

    if (isAutocompleteConstructor<T>(item)) {
      if (item.customElement)
        return item.customElement;

      var ti = getTypeInfo(item.type);
      return <em>{SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(ti.gender).formatWith(ti.niceName)} "{hl.query}"</em>;
    }

    var toStr = getToString(item);
    var html = Navigator.renderLite(item, hl);
    if (this.showType)
      return <span title={toStr}>{html}<TypeBadge entity={item} /></span>;
    else
      return html;
  }

  getEntityFromItem(item: Lite<T> | AutocompleteConstructor<T>): Promise<Lite<Entity> | ModifiableEntity | undefined> {

    if (isAutocompleteConstructor(item))
      return item.onClick() as Promise<Lite<Entity> | ModifiableEntity | undefined>;

    return Promise.resolve(item);
  }

  getDataKeyFromItem(item: Lite<T> | AutocompleteConstructor<T>): string | undefined {

    if (isAutocompleteConstructor(item))
      return "create-" + getTypeName(item.type);

    return liteKey(item);
  }

  getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<Lite<T>> {
    var lite = this.convertToLite(entity);;

    if (!this.requiresInitialLoad)
      return Promise.resolve(lite);

    if (lite.id == undefined)
      return Promise.resolve(lite);

    return this.abortableRequest.getData(lite.id!.toString()).then(lites => {

      const result = lites.filter(a => isLite(a) && is(a, lite)).firstOrNull() as Lite<T> | null;

      if (!result)
        throw new Error("Impossible to getInitialItem with the current implementation of getItems");

      return result;
    });
  }

  convertToLite(entity: Lite<Entity> | ModifiableEntity): Lite<T> {

    if (isLite(entity))
      return entity as Lite<T>;

    if (isEntity(entity))
      return toLite(entity, entity.isNew) as Lite<T>;

    throw new Error("Impossible to convert to Lite {0}".formatWith(entity.Type));
  }

  isCompatible(item: unknown, typeName: string): item is Lite<T> | AutocompleteConstructor<T> {
    return isLite(item) ? item.EntityType == typeName :
      isAutocompleteConstructor(item) ? getTypeName(item.type) == typeName :
        false;
}

  getSortByString(item: Lite<T> | AutocompleteConstructor<T>): string {
    return isLite(item) ? getToString(item)! :
      isAutocompleteConstructor(item) ? getTypeName(item.type) :
        "";
  }
}

//Usefull to make a MultiFindOptions autocomplete using 
export async function getLitesWithSubStr(fo: FindOptions, subStr: string, signal: AbortSignal): Promise<Lite<Entity>[]> {

  const foClean = Finder.defaultNoColumnsAllRows(fo, 5);

  const qd = await Finder.getQueryDescription(fo.queryName);
  const qs = Finder.getSettings(fo.queryName);

  const fop = await Finder.parseFindOptions({
    ...fo,
    orderOptions: [
      { token: "Entity.ToString.Length", orderType: "Ascending" },
      { token: "Entity.ToString", orderType: "Ascending" },
    ],
    filterOptions: FindOptionsAutocompleteConfig.filtersWithSubStr(fo, qd, qs, subStr),
    includeDefaultFilters: false,
  }, qd, true);

  var qr = Finder.getQueryRequest(fop);

  const rt = await Finder.API.executeQuery(qr, signal);

  return rt.rows.map(a => a.entity).notNull();
}


interface FindOptionsAutocompleteConfigOptions extends AutocompleteConfigOptions {
  getAutocompleteConstructor?: (str: string, foundRows: ResultRow[]) => AutocompleteConstructor<Entity>[],
  count?: number,
  requiresInitialLoad?: boolean,
  showType?: boolean,
  customRenderItem?: (row: ResultRow, table: ResultTable, hl: TextHighlighter) => React.ReactNode;
}

export class FindOptionsAutocompleteConfig implements AutocompleteConfig<ResultRow | AutocompleteConstructor<Entity>>{
  findOptions: FindOptions | ((subStr: string) => FindOptions);
  getAutocompleteConstructor?: (str: string, foundRows: ResultRow[]) => AutocompleteConstructor<Entity>[];
  requiresInitialLoad?: boolean;
  showType?: boolean;
  count?: number;
  customRenderItem?: (row: ResultRow, table: ResultTable, hl: TextHighlighter) => React.ReactNode;
  itemsDelay?: number;
  minLength?: number;

  constructor(
    findOptions: FindOptions | ((subStr: string) => FindOptions),
    options?: FindOptionsAutocompleteConfigOptions,
  ) {
    this.findOptions = findOptions;

    Dic.assign(this, options);
  }

  getItemsDelay(): number | undefined {
    return this.itemsDelay;
  }

  getMinLength(): number | undefined {
    return this.minLength;
  }

  abort(): void {
    this.abortableRequest.abort();
  }

  abortableRequest: AbortableRequest<QueryRequest, ResultTable> = new AbortableRequest((abortController, request: QueryRequest) => Finder.API.executeQuery(request, abortController));

  static filtersWithSubStr(fo: FindOptions, qd: QueryDescription, qs: Finder.QuerySettings | undefined, subStr: string): FilterOption[] {

    var filters = [...fo.filterOptions?.notNull() ?? []];

    /*When overriden in Finder very often uses not seen columns (like Telephone) that are not seen in autocomplete, better to use false by default and you can opt-in by adding includeDefaultFilters if needed */
    if (fo.includeDefaultFilters ?? false) {
      var defaultFilters = Finder.getDefaultFilter(qd, qs);
      if (defaultFilters)
        filters = [...defaultFilters, ...filters];
    }

    var lites = parseLiteList(subStr);
    if (lites.length > 0) {
      const tis = getTypeInfos(qd.columns["Entity"].type);
      lites = lites.filter(lite => tis.singleOrNull(ti => ti.name == lite.EntityType) != null);
        filters.insertAt(0, {
        token: "Entity",
        operation: lites.length == 0 ? "EqualTo" : "IsIn",
        value: lites.length == 0 ? null : lites,
        });

      return filters;
    }

    if (/^id[: ]/.test(subStr)) {

      var id = subStr.substr(3)?.trim();

      filters.insertAt(0, {
        token: "Entity.Id",
        operation: "EqualTo",
        value: id 
      });
      return filters;
    }

    var searchBox = filters.firstOrNull(a => a.pinned != null && a.pinned.splitValue == true);

    if (searchBox == null) {
      filters.insertAt(0, {
        groupOperation: "Or",
        pinned: { label: SearchMessage.Search.niceToString(), splitValue: true, active: "WhenHasValue" },
        filters: [
          { token: "Entity.ToString", operation: "Contains" },
          { token: "Entity.Id", operation: "EqualTo" },
        ],
        value: subStr
      });
    } else {
      filters[filters.indexOf(searchBox)] = { ...searchBox, value: subStr }
    }


    return filters;
  }

  resultTable: ResultTable | undefined; 

  async getItems(subStr: string): Promise<(ResultRow | AutocompleteConstructor<Entity>)[]> {

    var fo = Finder.defaultNoColumnsAllRows(typeof this.findOptions == "object" ? this.findOptions : this.findOptions(subStr), this.count ?? 5);
    const qs = Finder.getSettings(fo.queryName);

    return Finder.getQueryDescription(fo.queryName)
      .then(qd => Finder.parseFindOptions({
        orderOptions: [
          { token: "Entity.ToString.Length", orderType: "Ascending" },
          { token: "Entity.ToString", orderType: "Ascending" },
        ],
        ...fo,
        filterOptions: FindOptionsAutocompleteConfig.filtersWithSubStr(fo, qd, qs, subStr),
      }, qd, true))
      .then(fop => this.abortableRequest.getData(Finder.getQueryRequest(fop)))
      .then(rt => {
        this.resultTable = this.resultTable;
        return [
          ...rt.rows,
          ...(this.getAutocompleteConstructor && this.getAutocompleteConstructor(subStr, rt.rows)) ?? []
        ]
      });
  }

  renderItem(item: ResultRow | AutocompleteConstructor<Entity>, hl: TextHighlighter): React.ReactNode {
    if (isAutocompleteConstructor<Entity>(item)) {

      if (item.customElement)
        return item.customElement;

      var ti = getTypeInfo(item.type);
      return <em>{SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(ti.gender).formatWith(ti.niceName)} "{hl.query}"</em>;
    }

    if (this.customRenderItem)
      return this.customRenderItem(item, this.resultTable!, hl);

    var toStr = getToString(item.entity!);
    var html = Navigator.renderLite(item.entity!, hl);
    if (this.showType)
      return <span title={toStr}>{html}<TypeBadge entity={item.entity!} /></span>;
    else
      return html;
  }

  getEntityFromItem(item: ResultRow | AutocompleteConstructor<Entity>): Promise<Lite<Entity> | ModifiableEntity | undefined> {
    if (isAutocompleteConstructor(item))
      return item.onClick() as Promise<Lite<Entity> | ModifiableEntity | undefined>;

    return Promise.resolve(item.entity!);
  }

  getDataKeyFromItem(item: ResultRow | AutocompleteConstructor<Entity>): string | undefined {
    if (isAutocompleteConstructor(item))
      return "create-" + getTypeName(item.type);

    return liteKey(item.entity!);
  }

  getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<ResultRow> {

    var lite = this.convertToLite(entity);;

    if (!(this.requiresInitialLoad))
      return Promise.resolve({ entity: lite } as ResultRow);

    if (lite.id == undefined)
      return Promise.resolve({ entity: lite } as ResultRow);

    var fo = Finder.defaultNoColumnsAllRows(typeof this.findOptions == "object" ? this.findOptions : this.findOptions(""), 1);

    fo = {
      ...fo,
      filterOptions: [{ token: QueryTokenString.entity<Entity>().append(e => e.id), operation: "EqualTo", value: lite.id }],
    };

    return Finder.getQueryDescription(fo.queryName)
      .then(qd => Finder.parseFindOptions(fo, qd, false)
        .then(fop => Finder.API.executeQuery(Finder.getQueryRequest(fop)))
        .then(rt => {
          const result = rt.rows.filter(row => is(row.entity, lite)).firstOrNull();

          if (!result)
            throw new Error("Impossible to getInitialItem with the current implementation of getItems");

          return result;
        })
      );
  }

  convertToLite(entity: Lite<Entity> | ModifiableEntity): Lite<Entity> {

    if (isLite(entity))
      return entity;

    if (isEntity(entity))
      return toLite(entity, entity.isNew);

    throw new Error("Impossible to convert to Lite");
  }

  isCompatible(item: unknown, typeName: string): item is ResultRow | AutocompleteConstructor<Entity> {
    return isResultRow(item) ? item.entity?.EntityType == typeName :
      isAutocompleteConstructor(item) ? getTypeName(item.type) == typeName :
        false;
  }

  getSortByString(item: ResultRow | AutocompleteConstructor<Entity>): string {
    return isResultRow(item) ? getToString(item.entity)! :
      isAutocompleteConstructor(item) ? getTypeName(item.type) :
        "";
  }
}

export function TypeBadge(p: { entity: Lite<Entity> | ModifiableEntity }): React.ReactElement {

  var typeName = isEntity(p.entity) ? p.entity.Type :
    isLite(p.entity) ? p.entity.EntityType :
      null;

  if (typeName == null)
    return <span className="text-danger">Embedded?</span>;

  const ti = getTypeInfo(typeName);

  return <span className="sf-type-badge ms-1">{ti.niceName}</span>;
}

export class MultiAutoCompleteConfig implements AutocompleteConfig<unknown>{

  implementations: { [typeName: string]: AutocompleteConfig<unknown> };
  limit: number;
  constructor(implementations: { [typeName: string]: AutocompleteConfig<unknown> }, limit: number = 5) {
    this.implementations = implementations;
    this.limit = limit;
  }


  async getItems(subStr: string): Promise<unknown[]> {
    var items = await Promise.all(Object.values(this.implementations).map(a => a.getItems(subStr)));
    var acc = items.flatMap(r => r).orderBy(item => {
      for (var type in this.implementations) {
        var acc = this.implementations[type];
        if (acc.isCompatible(item, type))
          return acc.getSortByString(item);
      }
    });

    return [
      ...acc.filter(item => !isAutocompleteConstructor(item)).slice(0, this.limit),
      ...acc.filter(item => isAutocompleteConstructor(item))
    ];
  }

  getItemsDelay(): number | undefined {
    return Object.values(this.implementations).map(a => a.getItemsDelay()).notNull().max() ?? undefined;
  }

  getMinLength(): number | undefined {
    return Object.values(this.implementations).map(a => a.getMinLength()).notNull().max() ?? undefined;
  }

  renderItem(item: unknown, hl: TextHighlighter): React.ReactNode {
    for (var type in this.implementations) {
      var acc = this.implementations[type];
      if (acc.isCompatible(item, type))
        return acc.renderItem(item, hl);
    }

    if (isLite(item))
      return Navigator.renderLite(item, hl);

    throw new Error("Unexpected " + JSON.stringify(item));
  }
  getEntityFromItem(item: unknown): Promise<ModifiableEntity | Lite<Entity> | undefined> {
    for (var type in this.implementations) {
      var acc = this.implementations[type];
      if (acc.isCompatible(item, type))
        return acc.getEntityFromItem(item);
    }

    if (isLite(item))
      return Promise.resolve(item);

    throw new Error("Unexpected " + JSON.stringify(item));
  }
  getDataKeyFromItem(item: unknown): string | undefined {
    for (var type in this.implementations) {
      var acc = this.implementations[type];
      if (acc.isCompatible(item, type))
        return acc.getDataKeyFromItem(item);
    }

    if (isLite(item))
      return liteKey(item);

    throw new Error("Unexpected " + JSON.stringify(item));
  }
  getItemFromEntity(entity: ModifiableEntity | Lite<Entity>): Promise<unknown> {

    var type = isLite(entity) ? entity.EntityType : entity.Type;

    var acc = this.implementations[type];
    if (acc != null)
      return acc.getItemFromEntity(entity);

    if (isLite(entity))
      return Promise.resolve(entity);

    if (isEntity(entity))
      return Promise.resolve(toLite(entity, entity.isNew));

    throw new Error("Unexpected " + type);
  }

  abort(): void {
    Dic.foreach(this.implementations, (key, acc) => acc.abort());
  }

  isCompatible(item: unknown, type: string): item is unknown {
    return Object.values(this.implementations).some(a => a.isCompatible(item, type));
  }

  getSortByString(item: unknown): string {
    for (var type in this.implementations) {
      var acc = this.implementations[type];
      if (acc.isCompatible(item, type))
        return acc.getSortByString(item);
    }

    throw new Error("Unexpected " + JSON.stringify(item));
  }
}
