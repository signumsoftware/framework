/// <reference path="../typings/react/react.d.ts" />
/// <reference path="../typings/react/react-dom.d.ts" />
/// <reference path="../typings/react/react-addons-perf.d.ts" />
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

declare const require: {
    <T>(path: string): T;
    (paths: string[], callback: (...modules: any[]) => void): void;
    ensure: (paths: string[], callback: (require: <T>(path: string) => T) => void) => void;
};

declare interface Promise<T> {
    done(this: Promise<T>): void;
}

declare interface Window {
    __baseUrl: string;
    parentWindowData: any;
}

interface Array<T> {
    groupBy(this: Array<T>, keySelector: (element: T) => string): { key: string; elements: T[] }[];
    groupToObject(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T[] };
    groupWhen(this: Array<T>, condition: (element: T) => boolean): { key: T, elements: T[]}[];
    groupWhenChange(this: Array<T>, keySelector: (element: T) => string): { key: string, elements: T[]}[];
    orderBy<V>(this: Array<T>, keySelector: (element: T) => V): T[];
    orderByDescending<V>(this: Array<T>, keySelector: (element: T) => V): T[];
    toObject(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T };
    toObject<V>(this: Array<T>, keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    toObjectDistinct(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T };
    toObjectDistinct<V>(this: Array<T>, keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    flatMap<R>(this: Array<T>, selector: (element: T, index: number, array: T[]) => R[]): R[];
    clear(this: Array<T>): void;
    groupsOf(this: Array<T>, groupSize: number, elementSize?: (item: T) => number): T[][];
    max(this: Array<T>): T;
    min(this: Array<T>): T;
    sum(this: Array<number>): number;
    first(this: Array<T>, errorContext?: string): T;
    firstOrNull(this: Array<T>, ): T | null;
    last(this: Array<T>, errorContext?: string): T;
    lastOrNull(this: Array<T>, ): T | null;
    single(this: Array<T>, errorContext?: string): T;
    singleOrNull(this: Array<T>, errorContext?: string): T | null;
    contains(this: Array<T>, element: T): boolean;
    remove(this: Array<T>, element: T): boolean;
    removeAt(this: Array<T>, index: number): void;
    insertAt(this: Array<T>, index: number, element: T): void;
    clone(this: Array<T>, ): T[];
    joinComma(this: Array<T>, lastSeparator: string): string;
    extract(this: Array<T>, filter: (element: T) => boolean): T[];
}

interface ArrayConstructor {

    range(min: number, maxNotIncluded: number): number[];
    repeat<T>(count: number, value: T): T[];
}

interface String {
    contains(this: string, str: string): boolean;
    startsWith(this: string, str: string): boolean;
    endsWith(this: string, str: string): boolean;
    formatWith(this: string, ...parameters: any[]): string;
    forGenderAndNumber(this: string, number: number): string;
    forGenderAndNumber(this: string, gender: string | undefined): string;
    forGenderAndNumber(this: string, gender: any , number?: number): string;
    replaceAll(this: string, from: string, to: string): string;
    after(this: string, separator: string): string;
    before(this: string, separator: string): string;
    tryAfter(this: string, separator: string): string | undefined;
    tryBefore(this: string, separator: string): string | undefined;
    afterLast(this: string, separator: string): string;
    beforeLast(this: string, separator: string): string;
    tryAfterLast(this: string, separator: string): string | undefined;
    tryBeforeLast(this: string, separator: string): string | undefined;
    etc(this: string, maxLength: number): string;

    firstUpper(this: string): string;
    firstLower(this: string, ): string;

    trimEnd(this: string, char?: string): string;
    trimStart(this: string, char?: string): string;

    repeat(this: string, n: number): string;
}

declare module moment {
    interface Moment {
        fromUserInterface(this: moment.Moment): Moment;
        toUserInterface(this: moment.Moment): Moment;

    }

    interface MomentStatic {
        smartNow(this: moment.Moment): Moment;
    }
}

declare namespace __React {
    interface Component<P, S> {
        changeState(func: (state: S) => void): void;
    }
}

interface FetchAbortController { //Signum patch
    abort?: () => void;
}

interface RequestInit {
    abortController?: FetchAbortController;
}