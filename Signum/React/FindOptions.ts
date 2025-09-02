import { TypeReference, PseudoType, QueryKey, getLambdaMembers, QueryTokenString, tryGetTypeInfos, PropertyRoute, isTypeEnum, TypeInfo, Type, isNumberType, isDecimalType, isTypeEntity, isTypeModel, getTypeInfo, IsByAll } from './Reflection';
import { Lite, Entity } from './Signum.Entities';
import { PaginationMode, OrderType, FilterOperation, ColumnOptionsMode, UniqueType, SystemTimeMode, FilterGroupOperation, PinnedFilterActive, SystemTimeJoinMode, DashboardBehaviour, CombineRows, TimeSeriesUnit, FilterType } from './Signum.DynamicQuery';
import { SearchControlProps, SearchControlLoaded } from "./Search";
import { BsSize } from './Components';
import { isDecimalKey } from './Lines/NumberLine';
import { QueryTokenDateMessage, QueryTokenMessage } from './Signum.DynamicQuery.Tokens';
import { CollectionMessage } from './Signum.External';
import { hasAggregate, hasAny, QueryToken } from './QueryToken';

export { PaginationMode, OrderType, FilterOperation, FilterType, ColumnOptionsMode, UniqueType };

export interface ValueFindOptions {
  queryName: PseudoType | QueryKey;
  filterOptions?: FilterOption[];
}

export interface ValueFindOptionsParsed {
  queryKey: string;
  filterOptions: FilterOptionParsed;
}

export interface ModalFindOptionsMany extends ModalFindOptions{
  allowNoSelection?: boolean;
}

export interface ModalFindOptions {
  title?: React.ReactNode;
  message?: React.ReactNode;
  forProperty?: string;
  useDefaultBehaviour?: boolean;
  autoSelectIfOne?: boolean;
  autoSkipIfZero?: boolean;
  autoCheckSingleRowResult?: boolean;
  modalSize?: BsSize;
  searchControlProps?: Partial<SearchControlProps>;
  onOKClicked?: (sc: SearchControlLoaded) => Promise<boolean>;
}

export interface FindOptions {
  queryName: PseudoType | QueryKey;
  groupResults?: boolean;

  includeDefaultFilters?: boolean;
  filterOptions?: (FilterOption | null | undefined)[];
  orderOptions?: (OrderOption | null | undefined)[];
  columnOptionsMode?: ColumnOptionsMode;
  columnOptions?: (ColumnOption | null | undefined)[];
  pagination?: Pagination;
  systemTime?: SystemTime;
}

export interface FindOptionsParsed {
  queryKey: string;
  groupResults: boolean;
  filterOptions: FilterOptionParsed[];
  orderOptions: OrderOptionParsed[];
  columnOptions: ColumnOptionParsed[];
  pagination: Pagination;
  systemTime?: SystemTime;
}


export type FilterOption = FilterConditionOption | FilterGroupOption;

export function isFilterGroup(fo: FilterOptionParsed): fo is FilterGroupOptionParsed
export function isFilterGroup(fo: FilterOption): fo is FilterGroupOption
export function isFilterGroup(fr: FilterRequest): fr is FilterGroupRequest 
export function isFilterGroup(fo: FilterOption | FilterOptionParsed | FilterRequest): boolean{
  return (fo as FilterGroupOptionParsed | FilterGroupOption | FilterGroupRequest).groupOperation != undefined;
}

export function isFilterCondition(fo: FilterOptionParsed): fo is FilterConditionOptionParsed
export function isFilterCondition(fo: FilterOption): fo is FilterConditionOption
export function isFilterCondition(fr: FilterRequest): fr is FilterConditionRequest
export function isFilterCondition(fo: FilterOptionParsed | FilterOption | FilterRequest): boolean {
  return (fo as FilterGroupOptionParsed | FilterGroupOption | FilterGroupRequest).groupOperation == undefined;
}


export interface FilterConditionOption {
  token: string | QueryTokenString<any>;
  frozen?: boolean;
  removeElementWarning?: boolean;
  operation?: FilterOperation;
  value?: any;
  pinned?: PinnedFilter;
  dashboardBehaviour?: DashboardBehaviour;
}

export interface FilterGroupOption {
  token?: string | QueryTokenString<any>;
  groupOperation: FilterGroupOperation;
  filters: (FilterOption | null | undefined)[];
  pinned?: PinnedFilter;
  frozen?: boolean;
  dashboardBehaviour?: DashboardBehaviour;
  value?: string; /*For search in multiple columns*/
}

export interface PinnedFilter {
  label?: (() => string) | string;
  row?: number;
  column?: number;
  colSpan?: number;
  active?: PinnedFilterActive;
  splitValue?: boolean;
}

export type FilterOptionParsed = FilterConditionOptionParsed | FilterGroupOptionParsed;



