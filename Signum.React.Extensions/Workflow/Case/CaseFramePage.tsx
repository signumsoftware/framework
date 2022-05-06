import * as React from 'react'
import { TypeContext, StyleOptions, EntityFrame } from '@framework/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '@framework/Reflection'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import { Entity, JavascriptMessage, entityInfo, getToString, toLite, EntityPack } from '@framework/Signum.Entities'
import { renderWidgets, WidgetContext } from '@framework/Frames/Widgets'
import { ValidationErrors, ValidationErrorsHandle } from '@framework/Frames/ValidationErrors'
import { ButtonBar, ButtonBarHandle } from '@framework/Frames/ButtonBar'
import { CaseActivityEntity, WorkflowEntity, ICaseMainEntity, WorkflowMainEntityStrategy, WorkflowActivityEntity, WorkflowPermission } from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'
import CaseFromSenderInfo from './CaseFromSenderInfo'
import CaseButtonBar from './CaseButtonBar'
import CaseFlowButton from './CaseFlowButton'
import InlineCaseTags from './InlineCaseTags'
import { RouteComponentProps } from "react-router";
import { IHasCaseActivity } from '../WorkflowClient';
import { ErrorBoundary } from '@framework/Components';
import "@framework/Frames/Frames.css"
import "./CaseAct.css"
import { AutoFocus } from '@framework/Components/AutoFocus';
import * as AuthClient from '../../Authorization/AuthClient'
import { FunctionalAdapter } from '@framework/Modals'

interface CaseFramePageProps extends RouteComponentProps<{ workflowId: string; mainEntityStrategy: string; caseActivityId?: string }> {
}

interface CaseFramePageState {
  pack?: WorkflowClient.CaseEntityPack;
  getComponent?: (ctx: TypeContext<Entity>) => React.ReactElement<any>;
  refreshCount: number;
  executing?: boolean;
}

export default class CaseFramePage extends React.Component<CaseFramePageProps, CaseFramePageState> implements IHasCaseActivity {
  static showSubTitle = true;

  constructor(props: any) {
    super(props);
    this.state = this.calculateState(props);
  }

  getCaseActivity(): CaseActivityEntity | undefined {
    return this.state.pack && this.state.pack.activity;
  }

  componentWillMount() {
    this.load(this.props);
  }

  calculateState(props: CaseFramePageProps): CaseFramePageState {
    return { getComponent: undefined, refreshCount: 0 };
  }

  componentWillReceiveProps(newProps: CaseFramePageProps) {
    this.setState(this.calculateState(newProps), () => {
      this.load(newProps);
    });
  }

  componentDidMount() {
    window.addEventListener("keydown", this.hanldleKeyDown);
  }

  componentWillUnmount() {
    AppContext.setTitle();
    window.removeEventListener("keydown", this.hanldleKeyDown);
  }

  hanldleKeyDown = (e: KeyboardEvent) => {
    if (!e.openedModals && this.buttonBar)
      this.buttonBar.handleKeyDown(e);
  }

  load(props: CaseFramePageProps) {
    this.loadEntity(props)
      .then(pack => {
        if (pack) {

          this.setState({ pack: pack, refreshCount: 0 });
          AppContext.setTitle(pack.activity.case.toStr);
          this.loadComponent(pack);

        } else {
          AppContext.history.goBack();
        }
      })
      .done();
  }

  loadEntity(props: CaseFramePageProps): Promise<WorkflowClient.CaseEntityPack | undefined> {

    const routeParams = props.match.params;
    if (routeParams.caseActivityId) {
      return WorkflowClient.API.fetchActivityForViewing({ EntityType: CaseActivityEntity.typeName, id: routeParams.caseActivityId })

    } else if (routeParams.workflowId) {
      const ti = getTypeInfo(WorkflowEntity);
      return WorkflowClient.createNewCase(parseId(ti, routeParams.workflowId), (routeParams.mainEntityStrategy as WorkflowMainEntityStrategy));

    } else
      throw new Error("No caseActivityId or workflowId set");
  }

  loadComponent(pack: WorkflowClient.CaseEntityPack): Promise<void> {
    return WorkflowClient.getViewPromiseCompoment(pack.activity)
      .then(c => this.setState({ getComponent: c }));
  }

  onClose() {
    AppContext.history.push(WorkflowClient.getDefaultInboxUrl());
  }

  entityComponent?: React.Component<any, any> | null;

  setComponent(c: React.Component<any, any> | null) {
    if (c && this.entityComponent != c) {
      this.entityComponent = c;
      this.forceUpdate();
    }
  }

  buttonBar?: ButtonBarHandle | null;

