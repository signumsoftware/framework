import * as React from "react";
import { match, Route, RouterChildContext, matchPath } from "react-router-dom";
import * as H from "history";


export interface ComponentModule {
    default: React.ComponentClass<any>;
}

interface LoadComponentProps {
    onLoadModule: () => Promise<ComponentModule>;
    componentProps?: any;
    onRender?: (module: ComponentModule) => React.ReactElement<any>;
}

interface LoadComponentState {
    module?: ComponentModule;
}

export class LoadComponent extends React.Component<LoadComponentProps, LoadComponentState> {

    constructor(props: LoadComponentProps) {
        super(props);
        this.state = { module: undefined };
    }

    componentWillMount() {
        this.loadData(this.props);
    }
    
    loadData(props: LoadComponentProps) {
        this.props.onLoadModule()
            .then(mod => this.setState({ module: mod }))
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


interface LoadRouteProps {
    path?: string;
    exact?: boolean;
    strict?: boolean;
    onLoadModule: () => Promise<ComponentModule>;

    location?: H.Location;
    computedMatch?: match<any>; //For Switch component
}

interface LoadRouteState {
    match: match<any> | null;
}

export class LoadRoute extends React.Component<LoadRouteProps, LoadRouteState> {
    
    static contextTypes = {
        router: React.PropTypes.shape({
            history: React.PropTypes.object.isRequired,
            route: React.PropTypes.object.isRequired,
            staticContext: React.PropTypes.object
        })
    }

    static childContextTypes = {
        router: React.PropTypes.object.isRequired
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

    constructor(props: LoadRouteProps) {
        super(props);
        this.state = { match: this.computeMatch(props, this.context) };
    }

    componentWillReceiveProps(nextProps: LoadRouteProps, nextContext: RouterChildContext<any>) {
        this.setState({
            match: this.computeMatch(nextProps, nextContext)
        });
    }


    computeMatch(props: LoadRouteProps, ctx: RouterChildContext<any>): match<any> | null {
        if (props.computedMatch)
            return props.computedMatch // <Switch> already computed the match for us

        const pathname = (props.location || ctx.router.route.location).pathname

        return props.path ? matchPath(pathname, { path: props.path, strict: props.strict, exact: props.exact }) : ctx.router.route.match
    }

    render() {
        if (!this.state.match)
            return null;

        return <LoadComponent onLoadModule={this.props.onLoadModule} componentProps={this.props} />
    }
}
