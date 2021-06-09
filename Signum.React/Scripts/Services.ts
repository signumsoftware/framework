import { ModelState, isEntity } from './Signum.Entities'
import { GraphExplorer } from './Reflection'

export interface AjaxOptions {
  url: string;
  avoidNotifyPendingRequests?: boolean;
  avoidThrowError?: boolean;
  avoidRetry?: boolean;
  avoidGraphExplorer?: boolean;
  avoidAuthToken?: boolean;
  avoidVersionCheck?: boolean;
  avoidContextHeaders?: boolean;

  headers?: { [index: string]: string };
  mode?: string;
  credentials?: RequestCredentials;
  cache?: string;
  signal?: AbortSignal;
}

export function baseUrl(options: AjaxOptions): string {
  const baseUrl = window.__baseUrl;

  if (options.url.startsWith("~/"))
    return baseUrl + options.url.after("~/");

  return options.url;
}

export function ajaxGet<T>(options: AjaxOptions): Promise<T> {
  return ajaxGetRaw(options)
    .then(res => res.text())
    .then(text => text.length ? JSON.parse(text) : undefined);
}

export function ajaxGetRaw(options: AjaxOptions): Promise<Response> {

  return wrapRequest(options, () => {

    const cache = options.cache || "no-cache";
    const isIE11 = !!window.MSInputMethodContext && !!(document as any).documentMode;

    const headers = {
      'Accept': 'application/json',
      ...(cache == "no-cache" && isIE11 ? {
        'Cache-Control': 'no-cache',
        'Pragma': 'no-cache',
      } : undefined),
      ...options.headers
    } as any;

    return fetch(baseUrl(options), {
      method: "GET",
      headers: headers,
      mode: options.mode,
      credentials: options.credentials || "same-origin",
      cache: options.cache || "no-cache",
      signal: options.signal
    } as RequestInit);
  });
}

export function ajaxPost<T>(options: AjaxOptions, data: any): Promise<T> {
  return ajaxPostRaw(options, data)
    .then(res => res.text())
    .then(text => text.length ? JSON.parse(text) : undefined);
}


export function ajaxPostRaw(options: AjaxOptions, data: any): Promise<Response> {
  if (!options.avoidGraphExplorer) {
    GraphExplorer.propagateAll(data);
  }

  return wrapRequest(options, () => {

    const headers = {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      ...options.headers
    } as any;

    return fetch(baseUrl(options), {
      method: "POST",
      credentials: options.credentials || "same-origin",
      headers: headers,
      mode: options.mode,
      cache: options.cache || "no-cache",
      body: JSON.stringify(data),
      signal: options.signal
    } as RequestInit);
  });
}

export const addContextHeaders: ((options: AjaxOptions) => void)[] = [];

export function clearContextHeaders() {
  addContextHeaders.clear();
}

export function wrapRequest(options: AjaxOptions, makeCall: () => Promise<Response>): Promise<Response> {

  if (!options.avoidContextHeaders && addContextHeaders.length > 0) {
    addContextHeaders.forEach(f => f(options));
  }

  if (!options.avoidRetry) {
    const call = makeCall;
    makeCall = () => RetryFilter.retryFilter(call);
  }

  if (!options.avoidVersionCheck) {
    const call = makeCall;
    makeCall = () => VersionFilter.onVersionFilter(call);
  }

  if (!options.avoidThrowError) {
    const call = makeCall;
    makeCall = () => ThrowErrorFilter.throwError(call);
  }

  if (!options.avoidAuthToken && AuthTokenFilter.addAuthToken) {
    let call = makeCall;
    makeCall = () => AuthTokenFilter.addAuthToken(options, call);
  }

  if (!options.avoidNotifyPendingRequests) {
    let call = makeCall;
    makeCall = () => NotifyPendingFilter.onPendingRequest(call);
  }

  const promise = makeCall();

  if (!(promise as any).__proto__.done)
    (promise as any).__proto__.done = Promise.prototype.done;

  return promise;

}

export module RetryFilter {
  export function retryFilter(makeCall: () => Promise<Response>): Promise<Response>{
    return makeCall();
  }
}

