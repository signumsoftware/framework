import * as React from "react";
import { match, Route, RouterChildContext, matchPath, RouteProps, Switch } from "react-router";
import * as H from "history";
import * as PropTypes from "prop-types";
import * as Navigator from "./Navigator";

//monkey-patching prototypes to make use of  this is implemented: https://github.com/ReactTraining/react-router/issues/5000

function fixBaseName<T>(baseFunction: (location: H.LocationDescriptorObject | string, state?: any) => T): (location: H.LocationDescriptorObject | string, state?: any) => T {
    return (location, state) => {
        if (typeof location === "string") {
            return baseFunction(Navigator.toAbsoluteUrl(location), state);
        } else {
            location!.pathname = Navigator.toAbsoluteUrl(location!.pathname!);
            return baseFunction(location, state);
        }
    };
}

export function useAppRelativeBasename(history: H.History) {
    history.push = fixBaseName(history.push as any) as any;
    history.replace = fixBaseName(history.replace as any) as any;
    history.createHref = fixBaseName(history.createHref as any) as any;
}

export function useAppRelativeComputeMatch(RouteClass: typeof Route) {
    var baseMatch: Function = (RouteClass.prototype as any).computeMatch;

    (RouteClass.prototype as any).computeMatch = function computeMatch(this: Route, props: RouteProps, context: RouterChildContext<any>) {
        let { path, ...p } = props;

        const newPath = path && Navigator.toAbsoluteUrl(path);

        return baseMatch.call(this, { path: newPath, ...p }, context);
    };
}

export function useAppRelativeSwitch(SwitchClass: typeof Switch) {

    Switch.prototype.render = function render(this: Switch) {
        const { route } = this.context.router
        const { children } = this.props
        const location = this.props.location || route.location

        let match: match<any> | undefined;
        let child: React.ReactElement<any> | undefined;
        React.Children.forEach(children, (c) => {
            let element = c as React.ReactElement<RouteProps & { from?: string }>;

            if (!React.isValidElement(element))
                return;

            const { path: pathProp, exact, strict, from } = element.props;
            const path = pathProp || from

            if (match == null) {
                child = element
                match = path ? matchPath(location.pathname, { path: Navigator.toAbsoluteUrl(path), exact, strict }) : route.match
            }
        });
        
        return match && child ? React.cloneElement(child, { location, computedMatch: match }) : null
    };
}
