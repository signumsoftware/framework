// Type definitions for React Router 0.13.3
// Project: https://github.com/rackt/react-router
// Definitions by: Yuichi Murata <https://github.com/mrk21>, Václav Ostrožlík <https://github.com/vasek17>
// Definitions: https://github.com/borisyankov/DefinitelyTyped

///<reference path='../react/react.d.ts' />
/// <reference path="../history/history.d.ts" />

declare module ReactRouter {
    import React = __React;

    //
    // Transition
    // ----------------------------------------------------------------------
    interface Transition {
        path: string;
        abortReason: any;
        retry(): void;
        abort(reason?: any): void;
        redirect(to: string, params?: {}, query?: {}): void;
        cancel(): void;
        from: (transition: Transition, routes: Route[], components?: React.ReactElement<any>[], callback?: (error?: any) => void) => void;
        to: (transition: Transition, routes: Route[], params?: {}, query?: {}, callback?: (error?: any) => void) => void;
    }

    interface TransitionStaticLifecycle {
        willTransitionTo?(
            transition: Transition,
            params: {},
            query: {},
            callback: Function
        ): void;

        willTransitionFrom?(
            transition: Transition,
            component: React.ReactElement<any>,
            callback: Function
        ): void;
    }

    //
    // Route Configuration
    // ----------------------------------------------------------------------
    // IndexRoute
    interface IndexRouteProp {
        component?: React.ComponentClass<any>;
        components?: { [name: string]: React.ComponentClass<any> };
        getComponent?: (location: string, callback: (error: string, component: React.ComponentClass<any>) => void) => void;
        getComponents?: (location: string, callback: (error: string, components: { [name: string]: React.ComponentClass<any> }) => void) => void;
        onEnter?: (nextState: string, replaceState: string, callback: Function) => void;
        onLeave?: () => void;
    }
    interface IndexRoute extends React.ReactElement<IndexRouteProp> {}
    interface IndexRouteClass extends React.ComponentClass<IndexRouteProp> { }

    // Route
    interface RouteProp extends IndexRouteProp{
        path?: string;
    }
    interface Route extends React.ReactElement<RouteProp> { }
    interface RouteClass extends React.ComponentClass<RouteProp> { }

    // IndexRedirect
    interface IndexRedirectProp {
        to?: string;
        query?: string;
    }
    interface IndexRedirect extends React.ReactElement<IndexRedirectProp> { }
    interface IndexRedirectClass extends React.ComponentClass<IndexRedirectProp> { }


    // Redirect
    interface RedirectProp extends IndexRedirectProp {
        from?: string;
    }
    interface Redirect extends React.ReactElement<RedirectProp> { }
    interface RedirectClass extends React.ComponentClass<RedirectProp> { }

    var IndexRoute: IndexRouteClass;
    var Route: RouteClass;
    var IndexRedirect: IndexRedirectClass;
    var Redirect: RedirectClass;
  

    interface RouterProp {
        routes?: Route[];
        children?: Route[];
        history?: ReactHistory.History;
        createElement?: (component: React.ComponentClass<any>, props: any) => React.Component<any, any>;
    }

    interface Router extends React.ReactElement<RouterProp> { }
    interface RouterClass extends React.ComponentClass<RouterProp> { }


    var Router: RouterClass;

    interface CreateRouteOptions {
        name?: string;
        path?: string;
        ignoreScrollBehavior?: boolean;
        isDefault?: boolean;
        isNotFound?: boolean;
        onEnter?: (transition: Transition, params: {}, query: {}, callback: Function) => void;
        onLeave?: (transition: Transition, wtf: any, callback: Function) => void;
        handler?: Function;
        parentRoute?: Route;
    }

    type CreateRouteCallback = (route: Route) => void;

    function createRoute(callback: CreateRouteCallback): Route;
    function createRoute(options: CreateRouteOptions | string, callback: CreateRouteCallback): Route;
    function createDefaultRoute(options?: CreateRouteOptions | string): Route;
    function createNotFoundRoute(options?: CreateRouteOptions | string): Route;

    interface CreateRedirectOptions extends CreateRouteOptions {
        path?: string;
        from?: string;
        to: string;
        params?: {};
        query?: {};
    }
    function createRedirect(options: CreateRedirectOptions): Redirect;
    function createRoutesFromReactChildren(children: Route): Route[];

    //
    // Components
    // ----------------------------------------------------------------------
    // Link
    interface LinkProp extends React.HTMLAttributes {
        activeClassName?: string;
        activeStyle?: {};
        to: string;
        hash?: string;
        state?: any;
        params?: {};
        query?: {};
    }
    interface Link extends React.ReactElement<LinkProp>, Navigation, State {
        handleClick(event: any): void;
        getHref(): string;
        getClassName(): string;
        getActiveState(): boolean;
    }
    interface LinkClass extends React.ComponentClass<LinkProp> {}

    // RouteHandler


    var Link: LinkClass;

