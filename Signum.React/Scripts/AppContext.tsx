import * as React from "react";
import { Route, Switch } from "react-router"
import * as H from "history"
import * as AppRelativeRoutes from "./AppRelativeRoutes";
import { IUserEntity, TypeEntity } from "./Signum.Entities.Basics";
import { Dic, classes, } from './Globals';
import { ImportRoute } from "./AsyncImport";
import { clearContextHeaders, ajaxGet, ajaxPost } from "./Services";
import { PseudoType, Type, getTypeName } from "./Reflection";
import { Entity, EntityPack, Lite, ModifiableEntity } from "./Signum.Entities";

Dic.skipClasses.push(React.Component);

export let currentCulture: string | undefined;
export function setCurrentCulture(culture: string | undefined) {
  currentCulture = culture;
}

export let currentUser: IUserEntity | undefined;
export function setCurrentUser(user: IUserEntity | undefined) {
  currentUser = user;
}

export let history: H.History;
export function setCurrentHistory(h: H.History) {
  history = h;
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

export function createAppRelativeHistory(): H.History {
  var h = H.createBrowserHistory({});
  AppRelativeRoutes.useAppRelativeBasename(h);
  AppRelativeRoutes.useAppRelativeComputeMatch(Route);
  AppRelativeRoutes.useAppRelativeComputeMatch(ImportRoute as any);
  AppRelativeRoutes.useAppRelativeSwitch(Switch);
  setCurrentHistory(h);
  return h;
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
    history.push(path);
}

export function toAbsoluteUrl(appRelativeUrl: string): string {
  if (appRelativeUrl?.startsWith("~/"))
    return window.__baseUrl + appRelativeUrl.after("~/");

  var relativeCrappyUrl = history.location.pathname.beforeLast("/") + "/~/"; //In Link render ~/ is considered a relative url
  if (appRelativeUrl?.startsWith(relativeCrappyUrl))
    return window.__baseUrl + appRelativeUrl.after(relativeCrappyUrl);

  if (appRelativeUrl.startsWith(window.__baseUrl) || appRelativeUrl.startsWith("http"))
    return appRelativeUrl;

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

  return React.createElement("span", undefined, ...result);
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
