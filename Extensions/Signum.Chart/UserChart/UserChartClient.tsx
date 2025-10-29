import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { Constructor } from '@framework/Constructor'
import { Finder } from '@framework/Finder'
import { Entity, getToString, Lite, liteKey, SelectorMessage, toLite, translated } from '@framework/Signum.Entities'
import { QuickLinkClient, QuickLinkAction } from '@framework/QuickLinkClient'
import { AuthClient } from '../../Signum.Authorization/AuthClient'
import { DashboardClient, CreateNewButton } from '../../Signum.Dashboard/DashboardClient';
import { ChartPermission, ChartMessage, ChartRequestModel, ChartParameterEmbedded, ChartColumnEmbedded, ChartTimeSeriesEmbedded } from '../Signum.Chart'
import UserChartMenu from './UserChartMenu'
import { ChartClient } from '../ChartClient'
import { UserAssetClient } from '../../Signum.UserAssets/UserAssetClient'
import { ImportComponent } from '@framework/ImportComponent'
import { CombinedUserChartPartEntity, UserChartEntity, UserChartLiteModel, UserChartPartEntity } from './Signum.Chart.UserChart';
import { QueryTokenEmbedded } from '../../Signum.UserAssets/Signum.UserAssets.Queries';
import SelectorModal from '@framework/SelectorModal';
import { UserChartPartHandler } from '../Dashboard/View/UserChartPart';
import { OmniboxClient } from '../../Signum.Omnibox/OmniboxClient';
import UserChartOmniboxProvider from './UserChartOmniboxProvider';
import { ToolbarClient } from '../../Signum.Toolbar/ToolbarClient';
import UserChartToolbarConfig from './UserChartToolbarConfig';

export namespace UserChartClient {
  
