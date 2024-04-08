import * as React from 'react'
import { RouteObject } from 'react-router'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { ajaxGet, ajaxPost } from '@framework/Services';
import { Constructor } from '@framework/Constructor';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Operations, EntityOperationSettings } from '@framework/Operations'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { Entity, Lite, liteKey, toLite, EntityPack, getToString, SearchMessage, translated } from '@framework/Signum.Entities'
import * as QuickLinks from '@framework/QuickLinks'
import { getTypeInfos, getTypeName, PseudoType, Type, TypeInfo } from '@framework/Reflection'
import { onEmbeddedWidgets, EmbeddedWidget } from '@framework/Frames/Widgets'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import {
  DashboardPermission, DashboardEntity, LinkListPartEntity, IPartEntity, DashboardMessage, PanelPartEmbedded,
  CachedQueryEntity, DashboardOperation, ImagePartEntity, SeparatorPartEntity, DashboardLiteModel
} from './Signum.Dashboard'
import { UserAssetClient } from '../Signum.UserAssets/UserAssetClient'
import { ImportComponent } from '@framework/ImportComponent'
import { useAPI } from '@framework/Hooks';
import { DashboardController } from "./View/DashboardFilterController";
import { EntityFrame } from '@framework/TypeContext';
import { CachedQueryJS } from './CachedQueryExecutor';
import { QueryEntity } from '@framework/Signum.Basics';
import { downloadFile } from '../Signum.Files/Components/FileDownloader';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { QueryDescription } from '@framework/FindOptions';
import { ToolbarClient } from '../Signum.Toolbar/ToolbarClient';
import { OmniboxClient } from '../Signum.Omnibox/OmniboxClient';
import DashboardToolbarConfig from './DashboardToolbarConfig';
import DashboardOmniboxProvider from './DashboardOmniboxProvider';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';
import { parseIcon } from '@framework/Components/IconTypeahead';

