/// <reference path="../typings/react/react.d.ts" />
/// <reference path="../typings/react/react-dom.d.ts" />
/// <reference path="../typings/react-router/react-router.d.ts" />
/// <reference path="../typings/react-router/history.d.ts" />
/// <reference path="../typings/react-bootstrap/react-bootstrap.d.ts" />
/// <reference path="../typings/react-router-bootstrap/react-router-bootstrap.d.ts" />
/// <reference path="../typings/react-widgets/react-widgets.d.ts" />
/// <reference path="../typings/numeraljs/numeraljs.d.ts" />
/// <reference path="../typings/moment/moment.d.ts" />
/// <reference path="../typings/bluebird/bluebird.d.ts" />



declare var require: {
    <T>(path: string): T;
    (paths: string[], callback: (...modules: any[]) => void): void;
    ensure: (paths: string[], callback: (require: <T>(path: string) => T) => void) => void;
};


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
    joinComma(lastSeparator: string);
}



interface ArrayConstructor {

    range(min: number, max: number): number[];
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

declare module moment {
    interface Moment {
        fromUserInterface(): Moment;
        toUserInterface(): Moment;

    }

    interface MomentStatic {
        smartNow();
    }
}
