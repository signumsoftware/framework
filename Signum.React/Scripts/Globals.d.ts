declare function require<T>(path: string): T;
declare function require<T>(paths: string[], callback: (...modules: any[]) => void): void;

declare interface Promise<T> {
    done(this: Promise<T>): void;
}

declare interface Window {
    __baseUrl: string;
    dataForChildWindow?: any;
}

interface Array<T> {
    groupBy(this: Array<T>, keySelector: (element: T) => string): { key: string; elements: T[] }[];
    groupToObject(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T[] };
    groupWhen(this: Array<T>, condition: (element: T) => boolean): { key: T, elements: T[]}[];
    groupWhenChange(this: Array<T>, keySelector: (element: T) => string): { key: string, elements: T[]}[];
    orderBy<V>(this: Array<T>, keySelector: (element: T) => V): T[];
    orderByDescending<V>(this: Array<T>, keySelector: (element: T) => V): T[];
    withMin<V>(this: Array<T>, keySelector: (element: T) => V): T | undefined;
    withMax<V>(this: Array<T>, keySelector: (element: T) => V): T | undefined;
    toObject(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T };
    toObject<V>(this: Array<T>, keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    toObjectDistinct(this: Array<T>, keySelector: (element: T) => string): { [key: string]: T };
    toObjectDistinct<V>(this: Array<T>, keySelector: (element: T) => string, valueSelector: (element: T) => V): { [key: string]: V };
    distinctBy(this: Array<T>, keySelector?: (element: T) => string): T[];
    flatMap<R>(this: Array<T>, selector: (element: T, index: number, array: T[]) => R[]): R[];
    clear(this: Array<T>): void;
    groupsOf(this: Array<T>, groupSize: number, elementSize?: (item: T) => number): T[][];
    max(this: Array<T>): T;
    max<V>(this: Array<T>, selector: (element: T, index: number, array: T[]) => V): V;
    min(this: Array<T>): T;
    min<V>(this: Array<T>, selector: (element: T, index: number, array: T[]) => V): V;
    sum(this: Array<number>): number;
    sum(this: Array<T>, selector: (element: T, index: number, array: T[]) => number): number;

    first(this: Array<T>, errorContext?: string): T;
    first(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => boolean): T;

    firstOrNull(this: Array<T>): T | null;
    firstOrNull(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => boolean): T | null;

    last(this: Array<T>, errorContext?: string): T;
    last(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => boolean): T;

    lastOrNull(this: Array<T>, ): T | null;
    lastOrNull(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => boolean): T | null;

    single(this: Array<T>, errorContext?: string): T;
    single(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => boolean): T;

    singleOrNull(this: Array<T>, errorContext?: string): T | null;
    singleOrNull(this: Array<T>, predicate?: (element: T, index: number, array: T[]) => boolean): T | null;

    contains(this: Array<T>, element: T): boolean;
    remove(this: Array<T>, element: T): boolean;
    removeAt(this: Array<T>, index: number): void;
    moveUp(this: Array<T>, index: number): number;
    moveDown(this: Array<T>, index: number): number;
    insertAt(this: Array<T>, index: number, element: T): void;
    clone(this: Array<T>, ): T[];
    joinComma(this: Array<T>, lastSeparator: string): string;
    extract(this: Array<T>, filter: (element: T) => boolean): T[];
    findIndex(this: Array<T>, filter: (element: T, index: number, obj: Array<T>) => boolean): number;
    findLastIndex(this: Array<T>, filter: (element: T) => boolean): number;
}

interface ArrayConstructor {

    range(min: number, maxNotIncluded: number): number[];
    toArray<T>(arrayish: { length: number; [index: number]: T }): T[];
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
    indent(this: string, numChars: number): string;
    after(this: string, separator: string): string;
    before(this: string, separator: string): string;
    tryAfter(this: string, separator: string): string | undefined;
    tryBefore(this: string, separator: string): string | undefined;
    afterLast(this: string, separator: string): string;
    beforeLast(this: string, separator: string): string;
    tryAfterLast(this: string, separator: string): string | undefined;
    tryBeforeLast(this: string, separator: string): string | undefined;
    etc(this: string, maxLength: number, etcString?: string): string;

    firstUpper(this: string): string;
    firstLower(this: string, ): string;

    trimEnd(this: string, char?: string): string;
    trimStart(this: string, char?: string): string;

    repeat(this: string, n: number): string;
}

interface FetchAbortController { //Signum patch
    abort?: () => void;
}

interface RequestInit {
    abortController?: FetchAbortController;
}