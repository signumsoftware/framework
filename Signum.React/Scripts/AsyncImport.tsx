import * as React from "react";
import { match, Route, RouterChildContext, matchPath } from "react-router-dom";
import * as H from "history";
import * as PropTypes from "prop-types";

export interface ComponentModule {
    default: React.ComponentClass<any>;
}

interface ImportComponentProps {
    onImportModule: () => Promise<ComponentModule>;
    componentProps?: any;
    onRender?: (module: ComponentModule) => React.ReactElement<any>;
}

interface ImportComponentState {
    module?: ComponentModule;
}

export class ImportComponent extends React.Component<ImportComponentProps, ImportComponentState> {

    constructor(props: ImportComponentProps) {
        super(props);
        this.state = { module: undefined };
    }

    componentWillMount() {
        this.importModule(this.props);
    }

    _isMounted = true;

    componentWillUnmount() {
        this._isMounted = false;
    }

    componentWillReceiveProps(newProps: ImportComponentProps) {
        if (newProps.onImportModule != this.props.onImportModule &&
            newProps.onImportModule.toString() != this.props.onImportModule.toString()) {
            this.setState({ module: undefined },
                () => this.importModule(newProps));
        }
    }

    requestIndex = 0;
    importModule(props: ImportComponentProps) {
        this.requestIndex++;
        var currentIndex = this.requestIndex;
        this.props.onImportModule()
            .then(mod => this._isMounted && this.requestIndex == currentIndex && this.setState({ module: mod }))
            .done();
    }

    render() {
        if (!this.state.module)
            return null;

        if (this.props.onRender)
            return this.props.onRender(this.state.module);

        return React.createElement(this.state.module.default, this.props.componentProps);
    }
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
