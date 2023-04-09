
import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet, WebApiHttpError } from '@framework/Services'
import { Entity, Lite } from '@framework/Signum.Entities'
import { DynamicPanelPermission } from './Signum.Dynamic'
import * as AuthClient from '../Signum.Authorization/AuthClient'
import * as OmniboxClient from '../Signum.Omnibox/OmniboxClient'
import { ImportComponent } from '@framework/ImportComponent'
import { QueryEntitiesRequest } from '@framework/FindOptions'



export function start(options: { routes: RouteObject[], withCodeGen: boolean }) {

  if (options.withCodeGen)
    options.routes.push({ path: "/dynamic/panel", element: <ImportComponent onImport={() => import("./DynamicPanelCodeGenPage")} /> });
  else
    options.routes.push({ path: "/dynamic/panel", element: <ImportComponent onImport={() => import("./DynamicPanelSimplePage")} /> });

  Options.withCodeGen = options.withCodeGen;

  OmniboxClient.registerSpecialAction({
    allowed: () => AuthClient.isPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel),
    key: "DynamicPanel",
    onClick: () => Promise.resolve("/dynamic/panel")
  });
}

export namespace Options {
  export let withCodeGen: boolean;
}

export interface CompilationError {
  fileName: string;
  line: number;
  column: number;
  errorNumber: string;
  errorText: string;
  fileContent: string;
}

export namespace API {
  export function compile(): Promise<CompilationError[]> {
    return ajaxPost({ url: `/api/dynamic/compile?inMemory=false` }, null);
  }

  export function getCompilationErrors(): Promise<CompilationError[]> {
    return ajaxPost({ url: `/api/dynamic/compile?inMemory=true` }, null);
  }

  export function restartServer(): Promise<void> {
    return ajaxPost({ url: `/api/dynamic/restartServer` }, null);
  }

  export function getStartErrors(): Promise<WebApiHttpError[]> {
    return ajaxGet({ url: `/api/dynamic/startErrors` });
  }

  export function getEvalErrors(request: QueryEntitiesRequest): Promise<EvalEntityError[]> {
    return ajaxPost({ url: `/api/dynamic/evalErrors` }, request);
  }

  export function getPanelInformation(): Promise<DynamicPanelInformation> {
    return ajaxPost({ url: `/api/dynamic/getPanelInformation` }, null);
  }
}

export interface EvalEntityError {
  lite: Lite<Entity>;
  error: string;
}

export interface DynamicPanelInformation {
  lastDynamicCompilationDateTime?: string;
  loadedCodeGenAssemblyDateTime?: string;
  loadedCodeGenControllerAssemblyDateTime?: string;
  lastDynamicChangeDateTime?: string;
}

