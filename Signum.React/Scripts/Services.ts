/// <reference path="../typings/whatwg-fetch/whatwg-fetch.d.ts" />

export interface AjaxOptions {
    url: string;
    avoidNotifyPendingRequests?: boolean;
    avoidThrowError?: boolean;
    avoidShowError?: boolean;

    mode?: string | RequestMode;
    credentials?: string | RequestCredentials;
    cache?: string | RequestCache;
}

export function baseUrl(): string
{
    return window["__baseUrl"] as string;
}

export function ajaxGet<T>(options: AjaxOptions): Promise<T> {
    return wrapRequest(options, () =>
        fetch(baseUrl() + options.url, {
            method: "GET",
            headers: {
                'Accept': 'application/json',
            },
            mode: options.mode,
            credentials: options.credentials,
            cache: options.cache
        })).then(resp=> resp.json<T>());
}

export function ajaxPost<T>(options: AjaxOptions, data: any): Promise<T> {

    return wrapRequest(options, () =>
        fetch(baseUrl() + options.url, {
            method: "POST",
            credentials: options.credentials || "same-origin",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            mode: options.mode,
            cache: options.cache,
            body: JSON.stringify(data),
        })).then(resp=> resp.json<T>());
}


export function wrapRequest(options: AjaxOptions, makeCall: () => Promise<Response>): Promise<Response>
{
    var promise = options.avoidNotifyPendingRequests ? makeCall() : onPendingRequest(makeCall);

    if (!options.avoidThrowError)
        promise = promise.then(throwError);

    if (!options.avoidShowError)
        promise = promise.catch((error: any) =>
        {
            showError(error);
            throw error;
            return null as Response;
        });

    return promise;
}


export var notifyPendingRequests: (pendingRequests: number) => void = () => { };
var pendingRequests: number = 0;
function onPendingRequest(makeCall: ()=>Promise<Response>) {
    
    notifyPendingRequests(pendingRequests++);

    return makeCall().then(
        resp=> { notifyPendingRequests(pendingRequests--); return resp; },
        error => { notifyPendingRequests(pendingRequests--); throw error; });
}


function throwError(response: Response): Response | Promise<Response> {
    if (response.status >= 200 && response.status < 300) {
        return response;
    } else {
        return response.json().then<Response>(json=> {
            throw new ServiceError(response.statusText, json);
            return null;
        });
    }
}

export class ServiceError {
    constructor(public statusText: string, public body: any) {
    }

    toString() {
        return this.statusText + "\r\n" + JSON.stringify(this.body);
    }
}

export var showError = (error: any) => alert(error);
