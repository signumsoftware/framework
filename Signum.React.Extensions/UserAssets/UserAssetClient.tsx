import * as React from 'react'
import { ajaxPost, ajaxPostRaw, saveFile } from '@framework/Services';
import { Type } from '@framework/Reflection'
import { Entity, getToString, Lite, liteKey, MList, parseLite, toLite } from '@framework/Signum.Entities'
import * as QuickLinks from '@framework/QuickLinks'
import { FilterOption, FilterOperation, FilterOptionParsed, FilterGroupOptionParsed, FilterConditionOptionParsed, FilterGroupOption, FilterConditionOption, PinnedFilter, isFilterGroupOption, toPinnedFilterParsed, FindOptions, FindOptionsParsed } from '@framework/FindOptions'
import * as AuthClient from '../Authorization/AuthClient'
import { IUserAssetEntity, UserAssetMessage, UserAssetPreviewModel, UserAssetPermission, QueryTokenEmbedded } from './Signum.Entities.UserAssets'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from "@framework/AsyncImport";
import { QueryToken } from '@framework/FindOptions';
import { DashboardBehaviour, FilterGroupOperation } from '@framework/Signum.Entities.DynamicQuery';
import { QueryFilterEmbedded, PinnedQueryFilterEmbedded, UserQueryEntity } from '../UserQueries/Signum.Entities.UserQueries';
import { Dic, softCast } from '@framework/Globals';
import * as AppContext from '@framework/AppContext';
import { translated } from '../Translation/TranslatedInstanceTools'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import SelectorModal from '@framework/SelectorModal';
import * as UserQueryClient from '../UserQueries/UserQueryClient'
import { SearchControlLoaded } from '../../Signum.React/Scripts/Search';
import { CustomDrilldownOptions } from '@framework/SearchControl/SearchControlLoaded';

let started = false;
export function start(options: { routes: JSX.Element[] }) {
  if (started)
    return;

  options.routes.push(<ImportRoute path="~/userAssets/import" onImportModule={() => import("./ImportAssetsPage")} />);
  OmniboxClient.registerSpecialAction({
    allowed: () => AuthClient.isPermissionAuthorized(UserAssetPermission.UserAssetsToXML),
    key: "ImportUserAssets",
    onClick: () => Promise.resolve("~/userAssets/import")
  });

  AppContext.clearSettingsActions.push(() => started = false);

  SearchControlLoaded.onCustomDrilldown = (items, options?: CustomDrilldownOptions) => handleCustomDrilldowns(items, options);

  started = true;
}

export function registerExportAssertLink(type: Type<IUserAssetEntity>) {
  QuickLinks.registerQuickLink(type, ctx => {
    if (!AuthClient.isPermissionAuthorized(UserAssetPermission.UserAssetsToXML))
      return undefined;

    return new QuickLinks.QuickLinkAction(UserAssetMessage.ExportToXml.name, () => UserAssetMessage.ExportToXml.niceToString(), () => {
      API.exportAsset(ctx.lites);
    }, {
        iconColor: "#FCAE25",
        icon: "file-code"
      });
  }, { allowsMultiple : true });
}

export function toQueryTokenEmbedded(token: QueryToken): QueryTokenEmbedded {
  return QueryTokenEmbedded.New({
    token: token,
    tokenString: token.fullKey,
  });
}

export function getToken(token: QueryTokenEmbedded): QueryToken {
  if (token.parseException)
    throw new Error(token.parseException);

  return token.token!;
}

export module Converter {

  export function toFilterOptionParsed(fn: API.FilterNode): FilterOptionParsed {
    if (fn.groupOperation)
      return softCast<FilterGroupOptionParsed>({
        token: fn.token,
        groupOperation: fn.groupOperation,
        filters: fn.filters!.map(f => toFilterOptionParsed(f)),
        pinned: fn.pinned && toPinnedFilterParsed(fn.pinned),
        dashboardBehaviour: fn.dashboardBehaviour,
        frozen: false,
        expanded: false,
      });
    else
      return softCast<FilterConditionOptionParsed>({
        token: fn.token,
        operation: fn.operation ?? "EqualTo",
        value: fn.value,
        frozen: false,
        pinned: fn.pinned && toPinnedFilterParsed(fn.pinned),
        dashboardBehaviour: fn.dashboardBehaviour,
      });
  }