export function isActive(fo: FilterOptionParsed | FilterOption): boolean {
  return !(fo.dashboardBehaviour == "UseAsInitialSelection" ||
    fo.pinned &&
    (fo.pinned.active == "Checkbox_Unchecked" ||
      fo.pinned.active == "NotCheckbox_Unchecked" ||
      fo.pinned.active == "WhenHasValue" && fo.value == null ||
      fo.pinned.splitValue && !fo.value));
}

export function isCheckBox(active: PinnedFilterActive | undefined): boolean {
  return active == "Checkbox_Checked" ||
    active == "Checkbox_Unchecked" ||
    active == "NotCheckbox_Checked" ||
    active == "NotCheckbox_Unchecked";
}

export interface FilterConditionOptionParsed {
  token?: QueryToken;
  frozen: boolean;
  removeElementWarning?: boolean;
  operation?: FilterOperation;
  value: any;
  pinned?: PinnedFilterParsed;
  dashboardBehaviour?: DashboardBehaviour;
}

export interface PinnedFilterParsed {
  label?: string;
  row?: number;
  column?: number;
  colSpan?: number;
  active?: PinnedFilterActive;
  splitValue?: boolean;
}

export function toPinnedFilterParsed(pf: PinnedFilter): PinnedFilterParsed {
  return {
    label: typeof pf.label == "function" ? pf.label() : pf.label,
    column: pf.column,
    colSpan: pf.colSpan,
    row: pf.row,
    active: pf.active,
    splitValue: pf.splitValue
  };
}

export interface FilterGroupOptionParsed {
  groupOperation: FilterGroupOperation;
  frozen: boolean;
  token?: QueryToken;
  filters: FilterOptionParsed[];
  pinned?: PinnedFilterParsed;
  dashboardBehaviour?: DashboardBehaviour;
  value?: string; /*For search in multiple columns*/
}

export interface OrderOption {
  token: string | QueryTokenString<any>;
  orderType: OrderType;
}

export interface OrderOptionParsed {
  token: QueryToken;
  orderType: OrderType;
}

export interface ColumnOption {
  token: string | QueryTokenString<any>;
  displayName?: string | (() => string);
  summaryToken?: string | QueryTokenString<any>;
  hiddenColumn?: boolean;
  combineRows?: CombineRows;
}

export interface ColumnOptionParsed {
  token?: QueryToken;
  displayName?: string;
  summaryToken?: QueryToken;
  hiddenColumn?: boolean;
  combineRows?: CombineRows;
}

export const DefaultPagination: Pagination = {
  mode: "Paginate",
  elementsPerPage: 20,
  currentPage: 1
};


export type FindMode = "Find" | "Explore";


export interface QueryTokenWithoutParent extends Omit<QueryToken,  | "parent"> {
  subTokens?: { [name: string]: QueryTokenWithoutParent };
  parent: "fake";
}


export function withoutAggregate(fop: FilterOptionParsed): FilterOptionParsed | undefined {

  if (hasAggregate(fop.token))
    return undefined;

  if (isFilterGroup(fop)) {
    var newFilters = fop.filters.map(f => withoutAggregate(f)).filter(Boolean);
    if (newFilters.length == 0)
      return undefined;
    return ({
      ...fop,
      filters: newFilters,
    }) as FilterOptionParsed;
  };

  return {
    ...fop,
  };
}

export function withoutPinned(fop: FilterOptionParsed): FilterOptionParsed | undefined {

  if (!isActive(fop)) {
    return undefined;
  }

  if (fop.value != null && (fop.pinned && fop.pinned.splitValue || isFilterGroup(fop))) 
    return fop; //otherwise change meaning

  if (isFilterGroup(fop)) {
    var newFilters = fop.filters.map(f => withoutPinned(f)).filter(Boolean);
    if (newFilters.length == 0)
      return undefined;

    return ({
      ...fop,
      filters: newFilters,
      pinned: undefined,
    }) as FilterOptionParsed;
  };

  return {
    ...fop,
    pinned: undefined
  };
}

export function canSplitValue(fo: FilterOptionParsed): boolean | undefined {
  if (isFilterGroup(fo))
    return fo.pinned != null;

  else {
    return fo.operation && isList(fo.operation) && hasAny(fo.token) ||
      fo.token && fo.token.filterType == "String";
  }
}

export function mapFilterTokens(fo: FilterOption, mapToken : (token: string) => string): FilterOption {
  
  if (isFilterGroup(fo)) {
    return {
      ...fo,
      groupOperation: fo.groupOperation,
      filters: fo.filters.map(f => f && mapFilterTokens(f, mapToken)),
      token: fo.token && mapToken(fo.token.toString())
    };
  }
  else {
    return {
      ...fo,
      token: fo.token && mapToken(fo.token.toString()),
    }
  }
}


export type FilterRequest = FilterConditionRequest | FilterGroupRequest;

export interface FilterGroupRequest {
  groupOperation: FilterGroupOperation;
  token?: string;
  filters: FilterRequest[];
}

export interface FilterConditionRequest {
  token: string;
  operation: FilterOperation;
  value: any;
}

export interface OrderRequest {
  token: string;
  orderType: OrderType
}

