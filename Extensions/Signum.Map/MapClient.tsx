import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxGet } from '@framework/Services';
import { OperationMapInfo } from './Operation/OperationMap'
import { } from './Signum.Map'
import { ImportComponent } from '@framework/ImportComponent'
import * as Navigator from "@framework/Navigator";
import * as AppContext from "@framework/AppContext";
import * as OmniboxClient from '../Signum.Omnibox/OmniboxClient';
import MapOmniboxProvider from './MapOmniboxProvider';
import { SchemaMapInfo, getColorProviders } from './Schema/ClientColorProvider';
import { tryGetTypeInfo } from '@framework/Reflection';
import { RoleEntity, UserEntity } from '../Signum.Authorization/Signum.Authorization';



export function start(options: { routes: RouteObject[] }) {

  options.routes.push(
    { path: "/map", element: <ImportComponent onImport={() => import("./Schema/SchemaMapPage")} /> },
    { path: "/map/:type", element: <ImportComponent onImport={() => import("./Operation/OperationMapPage")} /> }
  );

  OmniboxClient.registerProvider(new MapOmniboxProvider());

  AppContext.clearSettingsActions.push(clearProviders);

  getColorProviders.push(smi => import("./Schema/DefaultColorProvider").then((c: any) => c.default(smi)));

  if (tryGetTypeInfo(RoleEntity))
    getColorProviders.push(smi => import("./Schema/AuthColorProvider").then((c: any) => c.default(smi)));

}

export function clearProviders() {
  getColorProviders.clear();
}

export namespace API {
  export function types(): Promise<SchemaMapInfo> {
    return ajaxGet({ url: "/api/map/types" });
  }

  export function operations(typeName: string): Promise<OperationMapInfo> {
    return ajaxGet({ url: "/api/map/operations/" + typeName });
  }
}
