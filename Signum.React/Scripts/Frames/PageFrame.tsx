
import * as React from 'react'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import * as Finder from '../Finder'
import ButtonBar from './ButtonBar'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, JavascriptMessage } from '../Signum.Entities'
import { TypeContext, StyleOptions, EntityFrame } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos, GraphExplorer } from '../Reflection'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from './Widgets'
import ValidationErrors from './ValidationErrors'

require("!style!css!./Frames.css");

interface PageFrameProps extends ReactRouter.RouteComponentProps<{}, { type: string; id?: string }> {
    showOperations?: boolean;
    component?: React.ComponentClass<{ ctx: TypeContext<Entity> }>;
    title?: string;
}


interface PageFrameState {
    pack?: EntityPack<Entity>;
    component?: React.ComponentClass<{ ctx: TypeContext<Entity> }>;
}

export default class PageFrame extends React.Component<PageFrameProps, PageFrameState> {

    static defaultProps: PageFrameProps = {
        showOperations: true,
        component: null,
    }

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
        
        return { component: null, pack: null } as PageFrameState;
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
            
            const id = ti.members["Id"].type.name == "number" ? parseInt(props.routeParams.id) : props.routeParams.id;

            const lite: Lite<Entity> = {
                EntityType: ti.name,
                id: id,
            };

            return Navigator.API.fetchEntityPack(lite)
                .then(pack => this.setState({ pack }));

        } else {

            return Constructor.construct(ti.name)
                .then(pack => this.setState({ pack : pack as EntityPack<Entity> }));
        }
    }
    

    loadComponent(): Promise<void> {

        const promise = this.props.component ? Promise.resolve(this.props.component) :
            Navigator.getComponent(this.state.pack.entity);

        return promise
            .then(c => this.setState({ component: c }));
    }

    render() {
        return (
            <div id="divMainPage" data-isnew={this.props.routeParams.id == null} className="form-horizontal">
                {this.renderEntityControl() }
            </div>
        );
    }

    onClose() {
        if (Finder.isFindable(this.state.pack.entity.Type))
            Navigator.currentHistory.push(Finder.findOptionsPath({ queryName: this.state.pack.entity.Type }));
        else
            Navigator.currentHistory.push("/");
    }

    renderEntityControl() {

        if (!this.state.pack) {
            return (
                <div className="normal-control">
                    {this.renderTitle() }
                </div>
            );
        }

        const entity = this.state.pack.entity;



        const frame: EntityFrame<Entity> = {
            component: this,
            onReload: pack => this.setState({ pack }),
            onClose: () => this.onClose(),
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
                <ButtonBar frame={frame} pack={this.state.pack} showOperations={this.props.showOperations} />
                <ValidationErrors entity={this.state.pack.entity}/>
                { embeddedWidgets.top }
                <div id="divMainControl" className="sf-main-control" data-test-ticks={new Date().valueOf() }>
                    {this.state.component && React.createElement<{ ctx: TypeContext<Entity> }>(this.state.component, { ctx: ctx }) }
                </div>
                { embeddedWidgets.bottom }
            </div>
        );
    }

    renderTitle() {

        if (!this.state.pack)
            return <h3>{JavascriptMessage.loading.niceToString() }</h3>;

        const entity = this.state.pack.entity;

        return (
            <h3>
                <span className="sf-entity-title">{ this.props.title || getToString(entity) }</span>
                <br/>
                <small className="sf-type-nice-name">{ Navigator.getTypeTitle(entity, null) }</small>
            </h3>
        );
    }
}




