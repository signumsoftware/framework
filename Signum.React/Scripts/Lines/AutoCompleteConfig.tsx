import * as React from 'react'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import { Dic } from '../Globals'
import { AbortableRequest } from '../Services'
import { FindOptions, QueryDescription, FilterOptionParsed, FilterRequest, OrderOptionParsed, OrderRequest } from '../FindOptions'
import { TypeContext, StyleContext, StyleOptions, FormGroupStyle } from '../TypeContext'
import { PropertyRoute, PropertyRouteType, MemberInfo, getTypeInfo, getTypeInfos, TypeInfo, IsByAll, getQueryKey } from '../Reflection'
import { LineBase, LineBaseProps, runTasks } from '../Lines/LineBase'
import { ModifiableEntity, Lite, Entity, EntityControlMessage, JavascriptMessage, toLite, is, liteKey, getToString, isLite, isEntity } from '../Signum.Entities'
import { Typeahead } from '../Components'
import { EntityBase, EntityBaseProps} from './EntityBase'

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
        public withCustomToString: boolean) {
    }

    abortableRequest = new AbortableRequest((abortController, subStr: string) => this.getItemsFunction(abortController, subStr));

    abort() {
        this.abortableRequest.abort();
    }
    
    getItems(subStr: string) {
        return this.abortableRequest.getData(subStr);
    }

    renderItem(item: Lite<T>, subStr: string) {
        return Typeahead.highlightedText(item.toStr || "", subStr)
    }

    getEntityFromItem(item: Lite<T>) {
        return item;
    }

    getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<Lite<T>> {

        var lite = this.convertToLite(entity);;

        if (!this.withCustomToString)
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

export class FindOptionsAutocompleteConfig implements AutocompleteConfig<Lite<Entity>>{

    constructor(
        public findOptions: FindOptions,
        public count: number = 5,
        public withCustomToString: boolean = false) {

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

    abortableRequest = new AbortableRequest((abortController, request: Finder.API.AutocompleteQueryRequest) => Finder.API.findLiteLikeWithFilters(request, abortController));

    getItems(subStr: string): Promise<Lite<Entity>[]> {
        return this.getParsedFilters()
            .then(filters =>
                this.getParsedOrders().then(orders =>
                    this.abortableRequest.getData({
                        queryKey: getQueryKey(this.findOptions.queryName),
                        filters: filters.map(f => ({ token: f.token!.fullKey, operation: f.operation, value: f.value }) as FilterRequest),
                        orders: orders.map(f => ({ token: f.token!.fullKey, orderType: f.orderType }) as OrderRequest),
                        count: this.count,
                        subString: subStr
                    })
                )
            );
    }

    renderItem(item: Lite<Entity>, subStr: string) {
        return Typeahead.highlightedText(item.toStr || "", subStr)
    }

    getEntityFromItem(item: Lite<Entity>) {
        return item;
    }

    getItemFromEntity(entity: Lite<Entity> | ModifiableEntity): Promise<Lite<Entity>> {

        var lite = this.convertToLite(entity);;

        if (!this.withCustomToString)
            return Promise.resolve(lite);

        if (lite.id == undefined)
            return Promise.resolve(lite);

        return Finder.API.findLiteLikeWithFilters({
            queryKey: getQueryKey(this.findOptions.queryName),
            filters: [{ token: "Entity.Id", operation: "EqualTo", value: lite.id }],
            orders: [],
            count: 1,
            subString: ""
        }).then(lites => {

            const result = lites.filter(a => is(a, lite)).firstOrNull();

            if (!result)
                throw new Error("Impossible to getInitialItem with the current implementation of getItems");

            return result;
        });
    }

    convertToLite(entity: Lite<Entity> | ModifiableEntity) {

        if (isLite(entity))
            return entity;

        if (isEntity(entity))
            return toLite(entity, entity.isNew);

        throw new Error("Impossible to convert to Lite");
    }
}