export interface ColumnRequest {
  token: string;
  displayName: string;
}

export interface QueryEntitiesRequest {
  queryKey: string;
  filters: FilterRequest[];
  orders: OrderRequest[];
  count: number | null;
}

export interface QueryRequest {
  queryKey: string;
  groupResults: boolean;
  filters: FilterRequest[];
  orders: OrderRequest[];
  columns: ColumnRequest[];
  pagination: Pagination;
  systemTime?: SystemTime;
}

export type AggregateType = "Count" | "Average" | "Sum" | "Min" | "Max";

export interface QueryValueRequest {
  queryKey: string;
  filters: FilterRequest[];
  multipleValues?: boolean;
  valueToken?: string;
  systemTime?: SystemTime;
}

export interface ResultTable {
  columns: string[];
  uniqueValues: { [token: string]: any[] }
  rows: ResultRow[];
  pagination: Pagination
  totalElements?: number;
}

export interface ResultRow {
  entity: Lite<Entity> | undefined;
  columns: any[];
}

export interface Pagination {
  mode: PaginationMode;
  elementsPerPage?: number;
  currentPage?: number;
}

export interface SystemTime {
  mode: SystemTimeMode;
  joinMode?: SystemTimeJoinMode;
  startDate?: string;
  splitQueries?: boolean;
  endDate?: string;
  timeSeriesUnit?: TimeSeriesUnit;
  timeSeriesStep?: number;
  timeSeriesMaxRowsPerStep?: number;

}

export namespace PaginateMath {
  export function startElementIndex(p: Pagination): number {
    return (p.elementsPerPage! * (p.currentPage! - 1)) + 1;
  }

  export function endElementIndex(p: Pagination, rows: number): number {
    return startElementIndex(p) + rows - 1;
  }

  export function totalPages(p: Pagination, totalElements: number): number {
    return Math.max(1, Math.ceil(totalElements / p.elementsPerPage!)); //Round up
  }

  export function maxElementIndex(p: Pagination): number {
    return (p.elementsPerPage! * (p.currentPage! + 1)) - 1;
  }
}


export interface QueryDescription {
  queryKey: string;
  columns: { [name: string]: QueryToken };
}

export interface QueryDescriptionDTO {
  queryKey: string;
  columns: { [name: string]: QueryTokenWithoutParent };
}


export function isList(fo: FilterOperation): boolean {
  return fo == "IsIn" ||
    fo == "IsNotIn";
}



export function getFilterOperations(qt: QueryToken): FilterOperation[] {

  if (qt.filterType == null)
    return [];

  var fops = filterOperations[qt.filterType];

  if (qt.queryTokenType == null && qt.propertyRoute != null) {
    var pr = PropertyRoute.tryParseFull(qt.propertyRoute);

    if (pr && pr.member?.hasFullTextIndex)
      return ["ComplexCondition", "FreeText", ...fops];
  }
  return fops;
}

export function getFilterGroupUnifiedFilterType(tr: TypeReference): FilterType | null {
  if (isNumberType(tr.name) || tr.name == "boolean" || tr.name == "string" || tr.name == "Guid")
    return "String";

  if (tr.name == "DateTime")
    return "DateTime";

  if (tr.isEmbedded)
    return "Embedded";

  if (isTypeEnum(tr.name))
    return "Enum";

  if (tr.isLite || tryGetTypeInfos(tr)[0]?.name)
    return "Lite";

  return null;
}

export const filterOperations: Record<FilterType, FilterOperation[]> = {
  "String": [
    "Contains",
    "EqualTo",
    "StartsWith",
    "EndsWith",
    "Like",
    "NotContains",
    "DistinctTo",
    "NotStartsWith",
    "NotEndsWith",
    "NotLike",
    "IsIn",
    "IsNotIn"
  ],

  "DateTime": [
    "EqualTo",
    "DistinctTo",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
    "IsIn",
    "IsNotIn"
  ],

  "Time": [
    "EqualTo",
    "DistinctTo",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
    "IsIn",
    "IsNotIn"
  ],

  "Integer": [
    "EqualTo",
    "DistinctTo",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
    "IsIn",
    "IsNotIn"
  ],

  "Decimal": [
    "EqualTo",
    "DistinctTo",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
    "IsIn",
    "IsNotIn"
  ],

  "Enum": [
    "EqualTo",
    "DistinctTo",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
    "IsIn",
    "IsNotIn",
  ],

  "Guid": [
    "EqualTo",
    "DistinctTo",
    "IsIn",
    "IsNotIn"
  ],

  "Lite": [
    "EqualTo",
    "DistinctTo",
    "IsIn",
    "IsNotIn"
  ],

  "Embedded": [
    "EqualTo",
    "DistinctTo",
  ],

  "Model": [
    "EqualTo",
    "DistinctTo",
  ],

  "Boolean": [
    "EqualTo",
    "DistinctTo",
  ],
  "TsVector": [
    "TsQuery",
    "TsQuery_Plain",
    "TsQuery_Phrase",
    "TsQuery_WebSearch",
  ]
};

