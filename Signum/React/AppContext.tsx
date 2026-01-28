import * as React from "react";
import { To, NavigateOptions, useOutletContext, DataRouter } from "react-router";
import { IUserEntity } from "./Signum.Security";
import { PermissionSymbol } from "./Signum.Basics";
import { Dic, classes, } from './Globals';
import { clearContextHeaders, ajaxGet, ajaxPost, RetryFilter } from "./Services";
import { PseudoType, Type, getTypeName, tryGetTypeInfo } from "./Reflection";
import { Entity, EntityPack, Lite, ModifiableEntity } from "./Signum.Entities";

Dic.skipClasses.push(React.Component);

export let currentCulture: string | undefined;
export function setCurrentCulture(culture: string | undefined): void {
  currentCulture = culture;
}

export let currentUser: IUserEntity | undefined;
export function setCurrentUser(user: IUserEntity | undefined): void {
  currentUser = user;
}

export let _internalRouter: DataRouter;
export function setRouter(r: DataRouter): void {
  _internalRouter = r
}

function toRelativeUrl(url: string) {
  if (window.__baseName && url.startsWith(window.__baseName))
    return url.after(window.__baseName);

  if (url.startsWith("~"))
    return url.after("~");

  return url;
}

export function location(): typeof _internalRouter.state.location {
  var loc = _internalRouter.state.location;

  return {
    ...loc,
    pathname: toRelativeUrl(loc.pathname)
  };
}

export function assertPermissionAuthorized(permission: PermissionSymbol | string): void {
  var key = (permission as PermissionSymbol).key ?? permission as string;
  if (!isPermissionAuthorized(key))
    throw new Error(`Permission ${key} is denied`);
}

export function isPermissionAuthorized(permission: PermissionSymbol | string): boolean {
  var key = (permission as PermissionSymbol).key ?? permission as string;
  const type = tryGetTypeInfo(key.before("."));

  if (!type)
    return false;

  const member = type.members[key.after(".")];
  if (!member)
    return false;

  return true;
}


export function navigate(to: To | number, options?: NavigateOptions): void
export function navigate(to: To | number, options?: NavigateOptions): void
export function navigate(to: To | number, options?: NavigateOptions): void {

  if (typeof to == "string" && Boolean(window.__baseName) && to.startsWith(window.__baseName))
    to = to.after(window.__baseName);

  if (typeof to == "number")
    _internalRouter.navigate(to);
  else
    _internalRouter.navigate(to, options);

  //else if (typeof to == "string")
  //  _internalRouter.navigate(toAbsoluteUrl(to), options);
  //else if (typeof to == "object")
  //  _internalRouter.navigate({ ...to, pathname: to.pathname && toAbsoluteUrl(to.pathname) }, options);
  //else
  //  throw new Error("Unexpected argument type: to");
};



export let setTitle: (pageTitle?: string) => void;
export function setTitleFunction(titleFunction: (pageTitle?: string) => void): void {
  setTitle = titleFunction;
}

export function useTitle(title: string, deps?: readonly any[]): void {
  React.useEffect(() => {
    setTitle(title);
    return () => setTitle();
  }, deps);
}

let rtl = false;
export function isRtl(): boolean {
  return rtl;
}

export function setRtl(isRtl: boolean): void {
  rtl = isRtl;
}

export const clearSettingsActions: Array<() => void> = [
  clearContextHeaders,
];

export function clearAllSettings(): void {
  clearSettingsActions.forEach(a => a());
  clearSettingsActions.clear();
  clearSettingsActions.push(clearContextHeaders);
}

export let resetUI: () => void = () => { };
export function setResetUI(reset: () => void): void {
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

export function useExpand(): void {
  useOutletContext();
  React.useEffect(() => {
    const wasExpanded = Expander.setExpanded(true);
    return () => { Expander.setExpanded(wasExpanded); }
  }, []);

}

export function pushOrOpenInTab(path: string, e: React.MouseEvent<any> | React.KeyboardEvent<any> | undefined): void {
  if (e && (e as React.MouseEvent<any>).button == 2)
    return;

  e?.preventDefault();
  if (e && (e.ctrlKey || (e as React.MouseEvent<any>).button == 1))
    window.open(toAbsoluteUrl(path));
  else if (path.startsWith("http"))
    window.location.href = path;
  else
    navigate(toAbsoluteUrl(path));
}



export function toAbsoluteUrl(appRelativeUrl: string, baseName?: string): string {
  baseName ??= window.__baseName;
  if (appRelativeUrl?.startsWith("/") && baseName != "")
    if (!appRelativeUrl.startsWith(baseName + (baseName.endsWith("/") ? "" : "/")))
      return baseName + appRelativeUrl;

  if (appRelativeUrl?.startsWith("~/"))
    return baseName + appRelativeUrl.after("~"); //For backwards compatibility

  //var relativeCrappyUrl = history.location.pathname.beforeLast("/") + "//"; //In Link render / is considered a relative url
  //if (appRelativeUrl?.startsWith(relativeCrappyUrl))
  //  return window.__baseUrl + appRelativeUrl.after(relativeCrappyUrl);

  //if (appRelativeUrl?.startsWith(window.__baseUrl) || appRelativeUrl?.startsWith("http"))
  //  return appRelativeUrl;

  return appRelativeUrl;
}


declare global {
  interface String {
    formatHtml(...parameters: any[]): React.ReactElement;
  }

  interface Array<T> {
    joinCommaHtml(this: Array<T>, lastSeparator: string): React.ReactElement;
    joinHtml(this: Array<T>, separator: string | React.ReactElement): React.ReactElement;
  }
}

String.prototype.formatHtml = function(this: string) {
  const regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

  const args = arguments;

  const parts = this.split(regex);

  const result: (string | React.ReactElement)[] = [];
  for (let i = 0; i < parts.length - 4; i += 4) {
    result.push(parts[i]);
    result.push(args[parseInt(parts[i + 1])]);
  }
  result.push(parts[parts.length - 1]);

  return React.createElement(React.Fragment, undefined, ...result);
};

Array.prototype.joinCommaHtml = function(this: any[], lastSeparator: string) {
  const args = arguments;

  const result: (string | React.ReactElement)[] = [];
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

Array.prototype.joinHtml = function(this: any[], separator: string | React.ReactElement) {
  const args = arguments;

  const result: (string | React.ReactElement)[] = [];
  for (let i = 0; i < this.length - 1; i++) {
    result.push(this[i]);
    result.push(separator);
  }

 
  if (this.length >= 1) {
    result.push(this[this.length - 1]);
  }

  return React.createElement("span", undefined, ...result);
}

