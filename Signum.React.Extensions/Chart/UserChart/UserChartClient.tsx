import * as React from 'react'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Entity, getToString, Lite, liteKey } from '@framework/Signum.Entities'
import * as QuickLinks from '@framework/QuickLinks'
import * as AuthClient from '../../Authorization/AuthClient'
import { UserChartEntity, ChartPermission, ChartMessage, ChartRequestModel, ChartParameterEmbedded, ChartColumnEmbedded } from '../Signum.Entities.Chart'
import { QueryFilterEmbedded, QueryOrderEmbedded } from '../../UserQueries/Signum.Entities.UserQueries'
import { QueryTokenEmbedded } from '../../UserAssets/Signum.Entities.UserAssets'
import UserChartMenu from './UserChartMenu'
import * as ChartClient from '../ChartClient'
import * as UserAssetsClient from '../../UserAssets/UserAssetClient'
import { ImportRoute } from "@framework/AsyncImport";

export function start(options: { routes: JSX.Element[] }) {

  UserAssetsClient.start({ routes: options.routes });
  UserAssetsClient.registerExportAssertLink(UserChartEntity);

  options.routes.push(<ImportRoute path="~/userChart/:userChartId/:entity?" onImportModule={() => import("./UserChartPage")} />);


  ChartClient.ButtonBarChart.onButtonBarElements.push(ctx => {
    if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting) || !Navigator.isViewable(UserChartEntity))
      return undefined;

    return <UserChartMenu chartRequestView={ctx.chartRequestView} />;
  });

  QuickLinks.registerGlobalQuickLink(ctx => {
    if (!AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting) || !Navigator.isViewable(UserChartEntity))
      return undefined;

    var promise = ctx.widgetContext ?
      Promise.resolve(ctx.widgetContext.frame.pack.userCharts ?? []) :
      API.forEntityType(ctx.lite.EntityType);

    return promise.then(uqs =>
      uqs.map(uc => new QuickLinks.QuickLinkAction(liteKey(uc), () => getToString(uc) ?? "", e => {
        window.open(AppContext.toAbsoluteUrl(`~/userChart/${uc.id}/${liteKey(ctx.lite)}`));
      }, { icon: "chart-bar", iconColor: "darkviolet" })));
  });

  QuickLinks.registerQuickLink(UserChartEntity, ctx => new QuickLinks.QuickLinkAction("preview", () => ChartMessage.Preview.niceToString(),
    e => {
      Navigator.API.fetchAndRemember(ctx.lite).then(uc => {
        if (uc.entityType == undefined)
          window.open(AppContext.toAbsoluteUrl(`~/userChart/${uc.id}`));
        else
          Navigator.API.fetch(uc.entityType)
            .then(t => Finder.find({ queryName: t.cleanName }))
            .then(lite => {
              if (!lite)
                return;

              window.open(AppContext.toAbsoluteUrl(`~/userChart/${uc.id}/${liteKey(lite)}`));
            });
      });
    }, { isVisible: AuthClient.isPermissionAuthorized(ChartPermission.ViewCharting), group: null, icon: "eye", iconColor: "blue", color: "info" }));


  Navigator.addSettings(new EntitySettings(UserChartEntity, e => import('./UserChart'), { isCreable: "Never" }));
}


export module Converter {


  export async function applyUserChart(cr: ChartRequestModel, uc: UserChartEntity, entity?: Lite<Entity>): Promise<ChartRequestModel> {
    cr.chartScript = uc.chartScript;
    cr.maxRows = uc.maxRows;
    cr.customDrilldowns = uc.customDrilldowns;

    const filters = await UserAssetsClient.API.parseFilters({
      queryKey: uc.query.key,
      canAggregate: true,
      entity: entity,
      filters: uc.filters!.map(mle => UserAssetsClient.Converter.toQueryFilterItem(mle.element))
    });


    cr.filterOptions = (cr.filterOptions ?? []).filter(f => f.frozen);

    cr.filterOptions.push(...filters.map(f => UserAssetsClient.Converter.toFilterOptionParsed(f)));

    await Finder.parseFilterValues(cr.filterOptions);

      cr.parameters = uc.parameters.map(mle => ({
        rowId: null,
        element: ChartParameterEmbedded.New({
          name: mle.element.name,
          value: mle.element.value,
        })
      }));

      cr.columns = uc.columns.map(mle => {
        var t = mle.element.token;

      return ({
        rowId: null,
        element: ChartColumnEmbedded.New({
          displayName: mle.element.displayName,
          format: mle.element.format,

          token: t && QueryTokenEmbedded.New({
            token: UserAssetsClient.getToken(t),
            tokenString: t.tokenString
          }),

          orderByIndex: mle.element.orderByIndex,
          orderByType: mle.element.orderByType,
        })
      })
    });

    return ChartClient.getChartScript(cr.chartScript)
      .then(cs => {
        ChartClient.synchronizeColumns(cr, cs);
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
    return ajaxGet({ url: "~/api/userChart/forEntityType/" + type });
  }

  export function forQuery(queryKey: string): Promise<Lite<UserChartEntity>[]> {
    return ajaxGet({ url: "~/api/userChart/forQuery/" + queryKey });
  }
}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    userCharts?: Array<Lite<UserChartEntity>>;
  }
}
