import * as React from 'react'
import * as Finder from '../Finder'
import { AbortableRequest } from '../Services'
import { FindOptions, FilterOptionParsed, OrderOptionParsed, OrderRequest, ResultRow, ColumnOptionParsed, ColumnRequest, QueryDescription } from '../FindOptions'
import { getTypeInfo, getQueryKey, QueryTokenString, getTypeName } from '../Reflection'
import { ModifiableEntity, Lite, Entity, toLite, is, isLite, isEntity, getToString, liteKey, SearchMessage } from '../Signum.Entities'
import { toFilterRequests } from '../Finder';
import { TypeaheadHandle, TypeaheadOptions } from '../Components/Typeahead'
import { AutocompleteConstructor, getAutocompleteConstructors } from '../Navigator';

export interface AutocompleteConfig<T> {
  getItems: (subStr: string) => Promise<T[]>;
  getItemsDelay?: number;
  minLength?: number;
  renderItem(item: T, subStr?: string): React.ReactNode;
  renderList?(typeahead: TypeaheadHandle): React.ReactNode;
  getEntityFromItem(item: T): Promise<Lite<Entity> | ModifiableEntity | undefined>;
  getDataKeyFromItem(item: T): string | undefined;
  getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<T>;
  abort(): void;
}

export function isAutocompleteConstructor<T extends ModifiableEntity>(a: any): a is AutocompleteConstructor<T> {
  return (a as AutocompleteConstructor<T>).onClick != null;
}

export class LiteAutocompleteConfig<T extends Entity> implements AutocompleteConfig<Lite<T> | AutocompleteConstructor<T>>{
  constructor(
    public getItemsFunction: (signal: AbortSignal, subStr: string) => Promise<(Lite<T> | AutocompleteConstructor<T>)[]>,
    public requiresInitialLoad: boolean,
    public showType: boolean) {
  }

  abortableRequest = new AbortableRequest((signal, subStr: string) => this.getItemsFunction(signal, subStr));

  abort() {
    this.abortableRequest.abort();
  }

  getItems(subStr: string) {
    return this.abortableRequest.getData(subStr);
  }

  renderItem(item: Lite<T> | AutocompleteConstructor<T>, subStr: string) {

    if (isAutocompleteConstructor<T>(item)) {
      var ti = getTypeInfo(item.type);
      return <em>{SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(ti.gender).formatWith(ti.niceName)} "{subStr}"</em>;
    }

    var toStr = getToString(item);
    var text = TypeaheadOptions.highlightedText(toStr, subStr);
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

interface FindOptionsAutocompleteConfigOptions {
  getAutocompleteConstructor?: (str: string, foundRows: ResultRow[]) => AutocompleteConstructor<Entity>[],
  count?: number,
  requiresInitialLoad?: boolean,
  showType?: boolean,
}

export class FindOptionsAutocompleteConfig implements AutocompleteConfig<ResultRow | AutocompleteConstructor<Entity>>{

  constructor(
    public findOptions: FindOptions,
    public options?: FindOptionsAutocompleteConfigOptions
  ) {
    Finder.expandParentColumn(this.findOptions);
  }

  abort() {
    this.abortableRequest.abort();
  }

  parsedFilters?: FilterOptionParsed[];
  getParsedFilters(qd: QueryDescription): Promise<FilterOptionParsed[]> {
    if (this.parsedFilters)
      return Promise.resolve(this.parsedFilters);

    return Finder.parseFilterOptions(this.findOptions.filterOptions || [], false, qd)
      .then(filters => this.parsedFilters = filters);
  }

  parsedOrders?: OrderOptionParsed[];
  getParsedOrders(qd: QueryDescription): Promise<OrderOptionParsed[]> {
    if (this.parsedOrders)
      return Promise.resolve(this.parsedOrders);

    return  Finder.parseOrderOptions(this.findOptions.orderOptions || [], false, qd)
      .then(orders => this.parsedOrders = orders);
  }

  parsedColumns?: ColumnOptionParsed[];
  getParsedColumns(qd: QueryDescription): Promise<ColumnOptionParsed[]> {
    if (this.parsedColumns)
      return Promise.resolve(this.parsedColumns);

    return Finder.parseColumnOptions(this.findOptions.columnOptions || [], false, qd)
      .then(columns => this.parsedColumns = columns);
  }

  abortableRequest = new AbortableRequest((abortController, request: Finder.API.AutocompleteQueryRequest) => Finder.API.FindRowsLike(request, abortController));

  async getItems(subStr: string): Promise<(ResultRow | AutocompleteConstructor<Entity>)[]> {

    return Finder.getQueryDescription(this.findOptions.queryName)
      .then(qd => Promise.all(
        [
          this.getParsedFilters(qd),
          this.getParsedOrders(qd),
          this.getParsedColumns(qd)
        ]).then(([filters, orders, columns]) =>
          this.abortableRequest.getData({
            queryKey: getQueryKey(this.findOptions.queryName),
            columns: columns.map(c => ({ token: c.token!.fullKey, displayName: c.displayName }) as ColumnRequest),
            filters: toFilterRequests(filters),
            orders: orders.map(o => ({ token: o.token!.fullKey, orderType: o.orderType }) as OrderRequest),
            count: this.options && this.options.count || 5,
            subString: subStr
          }).then(rt => [
            ...rt.rows,
            ...this.options && this.options.getAutocompleteConstructor && this.options.getAutocompleteConstructor(subStr, rt.rows) || []
          ])
        )
      );
  }

  renderItem(item: ResultRow | AutocompleteConstructor<Entity>, subStr: string) {
    if (isAutocompleteConstructor<Entity>(item)) {
      var ti = getTypeInfo(item.type);
      return <em>{SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(ti.gender).formatWith(ti.niceName)} "{subStr}"</em>;
    }

    var toStr = getToString(item.entity!);
    var text = TypeaheadOptions.highlightedText(toStr, subStr);
    if (this.options && this.options.showType)
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

    if (!(this.options && this.options.requiresInitialLoad))
      return Promise.resolve({ entity: lite } as ResultRow);

    if (lite.id == undefined)
      return Promise.resolve({ entity: lite } as ResultRow);

    return Finder.getQueryDescription(this.findOptions.queryName).then(qd => this.getParsedColumns(qd)).then(columns =>

      Finder.API.FindRowsLike({
        queryKey: getQueryKey(this.findOptions.queryName),
        columns: columns.map(c => ({ token: c.token!.fullKey, displayName: c.displayName }) as ColumnRequest),
        filters: [{ token: QueryTokenString.entity<Entity>().append(e => e.id).toString(), operation: "EqualTo", value: lite.id }],
        orders: [],
        count: 1,
        subString: ""
      }).then(rt => {
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
