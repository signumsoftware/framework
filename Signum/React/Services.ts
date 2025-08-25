import { ModelState, isEntity, ModelEntity } from './Signum.Entities'
import { GraphExplorer } from './Reflection'
import { toAbsoluteUrl } from './AppContext';
import luxon, { DateTime } from 'luxon';

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

export function ajaxGet<T>(options: AjaxOptions): Promise<T> {
  return ajaxGetRaw(options)
    .then(res => res.text())
    .then(text => text.length ? JSON.parse(text) : null);
}

export function ajaxGetRaw(options: AjaxOptions): Promise<Response> {

  return wrapRequest(options, () => {


    const headers = {
      'Accept': 'application/json',
      ...options.headers
    } as any;

    return fetch(toAbsoluteUrl(options.url), {
      method: "GET",
      headers: headers,
      mode: options.mode,
      credentials: options.credentials || "same-origin",
      cache: options.cache || 'no-store',
      signal: options.signal
    } as RequestInit);
  });
}

export function ajaxPost<T>(options: AjaxOptions, data: any): Promise<T> {
  return ajaxPostRaw(options, data)
    .then(res => res.text())
    .then(text => text.length ? JSON.parse(text) : null);
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

    return fetch(toAbsoluteUrl(options.url), {
      method: "POST",
      credentials: options.credentials || "same-origin",
      headers: headers,
      mode: options.mode,
      cache: options.cache || 'no-store',
      body: JSON.stringify(data),
      signal: options.signal
    } as RequestInit);
  });
}

export function ajaxPostUpload<T>(options: AjaxOptions, blob: Blob): Promise<T> {

  return wrapRequest(options, () => {

    if (options.signal?.aborted)
      throw new Error();

    const headers = {
      'Accept': 'application/json',
      'Content-Type': "application/octet-stream",
      ...options.headers
    } as any;

    return fetch(toAbsoluteUrl(options.url), {
      method: "POST",
      credentials: options.credentials || "same-origin",
      headers: headers,
      mode: options.mode,
      cache: options.cache || 'no-store',
      body: blob,
      signal: options.signal
    } as RequestInit);
  }).then(res => res.text())
    .then(text => text.length ? JSON.parse(text) : null);
}


export const addContextHeaders: ((options: AjaxOptions) => void)[] = [];

export function clearContextHeaders(): void {
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
    makeCall = () => ThrowErrorFilter.throwError(call, options.url);
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

  if (!(promise as any).__proto__)
    (promise as any).__proto__ = Promise.prototype;

  return promise;

}

export namespace RetryFilter {
  export function retryFilter(makeCall: () => Promise<Response>): Promise<Response>{
    return makeCall();
  }
}

export namespace AuthTokenFilter {
  export let addAuthToken: (options: AjaxOptions, makeCall: () => Promise<Response>) => Promise<Response>;
}

export namespace VersionFilter {
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
        if (buildTime && initialBuildTime && DateTime.fromISO(buildTime) > DateTime.fromISO(initialBuildTime)) {
          latestVersion = ver;
          if (versionHasChanged)
            versionHasChanged();
        }
      }
    }

    return makeCall().then(resp => { changeVersion(resp); return resp; });
  }
}

export namespace NotifyPendingFilter {
  export let notifyPendingRequests: (pendingRequests: number) => void = () => { };
  let pendingRequests: number = 0;
  export function onPendingRequest(makeCall: () => Promise<Response>): Promise<Response> {

    notifyPendingRequests(++pendingRequests);

    return makeCall()
      .finally(() => notifyPendingRequests(--pendingRequests));
  }
}

export namespace ThrowErrorFilter {
  export function throwError(makeCall: () => Promise<Response>, url: string): Promise<Response> {
    return makeCall().then(response => {
      if (response.status >= 200 && response.status < 300) {
        return response;
      } else {
        return response.text().then<Response>(text => {
          if (text.length) {
            var obj = null;
            try {
              var obj = JSON.parse(text);
            } catch (e) {
              throw new ServiceError({
                exceptionType: "Status " + response.status,
                exceptionMessage: response.statusText + "\n\n" + text,
                exceptionId: null,
                innerException: null,
                stackTrace: null,
              });
            }

            if (response.status == 400 && !(obj as WebApiHttpError).exceptionType)
              throw new ValidationError(obj as ModelState);
            else if ((obj as WebApiHttpError).model)
              throw new ModelRequestedError((obj as WebApiHttpError).model!);
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
    }).catch(error => { error.url = url; throw error; });
  }
}

let a = document.createElement("a");
a.href = "#";
document.body.appendChild(a);
a.style.display = "none";

export function saveFile(response: Response, overrideFileName?: string): Promise<void> {

  var fileName = overrideFileName || getFileName(response);

  return response.blob().then(blob => {
    saveFileBlob(blob, fileName);
  });
}

export function getFileName(response: Response): string {
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

export function saveFileBlob(blob: Blob, fileName: string): void {
  if ((window.navigator as any).msSaveBlob)
    (window.navigator as any).msSaveBlob(blob, fileName);
  else {
    const url = window.URL.createObjectURL(blob);
    a.href = url;

    (a as any).download = fileName;

    a.click();

    window.setTimeout(() => window.URL.revokeObjectURL(url), 500);
  }
}

export function b64toBlob(b64Data: string, contentType: string = "", sliceSize = 512): Blob {
  contentType = contentType || '';
  sliceSize = sliceSize || 512;

  var byteCharacters = atob(b64Data);
  var byteArrays: Uint8Array<ArrayBuffer>[] = [];

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

  get defaultIcon(): "lock" | "trash" | "clone" | "exclamation-triangle" {
    switch (this.httpError.exceptionType) {
      case "UnauthorizedAccessException": return "lock";
      case "EntityNotFoundException": return "trash";
      case "UniqueKeyException": return "clone";
      default: return "exclamation-triangle";
    }
  }

  toString(): string | null {
    return this.httpError.exceptionMessage;
  }
}

export class ExternalServiceError {
  serviceName: string;
  error: any;
  title?: string;
  message?: string;
  additionalInfo?: string;


  constructor(
    serviceName: string,
    error: any,
    title?: string,
    message?: string,
    additionalInfo?: string,
  ) {
    this.serviceName = serviceName;
    this.error = error;
    this.title = title,
      this.message = message;
    this.additionalInfo = additionalInfo;
  }
}

export interface WebApiHttpError {
  exceptionType: string;
  exceptionMessage: string | null;
  stackTrace: string | null;
  exceptionId: string | null;
  model?: ModelEntity;
  innerException: WebApiHttpError | null;
}

export class ValidationError {
  modelState: ModelState;

  constructor(modelState: ModelState) {
    this.modelState = modelState;
  }
}

export class ModelRequestedError {
  model: ModelEntity;

  constructor(model: ModelEntity) {
    this.model = model;
  }
}


export namespace SessionSharing {

  export let avoidSharingSession = false;

  //localStorage: Domain+Browser
  //sessionStorage: Browser tab, copied when Ctrl+Click from another tab, but not windows.open or just paste link

  var _appName: string = "";

  export function getAppName(): string {
    return _appName;
  }

  export function setAppNameAndRequestSessionStorage(appName: string): void {
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

  isRunning(): boolean {
    return this.abortController != null;
  }

  getData(query: Q): Promise<A> {

    this.abort();

    this.requestIndex++;

    var myIndex = this.requestIndex;

    this.abortController = new AbortController();

    return this.makeCall(this.abortController!.signal, query).then(result => {

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
