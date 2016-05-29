
Array.prototype.clear = function (): void {
    (this as any[]).length = 0;
};

Array.prototype.groupBy = function (keySelector: (element: any) => string): { key: string; elements: any[] }[] {
    const result: { key: string; elements: any[] }[] = [];
    const objectGrouped = this.groupToObject(keySelector);
    for (const prop in objectGrouped) {
        if (objectGrouped.hasOwnProperty(prop))
            result.push({ key: prop, elements: objectGrouped[prop] });
    }
    return result;
};

Array.prototype.groupToObject = function (keySelector: (element: any) => string): { [key: string]: any[] } {
    const result: { [key: string]: any[] } = {};

    for (let i = 0; i < this.length; i++) {
        const element: any = this[i];
        const key = keySelector(element);
        if (!result[key])
            result[key] = [];
        result[key].push(element);
    }
    return result;
};

Array.prototype.groupWhen = function (isGroupKey: (element: any) => boolean, includeKeyInGroup = false, initialGroup = false): {key: any, elements: any[]}[] {
    const result: {key: any, elements: any[]}[] = [];

    let group: {key: any, elements: any[]} = null;

    for (let i = 0; i < this.length; i++) {
        const item: any = this[i];
        if (isGroupKey(item))
        {
            group = { key: item, elements : includeKeyInGroup? [item]: []};
            result.push(group);
        }
        else
        {
            if (group == null)
            {
                if(!initialGroup)
                    throw new Error("Parameter initialGroup is false");

                group = { key: null, elements : []};
                result.push(group);
            }

            group.elements.push(item);
        }
    }
    return result;
};

Array.prototype.groupWhenChange = function (getGroupKey: (element: any) => string): {key: string, elements: any[]}[] {
    const result: {key: any, elements: any[]}[] = [];

    let current: {key: string, elements: any[]} = null;
    for (let i = 0; i < this.length; i++) {
        const item: any = this[i];
        if (current == null)
        {
            current =  { key: getGroupKey(getGroupKey(item)), elements : [item]};
        }
        else if (current.key == getGroupKey(item))
        {
            current.elements.push(item);
        }
        else
        {
            result.push(current);
            current = { key: getGroupKey(item), elements: [item]};
        }
    }

    if (current != null)
         result.push(current);

    return result;
};

Array.prototype.orderBy = function (keySelector: (element: any) => any): any[] {
    const cloned = (<any[]>this).slice(0);
    cloned.sort((e1, e2) => {
        const v1 = keySelector(e1);
        const v2 = keySelector(e2);
        if (v1 > v2)
            return 1;
        if (v1 < v2)
            return -1;
        return 0;
    });
    return cloned;
};

Array.prototype.orderByDescending = function (keySelector: (element: any) => any): any[] {
    const cloned = (<any[]>this).slice(0);
    cloned.sort((e1, e2) => {
        const v1 = keySelector(e1);
        const v2 = keySelector(e2);
        if (v1 < v2)
            return 1;
        if (v1 > v2)
            return -1;
        return 0;
    });
    return cloned;
};

Array.prototype.toObject = function (keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
    const obj = {};

    (<Array<any>>this).forEach(item=> {
        const key = keySelector(item);

        if (obj[key])
            throw new Error("Repeated key {0}".formatWith(key));


        obj[key] = valueSelector ? valueSelector(item) : item;
    });

    return obj;
};

Array.prototype.toObjectDistinct = function (keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
    const obj = {};

    (<Array<any>>this).forEach(item=> {
        const key = keySelector(item);

        obj[key] = valueSelector ? valueSelector(item) : item;
    });

    return obj;
};

Array.prototype.flatMap = function (selector: (element: any, index: number, array: any[]) => any[]): any {

    const result = [];
    (<Array<any>>this).forEach((item, index, array) =>
        selector(item, index, array).forEach(item2 =>
            result.push(item2)
        ));

    return result;
};

Array.prototype.groupsOf = function (maxCount: number) {

    var array = this as [];

    var result: any[][] = [];
    var newList: any[] = [];

    array.map(item => {
        newList.push(item);
        if (newList.length == maxCount) {
            result.push(newList);
            newList = [];
        }
    });

    if (newList.length != 0)
        result.push(newList);

    return result;

}

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

    const index = (this as Array<any>).indexOf(element);
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

