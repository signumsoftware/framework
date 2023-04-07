import * as React from 'react'
import { RouteObject } from 'react-router'
import { Link } from 'react-router-dom'
import { DateTime } from 'luxon'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import SelectorModal from '@framework/SelectorModal'
import ValueLineModal from '@framework/ValueLineModal'
import * as QuickLinks from '@framework/QuickLinks'
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome"
import { andClose } from '@framework/Operations/EntityOperations'
import { ajaxGet, ajaxPost } from '@framework/Services'
import * as Finder from '@framework/Finder'
import { IconProp } from '@fortawesome/fontawesome-svg-core'
import { Entity, isEntity, isLite, Lite, toLite } from '@framework/Signum.Entities'
import { EntityLink } from '@framework/Search'
import { ISymbol, PropertyRoute, symbolNiceName, Type } from '@framework/Reflection'
import { WhatsNewMessageEmbedded, WhatsNewEntity, WhatsNewOperation, WhatsNewMessage } from './Signum.WhatsNew'
import { ImportComponent } from '@framework/ImportComponent'
import { FilePathEmbedded } from '../Files/Signum.Entities.Files'

export function start(options: { routes: RouteObject[] }) {

  options.routes.push({ path: "/newspage/:newsId", element: <ImportComponent onImport={() => import("./Templates/NewsPage")} /> });
  options.routes.push({ path: "/news", element: <ImportComponent onImport={() => import("./Templates/AllNewsPage")} /> });

  Navigator.addSettings(new EntitySettings(WhatsNewEntity, t => import('./Templates/WhatsNew'), { modalSize: "xl" }));

  //Operations.addSettings(new EntityOperationSettings(WhatsNewOperation.Read, {
  //  alternatives: eoc => [andClose(eoc)],
  //  hideOnCanExecute: true
  //}));

  QuickLinks.registerQuickLink(WhatsNewEntity, ctx => new QuickLinks.QuickLinkLink("Preview",
    () => WhatsNewMessage.Preview.niceToString(), "/newspage/" + ctx.lite.id, {
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

  static coloredIcon(icon: IconProp | undefined, color: string | undefined): React.ReactChild | null {
    if (!icon)
      return null;

    return <FontAwesomeIcon icon={icon} className={"icon"} color={color} />;
  }
}

export const configs: { [type: string]: WhatsNewConfig<any>[] } = {};

export function registerConfig<T extends Entity>(config: WhatsNewConfig<T>) {
  (configs[config.type.typeName] ??= []).push(config);
}
