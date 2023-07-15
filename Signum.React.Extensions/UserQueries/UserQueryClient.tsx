import * as React from 'react'
import { Dropdown } from 'react-bootstrap'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { EntitySettings } from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Entity, getToString, Lite, liteKey, MList, parseLite, toLite, toMList } from '@framework/Signum.Entities'
import * as Constructor from '@framework/Constructor'
import * as QuickLinks from '@framework/QuickLinks'
import { translated  } from '../Translation/TranslatedInstanceTools'
import { FindOptionsParsed, FindOptions, OrderOption, ColumnOption, QueryRequest, Pagination, ResultRow, ResultTable, FilterOption, withoutPinned, withoutAggregate, hasAggregate, FilterOptionParsed } from '@framework/FindOptions'
import * as AuthClient from '../Authorization/AuthClient'
import {
  UserQueryEntity, UserQueryPermission, UserQueryMessage,
  QueryFilterEmbedded, QueryColumnEmbedded, QueryOrderEmbedded
} from './Signum.Entities.UserQueries'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets'
import UserQueryMenu from './UserQueryMenu'
import * as UserAssetsClient from '../UserAssets/UserAssetClient'
import { ImportRoute } from "@framework/AsyncImport";
import ContextMenu from '@framework/SearchControl/ContextMenu';
import { ContextualItemsContext, MenuItemBlock, onContextualItems } from '@framework/SearchControl/ContextualItems';
import SearchControlLoaded, { OnDrilldownOptions } from '@framework/SearchControl/SearchControlLoaded';
import SelectorModal from '@framework/SelectorModal';
import { DynamicTypeConditionSymbolEntity } from '../Dynamic/Signum.Entities.Dynamic';
import { Dic } from '@framework/Globals';
import { ChartRequestModel, UserChartEntity } from '../Chart/Signum.Entities.Chart';
import { ChartRow, hasAggregates } from '../Chart/ChartClient';
import UserQuery from './Templates/UserQuery';
import { QuickLinkGenerator } from '@framework/QuickLinks';
import { UserAssetModel } from '../UserAssets/UserAssetClient';

export function start(options: { routes: JSX.Element[] }) {
  UserAssetsClient.start({ routes: options.routes });
  UserAssetsClient.registerExportAssertLink(UserQueryEntity);

  options.routes.push(<ImportRoute path="~/userQuery/:userQueryId/:entity?" onImportModule={() => import("./Templates/UserQueryPage")} />);

  Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
    if (!ctx.searchControl.props.showBarExtension ||
      !AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery) ||
      !(ctx.searchControl.props.showBarExtensionOption?.showUserQuery ?? ctx.searchControl.props.largeToolbarButtons))
      return undefined;

    return { button: <UserQueryMenu searchControl={ctx.searchControl} /> };
  });

  QuickLinks.registerGlobalQuickLink(entityType => {
    if (!AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery))
      return undefined;

    return API.forEntityType(entityType)
      .then(uqs => uqs.map(uq =>
      ({
        key: liteKey(uq.asset),
        generator:
          {
          factory: ctx => new QuickLinks.QuickLinkAction(e => {
            window.open(AppContext.toAbsoluteUrl(`~/userQuery/${uq.asset.id}/${liteKey(ctx.lite)}`));
            }),
            options: {
              text: () => getToString(uq.asset),
              icon: ["far", "rectangle-list"], iconColor: "dodgerblue", color: "info",
              hideInAutos: uq.hideQuickLink
            }
          }
      })));
  });

  QuickLinks.registerQuickLink({
    type: UserQueryEntity,
    key: "preview",
    generator: {
      factory: ctx => new QuickLinks.QuickLinkAction( e => {
          Navigator.API.fetchAndRemember(ctx.lite!).then(uq => {
            if (uq.entityType == undefined)
              window.open(AppContext.toAbsoluteUrl(`~/userQuery/${uq.id}`));
            else
              Navigator.API.fetch(uq.entityType)
                .then(t => Finder.find({ queryName: t.cleanName }))
                .then(lite => {
                  if (!lite)
                    return;

                  window.open(AppContext.toAbsoluteUrl(`~/userQuery/${uq.id}/${liteKey(lite)}`));
                });
          });
        }),
      options: {
        text: () => UserQueryMessage.Preview.niceToString(),
        isVisible: AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery), group: null, icon: "eye", iconColor: "blue", color: "info"
      }
    }
  });

  onContextualItems.push(getGroupUserQueriesContextMenu);

  Constructor.registerConstructor<QueryFilterEmbedded>(QueryFilterEmbedded, () => QueryFilterEmbedded.New({ token: QueryTokenEmbedded.New() }));
  Constructor.registerConstructor<QueryOrderEmbedded>(QueryOrderEmbedded, () => QueryOrderEmbedded.New({ token: QueryTokenEmbedded.New() }));
  Constructor.registerConstructor<QueryColumnEmbedded>(QueryColumnEmbedded, () => QueryColumnEmbedded.New({ token: QueryTokenEmbedded.New() }));

  Navigator.addSettings(new EntitySettings(UserQueryEntity, e => import('./Templates/UserQuery'), { isCreable: "Never" }));

  SearchControlLoaded.onDrilldown = async (scl: SearchControlLoaded, row: ResultRow, options?: OnDrilldownOptions) => {
    return onDrilldownSearchControl(scl, row, options);
  }
}