export module AuthTokenFilter {
  export let addAuthToken: (options: AjaxOptions, makeCall: () => Promise<Response>) => Promise<Response>;
}

export module VersionFilter {
  export let initialVersion: string | undefined;
  export let initialBuildTime: string | undefined;
  export let latestVersion: string | undefined;

  export let versionHasChanged: () => void = () => console.warn("New Server version detected, handle VersionFilter.versionHasChanged to inform user");

  export function onVersionFilter(makeCall: () => Promise<Response>): Promise<Response> {
    function changeVersion(response: Response) {
      var ver = response.headers.get("X-App-Version");
      var buildTime = response.headers.get("X-App-BuildTime");

      if (!ver)
        return;

      if (initialVersion == undefined) {
        initialVersion = ver;
        latestVersion = ver;
        initialBuildTime = buildTime!;
      }

      if (latestVersion != ver) {
        latestVersion = ver;
        if (versionHasChanged)
          versionHasChanged();
      }
    }

    return makeCall().then(resp => { changeVersion(resp); return resp; });
  }
}

export module NotifyPendingFilter {
  export let notifyPendingRequests: (pendingRequests: number) => void = () => { };
  let pendingRequests: number = 0;
  export function onPendingRequest(makeCall: () => Promise<Response>): Promise<Response> {

    notifyPendingRequests(++pendingRequests);

    return makeCall().then(
      resp => { notifyPendingRequests(--pendingRequests); return resp; },
      error => { notifyPendingRequests(--pendingRequests); throw error; });
  }
}

export module ThrowErrorFilter {
  export function throwError(makeCall: () => Promise<Response>): Promise<Response> {
    return makeCall().then(response => {
      if (response.status >= 200 && response.status < 300) {
        return response;
      } else {
        return response.text().then<Response>(text => {
          if (text.length) {
            var obj = JSON.parse(text);
            if (response.status == 400 && !(obj as WebApiHttpError).exceptionType)
              throw new ValidationError(obj as ModelState);
            else
              throw new ServiceError(obj as WebApiHttpError);
          }
          else
            throw new ServiceError({
              exceptionType: "Status " + response.status,
              exceptionMessage: response.statusText,
              exceptionId: null,
              innerException: null,
              stackTrace: null,
            });
        });
      }
    });
  }
}

let a = document.createElement("a");
document.body.appendChild(a);
a.style.display = "none";

export function saveFile(response: Response, overrideFileName?: string) {

  var fileName = overrideFileName || getFileName(response);

  return response.blob().then(blob => {
    saveFileBlob(blob, fileName);
  });
}

export function getFileName(response: Response) {
  const contentDisposition = response.headers.get("Content-Disposition")!;
  const parts = contentDisposition.split(";");

  const fileNamePartUTF8 = parts.filter(a => a.trim().startsWith("filename*=")).singleOrNull();
  const fileNamePartAscii = parts.filter(a => a.trim().startsWith("filename=")).singleOrNull();

  if (fileNamePartUTF8)
    return decodeURIComponent(fileNamePartUTF8.trim().after("UTF-8''"));

  if (fileNamePartAscii)
    return fileNamePartAscii.trim().after("filename=").replace("\"", "");
  else
    return "file.dat";
}

export function saveFileBlob(blob: Blob, fileName: string) {
  if (window.navigator.msSaveBlob)
    window.navigator.msSaveBlob(blob, fileName);
  else {
    const url = window.URL.createObjectURL(blob);
    a.href = url;

    (a as any).download = fileName;

    a.click();

    setTimeout(() => window.URL.revokeObjectURL(url), 500);
  }
}

