import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxGet } from '@framework/Services';
import { SchemaMapInfo, ClientColorProvider } from './Schema/SchemaMap'
import { OperationMapInfo } from './Operation/OperationMap'
import { } from './Signum.Map'
import { ImportComponent } from '@framework/ImportComponent'
import * as Navigator from "@framework/Navigator";
import * as AppContext from "@framework/AppContext";

export const getProviders: Array<(info: SchemaMapInfo) => Promise<ClientColorProvider[]>> = [];

export function getAllProviders(info: SchemaMapInfo): Promise<ClientColorProvider[]> {
  return Promise.all(getProviders.map(func => func(info))).then(result => result.filter(ps => !!ps).flatMap(ps => ps).filter(p => !!p));
}

export function start(options: { routes: RouteObject[], auth: boolean; cache: boolean; disconnected: boolean; isolation: boolean }) {

  options.routes.push(
    { path: "/map", element: <ImportComponent onImport={() => import("./Schema/SchemaMapPage")} /> },
    { path: "/map/:type", element: <ImportComponent onImport={() => import("./Operation/OperationMapPage")} /> }
  );

  AppContext.clearSettingsActions.push(clearProviders);

  getProviders.push(smi => import("./Schema/ColorProviders/Default").then((c: any) => c.default(smi)));
  if (options.auth)
    getProviders.push(smi => import("./Schema/ColorProviders/Auth").then((c: any) => c.default(smi)));
  if (options.cache)
    getProviders.push(smi => import("./Schema/ColorProviders/Cache").then((c: any) => c.default(smi)));
  if (options.disconnected)
    getProviders.push(smi => import("./Schema/ColorProviders/Disconnected").then((c: any) => c.default(smi)));
  if (options.isolation)
    getProviders.push(smi => import("./Schema/ColorProviders/Isolation").then((c: any) => c.default(smi)));
}

export function clearProviders() {
  getProviders.clear();
}

export namespace API {
  export function types(): Promise<SchemaMapInfo> {
    return ajaxGet({ url: "/api/map/types" });
  }

  export function operations(typeName: string): Promise<OperationMapInfo> {
    return ajaxGet({ url: "/api/map/operations/" + typeName });
  }
}
