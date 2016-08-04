import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryKey } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { EntityOperationSettings } from '../../../../Framework/Signum.React/Scripts/Operations'
import { Entity, Lite, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import * as QuickLinks from '../../../../Framework/Signum.React/Scripts/QuickLinks'
import { FindOptions, FilterOption, FilterOptionParsed, FilterOperation, OrderOption, OrderOptionParsed, ColumnOption, FilterRequest, QueryRequest, Pagination, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import * as AuthClient  from '../../../../Extensions/Signum.React.Extensions/Authorization/AuthClient'
import { UserChartEntity, ChartPermission, ChartMessage, ChartRequest, ChartParameterEntity, ChartColumnEntity  } from '../Signum.Entities.Chart'
import { QueryFilterEntity, QueryOrderEntity } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryTokenEntity } from '../../UserAssets/Signum.Entities.UserAssets'
import UserChartMenu from './UserChartMenu'
import * as ChartClient from '../ChartClient'
import * as UserAssetsClient from '../../UserAssets/UserAssetClient'


export function start(options: { routes: JSX.Element[] }) {

    UserAssetsClient.start({ routes: options.routes });
    UserAssetsClient.registerExportAssertLink(UserChartEntity);

    options.routes.push(<Route path="userChart">
        <Route path=":userChartId(/:entity)" getComponent={ (loc, cb) => require(["./UserChartPage"], (Comp) => cb(undefined, Comp.default)) } />
    </Route>);


    ChartClient.ButtonBarChart.onButtonBarElements.push(ctx => {
        if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting))
            return undefined;

        return <UserChartMenu chartRequestView={ctx.chartRequestView}/>;
    }); 

    QuickLinks.registerGlobalQuickLink(ctx => {
        if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting))
            return undefined;

        return API.forEntityType(ctx.lite.EntityType).then(uqs =>
            uqs.map(uc => new QuickLinks.QuickLinkAction(liteKey(uc), uc.toStr || "", e => {
                window.open(Navigator.currentHistory.createHref(`~/userChart/${uc.id}/${liteKey(ctx.lite)}`));
            }, { glyphicon: "glyphicon-list-alt", glyphiconColor: "dodgerblue" })));
    });

    QuickLinks.registerQuickLink(UserChartEntity, ctx => new QuickLinks.QuickLinkAction("preview", ChartMessage.Preview.niceToString(),
        e => {
            Navigator.API.fetchAndRemember(ctx.lite).then(uc => {
                if (uc.entityType == undefined)
                    window.open(Navigator.currentHistory.createHref(`~/userChart/${uc.id}`));
                else
                    Navigator.API.fetchAndForget(uc.entityType)
                        .then(t => Finder.find({ queryName: t.cleanName }))
                        .then(lite => {
                            if (!lite)
                                return;

                            window.open(Navigator.currentHistory.createHref(`~/userChart/${uc.id}/${liteKey(lite)}`));
                        })
                        .done();
            }).done();
        }, { isVisible: AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting) }));


    Navigator.addSettings(new EntitySettings(UserChartEntity, e => new Promise(resolve => require(['./UserChart'], resolve)), { isCreable: "Never" }));
}


export module Converter {

    export function applyUserChart(cr: ChartRequest, uq: UserChartEntity, entity?: Lite<Entity>): Promise<ChartRequest> {

        const promise = UserAssetsClient.API.parseFilters({
            queryKey: uq.query.key,
            canAggregate: false,
            entity: entity,
            filters: uq.filters!.map(mle => mle.element).map(f => ({
                tokenString: f.token!.tokenString,
                operation: f.operation,
                valueString: f.valueString
            }) as UserAssetsClient.API.ParseFilterRequest)
        });

        return promise.then(filters => {

            if (filters) {
                cr.filterOptions = (cr.filterOptions || []).filter(f => f.frozen);
                cr.filterOptions.push(...uq.filters.map((f, i) => ({
                    token: f.element.token,
                    operation: f.element.operation!,
                    value: filters[i].value,
                    frozen: false,
                }) as FilterOptionParsed));
            }

            cr.parameters = uq.parameters!.map(mle => ({
                rowId: null,
                element: ChartParameterEntity.New(p => {
                    p.name = mle.element.name;
                    p.value = mle.element.value;
                })
            }));

            cr.columns = uq.columns!.map(mle => ({
                rowId: null,
                element: ChartColumnEntity.New(c => {
                    c.displayName = mle.element.displayName;
                    c.token = QueryTokenEntity.New(qt => {
                        qt.token = mle.element.token!.token;
                        qt.tokenString = mle.element.token!.tokenString;
                    });
                })
            }));

            cr.orderOptions = (uq.orders || []).map(f => ({
                token: f.element.token!.token,
                orderType: f.element.orderType
            }) as OrderOptionParsed);


            return cr;
        });
    }

    export function toChartRequest(uq: UserChartEntity, entity?: Lite<Entity>): Promise<ChartRequest> {
        const cs = ChartRequest.New(cr => cr.queryKey = uq.query!.key); 
        return applyUserChart(cs, uq, entity);
    }
}


export module API {
    export function forEntityType(type: string): Promise<Lite<UserChartEntity>[]> {
        return ajaxGet<Lite<UserChartEntity>[]>({ url: "~/api/userChart/forEntityType/" + type });
    }

    export function forQuery(queryKey: string): Promise<Lite<UserChartEntity>[]> {
        return ajaxGet<Lite<UserChartEntity>[]>({ url: "~/api/userChart/forQuery/" + queryKey });
    }

    export function fromChartRequest(chartRequest: ChartRequest): Promise<UserChartEntity> {

        const clone = ChartClient.API.cleanedChartRequest(chartRequest)

        return ajaxPost<UserChartEntity>({ url: "~/api/userChart/fromChartRequest/" }, clone);
    }
}
