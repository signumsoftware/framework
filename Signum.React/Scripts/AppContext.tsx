import * as React from "react";
import { NavigateFunction, Location, useNavigate, useLocation, To, NavigateOptions } from "react-router";
import { IUserEntity, TypeEntity } from "./Signum.Entities.Basics";
import { Dic, classes, } from './Globals';
import { clearContextHeaders, ajaxGet, ajaxPost } from "./Services";
import { PseudoType, Type, getTypeName } from "./Reflection";
import { Entity, EntityPack, Lite, ModifiableEntity } from "./Signum.Entities";
import { navigateRoute } from "./Navigator";

Dic.skipClasses.push(React.Component);

export let currentCulture: string | undefined;
export function setCurrentCulture(culture: string | undefined) {
  currentCulture = culture;
}

export let currentUser: IUserEntity | undefined;
export function setCurrentUser(user: IUserEntity | undefined) {
  currentUser = user;
}


/*
 * Global react-router navigate, but aware of removing ~ or baseName prefixes
 */
export let navigate: { 
  (to: To, options?: NavigateOptions): void;
  (delta: number): void;
};
export let location: Location;

const waitingQueue: ((val: undefined) => void)[] = [];
export function waitLoaded() {
  if (navigate != null)
    return Promise.resolve();

  return new Promise<undefined>(resolve => {
    waitingQueue.push(resolve);
  });
}

export function useGlobalReactRouter() {

  const [isLoaded, setIsLoaded] = React.useState<boolean>();

  function toRelativeUrl(url: string) {
    if (window.__baseName && url.startsWith(window.__baseName))
      return url.after(window.__baseName);

    if (url.startsWith("~"))
      return url.after("~");

    return url;
  }

  var nav = useNavigate();
  React.useEffect(() => {
    navigate = (to: To | number, options?: NavigateOptions) => {
      if (typeof to == "number")
        nav(to);
      else if (typeof to == "string") {
        nav(toRelativeUrl(to));
      } else if (typeof to == "object") {
        nav({ ...to, pathname: to.pathname && toRelativeUrl(to.pathname) }, options);
      }
      else
        throw new Error("Unexpected argument type: to");
    };

    waitingQueue.forEach(f => f(undefined));
    waitingQueue.clear();

    setIsLoaded(true);

    return () => {
      navigate = undefined!;
    };
  }, []);

  var loc = useLocation();
  React.useEffect(() => {
    location = loc;;
    return () => location = undefined!;
  }, [loc]);

  return isLoaded;

}


export let setTitle: (pageTitle?: string) => void;
export function setTitleFunction(titleFunction: (pageTitle?: string) => void) {
  setTitle = titleFunction;
}

export function useTitle(title: string, deps?: readonly any[]) {
  React.useEffect(() => {
    setTitle(title);
    return () => setTitle();
  }, deps);
}

let rtl = false;
export function isRtl() {
  return rtl;
}

export function setRtl(isRtl: boolean) {
  rtl = isRtl;
}

export const clearSettingsActions: Array<() => void> = [
  clearContextHeaders,
];

export function clearAllSettings() {
  clearSettingsActions.forEach(a => a());
  clearSettingsActions.clear();
  clearSettingsActions.push(clearContextHeaders);
}

export let resetUI: () => void = () => { };
export function setResetUI(reset: () => void) {
  resetUI = reset;
}

export namespace Expander {
  export let onGetExpanded: () => boolean;
  export let onSetExpanded: (isExpanded: boolean) => void;

  export function setExpanded(expanded: boolean): boolean {
    let wasExpanded = onGetExpanded != null && onGetExpanded();;
    if (onSetExpanded)
      onSetExpanded(expanded);

    return wasExpanded;
  }
}

export function useExpand() {
  React.useEffect(() => {
    const wasExpanded = Expander.setExpanded(true);
    return () => { Expander.setExpanded(wasExpanded); }
  }, []);

}

export function pushOrOpenInTab(path: string, e: React.MouseEvent<any> | React.KeyboardEvent<any>) {
  if ((e as React.MouseEvent<any>).button == 2)
    return;

  e.preventDefault();
  if (e.ctrlKey || (e as React.MouseEvent<any>).button == 1)
    window.open(toAbsoluteUrl(path));
  else if (path.startsWith("http"))
    window.location.href = path;
  else
    navigate(path);
}

export function toAbsoluteUrl(appRelativeUrl: string): string {
  if (appRelativeUrl?.startsWith("/") && window.__baseName != "")
    if (!appRelativeUrl.startsWith(window.__baseName))
      return window.__baseName + appRelativeUrl;

  if (appRelativeUrl?.startsWith("~/"))
    return window.__baseName + appRelativeUrl.after("~"); //For backwards compatibility

  //var relativeCrappyUrl = history.location.pathname.beforeLast("/") + "//"; //In Link render / is considered a relative url
  //if (appRelativeUrl?.startsWith(relativeCrappyUrl))
  //  return window.__baseUrl + appRelativeUrl.after(relativeCrappyUrl);

  //if (appRelativeUrl?.startsWith(window.__baseUrl) || appRelativeUrl?.startsWith("http"))
  //  return appRelativeUrl;

  return appRelativeUrl;
}


declare global {
  interface String {
    formatHtml(...parameters: any[]): React.ReactElement<any>;
  }

  interface Array<T> {
    joinCommaHtml(this: Array<T>, lastSeparator: string): React.ReactElement<any>;
  }
}

String.prototype.formatHtml = function (this: string) {
  const regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

  const args = arguments;

  const parts = this.split(regex);

  const result: (string | React.ReactElement<any>)[] = [];
  for (let i = 0; i < parts.length - 4; i += 4) {
    result.push(parts[i]);
    result.push(args[parseInt(parts[i + 1])]);
  }
  result.push(parts[parts.length - 1]);

  return React.createElement(React.Fragment, undefined, ...result);
};

Array.prototype.joinCommaHtml = function (this: any[], lastSeparator: string) {
  const args = arguments;

  const result: (string | React.ReactElement<any>)[] = [];
  for (let i = 0; i < this.length - 2; i++) {
    result.push(this[i]);
    result.push(", ");
  }

  if (this.length >= 2) {
    result.push(this[this.length - 2]);
    result.push(lastSeparator)
  }

  if (this.length >= 1) {
    result.push(this[this.length - 1]);
  }

  return React.createElement("span", undefined, ...result);
}
