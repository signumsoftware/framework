import * as React from "react";
import { Route, RouteProps, match } from "react-router-dom";
import * as H from "history";
import * as PropTypes from "prop-types";

export interface ComponentModule {
  default: React.ComponentClass<any> | React.FunctionComponent<any>;
}

interface ImportComponentProps {
  onImportModule: () => Promise<ComponentModule>;
  componentProps?: any;
  onRender?: (module: ComponentModule) => React.ReactElement<any>;
}

export function ImportComponent({ onImportModule, componentProps, onRender }: ImportComponentProps) {
  const [module, setModule] = React.useState<ComponentModule | undefined>(undefined);

  React.useEffect(() => {
    var controller = new AbortController();
    onImportModule()
      .then(mod => !controller.signal.aborted && setModule(mod))
      .done();
    return () => controller.abort();
  }, [onImportModule.toString()]);

  if (!module)
    return null;

  if (onRender)
    return onRender(module);

  return React.createElement(module.default, componentProps);
}

interface ImportRouteProps {
  path?: string | string[];
  exact?: boolean;
  sensitive?: boolean;
  strict?: boolean;
  location?: H.Location;

  onImportModule: () => Promise<ComponentModule>;

  computedMatch?: match<any>; //For Switch component
}

export function ImportRoute({ onImportModule, ...rest }: ImportRouteProps) {
  return (
    <Route {...rest}>
      <ImportComponent onImportModule={onImportModule} />
    </Route>
  );
 }
 
