
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

interface PageFrameProps extends ReactRouter.RouteComponentProps<{}, { type: string; id?: string }> {
}


interface PageFrameState {
    pack?: EntityPack<Entity>;
    componentClass?: React.ComponentClass<{ ctx: TypeContext<Entity> }>;
}

export default class PageFrame extends React.Component<PageFrameProps, PageFrameState> {

    constructor(props) {
        super(props);
        this.state = this.calculateState(props);

    }

    componentWillMount() {
        this.load(this.props);
    }

    getTypeInfo(): TypeInfo {
        return getTypeInfo(this.props.routeParams.type);
    }

    calculateState(props: PageFrameProps) {
        
        return { componentClass: null, pack: null } as PageFrameState;
    }


    componentWillReceiveProps(newProps) {
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
        
        const ti = this.getTypeInfo();

        if (this.props.routeParams.id) {
            
            const lite: Lite<Entity> = {
                EntityType: ti.name,
                id: parseId(ti, props.routeParams.id),
            };

            return Navigator.API.fetchEntityPack(lite)
                .then(pack => this.setState({ pack }));

        } else {

            return Constructor.construct(ti.name)
                .then(pack => this.setState({ pack : pack as EntityPack<Entity> }));
        }
    }
    

    loadComponent(): Promise<void> {
        return Navigator.getComponent(this.state.pack.entity)
            .then(c => this.setState({ componentClass: c }));
    }

    onClose() {
        if (Finder.isFindable(this.state.pack.entity.Type))
            Navigator.currentHistory.push(Finder.findOptionsPath({ queryName: this.state.pack.entity.Type }));
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
            setError: (ms, initialPrefix = "") => {
                GraphExplorer.setModelState(entity, ms, initialPrefix);
                this.forceUpdate()
            },
        };

        var ti = this.getTypeInfo();

        const styleOptions: StyleOptions = {
            readOnly: Navigator.isReadOnly(ti),
            frame: frame
        };

        const ctx = new TypeContext<Entity>(null, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(entity, ""));


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
                    {this.state.componentClass && React.createElement(this.state.componentClass, { ctx: ctx, ref: c => this.setComponent(c) } as any) }
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
                <small className="sf-type-nice-name">{ Navigator.getTypeTitle(entity, null) }</small>
            </h3>
        );
    }
}




