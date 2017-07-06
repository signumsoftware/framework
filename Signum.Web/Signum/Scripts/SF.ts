/// <reference path="../Headers/jquery/jquery.d.ts"/>


declare var lang: any;

var onceStorage: any = {};
function once(key: string, func: () => void) {
    if (onceStorage[key] === undefined) {
        func();
        onceStorage[key] = "loaded";
    }
}

module SF {
    export var Urls: any;
    export var Locale: any;

    export var debug = true;

    export function log(s: string) {
        if (debug) {
            if (typeof console != "undefined" && typeof console.debug != "undefined") console.log(s);
        }
    }

    var ajaxExtraParameters: { (extraArgs: FormObject): void }[] = [];
    export function registerAjaxExtraParameters(getExtraParams: (extraArgs: FormObject) => void) {
        if (getExtraParams != null)
            ajaxExtraParameters.push(getExtraParams);
    }
    export function addAjaxExtraParameters(originalParams: FormObject) {
        if (ajaxExtraParameters.length > 0) {
            ajaxExtraParameters.forEach(addExtraParametersFunc => {
                addExtraParametersFunc(originalParams);
            });
        }
    }

    once("setupAjaxRedirectPrefilter", () => {
        setupAjaxRedirect();
        setupAjaxExtraParameters();
    });

    function setupAjaxExtraParameters() {

        $.ajaxPrefilter((options: JQueryAjaxSettings, originalOptions: JQueryAjaxSettings, jqXHR: JQueryXHR) => {
            if (ajaxExtraParameters.length) {
                var data = $.extend({}, originalOptions.data);
                addAjaxExtraParameters(data);
                options.data = $.param(data);
            }
        });
    }

    function setupAjaxRedirect() {

        $.ajaxPrefilter(function (options: JQueryAjaxSettings, originalOptions: JQueryAjaxSettings, jqXHR: JQueryXHR) {

            var originalSuccess = options.success;

            var getRedirectUrl = function (ajaxResult) {
                if (SF.isEmpty(ajaxResult))
                    return null;

                if (typeof ajaxResult !== "object")
                    return null;

                if (ajaxResult.result == null)
                    return null;

                if (ajaxResult.result == 'url')
                    return ajaxResult.url;

                return null;
            };

            options.success = function (result, text, xhr) {
                //if (!options.avoidRedirect && jqXHR.status == 302)  
                //    location.href = jqXHR.getResponseHeader("Location");

                var url = getRedirectUrl(result);
                if (!SF.isEmpty(url))
                    location.href = url;
                else if (originalSuccess)
                    originalSuccess(result, text, xhr);
            };
        });
    }

    export function isEmpty(value): boolean {
        return (value == undefined || value == null || value === "" || value.toString() == "");
    };

    export module InputValidator {
        export function isNumber(e: KeyboardEvent): boolean {
            var c = e.keyCode;
            return ((c >= 48 && c <= 57) /*0-9*/ ||
                (c >= 96 && c <= 105) /*NumPad 0-9*/ ||
                (c == 8) /*BackSpace*/ ||
                (c == 9) /*Tab*/ ||
                (c == 12) /*Clear*/ ||
                (c == 27) /*Escape*/ ||
                (c == 37) /*Left*/ ||
                (c == 39) /*Right*/ ||
                (c == 46) /*Delete*/ ||
                (c == 36) /*Home*/ ||
                (c == 35) /*End*/ ||
                (c == 109) /*NumPad -*/ ||
                (c == 189) /*-*/ ||
                (e.ctrlKey && c == 86) /*Ctrl + v*/ ||
                (e.ctrlKey && c == 67) /*Ctrl + v*/
            );
        }

        export function isDecimal(e: KeyboardEvent): boolean {
            var c = e.keyCode;
            return (
                this.isNumber(e) ||
                (c == 110) /*NumPad Decimal*/ ||
                (c == 190) /*.*/ ||
                (c == 188) /*,*/
            );
        }
    }

