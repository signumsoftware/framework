import { TypeReference, PropertyRoute } from 'Framework/Signum.React/Scripts/Reflection';

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
    token?: QueryToken;
    frozen?: string;
    operation: FilterOperation;
    value: any;
}


export enum FilterOperation {
    EqualTo = "EqualTo" as any,
    DistinctTo= "DistinctTo" as any,
    GreaterThan= "GreaterThan"  as any,
    GreaterThanOrEqual= "GreaterThanOrEqual" as any,
    LessThan= "LessThan"  as any,
    LessThanOrEqual= "LessThanOrEqual" as any,
    Contains= "Contains" as any,
    StartsWith= "StartsWith" as any,
    EndsWith= "EndsWith" as any,
    Like= "Like" as any,
    NotContains= "NotContains" as any,
    NotStartsWith= "NotStartsWith" as any,
    NotEndsWith= "NotEndsWith" as any,
    NotLike= "NotLike" as any,
    IsIn= "IsIn" as any,
};





export interface OrderOption {
    columnName: string;
    token?: QueryToken;
    orderType: OrderType;
}

export enum OrderType {
    Ascending,
    Descending
}

export interface ColumnOptions {
    columnName: string;
    token?: QueryToken;
    displayName: string;
}

export enum PaginationMode {
    All,
    Firsts,
    Paginate
}

export enum ColumnOptionsMode {
    Add = "Add" as any,
    Remove = "Remove" as any,
    Replace = "Replace" as any,
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
    type: TypeReference;
    filterType: FilterType;
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



export interface QueryDescription {
    queryName: any;
    columns: { [name: string]: ColumnDescription };
}

export interface ColumnDescription {
    name: string;
    type: TypeReference;
    filterType: FilterType;
    unit?: string;
    format?: string;
    propertyRoutes: PropertyRoute[];
    displayName: string;
}
