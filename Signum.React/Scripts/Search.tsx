import { FindOptions, ColumnOption, ColumnOptionsMode, FilterOption, FilterOperation, FilterOptionParsed, FindOptionsParsed, OrderOption, OrderType, Pagination, PaginationMode, ResultTable, isFilterGroupOption, isFilterGroupOptionParsed, FilterConditionOptionParsed } from './FindOptions'
export { FindOptions, ColumnOption, ColumnOptionsMode, FilterOption, FilterOperation, FilterOptionParsed, FindOptionsParsed, OrderOption, OrderType, Pagination, PaginationMode, ResultTable };

import EntityLink, { EntityLinkProps } from './SearchControl/EntityLink'
export { EntityLink, EntityLinkProps };

import SearchControl, { SearchControlProps, ISimpleFilterBuilder } from './SearchControl/SearchControl'
export { SearchControl, SearchControlProps, ISimpleFilterBuilder };

import SearchControlLoaded, { SearchControlLoadedProps } from './SearchControl/SearchControlLoaded'
export { SearchControlLoaded, SearchControlLoadedProps };

import ValueSearchControl, { ValueSearchControlProps } from './SearchControl/ValueSearchControl'
export { ValueSearchControl, ValueSearchControlProps };

import ValueSearchControlLine, { ValueSearchControlLineProps } from './SearchControl/ValueSearchControlLine'
import { QueryTokenString } from './Reflection';
export { ValueSearchControlLine, ValueSearchControlLineProps };

export function extractFilterValue(filters: FilterOptionParsed[], token: string | QueryTokenString<any>, operation: FilterOperation): any {

  var f = extractFilter(filters, token, operation);

  return f && f.value;
}

export function extractFilter(filters: FilterOptionParsed[], token: string | QueryTokenString<any>, operation: FilterOperation): FilterOptionParsed | null {
  var f = filters.filter(f => !isFilterGroupOptionParsed(f) && f.token!.fullKey == token.toString() && f.operation == operation).firstOrNull() as FilterConditionOptionParsed | undefined;
  if (!f) {
    return null;
  }

  filters.remove(f);
  return f;
}
