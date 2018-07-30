
import * as React from 'react'
import { Route } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, QueryKey } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { CachePermission } from './Signum.Entities.Cache'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import { ImportRoute } from "@framework/AsyncImport";


export function start(options: { routes: JSX.Element[] }) {
    options.routes.push(<ImportRoute path="~/cache/statistics" onImportModule={() => import("./CacheStatisticsPage")} />);

    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(CachePermission.InvalidateCache),
        key: "ViewCache",
        onClick: () => Promise.resolve("~/cache/statistics")
    });
}


export module API {

    export function enable(): Promise<void> {
        return ajaxPost<void>({ url: "~/api/cache/enable" }, undefined);
    }

    export function disable(): Promise<void> {
        return ajaxPost<void>({ url: "~/api/cache/disable" }, undefined);
    }

    export function clear(): Promise<void> {
        return ajaxPost<void>({ url: "~/api/cache/clear" }, undefined);
    }

    export function view(): Promise<CacheState> {
        return ajaxGet<CacheState>({ url: "~/api/cache/view" });
    }
}


export interface CacheState {
    isEnabled: boolean;
    tables: CacheTableStats[];
    lazies: ResetLazyStats[];
}

export interface CacheTableStats {
    tableName: string;
    typeName: string;
    count: number;
    hits: number;
    invalidations: number;
    loads: number;
    sumLoadTime: string;
    subTables: CacheTableStats[];
}

export interface ResetLazyStats {
    typeName: string;
    hits: number;
    invalidations: number;
    loads: number;
    sumLoadTime: string;
}


