
import * as React from 'react'
import { ajaxPost, ajaxGet, WebApiHttpError } from '@framework/Services'
import { Entity, Lite } from '@framework/Signum.Entities'
import { DynamicPanelPermission } from './Signum.Entities.Dynamic'
import * as AuthClient from '../Authorization/AuthClient'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { ImportRoute } from '@framework/AsyncImport'
import { QueryEntitiesRequest } from '@framework/FindOptions'

export function start(options: { routes: JSX.Element[] }) {
  options.routes.push(<ImportRoute path="~/dynamic/panel" onImportModule={() => import("./DynamicPanelPage")} />);

  OmniboxClient.registerSpecialAction({
    allowed: () => AuthClient.isPermissionAuthorized(DynamicPanelPermission.ViewDynamicPanel),
    key: "DynamicPanel",
    onClick: () => Promise.resolve("~/dynamic/panel")
  });
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
    return ajaxPost<CompilationError[]>({ url: `~/api/dynamic/compile?inMemory=false` }, null);
  }

  export function getCompilationErrors(): Promise<CompilationError[]> {
    return ajaxPost<CompilationError[]>({ url: `~/api/dynamic/compile?inMemory=true` }, null);
  }

  export function restartServer(): Promise<void> {
    return ajaxPost<void>({ url: `~/api/dynamic/restartServer` }, null);
  }

  export function getStartErrors(): Promise<WebApiHttpError[]> {
    return ajaxGet<WebApiHttpError[]>({ url: `~/api/dynamic/startErrors` });
  }

  export function getEvalErrors(request: QueryEntitiesRequest): Promise<EvalEntityError[]> {
    return ajaxPost<EvalEntityError[]>({ url: `~/api/dynamic/evalErrors` }, request);
  }

  export function getPanelInformation(): Promise<DynamicPanelInformation> {
    return ajaxPost<DynamicPanelInformation>({ url: `~/api/dynamic/getPanelInformation` }, null);
  }
}

export interface EvalEntityError {
  lite: Lite<Entity>;
  error: string;
}

export interface DynamicPanelInformation {
  lastDynamicCompilationDateTime?: string;
  loadedCodeGenAssemblyDateTime?: string;
  lastDynamicChangeDateTime?: string;
}

