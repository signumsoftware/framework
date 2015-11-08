export interface CountOptions {
    queryName: any;
    filterOptions?: FilterOptions[];
}

export interface FindOptions {
    queryName: any;
    filterOptions?: FilterOptions[];
    orderOptions?: OrderOption[];
    columnOptionsMode?: ColumnOptionsMode;
    columnOptions?: ColumnOptions[];

    searchOnLoad?: boolean;
    showHeader?: boolean;
    showFilters?: boolean;
    showFilterButton?: boolean;
    showFooter?: boolean;
    create?: boolean;
    navigate?: boolean;
}

export interface FilterOptions {
    columnName: string;
    token: QueryToken;
    frozen: string;
    operation: FilterOperation;
    value: any;
}

export enum FilterOperation {
    EqualTo,
    DistinctTo,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    Like,
    NotContains,
    NotStartsWith,
    NotEndsWith,
    NotLike,
    IsIn,
}

export interface OrderOption {
    columnName: string;
    token: QueryToken;
    orderType: OrderType;
}

export enum OrderType {
    Ascending,
    Descending
}

export interface ColumnOptions {
    columnName: string;
    token: QueryToken;
    displayName: string;
}

export class QueryToken {

}

export enum ColumnOptionsMode {
    Add,
    Remove,
    Replace,
}

export var DefaultPagination: Pagination = {
    mode: PaginationMode.Paginate,
    elementsPerPage: 20,
    currentPage: 1
};


export enum FindMode {
    Find,
    Explore
}


export interface Column {
    displayName: string;
    token: QueryToken;
}


export interface QueryToken {
    toString: string;
    niceName: string;
    key: string;
    format?: string;
    unit?: string;
    filterType: FilterType;
    typeName: string;
    fullKey: string;
    hasAllOrAny?: boolean;
}

export enum FilterType {
    Integer,
    Decimal,
    String,
    DateTime,
    Lite,
    Embedded,
    Boolean,
    Enum,
    Guid,
}

export interface ResultTable {
    entityColumn: Column;
    columns: Column[];
    rows: any[];
    pagination: Pagination
}

export interface Pagination {
    mode: PaginationMode;
    elementsPerPage?: number;
    currentPage?: number;
}

export enum PaginationMode {
    All,
    Firsts,
    Paginate
}
