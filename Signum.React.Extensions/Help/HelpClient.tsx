import * as React from 'react'
import * as QueryString from 'query-string'
import { ajaxGet, ajaxPost } from '@framework/Services';
import * as Navigator from '@framework/Navigator'
import { OperationSymbol } from '@framework/Signum.Entities'
import { PropertyRoute, PseudoType, QueryKey, getQueryKey, getTypeName, getTypeInfo, getAllTypes, getQueryInfo} from '@framework/Reflection'
import { ImportRoute } from "@framework/AsyncImport";
import "./Help.css"
import { NamespaceHelpEntity, TypeHelpEntity, AppendixHelpEntity } from './Signum.Entities.Help';

export function start(options: { routes: JSX.Element[], markdownToHtml: (txt: string) => string }) {

  Options.markdownToHml = options.markdownToHtml;
  options.routes.push(<ImportRoute exact path="~/help" onImportModule={() => import("./Pages/HelpIndexPage")} />);
  options.routes.push(<ImportRoute exact path="~/help/namespace/:namespace*" onImportModule={() => import("./Pages/NamespaceHelpPage")} />);
  options.routes.push(<ImportRoute exact path="~/help/type/:cleanName" onImportModule={() => import("./Pages/TypeHelpPage")} />);
  options.routes.push(<ImportRoute exact path="~/help/appendix/:uniqueName?" onImportModule={() => import("./Pages/AppendixHelpPage")} />);
}

export namespace Options {
  export let markdownToHml = (txt: string) => txt;
}

var helpLinkRegex = /\[(?<letter>[tpqona]+):(?<link>[.a-z0-9_|]*)\]/gi;
var helpAppRelativeUrl = /\(~/;

export function toHtml(txt: string  | null) {
  if (txt == null)
    return "";

  function markdownLink(url: string | null, title: string) {

    if (url == null)
      return `<span class="text-danger">${title}</span>`;

    return `[${title}](${url})`;
  }

  var txt2 = txt.replace(helpLinkRegex, (match: any, letter: string, link: string) => {
    switch (letter) {
      case 't': {
        var ti = getTypeInfo(link);
        return markdownLink(Urls.typeUrl(link), ti?.niceName ?? link);
      }
      case 'a': return markdownLink(Urls.appendixUrl(link), link);
      case 'n': return markdownLink(Urls.namespaceUrl(link), link);
      case 'o': {
        var ti = getAllTypes().firstOrNull(ti => ti.kind == "Entity" && ti.operations != null && ti.operations[link] != null);
        return markdownLink(ti && Urls.operationUrl(ti.name, link), link);
      }
      case 'q': {
        var ti = getTypeInfo(link);
        return markdownLink(ti && Urls.queryUrl(ti.name, link), link);
      }
      case 'p': {
        var type = link.tryBefore(".");
        var ti = type ? getTypeInfo(type) : null;
        return markdownLink(ti && Urls.propertyUrl(ti.name, link.after(".")), link);
      }
      default: throw new Error("Not expected " + letter);
    }
  });

  var txt3 = txt2.replace(helpAppRelativeUrl, "[" + Navigator.toAbsoluteUrl("~"));

  return Options.markdownToHml(txt3);
}

export module API {

  export function index(): Promise<HelpIndexTS> {
    return ajaxGet({ url: "~/api/help/index" });
  }

  export function namespace(namespace: string): Promise<NamespaceHelp> {
    return ajaxGet({ url: "~/api/help/namespace/" + namespace });
  }

  export function saveNamespace(typeHelp: NamespaceHelpEntity): Promise<void> {
    return ajaxPost({ url: "~/api/help/saveNamespace" }, typeHelp);
  }

  export function type(cleanName: string): Promise<TypeHelpEntity> {
    return ajaxGet({ url: "~/api/help/type/" + cleanName });
  }

  export function saveType(typeHelp: TypeHelpEntity): Promise<void> {
    return ajaxPost({ url: "~/api/help/saveType" }, typeHelp);
  }

  export function appendix(uniqueName: string | undefined): Promise<AppendixHelpEntity> {
    return ajaxGet({ url: "~/api/help/appendix/" + (uniqueName ?? "") });
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
    return Navigator.toAbsoluteUrl("~/help/type/" + getTypeName(typeName));
  }

  export function namespaceUrl(namespace: string) {
    return Navigator.toAbsoluteUrl("~/help/namespace/" + namespace.replaceAll(".", "_"));
  }

  export function appendixUrl(uniqueName: string | null) {
    return Navigator.toAbsoluteUrl("~/help/appendix/" + (uniqueName ?? ""));
  }

  export function operationUrl(typeName: PseudoType, operation: OperationSymbol | string) {
    return typeUrl(typeName) + "#" + idOperation(operation);
  }

  export function idOperation(operation: OperationSymbol | string) {
    return "o-" + ((operation as OperationSymbol).key ?? operation as string).replaceAll('.', '_');
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
