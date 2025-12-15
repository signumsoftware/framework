import * as React from 'react'
import { RouteObject } from 'react-router'
import { Type } from '@framework/Reflection'
import { ajaxGet, ajaxPost, ajaxPostRaw, saveFile } from '@framework/Services';
import * as AppContext from '@framework/AppContext'
import { OperationSymbol } from '@framework/Signum.Operations'
import { PropertyRoute, PseudoType, QueryKey, getQueryKey, getTypeName, getTypeInfo, getAllTypes, getQueryInfo, tryGetTypeInfo } from '@framework/Reflection'
import { ImportComponent } from '@framework/ImportComponent'
import "./Help.css"
import { QuickLinkClient, QuickLinkAction } from '@framework/QuickLinkClient'
import { NamespaceHelpEntity, TypeHelpEntity, AppendixHelpEntity, QueryHelpEntity, HelpPermissions, IHelpEntity, HelpMessage, HelpImportPreviewModel, HelpImportReportModel } from './Signum.Help';
import { QueryString } from '@framework/QueryString';
import { OmniboxClient } from '../Signum.Omnibox/OmniboxClient';
import HelpOmniboxProvider from './HelpOmniboxProvider';
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { CultureInfoEntity } from '@framework/Signum.Basics';
import { WidgetContext, onWidgets } from '@framework/Frames/Widgets';
import { HelpIcon, HelpWidget } from './HelpWidget';
import { Entity, isEntity, Lite } from '@framework/Signum.Entities';
import { tasks } from '@framework/Lines';
import { LineBaseController, LineBaseProps } from '@framework/Lines/LineBase';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';

export namespace HelpClient {

