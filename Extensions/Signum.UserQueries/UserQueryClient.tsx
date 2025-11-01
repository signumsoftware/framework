import * as React from 'react'
import { RouteObject } from 'react-router'
import { Dropdown } from 'react-bootstrap'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Navigator, EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import { Finder } from '@framework/Finder'
import { Entity, getToString, Lite, liteKey, MList, parseLite, toLite, toMList, translated } from '@framework/Signum.Entities'
import { Constructor } from '@framework/Constructor'
import { QuickLinkClient, QuickLinkAction } from '@framework/QuickLinkClient'
import { FindOptionsParsed, FindOptions, OrderOption, ColumnOption, QueryRequest, Pagination, ResultRow, ResultTable, FilterOption, withoutPinned, withoutAggregate, hasAggregate, FilterOptionParsed } from '@framework/FindOptions'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { UserQueryEntity, UserQueryPermission, UserQueryMessage, ValueUserQueryListPartEntity, UserQueryPartEntity, UserQueryLiteModel, BigValuePartEntity } from './Signum.UserQueries'
import UserQueryMenu from './UserQueryMenu'
import { UserAssetClient } from '../Signum.UserAssets/UserAssetClient'
import { DashboardClient, CreateNewButton } from '../Signum.Dashboard/DashboardClient'
import { ImportComponent } from '@framework/ImportComponent'
import ContextMenu from '@framework/SearchControl/ContextMenu';
import { ContextualItemsContext, MenuItemBlock, onContextualItems, ContextualMenuItem } from '@framework/SearchControl/ContextualItems';
import SearchControlLoaded, { OnDrilldownOptions } from '@framework/SearchControl/SearchControlLoaded';
import SelectorModal from '@framework/SelectorModal';
import { Dic } from '@framework/Globals';
import { QueryColumnEmbedded, QueryFilterEmbedded, QueryOrderEmbedded, QueryTokenEmbedded } from '../Signum.UserAssets/Signum.UserAssets.Queries';
import { UserQueryPartHandler } from './Dashboard/View/UserQueryPart';
import { ToolbarClient } from '../Signum.Toolbar/ToolbarClient';
import UserQueryToolbarConfig from './UserQueryToolbarConfig';
import { OmniboxClient } from '../Signum.Omnibox/OmniboxClient';
import UserQueryOmniboxProvider from './UserQueryOmniboxProvider';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';

export namespace UserQueryClient {
    
