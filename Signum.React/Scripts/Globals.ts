
Array.prototype.clear = function (): void {
    this.length = 0;
};

Array.prototype.groupBy = function (this:any[], keySelector: (element: any) => string): { key: string; elements: any[] }[] {
    const result: { key: string; elements: any[] }[] = [];
    const objectGrouped = this.groupToObject(keySelector);
    for (const prop in objectGrouped) {
        if (objectGrouped.hasOwnProperty(prop))
            result.push({ key: prop, elements: objectGrouped[prop] });
    }
    return result;
};

Array.prototype.groupToObject = function (this: any[], keySelector: (element: any) => string): { [key: string]: any[] } {
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

Array.prototype.groupWhen = function (this: any[], isGroupKey: (element: any) => boolean, includeKeyInGroup = false, initialGroup = false): {key: any, elements: any[]}[] {
    const result: {key: any, elements: any[]}[] = [];

    let group: { key: any, elements: any[] } | undefined = undefined;

    for (let i = 0; i < this.length; i++) {
        const item: any = this[i];
        if (isGroupKey(item))
        {
            group = { key: item, elements : includeKeyInGroup? [item]: []};
            result.push(group);
        }
        else
        {
            if (group == undefined)
            {
                if(!initialGroup)
                    throw new Error("Parameter initialGroup is false");

                group = { key: undefined, elements : []};
                result.push(group);
            }

            group.elements.push(item);
        }
    }
    return result;
};

Array.prototype.groupWhenChange = function (this: any[], getGroupKey: (element: any) => string): {key: string, elements: any[]}[] {
    const result: {key: any, elements: any[]}[] = [];

    let current: { key: string, elements: any[] } | undefined = undefined;

    for (let i = 0; i < this.length; i++) {
        const item: any = this[i];
        if (current == undefined)
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

    if (current != undefined)
         result.push(current);

    return result;
};

Array.prototype.orderBy = function (this: any[], keySelector: (element: any) => any): any[] {
    const cloned = this.slice(0);
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

Array.prototype.orderByDescending = function (this: any[], keySelector: (element: any) => any): any[] {
    const cloned = this.slice(0);
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

Array.prototype.toObject = function (this: any[], keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
    const obj: any = {};

    this.forEach(item=> {
        const key = keySelector(item);

        if (obj[key])
            throw new Error("Repeated key {0}".formatWith(key));

        obj[key] = valueSelector ? valueSelector(item) : item;
    });

    return obj;
};

Array.prototype.toObjectDistinct = function (this: any[], keySelector: (element: any) => any, valueSelector?: (element: any) => any): any {
    const obj: any = {};

    this.forEach(item=> {
        const key = keySelector(item);

        obj[key] = valueSelector ? valueSelector(item) : item;
    });

    return obj;
};

Array.prototype.flatMap = function (this: any[], selector: (element: any, index: number, array: any[]) => any[]): any {

    const result : any[] = [];
    this.forEach((item, index, array) =>
        selector(item, index, array).forEach(item2 =>
            result.push(item2)
        ));

    return result;
};

Array.prototype.groupsOf = function (this: any[], maxCount: number) {
    

    const result: any[][] = [];
    let newList: any[] = [];

    this.map(item => {
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

Array.prototype.max = function (this: any[]) {
    return Math.max.apply(undefined, this);
};

Array.prototype.min = function (this: any[]) {
    return Math.min.apply(undefined, this);
};


Array.prototype.first = function (this: any[], errorContext: string) {

    if (this.length == 0)
        throw new Error("No " + (errorContext || "element") + " found");

    return this[0];
};


Array.prototype.firstOrNull = function (this: any[]) {

    if (this.length == 0)
        return null;

    return this[0];
};

Array.prototype.last = function (this: any[], errorContext: string) {

    if (this.length == 0)
        throw new Error("No " + (errorContext || "element") + " found");

    return this[this.length - 1];
};


Array.prototype.lastOrNull = function (this: any[]) {

    if (this.length == 0)
        return null;

    return this[this.length - 1];
};

Array.prototype.single = function (this: any[], errorContext: string) {

    if (this.length == 0)
        throw new Error("No " + (errorContext || "element")  + " found");

    if (this.length > 1)
        throw new Error("More than one " + (errorContext || "element")  + " found");

    return this[0];
};

Array.prototype.singleOrNull = function (this: any[], errorContext: string) {

    if (this.length == 0)
        return null;

    if (this.length > 1)
        throw new Error("More than one " + (errorContext || "element")  + " found");

    return this[0];
};

Array.prototype.contains = function (this: any[], element: any) {
    return this.indexOf(element) != -1;
};

Array.prototype.removeAt = function (this: any[], index: number) {
    this.splice(index, 1);
};

Array.prototype.remove = function (this: any[], element: any) {

    const index = this.indexOf(element);
    if (index == -1)
        return false;

    this.splice(index, 1);
    return true;
};

Array.prototype.insertAt = function (this: any[], index: number, element: any) {
    this.splice(index, 0, element);
};

Array.prototype.clone = function (this: any[]) {
    return this.slice(0);
};

Array.prototype.joinComma = function (this: any[], lastSeparator: string) {
    const array = this as any[];

    if (array.length == 0)
        return "";

    if (array.length == 1)
        return array[0] == undefined ? "" : array[0].toString(); 

    const lastIndex = array.length - 1;

    const rest = array.slice(0, lastIndex).join(", ");

    return rest + lastSeparator + (array[lastIndex] == undefined ? "" : array[lastIndex].toString()); 
};

Array.prototype.extract = function (this: any[], predicate: (element: any) => boolean) {
    const result = this.filter(predicate);

    result.forEach(element => { this.remove(element) });

    return result;
};

Array.range = function (min: number, maxNotIncluded: number) {
    const length = maxNotIncluded - min;

    const result = new Array(length);
    for (let i = 0; i < length; i++) {
        result[i] = min + i;
    }

    return result;
}

Array.repeat = function (count: number, val: any) : any[] {
    
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
    const index = this.lastIndexOf(str);
    return index !== -1 && index === (this.length - str.length); //keep it
}

String.prototype.formatWith = function () {
    const regex = /\{([\w-]+)(?:\:([\w\.]*)(?:\((.*?)?\))?)?\}/g;

    const args: any = arguments;

    return (this as string).replace(regex, match => {
        //match will look like {sample-match}
        //key will be 'sample-match';
        const key = match.substr(1, match.length - 2);

        return args[key];
    });
};


String.prototype.forGenderAndNumber = function (this: string, gender: any, number?: number) {

    if (!number && !isNaN(parseFloat(gender))) {
        number = gender;
        gender = undefined;
    }

    if ((gender == undefined || gender == "") && number == undefined)
        return this;

    function replacePart(textToReplace: string, ...prefixes: string[]): string {
        return textToReplace.replace(/\[[^\]\|]+(\|[^\]\|]+)*\]/g, m => {
            const captures = m.substr(1, m.length - 2).split("|");

            for (let i = 0; i < prefixes.length; i++){
                const pr = prefixes[i];
                const capture = captures.filter(c => c.startsWith(pr)).firstOrNull();
                if (capture != undefined)
                    return capture.substr(pr.length);
            }

            return "";
        });
    }
             

    if (number == undefined)
        return replacePart(this, gender + ":");

    if (gender == undefined) {
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


String.prototype.replaceAll = function (this: string, from: string, to: string) {
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
        return undefined;

    return this.substring(0, index);
};

String.prototype.tryAfter = function (separator) {
    const index = this.indexOf(separator);
    if (index == -1)
        return undefined;

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
        return undefined;

    return this.substring(0, index);
};

String.prototype.tryAfterLast = function (separator) {
    const index = this.lastIndexOf(separator);
    if (index == -1)
        return undefined;

    return this.substring(index + separator.length);
};

String.prototype.etc = function (this: string, maxLength: number) {
    let str = this;

    str = str.tryBefore("\n") || str;

    if (str.length > maxLength)
        str = str.substr(0, maxLength - 1) + "…";

    return str;
};

String.prototype.firstUpper = function () {
    return this[0].toUpperCase() + this.substring(1);
};

String.prototype.firstLower = function () {
    return this[0].toLowerCase() + this.substring(1);
};

String.prototype.trimStart = function (char) {
    let result = this;

    if (!char)
        char = " ";

    if (char == "")
        throw new Error("Empty char");


    while (result.startsWith(char))
        result = result.substr(char.length);

    return result;
};

String.prototype.trimEnd = function (char) {
    let result = this;

    if (!char)
        char = " ";

    if (char == "")
        throw new Error("Empty char");

    while (result.endsWith(char))
        result = result.substr(0, result.length - char.length);

    return result;
};

String.prototype.repeat = function (this: string, n: number) {
    let result = ""; 
    for (let i = 0; i < n; i++)
        result += this;
    return result;
};

Promise.prototype.done = function () {
    this.catch(error => setTimeout(() => { throw error; }, 0));
};

export module Dic {

    var simplesTypes = ["number", "boolean", "string"];
    export const skipClasses : Function[] = [];

    export function equals<V>(objA: V, objB: V, deep: boolean, depth = 0, visited : any[] = []) : boolean {

        if (objA === objB)
            return true;
        
        if (objA == null || objB == null)
            return false;

        if (simplesTypes.contains(typeof objA) ||
            simplesTypes.contains(typeof objB))
            return false; 
        
        if (Array.isArray(objA) !== Array.isArray(objB))
            return false;

        if (visited.indexOf(objB) != -1)
            return false;
        visited.push(objB);

        if (visited.indexOf(objA) != -1)
            return false;
        visited.push(objA);

        if (Array.isArray(objA)) {
            var ar = objA as any as any[]; 
            var br = objB as any as any[]; 

            if (ar.length != br.length)
                return false;

            return Array.range(0, ar.length).every(i => equals(ar[i], br[i], deep, depth + 1, visited));
        }

        if (Object.getPrototypeOf(objA) !== Object.getPrototypeOf(objB))
            return false;

        if (skipClasses.some(c => objA instanceof c))
            return false;

        const akeys = Dic.getKeys(objA);
        const bkeys = Dic.getKeys(objB);

        if (akeys.length != bkeys.length)
            return false;

        return akeys.every(k => equals((objA as any)[k], (objB as any)[k], deep, depth + 1, visited));
    }


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
        const objectCopy: any = {};

        for (const key in object) {
            if (object.hasOwnProperty(key)) {
                objectCopy[key] = (object as any)[key];
            }
        }

        return objectCopy as T;
    }

    export function extend<O>(out: O): O;
    export function extend<O, U>(out: O, arg1: U): O & U;
    export function extend<O, U, V>(out: O, arg1: U, arg2: V): O & U & V;
    export function extend<O, U, V>(out: O, ...args: Object[]): any;
    export function extend(out: any) {
        out = out || {};

        for (let i = 1; i < arguments.length; i++) {

            const a = arguments[i];

            if (!a)
                continue;

            for (const key in a) {
                if (a.hasOwnProperty(key))
                    out[key] = a[key];
            }
        }

        return out;
    };

    export function extendUndefined<O>(out: O): O;
    export function extendUndefined<O, U>(out: O, arg1: U): O & U;
    export function extendUndefined<O, U, V>(out: O, arg1: U, arg2: V): O & U & V;
    export function extendUndefined<O, U, V>(out: O, ...args: Object[]): any;
    export function extendUndefined(out: any) {
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
        const result: any = {};

        for (const key in obj) {
            if (!toRemove.hasOwnProperty(key))
                result[key] = (obj as any)[key];
            else
                (toRemove as any)[key] = (obj as any)[key];
        }

        return result as T;
    }
}

export function coalesce<T>(value: T | undefined | null, defaultValue: T): T {
    return value != null ? value : defaultValue;
}

export function classes(...classNames: (string | null | undefined | boolean /*false*/)[]) {
    return classNames.filter(a=> a && a != "").join(" ");
}

export function addClass(props: { className?: string } | null | undefined, newClasses: string) {
    if (!props || !props.className)
        return newClasses;

    return classes(props.className, newClasses)
}


export function combineFunction<F extends Function>(func1: F, func2: F) : F {
    if (!func1)
        return func2;

    if (!func2)
        return func1;

    return function combined(this: any, ...args: any[]) {
        func1.apply(this, args);
        func2.apply(this, args);
    } as any;
}



export function areEqual<T>(a: T | undefined, b: T | undefined, field: (value: T) => any): boolean {
    if (a == undefined)
        return b == undefined;

    if (b == undefined)
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

    export function closest(element: HTMLElement, selector: string, context?: Node): HTMLElement | undefined {
        context = context || document;
        // guard against orphans
        while (!matches(element, selector)) {
            if (element == context)
                return undefined;

            element = element.parentNode as HTMLElement;
        }

        return element;
    }

    export function offsetParent(element: HTMLElement): HTMLElement | undefined {

        const isRelativeOrAbsolute = (str: string | null) => str === "relative" || str === "absolute";

        // guard against orphans
        while (!isRelativeOrAbsolute(window.getComputedStyle(element).position)) {
            if (element.parentNode == document)
                return undefined;

            element = element.parentNode as HTMLElement;
        }

        return element;
    }
}
