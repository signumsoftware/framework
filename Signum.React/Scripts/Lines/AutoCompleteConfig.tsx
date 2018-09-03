import * as React from 'react'
import * as Finder from '../Finder'
import { AbortableRequest } from '../Services'
import { FindOptions, FilterOptionParsed, FilterRequest, OrderOptionParsed, OrderRequest, ResultRow, ColumnOptionParsed, ColumnRequest } from '../FindOptions'
import { getTypeInfo, getQueryKey } from '../Reflection'
import { ModifiableEntity, Lite, Entity, toLite, is, isLite, isEntity, getToString } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { toFilterRequest, toFilterRequests } from '../Finder';

export interface AutocompleteConfig<T> {
    getItems: (subStr: string) => Promise<T[]>;
    getItemsDelay?: number;
    minLength?: number;
    renderItem(item: T, subStr?: string) : React.ReactNode;
    renderList?(typeahead: Typeahead): React.ReactNode;
    getEntityFromItem(item: T) : Lite<Entity> | ModifiableEntity;
    getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<T>;
    abort(): void;
}

export class LiteAutocompleteConfig<T extends Entity> implements AutocompleteConfig<Lite<T>>{

    constructor(
        public getItemsFunction: (abortController: FetchAbortController, subStr: string) => Promise<Lite<T>[]>,
        public requiresInitialLoad: boolean,
        public showType: boolean) {
    }

    abortableRequest = new AbortableRequest((abortController, subStr: string) => this.getItemsFunction(abortController, subStr));

    abort() {
        this.abortableRequest.abort();
    }
    
    getItems(subStr: string) {
        return this.abortableRequest.getData(subStr);
    }

    renderItem(item: Lite<T>, subStr: string) {
        var text = Typeahead.highlightedText(getToString(item), subStr);

        if (this.showType)
            return <span><span className="sf-type-badge">{getTypeInfo(item.EntityType).niceName}</span> {text}</span>;
        else
            return text;
    }

    getEntityFromItem(item: Lite<T>) {
        return item;
    }

    getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<Lite<T>> {

        var lite = this.convertToLite(entity);;

        if (!this.requiresInitialLoad)
            return Promise.resolve(lite);

        if (lite.id == undefined)
            return Promise.resolve(lite);

        return this.abortableRequest.getData(lite.id!.toString()).then(lites => {

            const result = lites.filter(a => is(a, lite)).firstOrNull();

            if (!result)
                throw new Error("Impossible to getInitialItem with the current implementation of getItems");

            return result;
        });
    }

    convertToLite(entity: Lite<Entity> | ModifiableEntity) : Lite<T> {
        
        if (isLite(entity))
            return entity as Lite<T>;

        if (isEntity(entity))
            return toLite(entity, entity.isNew) as Lite<T>;

        throw new Error("Impossible to convert to Lite");
    }
}

export class FindOptionsAutocompleteConfig implements AutocompleteConfig<ResultRow>{

    constructor(
        public findOptions: FindOptions,
        public count: number = 5,
        public requiresInitialLoad: boolean = false,
        public showType: boolean = false,
    ) {
        Finder.expandParentColumn(this.findOptions);
    }

    abort() {
        this.abortableRequest.abort();
    }

    parsedFilters?: FilterOptionParsed[];
    getParsedFilters(): Promise<FilterOptionParsed[]> {
        if (this.parsedFilters)
            return Promise.resolve(this.parsedFilters);

        return Finder.getQueryDescription(this.findOptions.queryName)
            .then(qd => Finder.parseFilterOptions(this.findOptions.filterOptions || [], false, qd))
            .then(filters => this.parsedFilters = filters);
    }

    parsedOrders?: OrderOptionParsed[];
    getParsedOrders(): Promise<OrderOptionParsed[]> {
        if (this.parsedOrders)
            return Promise.resolve(this.parsedOrders);

        return Finder.getQueryDescription(this.findOptions.queryName)
            .then(qd => Finder.parseOrderOptions(this.findOptions.orderOptions || [], false, qd))
            .then(orders => this.parsedOrders = orders);
    }

    parsedColumns?: ColumnOptionParsed[];
    getParsedColumns(): Promise<ColumnOptionParsed[]> {
        if (this.parsedColumns)
            return Promise.resolve(this.parsedColumns);

        return Finder.getQueryDescription(this.findOptions.queryName)
            .then(qd => Finder.parseColumnOptions(this.findOptions.columnOptions || [], false, qd))
            .then(columns => this.parsedColumns = columns);
    }

    abortableRequest = new AbortableRequest((abortController, request: Finder.API.AutocompleteQueryRequest) => Finder.API.FindRowsLike(request, abortController));

    getItems(subStr: string): Promise<ResultRow[]> {
        return this.getParsedFilters().then(filters =>
            this.getParsedOrders().then(orders =>
                this.getParsedColumns().then(columns =>
                    this.abortableRequest.getData({
                        queryKey: getQueryKey(this.findOptions.queryName),
                        columns: columns.map(c => ({ token: c.token!.fullKey, displayName: c.displayName }) as ColumnRequest),
                        filters: toFilterRequests(filters),
                        orders: orders.map(o => ({ token: o.token!.fullKey, orderType: o.orderType }) as OrderRequest),
                        count: this.count,
                        subString: subStr
                    }).then(rt => rt.rows)
                )
            )
        );
    }

    renderItem(item: ResultRow, subStr: string) {
        var text = Typeahead.highlightedText(getToString(item.entity!), subStr);

        if (this.showType)
            return <span><span className="sf-type-badge">{getTypeInfo(item.entity!.EntityType).niceName}</span> {text}</span>;
        else
            return text;
    }

    getEntityFromItem(item: ResultRow): Lite<Entity> | ModifiableEntity {
        return item.entity!;
    }

    getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<ResultRow> {

        var lite = this.convertToLite(entity);;

        if (!this.requiresInitialLoad)
            return Promise.resolve({ entity: lite } as ResultRow);

        if (lite.id == undefined)
            return Promise.resolve({ entity: lite } as ResultRow);

        return this.getParsedColumns().then(columns =>

            Finder.API.FindRowsLike({
                queryKey: getQueryKey(this.findOptions.queryName),
                columns: columns.map(c => ({ token: c.token!.fullKey, displayName: c.displayName }) as ColumnRequest),
                filters: [{ token: "Entity.Id", operation: "EqualTo", value: lite.id }],
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