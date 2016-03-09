// Type definitions for history v2.0.0
// Project: https://github.com/rackt/history
// Definitions by: Sergey Buturlakin <https://github.com/sergey-buturlakin>, Nathan Brown <https://github.com/ngbrown>
// Definitions: https://github.com/borisyankov/DefinitelyTyped


declare namespace HistoryModule {

    // types based on https://github.com/rackt/history/blob/master/docs/Terms.md

    type Action = string

    type BeforeUnloadHook = () => string | boolean

    type CreateHistory<T> = (options?: HistoryOptions) => T

    type CreateHistoryEnhancer<T, E> = (createHistory: CreateHistory<T>) => CreateHistory<T & E>

    interface History {
        listenBefore(hook: TransitionHook): () => void
        listen(listener: LocationListener): () => void
        transitionTo(location: Location): void
        push(path: LocationDescriptorObject): void
        push(path: string): void
        replace(path: LocationDescriptorObject): void
        replace(path: string): void
        go(n: number): void
        goBack(): void
        goForward(): void
        createKey(): LocationKey
        createPath(path: LocationDescriptorObject): Path
        createPath(path: string): Path
        createHref(path: LocationDescriptorObject): Href
        createHref(path: string): Href
        createLocation(path?: LocationDescriptorObject, action?: Action, key?: LocationKey): Location
        createLocation(path?: string, action?: Action, key?: LocationKey): Location

        /** @deprecated use a location descriptor instead */
        createLocation(path?: Path, state?: LocationState, action?: Action, key?: LocationKey): Location
        /** @deprecated use location.key to save state instead */
        pushState(state: LocationState, path: Path): void
        /** @deprecated use location.key to save state instead */
        replaceState(state: LocationState, path: Path): void
        /** @deprecated use location.key to save state instead */
        setState(state: LocationState): void
        /** @deprecated use listenBefore instead */
        registerTransitionHook(hook: TransitionHook): void
        /** @deprecated use the callback returned from listenBefore instead */
        unregisterTransitionHook(hook: TransitionHook): void
    }

    type HistoryOptions = {
        getCurrentLocation?: () => Location
        finishTransition?: (nextLocation: Location) => boolean
        saveState?: (key: LocationKey, state: LocationState) => void
        go?: (n: number) => void
        getUserConfirmation?: (message: string, callback: (result: boolean) => void) => void
        keyLength?: number
        queryKey?: string | boolean
        stringifyQuery?: (obj: any) => string
        parseQueryString?: (str: string) => any
        basename?: string
        entries?: string | [any]
        current?: number
    }

    type Href = string

    type Location = {
        pathname: Pathname
        search: Search
        query: Query
        state: LocationState
        action: Action
        key: LocationKey
        basename?: string
    }

    type LocationDescriptorObject = {
        pathname?: Pathname
        search?: Search
        query?: Query
        state?: LocationState
    }
    
    type LocationKey = string

    type LocationListener = (location: Location) => void

    type LocationState = Object

    type Path = string // Pathname + QueryString

    type Pathname = string

    type Query = Object

    type QueryString = string

    type Search = string

    type TransitionHook = (location: Location, callback: (result: any) => void) => any


    interface HistoryBeforeUnload {
        listenBeforeUnload(hook: BeforeUnloadHook): () => void
    }
  

    // Global usage, without modules, needs the small trick, because lib.d.ts
    // already has `history` and `History` global definitions:
    // var createHistory = ((window as any).History as HistoryModule.Module).createHistory;
    interface Module {
        createHistory: CreateHistory<History>
        createHashHistory: CreateHistory<History>
        createMemoryHistory: CreateHistory<History>
        createLocation(path?: Path, state?: LocationState, action?: Action, key?: LocationKey): Location
        useBasename<T>(createHistory: CreateHistory<T>): CreateHistory<T>
        useBeforeUnload<T>(createHistory: CreateHistory<T>): CreateHistory<T & HistoryBeforeUnload>
        useQueries<T>(createHistory: CreateHistory<T>): CreateHistory<T>
        actions: {
            PUSH: string
            REPLACE: string
            POP: string
        }
    }

}


declare module "history/lib/createBrowserHistory" {

    export default function createBrowserHistory(options?: HistoryModule.HistoryOptions): HistoryModule.History

}


declare module "history/lib/createHashHistory" {

    export default function createHashHistory(options?: HistoryModule.HistoryOptions): HistoryModule.History

}


declare module "history/lib/createMemoryHistory" {

    export default function createMemoryHistory(options?: HistoryModule.HistoryOptions): HistoryModule.History

}


declare module "history/lib/createLocation" {

    export default function createLocation(path?: HistoryModule.Path, state?: HistoryModule.LocationState, action?: HistoryModule.Action, key?: HistoryModule.LocationKey): HistoryModule.Location

}


declare module "history/lib/useBasename" {

    export default function useBasename<T>(createHistory: HistoryModule.CreateHistory<T>): HistoryModule.CreateHistory<T>

}


declare module "history/lib/useBeforeUnload" {

    export default function useBeforeUnload<T>(createHistory: HistoryModule.CreateHistory<T>): HistoryModule.CreateHistory<T & HistoryModule.HistoryBeforeUnload>

}


declare module "history/lib/useQueries" {

    export default function useQueries<T>(createHistory: HistoryModule.CreateHistory<T>): HistoryModule.CreateHistory<T>

}


declare module "history/lib/actions" {

    export const PUSH: string

    export const REPLACE: string

    export const POP: string

    export default {
        PUSH,
        REPLACE,
        POP
    }

}

declare module "history/lib/DOMUtils" {
    export function addEventListener(node: EventTarget, event: string, listener: EventListenerOrEventListenerObject): void;
    export function removeEventListener(node: EventTarget, event: string, listener: EventListenerOrEventListenerObject): void;
    export function getHashPath(): string;
    export function replaceHashPath(path: string): void;
    export function getWindowPath(): string;
    export function go(n: number): void;
    export function getUserConfirmation(message: string, callback: (result: boolean) => void): void;
    export function supportsHistory(): boolean;
    export function supportsGoWithoutReloadUsingHash(): boolean;
}


declare module "history" {

    export { default as createHistory } from "history/lib/createBrowserHistory"

    export { default as createHashHistory } from "history/lib/createHashHistory"

    export { default as createMemoryHistory } from "history/lib/createMemoryHistory"

    export { default as createLocation } from "history/lib/createLocation"

    export { default as useBasename } from "history/lib/useBasename"

    export { default as useBeforeUnload } from "history/lib/useBeforeUnload"

    export { default as useQueries } from "history/lib/useQueries"

    import * as Actions from "history/lib/actions"

    export { Actions }

}