Array.prototype.joinComma = function (lastSeparator: string) {
    const array = this as any[];

    if (array.length == 0)
        return "";

    if (array.length == 1)
        return array[0] == null ? "" : array[0].toString(); 

    const lastIndex = array.length - 1;

    const rest = array.slice(0, lastIndex).join(", ");

    return rest + lastSeparator + (array[lastIndex] == null ? "" : array[lastIndex].toString()); 
};

Array.prototype.extract = function (predicate: (element: any) => boolean) {
    const array = this as any[];
    const result = array.filter(predicate);

    result.forEach(element => { array.remove(element) });

    return result;
};

Array.prototype.notNull = function () {
    return (this as any[]).filter(a => !!a);
};

Array.range = function (min, maxNotIncluded) {

    const length = maxNotIncluded - min;

    const result = new Array(length);
    for (let i = 0; i < length; i++) {
        result[i] = min + i;
    }

    return result;
}

Array.repeat = function (count, val) {
    
    const result = new Array(count);
    for (let i = 0; i < count; i++) {
        result[i] = val;
    }

    return result;
}

String.prototype.contains = function (str) {
    return this.indexOf(str) !== -1;
}

String.prototype.startsWith = function (str) {
    return this.indexOf(str) === 0;
}

String.prototype.endsWith = function (str) {
    var index = this.lastIndexOf(str);
    return index !== -1 && index === (this.length - str.length); //keep it
}

