/// <reference path="../typings/react/react.d.ts" />
/// <reference path="../typings/react/react-dom.d.ts" />
/// <reference path="../typings/react-router/react-router.d.ts" />
/// <reference path="../typings/react-router/history.d.ts" />
/// <reference path="../typings/react-bootstrap/react-bootstrap.d.ts" />
/// <reference path="../typings/react-router-bootstrap/react-router-bootstrap.d.ts" />
/// <reference path="../typings/react-widgets/react-widgets.d.ts" />
/// <reference path="../typings/es6-promise/es6-promise.d.ts" />
/// <reference path="../typings/requirejs/require.d.ts" />
/// <reference path="../typings/moment/moment.d.ts" />

function hasFlag(value: number, flag: number): boolean {
    return (value & flag) == flag;
}

module Dic {

    export function getValues<V>(obj: { [key: string]: V }): V[] {
        var result: V[] = [];

        for (var name in obj) {
            if (obj.hasOwnProperty(name)) {
                result.push(obj[name]);
            }
        }

        return result;
    }

    export function getKeys(obj: { [key: string]: any }): string[] {
        var result: string[] = [];

        for (var name in obj) {
            if (obj.hasOwnProperty(name)) {
                result.push(name);
            }
        }

        return result;
    }
    
    export function map<V, R>(obj: { [key: string]: V }, selector: (key: string, value: V) => R) : R[] {

        var result: R[] = [];
        for (var name in obj) {
            if (obj.hasOwnProperty(name)) {
                result.push(selector(name, obj[name]));
            }
        }
        return result;
    }

    export function foreach<V>(obj: { [key: string]: V }, action: (key: string, value: V) => void) {

        for (var name in obj) {
            if (obj.hasOwnProperty(name)) {
                action(name, obj[name]);
            }
        }
    }


    export function addOrThrow<V>(dic: { [key: string]: V }, key: string, value: V, errorContext?: string) {
        if (dic[key])
            throw new Error(`Key ${key} already added` + (errorContext ? "in " + errorContext : ""));

        dic[key] = value;
    }

    export function copy<T>(object: T): T {
        var objectCopy = <T>{};

        for (var key in object) {
            if (object.hasOwnProperty(key)) {
                objectCopy[key] = object[key];
            }
        }

        return objectCopy;
    }

    export function extend<O>(out: O): O;
    export function extend<O, U>(out: O, arg1: U): O & U;
    export function extend<O, U, V>(out: O, arg1: U, arg2: V): O & U & V;
    export function extend<O, U, V>(out: O, ...args: Object[]): any;
    export function extend(out) {
        out = out || {};

        for (var i = 1; i < arguments.length; i++) {

            var a = arguments[i];

            if (!a)
                continue;

            for (var key in a) {
                if (a.hasOwnProperty(key) && a[key] !== undefined)
                    out[key] = a[key];
            }
        }

        return out;
    };
}