    interface HandlerProps {
        isTransitioning?: boolean;
        location?: {};
        params?: any;
        route?: Route;
        routeParams: any;
        children: any;
    }
    
    //
    // Location
    // ----------------------------------------------------------------------
    interface LocationBase {
        getCurrentPath(): void;
        toString(): string;
    }
    interface Location extends LocationBase {
        push(path: string): void;
        replace(path: string): void;
        pop(): void;
    }

    interface LocationListener {
        addChangeListener(listener: Function): void;
        removeChangeListener(listener: Function): void;
    }

    interface HashLocation extends Location, LocationListener { }
    interface HistoryLocation extends Location, LocationListener { }
    interface RefreshLocation extends Location { }
    interface StaticLocation extends LocationBase { }
    interface TestLocation extends Location, LocationListener { }

    var HashLocation: HashLocation;
    var HistoryLocation: HistoryLocation;
    var RefreshLocation: RefreshLocation;
    var StaticLocation: StaticLocation;
    var TestLocation: TestLocation;


    //
    // Behavior
    // ----------------------------------------------------------------------
    interface ScrollBehaviorBase {
        updateScrollPosition(position: { x: number; y: number; }, actionType: string): void;
    }
    interface ImitateBrowserBehavior extends ScrollBehaviorBase { }
    interface ScrollToTopBehavior extends ScrollBehaviorBase { }

    var ImitateBrowserBehavior: ImitateBrowserBehavior;
    var ScrollToTopBehavior: ScrollToTopBehavior;


    //
    // Mixin
    // ----------------------------------------------------------------------
    interface Navigation {
        makePath(to: string, params?: {}, query?: {}): string;
        makeHref(to: string, params?: {}, query?: {}): string;
        transitionTo(to: string, params?: {}, query?: {}): void;
        replaceWith(to: string, params?: {}, query?: {}): void;
        goBack(): void;
    }

    interface State {
        getPath(): string;
        getRoutes(): Route[];
        getPathname(): string;
        getParams(): {};
        getQuery(): {};
        isActive(to: string, params?: {}, query?: {}): boolean;
    }

    var Navigation: Navigation;
    var State: State;
    

    //
    // Context
    // ----------------------------------------------------------------------
    interface Context {
        makePath(to: string, params?: {}, query?: {}): string;
        makeHref(to: string, params?: {}, query?: {}): string;
        transitionTo(to: string, params?: {}, query?: {}): void;
        replaceWith(to: string, params?: {}, query?: {}): void;
        goBack(): void;

        getCurrentPath(): string;
        getCurrentRoutes(): Route[];
        getCurrentPathname(): string;
        getCurrentParams(): {};
        getCurrentQuery(): {};
        isActive(to: string, params?: {}, query?: {}): boolean;
    }
}

declare module "react-router" {
    export = ReactRouter;
}

declare module __React {

  // for IndexRoute
  function createElement(
    type: ReactRouter.IndexRouteClass,
    props: ReactRouter.IndexRouteProp,
    ...children: __React.ReactNode[]): ReactRouter.IndexRoute;

  // for IndexRedirect
  function createElement(
      type: ReactRouter.IndexRedirectClass,
      props: ReactRouter.IndexRedirectProp,
      ...children: __React.ReactNode[]): ReactRouter.IndexRedirect;

  // for Link
  function createElement(
    type: ReactRouter.LinkClass,
    props: ReactRouter.LinkProp,
    ...children: __React.ReactNode[]): ReactRouter.Link;

  // for Redirect
  function createElement(
    type: ReactRouter.RedirectClass,
    props: ReactRouter.RedirectProp,
    ...children: __React.ReactNode[]): ReactRouter.Redirect;

  // for Route
  function createElement(
    type: ReactRouter.RouteClass,
    props: ReactRouter.RouteProp,
    ...children: __React.ReactNode[]): ReactRouter.Route;
}

declare module "react/addons" {

  // for IndexRoute
  function createElement(
      type: ReactRouter.IndexRouteClass,
      props: ReactRouter.IndexRouteProp,
      ...children: __React.ReactNode[]): ReactRouter.IndexRoute;

  // for IndexRedirect
  function createElement(
      type: ReactRouter.IndexRedirectClass,
      props: ReactRouter.IndexRedirectProp,
      ...children: __React.ReactNode[]): ReactRouter.IndexRedirect;

  // for Link
  function createElement(
    type: ReactRouter.LinkClass,
    props: ReactRouter.LinkProp,
    ...children: __React.ReactNode[]): ReactRouter.Link;

  // for Redirect
  function createElement(
    type: ReactRouter.RedirectClass,
    props: ReactRouter.RedirectProp,
    ...children: __React.ReactNode[]): ReactRouter.Redirect;

  // for Route
  function createElement(
    type: ReactRouter.RouteClass,
    props: ReactRouter.RouteProp,
    ...children: __React.ReactNode[]): ReactRouter.Route;
}
