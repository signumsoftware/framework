
import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { Modal, ModalProps, ModalClass, ButtonToolbar, Button } from 'react-bootstrap'
import { openModal, IModalProps } from '../../../../Framework/Signum.React/Scripts/Modals'
import { TypeContext, StyleOptions, EntityFrame  } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import { EntityPack, Entity, Lite, JavascriptMessage, NormalWindowMessage, entityInfo, getToString } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from '../../../../Framework/Signum.React/Scripts/Frames/Widgets'
import ValidationErrors from '../../../../Framework/Signum.React/Scripts/Frames/ValidationErrors'
import ButtonBar from '../../../../Framework/Signum.React/Scripts/Frames/ButtonBar'
import { CaseActivityEntity, WorkflowEntity, ICaseMainEntity, CaseActivityOperation } from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'

require("!style!css!../../../../Framework/Signum.React/Scripts/Frames/Frames.css");

interface ModalFrameProps extends React.Props<ModalFrame>, IModalProps {
    title?: string;
    entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | WorkflowClient.CaseEntityPack;
    validate?: boolean;
    avoidPromptLooseChange?: boolean;
    readOnly?: boolean;
}

interface ModalFrameState {
    pack?: WorkflowClient.CaseEntityPack;
    getComponent?: (ctx: TypeContext<ICaseMainEntity>) => React.ReactElement<any>;
    show: boolean;
    prefix?: string;
}

var modalCount = 0;

export default class ModalFrame extends React.Component<ModalFrameProps, ModalFrameState>  {

    constructor(props: any) {
        super(props);
        this.state = this.calculateState(props);
        this.state.prefix = "modal" + (modalCount++);
    }

    componentWillMount() {
        WorkflowClient.toEntityPackWorkflow(this.props.entityOrPack)
            .then(ep => this.setPack(ep))
            .then(() => this.loadComponent())
            .done();
    }

    componentWillReceiveProps(props: any) {
        this.setState(this.calculateState(props));

        WorkflowClient.toEntityPackWorkflow(this.props.entityOrPack)  
            .then(ep => this.setPack(ep))
            .then(() => this.loadComponent())
            .done();
    }

    calculateState(props: ModalFrameState): ModalFrameState {
        return {
            show: true,
        };
    }

    setPack(pack: WorkflowClient.CaseEntityPack): void {
        this.setState({ pack: pack });
    }

    loadComponent(): Promise<void> {
        const a = this.state.pack!.activity;
        if (a.workflowActivity) {
            return WorkflowClient.getViewPromise(a.case.mainEntity, a.workflowActivity.viewName!).promise
                .then(c => this.setState({ getComponent: c }));
        }
        else {
            return Navigator.getViewPromise(a.case.mainEntity).promise
                .then(c => this.setState({ getComponent: c }));
        }
    }

    handleCloseClicked = () => {

        if (this.hasChanges() && !this.props.avoidPromptLooseChange) {
            if (!confirm(NormalWindowMessage.LoseChanges.niceToString()))
                return;
        }

        this.setState({ show: false });
    }

    hasChanges() {

        var entity = this.state.pack!.activity;

        GraphExplorer.propagateAll(entity);

        return entity.modified;
    }

    handleOnExited = () => {
        this.props.onExited!(null);
    }