interface Array<T> {
    groupByArray(keySelector: (element: T) => string): { key: string; elements: T[] }[];
    groupByObject(keySelector: (element: T) => string): { [key: string]: T[] };
    orderBy<V>(keySelector: (element: T) => V): T[];
    orderByDescending<V>(keySelector: (element: T) => V): T[];
    toObject(keySelector: (element: T) => string): { [key: string]: T };
    toObject<V>(keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    toObjectDistinct(keySelector: (element: T) => string): { [key: string]: T };
    toObjectDistinct<V>(keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    flatMap<R>(selector: (element: T) => R[]): R[];
    max(): T;
    min(): T;
    first(errorContext?: string): T;
    firstOrNull(): T;
    last(errorContext?: string): T;
    lastOrNull(): T;
    single(errorContext?: string): T;
    singleOrNull(errorContext?: string): T;
    contains(element: T): boolean;
    remove(element: T): boolean;
    removeAt(index: number);
    insertAt(index: number, element: T);
    clone(): T[];
}


Array.prototype.groupByArray = function (keySelector: (element: any) => string): { key: string; elements: any[] }[] {
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

Array.prototype.toObject = function (keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
    var obj = {};

    (<Array<any>>this).forEach(item=> {
        var key = keySelector(item);

        if (obj[key])
            throw new Error("Repeated key {0}".formatWith(key));


        obj[key] = valueSelector ? valueSelector(item) : item;
    });

    return obj;
};

Array.prototype.toObjectDistinct = function (keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
    var obj = {};

    (<Array<any>>this).forEach(item=> {
        var key = keySelector(item);

        obj[key] = valueSelector ? valueSelector(item) : item;
    });

    return obj;
};

Array.prototype.flatMap = function (selector: (element: any) => any[]): any {

    var array = [];

    (<Array<any>>this).forEach(item=>
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


Array.prototype.first = function (errorContext) {

    if (this.length == 0)
        throw new Error("No " + (errorContext || "element") + " found");

    return this[0];
};


Array.prototype.firstOrNull = function () {

    if (this.length == 0)
        return null;

    return this[0];
};

Array.prototype.last = function (errorContext) {

    if (this.length == 0)
        throw new Error("No " + (errorContext || "element") + " found");

    return this[this.length - 1];
};


Array.prototype.lastOrNull = function () {

    if (this.length == 0)
        return null;

    return this[this.length - 1];
};

Array.prototype.single = function (errorContext) {

    if (this.length == 0)
        throw new Error("No " + (errorContext || "element")  + " found");

    if (this.length > 1)
        throw new Error("More than one " + (errorContext || "element")  + " found");

    return this[0];
};

Array.prototype.singleOrNull = function (errorContext) {

    if (this.length == 0)
        return null;

    if (this.length > 1)
        throw new Error("More than one " + (errorContext || "element")  + " found");

    return this[0];
};

Array.prototype.contains = function (element) {
    return (this as Array<any>).indexOf(element) != -1;
};

Array.prototype.removeAt = function (index) {
    (this as Array<any>).splice(index, 1);
};

Array.prototype.remove = function (element) {

    var index = (this as Array<any>).indexOf(element);
    if (index == -1)
        return false;

    (this as Array<any>).splice(index, 1);
    return true;
};

Array.prototype.insertAt = function (index, element) {
    (this as Array<any>).splice(index, 0, element);
};

Array.prototype.clone = function () {
    return (this as Array<any>).slice(0);
};

interface ArrayConstructor {

    range(min: number, max: number): number[];
}


Array.range = function (min, max) {

    var length = max - min;

    var result = new Array(length);
    for (var i = 0; i < length; i++) {
        result[i] = min + i;
    }

    return result;
}

interface String {
    hasText(): boolean;
    contains(str: string): boolean;
    startsWith(str: string): boolean;
    endsWith(str: string): boolean;
    formatWith(...parameters: any[]): string;
    formatHtml(...parameters: any[]): any[];
    forGenderAndNumber(number: number): string;
    forGenderAndNumber(gender: string): string;
    forGenderAndNumber(gender: any, number: number): string;
    replaceAll(from: string, to: string);
    after(separator: string): string;
    before(separator: string): string;
    tryAfter(separator: string): string;
    tryBefore(separator: string): string;
    afterLast(separator: string): string;
    beforeLast(separator: string): string;
    tryAfterLast(separator: string): string;
    tryBeforeLast(separator: string): string;

    firstUpper(): string;
    firstLower(): string;
}

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

String.prototype.formatWith = function () {
    var regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    var args = arguments;

    return this.replace(regex, function (match) {
        //match will look like {sample-match}
        //key will be 'sample-match';
        var key = match.substr(1, match.length - 2);

        return args[key];
    });
};

String.prototype.formatHtml = function () {
    var regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    var args = arguments;

    var parts = this.split(regex);

    var result = [];
    for (let i = 0; i < parts.length - 4; i += 4) {
        result.push(parts[i]);
        result.push(args[parts[i + 1]]);
    }
    result.push(parts[parts.length - 1]);

    return result;
};

String.prototype.forGenderAndNumber = function (gender: any, number?: number) {

    if (!number && !isNaN(parseFloat(gender))) {
        number = gender;
        gender = null;
    }

    if ((gender == null || gender == "") && number == null)
        return this;

    function replacePart(textToReplace: string, ...prefixes: string[]): string {
        return textToReplace.replace(/\[[^\]\|]+(\|[^\]\|]+)*\]/g, m => {
            var captures = m.substr(1, m.length - 2).split("|");

            for (var i = 0; i < prefixes.length; i++){
                var pr = prefixes[i];
                var capture = captures.filter(c => c.startsWith(pr)).firstOrNull();
                if (capture != null)
                    return capture.substr(pr.length);
            }

            return "";
        });
    }
             

    if (number == null)
        return replacePart(this, gender + ":");

    if (gender == null) {
        if (number == 1)
            return replacePart(this, "1:");

        return replacePart(this, number + ":", ":", "");
    }

    if (number == 1)
        return replacePart(this, "1" + gender.Value + ":", "1:");

    return replacePart(this, gender.Value + number + ":", gender.Value + ":", number + ":", ":");


};


function isNumber(n: any): boolean {
    return !isNaN(parseFloat(n)) && isFinite(n);
}


String.prototype.replaceAll = function (from, to) {
    return this.split(from).join(to)
};

String.prototype.before = function (separator) {
    var index = this.indexOf(separator);
    if (index == -1)
        throw Error("{0} not found".formatWith(separator));

    return this.substring(0, index);
};

String.prototype.after = function (separator) {
    var index = this.indexOf(separator);
    if (index == -1)
        throw Error("{0} not found".formatWith(separator));

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
        throw Error("{0} not found".formatWith(separator));

    return this.substring(0, index);
};

String.prototype.afterLast = function (separator) {
    var index = this.lastIndexOf(separator);
    if (index == -1)
        throw Error("{0} not found".formatWith(separator));

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

String.prototype.firstUpper = function () {
    return (this[0] as string).toUpperCase() + this.substring(1);
};

String.prototype.firstLower = function () {
    return (this[0] as string).toLowerCase() + this.substring(1);
};

if (typeof String.prototype.trim !== 'function') {
    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/, '');
    }
}




function classes(...classNames: string[]) {
    return classNames.filter(a=> a != null && a != "").join(" ");
}

declare module moment {
    interface Moment {
        fromUserInterface(): Moment;
        toUserInterface(): Moment;
        
    }

    interface MomentStatic {
        smartNow();
    }
}


function asumeGlobalUtcMode(moment: moment.MomentStatic, utcMode: boolean) {
    if (utcMode) {
        moment.fn.fromUserInterface = function () { return this.utc(); };
        moment.fn.toUserInterface = function () { return this.local(); };
        moment.smartNow = function () { return moment.utc(); };
    }

    else {
        moment.fn.fromUserInterface = function () { return this; };
        moment.fn.toUserInterface = function () { return this; };
        moment.smartNow = function () { return moment(); };
    }
}

function areEqual<T>(a: T, b: T, field: (value: T) => any) {
    if (a == null)
        return b == null;

    if (b == null)
        return false;

    return field(a) == field(b);
} 