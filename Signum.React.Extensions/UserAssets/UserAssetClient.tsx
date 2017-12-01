import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { Type } from '../../../Framework/Signum.React/Scripts/Reflection'
import { Entity, Lite, liteKey } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import { FindOptions, FilterOption, FilterOperation, OrderOption, ColumnOption, FilterRequest, QueryRequest, Pagination } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as AuthClient  from '../Authorization/AuthClient'
import { IUserAssetEntity, UserAssetMessage, UserAssetPreviewModel, UserAssetPermission, QueryTokenEmbedded }  from './Signum.Entities.UserAssets'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";
import { QueryToken } from '../../../Framework/Signum.React/Scripts/FindOptions';


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
        
        return new QuickLinks.QuickLinkAction(UserAssetMessage.ExportToXml.name, UserAssetMessage.ExportToXml.niceToString(), e => {
            API.exportAsset(ctx.lite);
        });
    });
}

export function toQueryTokenEmbedded(token: QueryToken) {
    return QueryTokenEmbedded.New({
        token: token,
        tokenString: token.fullKey,
    });
}

export module API {

    export function parseFilters(request: ParseFiltersRequest): Promise<FilterRequest[]> {
        return ajaxPost<FilterRequest[]>({ url: "~/api/userAssets/parseFilters/" }, request);
    }

    export interface ParseFiltersRequest {
        queryKey: string;
        filters: ParseFilterRequest[];
        entity: Lite<Entity> | undefined;
        canAggregate: boolean
    }

    export function exportAsset(entity: Lite<IUserAssetEntity>) {
        ajaxPostRaw({ url: "~/api/userAssets/export" }, entity)
            .then(resp => saveFile(resp))
            .done();
    }

    export interface ParseFilterRequest {
        tokenString: string;
        operation: FilterOperation;
        valueString: string;
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
