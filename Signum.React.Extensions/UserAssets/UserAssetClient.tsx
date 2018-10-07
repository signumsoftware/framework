import * as React from 'react'
import { ajaxPost, ajaxPostRaw, saveFile } from '@framework/Services';
import { Type } from '@framework/Reflection'
import { Entity, Lite } from '@framework/Signum.Entities'
import * as QuickLinks from '@framework/QuickLinks'
import { FilterOption, FilterOperation, FilterOptionParsed, FilterGroupOptionParsed, FilterConditionOptionParsed, FilterGroupOption, FilterConditionOption } from '@framework/FindOptions'
import * as AuthClient  from '../Authorization/AuthClient'
import { IUserAssetEntity, UserAssetMessage, UserAssetPreviewModel, UserAssetPermission, QueryTokenEmbedded }  from './Signum.Entities.UserAssets'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from "@framework/AsyncImport";
import { QueryToken } from '@framework/FindOptions';
import { FilterGroupOperation } from '@framework/Signum.Entities.DynamicQuery';


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

export function getToken(token: QueryTokenEmbedded)  : QueryToken {
    if (token.parseException)
        throw new Error(token.parseException);

    return token.token!;
}

export module Converter {

    export function toFilterOptionParsed(fr: API.FilterResponse): FilterOptionParsed {
        if (API.isFilterGroupResponse(fr))
            return ({
                token: fr.token,
                groupOperation: fr.groupOperation,
                filters: fr.filters.map(f => toFilterOptionParsed(f)),
            } as FilterGroupOptionParsed);
        else
            return ({
                token: fr.token,
                operation: fr.operation || "EqualTo",
                value: fr.value,
                frozen: true,
            } as FilterConditionOptionParsed);
    }

    export  function toFilterOption(fr: API.FilterResponse): FilterOption {
        if (API.isFilterGroupResponse(fr))
            return ({
                token: fr.token && fr.token.fullKey,
                groupOperation: fr.groupOperation,
                filters: fr.filters.map(f => toFilterOption(f)),
            } as FilterGroupOption);
        else
            return ({
                token: fr.token.fullKey,
                operation: fr.operation || "EqualTo",
                value: fr.value,
            } as FilterConditionOption);
    }

}

export module API {

    export function parseFilters(request: ParseFiltersRequest): Promise<FilterResponse[]> {
        return ajaxPost<FilterResponse[]>({ url: "~/api/userAssets/parseFilters/" }, request);
    }

    export interface ParseFiltersRequest {
        queryKey: string;
        filters: ParseFilterRequest[];
        entity: Lite<Entity> | undefined;
        canAggregate: boolean
    }

    export interface ParseFilterRequest {
        isGroup: boolean;
        tokenString: string;
        operation?: FilterOperation;
        valueString: string;
        groupOperation?: FilterGroupOperation;
        indentation: number;
    }


    export type FilterResponse = FilterConditionResponse | FilterGroupResponse;

    export function isFilterGroupResponse(fr: FilterResponse): fr is FilterGroupResponse {
        return (fr as FilterGroupResponse).groupOperation != null;
    }

    export interface FilterGroupResponse {
        groupOperation: FilterGroupOperation;
        token?: QueryToken;
        filters: FilterResponse[];
    }

    export interface FilterConditionResponse {
        token: QueryToken;
        operation: FilterOperation;
        value: any;
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