String.prototype.formatWith = function () {
    const regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    const args = arguments;

    return this.replace(regex, function (match) {
        //match will look like {sample-match}
        //key will be 'sample-match';
        const key = match.substr(1, match.length - 2);

        return args[key];
    });
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
            const captures = m.substr(1, m.length - 2).split("|");

            for (let i = 0; i < prefixes.length; i++){
                const pr = prefixes[i];
                const capture = captures.filter(c => c.startsWith(pr)).firstOrNull();
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


export function isNumber(n: any): boolean {
    return !isNaN(parseFloat(n)) && isFinite(n);
}


String.prototype.replaceAll = function (from, to) {
    return this.split(from).join(to)
};

String.prototype.before = function (separator) {
    const index = this.indexOf(separator);
    if (index == -1)
        throw Error("{0} not found".formatWith(separator));

    return this.substring(0, index);
};

String.prototype.after = function (separator) {
    const index = this.indexOf(separator);
    if (index == -1)
        throw Error("{0} not found".formatWith(separator));

    return this.substring(index + separator.length);
};

String.prototype.tryBefore = function (separator) {
    const index = this.indexOf(separator);
    if (index == -1)
        return null;

    return this.substring(0, index);
};

String.prototype.tryAfter = function (separator) {
    const index = this.indexOf(separator);
    if (index == -1)
        return null;

    return this.substring(index + separator.length);
};

String.prototype.beforeLast = function (separator) {
    const index = this.lastIndexOf(separator);
    if (index == -1)
        throw Error("{0} not found".formatWith(separator));

    return this.substring(0, index);
};

String.prototype.afterLast = function (separator) {
    const index = this.lastIndexOf(separator);
    if (index == -1)
        throw Error("{0} not found".formatWith(separator));

    return this.substring(index + separator.length);
};

String.prototype.tryBeforeLast = function (separator) {
    const index = this.lastIndexOf(separator);
    if (index == -1)
        return null;

    return this.substring(0, index);
};

String.prototype.tryAfterLast = function (separator) {
    const index = this.lastIndexOf(separator);
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

String.prototype.trimStart = function (char: string) {
    var result = this as string;
    if (char == "")
        throw new Error("Empty char");

    while (result.startsWith(char))
        result = result.substr(char.length);

    return result;
};

String.prototype.trimEnd = function (char: string) {
    var result = this as string;
    if (char == "")
        throw new Error("Empty char");

    while (result.endsWith(char))
        result = result.substr(0, result.length - char.length);

    return result;
};

String.prototype.repeat = function (n: number) {
    var result = ""; 
    for (var i = 0; i < n; i++)
        result += this;
    return result;
};

String.prototype.replaceAll = function (search, replacement) {
    var target = this;
    return target.split(search).join(replacement);
};

if (typeof String.prototype.trim !== 'function') {
    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/, '');
    }
}

Promise.prototype.done = function () {
    (this as Promise<any>).catch(error => setTimeout(() => { throw error; }, 0));
};

export module Dic {

    export function getValues<V>(obj: { [key: string]: V }): V[] {
        const result: V[] = [];

        for (const name in obj) {
            if (obj.hasOwnProperty(name)) {
                result.push(obj[name]);
            }
        }

        return result;
    }

    export function getKeys(obj: { [key: string]: any }): string[] {
        const result: string[] = [];

        for (const name in obj) {
            if (obj.hasOwnProperty(name)) {
                result.push(name);
            }
        }

        return result;
    }

    export function map<V, R>(obj: { [key: string]: V }, selector: (key: string, value: V, index: number) => R): R[] {
        let index = 0;
        const result: R[] = [];
        for (const name in obj) {
            if (obj.hasOwnProperty(name)) {
                result.push(selector(name, obj[name], index++));
            }
        }
        return result;
    }

    export function foreach<V>(obj: { [key: string]: V }, action: (key: string, value: V) => void) {

        for (const name in obj) {
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
        const objectCopy = <T>{};

        for (const key in object) {
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

        for (let i = 1; i < arguments.length; i++) {

            const a = arguments[i];

            if (!a)
                continue;

            for (const key in a) {
                if (a.hasOwnProperty(key) && a[key] !== undefined)
                    out[key] = a[key];
            }
        }

        return out;
    };

    /**  Waiting for https://github.com/Microsoft/TypeScript/issues/2103 */
    export function without<T>(obj: T, toRemove: {}): T {
        const result = {};

        for (const key in obj) {
            if (!toRemove.hasOwnProperty(key))
                result[key] = obj[key];
            else
                toRemove[key] = obj[key];
        }

        return result as T;
    }
}



export function classes(...classNames: string[]) {
    return classNames.filter(a=> a && a != "").join(" ");
}

export function addClass(props: { className?: string }, newClasses: string) {
    if (!props || !props.className)
        return newClasses;

    return classes(props.className, newClasses)
}


export function combineFunction<F extends Function>(func1: F, func2: F) : F {
    if (!func1)
        return func2;

    if (!func2)
        return func1;

    return function combined(...args) {
        func1.apply(this, args);
        func2.apply(this, args);
    } as any;
}



export function areEqual<T>(a: T, b: T, field: (value: T) => any) {
    if (a == null)
        return b == null;

    if (b == null)
        return false;

    return field(a) == field(b);
}

export function ifError<E extends Error, T>(ErrorClass: { new (...args: any[]): E }, onError: (error: E) => T): (error: any) => T {
    return error => {
        if (error instanceof ErrorClass)
            return onError((error as E));
        throw error;
    };
}

export module DomUtils {
    export function matches(elem: HTMLElement, selector: string): boolean {
        // Vendor-specific implementations of `Element.prototype.matches()`.
        const proto = Element.prototype as any;
        const nativeMatches = proto.matches ||
            proto.webkitMatchesSelector ||
            proto.mozMatchesSelector ||
            proto.msMatchesSelector ||
            proto.oMatchesSelector;

        if (!elem || elem.nodeType !== 1) {
            return false;
        }

        const parentElem = elem.parentNode as HTMLElement;

        // use native 'matches'
        if (nativeMatches) {
            return nativeMatches.call(elem, selector);
        }

        // native support for `matches` is missing and a fallback is required
        const nodes = parentElem.querySelectorAll(selector);
        const len = nodes.length;

        for (let i = 0; i < len; i++) {
            if (nodes[i] === elem) {
                return true;
            }
        }

        return false;
    }

    export function closest(element: HTMLElement, selector: string, context?: Node): HTMLElement {
        context = context || document;
        // guard against orphans
        while (!matches(element, selector)) {
            if (element == context)
                return null;

            element = element.parentNode as HTMLElement;
        }

        return element;
    }

    export function offsetParent(element: HTMLElement): HTMLElement {

        var isRelativeOrAbsolute = (str: string) => str === "relative" || str === "absolute";

        // guard against orphans
        while (!isRelativeOrAbsolute(window.getComputedStyle(element).position)) {
            if (element.parentNode == document)
                return null;

            element = element.parentNode as HTMLElement;
        }

        return element;
    }
}