    render() {

        var pack = this.state.pack;

        return (
            <Modal bsSize="lg" onHide={this.handleCloseClicked} show={this.state.show} onExited={this.handleOnExited} className="sf-popup-control">
                <Modal.Header closeButton={true}>
 
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
        var pack = this.state.pack!;

        var activityFrame: EntityFrame<CaseActivityEntity> = {
            frameComponent: this,
            entityComponent: this.entityComponent,
            onReload: newPack => {
                pack.activity = newPack.entity;
                pack.canExecuteActivity = newPack.canExecute;
                this.forceUpdate();
            },
            onClose: () => this.props.onExited!(null),
            revalidate: () => this.validationErrors && this.validationErrors.forceUpdate(),
            setError: (modelState, initialPrefix) => {
                GraphExplorer.setModelState(pack.activity, modelState, initialPrefix || "");
                this.forceUpdate();
            },
        };

        var activityPack = { entity: pack.activity, canExecute: pack.canExecuteActivity };
        
        return (
            <Modal.Body>
                <div className="sf-main-control form-horizontal" data-test-ticks={new Date().valueOf() } data-activity-entity={entityInfo(pack.activity) }>
                    { this.renderMainEntity() }
                </div>
                { this.entityComponent && <ButtonBar frame={activityFrame} pack={activityPack} showOperations={true} /> }
            </Modal.Body>
        );
    }

    validationErrors: ValidationErrors;

    getMainTypeInfo(): TypeInfo {
        return getTypeInfo(this.state.pack!.activity.case.mainEntity.Type);
    }

    renderMainEntity() {

        var pack = this.state.pack!;
        var mainEntity = pack.activity.case.mainEntity;
        const mainFrame: EntityFrame<ICaseMainEntity> = {
            frameComponent: this,
            entityComponent: this.entityComponent,
            onReload: newPack => {
                pack.activity.case.mainEntity = newPack.entity;
                pack.canExecuteMainEntity = newPack.canExecute;
                this.forceUpdate();
            },
            onClose: () => this.props.onExited!(null),
            revalidate: () => this.validationErrors && this.validationErrors.forceUpdate(),
            setError: (ms, initialPrefix) => {
                GraphExplorer.setModelState(mainEntity, ms, initialPrefix || "");
                this.forceUpdate()
            },
        };

        var ti = this.getMainTypeInfo();

        const styleOptions: StyleOptions = {
            readOnly: Navigator.isReadOnly(ti),
            frame: mainFrame
        };

        const ctx = new TypeContext<ICaseMainEntity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(mainEntity, ""));

        var mainPack = { entity: mainEntity, canExecute: pack.canExecuteMainEntity };

        const wc: WidgetContext = {
            ctx: ctx,
            pack: mainPack,
        };
        
        return (
            <div className="sf-main-entity" data-main-entity={entityInfo(mainEntity)}>
                {renderWidgets(wc)}
                {this.entityComponent && !mainEntity.isNew && !pack.activity.doneBy && < ButtonBar frame={mainFrame} pack={mainPack} showOperations={true} />}
                <ValidationErrors entity={mainEntity} ref={ve => this.validationErrors = ve} />
                {this.state.getComponent && React.cloneElement(this.state.getComponent(ctx), { ref: (c: React.Component<any, any>) => this.setComponent(c) })}
            </div>
        );
    }

    renderTitle() {

        if (!this.state.pack)
            return <h3>{JavascriptMessage.loading.niceToString() }</h3>;

        const activity = this.state.pack.activity;

        return (
            <h4>
                <span className="sf-entity-title">{this.props.title || getToString(activity) }</span>&nbsp;
                {this.renderExpandLink() }
                <br />
                <small> {Navigator.getTypeTitle(activity, undefined)}</small>
            </h4>
        );
    }

    renderExpandLink() {
        const entity = this.state.pack!.activity;

        if (entity == null || entity.isNew)
            return null;

        const ti = getTypeInfo(entity.Type);

        if (ti == null || !Navigator.isNavigable(ti, false)) //Embedded
            return null;

        return (
            <a href={ "~/workflow/activity/" + entity.id } className="sf-popup-fullscreen" onClick={this.handlePopupFullScreen}>
                <span className="glyphicon glyphicon-new-window"></span>
            </a>
        );
    }

    handlePopupFullScreen = (e: React.MouseEvent) => {

        if (e.ctrlKey || e.buttons) {

        } else {

            Navigator.currentHistory.push("~/workflow/activity/" + this.state.pack!.activity.id);

            e.preventDefault();
        }
    }


    static openNavigate(entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | WorkflowClient.CaseEntityPack, readOnly? :boolean): Promise<void> {

        return openModal<void>(<ModalFrame
            entityOrPack={entityOrPack}
            readOnly={readOnly || false} />);
    }
}
