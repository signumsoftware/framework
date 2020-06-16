import * as React from 'react'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { ajaxGet } from '@framework/Services';
import * as Constructor from '@framework/Constructor';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Entity, Lite, liteKey, toLite, EntityPack } from '@framework/Signum.Entities'
import * as QuickLinks from '@framework/QuickLinks'
import { Type } from '@framework/Reflection'
import { onEmbeddedWidgets } from '@framework/Frames/Widgets'
import * as AuthClient from '../Authorization/AuthClient'
import * as ChartClient from '../Chart/ChartClient'
import * as UserChartClient from '../Chart/UserChart/UserChartClient'
import * as UserQueryClient from '../UserQueries/UserQueryClient'
import { DashboardPermission, DashboardEntity, ValueUserQueryListPartEntity, LinkListPartEntity, UserChartPartEntity, UserQueryPartEntity, IPartEntity, DashboardMessage, PanelPartEmbedded } from './Signum.Entities.Dashboard'
import * as UserAssetClient from '../UserAssets/UserAssetClient'
import { ImportRoute } from "@framework/AsyncImport";
import { useAPI } from '../../../Framework/Signum.React/Scripts/Hooks';


export interface PanelPartContentProps<T extends IPartEntity> {
  partEmbedded: PanelPartEmbedded;
  part: T;
  entity?: Lite<Entity>;
}

interface IconColor {
  icon: IconProp;
  iconColor: string;
}

export interface PartRenderer<T extends IPartEntity> {
  component: () => Promise<React.ComponentType<PanelPartContentProps<T>>>;
  defaultIcon: (element: T) => IconColor;
  withPanel?: (element: T) => boolean;
  handleTitleClick?: (part: T, entity: Lite<Entity> | undefined, e: React.MouseEvent<any>) => void;
  handleEditClick?: (part: T, entity: Lite<Entity> | undefined, e: React.MouseEvent<any>) => void;
}


export const partRenderers: { [typeName: string]: PartRenderer<IPartEntity> } = {};

