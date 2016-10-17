
import * as React from 'react'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import ButtonBar from './ButtonBar'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, JavascriptMessage, entityInfo } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos, GraphExplorer, parseId } from '../Reflection'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from './Widgets'
import ValidationErrors from './ValidationErrors'

require("!style!css!./Frames.css");

interface PageFrameProps extends ReactRouter.RouteComponentProps<{}, { type: string; id?: string, waitData?: string }> {
}


interface PageFrameState {
    pack?: EntityPack<Entity>;
    getComponent?: (ctx: TypeContext<Entity>) => React.ReactElement<any>;
}

export default class PageFrame extends React.Component<PageFrameProps, PageFrameState> {

    constructor(props: PageFrameProps) {
        super(props);
        this.state = this.calculateState(props);

    }

    componentWillMount() {
        this.load(this.props);
    }

    getTypeInfo(): TypeInfo {
        return getTypeInfo(this.props.routeParams!.type);
    }

    calculateState(props: PageFrameProps) {

        return { componentClass: undefined, pack: undefined } as PageFrameState;
    }


    componentWillReceiveProps(newProps: PageFrameProps) {
        this.setState(this.calculateState(newProps), () => {
            this.load(newProps);
        });
    }

    load(props: PageFrameProps) {
        this.loadEntity(props)
            .then(() => this.loadComponent())
            .done();
    }

    loadEntity(props: PageFrameProps): Promise<void> {

        if (this.props.location.query["waitData"]) {
            if (window.opener.dataForChildWindow == undefined) {
                throw new Error("No dataForChildWindow in parent found!")
            }

            var pack = window.opener.dataForChildWindow;
            window.opener.dataForChildWindow = undefined;
            this.setState({ pack: pack });
            return Promise.resolve<void>();
        }

        const ti = this.getTypeInfo();

        if (this.props.routeParams!.id) {
            
            const lite: Lite<Entity> = {
                EntityType: ti.name,
                id: parseId(ti, props.routeParams!.id!),
            };

            return Navigator.API.fetchEntityPack(lite)
                .then(pack => this.setState({ pack }));

        } else {

            return Constructor.construct(ti.name)
                .then(pack => this.setState({ pack: pack as EntityPack<Entity> }));
        }
    }


    loadComponent(): Promise<void> {
        return Navigator.getViewPromise(this.state.pack!.entity).promise
            .then(c => this.setState({ getComponent: c }));
    }

    onClose() {
        if (Finder.isFindable(this.state.pack!.entity.Type))
            Navigator.currentHistory.push(Finder.findOptionsPath({ queryName: this.state.pack!.entity.Type }));
        else
            Navigator.currentHistory.push("~/");
    }


    entityComponent: React.Component<any, any>;

    setComponent(c: React.Component<any, any>) {
        if (c && this.entityComponent != c) {
            this.entityComponent = c;
            this.forceUpdate();
        }
    }

    render() {

        if (!this.state.pack) {
            return (
                <div className="normal-control">
                    {this.renderTitle() }
                </div>
            );
        }

        const entity = this.state.pack.entity;

        const frame: EntityFrame<Entity> = {
            frameComponent: this,
            entityComponent: this.entityComponent,
            onReload: pack => {
                if (pack.entity.id != null && entity.id == null)
                    Navigator.currentHistory.push(Navigator.navigateRoute(pack.entity));
                else
                    this.setState({ pack });
            },
            onClose: () => this.onClose(),
            revalidate: () => this.validationErrors && this.validationErrors.forceUpdate(),
            setError: (ms, initialPrefix) => {
                GraphExplorer.setModelState(entity, ms, initialPrefix || "");
                this.forceUpdate()
            },
        };

        const ti = this.getTypeInfo();

        const styleOptions: StyleOptions = {
            readOnly: Navigator.isReadOnly(ti),
            frame: frame
        };

        const ctx = new TypeContext<Entity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(entity, ""));


        const wc: WidgetContext = {
            ctx: ctx,
            pack: this.state.pack,
        };

        const embeddedWidgets = renderEmbeddedWidgets(wc);

        return (
            <div className="normal-control">
                { this.renderTitle() }
                { renderWidgets(wc) }
                { this.entityComponent && <ButtonBar frame={frame} pack={this.state.pack} showOperations={true} /> }
                <ValidationErrors entity={this.state.pack.entity} ref={ve => this.validationErrors = ve}/>
                { embeddedWidgets.top }
                <div className="sf-main-control form-horizontal" data-test-ticks={new Date().valueOf() } data-main-entity={entityInfo(ctx.value) }>
                    {this.state.getComponent && React.cloneElement(this.state.getComponent(ctx),  { ref: (c: React.Component<any, any>) => this.setComponent(c) }) }
                </div>
                { embeddedWidgets.bottom }
            </div>
        );
    }

    validationErrors: ValidationErrors;

    renderTitle() {

        if (!this.state.pack)
            return <h3>{JavascriptMessage.loading.niceToString() }</h3>;

        const entity = this.state.pack.entity;

        return (
            <h3>
                <span className="sf-entity-title">{ getToString(entity) }</span>
                <br/>
                <small className="sf-type-nice-name">{ Navigator.getTypeTitle(entity, undefined) }</small>
            </h3>
        );
    }
}