  export function start(options: { routes: RouteObject[] }): void {

    ChangeLogClient.registerChangeLogModule("Signum.Help", () => import("./Changelog"));

    OmniboxClient.registerProvider(new HelpOmniboxProvider());

    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => AppContext.isPermissionAuthorized(HelpPermissions.ExportHelp),
      key: "ImportHelp",
      onClick: () => Promise.resolve("/help/import")
    });

    options.routes.push({ path: "/help", element: <ImportComponent onImport={() => import("./Pages/HelpIndexPage")} /> });
    options.routes.push({ path: "/help/namespace/:namespace", element: <ImportComponent onImport={() => import("./Pages/NamespaceHelpPage")} /> });
    options.routes.push({ path: "/help/type/:cleanName", element: <ImportComponent onImport={() => import("./Pages/TypeHelpPage")} /> });
    options.routes.push({ path: "/help/appendix/:uniqueName?", element: <ImportComponent onImport={() => import("./Pages/AppendixHelpPage")} /> });

    options.routes.push({ path: "/help/import", element: <ImportComponent onImport={() => import("./Pages/ImportHelpPage")} /> });

    onWidgets.push(wc => AppContext.isPermissionAuthorized(HelpPermissions.ViewHelp) && isEntity(wc.ctx.value) ? <HelpWidget wc={wc as WidgetContext<Entity>} /> : undefined);

    tasks.push(taskHelpIcon);

    registerExportLink(TypeHelpEntity);
    registerExportLink(NamespaceHelpEntity); 
    registerExportLink(AppendixHelpEntity); 
    registerExportLink(QueryHelpEntity); 

  }

    export function registerExportLink(type: Type<IHelpEntity>): void {
    if (AppContext.isPermissionAuthorized(HelpPermissions.ExportHelp))
      QuickLinkClient.registerQuickLink(type,
        new QuickLinkAction(HelpMessage.ExportAsZip.name, () => HelpMessage.ExportAsZip.niceToString(), ctx => API.exportHelpEntities(ctx.lites), {
          allowsMultiple: true,
          iconColor: "#FCAE25",
          icon: "file-code"
        }));
    }
  


  export function taskHelpIcon(lineBase: LineBaseController<LineBaseProps, unknown>, state: LineBaseProps) : void {
    if (state.labelIcon === undefined &&
      state.ctx.propertyRoute &&
      state.ctx.frame?.pack.typeHelp
    ) {
      state.labelIcon = <HelpIcon ctx={state.ctx} />;
    }
  }

  var helpLinkRegex = /\[(?<letter>[tpqona]+):(?<link>[.a-z0-9_|/]*)\]/gi;
  var helpAppRelativeUrl = /href="\//gi;

  export function replaceHtmlLinks(txt: string | null): React.JSX.Element | string {
    if (txt == null)
      return "";

    function htmlLink(url: string | null | undefined, title: string) {

      if (url == null)
        return `<span class="text-danger">${title}</span>`;

      return `<a href="${url}">${title}</a>`;
    }

    var txt2 = txt.replace(helpLinkRegex, (match: any, letter: string, link: string) => {
      switch (letter) {
        case 't': {
          const ti = tryGetTypeInfo(link);
          return htmlLink(ti && Urls.typeUrl(link), ti?.niceName ?? link);
        }
        case 'a': return htmlLink(Urls.appendixUrl(link), link);
        case 'n': return htmlLink(Urls.namespaceUrl(link), link);
        case 'o': {
          const ti = getAllTypes().firstOrNull(ti => ti.kind == "Entity" && ti.operations != null && ti.operations[link] != null);
          return htmlLink(ti && Urls.operationUrl(ti.name, link), link);
        }
        case 'q': {
          const ti = tryGetTypeInfo(link);
          return htmlLink(ti && Urls.queryUrl(ti.name, link), link);
        }
        case 'p': {
          const type = link.tryBefore(".");
          const ti = type ? tryGetTypeInfo(type) : null;
          return htmlLink(ti && Urls.propertyUrl(ti.name, link.after(".")), link);
        }
        default: throw new Error("Not expected " + letter);
      }
    });

    var txt3 = txt2.replace(helpAppRelativeUrl, "href=\"" + AppContext.toAbsoluteUrl("~/"));

    return txt3;
  }


  const cache: { [cleanName: string]: Promise<TypeHelpEntity> } = {};

  export namespace API {

    export function index(): Promise<HelpIndexTS> {
      return ajaxGet({ url: "/api/help/index" });
    }

    export function namespace(namespace: string): Promise<NamespaceHelp> {
      return ajaxGet({ url: "/api/help/namespace/" + namespace });
    }

    export function saveNamespace(typeHelp: NamespaceHelpEntity): Promise<void> {
      return ajaxPost({ url: "/api/help/saveNamespace" }, typeHelp);
    }

    export function type(cleanName: string): Promise<TypeHelpEntity> {
      return cache[cleanName] ??= ajaxGet({ url: "/api/help/type/" + cleanName });
    }

    export function saveType(typeHelp: TypeHelpEntity): Promise<void> {
      delete cache[typeHelp.type.cleanName];
      return ajaxPost({ url: "/api/help/saveType" }, typeHelp);
    }

    export function appendix(uniqueName: string | undefined): Promise<AppendixHelpEntity> {
      return ajaxGet({ url: "/api/help/appendix/" + (uniqueName ?? "") });
    }

    export function saveAppendix(appendix: AppendixHelpEntity): Promise<void> {
      return ajaxPost({ url: "/api/help/saveAppendix" }, appendix);
    }

    export function exportHelpEntities(entity: Lite<IHelpEntity>[]): void {
      ajaxPostRaw({ url: "/api/help/export" }, entity)
        .then(resp => saveFile(resp));
    }

    export interface FileUpload {
      fileName: string;
      content: string;
    }

    export function importPreview(file: FileUpload): Promise<HelpImportPreviewModel> {
      return ajaxPost({ url: "/api/help/importPreview" }, file);
    }

    export function applyImport(file: FileUpload, model: HelpImportPreviewModel): Promise<HelpImportReportModel> {
      return ajaxPost({ url: "/api/help/applyImport" }, { file, model });
    }



  }

  export interface HelpIndexTS {
    culture: CultureInfoEntity;
    namespaces: Array<NamespaceItemTS>;
    appendices: Array<AppendiceItemTS>;
  }

  export interface NamespaceItemTS {
    namespace: string;
    module?: string;
    title: string;
    hasEntity?: boolean;
    allowedTypes: EntityItem[];
  }

  export interface EntityItem {
    cleanName: string;
    hasEntity?: boolean;
  }

  export interface AppendiceItemTS {
    title: string;
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


  export namespace Urls {
    export function indexUrl() {
      return "/help";
    }

    export function searchUrl(query: PseudoType): string {
      return "/help/search?" + QueryString.stringify({ q: getQueryKey(query) });
    }

    export function typeUrl(typeName: PseudoType): string {
      return "/help/type/" + getTypeName(typeName);
    }

    export function namespaceUrl(namespace: string): string {
      return "/help/namespace/" + namespace.replaceAll(".", "_");
    }

    export function appendixUrl(uniqueName: string | null): string {
      return "/help/appendix/" + (uniqueName ?? "");
    }

    export function operationUrl(typeName: PseudoType, operation: OperationSymbol | string): string {
      return typeUrl(typeName) + "#" + idOperation(operation);
    }

    export function idOperation(operation: OperationSymbol | string): string {
      return "o-" + ((operation as OperationSymbol).key ?? operation as string).replaceAll('.', '_');
    }


    export function propertyUrl(typeName: PseudoType, route: PropertyRoute | string): string {
      return typeUrl(typeName) + "#" + idProperty(route);
    }

    export function idProperty(route: PropertyRoute | string): string {
      return "p-" + ((route instanceof PropertyRoute) ? route.propertyPath() : route).replaceAll('.', '_').replaceAll('/', '_').replaceAll('[', '_').replaceAll(']', '_');
    }


    export function queryUrl(typeName: PseudoType, query: PseudoType | QueryKey): string {
      return typeUrl(typeName) + "#" + idQuery(query);
    }

    export function idQuery(query: PseudoType | QueryKey): string {
      return "q-" + getQueryKey(query).replaceAll(".", "_");
    }

  }
}

declare module '@framework/Signum.Entities' {

  export interface EntityPack<T extends ModifiableEntity> {
    typeHelp?: TypeHelpEntity;
  }
}
