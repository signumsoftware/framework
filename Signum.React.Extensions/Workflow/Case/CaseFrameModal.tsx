import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '@framework/Modals'
import { TypeContext, StyleOptions, EntityFrame } from '@framework/TypeContext'
import { TypeInfo, getTypeInfo, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import MessageModal from '@framework/Modals/MessageModal'
import { Lite, JavascriptMessage, NormalWindowMessage, entityInfo, getToString, toLite } from '@framework/Signum.Entities'
import { renderWidgets, WidgetContext } from '@framework/Frames/Widgets'
import ValidationErrors from '@framework/Frames/ValidationErrors'
import ButtonBar from '@framework/Frames/ButtonBar'
import { CaseActivityEntity, ICaseMainEntity, WorkflowActivityEntity } from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'
import CaseFromSenderInfo from './CaseFromSenderInfo'
import CaseButtonBar from './CaseButtonBar'
import CaseFlowButton from './CaseFlowButton'
import InlineCaseTags from './InlineCaseTags'
import { IHasCaseActivity } from '../WorkflowClient';
import { Modal, ErrorBoundary } from '@framework/Components';
import { ModalHeaderButtons } from '@framework/Components/Modal';
import "@framework/Frames/Frames.css"
import "./CaseAct.css"

interface CaseFrameModalProps extends React.Props<CaseFrameModal>, IModalProps {
  title?: string;
  entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | WorkflowClient.CaseEntityPack;
  avoidPromptLooseChange?: boolean;
  readOnly?: boolean;
  isNavigate?: boolean;
}

interface CaseFrameModalState {
  pack?: WorkflowClient.CaseEntityPack;
  getComponent?: (ctx: TypeContext<ICaseMainEntity>) => React.ReactElement<any>;
  show: boolean;
  prefix?: string;
  refreshCount: number;
}

var modalCount = 0;

export default class CaseFrameModal extends React.Component<CaseFrameModalProps, CaseFrameModalState> implements IHasCaseActivity {
  prefix = "caseModal" + (modalCount++)
  constructor(props: any) {
    super(props);
    this.state = this.calculateState(props);
  }

  componentWillMount() {
    WorkflowClient.toEntityPackWorkflow(this.props.entityOrPack)
      .then(ep => this.setPack(ep))
      .then(pack => this.loadComponent(pack))
      .done();
  }

  componentWillReceiveProps(props: any) {
    this.setState(this.calculateState(props));

    WorkflowClient.toEntityPackWorkflow(this.props.entityOrPack)
      .then(ep => this.setPack(ep))
      .then(pack => this.loadComponent(pack))
      .done();
  }

  calculateState(props: CaseFrameModalState): CaseFrameModalState {
    return {
      show: true,
      refreshCount: 0,
    };
  }

  setPack(pack: WorkflowClient.CaseEntityPack): WorkflowClient.CaseEntityPack {
    this.setState({ pack: pack, refreshCount: 0 });
    return pack;
  }

  loadComponent(pack: WorkflowClient.CaseEntityPack): Promise<void> {
    const ca = pack.activity;
    const wa = ca.workflowActivity as WorkflowActivityEntity;

    return Navigator.viewDispatcher.getViewPromise(ca.case.mainEntity, wa.viewName || undefined).promise
      .then(c => this.setState({ getComponent: c }));
  }

  handleCloseClicked = () => {

    if (this.hasChanges() && !this.props.avoidPromptLooseChange) {
      MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
        message: NormalWindowMessage.LoseChanges.niceToString(),
        buttons: "yes_no",
        icon: "warning",
        style: "warning"
      }).then(result => {
        if (result != "yes")
          return;

        this.setState({ show: false });
      }).done();
    }
    else
      this.setState({ show: false });
  }

  hasChanges() {

    var entity = this.state.pack!.activity;

    GraphExplorer.propagateAll(entity);

    return entity.modified;
  }

  okClicked: boolean = false;
  handleCancelClicked = () => {
    if (this.hasChanges() && !this.props.avoidPromptLooseChange) {
      MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
        message: NormalWindowMessage.LoseChanges.niceToString(),
        buttons: "yes_no",
        style: "warning",
        icon: "warning"
      }).then(result => {
        if (result == "yes")
          this.setState({ show: false });
      }).done();
    } else {
      this.setState({ show: false });
    }
  }

  handleOkClicked = () => {
    if (this.hasChanges()) {
      MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.saveChangesBeforeOrPressCancel.niceToString(),
        buttons: "ok",
        style: "warning",
        icon: "warning"
      }).done();
    } else {
      this.okClicked = true;
      this.setState({ show: false });
    }
  }

  handleOnExited = () => {
    this.props.onExited!(this.okClicked ? this.getCaseActivity() : undefined);
  }

  getCaseActivity(): CaseActivityEntity | undefined {
    return this.state.pack && this.state.pack.activity;
  }

  render() {

    var pack = this.state.pack;

    return (
      <Modal size="lg" show={this.state.show} onExited={this.handleOnExited} onHide={this.handleCancelClicked} className="sf-popup-control" >
        <ModalHeaderButtons htmlAttributes={{ style: { display: "block" } }} closeBeforeTitle={true}
          onClose={this.props.isNavigate ? this.handleCancelClicked : undefined}
          onOk={!this.props.isNavigate ? this.handleOkClicked : undefined}
          onCancel={!this.props.isNavigate ? this.handleCancelClicked : undefined}
          okDisabled={!pack}>
          {this.renderTitle()}
        </ModalHeaderButtons>
        {pack && this.renderBody()}
      </Modal>
    );
  }

  entityComponent?: React.Component<any, any>;

  setComponent(c: React.Component<any, any>) {
    if (c && this.entityComponent != c) {
      this.entityComponent = c;
      this.forceUpdate();
    }
  }

  renderBody() {
    var pack = this.state.pack!;

    var activityFrame: EntityFrame = {
      frameComponent: this,
      entityComponent: this.entityComponent,
      onReload: newPack => {
        if (newPack) {
          pack.activity = newPack.entity as CaseActivityEntity;
          pack.canExecuteActivity = newPack.canExecute;
        }
        this.setState({ refreshCount: this.state.refreshCount + 1 });
      },
      onClose: (ok?: boolean) => this.props.onExited!(ok ? this.getCaseActivity() : undefined),
      revalidate: () => this.validationErrors && this.validationErrors.forceUpdate(),
      setError: (modelState, initialPrefix) => {
        GraphExplorer.setModelState(pack.activity, modelState, initialPrefix || "");
        this.forceUpdate();
      },
      refreshCount: this.state.refreshCount,
    };

    var activityPack = { entity: pack.activity, canExecute: pack.canExecuteActivity };

    return (
      <div className="modal-body">
        <CaseFromSenderInfo current={pack.activity} />
        {!pack.activity.case.isNew && <div className="inline-tags"> <InlineCaseTags case={toLite(pack.activity.case)} /></div>}
        <div className="sf-main-control" data-test-ticks={new Date().valueOf()} data-activity-entity={entityInfo(pack.activity)}>
          {this.renderMainEntity()}
        </div>
        {this.entityComponent && <CaseButtonBar frame={activityFrame} pack={activityPack} />}
      </div>
    );
  }

  validationErrors?: ValidationErrors | null;

  getMainTypeInfo(): TypeInfo {
    return getTypeInfo(this.state.pack!.activity.case.mainEntity.Type);
  }

  renderMainEntity() {

    var pack = this.state.pack!;
    var mainEntity = pack.activity.case.mainEntity;
    const mainFrame: EntityFrame = {
      frameComponent: this,
      entityComponent: this.entityComponent,
      onReload: newPack => {
        if (newPack) {
          pack.activity.case.mainEntity = newPack.entity as CaseActivityEntity;
          pack.canExecuteMainEntity = newPack.canExecute;
        }
        this.setState({ refreshCount: this.state.refreshCount + 1 });
      },
      onClose: () => this.props.onExited!(null),
      revalidate: () => this.validationErrors && this.validationErrors.forceUpdate(),
      setError: (ms, initialPrefix) => {
        GraphExplorer.setModelState(mainEntity, ms, initialPrefix || "");
        this.forceUpdate()
      },
      refreshCount: this.state.refreshCount,
    };

    var ti = this.getMainTypeInfo();

    const styleOptions: StyleOptions = {
      readOnly: Navigator.isReadOnly(ti) || Boolean(pack.activity.doneDate),
      frame: mainFrame
    };

    const ctx = new TypeContext<ICaseMainEntity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(mainEntity, this.prefix));

    var { activity, canExecuteActivity, canExecuteMainEntity, ...extension } = this.state.pack!;

    var mainPack = { entity: mainEntity, canExecute: pack.canExecuteMainEntity, ...extension };

    const wc: WidgetContext<ICaseMainEntity> = {
      ctx: ctx,
      pack: mainPack,
    };

    return (
      <div className="sf-main-entity case-main-entity" data-main-entity={entityInfo(mainEntity)}>
        {renderWidgets(wc)}
        {this.entityComponent && !mainEntity.isNew && !pack.activity.doneBy ? <ButtonBar frame={mainFrame} pack={mainPack} /> : <br />}
        <ValidationErrors entity={mainEntity} ref={ve => this.validationErrors = ve} />
        <ErrorBoundary>
          {this.state.getComponent && React.cloneElement(this.state.getComponent(ctx), { ref: (c: React.Component<any, any>) => this.setComponent(c) })}
        </ErrorBoundary>
      </div>
    );
  }

  renderTitle() {

    if (!this.state.pack)
      return JavascriptMessage.loading.niceToString();

    const activity = this.state.pack.activity;

    return (
      <div>
        <span className="sf-entity-title">{this.props.title || getToString(activity)}</span>&nbsp;
                {this.renderExpandLink()}
        <br />
        {!activity.case.isNew && <CaseFlowButton caseActivity={this.state.pack.activity} />}
        <small className="sf-type-nice-name text-muted"> {Navigator.getTypeTitle(activity, undefined)}</small>
      </div>
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
      <a href="#" className="sf-popup-fullscreen" onClick={this.handlePopupFullScreen}>
        <FontAwesomeIcon icon="external-link-alt" />
      </a>
    );
  }

  handlePopupFullScreen = (e: React.MouseEvent<any>) => {
    Navigator.pushOrOpenInTab("~/workflow/activity/" + this.state.pack!.activity.id, e);
  }

  static openView(entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | WorkflowClient.CaseEntityPack, readOnly?: boolean): Promise<CaseActivityEntity | undefined> {

    return openModal<CaseActivityEntity>(<CaseFrameModal
      entityOrPack={entityOrPack}
      readOnly={readOnly || false}
      isNavigate={false}
    />);
  }


  static openNavigate(entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | WorkflowClient.CaseEntityPack, readOnly?: boolean): Promise<void> {

    return openModal<void>(<CaseFrameModal
      entityOrPack={entityOrPack}
      readOnly={readOnly || false}
      isNavigate={true}
    />) as Promise<void>;
  }
}
