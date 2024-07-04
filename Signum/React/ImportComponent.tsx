import * as React from "react";
import { PathRouteProps, Route } from "react-router-dom";
import * as PropTypes from "prop-types";
import { useAPI } from "./Hooks";

interface ImportComponentProps {
  onImport: () => Promise<{ default: React.ComponentType<any>; }>;
  componentProps?: any;
}

export function ImportComponent({ onImport, componentProps }: ImportComponentProps): React.ReactElement | null {
  const module = useAPI(() => onImport(), [onImport.toString()]);

  if (!module)
    return null;

  return React.createElement(module.default, componentProps);
}
