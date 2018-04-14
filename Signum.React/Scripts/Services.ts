import { ModelState } from './Signum.Entities'
import { Dic } from './Globals'
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
        .then(a => a.status == 204 ? undefined as any : a.json().then(a => a as T));
}

export function ajaxGetRaw(options: AjaxOptions) : Promise<Response> {
    return wrapRequest(options, () =>
        fetchWithAbortModule.fetch(baseUrl(options), {
            method: "GET",
            headers: {
                'Accept': 'application/json',
                ...options.headers
            } as any,
            mode: options.mode,
            credentials: options.credentials || "same-origin",
            cache: options.cache,
            abortController: options.abortController
        } as RequestInit));
}

export function ajaxPost<T>(options: AjaxOptions, data: any): Promise<T> {
    return ajaxPostRaw(options, data)
        .then(a => a.status == 204 ? undefined as any : a.json().then(a => a as T));
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
            cache: options.cache,
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

    export let versionChanged: () => void = () => console.warn("New Server version detected, handle VersionFilter.versionChanged to inform user");

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
                if (versionChanged)
                    versionChanged();
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
                return response.json().then((json: WebApiHttpError) => {
                    if (json.ModelState)
                        throw new ValidationError(response.statusText, json);
                    else if (json.Message)
                        throw new ServiceError(response.statusText, response.status, json);
                }) as any;
            }
        });
    }
}

let a = document.createElement("a");
document.body.appendChild(a);
a.style.display = "none";


export function saveFile(response: Response) {
    let fileName = "file.dat";
    let match = /attachment; filename=(.+)/.exec(response.headers.get("Content-Disposition")!);
    if (match)
        fileName = match[1].trimEnd("\"").trimStart("\"");

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
        public statusText: string,
        public status: number,
        public httpError: WebApiHttpError) {
    }

    get defaultIcon() {
        switch (this.httpError.ExceptionType) {
            case "UnauthorizedAccessException": return "glyphicon-lock";
            case "EntityNotFoundException": return "glyphicon-trash";
            case "UniqueKeyException": return "glyphicon-duplicate";
            default: return "glyphicon-alert";
        }
    }

    toString() {
        return this.httpError.Message;
    }
}

export interface WebApiHttpError {
    Message: string;
    ModelState?: { [member: string]: string[] }
    ExceptionMessage?: string;
    ExceptionType: string;
    StackTrace?: string;
    MessageDetail?: string;
    ExceptionID?: string;
}

export class ValidationError  {
    modelState: ModelState;
    message: string;

    constructor(public statusText: string, json: WebApiHttpError) {
        this.message = json.Message || "";
        this.modelState = json.ModelState!;
    }

    toString() {
        return this.statusText + "\r\n" + this.message;
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