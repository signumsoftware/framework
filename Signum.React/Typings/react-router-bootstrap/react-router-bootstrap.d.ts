// Type definitions for React Router 0.19.3
// Project: https://github.com/react-bootstrap/react-router-bootstrap
// Definitions by: Yuichi Murata <https://github.com/mrk21>, Václav Ostrožlík <https://github.com/vasek17>
// Definitions: https://github.com/borisyankov/DefinitelyTyped

///<reference path='../react/react.d.ts' />
/// <reference path="../react-router/history.d.ts" />

declare module ReactRouterBootstrap {
    import React = __React;
    //
    // Components
    // ----------------------------------------------------------------------
    // Link
    interface LinkContainerProps extends React.HTMLAttributes {
        onlyActiveOnIndex?: string;
        activeStyle?: {};
        to: string;
        hash?: string;
        state?: any;
        query?: {};
    }
    interface LinkContainer extends React.ReactElement<LinkContainerProps> {

    }
    interface LinkContainerClass extends React.ComponentClass<LinkContainerProps> {}

    // RouteHandler


    var LinkContainer : LinkContainerClass;
    var IndexLinkContainer: LinkContainerClass;
}

declare module "react-router-bootstrap" {
    export = ReactRouterBootstrap;
}