export function b64toBlob(b64Data: string, contentType: string = "", sliceSize = 512) {
  contentType = contentType || '';
  sliceSize = sliceSize || 512;

  var byteCharacters = atob(b64Data);
  var byteArrays: Uint8Array[] = [];

  for (var offset = 0; offset < byteCharacters.length; offset += sliceSize) {
    var slice = byteCharacters.slice(offset, offset + sliceSize);

    var byteNumbers = new Array(slice.length);
    for (var i = 0; i < slice.length; i++) {
      byteNumbers[i] = slice.charCodeAt(i);
    }

    var byteArray = new Uint8Array(byteNumbers);

    byteArrays.push(byteArray);
  }

  var blob = new Blob(byteArrays, { type: contentType });
  return blob;
}

export class ServiceError {
  constructor(
    public httpError: WebApiHttpError) {
  }

  get defaultIcon() {
    switch (this.httpError.exceptionType) {
      case "UnauthorizedAccessException": return "lock";
      case "EntityNotFoundException": return "trash";
      case "UniqueKeyException": return "clone";
      default: return "exclamation-triangle";
    }
  }

  toString() {
    return this.httpError.exceptionMessage;
  }
}

export interface WebApiHttpError {
  exceptionType: string;
  exceptionMessage: string | null;
  stackTrace: string | null;
  exceptionId: string | null;
  innerException: WebApiHttpError | null;
}

export class ValidationError {
  modelState: ModelState;

  constructor(modelState: ModelState) {
    this.modelState = modelState;
  }
}


export namespace SessionSharing {

  export let avoidSharingSession = false;

  //localStorage: Domain+Browser
  //sessionStorage: Browser tab, copied when Ctrl+Click from another tab, but not windows.open or just paste link

  var _appName: string = "";

  export function getAppName() {
    return _appName;
  }

  export function setAppNameAndRequestSessionStorage(appName: string) {
    _appName = appName;
    if (!sessionStorage.length) { //Copied from anote
      requestSessionStorageFromAnyTab();
    }
  }

  function requestSessionStorageFromAnyTab() {
    localStorage.setItem('requestSessionStorage' + _appName, new Date().toString());
    localStorage.removeItem('requestSessionStorage' + _appName);
  }

  //http://blog.guya.net/2015/06/12/sharing-sessionstorage-between-tabs-for-secure-multi-tab-authentication/
  //To share session storage between tabs for new tabs WITHOUT windows.opener
  window.addEventListener("storage", se => {

    if (avoidSharingSession)
      return;

    if (se.key == 'requestSessionStorage' + _appName) {
      // Some tab asked for the sessionStorage -> send it

      localStorage.setItem('responseSessionStorage' + _appName, JSON.stringify(sessionStorage));
      localStorage.removeItem('responseSessionStorage' + _appName);

    } else if (se.key == ('responseSessionStorage' + _appName) && !sessionStorage.length) {
      // sessionStorage is empty -> fill it
      if (se.newValue) {
        const data = JSON.parse(se.newValue);

        for (let key in data) {
          sessionStorage.setItem(key, data[key]);
        }

        console.log("SessionStorage taken from any tab");
      }
    }
  });


}


/// This class encapsulates a sequence of ajax request, making them abortable, and auto-aborting previous request when a new one is made
export class AbortableRequest<Q, A> {

  private requestIndex = 0;
  private abortController?: AbortController;

  constructor(public makeCall: (signal: AbortSignal, query: Q) => Promise<A>) {
  }

  abort(): boolean {
    if (!this.abortController || !this.abortController.abort) {
      this.abortController = undefined;
      return false;
    } else {
      this.abortController.abort!();
      this.abortController = undefined;
      return true;
    }
  }

  isRunning() {
    return this.abortController != null;
  }

  getData(query: Q): Promise<A> {

    this.abort();

    this.requestIndex++;

    var myIndex = this.requestIndex;

    this.abortController = new AbortController();

    return this.makeCall(this.abortController.signal, query).then(result => {

      if (this.abortController == undefined)
        return new Promise<A>(resolve => { /*never*/ });

      if (myIndex != this.requestIndex) //request is too old
        return new Promise<A>(resolve => { /*never*/ });

      this.abortController = undefined;
      return result;
    }, (ex: TypeError) => {
      if (ex.name === 'AbortError')
        return new Promise<A>(resolve => { /*never*/ });

      throw ex
    }) as Promise<A>;
  }
}