    export module Cookies {
        export function read(name: string) {
            var nameEQ = name + "=";
            var ca = document.cookie.split(';');
            for (var i = 0; i < ca.length; i++) {
                var c = ca[i];
                while (c.charAt(0) == ' ') c = c.substring(1, c.length);
                if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
            }
            return null;
        }

        export function create(name: string, value: string, days: number, domain?: string) {
            var expires = null,
                path = "/";

            if (days) {
                var date = new Date();
                date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
                expires = date;
            }

            document.cookie = name + "=" + encodeURI(value) + ((expires) ? ";expires=" + expires.toGMTString() : "") + ((path) ? ";path=" + path : "") + ((domain) ? ";domain=" + domain : "");
        }
    }


    export module LocalStorage {
        var isSupported = typeof (localStorage) != 'undefined';

        export function getItem(key: string) {
            if (isSupported) {
                try {
                    return localStorage.getItem(key);
                } catch (e) { }
            }
            return Cookies.read(key);
        }

        export function setItem(key: string, value: string, days?: number) {
            if (isSupported) {
                try {
                    localStorage.setItem(key, value);
                    return true;
                } catch (e) { }
            } else
                Cookies.create(key, value, days ? days : 30);
        }
    }

    export function hiddenInput(id: string, value: any) {
        return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
    }

    export function hiddenDiv(id: string, innerHtml: any) {
        return $("<div id='" + id + "' style='display:none'></div>").html(innerHtml);
    }

    export function ajaxPost(settings: JQueryAjaxSettings): Promise<any> {

        return new Promise<any>((resolve, reject) => {
            settings.success = resolve;
            settings.error = (jqXHR: JQueryXHR, textStatus: string, errorThrow: string) => reject({ jqXHR: jqXHR, textStatus: textStatus, errorThrow: errorThrow });
            settings.type = "POST";
            $.ajax(settings);
        });
    }

    export function ajaxGet(settings: JQueryAjaxSettings): Promise<any> {

        return new Promise<any>((resolve, reject) => {
            settings.success = resolve;
            settings.error = (jqXHR: JQueryXHR, textStatus: string, errorThrow: string) => reject({ jqXHR: jqXHR, textStatus: textStatus, errorThrow: errorThrow });
            settings.type = "GET";
            $.ajax(settings);
        });
    }

    export function promiseForeach<T>(array: T[], action: (elem: T) => Promise<void>): Promise<void> {
        return array.reduce<Promise<void>>(
            (prom, val) => prom.then(() => action(val)),
            Promise.resolve<void>(null));
    }

    export function submit(urlController: string, requestExtraJsonData?: any, $form?: JQuery): void {
        $form = $form || $("form");
        if (!SF.isEmpty(requestExtraJsonData)) {
            if ($.isFunction(requestExtraJsonData))
                requestExtraJsonData = requestExtraJsonData();

            for (var key in requestExtraJsonData) {
                if (requestExtraJsonData.hasOwnProperty(key)) {
                    $form.append(SF.hiddenInput(key, requestExtraJsonData[key]));
                }
            }
        }

        (<HTMLFormElement>$form.attr("action", urlController)[0]).submit();
    }

    export function submitOnly(urlController: string, requestExtraJsonData: any, openNewWindow?: boolean) {
        if (requestExtraJsonData == null)
            throw "SubmitOnly needs requestExtraJsonData. Use Submit instead";

        var $form = $("<form />",
            {
                method: 'post',
                action: urlController
            });

        if (openNewWindow)
            $form.attr("target", "_blank");

        if (!SF.isEmpty(requestExtraJsonData)) {
            if ($.isFunction(requestExtraJsonData)) {
                requestExtraJsonData = requestExtraJsonData();
            }
            for (var key in requestExtraJsonData) {
                if (requestExtraJsonData.hasOwnProperty(key)) {
                    $form.append(SF.hiddenInput(key, requestExtraJsonData[key]));
                }
            }
        }

        $("body").after($form);

        (<HTMLFormElement>$form[0]).submit();
        $form.remove();

        return false;
    }

    export function isTouchDevice() {
        try {
            document.createEvent("TouchEvent");
            return true;
        } catch (e) {
            return false;
        }
    }

