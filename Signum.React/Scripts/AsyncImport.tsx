import * as React from "react";
import { PathRouteProps, Route } from "react-router-dom";
import * as H from 'history';
import * as PropTypes from "prop-types";
import { useAPI } from "./Hooks";

export interface ComponentModule {
  default: React.ComponentType<any>;
}

interface ImportComponentProps {
  onImport: () => Promise<ComponentModule>;
  componentProps?: any;
}

export function ImportComponent({ onImport, componentProps }: ImportComponentProps) {
  const module = useAPI(() => onImport(), [onImport.toString()]);

  if (!module)
    return null;

  return React.createElement(module.default, componentProps);
}

//interface ImportRouteProps extends PathRouteProps {
  
//  onImportModule: () => Promise<ComponentModule>;
//}

//export function GetImportRoute({ onImportModule, ...rest }: ImportRouteProps) {
//  return (
//    <Route {...rest} element={<ImportComponent onImportModule={onImportModule} />}/>
//  );
//}
 