  export function toFilterOption(fr: API.FilterNode): FilterOption {
    if (fr.groupOperation)
      return ({
        token: fr.token && fr.token.fullKey,
        groupOperation: fr.groupOperation,
        filters: fr.filters!.map(f => toFilterOption(f)),
        pinned: fr.pinned,
        dashboardBehaviour: fr.dashboardBehaviour,
      } as FilterGroupOption);
    else
      return ({
        token: fr.token!.fullKey,
        operation: fr.operation ?? "EqualTo",
        value: fr.value,
        pinned: fr.pinned,
        dashboardBehaviour: fr.dashboardBehaviour,
      } as FilterConditionOption);
  }

  export function toFilterNode(fr: FilterOption) : API.FilterNode {

    if (isFilterGroupOption(fr))
      return ({
        groupOperation: fr.groupOperation,
        tokenString: fr.token && fr.token.toString(),
        value: fr.value,
        pinned: fr.pinned,
        filters: fr.filters.notNull().map(f => toFilterNode(f)),
        dashboardBehaviour: fr.dashboardBehaviour,
      });

    else
      return ({
        tokenString: fr.token!.toString(),
        operation: fr.operation ?? "EqualTo",
        value: fr.value,
        pinned: fr.pinned,
        dashboardBehaviour: fr.dashboardBehaviour,
      });
  }

  export function toQueryFilterEmbedded(e: API.QueryFilterItem): QueryFilterEmbedded {

    function toPinnedFilterEmbedded(e: PinnedFilter): PinnedQueryFilterEmbedded {
      return PinnedQueryFilterEmbedded.New({
        label: typeof e.label == "function" ? e.label() : e.label,
        column: e.column,
        row: e.row,
        active: e.active ?? "Always",
        splitText: e.splitText ?? false
      });
    }

    return QueryFilterEmbedded.New({
      isGroup: e.isGroup,
      groupOperation: e.groupOperation,
      token: e.token && QueryTokenEmbedded.New({
        tokenString: e.token.fullKey,
        token: e.token
      }),
      operation: e.operation,
      valueString: e.valueString,
      pinned: e.pinned && toPinnedFilterEmbedded(e.pinned),
      dashboardBehaviour: e.dashboardBehaviour,
      indentation: e.indentation,
    });
  }

  export function toQueryFilterItem(qf: QueryFilterEmbedded): API.QueryFilterItem  {

    function toPinnedFilter(pqf: PinnedQueryFilterEmbedded): PinnedFilter {
      return ({
        label: pqf.label == null ? undefined : translated(pqf, e => e.label) ?? undefined,
        column: pqf.column == null ? undefined : pqf.column,
        row: pqf.row == null ? undefined : pqf.row,
        active: pqf.active,
        splitText: pqf.splitText
      })
    }

    return ({
      isGroup: qf.isGroup,
      groupOperation: qf.groupOperation ?? undefined,
      tokenString: qf.token ? qf.token.tokenString! : undefined,
      operation: qf.operation ?? undefined,
      valueString: qf.valueString ?? undefined,
      pinned: qf.pinned ? toPinnedFilter(qf.pinned) : undefined,
      dashboardBehaviour: qf.dashboardBehaviour ?? undefined,
      indentation: qf.indentation!,
    });
  }

  
}


export module API {

  export function parseFilters(request: ParseFiltersRequest): Promise<FilterNode[]> {
    return ajaxPost({ url: "~/api/userAssets/parseFilters/" }, request);
  }

  export interface ParseFiltersRequest {
    queryKey: string;
    filters: QueryFilterItem[];
    entity: Lite<Entity> | undefined;
    canAggregate: boolean
  }


  export function stringifyFilters(request: StringifyFiltersRequest): Promise<QueryFilterItem[]> {
    return ajaxPost({ url: "~/api/userAssets/stringifyFilters/" }, request);
  }
  
  export interface StringifyFiltersRequest {
    queryKey: string;
    filters: FilterNode[];
    canAggregate: boolean
  }
  
