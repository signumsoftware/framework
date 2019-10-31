import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '@framework/Modals'
import { TypeContext, StyleOptions, EntityFrame } from '@framework/TypeContext'
import { TypeInfo, getTypeInfo, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import MessageModal from '@framework/Modals/MessageModal'
import { Lite, JavascriptMessage, NormalWindowMessage, entityInfo, getToString, toLite, EntityPack, ModifiableEntity } from '@framework/Signum.Entities'
import { renderWidgets, WidgetContext } from '@framework/Frames/Widgets'
import { ValidationErrors, ValidationErrorHandle } from '@framework/Frames/ValidationErrors'
import { ButtonBar, ButtonBarHandle } from '@framework/Frames/ButtonBar'
import { CaseActivityEntity, ICaseMainEntity, WorkflowActivityEntity, WorkflowPermission } from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'
import CaseFromSenderInfo from './CaseFromSenderInfo'
import CaseButtonBar from './CaseButtonBar'
import CaseFlowButton from './CaseFlowButton'
import InlineCaseTags from './InlineCaseTags'
import { IHasCaseActivity } from '../WorkflowClient';
import { ErrorBoundary, ModalHeaderButtons } from '@framework/Components';
import { Modal } from 'react-bootstrap';
import "@framework/Frames/Frames.css"
import "./CaseAct.css"
import { AutoFocus } from '@framework/Components/AutoFocus';
import { FunctionalAdapter } from '@framework/Frames/FrameModal';
import * as AuthClient from '../../Authorization/AuthClient'

interface CaseFrameModalProps extends React.Props<CaseFrameModal>, IModalProps<CaseActivityEntity | undefined> {
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

  handleKeyDown(e: KeyboardEvent) {
    this.buttonBar && this.buttonBar.handleKeyDown(e);
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

    return WorkflowClient.getViewPromiseCompoment(ca)
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

  setComponent(c: React.Component<any, any> | null) {
    if (c && this.entityComponent != c) {
      this.entityComponent = c;
      this.forceUpdate();
    }
  }

  buttonBar?: ButtonBarHandle | null;

  renderBody() {
    var pack = this.state.pack!;

    var activityFrame: EntityFrame = {
      frameComponent: this,
      entityComponent: this.entityComponent,
      pack: pack && { entity: pack.activity, canExecute: pack.canExecuteActivity },
      onReload: (newPack, reloadComponent, callback) => {
        if (newPack) {
          pack.activity = newPack.entity as CaseActivityEntity;
          pack.canExecuteActivity = newPack.canExecute;
        }
        this.setState({ refreshCount: this.state.refreshCount + 1 }, callback);
      },
      onClose: (pack?: EntityPack<ModifiableEntity>) => this.props.onExited!(this.getCaseActivity()),
      revalidate: () => {
        this.validationErrorsTop && this.validationErrorsTop.forceUpdate();
        this.validationErrorsBottom && this.validationErrorsBottom.forceUpdate();
      },
      setError: (modelState, initialPrefix) => {
        GraphExplorer.setModelState(pack.activity, modelState, initialPrefix || "");
        this.forceUpdate();
      },
      refreshCount: this.state.refreshCount,
      allowChangeEntity: false,
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

  validationErrorsTop?: ValidationErrorHandle | null;
  validationErrorsBottom?: ValidationErrorHandle | null;

  getMainTypeInfo(): TypeInfo {
    return getTypeInfo(this.state.pack!.activity.case.mainEntity.Type);
  }

  renderMainEntity() {

    var { activity, canExecuteActivity, canExecuteMainEntity, ...extension } = this.state.pack!;

    var pack = this.state.pack!;
    var mainEntity = pack.activity.case.mainEntity;
    const mainFrame: EntityFrame = {
      frameComponent: this,
      entityComponent: this.entityComponent,
      pack: pack && { entity: pack.activity.case.mainEntity, canExecute: pack.canExecuteMainEntity, ...extension },
      onReload: (newPack, reloadComponent, callback) => {
        if (newPack) {
          pack.activity.case.mainEntity = newPack.entity as CaseActivityEntity;
          pack.canExecuteMainEntity = newPack.canExecute;
        }
        this.setState({ refreshCount: this.state.refreshCount + 1 }, callback);
      },
      onClose: () => this.props.onExited!(undefined),
      revalidate: () => {
        this.validationErrorsTop && this.validationErrorsTop.forceUpdate();
        this.validationErrorsBottom && this.validationErrorsBottom.forceUpdate();
      },
      setError: (ms, initialPrefix) => {
        GraphExplorer.setModelState(mainEntity, ms, initialPrefix || "");
        this.forceUpdate()
      },
      refreshCount: this.state.refreshCount,
      allowChangeEntity: false,
    };

    var ti = this.getMainTypeInfo();

    const styleOptions: StyleOptions = {
      readOnly: Navigator.isReadOnly(ti) || Boolean(pack.activity.doneDate),
      frame: mainFrame
    };

    const ctx = new TypeContext<ICaseMainEntity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(mainEntity, this.prefix));

    const wc: WidgetContext<ICaseMainEntity> = {
      ctx: ctx,
      frame: mainFrame,
    };

    return (
      <div className="sf-main-entity case-main-entity" data-main-entity={entityInfo(mainEntity)}>
        {renderWidgets(wc)}
        {this.entityComponent && !mainEntity.isNew && !pack.activity.doneBy ? <ButtonBar ref={bb => this.buttonBar = bb} frame={mainFrame} pack={mainFrame.pack} /> : <br />}
        <ValidationErrors entity={mainEntity} ref={ve => this.validationErrorsTop = ve} prefix={this.prefix} />
        <ErrorBoundary>
          {this.state.getComponent && <AutoFocus>{FunctionalAdapter.withRef(this.state.getComponent(ctx), c => this.setComponent(c))}</AutoFocus>}
        </ErrorBoundary>
        <br />
        <ValidationErrors entity={mainEntity} ref={ve => this.validationErrorsBottom = ve} prefix={this.prefix} />
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
        {!activity.case.isNew && AuthClient.isPermissionAuthorized(WorkflowPermission.ViewCaseFlow) &&
          <CaseFlowButton caseActivity={this.state.pack.activity} />}
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

