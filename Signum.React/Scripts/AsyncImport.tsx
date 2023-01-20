import * as React from "react";
import { PathRouteProps, Route } from "react-router-dom";
import * as H from "history";
import * as PropTypes from "prop-types";
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

interface ImportRouteProps extends PathRouteProps {
  
  onImportModule: () => Promise<ComponentModule>;
}

export function ImportRoute({ onImportModule, ...rest }: ImportRouteProps) {
  return (
    <Route {...rest} element={<ImportComponent onImportModule={onImportModule} />}/>
  );
}
 
