import * as React from "react";
import { Route, RouteProps, match } from "react-router-dom";
import * as H from "history";
import * as PropTypes from "prop-types";
import { RouteChildrenProps } from "react-router";
import { useAPI } from "./Hooks";

export interface ComponentModule {
  default: React.ComponentType<any>;
}

interface ImportComponentProps {
  onImportModule: () => Promise<ComponentModule>;
  componentProps?: any;
}

export function ImportComponent({ onImportModule, componentProps }: ImportComponentProps) {
  const module = useAPI(() => onImportModule(), [onImportModule.toString()]);

  if (!module)
    return null;

  return React.createElement(module.default, componentProps);
}

interface ImportRouteProps {
  path?: string | string[];
  exact?: boolean;
  sensitive?: boolean;
  strict?: boolean;
  location?: H.Location<any>;

  onImportModule: () => Promise<ComponentModule>;

  computedMatch?: match<any>; //For Switch component
}

export function ImportRoute({ onImportModule, ...rest }: ImportRouteProps) {
  return (
    <Route {...rest}>
      {(props: RouteChildrenProps<any>) => <ImportComponent onImportModule={onImportModule} componentProps={props} />}
    </Route>
  );
}
 
