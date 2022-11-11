import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps } from '@framework/Modals'
import { TypeContext, StyleOptions, EntityFrame } from '@framework/TypeContext'
import { TypeInfo, getTypeInfo, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '@framework/Reflection'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import MessageModal from '@framework/Modals/MessageModal'
import { Lite, JavascriptMessage, entityInfo, getToString, toLite, EntityPack, ModifiableEntity, SaveChangesMessage } from '@framework/Signum.Entities'
import { renderWidgets, WidgetContext } from '@framework/Frames/Widgets'
import { ValidationErrors, ValidationErrorsHandle } from '@framework/Frames/ValidationErrors'
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
import { FunctionalAdapter } from '@framework/Modals';
import * as AuthClient from '../../Authorization/AuthClient'

interface CaseFrameModalProps extends IModalProps<CaseActivityEntity | undefined> {
  title?: string;
  entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | WorkflowClient.CaseEntityPack;
  avoidPromptLooseChange?: boolean;
  readOnly?: boolean;
}

interface CaseFrameModalState {
  pack?: WorkflowClient.CaseEntityPack;
  getComponent?: (ctx: TypeContext<ICaseMainEntity>) => React.ReactElement<any>;
  show: boolean;
  prefix?: string;
  refreshCount: number;
  executing?: boolean;
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
      .then(pack => this.loadComponent(pack));
  }

  componentWillReceiveProps(props: any) {
    this.setState(this.calculateState(props));

    WorkflowClient.toEntityPackWorkflow(this.props.entityOrPack)
      .then(ep => this.setPack(ep))
      .then(pack => this.loadComponent(pack));
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


  hasChanges() {

    var entity = this.state.pack!.activity;

    GraphExplorer.propagateAll(entity);

    return entity.modified;
  }

  okClicked: boolean = false;
  handleCloseClicked = () => {
    if (this.hasChanges() && !this.props.avoidPromptLooseChange) {
      MessageModal.show({
        title: SaveChangesMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.loseCurrentChanges.niceToString(),
        buttons: "yes_no",
        style: "warning",
        icon: "warning"
      }).then(result => {
        if (result == "yes")
          this.setState({ show: false });
      });
    } else {
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

    if(pack == null){
      return (
        <Modal size="lg" show={this.state.show} onExited={this.handleOnExited} onHide={this.handleCloseClicked} className="sf-popup-control" >
          <ModalHeaderButtons
            onClose={this.handleCloseClicked} stickyHeader={settings?.stickyHeader}>
            <span className="sf-entity-title">{JavascriptMessage.loading.niceToString()}</span>
          </ModalHeaderButtons>
        </Modal>
      );
    }

    var mainEntity = pack.activity.case.mainEntity;
    var settings = mainEntity && Navigator.getSettings(mainEntity.Type);

    var { activity, canExecuteActivity, canExecuteMainEntity, ...extension } = this.state.pack!;

    var activityFrame: EntityFrame = {
      tabs: undefined,
      frameComponent: this,
      entityComponent: this.entityComponent,
      pack: pack && { entity: pack.activity, canExecute: pack.canExecuteActivity },
      onReload: (newPack, reloadComponent, callback) => {
        if (newPack) {
          pack!.activity = newPack.entity as CaseActivityEntity;
          pack!.canExecuteActivity = newPack.canExecute;
        }
        this.setState({ refreshCount: this.state.refreshCount + 1 }, callback);
      },
      onClose: (pack?: EntityPack<ModifiableEntity>) => this.props.onExited!(this.getCaseActivity()),
      revalidate: () => {
        this.validationErrorsTop && this.validationErrorsTop.forceUpdate();
        this.validationErrorsBottom && this.validationErrorsBottom.forceUpdate();
      },
      setError: (modelState, initialPrefix) => {
        GraphExplorer.setModelState(pack!.activity, modelState, initialPrefix || "");
        this.forceUpdate();
      },
      refreshCount: this.state.refreshCount,
      allowExchangeEntity: false,
      prefix: this.prefix,
      isExecuting: () => this.state.executing == true,
      execute: async action => {
        if (this.state.executing)
          return;

        this.setState({ executing: true });
        try {
          await action();
        } finally {
          this.setState({ executing: undefined });
        }
      }
    };

    var activityPack = { entity: pack.activity, canExecute: pack.canExecuteActivity };

    const mainFrame: EntityFrame | undefined = pack && {
      tabs: undefined,
      frameComponent: this,
      entityComponent: this.entityComponent,
      pack: { entity: pack.activity.case.mainEntity, canExecute: pack.canExecuteMainEntity, ...extension },
      onReload: (newPack, reloadComponent, callback) => {
        if (newPack) {
          pack!.activity.case.mainEntity = newPack.entity as CaseActivityEntity;
          pack!.canExecuteMainEntity = newPack.canExecute;
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
      allowExchangeEntity: false,
      prefix: this.prefix,
      isExecuting: () => this.state.executing == true,
      execute: async action => {
        if (this.state.executing)
          return;

        this.setState({ executing: true });
        try {
          await action();
        } finally {
          this.setState({ executing: undefined })
        }
      }
    };

    var mainEntity = pack.activity.case.mainEntity;

    var ti = getTypeInfo(pack.activity.case.mainEntity.Type);

    const styleOptions: StyleOptions = {
      readOnly: Navigator.isReadOnly(ti) || Boolean(pack.activity.doneDate),
      frame: mainFrame
    };

    const ctx = new TypeContext<ICaseMainEntity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(mainEntity, this.prefix));

    return (
      <Modal size="lg" show={this.state.show} onExited={this.handleOnExited} onHide={this.handleCloseClicked} className="sf-popup-control">
        <ModalHeaderButtons
          onClose={this.handleCloseClicked} stickyHeader={settings?.stickyHeader}>
          {this.renderTitle(mainFrame, pack, ctx)}
        </ModalHeaderButtons>

        <div className="case-activity-widgets mt-2 me-2">
          {!pack.activity.case.isNew && <div className="mx-2"> <InlineCaseTags case={toLite(pack.activity.case)} avoidHideIcon={true} /></div>}
          {!pack.activity.case.isNew && AuthClient.isPermissionAuthorized(WorkflowPermission.ViewCaseFlow) && <CaseFlowButton caseActivity={pack.activity} />}
        </div>
        <CaseFromSenderInfo current={pack.activity} />
        <div className="modal-body">
          <div className="sf-main-control" data-refresh-count={this.state.refreshCount} data-activity-entity={entityInfo(pack.activity)}>
            <div className="sf-main-entity case-main-entity" style={this.state.executing == true ? { opacity: ".7" } : undefined} data-main-entity={entityInfo(mainEntity)}>
              <div className="sf-button-widget-container">
                {this.entityComponent && !mainEntity.isNew && !pack.activity.doneBy ? <ButtonBar ref={bb => this.buttonBar = bb} frame={mainFrame} pack={mainFrame.pack} /> : <br />}
              </div>
              <ValidationErrors entity={mainEntity} ref={ve => this.validationErrorsTop = ve} prefix={this.prefix} />
              <ErrorBoundary>
                {this.state.getComponent && <AutoFocus>{FunctionalAdapter.withRef(this.state.getComponent(ctx), c => this.setComponent(c))}</AutoFocus>}
              </ErrorBoundary>
              <br />
              <ValidationErrors entity={mainEntity} ref={ve => this.validationErrorsBottom = ve} prefix={this.prefix} />
            </div>
          </div>
          {this.entityComponent && <CaseButtonBar frame={activityFrame} pack={activityPack} />}
        </div>

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

  validationErrorsTop?: ValidationErrorsHandle | null;
  validationErrorsBottom?: ValidationErrorsHandle | null;


  renderTitle(mainFrame: EntityFrame, pack: WorkflowClient.CaseEntityPack, ctx: TypeContext<ICaseMainEntity>) {

    var mainEntity = pack.activity.case.mainEntity;

    const wc: WidgetContext<ICaseMainEntity> = {
      ctx: ctx,
      frame: mainFrame,
    };

    const widgets = renderWidgets(wc, settings?.stickyHeader);
    const subTitle = Navigator.getTypeSubTitle(pack.activity, undefined);
    var settings = mainEntity && Navigator.getSettings(mainEntity.Type);

    return (
      <div>
        <span className="sf-entity-title">{this.props.title || getToString(pack.activity)}</span>&nbsp;
        {this.renderExpandLink(pack)}
        {
          (subTitle || widgets) &&
          <div className="sf-entity-sub-title">
            {subTitle && <small className="sf-type-nice-name text-muted"> {subTitle}</small>}
            {widgets}
          </div>
        }
      </div>
    );
  }

  renderExpandLink(pack: WorkflowClient.CaseEntityPack) {
    const activity = pack.activity;

    if (activity == null || activity.isNew)
      return null;

    const ti = getTypeInfo(activity.Type);

    if (!Navigator.isViewable(ti, { buttons: "close" })) //Embedded
      return null;

    return (
      <a href="#" className="sf-popup-fullscreen" onClick={this.handlePopupFullScreen}>
        <FontAwesomeIcon icon="up-right-from-square" />
      </a>
    );
  }

  handlePopupFullScreen = (e: React.MouseEvent<any>) => {
    AppContext.pushOrOpenInTab("~/workflow/activity/" + this.state.pack!.activity.id, e);
  }

  static openView(entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | WorkflowClient.CaseEntityPack, options?: Navigator.ViewOptions): Promise<CaseActivityEntity | undefined> {

    return openModal<CaseActivityEntity>(<CaseFrameModal
      entityOrPack={entityOrPack}
      readOnly={options?.readOnly ?? false}
    />);
  }
}

