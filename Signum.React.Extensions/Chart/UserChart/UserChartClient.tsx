import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '../../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { getQueryKey } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { EntityOperationSettings } from '../../../../Framework/Signum.React/Scripts/Operations'
import { Entity, Lite, liteKey } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import * as QuickLinks from '../../../../Framework/Signum.React/Scripts/QuickLinks'
import { FindOptions, QueryToken, FilterOption, FilterOptionParsed, FilterOperation, OrderOption, OrderOptionParsed, ColumnOption, FilterRequest, QueryRequest, Pagination, SubTokensOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import * as AuthClient from '../../Authorization/AuthClient'
import { UserChartEntity, ChartPermission, ChartMessage, ChartRequest, ChartParameterEmbedded, ChartColumnEmbedded } from '../Signum.Entities.Chart'
import { QueryFilterEmbedded, QueryOrderEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import UserChartMenu from './UserChartMenu'
import * as ChartClient from '../ChartClient'
import * as UserAssetsClient from '../../UserAssets/UserAssetClient'
import { ImportRoute } from "../../../../Framework/Signum.React/Scripts/AsyncImport";
import { OrderRequest } from '../../../../Framework/Signum.React/Scripts/FindOptions';


export function start(options: { routes: JSX.Element[] }) {

    UserAssetsClient.start({ routes: options.routes });
    UserAssetsClient.registerExportAssertLink(UserChartEntity);

    options.routes.push(<ImportRoute path="~/userChart/:userChartId/:entity?" onImportModule={() => import("./UserChartPage")} />);


    ChartClient.ButtonBarChart.onButtonBarElements.push(ctx => {
        if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting))
            return undefined;

        return <UserChartMenu chartRequestView={ctx.chartRequestView}/>;
    }); 

    QuickLinks.registerGlobalQuickLink(ctx => {
        if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting))
            return undefined;

        var promise = ctx.widgetContext ?
            Promise.resolve(ctx.widgetContext.pack.userCharts || []) :
            API.forEntityType(ctx.lite.EntityType);

        return promise.then(uqs =>
            uqs.map(uc => new QuickLinks.QuickLinkAction(liteKey(uc), uc.toStr || "", e => {
                window.open(Navigator.toAbsoluteUrl(`~/userChart/${uc.id}/${liteKey(ctx.lite)}`));
            }, { icon: "fa fa-bar-chart", iconColor: "darkviolet" })));
    });

    QuickLinks.registerQuickLink(UserChartEntity, ctx => new QuickLinks.QuickLinkAction("preview", ChartMessage.Preview.niceToString(),
        e => {
            Navigator.API.fetchAndRemember(ctx.lite).then(uc => {
                if (uc.entityType == undefined)
                    window.open(Navigator.toAbsoluteUrl(`~/userChart/${uc.id}`));
                else
                    Navigator.API.fetchAndForget(uc.entityType)
                        .then(t => Finder.find({ queryName: t.cleanName }))
                        .then(lite => {
                            if (!lite)
                                return;

                            window.open(Navigator.toAbsoluteUrl(`~/userChart/${uc.id}/${liteKey(lite)}`));
                        })
                        .done();
            }).done();
        }, { isVisible: AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting) }));


    Navigator.addSettings(new EntitySettings(UserChartEntity, e => import('./UserChart'), { isCreable: "Never" }));
}


export module Converter {

    export function applyUserChart(cr: ChartRequest, uq: UserChartEntity, entity?: Lite<Entity>): Promise<ChartRequest> {

        cr.chartScript = uq.chartScript;

        const promise = UserAssetsClient.API.parseFilters({
            queryKey: uq.query.key,
            canAggregate: uq.groupResults,
            entity: entity,
            filters: uq.filters!.map(mle => mle.element).map(f => ({
                tokenString: f.token!.tokenString,
                operation: f.operation,
                valueString: f.valueString
            }) as UserAssetsClient.API.ParseFilterRequest)
        });

        return promise.then(filters => {

            cr.groupResults = uq.groupResults;

            cr.filterOptions = (cr.filterOptions || []).filter(f => f.frozen);
            cr.filterOptions.push(...uq.filters.map((f, i) => ({
                token: f.element.token!.token,
                operation: f.element.operation!,
                value: filters[i].value,
                frozen: false,
            }) as FilterOptionParsed));
            
            cr.parameters = uq.parameters!.map(mle => ({
                rowId: null,
                element: ChartParameterEmbedded.New({
                    name : mle.element.name,
                    value : mle.element.value,
                })
            }));

            cr.columns = uq.columns!.map(mle => {
                var t = mle.element.token;

                return ({
                    rowId: null,
                    element: ChartColumnEmbedded.New({
                        displayName: mle.element.displayName,

                        token: t && QueryTokenEmbedded.New({
                            token: t!.token,
                            tokenString: t!.tokenString
                        })
                    })
                })
            });

            cr.orderOptions = (uq.orders || []).map(f => ({
                token: f.element.token!.token,
                orderType: f.element.orderType
            }) as OrderOptionParsed);



            return cr;
        });
    }

    export function toChartRequest(uq: UserChartEntity, entity?: Lite<Entity>): Promise<ChartRequest> {
        const cs = ChartRequest.New({ queryKey: uq.query!.key }); 
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

    export function cleanedChartRequest(request: ChartRequest) {
        const clone = { ...request };
        clone.orders = clone.orderOptions!
            .map(oo => ({ token: oo.token.fullKey, orderType: oo.orderType }) as OrderRequest);
        delete clone.orderOptions;

        clone.filters = clone.filterOptions!
            .filter(a => a.token != null)
            .map(fo => ({ token: fo.token!.fullKey, operation: fo.operation, value: fo.value }) as FilterRequest);
        delete clone.filterOptions;

        return clone;
    }

    export function fromChartRequest(chartRequest: ChartRequest): Promise<UserChartEntity> {

        const clone = cleanedChartRequest(chartRequest);

        return ajaxPost<UserChartEntity>({ url: "~/api/userChart/fromChartRequest/" }, clone);
    }
}

declare module '../../../../Framework/Signum.React/Scripts/Signum.Entities' {

    export interface EntityPack<T extends ModifiableEntity> {
        userCharts?: Array<Lite<UserChartEntity>>;
    }
}