  render() {

    if (!this.state.pack) {
      return (
        <div className="normal-control">
          <h3 className="border-bottom pb-3">{JavascriptMessage.loading.niceToString()}</h3>
        </div>
      );
    }

    var pack = this.state.pack;

    const activityFrame: EntityFrame = {
      tabs: undefined,
      frameComponent: this,
      entityComponent: this.entityComponent,
      pack: pack && { entity: pack.activity, canExecute: pack.canExecuteActivity },
      onReload: (newPack, reloadComponent, callback) => {
        if (newPack) {
          let newActivity = newPack.entity as CaseActivityEntity;
          if (pack.activity.isNew && !newActivity.isNew) {
            AppContext.history.push("~/workflow/activity/" + newActivity.id);
            return;
          }
          else {
            pack.activity = newActivity;
            pack.canExecuteActivity = newPack.canExecute;
          }
        }
        this.setState({ refreshCount: this.state.refreshCount + 1 }, callback);
      },
      onClose: () => this.onClose(),
      revalidate: () => { throw new Error("Not implemented"); },
      setError: (ms, initialPrefix) => {
        GraphExplorer.setModelState(pack.activity, ms, initialPrefix ?? "");
        this.forceUpdate()
      },
      refreshCount: this.state.refreshCount,
      allowExchangeEntity: false,
      prefix: "caseFrame",
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

    var mainEntity = pack.activity.case.mainEntity;

    const mainFrame: EntityFrame = {
      tabs: undefined,
      frameComponent: this,
      entityComponent: this.entityComponent,
      pack: pack && { entity: pack.activity.case.mainEntity, canExecute: pack.canExecuteMainEntity },
      onReload: (newPack, reloadComponent, callback) => {
        if (newPack) {
          pack.activity.case.mainEntity = newPack.entity as ICaseMainEntity;
          pack.canExecuteMainEntity = newPack.canExecute;
        }
        this.setState({ refreshCount: this.state.refreshCount + 1 }, callback);
      },
      onClose: () => this.onClose(),
      revalidate: () => {
        this.validationErrorsTop && this.validationErrorsTop.forceUpdate();
        this.validationErrorsBottom && this.validationErrorsBottom.forceUpdate();
      },
      setError: (ms, initialPrefix) => {
        GraphExplorer.setModelState(mainEntity, ms, initialPrefix ?? "");
        this.forceUpdate()
      },
      refreshCount: this.state.refreshCount,
      allowExchangeEntity: false,
      prefix: "caseFrame",
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

    var ti = getTypeInfo(pack.activity.case.mainEntity.Type);

    const styleOptions: StyleOptions = {
      readOnly: Navigator.isReadOnly(ti) || Boolean(pack.activity.doneDate),
      frame: mainFrame
    };

    const ctx = new TypeContext<ICaseMainEntity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(mainEntity, "caseFrame"));

    var activityPack = { entity: pack.activity, canExecute: pack.canExecuteActivity };

    return (
      <div className="normal-control">
        {this.renderTitle(mainFrame, pack, ctx)}
        <div className="case-activity-widgets mt-2 me-2">
          {!pack.activity.case.isNew && <div className="mx-2"> <InlineCaseTags case={toLite(pack.activity.case)} avoidHideIcon={true} /></div>}
          {!pack.activity.case.isNew && AuthClient.isPermissionAuthorized(WorkflowPermission.ViewCaseFlow) && <CaseFlowButton caseActivity={pack.activity} />}
        </div>
        <div className="sf-main-control" data-refresh-count={this.state.refreshCount} data-activity-entity={entityInfo(pack.activity)}>
          {this.renderMainEntity(mainFrame, pack, ctx)}
        </div>
        {this.entityComponent && <CaseButtonBar frame={activityFrame} pack={activityPack} />}
      </div>
    );
  }

  renderTitle(mainFrame: EntityFrame, pack: WorkflowClient.CaseEntityPack, ctx: TypeContext<ICaseMainEntity>) {

    var mainEntity = pack.activity.case.mainEntity;

    const wc: WidgetContext<ICaseMainEntity> = {
      ctx: ctx,
      frame: mainFrame,
    };

    const widgets = renderWidgets(wc, settings?.stickyHeader);
    const subTitle = CaseFramePage.showSubTitle ? Navigator.getTypeSubTitle(pack.activity, undefined) : undefined;
    var settings = mainEntity && Navigator.getSettings(mainEntity.Type);

    return (
      <h3 className="border-bottom pb-3">
        <span className="sf-entity-title">{getToString(pack.activity)}</span>
        {
          (subTitle || widgets) &&
          <div className="sf-entity-sub-title">
            {subTitle && <small className="sf-type-nice-name text-muted"> {subTitle}</small>}
            {widgets}
          </div>
        }
      </h3>
    );
  }

  validationErrorsTop?: ValidationErrorsHandle | null;
  validationErrorsBottom?: ValidationErrorsHandle | null;



  renderMainEntity(mainFrame: EntityFrame, pack: WorkflowClient.CaseEntityPack, ctx: TypeContext<ICaseMainEntity>) {

    var mainEntity = pack.activity.case.mainEntity;

    return (
      <div className="sf-main-entity case-main-entity" style={this.state.executing == true ? { opacity: ".7" } : undefined} data-main-entity={entityInfo(mainEntity)}>
        <div className="sf-button-widget-container">
          {this.entityComponent && !mainEntity.isNew && !pack.activity.doneBy ? <ButtonBar ref={a => this.buttonBar = a} frame={mainFrame} pack={mainFrame.pack} /> : <br />}
        </div>
        <ValidationErrors entity={mainEntity} ref={ve => this.validationErrorsTop = ve} prefix="caseFrame" />
        <ErrorBoundary>
          {this.state.getComponent && <AutoFocus>{FunctionalAdapter.withRef(this.state.getComponent(ctx), c => this.setComponent(c))}</AutoFocus>}
        </ErrorBoundary>
        <br />
        <ValidationErrors entity={mainEntity} ref={ve => this.validationErrorsBottom = ve} prefix="caseFrame" />
      </div>
    );
  }
}


