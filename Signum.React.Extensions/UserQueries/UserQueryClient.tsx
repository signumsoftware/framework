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
import { UserQueryEntity, UserQueryPermission, UserQueryMessage,
    QueryFilterEntity, QueryColumnEntity, QueryOrderEntity} from './Signum.Entities.UserQueries'
import { QueryTokenEntity } from '../UserAssets/Signum.Entities.UserAssets'
import UserQueryMenu from './UserQueryMenu'
import * as UserAssetsClient from '../UserAssets/UserAssetClient'

export function start(options: { routes: JSX.Element[] }) {

    UserAssetsClient.start({ routes: options.routes });
    UserAssetsClient.registerExportAssertLink(UserQueryEntity);

    options.routes.push(<Route path="userQuery">
        <Route path=":userQueryId(/:entity)" getComponent={ (loc, cb) => require(["./Templates/UserQueryPage"], (Comp) => cb(null, Comp.default)) } />
    </Route>);

    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
        if (!AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery))
            return null;

        return <UserQueryMenu searchControl={ctx.searchControl}/>;
    }); 

    QuickLinks.registerGlobalQuickLink(ctx => {
        if (!AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery))
            return null;

        return API.forEntityType(ctx.lite.EntityType).then(uqs =>
            uqs.map(uq => new QuickLinks.QuickLinkAction(liteKey(uq), uq.toStr, e => {
                window.open(Navigator.currentHistory.createHref(`~/userQuery/${uq.id}/${liteKey(ctx.lite)}`));
            }, { glyphicon: "glyphicon-list-alt", glyphiconColor: "dodgerblue" })));
    });

    QuickLinks.registerQuickLink(UserQueryEntity, ctx => new QuickLinks.QuickLinkAction("preview", UserQueryMessage.Preview.niceToString(),
        e => {
            Navigator.API.fetchAndRemember(ctx.lite).then(uq => {
                if (uq.entityType == null)
                    window.open(Navigator.currentHistory.createHref(`~/userQuery/${uq.id}`));
                else
                    Navigator.API.fetchAndForget(uq.entityType)
                        .then(t => Finder.find({ queryName: t.cleanName }))
                        .then(lite => {
                            if (!lite)
                                return;

                            window.open(Navigator.currentHistory.createHref(`~/userQuery/${uq.id}/${liteKey(lite)}`));
                        })
                        .done();
            }).done();
        }, { isVisible: AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery) }));

    Constructor.registerConstructor<QueryFilterEntity>(QueryFilterEntity, () => QueryFilterEntity.New(f => f.token = QueryTokenEntity.New()));
    Constructor.registerConstructor<QueryOrderEntity>(QueryOrderEntity, () => QueryOrderEntity.New(o => o.token = QueryTokenEntity.New()));
    Constructor.registerConstructor<QueryColumnEntity>(QueryColumnEntity, () => QueryColumnEntity.New(c => c.token = QueryTokenEntity.New()));

    Navigator.addSettings(new EntitySettings(UserQueryEntity, e => new Promise(resolve => require(['./Templates/UserQuery'], resolve)), { isCreable: "Never" }));
}


export module Converter {

    export function applyUserQuery(fo: FindOptions, uq: UserQueryEntity, entity: Lite<Entity>): Promise<FindOptions> {

        var convertedFilters = uq.withoutFilters ? Promise.resolve([] as FilterRequest[]) : UserAssetsClient.API.parseFilters({
            queryKey: uq.query.key,
            canAggregate: false,
            entity: entity,
            filters: uq.filters.map(mle => mle.element).map(f => ({
                tokenString: f.token.tokenString,
                operation: f.operation,
                valueString: f.valueString
            }) as UserAssetsClient.API.ParseFilterRequest)
        });

        return convertedFilters.then(filters => {

            if (!uq.withoutFilters && filters) {
                fo.filterOptions = (fo.filterOptions || []).filter(f => f.frozen);
                fo.filterOptions.push(...filters.map(f => ({
                    columnName: f.token,
                    operation: f.operation,
                    value: f.value,
                }) as FilterOption));
            }

            fo.columnOptionsMode = uq.columnsMode;

            fo.columnOptions = (uq.columns || []).map(f => ({
                columnName: f.element.token.tokenString,
                displayName: f.element.displayName
            }) as ColumnOption);

            fo.orderOptions = (uq.orders || []).map(f => ({
                columnName: f.element.token.tokenString,
                orderType: f.element.orderType
            }) as OrderOption);


            var qs = Finder.querySettings[uq.query.key];

            fo.pagination = uq.paginationMode == null ?
                ((qs && qs.pagination) || Finder.defaultPagination) : {
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
        return ajaxGet<Lite<UserQueryEntity>[]>({ url: "~/api/userQueries/forEntityType/" + type });
    }

    export function forQuery(queryKey: string): Promise<Lite<UserQueryEntity>[]> {
        return ajaxGet<Lite<UserQueryEntity>[]>({ url: "~/api/userQueries/forQuery/" + queryKey });
    }

    export function fromQueryRequest(request: { queryRequest: QueryRequest; defaultPagination: Pagination}): Promise<UserQueryEntity> {
        return ajaxPost<UserQueryEntity>({ url: "~/api/userQueries/fromQueryRequest/" }, request);
    }
}
