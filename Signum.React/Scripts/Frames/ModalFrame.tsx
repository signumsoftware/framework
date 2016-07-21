
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar, Button } from 'react-bootstrap'
import { openModal, IModalProps } from '../Modals'
import * as Navigator from '../Navigator'
import ButtonBar from './ButtonBar'

import { ValidationError } from '../Services'
import { ifError } from '../Globals'
import { TypeContext, StyleOptions, EntityFrame, IRenderButtons } from '../TypeContext'
import { Entity, Lite, ModifiableEntity, JavascriptMessage, NormalWindowMessage, toLite, getToString, EntityPack, ModelState, entityInfo, isEntityPack, isLite } from '../Signum.Entities'
import { getTypeInfo, TypeInfo, PropertyRoute, ReadonlyBinding, GraphExplorer, isModel, parseId } from '../Reflection'
import ValidationErrors from './ValidationErrors'
import { renderWidgets, WidgetContext } from './Widgets'
import { needsCanExecute } from '../Operations/EntityOperations'

require("!style!css!./Frames.css");

interface ModalFrameProps extends React.Props<ModalFrame>, IModalProps {
    title?: string;
    entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>;
    propertyRoute?: PropertyRoute;
    showOperations?: boolean;
    validate?: boolean;
    requiresSaveOperation?: boolean;
    avoidPromptLooseChange?: boolean;
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
    isNavigate?: boolean;
    readOnly?: boolean;
}

interface ModalFrameState {
    pack?: EntityPack<ModifiableEntity>;
    getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
    propertyRoute?: PropertyRoute;
    show?: boolean;
    prefix?: string;
}

let modalCount = 0;

export default class ModalFrame extends React.Component<ModalFrameProps, ModalFrameState>  {

    static defaultProps: ModalFrameProps = {
        showOperations: true,
        getComponent: undefined,
        entityOrPack: null as any
    }

    constructor(props: ModalFrameProps) {
        super(props);
        this.state = this.calculateState(props);
        this.state.prefix = "modal" + (modalCount++);
    }

    componentWillMount() {
        Navigator.toEntityPack(this.props.entityOrPack, this.props.showOperations!)
            .then(ep => this.setPack(ep))
            .then(() => this.loadComponent())
            .done();
    }

    componentWillReceiveProps(props: ModalFrameProps) {
        this.setState(this.calculateState(props));

        Navigator.toEntityPack(props.entityOrPack, this.props.showOperations!)
            .then(ep => this.setPack(ep))
            .then(() => this.loadComponent())
            .done();
    }


    getTypeName() {
        return (this.props.entityOrPack as Lite<Entity>).EntityType ||
            (this.props.entityOrPack as ModifiableEntity).Type ||
            (this.props.entityOrPack as EntityPack<ModifiableEntity>).entity.Type;
    }
    
    getTypeInfo() {
        const typeName = this.getTypeName();

        return getTypeInfo(typeName);
    }

    calculateState(props: ModalFrameState): ModalFrameState {

        const typeInfo = this.getTypeInfo();

        const pr = typeInfo ? PropertyRoute.root(typeInfo) : this.props.propertyRoute;

        if (!pr)
            throw new Error("propertyRoute is mandatory for embeddedEntities");

        return {
            propertyRoute: pr,
            show: true,
        };
    }



    setPack(pack: EntityPack<ModifiableEntity>): void {
        this.setState({
            pack: pack
        });
    }

    loadComponent() {

        if (this.props.getComponent) {
            this.setState({ getComponent: this.props.getComponent });
            return Promise.resolve(undefined);
        }

        return Navigator.getComponent(this.state.pack!.entity)
            .then(c => this.setState({ getComponent: (ctx) => React.createElement(c, { ctx: ctx }) }));
    }

    okClicked: boolean;
    handleOkClicked = (val: any) => {
        if (this.hasChanges() && this.props.requiresSaveOperation) {
            alert(JavascriptMessage.saveChangesBeforeOrPressCancel.niceToString());
            return;
        }

        if (!this.props.validate) {

            this.okClicked = true;
            this.setState({ show: false });

            return;
        }

        Navigator.API.validateEntity(this.state.pack!.entity)
            .then(() => {

                this.okClicked = true;
                this.setState({ show: false });

            }, ifError(ValidationError, e => {
                GraphExplorer.setModelState(this.state.pack!.entity, e.modelState, "entity");
                this.forceUpdate();
            }));
    }


    handleCancelClicked = () => {

        if (this.hasChanges() && !this.props.avoidPromptLooseChange) {
            if (!confirm(NormalWindowMessage.LoseChanges.niceToString()))
                return;
        }

        this.setState({ show: false });
    }

    hasChanges() {

        const entity = this.state.pack!.entity;

        GraphExplorer.propagateAll(entity);

        return entity.modified;
    }

    handleOnExited = () => {
        this.props.onExited!(this.okClicked ? this.state.pack!.entity : undefined);
    }

