import * as React from 'react'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Entity, Lite, liteKey } from '@framework/Signum.Entities'
import * as Constructor from '@framework/Constructor'
import * as QuickLinks from '@framework/QuickLinks'
import { FindOptionsParsed, FindOptions, OrderOption, ColumnOption, QueryRequest, Pagination } from '@framework/FindOptions'
import * as AuthClient from '../Authorization/AuthClient'
import {
  UserQueryEntity, UserQueryPermission, UserQueryMessage,
  QueryFilterEmbedded, QueryColumnEmbedded, QueryOrderEmbedded
} from './Signum.Entities.UserQueries'
import { QueryTokenEmbedded } from '../UserAssets/Signum.Entities.UserAssets'
import UserQueryMenu from './UserQueryMenu'
import * as UserAssetsClient from '../UserAssets/UserAssetClient'
import { ImportRoute } from "@framework/AsyncImport";

export function start(options: { routes: JSX.Element[] }) {
  UserAssetsClient.start({ routes: options.routes });
  UserAssetsClient.registerExportAssertLink(UserQueryEntity);

  options.routes.push(<ImportRoute path="~/userQuery/:userQueryId/:entity?" onImportModule={() => import("./Templates/UserQueryPage")} />);

  Finder.ButtonBarQuery.onButtonBarElements.push(ctx => {
    if (!ctx.searchControl.props.showBarExtension ||
      !AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery) ||
      (ctx.searchControl.props.showBarExtensionOption && ctx.searchControl.props.showBarExtensionOption.showUserQuery == false))
      return undefined;

    return <UserQueryMenu searchControl={ctx.searchControl} />;
  });

  QuickLinks.registerGlobalQuickLink(ctx => {
    if (!AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery))
      return undefined;

    var promise = ctx.widgetContext ?
      Promise.resolve(ctx.widgetContext.pack.userQueries || []) :
      API.forEntityType(ctx.lite.EntityType);

    return promise.then(uqs =>
      uqs.map(uq => new QuickLinks.QuickLinkAction(liteKey(uq), uq.toStr || "", e => {
        window.open(Navigator.toAbsoluteUrl(`~/userQuery/${uq.id}/${liteKey(ctx.lite)}`));
      }, { icon: ["far", "list-alt"], iconColor: "dodgerblue" })));
  });

  QuickLinks.registerQuickLink(UserQueryEntity, ctx => new QuickLinks.QuickLinkAction("preview", UserQueryMessage.Preview.niceToString(),
    e => {
      Navigator.API.fetchAndRemember(ctx.lite).then(uq => {
        if (uq.entityType == undefined)
          window.open(Navigator.toAbsoluteUrl(`~/userQuery/${uq.id}`));
        else
          Navigator.API.fetchAndForget(uq.entityType)
            .then(t => Finder.find({ queryName: t.cleanName }))
            .then(lite => {
              if (!lite)
                return;

              window.open(Navigator.toAbsoluteUrl(`~/userQuery/${uq.id}/${liteKey(lite)}`));
            })
            .done();
      }).done();
    }, { isVisible: AuthClient.isPermissionAuthorized(UserQueryPermission.ViewUserQuery) }));

  Constructor.registerConstructor<QueryFilterEmbedded>(QueryFilterEmbedded, () => QueryFilterEmbedded.New({ token: QueryTokenEmbedded.New() }));
  Constructor.registerConstructor<QueryOrderEmbedded>(QueryOrderEmbedded, () => QueryOrderEmbedded.New({ token: QueryTokenEmbedded.New() }));
  Constructor.registerConstructor<QueryColumnEmbedded>(QueryColumnEmbedded, () => QueryColumnEmbedded.New({ token: QueryTokenEmbedded.New() }));

  Navigator.addSettings(new EntitySettings(UserQueryEntity, e => import('./Templates/UserQuery'), { isCreable: "Never" }));
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

      fo.columnOptionsMode = uq.columnsMode;

      fo.columnOptions = (uq.columns || []).map(f => ({
        token: f.element.token!.tokenString,
        displayName: f.element.displayName
      }) as ColumnOption);

      fo.orderOptions = (uq.orders || []).map(f => ({
        token: f.element.token!.tokenString,
        orderType: f.element.orderType
      }) as OrderOption);


      const qs = Finder.querySettings[query.key];

      fo.pagination = uq.paginationMode == undefined ?
        ((qs && qs.pagination) || Finder.defaultPagination) : {
          mode: uq.paginationMode,
          currentPage: uq.paginationMode == "Paginate" ? 1 : undefined,
          elementsPerPage: uq.paginationMode == "All" ? undefined : uq.elementsPerPage,
        } as Pagination;

      return fo;
    });
  }

  export function applyUserQuery(fop: FindOptionsParsed, uq: UserQueryEntity, entity: Lite<Entity> | undefined): Promise<FindOptionsParsed> {
    return toFindOptions(uq, entity)
      .then(fo => Finder.getQueryDescription(fo.queryName).then(qd => Finder.parseFindOptions(fo, qd)))
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
    return ajaxGet<Lite<UserQueryEntity>[]>({ url: "~/api/userQueries/forEntityType/" + type });
  }

  export function forQuery(queryKey: string): Promise<Lite<UserQueryEntity>[]> {
    return ajaxGet<Lite<UserQueryEntity>[]>({ url: "~/api/userQueries/forQuery/" + queryKey });
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