export namespace DashboardClient {
  


interface IconColor {
  icon: IconProp;
  iconColor: string;
}

export interface PartRenderer<T extends IPartEntity> {
  component: () => Promise<React.ComponentType<PanelPartContentProps<T>>>;
  waitForInvalidation?: boolean;
  defaultIcon: () => IconColor;
  defaultTitle?: (elenent: T) => string;
  withPanel?: (element: T) => boolean;
  getQueryNames?: (element: T) => QueryEntity[];
  handleTitleClick?: (content: T, entity: Lite<Entity> | undefined, customDataRef: React.MutableRefObject<any>, e: React.MouseEvent<any>) => void;
  handleEditClick?: (content: T, entity: Lite<Entity> | undefined, customDataRef: React.MutableRefObject<any>, e: React.MouseEvent<any>) => Promise<boolean>;
  customTitleButtons?: (content: T, entity: Lite<Entity> | undefined, customDataRef: React.MutableRefObject<any>) => React.ReactNode;
}


export const partRenderers: { [typeName: string]: PartRenderer<IPartEntity> } = {};

export function start(options: { routes: RouteObject[] }) {

    ChangeLogClient.registerChangeLogModule("Signum.Dashboard", () => import("./Changelog"));

  UserAssetClient.start({ routes: options.routes });
  UserAssetClient.registerExportAssertLink(DashboardEntity);

  Constructor.registerConstructor(DashboardEntity, () => DashboardEntity.New({ owner: AppContext.currentUser && toLite(AppContext.currentUser) }));

  Navigator.addSettings(new EntitySettings(DashboardEntity, e => import('./Admin/Dashboard')));
  Navigator.addSettings(new EntitySettings(CachedQueryEntity, e => import('./Admin/CachedQuery')));

  Navigator.addSettings(new EntitySettings(LinkListPartEntity, e => import('./Admin/LinkListPart')));
  Navigator.addSettings(new EntitySettings(ImagePartEntity, e => import('./Admin/ImagePart')));
  Navigator.addSettings(new EntitySettings(SeparatorPartEntity, e => import('./Admin/SeparatorPart')));

  ToolbarClient.registerConfig(new DashboardToolbarConfig());
  OmniboxClient.registerProvider(new DashboardOmniboxProvider());

  Operations.addSettings(new EntityOperationSettings(DashboardOperation.RegenerateCachedQueries, {
    isVisible: () => false,
    color: "warning",
    icon: "gears",
    contextual: { isVisible: () => true },
    contextualFromMany: { isVisible: () => true },
  }));

  Finder.addSettings({
    queryName: DashboardEntity,
    defaultOrders: [{ token: DashboardEntity.token(d => d.dashboardPriority), orderType: "Descending" }]
  });

  options.routes.push({ path: "/dashboard/:dashboardId", element: <ImportComponent onImport={() => import("./View/DashboardPage")} /> });

  registerRenderer(LinkListPartEntity, {
    component: () => import('./View/LinkListPart').then(a => a.default),
    defaultIcon: () => ({ icon: "list", iconColor: "#B9770E" })
  });

  registerRenderer(ImagePartEntity, {
    component: () => import('./View/ImagePartView').then(a => a.default),
    defaultIcon: () => ({ icon: "rectangle-list", iconColor: "forestgreen" }),
    withPanel: () => false
  });
  registerRenderer(SeparatorPartEntity, {
    component: () => import('./View/SeparatorPartView').then(a => a.default),
    defaultIcon: () => ({ icon: "rectangle-list", iconColor: "forestgreen" }),
    withPanel: () => false
  });

  onEmbeddedWidgets.push(wc => {
    if (!wc.frame.pack.embeddedDashboards)
      return undefined;

    return wc.frame.pack.embeddedDashboards.map(d => {
      return {
        position: d.embeddedInEntity as "Top" | "Tab" | "Bottom",
        embeddedWidget: <DashboardWidget dashboard={d} pack={wc.frame.pack as EntityPack<Entity>} frame={wc.frame} />,
        eventKey: liteKey(toLite(d)),
        title: Options.customTitle(d),
      } as EmbeddedWidget;
    });
  });

  if (AppContext.isPermissionAuthorized(DashboardPermission.ViewDashboard))
    QuickLinks.registerGlobalQuickLink(entityType =>
      API.forEntityType(entityType)
        .then(ds => ds.map(d => new QuickLinks.QuickLinkAction(liteKey(d), () => getToString(d), (ctx, e) => AppContext.pushOrOpenInTab(dashboardUrl(d, ctx.lite), e),
        {
          order: 0,
          icon: "gauge",
          iconColor: "darkslateblue",
          color: "success",
          onlyForToken: (d.model as DashboardLiteModel).hideQuickLink
        }
      )))
    );

  QuickLinks.registerQuickLink(DashboardEntity, new QuickLinks.QuickLinkAction("preview", () => DashboardMessage.Preview.niceToString(),
    (ctx, e) => Navigator.API.fetchAndRemember(ctx.lite)
      .then(db => {
        if (db.entityType == undefined)
          AppContext.pushOrOpenInTab(dashboardUrl(ctx.lite), e);
        else
          Navigator.API.fetchAndRemember(db.entityType)
            .then(t => Finder.find({ queryName: t.cleanName }))
            .then(entity => {
              if (!entity)
                return;

              AppContext.pushOrOpenInTab(dashboardUrl(ctx.lite, entity), e);
            });
      }),
    {
      group: null,
      icon: "eye",
      iconColor: "blue",
      color: "info"
    }
  ));
};


export function home(): Promise<Lite<DashboardEntity> | null> {
  if (!Navigator.isViewable(DashboardEntity))
    return Promise.resolve(null);

  return API.home();
}

export function hasWaitForInvalidation(type: PseudoType) {
  return partRenderers[getTypeName(type)].waitForInvalidation;
}

export function defaultIcon(type: PseudoType) {
  return partRenderers[getTypeName(type)].defaultIcon();
}

export function getQueryNames(part: IPartEntity) {
  return partRenderers[getTypeName(part)].getQueryNames?.(part) ?? [];
}

export function dashboardUrl(lite: Lite<DashboardEntity>, entity?: Lite<Entity>) {
  return "/dashboard/" + lite.id + (!entity ? "" : "?entity=" + liteKey(entity));
}

export function registerRenderer<T extends IPartEntity>(type: Type<T>, renderer: PartRenderer<T>) {
  partRenderers[type.typeName] = renderer as PartRenderer<any> as PartRenderer<IPartEntity>;
}

export module API {
  export function forEntityType(type: string): Promise<Lite<DashboardEntity>[]> {
    return ajaxGet({ url: `/api/dashboard/forEntityType/${type}` });
  }

