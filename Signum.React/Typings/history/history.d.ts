// Type definitions for history 1.13.1
// Project: https://github.com/rackt/history
// Definitions by: Olmo del Corral <https://github.com/olmobrutall>
// Definitions: https://github.com/borisyankov/DefinitelyTyped

///<reference path='../react/react.d.ts' />

declare module ReactHistory {
    import React = __React;

    export interface ReactLocation extends Location {
        query?: ParsedQuery;
        state?: any;
        action?: string; //"PUSH", "REPLACE" or "POP"
        key?: string;
    }

    export interface ParsedQuery {
        [name: string]: any
    }
    
    export interface History {
        listen(callback: (locaton: ReactLocation) => void): void;
        listenBefore(callback: (locaton: ReactLocation) => string | boolean): void;
        listenBefore(callback: (locaton: ReactLocation, continuation: (result: string | boolean) => void) => void): void;
        listenBeforeUnload(callback: () => string);

        pushState(state: any, path: string);
        pushState(state: any, path: string, query: ParsedQuery);
        push(path: string, state?: any);
        push(path: string, query: ParsedQuery, state?: any);
        replaceState(state: any, path: string);
        replaceState(state: any, path: string, query: ParsedQuery);
        replace(path: string, state?: any);
        replace(path: string, query: ParsedQuery, state?: any);

        go(n: number);
        goBack();
        goForward();

        createKey(): string;
        createHref(path: string): string;
        createPath(path: string): string; 
        createPath(path: string, query: ParsedQuery): string; 
        createLocation(path: string): string;
        createLocation(path: string, state?: any): ReactLocation;
    }

    export interface HistoryOptions {
        //for query
        parseQueryString?: (queryString: string) => ParsedQuery;
        stringifyQuery?: (query: ParsedQuery) => string;

        //for basename
        basename: string;

    }

    export function createLocation(href: string, state?: any): ReactLocation;

    export function createHistory(options?: HistoryOptions): History;
    export function createHashHistory(options?: HistoryOptions): History;
    export function createMemoryHistory(options?: HistoryOptions): History;

    export function useBoforeUnload(baseCreateHistory: (options?: HistoryOptions) => History): (options?: HistoryOptions) => History;
    export function useQueries(baseCreateHistory: (options?: HistoryOptions) => History): (options?: HistoryOptions) => History;
    export function useBasename(baseCreateHistory: (options?: HistoryOptions) => History): (options?: HistoryOptions) => History;
}

declare module "history" {
    export = ReactHistory;
}