  export function start(options: { routes: RouteObject[] }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.UserQueries", () => import("./Changelog"));
  
    UserAssetClient.start({ routes: options.routes });
    UserAssetClient.registerExportAssertLink(UserQueryEntity);
  
    ToolbarClient.registerConfig(new UserQueryToolbarConfig());
    OmniboxClient.registerProvider(new UserQueryOmniboxProvider());
  
    options.routes.push({ path: "/userQuery/:userQueryId/:entity?", element: <ImportComponent onImport={() => import("./Templates/UserQueryPage")} /> });
  
    Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
  
      const isHidden = !ctx.searchControl.props.showBarExtension ||
        !AppContext.isPermissionAuthorized(UserQueryPermission.ViewUserQuery) ||
        !(ctx.searchControl.props.showBarExtensionOption?.showUserQuery ?? ctx.searchControl.props.largeToolbarButtons);
  
      return { button: <UserQueryMenu searchControl={ctx.searchControl} isHidden={isHidden} /> };
    });
  
    if (AppContext.isPermissionAuthorized(UserQueryPermission.ViewUserQuery))
      QuickLinkClient.registerGlobalQuickLink(entityType =>
        API.forEntityType(entityType)
          .then(uqs => uqs.map(uq => new QuickLinkAction(liteKey(uq), () => getToString(uq), async ctx => {
            const uqe = await Navigator.API.fetch(uq);
            const url = await getUserQueryUrl(uqe, ctx.lite);
            window.open(url);
          }, {
            icon: "rectangle-list", iconColor: "dodgerblue", color: "info",
            onlyForToken: (uq.model as UserQueryLiteModel).hideQuickLink
          })))
      );
  
    QuickLinkClient.registerQuickLink(UserQueryEntity, new QuickLinkAction("preview", () => UserQueryMessage.Preview.niceToString(), async ctx => {
      const uq = await Navigator.API.fetchAndRemember(ctx.lite!);
      if (uq) {
        if (uq.entityType == undefined) {
          const url = await getUserQueryUrl(uq);
          window.open(AppContext.toAbsoluteUrl(url));
        }
        else {

          var t = await Navigator.API.fetch(uq.entityType);
          var lite = await Finder.find({ queryName: t.cleanName });
          if (lite == null)
            return;

          const url = await getUserQueryUrl(uq, lite);
          window.open(AppContext.toAbsoluteUrl(url));
        }
      }  
    },
      {
        isVisible: AppContext.isPermissionAuthorized(UserQueryPermission.ViewUserQuery), group: null, icon: "eye", iconColor: "blue", color: "info"
      }
    ));
  
    onContextualItems.push(getGroupUserQueriesContextMenu);
  
    Constructor.registerConstructor<QueryFilterEmbedded>(QueryFilterEmbedded, () => QueryFilterEmbedded.New({ token: QueryTokenEmbedded.New() }));
    Constructor.registerConstructor<QueryOrderEmbedded>(QueryOrderEmbedded, () => QueryOrderEmbedded.New({ token: QueryTokenEmbedded.New() }));
    Constructor.registerConstructor<QueryColumnEmbedded>(QueryColumnEmbedded, () => QueryColumnEmbedded.New({ token: QueryTokenEmbedded.New() }));
  
    Navigator.addSettings(new EntitySettings(UserQueryEntity, e => import('./Templates/UserQuery'), { isCreable: "Never" }));
    Navigator.addSettings(new EntitySettings(ValueUserQueryListPartEntity, e => import('./Dashboard/Admin/ValueUserQueryListPart')));
    Navigator.addSettings(new EntitySettings(UserQueryPartEntity, e => import('./Dashboard/Admin/UserQueryPart')));
    Navigator.addSettings(new EntitySettings(BigValuePartEntity, e => import('./Dashboard/Admin/BigValuePart')));
  
    SearchControlLoaded.onDrilldown = async (scl: SearchControlLoaded, row: ResultRow, options?: OnDrilldownOptions) => {
      return onDrilldownSearchControl(scl, row, options);
    }
  
    DashboardClient.registerRenderer(ValueUserQueryListPartEntity, {
      component: () => import('./Dashboard/View/ValueUserQueryListPart').then(a => a.default),
      icon: () => ({ icon: ["fas", "list"], iconColor: "#21618C" }),
      getQueryNames: p => p.userQueries.map(a => a.element.userQuery?.query).notNull(),
    });
  
    DashboardClient.registerRenderer(UserQueryPartEntity, {
      waitForInvalidation: true,
      component: () => import('./Dashboard/View/UserQueryPart').then((a: any) => a.default),
      icon: () => ({ icon: "rectangle-list", iconColor: "#2E86C1" }),
      defaultTitle: c => translated(c.userQuery, uc => uc.displayName),
      withPanel: c => true,
      getQueryNames: c => [c.userQuery?.query].notNull(),
      handleEditClick: !Navigator.isViewable(UserQueryPartEntity) || Navigator.isReadOnly(UserQueryPartEntity) ? undefined :
        (c, e, cdRef, ev) => {
          return Navigator.view(c.userQuery!).then(uq => Boolean(uq));
        },
      handleTitleClick:
        (c, e, cdRef, ev) => {
          ev.persist();
          const handler = cdRef.current as UserQueryPartHandler;
          AppContext.pushOrOpenInTab(Finder.findOptionsPath(handler.findOptions, { userQuery: liteKey(toLite(c.userQuery!)) }), ev);
        },
      customTitleButtons: (c, entity, cdRef) => {
        if (!c.createNew)
          return null;
  
        return <CreateNewButton queryKey={c.userQuery.query.key} onClick={(tis, qd) => {
          const handler = cdRef.current as UserQueryPartHandler;
          return Finder.parseFilterOptions(handler.findOptions.filterOptions ?? [], handler.findOptions.groupResults ?? false, qd!)
            .then(fop => SelectorModal.chooseType(tis!)
              .then(ti => ti && Finder.getPropsFromFilters(ti, fop)
                .then(props => Constructor.constructPack(ti.name, props)))
              .then(pack => pack && Navigator.view(pack))
              .then(() => {
                handler.refresh();
              }));
  
        }} />
      }
    });

    DashboardClient.registerRenderer(BigValuePartEntity, {
      waitForInvalidation: true,
      component: () => import('./Dashboard/View/BigValuePart').then((a: any) => a.default),
      icon: () => ({ icon: "circle", iconColor: "#2E86C1" }),
      defaultTitle: c => (c.userQuery ? translated(c.userQuery, uc => uc.displayName) : c.valueToken?.token?.niceName ?? ""),
      withPanel: c => false,
      getQueryNames: c => [c.userQuery?.query].notNull(),
    });
  }

  export function getUserQueryUrl(uq: UserQueryEntity, entity?: Lite<Entity>) : Promise<string> {
    if (uq.refreshMode == "Manual")
      return Promise.resolve(UserQueryClient.userQueryUrl(toLite(uq), entity));

    return UserQueryClient.Converter.toFindOptions(uq, entity)
      .then(fo => Finder.findOptionsPath(fo, { userQuery: liteKey(toLite(uq)), entity: entity ? liteKey(entity) : undefined }));
  }
  
  export function userQueryUrl(uq: Lite<UserQueryEntity>, entity?: Lite<Entity>): string {

    if (entity)
      return `/userQuery/${uq.id}/${liteKey(entity)}`;

    return `/userQuery/${uq.id}`;
  }
  
  function getGroupUserQueriesContextMenu(cic: ContextualItemsContext<Entity>) {
    if (!(cic.container instanceof SearchControlLoaded))
      return undefined;
  
    if (cic.container.state.resultFindOptions?.systemTime)
      return undefined;
  
    if (!AppContext.isPermissionAuthorized(UserQueryPermission.ViewUserQuery))
      return undefined;
  
    const resFO = cic.container.state.resultFindOptions;
    const resTable = cic.container.state.resultTable;
  
    if (resFO == null || resTable == null)
      return undefined;
  
    if (cic.container.state.selectedRows?.length != 1)
      return undefined;
  
    return API.forQueryAppendFilters(resFO.queryKey)
      .then(uqs => {
        if (uqs.length == 0)
          return undefined;
  
        return ({
          header: UserQueryEntity.nicePluralName(),
          menuItems: uqs.map(uq =>
          ({
            fullText: getToString(uq),
            menu: <Dropdown.Item data-user-query={uq.id} onClick={() => handleGroupMenuClick(uq, resFO, resTable, cic)} >
              <FontAwesomeIcon aria-hidden={true} icon={"rectangle-list"} className="icon" color="dodgerblue" />
              {getToString(uq)}
            </Dropdown.Item>
          } as ContextualMenuItem)
          )
        } as MenuItemBlock);
      });
  }
  
  function handleGroupMenuClick(uq: Lite<UserQueryEntity>, resFo: FindOptionsParsed, resTable: ResultTable, cic: ContextualItemsContext<Entity>): void {
    var sc = cic.container as SearchControlLoaded;
  
    Navigator.API.fetch(uq)
      .then(uqe => Converter.toFindOptions(uqe, undefined)
        .then(fo => {
  
          var filters = SearchControlLoaded.getGroupFilters(sc.state.selectedRows!.single(), resTable, resFo);
  
          fo.filterOptions = [...filters, ...fo.filterOptions ?? []];
  
          return Finder.explore(fo, { searchControlProps: { extraOptions: { userQuery: uq } } })
            .then(() => cic.markRows({}));
        }));
  }
  
  export async function onDrilldownSearchControl(scl: SearchControlLoaded, row: ResultRow, options?: OnDrilldownOptions): Promise<boolean | undefined> {
    var uq = scl.getCurrentUserQuery?.();
    if (uq == null)
      return false;
  
    await Navigator.API.fetchAndRemember(uq);
  
    if (uq.entity!.customDrilldowns.length == 0 || scl.state.resultFindOptions?.groupResults != uq.entity!.groupResults)
      return false;
  
    const filters = scl.state.resultFindOptions && SearchControlLoaded.getGroupFilters(row, scl.state.resultTable!, scl.state.resultFindOptions);
  
    const val = row.entity ?
      await onDrilldownEntity(uq.entity!.customDrilldowns, row.entity) :
      await onDrilldownGroup(uq.entity!.customDrilldowns, filters);
  
    if (!val)
      return undefined;
  
    return drilldownToUserQuery(val.fo, val.uq, options);
  }
  
  export function onDrilldownEntity(items: MList<Lite<Entity>>, entity: Lite<Entity>): Promise<{ fo: FindOptions; uq: UserQueryEntity & Entity; } | undefined> {
    const elements = items.map(a => a.element);
    return SelectorModal.chooseElement(elements, { buttonDisplay: i => getToString(i), buttonName: i => liteKey(i) })
      .then(lite => {
        if (!lite || !UserQueryEntity.isLite(lite))
          return undefined;
  
        return Navigator.API.fetch(lite)
          .then(uq => Converter.toFindOptions(uq, entity)
            .then(fo => ({ fo, uq })));
      });
  }
  
  export function onDrilldownGroup(items: MList<Lite<Entity>>, filters?: FilterOption[]): Promise<{ fo: FindOptions; uq: UserQueryEntity & Entity; } | undefined> {
    const elements = items.map(a => a.element);
    return SelectorModal.chooseElement(elements, { buttonDisplay: i => getToString(i), buttonName: i => liteKey(i) })
      .then(lite => {
        if (!lite || !UserQueryEntity.isLite(lite))
          return undefined;
  
        return Navigator.API.fetch(lite)
          .then(uq => Converter.toFindOptions(uq, undefined)
            .then(fo => {
              if (filters)
                fo.filterOptions = [...filters, ...fo.filterOptions ?? []];
  
              return ({ fo, uq });
            }));
      });
  }
  
  export async function drilldownToUserQuery(fo: FindOptions, uq: UserQueryEntity, options?: OnDrilldownOptions): Promise<boolean> {
    const openInNewTab = options?.openInNewTab;
    const showInPlace = options?.showInPlace;
    const onReload = options?.onReload;
  
    const qd = await Finder.getQueryDescription(fo.queryName);
    const fop = await Finder.parseFilterOptions(fo.filterOptions ?? [], fo.groupResults ?? false, qd);
  
    fo.filterOptions = Finder.toFilterOptions(fop);
  
    if (openInNewTab || showInPlace) {
      const url = Finder.findOptionsPath(fo, { userQuery: liteKey(toLite(uq)) });
  
      if (showInPlace && !openInNewTab)
        AppContext.navigate(url);
      else
        window.open(AppContext.toAbsoluteUrl(url));
  
      return Promise.resolve(true);
    }
  
    return Finder.explore(fo, { searchControlProps: { extraOptions: { userQuery: toLite(uq) } } })
      .then(() => {
        onReload?.();
        return true;
      });
  }
  
  export namespace Converter {
  
    export async function toFindOptions(uq: UserQueryEntity, entity: Lite<Entity> | undefined): Promise<FindOptions> {

      var query = uq.query!;
  
      var fo = { queryName: query.key, groupResults: uq.groupResults } as FindOptions;
  
      const filters = await UserAssetClient.API.parseFilters({
        queryKey: query.key,
        canAggregate: uq.groupResults || false,
        canTimeSeries: fo.systemTime?.mode == 'TimeSeries',
        entity: entity,
        filters: uq.filters!.map(mle => UserAssetClient.Converter.toQueryFilterItem(mle.element))
      });
  
  
  
      fo.filterOptions = filters.map(f => UserAssetClient.Converter.toFilterOption(f));
      fo.includeDefaultFilters = uq.includeDefaultFilters == null ? undefined : uq.includeDefaultFilters;
      fo.columnOptionsMode = uq.columnsMode;
  
      fo.columnOptions = (uq.columns ?? []).map(f => ({
        token: f.element.token.tokenString,
        displayName: translated(f.element, c => c.displayName),
        summaryToken: f.element.summaryToken?.tokenString,
        hiddenColumn: f.element.hiddenColumn,
        combineRows: f.element.combineRows,
      }) as ColumnOption);
  
      fo.orderOptions = (uq.orders ?? []).map(f => ({
        token: f.element.token!.tokenString,
        orderType: f.element.orderType
      }) as OrderOption);
  
      fo.pagination = uq.paginationMode == undefined ? undefined : {
        mode: uq.paginationMode,
        currentPage: uq.paginationMode == "Paginate" ? 1 : undefined,
        elementsPerPage: uq.paginationMode == "All" ? undefined : uq.elementsPerPage,
      } as Pagination;
  
      async function parseDate(dateExpression: string | null): Promise<string | undefined> {
        if (dateExpression == null)
          return undefined;
  
        var date = await UserAssetClient.API.parseDate(dateExpression);
  
        return date;
      }
  
      fo.systemTime = uq.systemTime == null ? undefined : {
        mode: uq.systemTime.mode ?? undefined,
        startDate: await parseDate(uq.systemTime.startDate),
        endDate: await parseDate(uq.systemTime.endDate),
        joinMode: uq.systemTime.joinMode ?? undefined,
        timeSeriesStep: uq.systemTime.timeSeriesStep ?? undefined,
        timeSeriesUnit: uq.systemTime.timeSeriesUnit ?? undefined,
        timeSeriesMaxRowsPerStep: uq.systemTime.timeSeriesMaxRowsPerStep ?? undefined,
        splitQueries: uq.systemTime.splitQueries ?? undefined,
      };
  
      return fo;
  
    }
  
    export function applyUserQuery(fop: FindOptionsParsed, uq: UserQueryEntity, entity: Lite<Entity> | undefined, defaultIncudeDefaultFilters: boolean): Promise<FindOptionsParsed> {
      return toFindOptions(uq, entity)
        .then(fo => Finder.getQueryDescription(fo.queryName).then(qd => Finder.parseFindOptions(fo, qd, uq.includeDefaultFilters == null ? defaultIncudeDefaultFilters : uq.includeDefaultFilters)))
        .then(fop2 => {
          if (!uq.appendFilters)
            fop.filterOptions = fop.filterOptions.filter(a => a.frozen);
          fop.filterOptions.push(...fop2.filterOptions);
          fop.groupResults = fop2.groupResults;
          fop.orderOptions = fop2.orderOptions;
          fop.columnOptions = fop2.columnOptions;
          fop.pagination = fop2.pagination;
          fop.systemTime = fop2.systemTime;
          return fop;
        });
    }
  }
  
  export namespace API {
    export function forEntityType(type: string): Promise<Lite<UserQueryEntity>[]> {
      return ajaxGet({ url: "/api/userQueries/forEntityType/" + type });
    }
  
    export function forQuery(queryKey: string): Promise<Lite<UserQueryEntity>[]> {
      return ajaxGet({ url: "/api/userQueries/forQuery/" + queryKey });
    }
  
  
    export function translated(userQuery: Lite<UserQueryEntity>): Promise<UserQueryLiteModel> {
      return ajaxPost({ url: "/api/userQueries/translated" }, userQuery);
    }
  
    export function forQueryAppendFilters(queryKey: string): Promise<Lite<UserQueryEntity>[]> {
      return ajaxGet({ url: "/api/userQueries/forQueryAppendFilters/" + queryKey });
    }
  }
}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    userQueries?: Array<Lite<UserQueryEntity>>;
  }
}

declare module '@framework/SearchControl/SearchControlLoaded' {

  export interface ShowBarExtensionOption {
    showUserQuery?: boolean;
  }

  export interface SearchControlLoaded {
    getCurrentUserQuery?: () => Lite<UserQueryEntity> | undefined;
  }
}
