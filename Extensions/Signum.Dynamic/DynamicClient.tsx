
import * as React from 'react'
import { RouteObject } from 'react-router'
import { ajaxPost, ajaxGet, WebApiHttpError } from '@framework/Services'
import { ImportComponent } from '@framework/ImportComponent'



export function start(options: { routes: RouteObject[], withCodeGen: boolean }) {

  options.routes.push({ path: "/dynamic/panel", element: <ImportComponent onImport={() => import("./DynamicPanelCodeGenPage")} /> });


  Options.withCodeGen = options.withCodeGen;

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

  export function getPanelInformation(): Promise<DynamicPanelInformation> {
    return ajaxPost({ url: `/api/dynamic/getPanelInformation` }, null);
  }
}



export interface DynamicPanelInformation {
  lastDynamicCompilationDateTime?: string;
  loadedCodeGenAssemblyDateTime?: string;
  loadedCodeGenControllerAssemblyDateTime?: string;
  lastDynamicChangeDateTime?: string;
}

