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
    allowChangeColumns?: boolean;
    create?: boolean;
    navigate?: boolean;
    contextMenu?: boolean;
}

export interface FilterOption {
    columnName: string;
    token?: QueryToken;
    frozen?: boolean;
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

export enum SubTokensOptions {
    CanAggregate = 1,
    CanAnyAll = 2,
    CanElement = 4,
}

export interface QueryToken {
    toString: string;
    niceName: string;
    key: string;
    format?: string;
    unit?: string;
    type: TypeReference;
    typeColor: string;
    niceTypeName: string;
    filterType: FilterType;
    fullKey: string;
    hasAllOrAny?: boolean;
    parent?: QueryToken;
}

export function toQueryToken(cd: ColumnDescription): QueryToken {
    return {
        toString: cd.displayName,
        niceName: cd.displayName,
        key: cd.name,
        fullKey: cd.name,
        unit: cd.unit,
        format: cd.format,
        type: cd.type,
        typeColor: cd.typeColor,
        niceTypeName: cd.niceTypeName,
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
        return Math.ceil(totalElements / p.elementsPerPage); //Round up
    }

    export function maxElementIndex(p: Pagination) {
        return (p.elementsPerPage * (p.currentPage + 1)) - 1;
    }
}





export interface QueryDescription {
    queryKey: string;
    columns: { [name: string]: ColumnDescription };
}

export interface ColumnDescription {
    name: string;
    type: TypeReference;
    filterType: FilterType;
    typeColor: string;
    niceTypeName: string;
    unit?: string;
    format?: string;
    displayName: string;
}



export var filterOperations: { [a: string]: FilterOperation[] } = {};
filterOperations[FilterType.String as any] = [
    FilterOperation.Contains,
    FilterOperation.EqualTo,
    FilterOperation.StartsWith,
    FilterOperation.EndsWith,
    FilterOperation.Like,
    FilterOperation.NotContains,
    FilterOperation.DistinctTo,
    FilterOperation.NotStartsWith,
    FilterOperation.NotEndsWith,
    FilterOperation.NotLike,
    FilterOperation.IsIn
];

filterOperations[FilterType.DateTime as any] = [
    FilterOperation.EqualTo,
    FilterOperation.DistinctTo,
    FilterOperation.GreaterThan,
    FilterOperation.GreaterThanOrEqual,
    FilterOperation.LessThan,
    FilterOperation.LessThanOrEqual,
    FilterOperation.IsIn
];

filterOperations[FilterType.Integer as any] = [
    FilterOperation.EqualTo,
    FilterOperation.DistinctTo,
    FilterOperation.GreaterThan,
    FilterOperation.GreaterThanOrEqual,
    FilterOperation.LessThan,
    FilterOperation.LessThanOrEqual,
    FilterOperation.IsIn
];

filterOperations[FilterType.Decimal as any] = [
    FilterOperation.EqualTo,
    FilterOperation.DistinctTo,
    FilterOperation.GreaterThan,
    FilterOperation.GreaterThanOrEqual,
    FilterOperation.LessThan,
    FilterOperation.LessThanOrEqual,
    FilterOperation.IsIn
];

filterOperations[FilterType.Enum as any] = [
    FilterOperation.EqualTo,
    FilterOperation.DistinctTo,
    FilterOperation.IsIn
];

filterOperations[FilterType.Guid as any] = [
    FilterOperation.EqualTo,
    FilterOperation.DistinctTo,
    FilterOperation.IsIn
];

filterOperations[FilterType.Lite as any] = [
    FilterOperation.EqualTo,
    FilterOperation.DistinctTo,
    FilterOperation.IsIn
];

filterOperations[FilterType.Embedded as any] = [
    FilterOperation.EqualTo,
    FilterOperation.DistinctTo,
];

filterOperations[FilterType.Boolean as any] = [
    FilterOperation.EqualTo,
    FilterOperation.DistinctTo,
];