  export interface FilterNode {
    groupOperation?: FilterGroupOperation;
    tokenString?: string;
    token?: QueryToken;
    operation?: FilterOperation;
    value?: any;
    filters?: FilterNode[];
    pinned?: PinnedFilter;
    dashboardBehaviour?: DashboardBehaviour;
  }

  export interface QueryFilterItem {
    token?: QueryToken;
    tokenString?: string;
    isGroup?: boolean;
    groupOperation?: FilterGroupOperation;
    operation?: FilterOperation ;
    valueString?: string;
    pinned?: PinnedFilter;
    dashboardBehaviour?: DashboardBehaviour;
    indentation?: number;
  }


  export function exportAsset(entity: Lite<IUserAssetEntity>[]) {
    ajaxPostRaw({ url: "~/api/userAssets/export" }, entity)
      .then(resp => saveFile(resp));
  }


  export function importPreview(request: FileUpload): Promise<UserAssetPreviewModel> {
    return ajaxPost({ url: "~/api/userAssets/importPreview/" }, request);
  }

  export interface FileUpload {
    fileName: string;
    content: string;
  }

  export function importAssets(request: FileUploadWithModel): Promise<void> {
    return ajaxPost({ url: "~/api/userAssets/import" }, request);
  }

  export interface FileUploadWithModel {
    file: FileUpload;
    model: UserAssetPreviewModel;
  }
}

const scapeTilde = Finder.Encoder.scapeTilde;

export module Encoder {
  export function encodeCustomDrilldowns(query: any, drilldowns: MList<Lite<Entity>> | undefined) {
    if (drilldowns && drilldowns.length > 0)
      drilldowns.map(mle => mle.element).map((d, i) => query["customDrilldown" + i] = liteKey(d) + "~" + scapeTilde(getToString(d)));
    else {
      const keys = Dic.getKeys(query);
      keys.filter(k => k.startsWith("customDrilldown")).forEach(k => delete query[k]);
    }
  }
}

export module Decoder {
  export function decodeCustomDrilldowns(query: any): MList<Lite<Entity>> {
    return Finder.Decoder.valuesInOrder(query, "customDrilldown").map(d => {
      var parts = d.value.split("~");

      let liteKey: string;
      let toStr: string;

      [liteKey, toStr] = parts;

      var lite = parseLite(liteKey);
      lite.model = toStr;

      return ({
        rowId: null,
        element: lite
      });
    });
  }
}

export function handleCustomDrilldowns(items: Lite<Entity>[], options?: CustomDrilldownOptions) {
  const fo = options?.fo;
  const entity = options?.entity;
  const openInNewTab = options?.openInNewTab;
  const showInPlace = options?.showInPlace;
  const onReload = options?.onReload;

  return SelectorModal.chooseElement(items, { buttonDisplay: i => getToString(i), buttonName: i => liteKey(i) })
    .then(lite => {
      if (!lite || !UserQueryEntity.isLite(lite))
        return;

      return Navigator.API.fetch(lite)
        .then(uq => {
          if (fo)
            return Finder.getQueryDescription(fo.queryName)
              .then(qd => Finder.parseFindOptions(fo, qd, false)
                .then(fop => UserQueryClient.Converter.applyUserQuery(fop, uq, entity, fo.includeDefaultFilters ?? false)
                  .then(fop2 => Finder.toFindOptions(fop2, qd, uq.includeDefaultFilters ?? fo.includeDefaultFilters ?? false))
                  .then(dfo => ({ fo: dfo, uq: uq }))));

          return UserQueryClient.Converter.toFindOptions(uq, entity)
            .then(dfo => ({ fo: dfo, uq: uq }));
        });
    })
    .then(val => {
      if (!val)
        return;

      if (openInNewTab || showInPlace) {
        var extra: any = {};
        extra.userQuery = liteKey(toLite(val.uq));
        Encoder.encodeCustomDrilldowns(extra, val.uq.customDrilldowns);

        const url = Finder.findOptionsPath(val.fo, extra);

        if (showInPlace && !openInNewTab)
          AppContext.history.push(url);
        else
          window.open(url);
      }
      else
        Finder.explore(val.fo, { searchControlProps: { extraOptions: { userQuery: toLite(val.uq), customDrilldowns: val.uq.customDrilldowns } } })
          .then(() => onReload?.());
    });
}
