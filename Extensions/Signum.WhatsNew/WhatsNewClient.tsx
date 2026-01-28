import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { QuickLinkClient, QuickLinkLink } from '@framework/QuickLinkClient'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome"
import { ajaxGet, ajaxPost } from '@framework/Services'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { Entity, Lite } from '@framework/Signum.Entities'
import { Type } from '@framework/Reflection'
import { WhatsNewEntity, WhatsNewMessage } from './Signum.WhatsNew'
import { ImportComponent } from '@framework/ImportComponent'
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient'

export namespace WhatsNewClient {
  
  export function start(options: { routes: RouteObject[] }): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.WhatsNew", () => import("./Changelog"));
  
    options.routes.push({ path: "/newspage/:newsId", element: <ImportComponent onImport={() => import("./Templates/NewsPage")} /> });
    options.routes.push({ path: "/news", element: <ImportComponent onImport={() => import("./Templates/AllNewsPage")} /> });
  
    Navigator.addSettings(new EntitySettings(WhatsNewEntity, t => import('./Templates/WhatsNew'), { modalSize: "xl" }));
  
    //Operations.addSettings(new EntityOperationSettings(WhatsNewOperation.Read, {
    //  alternatives: eoc => [EntityOperations.andClose(eoc)],
    //  hideOnCanExecute: true
    //}));
  
    QuickLinkClient.registerQuickLink(WhatsNewEntity, new QuickLinkLink("Preview",
      () => WhatsNewMessage.Preview.niceToString(),
      ctx => "/newspage/" + ctx.lite.id,
      {
        icon: "newspaper",
        iconColor: "purple",
      }));
  
    const TextPlaceholder = /{(?<prop>(\w|\d|\.)+)}/
    const NumericPlaceholder = /^[ \d]+$/;
  
    function replacePlaceHolders(value: string | null | undefined, whatsnew: Partial<WhatsNewEntity>) {
      if (value == null)
        return null;
  
      return value.replace(TextPlaceholder, args => {
  
        var prop = args[1];
  
        return getPropertyValue(prop, whatsnew)?.ToString()! ?? "";
      });
    }
  
    function getPropertyValue(str: string, object: Partial<WhatsNewEntity>): any {
      if (str.contains(".")) {
        var obj2 = getPropertyValue(str.before("."), object);
  
        return obj2 == null ? obj2 : obj2[str.after(".").firstLower()];
      }
  
      return (object as any)[str.firstLower()]
    }
  }
  
  export module API {
  
    export function myNews(): Promise<WhatsNewShort[]> {
      return ajaxGet({ url: "/api/whatsnew/myNews", avoidNotifyPendingRequests: true });
    }
  
    export function myNewsCount(): Promise<NumWhatsNews> {
      return ajaxGet({ url: "/api/whatsnew/myNewsCount", avoidNotifyPendingRequests: true });
    }
  
    export function getAllNews(): Promise<WhatsNewFull[]> {
      return ajaxGet({ url: "/api/whatsnew/all" });
    }
  
    export function newsPage(id: number | string): Promise<WhatsNewFull> {
      return ajaxGet({ url: "/api/whatsnew/" + id });
    }
  
    export function setNewsLogRead(lites: Lite<WhatsNewEntity>[]): Promise<void> {
      return ajaxPost({ url: "/api/whatsnew/setNewsLog" }, lites);
    }
  }
  
  export interface NumWhatsNews
  {
    numWhatsNews: number
  };
  
  export interface WhatsNewShort
  {
    whatsNew: Lite<WhatsNewEntity>,
    creationDate: string /*DateTime*/;
    title: string,
    description: string,
    status: string,
  };
  
  export interface WhatsNewFull
  {
    whatsNew: Lite<WhatsNewEntity>,
    creationDate: string /*DateTime*/;
    title: string,
    description: string,
    attachments: number,
    previewPicture: boolean,
    status: string,
    read: boolean,
  };
  
  
  
  export interface IconColor {
    icon: IconProp;
    iconColor: string;
  }
  
  export abstract class WhatsNewConfig<T extends Entity> {
    type: Type<T>;
    constructor(type: Type<T>) {
      this.type = type;
    }
  
    abstract getDefaultIcon(): IconColor;

    static coloredIcon(icon: IconProp | undefined, color: string | undefined): React.ReactNode | null {
      if (!icon)
        return null;
  
      return <FontAwesomeIcon aria-hidden={true} icon={icon} className={"icon"} color={color} />;
    }
  }
  
  export const configs: { [type: string]: WhatsNewConfig<any>[] } = {};
  
  export function registerConfig<T extends Entity>(config: WhatsNewConfig<T>): void {
    (configs[config.type.typeName] ??= []).push(config);
  }
}