export function userQueryUrl(uq: Lite<UserQueryEntity>): any {
  return AppContext.toAbsoluteUrl(`~/userQuery/${uq.id}`)
}

function getGroupUserQueriesContextMenu(cic: ContextualItemsContext<Entity>) {
  if (!(cic.container instanceof SearchControlLoaded))
    return undefined;

  if (!AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery))
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
          <Dropdown.Item data-user-query={uq.id} onClick={() => handleGroupMenuClick(uq, resFO, resTable, cic)}>
            <FontAwesomeIcon icon={["far", "rectangle-list"]} className="icon" color="dodgerblue" />
            {getToString(uq)}
          </Dropdown.Item>
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

export async function onDrilldownUserChart(cr: ChartRequestModel, row: ChartRow, uc?: Lite<UserChartEntity>, options?: OnDrilldownOptions): Promise<boolean | undefined> {
  if (uc == null)
    return false;

  await Navigator.API.fetchAndRemember(uc);

  if (uc.entity!.customDrilldowns.length == 0 || hasAggregates(uc.entity!) != hasAggregates(cr))
    return false;

  debugger;
  const fo = extractFindOptions(cr, row);
  const entity = row.entity ?? (hasAggregates(cr) ? undefined : fo.filterOptions?.singleOrNull(f => f?.token == "Entity")?.value);
  const filters = fo.filterOptions?.notNull();

  const val = entity ?
    await onDrilldownEntity(uc.entity!.customDrilldowns, entity) :
    await onDrilldownGroup(uc.entity!.customDrilldowns, filters);

  if (!val)
    return undefined;

  return drilldownToUserQuery(val.fo, val.uq, options);
}

