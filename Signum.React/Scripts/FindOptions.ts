import { TypeReference, PropertyRoute } from 'Framework/Signum.React/Scripts/Reflection';
import { Lite, IEntity, DynamicQuery } from 'Framework/Signum.React/Scripts/Signum.Entities';

import PaginationMode = DynamicQuery.PaginationMode;
import OrderType = DynamicQuery.OrderType;
import FilterOperation = DynamicQuery.FilterOperation;
import FilterType = DynamicQuery.FilterType;
import ColumnOptionsMode = DynamicQuery.ColumnOptionsMode;
import UniqueType = DynamicQuery.UniqueType;

export {PaginationMode, OrderType, FilterOperation, FilterType, ColumnOptionsMode, UniqueType};

export interface CountOptions {
    queryName: any;
    filterOptions?: FilterOption[];
}

export interface FindOptions {
    queryName: any;
    filterOptions?: FilterOption[];
    orderOptions?: OrderOption[];
    columnOptionsMode?: ColumnOptionsMode;
    columnOptions?: ColumnOption[];
    pagination?: Pagination

    searchOnLoad?: boolean;
    showHeader?: boolean;
    showFilters?: boolean;
    showFilterButton?: boolean;
    showFooter?: boolean;
    create?: boolean;
    navigate?: boolean;
    contextMenu?: boolean;
}

export interface FilterOption {
    columnName: string;
    token?: QueryToken;
    frozen?: string;
    operation: FilterOperation;
    value: any;
}




export interface OrderOption {
    columnName: string;
    token?: QueryToken;
    orderType: OrderType;
}


export interface ColumnOption {
    columnName: string;
    token?: QueryToken;
    displayName: string;
}


export var DefaultPagination: Pagination = {
    mode: PaginationMode.Paginate,
    elementsPerPage: 20,
    currentPage: 1
};


export enum FindMode {
    Find = <any>"Find",
    Explore = <any>"Explore"
}





export interface QueryToken {
    toString: string;
    niceName: string;
    key: string;
    format?: string;
    unit?: string;
    type: TypeReference;
    filterType: FilterType;
    fullKey: string;
    hasAllOrAny?: boolean;
}

export function toQueryToken(cd: ColumnDescription): QueryToken {
    return {
        toString: cd.displayName,
        niceName: cd.displayName,
        key: cd.name,
        fullKey: cd.name,
        unit: cd.unit,
        type: cd.type,
        filterType: cd.filterType,
    };
}

export interface QueryRequest {
    queryKey: string;
    filters: { token: string; operation: FilterOperation; value: any }[];
    orders: { token: string; orderType: OrderType }[];
    columns: { token: string; displayName: string }[];
    pagination: Pagination;
}



export interface ResultColumn {
    displayName: string;
    token: QueryToken;
}

export interface ResultTable {
    queryKey: string;
    entityColumn: string;
    columns: string[];
    rows: ResultRow[];
    pagination: Pagination
    totalElements: number;
}


export interface ResultRow {
    entity: Lite<IEntity>;
    columns: any[];
}

export interface Pagination {
    mode: PaginationMode;
    elementsPerPage?: number;
    currentPage?: number;
}

export module PaginateMath {
    export function startElementIndex(p: Pagination) {
        return (p.elementsPerPage * (p.currentPage - 1)) + 1;
    }

    export function endElementIndex(p: Pagination, rows: number) {
        return startElementIndex(p) + rows - 1;
    }

    export function totalPages(p: Pagination, totalElements: number) {
        return (totalElements + p.elementsPerPage - 1) / p.elementsPerPage; //Round up
    }

    export function maxElementIndex(p: Pagination) {
        return (p.elementsPerPage * (p.currentPage + 1)) - 1;
    }
}





export interface QueryDescription {
    queryKey: any;
    columns: { [name: string]: ColumnDescription };
}

export interface ColumnDescription {
    name: string;
    type: TypeReference;
    filterType: FilterType;
    unit?: string;
    format?: string;
    displayName: string;
}