export function start(options: { routes: JSX.Element[] }) {

  UserAssetClient.start({ routes: options.routes });
  UserAssetClient.registerExportAssertLink(DashboardEntity);

  Constructor.registerConstructor(DashboardEntity, () => DashboardEntity.New({ owner: Navigator.currentUser && toLite(Navigator.currentUser) }));

  Navigator.addSettings(new EntitySettings(DashboardEntity, e => import('./Admin/Dashboard')));

  Navigator.addSettings(new EntitySettings(ValueUserQueryListPartEntity, e => import('./Admin/ValueUserQueryListPart')));
  Navigator.addSettings(new EntitySettings(LinkListPartEntity, e => import('./Admin/LinkListPart')));
  Navigator.addSettings(new EntitySettings(UserChartPartEntity, e => import('./Admin/UserChartPart')));
  Navigator.addSettings(new EntitySettings(UserQueryPartEntity, e => import('./Admin/UserQueryPart')));

  Finder.addSettings({ queryName: DashboardEntity, defaultOrderColumn: DashboardEntity.token(d => d.dashboardPriority), defaultOrderType: "Descending" });

  options.routes.push(<ImportRoute path="~/dashboard/:dashboardId" onImportModule={() => import("./View/DashboardPage")} />);

  registerRenderer(ValueUserQueryListPartEntity, {
    component: () => import('./View/ValueUserQueryListPart').then(a => a.default),
    defaultIcon: () => ({ icon: ["far", "list-alt"], iconColor: "lightblue" })
  });
  registerRenderer(LinkListPartEntity, {
    component: () => import('./View/LinkListPart').then(a => a.default),
    defaultIcon: () => ({ icon: ["far", "list-alt"], iconColor: "forestgreen" })
  });
  registerRenderer(UserChartPartEntity, {
    component: () => import('./View/UserChartPart').then(a => a.default),
    defaultIcon: () => ({ icon: "chart-bar", iconColor: "violet" }),
    handleEditClick: !Navigator.isViewable(UserChartPartEntity) || Navigator.isReadOnly(UserChartPartEntity) ? undefined :
      (p, e, ev) => {
        ev.preventDefault();
        Navigator.pushOrOpenInTab(Navigator.navigateRoute(p.userChart!), ev);
      },
    handleTitleClick: !Navigator.isViewable(UserChartPartEntity) || Navigator.isReadOnly(UserChartPartEntity) ? undefined :
      (p, e, ev) => {
        ev.preventDefault();
        ev.persist();
        UserChartClient.Converter.toChartRequest(p.userChart!, e)
          .then(cr => ChartClient.Encoder.chartPathPromise(cr, toLite(p.userChart!)))
          .then(path => Navigator.pushOrOpenInTab(path, ev))
          .done();
      },
  });

  registerRenderer(UserQueryPartEntity, {
    component: () => import('./View/UserQueryPart').then((a: any) => a.default),
    defaultIcon: () => ({ icon: ["far", "list-alt"], iconColor: "dodgerblue" }),
    withPanel: p => p.renderMode != "BigValue",
    handleEditClick: !Navigator.isViewable(UserQueryPartEntity) || Navigator.isReadOnly(UserQueryPartEntity) ? undefined :
      (p, e, ev) => {
        ev.preventDefault();
        Navigator.pushOrOpenInTab(Navigator.navigateRoute(p.userQuery!), ev);
      },
    handleTitleClick: !Navigator.isViewable(UserQueryPartEntity) || Navigator.isReadOnly(UserQueryPartEntity) ? undefined :
      (p, e, ev) => {
        ev.preventDefault();
        ev.persist();
        UserQueryClient.Converter.toFindOptions(p.userQuery!, e)
          .then(cr => Navigator.pushOrOpenInTab(Finder.findOptionsPath(cr, { userQuery: liteKey(toLite(p.userQuery!)) }), ev))
          .done()
      }
  });

  onEmbeddedWidgets.push(wc => wc.frame.pack.embeddedDashboard &&
    {
      position: wc.frame.pack.embeddedDashboard.embeddedInEntity as "Top" | "Bottom",
      embeddedWidget: <DashboardWidget dashboard={wc.frame.pack.embeddedDashboard} pack={wc.frame.pack as EntityPack<Entity>} />
    });

  QuickLinks.registerGlobalQuickLink(ctx => {
    if (!AuthClient.isPermissionAuthorized(DashboardPermission.ViewDashboard))
      return undefined;

    var promise = ctx.widgetContext ?
      Promise.resolve(ctx.widgetContext.frame.pack.dashboards ?? []) :
      API.forEntityType(ctx.lite.EntityType);

    return promise.then(das =>
      das.map(d => new QuickLinks.QuickLinkAction(liteKey(d), d.toStr ?? "", e => {
        Navigator.pushOrOpenInTab(dashboardUrl(d, ctx.lite), e)
      }, { icon: "tachometer-alt", iconColor: "darkslateblue" })));
  });

  QuickLinks.registerQuickLink(DashboardEntity, ctx => new QuickLinks.QuickLinkAction("preview", DashboardMessage.Preview.niceToString(),
    e => Navigator.API.fetchAndRemember(ctx.lite)
      .then(db => {
        if (db.entityType == undefined)
          Navigator.pushOrOpenInTab(dashboardUrl(ctx.lite), e);
        else
          Navigator.API.fetchAndRemember(db.entityType)
            .then(t => Finder.find({ queryName: t.cleanName }))
            .then(entity => {
              if (!entity)
                return;

              Navigator.pushOrOpenInTab(dashboardUrl(ctx.lite, entity), e);
            }).done();
      }).done()));
}

export function defaultIcon<T extends IPartEntity>(part: T) {
  return partRenderers[part.Type].defaultIcon(part);
}

export function dashboardUrl(lite: Lite<DashboardEntity>, entity?: Lite<Entity>) {
  return "~/dashboard/" + lite.id + (!entity ? "" : "?entity=" + liteKey(entity));
}

export function registerRenderer<T extends IPartEntity>(type: Type<T>, renderer: PartRenderer<T>) {
  partRenderers[type.typeName] = renderer as PartRenderer<any> as PartRenderer<IPartEntity>;
}

export module API {
  export function forEntityType(type: string): Promise<Lite<DashboardEntity>[]> {
    return ajaxGet({ url: `~/api/dashboard/forEntityType/${type}` });
  }

  export function home(): Promise<Lite<DashboardEntity> | null> {
    return ajaxGet({ url: "~/api/dashboard/home" });
  }
}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    dashboards?: Array<Lite<DashboardEntity>>;
    embeddedDashboard?: DashboardEntity;
  }
}

export interface DashboardWidgetProps {
  pack: EntityPack<Entity>;
  dashboard: DashboardEntity;
}

export function DashboardWidget(p: DashboardWidgetProps) {

  const component = useAPI(() => import("./View/DashboardView").then(mod => mod.default), []);

  if (!component)
    return null;

  return React.createElement(component, {
    dashboard: p.dashboard,
    entity: p.pack.entity
  });
}