  export function start(options: { routes: RouteObject[] }): void {
    
    ToolbarClient.registerConfig(new UserChartToolbarConfig());
    OmniboxClient.registerProvider(new UserChartOmniboxProvider());
  
    UserAssetClient.start({ routes: options.routes });
    UserAssetClient.registerExportAssertLink(UserChartEntity);
  
    options.routes.push({ path: "/userChart/:userChartId/:entity?", element: <ImportComponent onImport={() => import("./UserChartPage")} /> });
  
  
    ChartClient.ButtonBarChart.onButtonBarElements.push(ctx => {
      if (!AppContext.isPermissionAuthorized(ChartPermission.ViewCharting) || !Navigator.isViewable(UserChartEntity))
        return undefined;
  
      return <UserChartMenu chartRequestView={ctx.chartRequestView} />;
    });
  
    if (AppContext.isPermissionAuthorized(ChartPermission.ViewCharting) && Navigator.isViewable(UserChartEntity))
      QuickLinkClient.registerGlobalQuickLink(entityType =>
        API.forEntityType(entityType)
          .then(ucs => ucs.map(uc =>
            new QuickLinkAction(liteKey(uc), () => getToString(uc), (ctx, e) => window.open(AppContext.toAbsoluteUrl(`/userChart/${uc.id}/${liteKey(ctx.lite)}`)),
              {
                onlyForToken: (uc.model as UserChartLiteModel).hideQuickLink,
                icon: "chart-bar", iconColor: "darkviolet"
              }
            ))
          ));
  
    QuickLinkClient.registerQuickLink(UserChartEntity, new QuickLinkAction("preview", () => ChartMessage.Preview.niceToString(),
      ctx => {
        Navigator.API.fetchAndRemember(ctx.lite).then(uc => {
          if (uc.entityType == undefined)
            window.open(AppContext.toAbsoluteUrl(`/userChart/${uc.id}`));
          else
            Navigator.API.fetch(uc.entityType)
              .then(t => Finder.find({ queryName: t.cleanName }))
              .then(lite => {
                if (!lite)
                  return;
  
                window.open(AppContext.toAbsoluteUrl(`/userChart/${uc.id}/${liteKey(lite)}`));
              });
        })
      },
      {
        isVisible: AppContext.isPermissionAuthorized(ChartPermission.ViewCharting), group: null, icon: "eye", iconColor: "blue", color: "info"
      }
    ));
  
   
    Navigator.addSettings(new EntitySettings(UserChartEntity, e => import('./UserChart'), { isCreable: "Never", modalSize: 'xl' }));
    Navigator.addSettings(new EntitySettings(UserChartPartEntity, e => import('../Dashboard/Admin/UserChartPart')));
    Navigator.addSettings(new EntitySettings(CombinedUserChartPartEntity, e => import('../Dashboard/Admin/CombinedUserChartPart')));
  
  
    DashboardClient.registerRenderer(UserChartPartEntity, {
      waitForInvalidation: true,
      component: () => import('../Dashboard/View/UserChartPart').then(a => a.default),
      icon: () => ({ icon: "chart-bar", iconColor: "#6C3483" }),
      defaultTitle: c => translated(c.userChart, uc => uc.displayName),
      getQueryNames: c => [c.userChart?.query].notNull(),
      handleEditClick: !Navigator.isViewable(UserChartPartEntity) || Navigator.isReadOnly(UserChartPartEntity) ? undefined :
        (c, e, cdRef, ev) => {
          return Navigator.view(c.userChart!).then(e => Boolean(e));
        },
      handleTitleClick: !AppContext.isPermissionAuthorized(ChartPermission.ViewCharting) ? undefined :
        (p, e, cdRef, ev) => {
          ev.persist();
          const handler = cdRef.current as UserChartPartHandler;
          ChartClient.Encoder.chartPathPromise(handler.chartRequest!, toLite(p.userChart!))
            .then(path => AppContext.pushOrOpenInTab(path, ev));
        },
      customTitleButtons: (c, entity, customDataRef) => {
        if (!c.createNew)
          return null;
  
        return <CreateNewButton queryKey={c.userChart.query.key} onClick={tis => {
          const handler = customDataRef.current as UserChartPartHandler;
          return SelectorModal.chooseType(tis)
            .then(ti => ti && Finder.getPropsFromFilters(ti, handler.chartRequest!.filterOptions)
              .then(props => Constructor.constructPack(ti.name, props)))
            .then(pack => pack && Navigator.view(pack))
            .then(() => handler.reloadQuery());
        }} />
      }
    });
  
    DashboardClient.registerRenderer(CombinedUserChartPartEntity, {
      component: () => import('../Dashboard/View/CombinedUserChartPart').then(a => a.default),
      icon: () => ({ icon: "chart-line", iconColor: "#8E44AD" }),
      getQueryNames: c => c.userCharts.map(a => a.element.userChart?.query).notNull(),
      handleEditClick: !Navigator.isViewable(UserChartPartEntity) || Navigator.isReadOnly(UserChartPartEntity) ? undefined :
        (c, e, cdRef, ev) => {
          return SelectorModal.chooseElement(c.userCharts.map(a => a.element), {
            buttonDisplay: a => a.userChart.displayName ?? "",
            buttonName: a => a.userChart.id!.toString(),
            title: SelectorMessage.SelectAnElement.niceToString(),
            message: SelectorMessage.PleaseSelectAnElement.niceToString()
          })
            .then(lite => lite && Navigator.view(lite!))
            .then(entity => Boolean(entity));
        },
      handleTitleClick: !AppContext.isPermissionAuthorized(ChartPermission.ViewCharting) ? undefined :
        (c, e, cdRef, ev) => {
          ev.persist();
          SelectorModal.chooseElement(c.userCharts.map(a => a.element), {
            buttonDisplay: a => a.userChart.displayName ?? "",
            buttonName: a => a.userChart.id!.toString(),
            title: SelectorMessage.SelectAnElement.niceToString(),
            message: SelectorMessage.PleaseSelectAnElement.niceToString()
          }).then(uc => {
            if (uc) {
              Converter.toChartRequest(uc.userChart, e)
                .then(cr => ChartClient.Encoder.chartPathPromise(cr, toLite(uc.userChart)))
                .then(path => AppContext.pushOrOpenInTab(path, ev));
            }
          });
        },
    });
  
  }
  
  
  export namespace Converter {
  
  
    export async function applyUserChart(cr: ChartRequestModel, uc: UserChartEntity, entity?: Lite<Entity>): Promise<ChartRequestModel> {
      cr.chartScript = uc.chartScript;
      cr.maxRows = uc.maxRows;
  
      const filters = await UserAssetClient.API.parseFilters({
        queryKey: uc.query.key,
        canAggregate: true,
        canTimeSeries: cr.chartTimeSeries != null,
        entity: entity,
        filters: uc.filters!.map(mle => UserAssetClient.Converter.toQueryFilterItem(mle.element))
      });
  
  
      cr.filterOptions = (cr.filterOptions ?? []).filter(f => f.frozen);
  
      cr.filterOptions.push(...filters.map(f => UserAssetClient.Converter.toFilterOptionParsed(f)));
  
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
              token: UserAssetClient.getToken(t),
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
  
    async function parseDate(dateExpression: string | null): Promise<string | undefined> {
      if (dateExpression == null)
        return undefined;

      var date = await UserAssetClient.API.parseDate(dateExpression);

      return date;
    }
    
    export async function toChartRequest(uq: UserChartEntity, entity?: Lite<Entity>): Promise<ChartRequestModel> {
      var ts = uq.chartTimeSeries;
      const cs = ChartRequestModel.New({ queryKey: uq.query!.key, chartTimeSeries: !ts ? null : ChartTimeSeriesEmbedded.New({
        timeSeriesUnit: ts.timeSeriesUnit,
        startDate: await parseDate(ts.startDate),
        endDate: await parseDate(ts.endDate),
        timeSeriesStep: ts.timeSeriesStep,
        timeSeriesMaxRowsPerStep: ts.timeSeriesMaxRowsPerStep,
        splitQueries: ts.splitQueries,
      })});
      return applyUserChart(cs, uq, entity);
    }
  }
  
  
  export namespace API {
    export function forEntityType(type: string): Promise<Lite<UserChartEntity>[]> {
      return ajaxGet({ url: "/api/userChart/forEntityType/" + type });
    }
  
    export function forQuery(queryKey: string): Promise<Lite<UserChartEntity>[]> {
      return ajaxGet({ url: "/api/userChart/forQuery/" + queryKey });
    }
  }
  

}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    userCharts?: Array<Lite<UserChartEntity>>;
  }
}
