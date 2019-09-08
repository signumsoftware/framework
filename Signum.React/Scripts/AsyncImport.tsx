import * as React from "react";
import { match, RouterChildContext, matchPath } from "react-router-dom";
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
  path?: string;
  exact?: boolean;
  strict?: boolean;
  onImportModule: () => Promise<ComponentModule>;

  location?: H.Location;
  computedMatch?: match<any>; //For Switch component
}

interface ImportRouteState {
  match: match<any> | null;
}

export class ImportRoute extends React.Component<ImportRouteProps, ImportRouteState> {

  static contextTypes = {
    router: PropTypes.shape({
      history: PropTypes.object.isRequired,
      route: PropTypes.object.isRequired,
      staticContext: PropTypes.object
    })
  }

  static childContextTypes = {
    router: PropTypes.object.isRequired
  }

  getChildContext() {
    return {
      router: {
        ...this.context.router,
        route: {
          location: this.props.location || this.context.router.route.location,
          match: this.state.match
        }
      }
    }
  }

  constructor(props: ImportRouteProps) {
    super(props);
    this.state = { match: this.computeMatch(props, this.context) };
  }

  componentWillReceiveProps(nextProps: ImportRouteProps, nextContext: RouterChildContext<any>) {
    this.setState({
      match: this.computeMatch(nextProps, nextContext)
    });
  }


  computeMatch(props: ImportRouteProps, context: RouterChildContext<any>): match<any> | null {
    if (props.computedMatch)
      return props.computedMatch // <Switch> already computed the match for us

    const pathname = (props.location || context.router.route.location).pathname

    return props.path ? matchPath(pathname, { path: props.path, strict: props.strict, exact: props.exact }) : context.router.route.match
  }

  render() {
    if (!this.state.match)
      return null;

    return <ImportComponent onImportModule={this.props.onImportModule} componentProps={{ ... this.props, ... this.state }} />
  }
}
