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
import { QuickLinkClient, QuickLinkAction } from '@framework/QuickLinkClient'
import { getTypeInfos, getTypeName, PseudoType, Type, TypeInfo } from '@framework/Reflection'
import { onEmbeddedWidgets, EmbeddedWidget } from '@framework/Frames/Widgets'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import {
  DashboardPermission, DashboardEntity, ToolbarPartEntity, IPartEntity, DashboardMessage, PanelPartEmbedded,
  CachedQueryEntity, DashboardOperation, ImagePartEntity, SeparatorPartEntity, DashboardLiteModel,
  HealthCheckPartEntity, CustomPartEntity,
  TextPartEntity
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
    icon: () => IconColor;
    defaultTitle?: (elenent: T) => string;
    withPanel?: (element: T, entity: Lite<Entity> | undefined) => boolean;
    getQueryNames?: (element: T) => QueryEntity[];
    handleTitleClick?: (content: T, entity: Lite<Entity> | undefined, customDataRef: React.MutableRefObject<any>, e: React.MouseEvent<any>) => void;
    handleEditClick?: (content: T, entity: Lite<Entity> | undefined, customDataRef: React.MutableRefObject<any>, e: React.MouseEvent<any>) => Promise<boolean>;
    customTitleButtons?: (content: T, entity: Lite<Entity> | undefined, customDataRef: React.MutableRefObject<any>) => React.ReactNode;
  }


  export const partRenderers: { [typeName: string]: PartRenderer<IPartEntity> } = {};
  export const GlobalVariables: Map<string, () => string> = new Map<string, () => string>();

  export function start(options: { routes: RouteObject[] }): void {

    ChangeLogClient.registerChangeLogModule("Signum.Dashboard", () => import("./Changelog"));

    UserAssetClient.start({ routes: options.routes });
    UserAssetClient.registerExportAssertLink(DashboardEntity);

    Constructor.registerConstructor(DashboardEntity, () => DashboardEntity.New({ owner: AppContext.currentUser && toLite(AppContext.currentUser) }));

    Navigator.addSettings(new EntitySettings(DashboardEntity, e => import('./Admin/Dashboard'), { modalSize: "xl" }));
    Navigator.addSettings(new EntitySettings(CachedQueryEntity, e => import('./Admin/CachedQuery')));

    Navigator.addSettings(new EntitySettings(CustomPartEntity, e => import('./Admin/CustomPart')));
    Navigator.addSettings(new EntitySettings(TextPartEntity, e => import('./Admin/TextPart')));
    Navigator.addSettings(new EntitySettings(ToolbarPartEntity, e => import('./Admin/ToolbarPart')));
    Navigator.addSettings(new EntitySettings(ImagePartEntity, e => import('./Admin/ImagePart')));
    Navigator.addSettings(new EntitySettings(SeparatorPartEntity, e => import('./Admin/SeparatorPart')));
    Navigator.addSettings(new EntitySettings(HealthCheckPartEntity, e => import('./Admin/HealthCheckPart')));

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

    registerRenderer(TextPartEntity, {
      component: () => import('./View/TextPart').then(a => a.default),
      icon: () => ({ icon: "code", iconColor: "#000000" }),
      withPanel: () => false,
    });

    registerRenderer(ToolbarPartEntity, {
      component: () => import('./View/ToolbarPart').then(a => a.default),
      icon: () => ({ icon: "list", iconColor: "#B9770E" })
    });

    registerRenderer(ImagePartEntity, {
      component: () => import('./View/ImagePartView').then(a => a.default),
      icon: () => ({ icon: "rectangle-list", iconColor: "forestgreen" }),
      withPanel: () => false
    });

    registerRenderer(SeparatorPartEntity, {
      component: () => import('./View/SeparatorPartView').then(a => a.default),
      icon: () => ({ icon: "rectangle-list", iconColor: "forestgreen" }),
      withPanel: () => false
    });

    registerRenderer(HealthCheckPartEntity, {
      component: () => import('./View/HealthCheckPart').then(a => a.default),
      icon: () => ({ icon: "heart-pulse", iconColor: "forestgreen" }),
      withPanel: () => false
    });

    registerRenderer(CustomPartEntity, {
      component: () => import('./View/CustomPart').then(a => a.default),
      icon: () => ({ icon: "cube", iconColor: "forestgreen" }),
      withPanel: (cp, e) => DashboardClient.Options.customPartRenderers[e?.EntityType ?? "NONE"]?.[cp.customPartName]?.withPanel ?? true,
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
      QuickLinkClient.registerGlobalQuickLink(entityType =>
        API.forEntityType(entityType)
          .then(ds => ds.map(d => new QuickLinkAction(liteKey(d), () => getToString(d), (ctx, e) => AppContext.pushOrOpenInTab(dashboardUrl(d, ctx.lite), e),
            {
              order: 0,
              icon: "gauge",
              iconColor: "darkslateblue",
              color: "success",
              onlyForToken: (d.model as DashboardLiteModel).hideQuickLink
            }
          )))
      );

    QuickLinkClient.registerQuickLink(DashboardEntity, new QuickLinkAction("preview", () => DashboardMessage.Preview.niceToString(),
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

    GlobalVariables.set('UserName', () => AuthClient.currentUser().userName);
  };


  export function home(): Promise<Lite<DashboardEntity> | null> {
    if (!Navigator.isViewable(DashboardEntity))
      return Promise.resolve(null);

    return API.home();
  }

  export function hasWaitForInvalidation(type: PseudoType): boolean | undefined {
    return partRenderers[getTypeName(type)].waitForInvalidation;
  }

  export function icon(type: PseudoType): IconColor {
    return partRenderers[getTypeName(type)].icon();
  }

  export function getQueryNames(part: IPartEntity): QueryEntity[] {
    return partRenderers[getTypeName(part)]?.getQueryNames?.(part) ?? [];
  }

  export function dashboardUrl(lite: Lite<DashboardEntity>, entity?: Lite<Entity>): string {
    return "/dashboard/" + lite.id + (!entity ? "" : "?entity=" + liteKey(entity));
  }

  export function registerRenderer<T extends IPartEntity>(type: Type<T>, renderer: PartRenderer<T>): void {
    partRenderers[type.typeName] = renderer as PartRenderer<any> as PartRenderer<IPartEntity>;
  }

  export namespace API {
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

  export function DashboardWidget(p: DashboardWidgetProps): React.FunctionComponentElement<{
    dashboard: DashboardEntity;
    cachedQueries: {
      [userAssetKey: string]: Promise<CachedQueryJS>;
    };
    entity?: Entity;
    deps?: React.DependencyList;
    reload: () => void;
    hideEditButton?: boolean;
  }> | null {

    const component = useAPI(() => import("./View/DashboardView").then(mod => mod.default), []);

    if (!component)
      return null;

    return React.createElement(component, {
      dashboard: p.dashboard,
      entity: p.pack.entity,
      reload: () => p.frame.onReload(),
      cachedQueries: {}, /*for now*/
      embedded: true,
    });
  }

  export function toCachedQueries(dashboardWithQueries?: DashboardWithCachedQueries | null): {
    [key: string]: Promise<CachedQueryJS>;
  } | undefined {

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

    export const customPartRenderers: Record<string /*typeName*/, Record<string /*customPartName*/, CustomPartRenderer>> = {};

    export function registerCustomPartRenderer<T extends Entity>(type: Type<T>, customPartName: string, renderer: () => Promise<{ default: React.ComponentType<CustomPartProps<T>> }>, opts?: { withPanel?: boolean }): void {
      const dic = customPartRenderers[type.typeName] ??= {};
      dic[customPartName] = {
        renderer: renderer as () => Promise<{ default: React.ComponentType<CustomPartProps<Entity>> }>,
        withPanel: opts?.withPanel ?? true,
      };
    }
  }
}

interface CustomPartRenderer {
  renderer: () => Promise<{ default: React.ComponentType<CustomPartProps<Entity>> }>;
  withPanel: boolean;
}

export interface CustomPartProps<T extends Entity> {

  partEmbedded: PanelPartEmbedded;
  content: CustomPartEntity;
  entity: Lite<T>;
  dashboardController: DashboardController;
}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    dashboards?: Array<Lite<DashboardEntity>>;
    embeddedDashboards?: DashboardEntity[];
  }
}

export function CreateNewButton(p: { queryKey: string, onClick: (types: TypeInfo[], qd: QueryDescription) => void }): React.JSX.Element | null {

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
    <a onClick={e => { e.preventDefault(); p.onClick(tis, qd); }} href="#" className="btn btn-sm btn-tertiary sf-create me-2" title={title}>
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
  customDataRef: React.RefObject<any>;
  cachedQueries: {
    [userAssetKey: string]: Promise<CachedQueryJS>
  }
}

export function DashboardTitle(p: { dashboard: DashboardEntity }): React.JSX.Element | undefined {

  const icon = parseIcon(p.dashboard.iconName);
  const title = p.dashboard.hideDisplayName ? undefined :
    <span style={{ color: p.dashboard.titleColor ?? undefined }} >
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


