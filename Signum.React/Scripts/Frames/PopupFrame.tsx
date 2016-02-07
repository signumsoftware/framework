
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar, Button } from 'react-bootstrap'
import { openModal, IModalProps } from '../Modals'
import * as Navigator from '../Navigator'
import { EntityFrame, EntityComponentProps } from '../Lines'
import ButtonBar from './ButtonBar'

import { TypeContext, StyleOptions } from '../TypeContext'
import { Entity, Lite, ModifiableEntity, JavascriptMessage, NormalWindowMessage, toLite, getToString, EntityPack, ModelState } from '../Signum.Entities'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding } from '../Reflection'

require("!style!css!./Frames.css");

interface PopupFrameProps extends React.Props<PopupFrame>, IModalProps {
    title?: string;
    entityOrPack?: Lite<ModifiableEntity> | ModifiableEntity | EntityPack<ModifiableEntity>;
    propertyRoute?: PropertyRoute;
    showOperations?: boolean;
    saveProtected?: boolean;
    component?: React.ComponentClass<EntityComponentProps<Entity>>;
    isNavigate?: boolean;
    readOnly?: boolean
}

interface PopupFrameState {
    pack?: EntityPack<ModifiableEntity>;
    modelState?: ModelState;
    component?: React.ComponentClass<EntityComponentProps<Entity>>;
    entitySettings?: Navigator.EntitySettingsBase;
    propertyRoute?: PropertyRoute;
    savedEntity?: string;
    show?: boolean;
}

export default class PopupFrame extends React.Component<PopupFrameProps, PopupFrameState>  {

    static defaultProps: PopupFrameProps = {
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

    calculateState(props: PopupFrameState) {

        const typeName = (this.props.entityOrPack as Lite<Entity>).EntityType ||
            (this.props.entityOrPack as ModifiableEntity).Type ||
            (this.props.entityOrPack as EntityPack<ModifiableEntity>).entity.Type;

        const entitySettings = Navigator.getSettings(typeName);

        const typeInfo = getTypeInfo(typeName);

        const pr = typeInfo ? PropertyRoute.root(typeInfo) : this.props.propertyRoute;

        if (!pr)
            throw new Error("propertyRoute is mandatory for embeddedEntities");

        return {
            entitySettings: entitySettings,
            propertyRoute: pr,
            entity: null,
            show: true
        };
    }

    clone(obj: any) {
        return JSON.parse(JSON.stringify(obj));
    }

    loadEntity(props: PopupFrameProps): Promise<void> {

        if ((this.props.entityOrPack as EntityPack<ModifiableEntity>).canExecute) {
            this.setPack(this.props.entityOrPack as EntityPack<ModifiableEntity>);
            return Promise.resolve(null);
        }

        const entity = (this.props.entityOrPack as ModifiableEntity).Type ?
            this.props.entityOrPack as ModifiableEntity :
            (this.props.entityOrPack as Lite<Entity>).entity;

        if (entity != null && (!getTypeInfo(entity.Type) || !this.props.showOperations)) {

            this.setPack({ entity: this.clone(entity), canExecute: {} });
            return Promise.resolve(null);

        } else {
            return Navigator.API.fetchEntityPack(entity ? toLite(entity) : this.props.entityOrPack as Lite<Entity>)
                .then(pack => this.setPack({
                    entity: entity ? this.clone(entity) : pack.entity,
                    canExecute: pack.canExecute
                }));
        }
    }

    setPack(pack: EntityPack<ModifiableEntity>): void {
        this.setState({
            pack: pack,
            savedEntity: JSON.stringify(pack.entity),
        });
    }

    loadComponent() {

        const promise = this.props.component ? Promise.resolve(this.props.component) :
            this.state.entitySettings.onGetComponent(this.state.pack.entity);

        return promise
            .then(c => this.setState({ component: c }));
    }

    okClicked: boolean;
    handleOkClicked = (val: any) => {
        this.okClicked = true;
        this.setState({ show: false });
    }

    handleCancelClicked = () => {
        if (JSON.stringify(this.state.pack.entity) != this.state.savedEntity) {
            if (!confirm(NormalWindowMessage.LoseChanges.niceToString()))
                return;
        }

        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited(this.okClicked ? this.state.pack.entity : null);
    }

    render() {

        var pack = this.state.pack;

        const styleOptions: StyleOptions = {
            readOnly: this.props.readOnly != null ? this.props.readOnly : this.state.entitySettings.onIsReadonly()
        };

        var frame: EntityFrame<Entity> = {
            onReload: pack => this.setPack(pack),
            onClose: () => this.props.onExited(null),
            setError: modelState => this.setState({ modelState }),
        };

        return (
            <Modal bsSize="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-popup-control">
                <Modal.Header closeButton={this.props.isNavigate}>
                    {!this.props.isNavigate && <ButtonToolbar style={{ float: "right" }}>
                        <Button className="sf-entity-button sf-close-button sf-ok-button" bsStyle="primary" disabled={!pack} onClick={this.handleOkClicked}>{JavascriptMessage.ok.niceToString() }</Button>
                        <Button className="sf-entity-button sf-close-button sf-cancel-button" bsStyle="default" disabled={!pack} onClick={this.handleCancelClicked}>{JavascriptMessage.cancel.niceToString() }</Button>
                    </ButtonToolbar>}
                    {this.renderTitle() }
                </Modal.Header>

                {this.state.pack &&
                    <Modal.Body>
                        {Navigator.renderWidgets({ entity: pack.entity }) }
                        <ButtonBar frame={frame} pack={this.state.pack} showOperations={this.props.showOperations} />
                        <div className="sf-main-control form-horizontal" data-test-ticks={new Date().valueOf() }>
                        { this.state.component && React.createElement(this.state.component, {
                            ctx: new TypeContext<Entity>(null, styleOptions, this.state.propertyRoute, new ReadonlyBinding(pack.entity)),
                            frame: frame
                        }) }
                        </div>
                    </Modal.Body>
                }
            </Modal>
        );
    }


    renderTitle() {
        
        if (!this.state.pack)
            return <h3>{JavascriptMessage.loading.niceToString() }</h3>;

        const entity = this.state.pack.entity;
        const pr = this.props.propertyRoute;

        return (
            <h4>
                <span className="sf-entity-title">{this.props.title || getToString(entity) }</span>
                {this.renderExpandLink() }
                <br />
                <small> {pr && pr.member && pr.member.typeNiceName || Navigator.getTypeTitel(entity) }</small>
            </h4>
        );
    }

    renderExpandLink() {
        const entity = this.state.pack.entity;

        if (entity == null)
            return null;

        const ti = getTypeInfo(entity.Type);

        if (ti == null || !Navigator.isNavigable(ti, null)) //Embedded
            return null;

        return (
            <a href={Navigator.navigateRoute(entity) } className="sf-popup-fullscreen">
                <span className="glyphicon glyphicon-new-window"></span>
            </a>
        );
    }

    static open(options: Navigator.ViewOptions): Promise<Entity> {

        return openModal<Entity>(<PopupFrame
            entityOrPack={options.entity}
            readOnly={options.readOnly}
            propertyRoute={options.propertyRoute}
            component={options.component}
            showOperations={options.showOperations}
            saveProtected={options.saveProtected}/>);
    }
}