    render() {

        const pack = this.state.pack;

        return (
            <Modal bsSize="lg" onHide={this.handleCancelClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-popup-control">
                <Modal.Header closeButton={this.props.isNavigate}>
                    {!this.props.isNavigate && <ButtonToolbar style={{ float: "right" }}>
                        <Button className="sf-entity-button sf-close-button sf-ok-button" bsStyle="primary" disabled={!pack} onClick={this.handleOkClicked}>{JavascriptMessage.ok.niceToString() }</Button>
                        <Button className="sf-entity-button sf-close-button sf-cancel-button" bsStyle="default" disabled={!pack} onClick={this.handleCancelClicked}>{JavascriptMessage.cancel.niceToString() }</Button>
                    </ButtonToolbar>}
                    {this.renderTitle() }
                </Modal.Header>
                {pack && this.renderBody() }  
            </Modal>
        );
    }

    entityComponent: React.Component<any, any>;

    setComponent(c: React.Component<any, any>) {
        if (c && this.entityComponent != c) {
            this.entityComponent = c;
            this.forceUpdate();
        }
    }

    renderBody() {
        
        const frame: EntityFrame<Entity> = {
            frameComponent: this,
            entityComponent: this.entityComponent,
            onReload: pack => this.setPack(pack),
            onClose: () => this.props.onExited!(undefined),
            revalidate: () => this.validationErrors && this.validationErrors.forceUpdate(),
            setError: (modelState, initialPrefix = "") => {
                GraphExplorer.setModelState(this.state.pack!.entity, modelState, initialPrefix!);
                this.forceUpdate();
            },
        };

        const styleOptions: StyleOptions = {
            readOnly: this.props.readOnly != undefined ? this.props.readOnly : Navigator.isReadOnly(this.getTypeName()),
            frame: frame,
        };

        const pack = this.state.pack!;

        const ctx = new TypeContext(undefined, styleOptions, this.state.propertyRoute!, new ReadonlyBinding(pack.entity, this.state.prefix!));

        return (
            <Modal.Body>
                {renderWidgets({ ctx: ctx, pack: pack }) }
                { this.entityComponent && <ButtonBar frame={frame} pack={pack} showOperations={this.props.showOperations!} />}
                <ValidationErrors entity={pack.entity} ref={ve => this.validationErrors = ve}/>
                <div className="sf-main-control form-horizontal" data-test-ticks={new Date().valueOf() } data-main-entity={entityInfo(ctx.value) }>
                    { this.state.getComponent && React.cloneElement(this.state.getComponent(ctx), { ref: (c: React.Component<any,any>) => this.setComponent(c) }) }
                </div>
            </Modal.Body>
        );
    }

    validationErrors: ValidationErrors;

    renderTitle() {
        
        if (!this.state.pack)
            return <h3>{JavascriptMessage.loading.niceToString() }</h3>;

        const entity = this.state.pack.entity;
        const pr = this.props.propertyRoute;

        return (
            <h4>
                <span className="sf-entity-title">{this.props.title || getToString(entity) }</span>&nbsp;
                {this.renderExpandLink() }
                <br />
                <small> {pr && pr.member && pr.member.typeNiceName || Navigator.getTypeTitle(entity, pr) }</small>
            </h4>
        );
    }

    renderExpandLink() {
        const entity = this.state.pack!.entity;

        if (entity == undefined || entity.isNew)
            return undefined;

        const ti = getTypeInfo(entity.Type);

        if (ti == undefined || !Navigator.isNavigable(ti, false)) //Embedded
            return undefined;

        return (
            <a href={ Navigator.navigateRoute(entity as Entity) } className="sf-popup-fullscreen" onClick={this.handlePopupFullScreen}>
                <span className="glyphicon glyphicon-new-window"></span>
            </a>
        );
    }

    handlePopupFullScreen = (e: React.MouseEvent) => {

        if (e.ctrlKey || e.buttons) {

        } else {

            Navigator.currentHistory.push(Navigator.navigateRoute(this.state.pack!.entity as Entity));

            e.preventDefault();
        }
    }

    static openView(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, options: Navigator.ViewOptions): Promise<Entity> {

        return openModal<Entity>(<ModalFrame
            entityOrPack={entityOrPack}
            readOnly={options.readOnly}
            propertyRoute={options.propertyRoute}
            getComponent={options.getComponent}
            showOperations={options.showOperations}
            requiresSaveOperation={options.requiresSaveOperation}
            avoidPromptLooseChange={options.avoidPromptLooseChange}
            validate={options.validate == undefined ? ModalFrame.isModelEntity(entityOrPack) : options.validate }
            title={options.title}
            isNavigate={false}/>);
    }

    static isModelEntity(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>) {
        const typeName = isEntityPack(entityOrPack) ? entityOrPack.entity.Type :
            isLite(entityOrPack) ? entityOrPack.EntityType : entityOrPack.Type;

        return isModel(typeName);
    }

    static openNavigate(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, options: Navigator.NavigateOptions): Promise<void> {

        return openModal<void>(<ModalFrame
            entityOrPack={entityOrPack}
            readOnly={options.readOnly}
            propertyRoute={undefined}
            getComponent={options.getComponent}
            showOperations={true}
            requiresSaveOperation={undefined}
            avoidPromptLooseChange={options.avoidPromptLooseChange}
            isNavigate={true}/>);
    }
}



