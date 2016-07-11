/// <reference path="../typings/react/react.d.ts" />
/// <reference path="../typings/react/react-dom.d.ts" />
/// <reference path="../typings/react/react-addons-transition-group.d.ts" />
/// <reference path="../typings/react/react-addons-css-transition-group.d.ts" />
/// <reference path="../typings/react-router/react-router.d.ts" />
/// <reference path="../typings/react-router/history.d.ts" />
/// <reference path="../typings/react-bootstrap/react-bootstrap.d.ts" />
/// <reference path="../typings/react-router-bootstrap/react-router-bootstrap.d.ts" />
/// <reference path="../typings/react-widgets/react-widgets.d.ts" />
/// <reference path="../typings/numbro/numbro.d.ts" />
/// <reference path="../typings/moment/moment.d.ts" />
/// <reference path="../typings/moment-duration-format/moment-duration-format.d.ts"/>
/// <reference path="../typings/es6-promise/es6-promise.d.ts" />

declare var require: {
    <T>(path: string): T;
    (paths: string[], callback: (...modules: any[]) => void): void;
    ensure: (paths: string[], callback: (require: <T>(path: string) => T) => void) => void;
};

declare interface Promise<T> {
    done(): void;
}

interface Array<T> {
    groupBy(keySelector: (element: T) => string): { key: string; elements: T[] }[];
    groupToObject(keySelector: (element: T) => string): { [key: string]: T[] };
    groupWhen(condition: (element: T) => boolean): { key: T, elements: T[]}[];
    groupWhenChange(keySelector: (element: T) => string): { key: string, elements: T[]}[];
    orderBy<V>(keySelector: (element: T) => V): T[];
    orderByDescending<V>(keySelector: (element: T) => V): T[];
    toObject(keySelector: (element: T) => string): { [key: string]: T };
    toObject<V>(keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    toObjectDistinct(keySelector: (element: T) => string): { [key: string]: T };
    toObjectDistinct<V>(keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    flatMap<R>(selector: (element: T, index: number, array: T[]) => R[]): R[];
    clear(): void;
    groupsOf(maxCount: number): T[][];
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
    removeAt(index: number): void;
    insertAt(index: number, element: T): void;
    clone(): T[];
    joinComma(lastSeparator: string): string;
    extract(filter: (element: T) => boolean): T[]; 
    notNull(): T[]; 
}

interface ArrayConstructor {

    range(min: number, max: number): number[];
    repeat<T>(count: number, value: T): T[];
}

interface String {
    contains(str: string): boolean;
    startsWith(str: string): boolean;
    endsWith(str: string): boolean;
    formatWith(...parameters: any[]): string;
    forGenderAndNumber(number: number): string;
    forGenderAndNumber(gender: string): string;
    forGenderAndNumber(gender: any, number: number): string;
    replaceAll(from: string, to: string): string;
    after(separator: string): string;
    before(separator: string): string;
    tryAfter(separator: string): string;
    tryBefore(separator: string): string;
    afterLast(separator: string): string;
    beforeLast(separator: string): string;
    tryAfterLast(separator: string): string;
    tryBeforeLast(separator: string): string;
    etc(maxLength: number): string;

    replaceAll(search: string, replacement: string): string;
    firstUpper(): string;
    firstLower(): string;

    trimEnd(char?: string): string;
    trimStart(char?: string): string;

    repeat(n: number): string;
}

declare module moment {
    interface Moment {
        fromUserInterface(): Moment;
        toUserInterface(): Moment;

    }

    interface MomentStatic {
        smartNow(): Moment;
    }
}
