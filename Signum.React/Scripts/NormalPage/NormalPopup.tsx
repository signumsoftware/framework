
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar, Button } from 'react-bootstrap'
import { openModal, IModalProps } from '../Modals'
import * as Navigator from '../Navigator'
import { TypeContext, StyleOptions } from '../TypeContext'
import { Entity, Lite, ModifiableEntity, JavascriptMessage, NormalWindowMessage, toLite, getToString } from '../Signum.Entities'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding } from '../Reflection'

require("!style!css!./NormalPage.css");

interface NormalPopupProps extends React.Props<NormalPopup>, IModalProps {
    title?: string;
    entity?: Lite<Entity> | Entity;
    propertyRoute?: PropertyRoute;
    showOperations?: boolean;
    saveProtected?: boolean;
    component?: Navigator.EntityComponent<any>;
    isNavigate?: boolean;
    readOnly?: boolean
}

interface NormalPopupState {
    entity?: ModifiableEntity;
    canExecute?: { [key: string]: string };
    validationErrors?: { [key: string]: string };
    component?: Navigator.EntityComponent<any>;
    entitySettings?: Navigator.EntitySettingsBase;
    propertyRoute?: PropertyRoute;
    savedEntity?: string;
    show?: boolean;
}

export default class NormalPopup extends React.Component<NormalPopupProps, NormalPopupState>  {

    static defaultProps: NormalPopupProps = {
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

    calculateState(props: NormalPopupState) {

        var typeName = (this.props.entity as Lite<Entity>).EntityType || (this.props.entity as ModifiableEntity).Type; 

        var entitySettings = Navigator.getSettings(typeName);

        var typeInfo = getTypeInfo(typeName);
        
        var pr = typeInfo ? PropertyRoute.root(typeInfo) : this.props.propertyRoute;

        if (!pr)
            throw new Error("propertyRoute is mandatory for embeddedEntities");

        return {
            entitySettings: entitySettings,
            propertyRoute: pr,
            entity: null,
            show: true
        };
    }

    loadEntity(props: NormalPopupProps) : Promise<void> {
        
        var entity = (this.props.entity as ModifiableEntity).Type ?
            this.props.entity as ModifiableEntity :
            (this.props.entity as Lite<Entity>).entity;

        if (entity != null && (!getTypeInfo(entity.Type) || !this.props.showOperations)) {

            this.setEntity(entity, null);
            return Promise.resolve();

        } else {
            return Navigator.API.fetchEntityPack(entity ? toLite(entity) : this.props.entity as Lite<Entity>)
                .then(pack=> this.setEntity(entity || pack.entity, pack.canExecute));
        }
    }

    setEntity(e: ModifiableEntity, canExecute: { [key: string]: string }): void {
        this.setState({
            entity: e,
            savedEntity: JSON.stringify(e),
            canExecute: canExecute
        });
    }

    loadComponent() {

        var promise = this.props.component ? Promise.resolve(this.props.component) :
            this.state.entitySettings.onGetComponentDefault(this.state.entity); 

        return promise
            .then(c=> this.setState({ component: c }));
    }

    okClicked: boolean;
    handleOkClicked = (val: any) => {
        this.okClicked = true;
        this.setState({ show: false });
    }

    handleCancelClicked = () => {
        if (JSON.stringify(this.state.entity) != this.state.savedEntity) {
            if (!confirm(NormalWindowMessage.LoseChanges.niceToString()))
                return;
        }

        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited(this.okClicked ? this.state.entity : null);
    }

    render() {

        var styleOptions: StyleOptions = {
            readOnly: this.props.readOnly != null ? this.props.readOnly : this.state.entitySettings.onIsReadonly()
        };
        var ctx = new TypeContext<Entity>(null, styleOptions, this.state.propertyRoute, new ReadonlyBinding(this.state.entity));

        return <Modal bsSize="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-popup-control">
            <Modal.Header closeButton={this.props.isNavigate}>
                {!this.props.isNavigate && <ButtonToolbar style={{ float: "right" }}>
                    <Button className="sf-entity-button sf-close-button sf-ok-button" bsStyle="primary" onClick={this.handleOkClicked}>{JavascriptMessage.ok.niceToString() }</Button>
                    <Button className="sf-entity-button sf-close-button sf-cancel-button" bsStyle="default" onClick={this.handleCancelClicked}>{JavascriptMessage.cancel.niceToString() }</Button>
                    </ButtonToolbar>}
                {this.renderTitle() }
                </Modal.Header>

            <Modal.Body>
                   {Navigator.renderWidgets({ entity: this.state.entity }) }
                    <div className="btn-toolbar sf-button-bar">
                {Navigator.renderButtons({ entity: this.state.entity, canExecute: this.state.canExecute }) }
                        </div>

                 <div className="sf-main-control form-horizontal" data-test-ticks={new Date().valueOf() }>
                    {this.state.component && React.createElement(this.state.component, { ctx: ctx }) }
                     </div>
                </Modal.Body>
            </Modal>;
    }


    renderTitle() {

        if (this.state.entity == null)
            return <h3>{JavascriptMessage.loading.niceToString()}</h3>;

        var pr = this.props.propertyRoute;
        

        return <h4>
            <span className="sf-entity-title">{this.props.title || (this.state.entity && getToString(this.state.entity)) }</span>
            {this.renderExpandLink() }
               <br />
                    <small> {pr && pr.member && pr.member.typeNiceName || Navigator.getTypeTitel(this.state.entity) }</small>
            </h4>;
    }

    renderExpandLink() {
        var entity = this.state.entity;

        if (entity == null)
            return null;

        var ti = getTypeInfo(entity.Type);

        if (ti == null || !Navigator.isNavigable(ti, null)) //Embedded
            return null;

        return <a href={Navigator.navigateRoute(entity) } className="sf-popup-fullscreen">
            <span className="glyphicon glyphicon-new-window"></span>
            </a>;
    }

    static open(options: Navigator.ViewOptions): Promise<Entity> {

        return openModal<Entity>(<NormalPopup
            entity={options.entity}
            readOnly={options.readOnly}
            propertyRoute={options.propertyRoute}
            component={options.compoenent}
            showOperations={options.showOperations}
            saveProtected={options.saveProtected}/>);
    }
}



