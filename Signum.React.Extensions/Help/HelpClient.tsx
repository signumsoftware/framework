import * as React from 'react'
import * as QueryString from 'query-string'
import { ajaxGet, ajaxPost } from '@framework/Services';
import * as Navigator from '@framework/Navigator'
import { OperationSymbol } from '@framework/Signum.Entities'
import { PropertyRoute, PseudoType, QueryKey, getQueryKey, getTypeName } from '@framework/Reflection'
import { ImportRoute } from "@framework/AsyncImport";
import "./Help.css"
import { NamespaceHelpEntity, TypeHelpEntity, AppendixHelpEntity } from './Signum.Entities.Help';

export function start(options: { routes: JSX.Element[], toHtml: (txt: string) => string }) {

  Options.toHtml = options.toHtml;
  options.routes.push(<ImportRoute exact path="~/help" onImportModule={() => import("./Pages/HelpIndexPage")} />);
  options.routes.push(<ImportRoute exact path="~/help/namespace/:namespace*" onImportModule={() => import("./Pages/HelpNamespacePage")} />);
  options.routes.push(<ImportRoute exact path="~/help/entity/:cleanName" onImportModule={() => import("./Pages/HelpEntityPage")} />);
  options.routes.push(<ImportRoute exact path="~/help/appendix/:uniqueName?" onImportModule={() => import("./Pages/HelpAppendixPage")} />);
}

export namespace Options {
  export let toHtml = (txt: string) => txt;
}

export module API {

  export function index(): Promise<HelpIndexTS> {
    return ajaxGet<HelpIndexTS>({ url: "~/api/help/index" });
  }

  export function namespace(namespace: string): Promise<NamespaceHelp> {
    return ajaxGet<NamespaceHelp>({ url: "~/api/help/namespace/" + namespace });
  }

  export function saveNamespace(typeHelp: NamespaceHelpEntity): Promise<void> {
    return ajaxPost<void>({ url: "~/api/help/saveNamespace" }, typeHelp);
  }

  export function type(cleanName: string): Promise<TypeHelpEntity> {
    return ajaxGet<TypeHelpEntity>({ url: "~/api/help/entity/" + cleanName });
  }

  export function saveType(typeHelp: TypeHelpEntity): Promise<void> {
    return ajaxPost<void>({ url: "~/api/help/saveEntity" }, typeHelp);
  }

  export function appendix(uniqueName: string): Promise<AppendixHelpEntity> {
    return ajaxGet<AppendixHelpEntity>({ url: "~/api/help/appendix/" + uniqueName });
  }
}

export interface HelpIndexTS {
  namespaces: Array<NamespaceItemTS>;
  appendices: Array<AppendiceItemTS>;
}

export interface NamespaceItemTS {
  namespace: string;
  before?: string;
  title: string;
  allowedTypes: EntityItem[];
}

export interface EntityItem {
  cleanName: string;
  hasDescription: boolean; 
}

export interface AppendiceItemTS {
  title: string ;
  uniqueName: string;
}

export interface NamespaceHelp {
  namespace: string;
  before?: string;
  title: string;
  description?: string;
  entity: NamespaceHelpEntity;
  allowedTypes: EntityItem[];
}


export module Urls {
  export function indexUrl() {
    return Navigator.toAbsoluteUrl("~/help");
  }

  export function searchUrl(query: PseudoType) {
    return Navigator.toAbsoluteUrl("~/help/search?" + QueryString.stringify({ q: query }));
  }

  export function typeUrl(typeName: PseudoType) {
    return Navigator.toAbsoluteUrl("~/help/entity/" + getTypeName(typeName));
  }

  export function namespaceUrl(namespace: string) {
    return Navigator.toAbsoluteUrl("~/help/namespace/" + namespace.replaceAll(".", "_"));
  }

  export function appendixUrl(uniqueName: string | null) {
    return Navigator.toAbsoluteUrl("~/help/appendix/" + (uniqueName || ""));
  }

  export function operationUrl(typeName: PseudoType, operation: OperationSymbol | string) {
    return typeUrl(typeName) + "#" + idOperation(operation);
  }

  export function idOperation(operation: OperationSymbol | string) {
    return "o-" + ((operation as OperationSymbol).key || operation as string).replaceAll('.', '_');
  }


  export function propertyUrl(typeName: PseudoType, route: PropertyRoute | string) {
    return typeUrl(typeName) + "#" + idProperty(route);
  }

  export function idProperty(route: PropertyRoute | string) {
    return "p-" + ((route instanceof PropertyRoute) ? route.toString() : route).replaceAll('.', '_').replaceAll('/', '_').replaceAll('[', '_').replaceAll(']', '_');
  }


  export function queryUrl(typeName: PseudoType, query: PseudoType | QueryKey) {
    return typeUrl(typeName) + "#" + idQuery(query);
  }

  export function idQuery(query: PseudoType | QueryKey) {
    return "q-" + getQueryKey(query).replaceAll(".", "_");
  }

}
