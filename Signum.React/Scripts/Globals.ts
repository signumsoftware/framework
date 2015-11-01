
function getValues<V>(obj: { [name: string]: V }): V[] {
    var result: V[] = [];

    for (var name in obj) {
        if (obj.hasOwnProperty(name)) {
            result.push(obj[name]);
        }
    }

    return result;
}

interface Array<T> {
    groupByArray(keySelector: (element: T) => string): { key: string; elements: T[] }[];
    groupByObject(keySelector: (element: T) => string): { [key: string]: T[] };
    orderBy<V>(keySelector: (element: T) => V): T[];
    orderByDescending<V>(keySelector: (element: T) => V): T[];
    toObject(keySelector: (element: T) => string): { [key: string]: T };
    toObjectDistinct(keySelector: (element: T) => string): { [key: string]: T };
    flatMap<R>(selector: (element: T) => R[]): R[];
    max(): T;
    min(): T;
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

Array.prototype.toObject = function (keySelector: (element: any) => any): any {
    var obj = {};

    (<Array<any>>this).forEach(item=> {
        var key = keySelector(item);

        if (obj[key])
            throw new Error("Repeated key {0}".format(key));

        obj[key] = item;
    });

    return obj;
};

Array.prototype.toObjectDistinct = function (keySelector: (element: any) => any): any {
    var obj = {};

    (<Array<any>>this).forEach(item=> {
        var key = keySelector(item);

        obj[key] = item;
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


