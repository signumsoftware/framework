import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet } from '@framework/Services';
import { CachePermission } from './Signum.Cache'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import * as AuthClient from '../Signum.Authorization/AuthClient'
import { ImportComponent } from '@framework/ImportComponent'
import { getColorProviders } from '../Signum.Map/Schema/ClientColorProvider';
import { isPermissionAuthorized } from '@framework/AppContext';
import { registerChangeLogModule } from '@framework/Basics/ChangeLogClient';


export function start(options: { routes: RouteObject[] }) {

  registerChangeLogModule("Signum.Caching", () => import("./Changelog"));

  options.routes.push({ path: "/cache/statistics", element: <ImportComponent onImport={() => import("./CacheStatisticsPage")} /> });

  OmniboxSpecialAction.registerSpecialAction({
    allowed: () => isPermissionAuthorized(CachePermission.InvalidateCache),
    key: "ViewCache",
    onClick: () => Promise.resolve("/cache/statistics")
  });

  getColorProviders.push(smi => import("./CacheColorProvider").then((c: any) => c.default(smi)));
}


export module API {

  export function enable(): Promise<void> {
    return ajaxPost({ url: "/api/cache/enable" }, undefined);
  }

  export function disable(): Promise<void> {
    return ajaxPost({ url: "/api/cache/disable" }, undefined);
  }

  export function clear(): Promise<void> {
    return ajaxPost({ url: "/api/cache/clear" }, undefined);
  }

  export function view(): Promise<CacheState> {
    return ajaxGet({ url: "/api/cache/view" });
  }
}


export interface CacheState {
  isEnabled: boolean;
  serverBroadcast: string | undefined;
  sqlDependency: boolean;
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


