import * as React from "react";
import { match, Route, RouterChildContext, matchPath, RouteProps, Switch, __RouterContext } from "react-router";
import * as H from "history";
import * as AppContext from './AppContext'
import * as Navigator from "./Navigator";

//monkey-patching prototypes to make use of  this is implemented: https://github.com/ReactTraining/react-router/issues/5000

function fixBaseName<T>(baseFunction: (location: H.LocationDescriptorObject | string, state?: any) => T): (location: H.LocationDescriptorObject | string, state?: any) => T {
  return (location, state) => {
    if (typeof location === "string") {
      return baseFunction(AppContext.toAbsoluteUrl(location), state);
    } else {
      location!.pathname = AppContext.toAbsoluteUrl(location!.pathname!);
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

    const newPath = path && AppContext.toAbsoluteUrl(path as string);

    return baseMatch.call(this, { path: newPath, ...p }, context);
  };
}

export function useAppRelativeSwitch(SwitchClass: typeof Switch) {

  Switch.prototype.render = function render(this: Switch) {

    return (
      <__RouterContext.Consumer>
        {context => {
          if (!context)
            throw new Error("You should not use <Switch> outside a <Router>");

          const location = this.props.location ?? context.location;

          let element: React.ReactElement<any> | undefined;
          let match: match<any> | undefined | null;

          // We use React.Children.forEach instead of React.Children.toArray().find()
          // here because toArray adds keys to all child elements and we do not want
          // to trigger an unmount/remount for two <Route>s that render the same
          // component at different URLs.
          React.Children.forEach(this.props.children, child => {
            if (match == null && React.isValidElement(child)) {
              element = child;

              const path = child.props.path ?? child.props.from;

              match = path
                ? matchPath(location.pathname, { ...child.props, path: AppContext.toAbsoluteUrl(path as string) })
                : context.match;
            }
          });

          return match
            ? React.cloneElement(element!, { location, computedMatch: match })
            : null;
        }}
      </__RouterContext.Consumer>
    );
  };
}