export function onDrilldownEntity(items: MList<Lite<Entity>>, entity: Lite<Entity>) {
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

export function onDrilldownGroup(items: MList<Lite<Entity>>, filters?: FilterOption[]) {
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

export async function drilldownToUserQuery(fo: FindOptions, uq: UserQueryEntity, options?: OnDrilldownOptions) {
  const openInNewTab = options?.openInNewTab;
  const showInPlace = options?.showInPlace;
  const onReload = options?.onReload;

  const qd = await Finder.getQueryDescription(fo.queryName);
  const fop = await Finder.parseFilterOptions(fo.filterOptions ?? [], fo.groupResults ?? false, qd);

  const filters = fop.map(f => {
    let f2 = withoutPinned(f);
    if (f2 == null)
      return null;

    return f2;
  }).notNull();

  fo.filterOptions = Finder.toFilterOptions(filters);

  if (openInNewTab || showInPlace) {
    const url = Finder.findOptionsPath(fo, { userQuery: liteKey(toLite(uq)) });

    if (showInPlace && !openInNewTab)
      AppContext.history.push(url);
    else
      window.open(url);

    return Promise.resolve(true);
  }

  return Finder.explore(fo, { searchControlProps: { extraOptions: { userQuery: toLite(uq) } } })
    .then(() => {
      onReload?.();
      return true;
    });
}

export function extractFindOptions(cr: ChartRequestModel, r: ChartRow) {

  const filters = cr.filterOptions.map(f => {
    let f2 = withoutPinned(f);
    if (f2 == null)
      return null;
    return withoutAggregate(f2);
  }).notNull();

  const columns: ColumnOption[] = [];

  cr.columns.map((a, i) => {

    const qte = a.element.token;

    if (qte?.token && !hasAggregate(qte!.token!) && r.hasOwnProperty("c" + i)) {
      filters.push({
        token: qte!.token!,
        operation: "EqualTo",
        value: (r as any)["c" + i],
        frozen: false
      } as FilterOptionParsed);
    }

    if (qte?.token && qte.token.parent != undefined) //Avoid Count and simple Columns that are already added
    {
      var t = qte.token;
      if (t.queryTokenType == "Aggregate") {
        columns.push({
          token: t.parent!.fullKey,
          summaryToken: t.fullKey
        });
      } else {
        columns.push({
          token: t.fullKey,
        });
      }
    }
  });

  var fo: FindOptions = {
    queryName: cr.queryKey,
    filterOptions: Finder.toFilterOptions(filters),
    includeDefaultFilters: false,
    columnOptions: columns,
    columnOptionsMode: "ReplaceOrAdd",
  };

  return fo;
}

export module Converter {

  export function toFindOptions(uq: UserQueryEntity, entity: Lite<Entity> | undefined): Promise<FindOptions> {

    var query = uq.query!;

    var fo = { queryName: query.key, groupResults: uq.groupResults } as FindOptions;

    const convertedFilters = UserAssetsClient.API.parseFilters({
      queryKey: query.key,
      canAggregate: uq.groupResults || false,
      entity: entity,
      filters: uq.filters!.map(mle => UserAssetsClient.Converter.toQueryFilterItem(mle.element))
    });

    return convertedFilters.then(filters => {

      fo.filterOptions = filters.map(f => UserAssetsClient.Converter.toFilterOption(f));
      fo.includeDefaultFilters = uq.includeDefaultFilters == null ? undefined : uq.includeDefaultFilters;
      fo.columnOptionsMode = uq.columnsMode;

      fo.columnOptions = (uq.columns ?? []).map(f => ({
        token: f.element.token.tokenString,
        displayName: translated(f.element, c => c.displayName),
        summaryToken: f.element.summaryToken?.tokenString,
        hiddenColumn: f.element.hiddenColumn,
      }) as ColumnOption);

      fo.orderOptions = (uq.orders ?? []).map(f => ({
        token: f.element.token!.tokenString,
        orderType: f.element.orderType
      }) as OrderOption);


      const qs = Finder.querySettings[query.key];

      fo.pagination = uq.paginationMode == undefined ? undefined : {
          mode: uq.paginationMode,
          currentPage: uq.paginationMode == "Paginate" ? 1 : undefined,
          elementsPerPage: uq.paginationMode == "All" ? undefined : uq.elementsPerPage,
        } as Pagination;

      return fo;
    });
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
        return fop;
      });
  }
}

export module API {
  export function forEntityType(type: string): Promise<UserAssetModel<UserQueryEntity>[]> {
    return ajaxGet({ url: "~/api/userQueries/forEntityType/" + type });
  }

  export function forQuery(queryKey: string): Promise<Lite<UserQueryEntity>[]> {
    return ajaxGet({ url: "~/api/userQueries/forQuery/" + queryKey });
  }

  export function forQueryAppendFilters(queryKey: string): Promise<Lite<UserQueryEntity>[]> {
    return ajaxGet({ url: "~/api/userQueries/forQueryAppendFilters/" + queryKey });
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
