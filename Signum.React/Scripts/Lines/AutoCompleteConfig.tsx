import * as React from 'react'
import * as Finder from '../Finder'
import { AbortableRequest } from '../Services'
import { FindOptions, FilterOptionParsed, OrderOptionParsed, OrderRequest, ResultRow, ColumnOptionParsed, ColumnRequest, QueryDescription, QueryRequest, FilterOption, ResultTable } from '../FindOptions'
import { getTypeInfo, getQueryKey, QueryTokenString, getTypeName } from '../Reflection'
import { ModifiableEntity, Lite, Entity, toLite, is, isLite, isEntity, getToString, liteKey, SearchMessage } from '../Signum.Entities'
import { toFilterRequests } from '../Finder';
import { TypeaheadController, TypeaheadOptions } from '../Components/Typeahead'
import { AutocompleteConstructor, getAutocompleteConstructors } from '../Navigator';
import { Dic } from '../Globals'

export interface AutocompleteConfig<T> {
  getItems: (subStr: string) => Promise<T[]>;
  getItemsDelay?: number;
  minLength?: number;
  renderItem(item: T, subStr?: string): React.ReactNode;
  renderList?(typeahead: TypeaheadController): React.ReactNode;
  getEntityFromItem(item: T): Promise<Lite<Entity> | ModifiableEntity | undefined>;
  getDataKeyFromItem(item: T): string | undefined;
  getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<T>;
  abort(): void;
}

export interface AutocompleteConfigOptions {
  getItemsDelay?: number;
  minLength?: number;
}

export interface LiteAutocomplateConfigOptions extends AutocompleteConfigOptions {
  requiresInitialLoad?: boolean,
  showType?: boolean
}

export function isAutocompleteConstructor<T extends ModifiableEntity>(a: any): a is AutocompleteConstructor<T> {
  return (a as AutocompleteConstructor<T>).onClick != null;
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

  abortableRequest = new AbortableRequest((signal, subStr: string) => this.getItemsFunction(signal, subStr));

  abort() {
    this.abortableRequest.abort();
  }

  getItems(subStr: string) {
    return this.abortableRequest.getData(subStr);
  }

  renderItem(item: Lite<T> | AutocompleteConstructor<T>, subStr: string): React.ReactNode{

    if (isAutocompleteConstructor<T>(item)) {
      var ti = getTypeInfo(item.type);
      return <em>{SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(ti.gender).formatWith(ti.niceName)} "{subStr}"</em>;
    }

    var toStr = getToString(item);
    var text = TypeaheadOptions.highlightedTextAll(toStr, subStr);
    if (this.showType)
      return <span style={{ wordBreak: "break-all" }} title={toStr}><span className="sf-type-badge">{getTypeInfo(item.EntityType).niceName}</span> {text}</span>;
    else
      return text;
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

    throw new Error("Impossible to convert to Lite");
  }
}

interface FindOptionsAutocompleteConfigOptions extends AutocompleteConfigOptions {
  getAutocompleteConstructor?: (str: string, foundRows: ResultRow[]) => AutocompleteConstructor<Entity>[],
  count?: number,
  requiresInitialLoad?: boolean,
  showType?: boolean,
  customRenderItem?: (row: ResultRow, table: ResultTable, subStr: string) => React.ReactNode;
}

export class FindOptionsAutocompleteConfig implements AutocompleteConfig<ResultRow | AutocompleteConstructor<Entity>>{
  getAutocompleteConstructor?: (str: string, foundRows: ResultRow[]) => AutocompleteConstructor<Entity>[];
  requiresInitialLoad?: boolean;
  showType?: boolean;
  count?: number;
  customRenderItem?: (row: ResultRow, table: ResultTable, subStr: string) => React.ReactNode;

  constructor(
    public findOptions: FindOptions | ((subStr: string) => FindOptions),
    options?: FindOptionsAutocompleteConfigOptions,
  ) {
    if (typeof this.findOptions == "object")
      Finder.expandParentColumn(this.findOptions);

    Dic.assign(this, options);
  }

  abort() {
    this.abortableRequest.abort();
  }

  abortableRequest = new AbortableRequest((abortController, request: QueryRequest) => Finder.API.executeQuery(request, abortController));


  static filtersWithSubStr(fo: FindOptions, qd: QueryDescription, qs: Finder.QuerySettings | undefined, subStr: string): FilterOption[] {

    var filters = [...fo.filterOptions ?? []];

    /*When overriden in Finder very often uses not seen columns (like Telephone) that are not seen in autocomplete, better to use false by default and you can opt-in by adding includeDefaultFilters if needed */
    if (fo.includeDefaultFilters ?? false) {
      var defaultFilters = Finder.getDefaultFilter(qd, qs);
      if (defaultFilters)
        filters = [...defaultFilters, ...filters];
    }

    if (/^id[: ]/.test(subStr)) {
      filters.insertAt(0, {
        token: "Entity.Id",
        operation: "EqualTo",
        value: subStr.substr(3)
      });
      return filters;
    }

    var searchBox = filters.firstOrNull(a => a.pinned != null && a.pinned.splitText == true);

    if (searchBox == null) {
      filters.insertAt(0, {
        groupOperation: "Or",
        pinned: { label: SearchMessage.Search.niceToString(), splitText: true, active: "WhenHasValue" },
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

    var fo = typeof this.findOptions == "object" ? this.findOptions : this.findOptions(subStr);
    const qs = Finder.getSettings(fo.queryName);

    return Finder.getQueryDescription(fo.queryName)
      .then(qd => Finder.parseFindOptions({
        columnOptionsMode: "Replace",
        columnOptions: [],
        pagination: { mode: "Firsts", elementsPerPage: this.count ?? 5 },
        ...fo,
        filterOptions: FindOptionsAutocompleteConfig.filtersWithSubStr(fo, qd, qs, subStr),
        includeDefaultFilters: false,
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

  renderItem(item: ResultRow | AutocompleteConstructor<Entity>, subStr: string): React.ReactNode {
    if (isAutocompleteConstructor<Entity>(item)) {
      var ti = getTypeInfo(item.type);
      return <em>{SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(ti.gender).formatWith(ti.niceName)} "{subStr}"</em>;
    }

    if (this.customRenderItem)
      return this.customRenderItem(item, this.resultTable!, subStr);

    var toStr = getToString(item.entity!);
    var text = TypeaheadOptions.highlightedTextAll(toStr, subStr);
    if (this.showType)
      return <span style={{ wordBreak: "break-all" }} title={toStr}><span className="sf-type-badge">{getTypeInfo(item.entity!.EntityType).niceName}</span> {text}</span>;
    else
      return text;
  }

  getEntityFromItem(item: ResultRow): Promise<Lite<Entity> | ModifiableEntity | undefined> {
    if (isAutocompleteConstructor(item))
      return item.onClick() as Promise<Lite<Entity> | ModifiableEntity | undefined>;

    return Promise.resolve(item.entity!);
  }

  getDataKeyFromItem(item: ResultRow): string | undefined {
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

    var fo = typeof this.findOptions == "object" ? this.findOptions : this.findOptions("");

    fo = {
      ...fo,
      filterOptions: [{ token: QueryTokenString.entity<Entity>().append(e => e.id), operation: "EqualTo", value: lite.id }],
      pagination: { mode: "Firsts", elementsPerPage: 1 }
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

  convertToLite(entity: Lite<Entity> | ModifiableEntity) {

    if (isLite(entity))
      return entity;

    if (isEntity(entity))
      return toLite(entity, entity.isNew);

    throw new Error("Impossible to convert to Lite");
  }
}
