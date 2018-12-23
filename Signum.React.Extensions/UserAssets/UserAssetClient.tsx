import * as React from 'react'
import { ajaxPost, ajaxPostRaw, saveFile } from '@framework/Services';
import { Type } from '@framework/Reflection'
import { Entity, Lite } from '@framework/Signum.Entities'
import * as QuickLinks from '@framework/QuickLinks'
import { FilterOption, FilterOperation, FilterOptionParsed, FilterGroupOptionParsed, FilterConditionOptionParsed, FilterGroupOption, FilterConditionOption, PinnedFilter, isFilterGroupOption } from '@framework/FindOptions'
import * as AuthClient from '../Authorization/AuthClient'
import { IUserAssetEntity, UserAssetMessage, UserAssetPreviewModel, UserAssetPermission, QueryTokenEmbedded } from './Signum.Entities.UserAssets'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from "@framework/AsyncImport";
import { QueryToken } from '@framework/FindOptions';
import { FilterGroupOperation } from '@framework/Signum.Entities.DynamicQuery';
import { QueryFilterEmbedded, PinnedQueryFilterEmbedded } from '../UserQueries/Signum.Entities.UserQueries';

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


  started = true;
}

export function registerExportAssertLink(type: Type<IUserAssetEntity>) {
  QuickLinks.registerQuickLink(type, ctx => {
    if (!AuthClient.isPermissionAuthorized(UserAssetPermission.UserAssetsToXML))
      return undefined;

    return new QuickLinks.QuickLinkAction(UserAssetMessage.ExportToXml.name, UserAssetMessage.ExportToXml.niceToString(), () => {
      API.exportAsset(ctx.lite);
    });
  });
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

  export function toFilterOptionParsed(fr: API.FilterNode): FilterOptionParsed {
    if (fr.groupOperation)
      return ({
        token: fr.token,
        groupOperation: fr.groupOperation,
        filters: fr.filters!.map(f => toFilterOptionParsed(f)),
        pinned: fr.pinned
      } as FilterGroupOptionParsed);
    else
      return ({
        token: fr.token,
        operation: fr.operation || "EqualTo",
        value: fr.value,
        frozen: false,
        pinned: fr.pinned,
      } as FilterConditionOptionParsed);
  }

  export function toFilterOption(fr: API.FilterNode): FilterOption {
    if (fr.groupOperation)
      return ({
        token: fr.token && fr.token.fullKey,
        groupOperation: fr.groupOperation,
        filters: fr.filters!.map(f => toFilterOption(f)),
        pinned: fr.pinned
      } as FilterGroupOption);
    else
      return ({
        token: fr.token!.fullKey,
        operation: fr.operation || "EqualTo",
        value: fr.value,
        pinned: fr.pinned
      } as FilterConditionOption);
  }

  export function toFilterNode(fr: FilterOption) : API.FilterNode {

    if (isFilterGroupOption(fr))
      return ({
        groupOperation: fr.groupOperation,
        tokenString: fr.token && fr.token.toString(),
        value: fr.value,
        pinned: fr.pinned,
        filters: fr.filters.map(f => toFilterNode(f)),
      });

    else
      return ({
        tokenString: fr.token!.toString(),
        operation: fr.operation || "EqualTo",
        value: fr.value,
        pinned: fr.pinned,
      });
  }

  export function toQueryFilterEmbedded(e: API.QueryFilterItem): QueryFilterEmbedded {

    function toPinnedFilterEmbedded(e: PinnedFilter): PinnedQueryFilterEmbedded {
      return PinnedQueryFilterEmbedded.New({
        label: e.label,
        column: e.column,
        row: e.row,
        disableOnNull: e.disableOnNull,
        splitText: e.splitText
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
      indentation: e.indentation,
    });
  }

  export function toQueryFilterItem(e: QueryFilterEmbedded): API.QueryFilterItem  {

    function toPinnedFilter(e: PinnedQueryFilterEmbedded): PinnedFilter {
      return ({
        label: e.label == null ? undefined : e.label,
        column: e.column == null ? undefined : e.column,
        row: e.row == null ? undefined : e.row,
        disableOnNull: e.disableOnNull,
        splitText: e.splitText
      })
    }

    return ({
      isGroup: e.isGroup,
      groupOperation: e.groupOperation || undefined,
      tokenString: e.token ? e.token.tokenString! : undefined,
      operation: e.operation || undefined,
      valueString: e.valueString || undefined,
      pinned: e.pinned ? toPinnedFilter(e.pinned) : undefined,
      indentation: e.indentation!,
    });
  }

  
}


export module API {

  export function parseFilters(request: ParseFiltersRequest): Promise<FilterNode[]> {
    return ajaxPost<FilterNode[]>({ url: "~/api/userAssets/parseFilters/" }, request);
  }

  export interface ParseFiltersRequest {
    queryKey: string;
    filters: QueryFilterItem[];
    entity: Lite<Entity> | undefined;
    canAggregate: boolean
  }


  export function stringifyFilters(request: StringifyFiltersRequest): Promise<QueryFilterItem[]> {
    return ajaxPost<QueryFilterItem[]>({ url: "~/api/userAssets/stringifyFilters/" }, request);
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
  }

  export interface QueryFilterItem {
    token?: QueryToken;
    tokenString?: string;
    isGroup?: boolean;
    groupOperation?: FilterGroupOperation;
    operation?: FilterOperation ;
    valueString?: string;
    pinned?: PinnedFilter;
    indentation?: number;
  }


  export function exportAsset(entity: Lite<IUserAssetEntity>) {
    ajaxPostRaw({ url: "~/api/userAssets/export" }, entity)
      .then(resp => saveFile(resp))
      .done();
  }


  export function importPreview(request: FileUpload): Promise<UserAssetPreviewModel> {
    return ajaxPost<UserAssetPreviewModel>({ url: "~/api/userAssets/importPreview/" }, request);
  }

  export interface FileUpload {
    fileName: string;
    content: string;
  }

  export function importAssets(request: FileUploadWithModel): Promise<void> {
    return ajaxPost<void>({ url: "~/api/userAssets/import" }, request);
  }

  export interface FileUploadWithModel {
    file: FileUpload;
    model: UserAssetPreviewModel;
  }
}
