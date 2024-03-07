import type {
  FindOptions, ColumnOption, ColumnOptionsMode, FilterOption, FilterOperation,
  FindOptionsParsed, OrderOption, OrderType, Pagination, PaginationMode, ResultTable,
  FilterOptionParsed, FilterConditionOptionParsed, FilterGroupOptionParsed, FilterGroupOption, FilterConditionOption
} from './FindOptions'
import { isActive, isFilterCondition, isFilterGroup } from './FindOptions'
export type {
  FindOptions, ColumnOption, ColumnOptionsMode, FilterOption, FilterOperation,
  FindOptionsParsed, OrderOption, OrderType, Pagination, PaginationMode, ResultTable,
  FilterOptionParsed, FilterConditionOptionParsed, FilterGroupOptionParsed, FilterGroupOption, FilterConditionOption
};

export { default as EntityLink } from './SearchControl/EntityLink'
export type { EntityLinkProps } from './SearchControl/EntityLink'

export { default as SearchControl } from './SearchControl/SearchControl'
export type { SearchControlProps, ISimpleFilterBuilder, SearchControlHandler } from './SearchControl/SearchControl'

export { default as SearchControlLoaded } from './SearchControl/SearchControlLoaded'
export type { SearchControlLoadedProps } from './SearchControl/SearchControlLoaded'

export { default as SearchValue } from './SearchControl/SearchValue'
export type { SearchValueProps, SearchValueController } from './SearchControl/SearchValue'

export { default as SearchValueLine } from './SearchControl/SearchValueLine'
export type { SearchValueLineProps, SearchValueLineController } from './SearchControl/SearchValueLine'
import { QueryTokenString } from './Reflection';
import { AddToLite } from './Finder';

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
  var f = filters.firstOrNull(f => isFilterCondition(f) && isActive(f) && 
    similarToken(f.token!.fullKey, token.toString()) &&
    (typeof operation == "function" ? operation(f.operation!) : f.operation == operation) &&
    (valueCondition == null || valueCondition(f.value))) as FilterConditionOptionParsed | undefined;

  if (!f) {
    return undefined;
  }

  filters.remove(f);
  return f;
}

export function extractGroupFilter(filters: FilterOptionParsed[], fo: FilterGroupOption): FilterGroupOptionParsed | undefined
{
  var f = filters.firstOrNull(f => isFilterGroup(f) && Boolean(f.pinned) == Boolean(fo.pinned) && f.pinned?.splitValue == fo.pinned?.splitValue && f.groupOperation == fo.groupOperation
    && f.filters.length == fo.filters.length &&
    f.filters.every((f2, i) => {
      var fo2 = fo.filters[i];
      if (fo2 == null || isFilterGroup(f2) || isFilterGroup(fo2))
        return false;

      return similarToken(f2.token?.fullKey, fo2.token?.toString()) && f2.operation == fo2.operation && f2.value == fo2.value;
    }));

  if (!f) {
    return undefined;
  }

  filters.remove(f);
  return f as FilterGroupOptionParsed;
}

export function similarToken(tokenA: string | undefined, tokenB: string | undefined) {
  return (tokenA?.startsWith("Entity.") ? tokenA.after("Entity.") : tokenA) ==
    (tokenB?.startsWith("Entity.") ? tokenB.after("Entity.") : tokenB);
}