  export function home(): Promise<Lite<DashboardEntity> | null> {
    return ajaxGet({ url: "/api/dashboard/home" });
  }

  export function get(dashboard: Lite<DashboardEntity>): Promise<DashboardWithCachedQueries | null> {
    return ajaxPost({ url: "/api/dashboard/get" }, dashboard);
  }
}

export interface DashboardWithCachedQueries {
  dashboard: DashboardEntity
  cachedQueries: Array<CachedQueryEntity>;
}

export interface DashboardWidgetProps {
  pack: EntityPack<Entity>;
  dashboard: DashboardEntity;
  frame: EntityFrame;
}

export function DashboardWidget(p: DashboardWidgetProps) {

  const component = useAPI(() => import("./View/DashboardView").then(mod => mod.default), []);

  if (!component)
    return null;

  return React.createElement(component, {
    dashboard: p.dashboard,
    entity: p.pack.entity,
    reload: () => p.frame.onReload(),
    cachedQueries: {} /*for now*/
  });
}

export function toCachedQueries(dashboardWithQueries?: DashboardWithCachedQueries | null) {

  if (!dashboardWithQueries)
    return undefined;

  const result = dashboardWithQueries.cachedQueries
    .map(a => ({ userAssets: a.userAssets, promise: downloadFile(a.file).then(r => r.json() as Promise<CachedQueryJS>).then(cq => { Finder.decompress(cq.resultTable); return cq; }) })) //share promise
    .flatMap(a => a.userAssets.map(mle => ({ ua: mle.element, promise: a.promise })))
    .toObject(a => liteKey(a.ua), a => a.promise);

  return result;
}

export namespace Options {

  export let customTitle: (dashboard: DashboardEntity) => React.ReactNode = d => <DashboardTitle dashboard={d} />;
}

}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    dashboards?: Array<Lite<DashboardEntity>>;
    embeddedDashboards?: DashboardEntity[];
  }
}


export function CreateNewButton(p: { queryKey: string, onClick: (types: TypeInfo[], qd: QueryDescription) => void }) {

  const qd = useAPI(() => Finder.getQueryDescription(p.queryKey), [p.queryKey]);

  if (qd == null)
    return null;

  const tis = getTypeInfos(qd.columns["Entity"].type).filter(ti => Navigator.isCreable(ti, { isSearch: true }));

  if (tis.length == 0)
    return null;

  const types = tis.map(ti => ti.niceName).join(", ");
  const gender = tis.first().gender;

  var title = SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(gender).formatWith(types);

  return (
    <a onClick={e => { e.preventDefault(); p.onClick(tis, qd); }} href="#" className="btn btn-sm btn-light sf-create me-2" title={title}>
      <FontAwesomeIcon icon={"plus"} /> {title}
    </a>
  );
}

export interface PanelPartContentProps<T extends IPartEntity> {
  partEmbedded: PanelPartEmbedded;
  content: T;
  entity?: Lite<Entity>;
  deps?: React.DependencyList;
  dashboardController: DashboardController;
  customDataRef: React.MutableRefObject<any>;
  cachedQueries: {
    [userAssetKey: string]: Promise<CachedQueryJS>
  }
}

export function DashboardTitle(p: { dashboard: DashboardEntity }) {

  const icon = parseIcon(p.dashboard.iconName);
  const title = <span style={{ color: p.dashboard.titleColor ?? undefined }}>
    {translated(p.dashboard, d => d.displayName)}
  </span>;

  if (icon == null)
    return title;

  return (
    <div className="dashboard-title">
      <FontAwesomeIcon icon={icon} color={p.dashboard.iconColor ?? undefined} />
      &nbsp;{title}
    </div>
  );
}


