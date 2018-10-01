import { ModelState } from './Signum.Entities'
import { GraphExplorer } from './Reflection'

var fetchWithAbortModule = require('./fetchWithAbort') as { fetch: typeof fetch };

export interface AjaxOptions {
    url: string;
    avoidNotifyPendingRequests?: boolean;
    avoidThrowError?: boolean;
    avoidGraphExplorer?: boolean;
    avoidAuthToken?: boolean;
    avoidVersionCheck?: boolean;
    
    headers?: { [index: string]: string };
    mode?: string;
    credentials?: RequestCredentials;
    cache?: string;
    abortController?: FetchAbortController;
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

    if (window.navigator.userAgent.contains("Trident") && (options.cache || "no-cache" == "no-cache")) {
        options.url += "?cacheTicks=" + new Date().getTime();
    }

    return wrapRequest(options, () =>
        fetchWithAbortModule.fetch(baseUrl(options), {
            method: "GET",
            headers: {
                'Accept': 'application/json',
                ...options.headers
            } as any,
            mode: options.mode,
            credentials: options.credentials || "same-origin",
            cache: options.cache || "no-cache",
            abortController: options.abortController
        } as RequestInit));
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
    
    return wrapRequest(options, () =>
        fetchWithAbortModule.fetch(baseUrl(options), {
            method: "POST",
            credentials: options.credentials || "same-origin",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                 ...options.headers
            } as any,
            mode: options.mode,
            cache: options.cache || "no-cache",
            body: JSON.stringify(data),
            abortController: options.abortController
        } as RequestInit));
}





export function wrapRequest(options: AjaxOptions, makeCall: () => Promise<Response>): Promise<Response>
{
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

export module AuthTokenFilter {
    export let addAuthToken: (options: AjaxOptions, makeCall: () => Promise<Response>) => Promise<Response>;
}

export module VersionFilter {
    export let initialVersion: string | undefined;
    export let latestVersion: string | undefined;

    export let versionHasChanged: () => void = () => console.warn("New Server version detected, handle VersionFilter.versionHasChanged to inform user");

    export function onVersionFilter(makeCall: () => Promise<Response>): Promise<Response> {
        function changeVersion(response: Response) {
            var ver = response.headers.get("X-App-Version");

            if (!ver)
                return;

            if (initialVersion == undefined) {
                initialVersion = ver;
                latestVersion = ver;
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
            } else if (response.status == 400) {
                return response.json().then<Response>((modelState: ModelState) => {
                    throw new ValidationError(modelState);
                });
            } else {
                return response.json().then<Response>((error: WebApiHttpError) => {
                    throw new ServiceError(error);
                });
            }
        });
    }
}

let a = document.createElement("a");
document.body.appendChild(a);
a.style.display = "none";


export function saveFile(response: Response) {
    const contentDisposition = response.headers.get("Content-Disposition")!;
    const fileNamePart = contentDisposition.split(";").filter(a => a.trim().startsWith("filename=")).singleOrNull();
    const fileName = fileNamePart ? fileNamePart.trim().after("filename=") : "file.dat";

    response.blob().then(blob => {
        saveFileBlob(blob, fileName);
    });
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

export class ValidationError  {
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
    private abortController?: FetchAbortController;

    constructor(public makeCall: (abortController: FetchAbortController, query: Q) => Promise<A>)
    {
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
    

    getData(query: Q): Promise<A> {

        this.abort();

        this.requestIndex++;

        var myIndex = this.requestIndex;

        this.abortController = {};

        return this.makeCall(this.abortController, query).then(result => {

            if (this.abortController == undefined)
                return new Promise<A>(resolve => { /*never*/ });

            if (myIndex != this.requestIndex) //request is too old
                return new Promise<A>(resolve => { /*never*/ });

            this.abortController = undefined;
            return result;
        }, (error: TypeError) => {
            if (error.message == "Aborted request")
                return new Promise<A>(resolve => { /*never*/ });

            throw error
        }) as Promise<A>;
    }
}