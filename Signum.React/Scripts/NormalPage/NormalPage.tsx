
import * as React from 'react'
import { Dic } from '../Globals'
import * as Navigator from '../Navigator'
import ButtonBar from './ButtonBar'
import { EntityComponent, EntityComponentProps } from '../Lines'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '../FindOptions'
import { Entity, Lite, is, toLite, LiteMessage, getToString, EntityPack, ModelState, JavascriptMessage } from '../Signum.Entities'
import { TypeContext, StyleOptions } from '../TypeContext'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, getTypeInfos } from '../Reflection'

require("!style!css!./NormalPage.css");

interface NormalPageProps extends ReactRouter.RouteComponentProps<{}, { type: string; id?: string }> {
    showOperations?: boolean;
    component?: React.ComponentClass<EntityComponentProps<Entity>>;
    title?: string;
}


interface NormalPageState {
    pack?: EntityPack<Entity>;
    modelState?: ModelState;
    component?: React.ComponentClass<EntityComponentProps<Entity>>;
    entitySettings?: Navigator.EntitySettingsBase;
    typeInfo?: TypeInfo;
}

export default class NormalPage extends React.Component<NormalPageProps, NormalPageState> {

    static defaultProps: NormalPageProps = {
        showOperations: true,
        component: null,
    }

    constructor(props) {
        super(props);
        this.state = this.calculateState(props);
        this.loadEntity(props)
            .then(() => this.loadComponent());
    }

    componentWillReceiveProps(props) {
        this.setState(this.calculateState(props));
        this.loadEntity(props)
            .then(() => this.loadComponent());
    }

    calculateState(props: NormalPageProps) {
        const typeInfo = getTypeInfo(props.routeParams.type);

        const entitySettings = Navigator.getSettings(typeInfo.name);

        return { entitySettings: entitySettings, typeInfo: typeInfo, entity: null };
    }

    loadEntity(props: NormalPageProps): Promise<void> {

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
            </div>
        );
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

        const ctx = new TypeContext<Entity>(null, styleOptions, PropertyRoute.root(this.state.typeInfo), new ReadonlyBinding(entity));

        var component: EntityComponent<Entity> = this.state.component && React.createElement(this.state.component, { ctx: ctx }) as any;

        return (
            <div className="normal-control">
                {this.renderTitle() }
                {Navigator.renderWidgets({ entity: entity }) }
                <ButtonBar component={component} pack={this.state.pack} showOperations={this.props.showOperations} />
                <ValidationErrors modelState={this.state.modelState}/>
                {Navigator.renderEmbeddedWidgets({ entity: entity }, Navigator.EmbeddedWidgetPosition.Top) }
                <div id="divMainControl" className="sf-main-control" data-test-ticks={new Date().valueOf() }>
                    {component}
                </div>
                {Navigator.renderEmbeddedWidgets({ entity: entity }, Navigator.EmbeddedWidgetPosition.Bottom) }
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


export class ValidationErrors extends React.Component<{ modelState: ModelState }, void>
{
    render() {

        var modelState = this.props.modelState;

        if (!modelState || Dic.getKeys(modelState).length == 0)
            return null;

        return (
            <ul className="validaton-summary alert alert-danger">
                { Dic.getValues(modelState).map(error => <li>{error}</li>) }
            </ul>
        );
    }
}