    export function addCssDynamically(path: string) {
        if ($('link[href="' + path + '"]').length)
            return;

        $('head').append('<link rel="stylesheet" href="' + path + '" type="text/css" />');
    }
}

interface JQuery {
    serializeObject(): FormObject
}

interface JQueryAjaxSettings {
    avoidRedirect?: boolean;
}

interface FormObject {
    [formKey: string]: any
}

once("serializeObject", () => {
    $.fn.serializeObject = function () {
        var o = {};
        var a = this.serializeArray();
        $.each(a, function () {
            if (o[this.name] !== undefined) {
                o[this.name] += "," + (this.value || '');
            } else {
                o[this.name] = this.value || '';
            }
        });
        return o;
    };
});

interface Array<T> {
    groupBy(keySelector: (element: T) => string): { key: string; elements: T[] }[];
    groupToObject(keySelector: (element: T) => string): { [key: string]: T[] };
    orderBy<V>(keySelector: (element: T) => V): T[];
    orderByDescending<V>(keySelector: (element: T) => V): T[];
    toObject(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T };
    toObject<V>(this: Array<T>, keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    toObjectDistinct(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T };
    toObjectDistinct<V>(this: Array<T>, keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    flatMap<R>(selector: (element: T) => R[]): R[];
    max(): T;
    min(): T;
}

once("arrayExtensions", () => {
    Array.prototype.groupBy = function (keySelector: (element: any) => string): { key: string; elements: any[] }[] {
        var result: { key: string; elements: any[] }[] = [];
        var objectGrouped = (this as any[]).groupToObject(keySelector);
        for (var prop in objectGrouped) {
            if (objectGrouped.hasOwnProperty(prop))
                result.push({ key: prop, elements: objectGrouped[prop] });
        }
        return result;
    };

    Array.prototype.groupToObject = function (keySelector: (element: any) => string): { [key: string]: any[] } {
        var result: { [key: string]: any[] } = {};

        for (var i = 0; i < this.length; i++) {
            var element: any = this[i];
            var key = keySelector(element);
            if (!result[key])
                result[key] = [];
            result[key].push(element);
        }
        return result;
    };

    Array.prototype.orderBy = function (keySelector: (element: any) => any): any[] {
        var cloned = (<any[]>this).slice(0);
        cloned.sort((e1, e2) => {
            var v1 = keySelector(e1);
            var v2 = keySelector(e2);
            if (v1 > v2)
                return 1;
            if (v1 < v2)
                return -1;
            return 0;
        });
        return cloned;
    };

    Array.prototype.orderByDescending = function (keySelector: (element: any) => any): any[] {
        var cloned = (<any[]>this).slice(0);
        cloned.sort((e1, e2) => {
            var v1 = keySelector(e1);
            var v2 = keySelector(e2);
            if (v1 < v2)
                return 1;
            if (v1 > v2)
                return -1;
            return 0;
        });
        return cloned;
    };

    Array.prototype.toObject = function (this: any[], keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
        const obj: any = {};

        this.forEach(item => {
            const key = keySelector(item);

            if (obj[key])
                throw new Error("Repeated key {0}".format(key));

            obj[key] = valueSelector ? valueSelector(item) : item;
        });

        return obj;
    };

    Array.prototype.toObjectDistinct = function (this: any[], keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
        const obj: any = {};

        this.forEach(item => {
            const key = keySelector(item);

            obj[key] = valueSelector ? valueSelector(item) : item;
        });

        return obj;
    };

    Array.prototype.flatMap = function (selector: (element: any) => any[]): any {

        var array = [];

        (<Array<any>>this).forEach(item =>
            selector(item).forEach(item2 =>
                array.push(item2)
            ));

        return array;
    };

    Array.prototype.max = function () {
        return Math.max.apply(null, this);
    };

    Array.prototype.min = function () {
        return Math.min.apply(null, this);
    };
});

interface String {
    hasText(): boolean;
    contains(str: string): boolean;
    startsWith(str: string): boolean;
    endsWith(str: string): boolean;
    format(...parameters: any[]): string;
    replaceAll(from: string, to: string);
    after(separator: string): string;
    before(separator: string): string;
    tryAfter(separator: string): string;
    tryBefore(separator: string): string;
    afterLast(separator: string): string;
    beforeLast(separator: string): string;
    tryAfterLast(separator: string): string;
    tryBeforeLast(separator: string): string;

    child(pathPart: string): string;
    parent(pathPart?: string): string;

    get(context?: JQuery): JQuery;
    get(context?: Element): JQuery;
    tryGet(context?: JQuery): JQuery;
    tryGet(context?: Element): JQuery;

    getChild(pathPart: string): JQuery;
    tryGetChild(pathPart: string): JQuery;
}

once("stringExtensions", () => {
    String.prototype.hasText = function () {
        return (this == null || this == undefined || this == '') ? false : true;
    }

    String.prototype.contains = function (str) {
        return this.indexOf(str) !== -1;
    }

    String.prototype.startsWith = function (str) {
        return this.indexOf(str) === 0;
    }

    String.prototype.endsWith = function (str) {
        return this.lastIndexOf(str) === (this.length - str.length);
    }

    String.prototype.format = function () {
        var regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

        var args = arguments;

        var getValue = function (key) {
            if (args == null || typeof args === 'undefined') return null;

            var value = args[key];
            var type = typeof value;

            return type === 'string' || type === 'number' ? value : null;
        };

        return this.replace(regex, function (match) {
            //match will look like {sample-match}
            //key will be 'sample-match';
            var key = match.substr(1, match.length - 2);

            var value = getValue(key);

            return value != null ? value : match;
        });
    };

    String.prototype.replaceAll = function (from, to) {
        return this.split(from).join(to)
    };

    String.prototype.before = function (separator) {
        var index = this.indexOf(separator);
        if (index == -1)
            throw Error("{0} not found".format(separator));

        return this.substring(0, index);
    };

    String.prototype.after = function (separator) {
        var index = this.indexOf(separator);
        if (index == -1)
            throw Error("{0} not found".format(separator));

        return this.substring(index + separator.length);
    };

    String.prototype.tryBefore = function (separator) {
        var index = this.indexOf(separator);
        if (index == -1)
            return null;

        return this.substring(0, index);
    };

    String.prototype.tryAfter = function (separator) {
        var index = this.indexOf(separator);
        if (index == -1)
            return null;

        return this.substring(index + separator.length);
    };

    String.prototype.beforeLast = function (separator) {
        var index = this.lastIndexOf(separator);
        if (index == -1)
            throw Error("{0} not found".format(separator));

        return this.substring(0, index);
    };

    String.prototype.afterLast = function (separator) {
        var index = this.lastIndexOf(separator);
        if (index == -1)
            throw Error("{0} not found".format(separator));

        return this.substring(index + separator.length);
    };

    String.prototype.tryBeforeLast = function (separator) {
        var index = this.lastIndexOf(separator);
        if (index == -1)
            return null;

        return this.substring(0, index);
    };

    String.prototype.tryAfterLast = function (separator) {
        var index = this.lastIndexOf(separator);
        if (index == -1)
            return null;

        return this.substring(index + separator.length);
    };

    if (typeof String.prototype.trim !== 'function') {
        String.prototype.trim = function () {
            return this.replace(/^\s+|\s+$/, '');
        }
    }

    String.prototype.child = function (pathPart) {

        if (SF.isEmpty(this))
            return pathPart;

        if (SF.isEmpty(pathPart))
            return this;

        if (this.endsWith("_"))
            throw new Error("path {0} ends with _".format(this.toString()));

        if (pathPart.startsWith("_"))
            throw new Error("pathPart {0} starts with _".format(pathPart));

        return this + "_" + pathPart;
    };

    String.prototype.parent = function (pathPart) {

        if (SF.isEmpty(this))
            throw new Error("impossible to pop the empty string");

        if (SF.isEmpty(pathPart)) {
            var index = this.lastIndexOf("_");

            if (index == -1)
                return "";

            return this.substr(0, index);
        }
        else {
            if (this == pathPart)
                return "";

            var index = this.lastIndexOf("_" + pathPart);

            if (index != -1)
                return this.substr(0, index);

            if (pathPart.startsWith(pathPart + "_"))
                return "";

            throw Error("pathPart {0} not found on {1}".format(pathPart, this.toString()));
        }
    };

    String.prototype.get = function (context) {

        if (SF.isEmpty(this))
            throw new Error("Impossible to call 'get' on the empty string");

        var selector = "[id='" + this + "']";

        var result = $(selector, context);

        if (result.length == 0 && context)
            result = $(context).filter(selector);

        if (result.length == 0)
            throw new Error("No element with id = '{0}' found".format(this.toString()));

        if (result.length > 1)
            throw new Error("{0} elements with id = '{1}' found".format(result.length, this.toString()));

        return result;
    };

    String.prototype.tryGet = function (context) {

        if (SF.isEmpty(this))
            throw new Error("Impossible to call 'get' on the empty string");

        var selector = "[id='" + this + "']";

        var result = $(selector, context);

        if (result.length == 0 && context)
            result = $(context).filter(selector);

        if (result.length > 1)
            throw new Error("{0} elements with id = '{1}' found".format(result.length, this.toString()));

        return result;
    };
});

interface Date {
    addMiliseconds(inc: number): Date;
    addSecond(inc: number): Date;
    addMinutes(inc: number): Date;
    addHour(inc: number): Date;
    addDate(inc: number): Date;
    addMonth(inc: number): Date;
    addYear(inc: number): Date;
}

once("dateExtensions", () => {

    Date.prototype.addMiliseconds = function (inc: number) {
        var n = new Date(this.valueOf());
        n.setMilliseconds(this.getMilliseconds() + inc);
        return n;
    };

    Date.prototype.addSecond = function (inc: number) {
        var n = new Date(this.valueOf());
        n.setSeconds(this.getSeconds() + inc);
        return n;
    };

    Date.prototype.addMinutes = function (inc: number) {
        var n = new Date(this.valueOf());
        n.setMinutes(this.getMinutes() + inc);
        return n;
    };

    Date.prototype.addHour = function (inc: number) {
        var n = new Date(this.valueOf());
        n.setHours(this.getHours() + inc);
        return n;
    };

    Date.prototype.addDate = function (inc: number) {
        var n = new Date(this.valueOf());
        n.setDate(this.getDate() + inc);
        return n;
    };

    Date.prototype.addMonth = function (inc: number) {
        var n = new Date(this.valueOf());
        n.setMonth(this.getMonth() + inc);
        return n;
    };

    Date.prototype.addYear = function (inc: number) {
        var n = new Date(this.valueOf());
        n.setFullYear(this.getFullYear() + inc);
        return n;
    };
});

interface Window {
    File: any;
    FileList: any;
    FileReader: any;
}

interface Window {
    changeTextArea(value: string, runtimeInfo: string);

    getExceptionNumber(): number;
}

interface Error {
    lineNumber: number;
}

interface Document {
    onpaste: (ev: { clipboardData: DataTransfer }) => any;
}


//https://github.com/spencertipping/jquery.fix.clone/blob/master/jquery.fix.clone.js

(function (original) {
    jQuery.fn.clone = function () {
        var result = original.apply(this, arguments),
            my_textareas = this.find('textarea').add(this.filter('textarea')),
            result_textareas = result.find('textarea').add(result.filter('textarea')),
            my_selects = this.find('select').add(this.filter('select')),
            result_selects = result.find('select').add(result.filter('select'));

        for (var i = 0, l = my_textareas.length; i < l; ++i) $(result_textareas[i]).val($(my_textareas[i]).val());
        for (var i = 0, l = my_selects.length; i < l; ++i) {
            for (var j = 0, m = my_selects[i].options.length; j < m; ++j) {
                if (my_selects[i].options[j].selected === true) {
                    result_selects[i].options[j].selected = true;
                }
            }
        }
        return result;
    };
})(jQuery.fn.clone);

