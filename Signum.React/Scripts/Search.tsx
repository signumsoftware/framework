import * as React from 'react'


import { FindOptions, ColumnOption, ColumnOptionsMode, FilterOption, FilterOperation, FilterOptionParsed, FindOptionsParsed, OrderOption, OrderType, Pagination, PaginationMode, ResultTable } from './FindOptions'
export { FindOptions, ColumnOption, ColumnOptionsMode, FilterOption, FilterOperation, FilterOptionParsed, FindOptionsParsed, OrderOption, OrderType, Pagination, PaginationMode, ResultTable };

import EntityLink, { EntityLinkProps } from  './SearchControl/EntityLink'
export { EntityLink, EntityLinkProps };

import SearchControl, { SearchControlProps, ISimpleFilterBuilder } from  './SearchControl/SearchControl'
export { SearchControl, SearchControlProps, ISimpleFilterBuilder };

import SearchControlLoaded, { SearchControlLoadedProps} from './SearchControl/SearchControlLoaded'
export { SearchControlLoaded, SearchControlLoadedProps};

import ValueSearchControl, { ValueSearchControlProps } from './SearchControl/ValueSearchControl'
export { ValueSearchControl, ValueSearchControlProps };

import ValueSearchControlLine, { ValueSearchControlLineProps } from './SearchControl/ValueSearchControlLine'
export { ValueSearchControlLine, ValueSearchControlLineProps };

export function extractFilterValue(filters: FilterOptionParsed[], columnName: string, operation: FilterOperation): any {
    var f = filters.filter(f => f.token!.fullKey == columnName && f.operation == operation).firstOrNull();
    if (!f)
        return null;

    filters.remove(f);
    return f.value;
}
