import * as React from 'react'
import { ajaxGet } from '@framework/Services';
import { SchemaMapInfo, ClientColorProvider } from './Schema/SchemaMap'
import { OperationMapInfo } from './Operation/OperationMap'
import { } from './Signum.Entities.Map'
import { ImportRoute } from "@framework/AsyncImport";
import * as Navigator from "@framework/Navigator";

export const getProviders: Array<(info: SchemaMapInfo) => Promise<ClientColorProvider[]>> = [];

export function getAllProviders(info: SchemaMapInfo): Promise<ClientColorProvider[]> {
  return Promise.all(getProviders.map(func => func(info))).then(result => result.filter(ps => !!ps).flatMap(ps => ps).filter(p => !!p));
}

export function start(options: { routes: JSX.Element[], auth: boolean; cache: boolean; disconnected: boolean; isolation: boolean }) {

  options.routes.push(
    <ImportRoute path="~/map" exact onImportModule={() => import("./Schema/SchemaMapPage")} />,
    <ImportRoute path="~/map/:type" onImportModule={() => import("./Operation/OperationMapPage")} />
  );

  Navigator.clearSettingsActions.push(clearProviders);

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
    return ajaxGet({ url: "~/api/map/types" });
  }

  export function operations(typeName: string): Promise<OperationMapInfo> {
    return ajaxGet({ url: "~/api/map/operations/" + typeName });
  }
}
