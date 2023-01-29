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
import { FindOptionsParsed, FindOptions, OrderOption, ColumnOption, QueryRequest, Pagination, ResultRow, ResultTable } from '@framework/FindOptions'
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
import { SearchControlLoaded } from '@framework/Search';
import SelectorModal from '@framework/SelectorModal';
import { DynamicTypeConditionSymbolEntity } from '../Dynamic/Signum.Entities.Dynamic';
import { Dic } from '@framework/Globals';

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

  QuickLinks.registerGlobalQuickLink(ctx => {
    if (!AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery))
      return undefined;

    var promise = ctx.widgetContext ?
      Promise.resolve(ctx.widgetContext.frame.pack.userQueries || []) :
      API.forEntityType(ctx.lite.EntityType);

    return promise.then(uqs =>
      uqs.map(uq => new QuickLinks.QuickLinkAction(liteKey(uq), () => getToString(uq) ?? "", e => {
        window.open(AppContext.toAbsoluteUrl(`~/userQuery/${uq.id}/${liteKey(ctx.lite)}`));
      }, { icon: ["far", "rectangle-list"], iconColor: "dodgerblue" })));
  });

  QuickLinks.registerQuickLink(UserQueryEntity, ctx => new QuickLinks.QuickLinkAction("preview", () => UserQueryMessage.Preview.niceToString(),
    e => {
      Navigator.API.fetchAndRemember(ctx.lite).then(uq => {
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
    }, { isVisible: AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery), group: null, icon: "eye", iconColor: "blue", color: "info" }));

  onContextualItems.push(getGroupUserQueriesContextMenu);

  Constructor.registerConstructor<QueryFilterEmbedded>(QueryFilterEmbedded, () => QueryFilterEmbedded.New({ token: QueryTokenEmbedded.New() }));
  Constructor.registerConstructor<QueryOrderEmbedded>(QueryOrderEmbedded, () => QueryOrderEmbedded.New({ token: QueryTokenEmbedded.New() }));
  Constructor.registerConstructor<QueryColumnEmbedded>(QueryColumnEmbedded, () => QueryColumnEmbedded.New({ token: QueryTokenEmbedded.New() }));

  Navigator.addSettings(new EntitySettings(UserQueryEntity, e => import('./Templates/UserQuery'), { isCreable: "Never" }));
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

        return Finder.explore(fo, { searchControlProps: { extraOptions: { userQuery: uq, customDrilldowns: uqe.customDrilldowns } } })
          .then(() => cic.markRows({}));
      }));
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
  export function forEntityType(type: string): Promise<Lite<UserQueryEntity>[]> {
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
}
