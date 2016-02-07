/// <reference path="../typings/whatwg-fetch/whatwg-fetch.d.ts" />
import { ModelState } from './Signum.Entities'

export interface AjaxOptions {
    avoidBaseUrl?: boolean;
    url: string;
    avoidNotifyPendingRequests?: boolean;
    avoidThrowError?: boolean;
    showError?: ShowError;

    mode?: string | RequestMode;
    credentials?: string | RequestCredentials;
    cache?: string | RequestCache;
}

export enum ShowError {
    Never,
    Default,
    Always,
}

export function baseUrl(options: AjaxOptions): string {
    if (options.avoidBaseUrl)
        return options.url;


    const baseUrl = window["__baseUrl"] as string;

    if (!baseUrl || options.url.startsWith(baseUrl)) //HACK: Too smart?
        return options.url;

    return baseUrl + options.url;
}

export function ajaxGet<T>(options: AjaxOptions): Promise<T> {
    return wrapRequest<T>(options, () =>
        fetch(baseUrl(options), {
            method: "GET",
            headers: {
                'Accept': 'application/json',
            },
            mode: options.mode,
            credentials: options.credentials || "same-origin",
            cache: options.cache
        }));
}

export function ajaxPost<T>(options: AjaxOptions, data: any): Promise<T> {

    return wrapRequest<T>(options, () =>
        fetch(baseUrl(options), {
            method: "POST",
            credentials: options.credentials || "same-origin",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            mode: options.mode,
            cache: options.cache,
            body: JSON.stringify(data),
        }));
}


export function wrapRequest<T>(options: AjaxOptions, makeCall: () => Promise<Response>): Promise<T>
{
    let promise = options.avoidNotifyPendingRequests ? makeCall() : onPendingRequest(makeCall);

    if (!options.avoidThrowError)
        promise = promise.then(throwError);

    if (options.showError != ShowError.Never)
        promise = promise.catch((error: any) =>
        {
            if (options.showError == ShowError.Always || !(error instanceof ValidationError))
                showError(error);

            throw error;
        }) as any;

    return promise.then(a=>
        a.status == 204 ? null : a.json<T>());
}


export const notifyPendingRequests: (pendingRequests: number) => void = () => { };
let pendingRequests: number = 0;
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
        return response.json().then(json=> {
            if (json.ModelState)
                throw new ValidationError(response.statusText, json);
            else
                throw new ServiceError(response.statusText, json);
        }) as any;
    }
}

export class ServiceError extends Error {
    constructor(public statusText: string, public body: any) {
        super(statusText)
    }

    toString() {
        return this.statusText + "\r\n" + JSON.stringify(this.body);
    }
}

export class ValidationError extends Error {
    modelState: ModelState;
    message: string;

    constructor(public statusText: string, json: { Message: string, ModelState: ModelState }) {
        super(statusText)
        this.message = json.Message;
        this.modelState = json.ModelState;
    }

    toString() {
        return this.statusText + "\r\n" + this.message;
    }
}



export const showError = (error: any) => alert(error);
