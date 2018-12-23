import * as React from 'react'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Entity, Lite, liteKey } from '@framework/Signum.Entities'
import * as QuickLinks from '@framework/QuickLinks'
import { OrderOptionParsed } from '@framework/FindOptions'
import * as AuthClient from '../../Authorization/AuthClient'
import { UserChartEntity, ChartPermission, ChartMessage, ChartRequestModel, ChartParameterEmbedded, ChartColumnEmbedded } from '../Signum.Entities.Chart'
import { QueryFilterEmbedded, QueryOrderEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import UserChartMenu from './UserChartMenu'
import * as ChartClient from '../ChartClient'
import * as UserAssetsClient from '../../UserAssets/UserAssetClient'
import { ImportRoute } from "@framework/AsyncImport";
import { OrderRequest } from '@framework/FindOptions';
import { toFilterRequests } from '@framework/Finder';

export function start(options: { routes: JSX.Element[] }) {

  UserAssetsClient.start({ routes: options.routes });
  UserAssetsClient.registerExportAssertLink(UserChartEntity);

  options.routes.push(<ImportRoute path="~/userChart/:userChartId/:entity?" onImportModule={() => import("./UserChartPage")} />);


  ChartClient.ButtonBarChart.onButtonBarElements.push(ctx => {
    if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting))
      return undefined;

    return <UserChartMenu chartRequestView={ctx.chartRequestView} />;
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
      }, { icon: "chart-bar", iconColor: "darkviolet" })));
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


  export function applyUserChart(cr: ChartRequestModel, uq: UserChartEntity, entity?: Lite<Entity>): Promise<ChartRequestModel> {
    cr.chartScript = uq.chartScript;

    const promise = UserAssetsClient.API.parseFilters({
      queryKey: uq.query.key,
      canAggregate: true,
      entity: entity,
      filters: uq.filters!.map(mle => UserAssetsClient.Converter.toQueryFilterItem(mle.element))
    });

    return promise.then(filters => {

      cr.filterOptions = (cr.filterOptions || []).filter(f => f.frozen);

      cr.filterOptions.push(...filters.map(f => UserAssetsClient.Converter.toFilterOptionParsed(f)));

      cr.parameters = uq.parameters!.map(mle => ({
        rowId: null,
        element: ChartParameterEmbedded.New({
          name: mle.element.name,
          value: mle.element.value,
        })
      }));

      cr.columns = uq.columns!.map(mle => {
        var t = mle.element.token;

        return ({
          rowId: null,
          element: ChartColumnEmbedded.New({
            displayName: mle.element.displayName,

            token: t && QueryTokenEmbedded.New({
              token: UserAssetsClient.getToken(t),
              tokenString: t.tokenString
            }),

            orderByIndex: mle.element.orderByIndex,
            orderByType: mle.element.orderByType,
          })
        })
      });
      
      return cr;
    });
  }


  export function toChartRequest(uq: UserChartEntity, entity?: Lite<Entity>): Promise<ChartRequestModel> {
    const cs = ChartRequestModel.New({ queryKey: uq.query!.key });
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
}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    userCharts?: Array<Lite<UserChartEntity>>;
  }
}
