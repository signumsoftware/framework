/// <reference path="../Headers/es6-promises/es6-promises.d.ts"/>
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

    once("setupAjaxRedirectPrefilter", () =>
        setupAjaxRedirect());

    function setupAjaxRedirect() {

        $.ajaxPrefilter(function (options : JQueryAjaxSettings, originalOptions : JQueryAjaxSettings, jqXHR : JQueryXHR) {

            var originalSuccess = options.success;

            var getRredirectUrl = function (ajaxResult) {
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

                var url = getRredirectUrl(result);
                if (!SF.isEmpty(url))
                    location.href = url;

                if (originalSuccess)
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
        };

        export function setItem(key: string, value: string, days?: number) {
            if (isSupported) {
                try {
                    localStorage.setItem(key, value);
                    return true;
                } catch (e) { }
            } else
                Cookies.create(key, value, days ? days : 30);
        };

        return {
            getItem: getItem,
            setItem: setItem
        };
    }

    export function hiddenInput(id: string, value: any) {
        return "<input type='hidden' id='" + id + "' name='" + id + "' value='" + value + "' />\n";
    }

    export function hiddenDiv(id: string, innerHtml: any) {
        return $("<div id='" + id + "' style='display:none'></div>").html(innerHtml);
    }

    export function compose(str: string, ...nextParts: string[]) {

        var result = str; 
        for (var i = 0; i < nextParts.length; i++) {
            var part = nextParts[i];

            result = !result ? part :
            !part ? result :
            result + "_" + part;
        }

        return result;
    }

    export function cloneWithValues(elements: JQuery): JQuery {
        var clone = elements.clone(true);

        var sourceSelect = elements.filter("select").add(elements.find("select"));
        var cloneSelect = clone.filter("select").add(clone.filter("selet"));

        for (var i = 0, l = sourceSelect.length; i < l; i++) {
            cloneSelect.eq(i).val(sourceSelect.eq(i).val());
        }

        return clone;
    }


    export function getPathPrefixes(prefix) {
        var path = [],
            pathSplit = prefix.split("_");

        for (var i = 0, l = pathSplit.length; i < l; i++)
            path[i] = pathSplit.slice(0, i).join("_");

        return path;
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

    export function submit(urlController: string, requestExtraJsonData?: any, $form?: JQuery) : void {
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

    export function submitOnly(urlController: string, requestExtraJsonData: any) {
        if (requestExtraJsonData == null)
            throw "SubmitOnly needs requestExtraJsonData. Use Submit instead";

        var $form = $("<form />",
            {
                method: 'post',
                action: urlController
            });

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

        var currentForm = $("form");
        currentForm.after($form);

        (<HTMLFormElement>$form[0]).submit();
        $form.remove();

        return false;
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
    groupByArray(keySelector: (element: T) => string): { key: string; elements: T[] }[];
    groupByObject(keySelector: (element: T) => string): { [key: string]: T[] };
    orderBy<V>(keySelector: (element: T) => V): T[];
    orderByDescending<V>(keySelector: (element: T) => V): T[];
}

once("arrayExtensions", () => {
    Array.prototype.groupByArray = function (keySelector: (element: any) => string): { key: string; elements: any[] }[]{
        var result: { key: string; elements: any[] }[] = [];
        var objectGrouped = this.groupByObject(keySelector);
        for (var prop in objectGrouped) {
            if (objectGrouped.hasOwnProperty(prop))
                result.push({ key: prop, elements: objectGrouped[prop] });
        }
        return result;
    };

    Array.prototype.groupByObject = function (keySelector: (element: any) => string): { [key: string]: any[] } {
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
});

interface Date {
    addMilisecconds(inc: number): Date;
    addSeccond(inc: number): Date;
    addMinutes(inc: number): Date;
    addHour(inc: number): Date;
    addDate(inc: number): Date;
    addMonth(inc: number): Date;
    addYear(inc: number) : Date;
}

once("dateExtensions", () => {

    Date.prototype.addMilisecconds = function(inc: number) {
        var n = new Date(this.valueOf());
        n.setMilliseconds(this.getMilliseconds() + inc);
        return n;
    };

    Date.prototype.addSeccond = function(inc: number) {
        var n = new Date(this.valueOf());
        n.setSeconds(this.getSeconds() + inc);
        return n;
    };

    Date.prototype.addMinutes = function(inc: number) {
        var n = new Date(this.valueOf());
        n.setMinutes(this.getMinutes() + inc);
        return n;
    };

    Date.prototype.addHour = function(inc: number) {
        var n = new Date(this.valueOf());
        n.setHours(this.getHours() + inc);
        return n;
    };

    Date.prototype.addDate = function(inc: number) {
        var n = new Date(this.valueOf());
        n.setDate(this.getDate() + inc);
        return n;
    };

    Date.prototype.addMonth = function(inc: number) {
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

interface DataTransfer {
    items: {
        type: string;
        kind: string;
        getAsFile(): Blob;
    }[]
}





