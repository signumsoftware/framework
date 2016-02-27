import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { Entity, Lite, liteKey } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { FindOptions, FilterOption, FilterOperation, OrderOption, ColumnOption, FilterRequest, QueryRequest, Pagination } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as AuthClient  from '../../../Extensions/Signum.React.Extensions/Authorization/AuthClient'
import { UserQueryEntity_Type, UserQueryEntity, UserQueryPermission, UserQueryMessage, QueryFilterEntity } from './Signum.Entities.UserQueries'
import UserQueryMenu from './UserQueryMenu'

export function start(options: { routes: JSX.Element[] }) {

    options.routes.push(<Route path="userQuery">
        <Route path=":userQueryId" getComponent={ (loc, cb) => require(["./UserQueryPage"], (Comp) => cb(null, Comp.default)) } />
    </Route>);

    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
        if (AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery))
            return null;

        return <UserQueryMenu searchControl={ctx.searchControl}/>;
    }); 

    QuickLinks.registerGlobalQuickLink(ctx => {
        if (AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery))
            return null;

        API.forEntityType(ctx.lite.EntityType).then(uqs => {
            uqs.map(uq => new QuickLinks.QuickLinkAction(liteKey(uq), uq.toStr, e => {
                Navigator.API.fetch(uq)
                    .then(uq => Converter.toFindOptions(uq, null))
                    .then(fo => Finder.exploreWindowsOpen(fo, e));
            }));
        });
    });

    QuickLinks.registerQuickLink(UserQueryEntity_Type, ctx => new QuickLinks.QuickLinkAction("preview", UserQueryMessage.Preview.niceToString(),
        e => {
            Navigator.API.fetch(ctx.lite).then(uq => {
                if (uq.entityType == null)
                    return Converter.toFindOptions(uq, null);
                else
                    return Navigator.API.fetch(uq.entityType)
                        .then(t => Finder.find({ queryName: t.cleanName }))
                        .then(lite => lite == null ? null : Converter.toFindOptions(uq, lite));
            }).then(fo => {

                if (fo == null)
                    return;

                Finder.exploreWindowsOpen(fo, e);
            });
        }, { isVisible: AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery) }));

    Navigator.addSettings(new EntitySettings(UserQueryEntity_Type, e => new Promise(resolve => require(['./Templates/UserQuery'], resolve))));
}


export module Converter {

    export function applyUserQuery(fo: FindOptions, uq: UserQueryEntity, entity: Lite<Entity>): Promise<FindOptions> {

        var convertedFilters = uq.withoutFilters ? Promise.resolve([] as FilterRequest[]) : API.parseFilters({
            queryKey: uq.query.key,
            canAggregate: false,
            entity: entity,
            filters: uq.filters.map(mle => mle.element)
        });

        return convertedFilters.then(filters => {

            if (!uq.withoutFilters) {
                fo.filterOptions = fo.filterOptions.filter(f => !f.frozen);
                fo.filterOptions.push(...filters.map(f => ({
                    columnName: f.token,
                    operation: f.operation,
                    value: f.value,
                }) as FilterOption));
            }

            fo.columnOptionsMode = uq.columnsMode;

            fo.columnOptions = uq.columns.map(f => ({
                columnName: f.element.token.tokenString,
                displayName: f.element.displayName
            }) as ColumnOption);

            fo.orderOptions = uq.orders.map(f => ({
                columnName: f.element.token.tokenString,
                orderType: f.element.orderType
            }) as OrderOption);

            fo.pagination = {
                mode: uq.paginationMode,
                currentPage: null,
                elementsPerPage: uq.elementsPerPage
            };

            return Finder.parseTokens(fo);
        });
    }

    export function toFindOptions(uq: UserQueryEntity, entity: Lite<Entity>): Promise<FindOptions> {
        var fo: FindOptions = { queryName: uq.query.key }; 
        return applyUserQuery(fo, uq, entity);
    }
}

export module API {
    export function forEntityType(type: string): Promise<Lite<UserQueryEntity>[]> {
        return ajaxGet<Lite<UserQueryEntity>[]>({ url: "/api/userQueries/forEntityType/" + type });
    }

    export function forQuery(queryKey: string): Promise<Lite<UserQueryEntity>[]> {
        return ajaxGet<Lite<UserQueryEntity>[]>({ url: "/api/userQueries/forQuery/" + queryKey });
    }

    export function parseFilters(request: { queryKey: string; filters: QueryFilterEntity[]; entity: Lite<Entity>; canAggregate: boolean }): Promise<FilterRequest[]> {
        return ajaxPost<FilterRequest[]>({ url: "/api/userQueries/parseFilters/" }, request);
    }

    export function toStringFilters(request: { queryRequest: QueryRequest; defaultPagination: Pagination}): Promise<UserQueryEntity> {
        return ajaxPost<UserQueryEntity>({ url: "/api/userQueries/fromQueryRequest/" }, request);
    }
}
