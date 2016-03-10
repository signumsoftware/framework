import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity, Lite, liteKey } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { FindOptions, FilterOption, FilterOperation, OrderOption, ColumnOption, FilterRequest, QueryRequest, Pagination } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as AuthClient  from '../../../Extensions/Signum.React.Extensions/Authorization/AuthClient'

export function start(options: { routes: JSX.Element[] }) {

    
}

export module API {

    export function parseFilters(request: ParseFiltersRequest): Promise<FilterRequest[]> {
        return ajaxPost<FilterRequest[]>({ url: "/api/userQueries/parseFilters/" }, request);
    }

    export interface ParseFiltersRequest {
        queryKey: string;
        filters: ParseFilterRequest[];
        entity: Lite<Entity>;
        canAggregate: boolean
    }

    export interface ParseFilterRequest {
        tokenString: string;
        operation: FilterOperation;
        valueString: string;
    }
}
