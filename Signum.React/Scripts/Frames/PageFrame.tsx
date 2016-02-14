
import * as React from 'react'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import * as Finder from '../Finder'
import ButtonBar from './ButtonBar'
import { EntityComponent, EntityComponentProps, EntityFrame } from '../Lines'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, JavascriptMessage } from '../Signum.Entities'
import { TypeContext, StyleOptions } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from './Widgets'
import { GlobalModalContainer} from '../Modals'
import ValidationErrors from './ValidationErrors'

require("!style!css!./Frames.css");

interface PageFrameProps extends ReactRouter.RouteComponentProps<{}, { type: string; id?: string }> {
    showOperations?: boolean;
    component?: React.ComponentClass<EntityComponentProps<Entity>>;
    title?: string;
}


interface PageFrameState {
    pack?: EntityPack<Entity>;
    modelState?: ModelState;
    component?: React.ComponentClass<EntityComponentProps<Entity>>;
    entitySettings?: Navigator.EntitySettingsBase;
    typeInfo?: TypeInfo;
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
        this.loadEntity(this.props)
            .then(() => this.loadComponent());
    }

    componentWillReceiveProps(props) {
        this.setState(this.calculateState(props), () => {
            this.loadEntity(props)
                .then(() => this.loadComponent());
        });
    }

    calculateState(props: PageFrameProps) {
        const typeInfo = getTypeInfo(props.routeParams.type);

        const entitySettings = Navigator.getSettings(typeInfo.name);

        return { entitySettings: entitySettings, typeInfo: typeInfo, component: null, modelState: null, pack: null } as PageFrameState;
    }

    loadEntity(props: PageFrameProps): Promise<void> {

        const ti = this.state.typeInfo;

        const id = ti.members["Id"].type.name == "number" &&
            this.props.routeParams.id != "" ? parseInt(props.routeParams.id) : props.routeParams.id;

        const lite: Lite<Entity> = {
            EntityType: ti.name,
            id: id,
        };

        return Navigator.API.fetchEntityPack(lite)
            .then(pack => this.setState({ pack }));
    }

    loadComponent(): Promise<void> {

        const promise = this.props.component ? Promise.resolve(this.props.component) :
            this.state.entitySettings.onGetComponent(this.state.pack.entity);

        return promise
            .then(c => this.setState({ component: c }));
    }

    render() {
        return (
            <div id="divMainPage" data-isnew={this.props.routeParams.id == null} className="form-horizontal">
                {this.renderEntityControl() }
                <GlobalModalContainer/>
            </div>
        );
    }

    onClose() {
        if (Finder.isFindable(this.state.pack.entity.Type))
            Navigator.currentHistory.push(Finder.findOptionsPath(this.state.pack.entity.Type));
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

        const styleOptions: StyleOptions = {
            readOnly: this.state.entitySettings.onIsReadonly()
        };

        const ctx = new TypeContext<Entity>(null, styleOptions, PropertyRoute.root(this.state.typeInfo), new ReadonlyBinding(entity), this.state.modelState);

        var frame: EntityFrame<Entity> = {
            onReload: pack => this.setState({ pack, modelState: null }),
            onClose: () => this.onClose(),
            setError: ms => this.setState({ modelState: ms }),
        };

        var wc: WidgetContext = {
            ctx: ctx,
            pack: this.state.pack,
        };

        var embeddedWidgets = renderEmbeddedWidgets(wc);

        return (
            <div className="normal-control">
                { this.renderTitle() }
                { renderWidgets(wc) }
                <ButtonBar frame={frame} pack={this.state.pack} showOperations={this.props.showOperations} />
                <ValidationErrors modelState={this.state.modelState}/>
                { embeddedWidgets.top }
                <div id="divMainControl" className="sf-main-control" data-test-ticks={new Date().valueOf() }>
                    {this}
                    {this.state.component && React.createElement<EntityComponentProps<Entity>>(this.state.component, { ctx: ctx, frame: frame }) }
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
                <small className="sf-type-nice-name">{Navigator.getTypeTitel(entity) }</small>
            </h3>
        );
    }
}




