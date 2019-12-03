import { FindOptions, ColumnOption, ColumnOptionsMode, FilterOption, FilterOperation, FilterOptionParsed, FindOptionsParsed, OrderOption, OrderType, Pagination, PaginationMode, ResultTable, isFilterGroupOption, isFilterGroupOptionParsed, FilterConditionOptionParsed } from './FindOptions'
export { FindOptions, ColumnOption, ColumnOptionsMode, FilterOption, FilterOperation, FilterOptionParsed, FindOptionsParsed, OrderOption, OrderType, Pagination, PaginationMode, ResultTable };

import EntityLink, { EntityLinkProps } from './SearchControl/EntityLink'
export { EntityLink, EntityLinkProps };

import SearchControl, { SearchControlProps, ISimpleFilterBuilder, SearchControlHandler } from './SearchControl/SearchControl'
export { SearchControl, SearchControlProps, ISimpleFilterBuilder, SearchControlHandler };

import SearchControlLoaded, { SearchControlLoadedProps } from './SearchControl/SearchControlLoaded'
export { SearchControlLoaded, SearchControlLoadedProps };

import ValueSearchControl, { ValueSearchControlProps } from './SearchControl/ValueSearchControl'
export { ValueSearchControl, ValueSearchControlProps };

import ValueSearchControlLine, { ValueSearchControlLineProps } from './SearchControl/ValueSearchControlLine'
import { QueryTokenString } from './Reflection';
export { ValueSearchControlLine, ValueSearchControlLineProps };
import { AddToLite } from './Finder';
import { bool } from 'prop-types';

export function extractFilterValue<T>(filters: FilterOptionParsed[], token: QueryTokenString<T>, operation: FilterOperation | ((op: FilterOperation) => boolean), valueCondition?: (v: AddToLite<T> | null) => boolean): AddToLite<T> | null | undefined;
export function extractFilterValue(filters: FilterOptionParsed[], token: string | QueryTokenString<any>, operation: FilterOperation | ((op: FilterOperation) => boolean), valueCondition?: (v: any) => boolean): any;
export function extractFilterValue(filters: FilterOptionParsed[], token: string | QueryTokenString<any>, operation: FilterOperation | ((op: FilterOperation) => boolean), valueCondition?: (v: any) => boolean): any {

  var f = extractFilter(filters, token, operation, valueCondition);
  if (f == null)
    return undefined; 

  return f.value;
}

export function extractFilter<T>(filters: FilterOptionParsed[], token: QueryTokenString<T>, operation: FilterOperation | ((op: FilterOperation) => boolean), valueCondition?: (v: AddToLite<T> | null) => boolean): FilterConditionOptionParsed | undefined;
export function extractFilter(filters: FilterOptionParsed[], token: string | QueryTokenString<any>, operation: FilterOperation | ((op: FilterOperation) => boolean), valueCondition?: (v: AddToLite<any> | null) => boolean): FilterConditionOptionParsed | undefined;
export function extractFilter<T>(filters: FilterOptionParsed[], token: string | QueryTokenString<any>, operation: FilterOperation | ((op: FilterOperation) => boolean), valueCondition?: (v: AddToLite<any> | null) => boolean): FilterConditionOptionParsed | undefined {
  var f = filters.firstOrNull(f => !isFilterGroupOptionParsed(f) &&
    f.token!.fullKey == token.toString() &&
    (typeof operation == "function" ? operation(f.operation!) : f.operation == operation) &&
    (valueCondition == null || valueCondition(f.value))) as FilterConditionOptionParsed | undefined;

  if (!f) {
    return undefined;
  }

  filters.remove(f);
  return f;
}
