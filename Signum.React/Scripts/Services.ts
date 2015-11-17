/// <reference path="../typings/es6-promise/es6-promise.d.ts" />
/// <reference path="../typings/jquery/jquery.d.ts" />

import * as JQuery from "jquery"

var $ = JQuery;

export function baseUrl(): string
{
    return window["__baseUrl"];
}

export function ajaxPost(settings: JQueryAjaxSettings): Promise<any> {

    settings.url = baseUrl() + settings.url;

    return new Promise<any>((resolve, reject) => {
        settings.success = resolve;
        settings.error = (jqXHR: JQueryXHR, textStatus: string, errorThrow: string) => reject({ jqXHR: jqXHR, textStatus: textStatus, errorThrow: errorThrow });
        settings.type = "POST";
        $.ajax(settings);
    });
}

export function ajaxGet(settings: JQueryAjaxSettings): Promise<any> {

    settings.url = baseUrl() + settings.url;

    return new Promise<any>((resolve, reject) => {
        settings.success = resolve;
        settings.error = (jqXHR: JQueryXHR, textStatus: string, errorThrow: string) => reject({ jqXHR: jqXHR, textStatus: textStatus, errorThrow: errorThrow });
        settings.type = "GET";
        $.ajax(settings);
    });
}
