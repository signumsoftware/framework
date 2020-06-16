import * as React from 'react'
import { TypeContext, StyleOptions, EntityFrame } from '@framework/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import { Entity, JavascriptMessage, entityInfo, getToString, toLite, EntityPack } from '@framework/Signum.Entities'
import { renderWidgets, WidgetContext } from '@framework/Frames/Widgets'
import { ValidationErrors, ValidationErrorHandle } from '@framework/Frames/ValidationErrors'
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
import { FunctionalAdapter } from '@framework/Frames/FrameModal';
import * as AuthClient from '../../Authorization/AuthClient'

interface CaseFramePageProps extends RouteComponentProps<{ workflowId: string; mainEntityStrategy: string; caseActivityId?: string }> {
}

interface CaseFramePageState {
  pack?: WorkflowClient.CaseEntityPack;
  getComponent?: (ctx: TypeContext<Entity>) => React.ReactElement<any>;
  refreshCount: number;
}

export default class CaseFramePage extends React.Component<CaseFramePageProps, CaseFramePageState> implements IHasCaseActivity {
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
    Navigator.setTitle();
    window.removeEventListener("keydown", this.hanldleKeyDown);
  }

  hanldleKeyDown = (e: KeyboardEvent) => {
    if (!e.openedModals && this.buttonBar)
      this.buttonBar.handleKeyDown(e);
  }

  load(props: CaseFramePageProps) {
    this.loadEntity(props)
      .then(() => this.state.pack && Navigator.setTitle(this.state.pack!.activity.case.toStr))
      .then(() => this.loadComponent())
      .done();
  }

  loadEntity(props: CaseFramePageProps): Promise<void> {

    const routeParams = props.match.params;
    if (routeParams.caseActivityId) {
      return WorkflowClient.API.fetchActivityForViewing({ EntityType: CaseActivityEntity.typeName, id: routeParams.caseActivityId })
        .then(pack => this.setState({ pack: pack, refreshCount: 0 }));

    } else if (routeParams.workflowId) {
      const ti = getTypeInfo(WorkflowEntity);
      return WorkflowClient.createNewCase(parseId(ti, routeParams.workflowId), (routeParams.mainEntityStrategy as WorkflowMainEntityStrategy))
        .then(pack => {
          if (!pack)
            Navigator.history.goBack();
          else
            this.setState({ pack, refreshCount: 0 });
        });

    } else
      throw new Error("No caseActivityId or workflowId set");
  }

  loadComponent(): Promise<void> {
    if (!this.state.pack)
      return Promise.resolve(undefined);

    return WorkflowClient.getViewPromiseCompoment(this.state.pack!.activity)
      .then(c => this.setState({ getComponent: c }));
  }

  onClose() {
    Navigator.history.push(WorkflowClient.getDefaultInboxUrl());
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
          {this.renderTitle()}
        </div>
      );
    }

    var pack = this.state.pack;

    const activityFrame: EntityFrame = {
      frameComponent: this,
      entityComponent: this.entityComponent,
      pack: pack && { entity: pack.activity, canExecute: pack.canExecuteActivity },
      onReload: (newPack, reloadComponent, callback) => {
        if (newPack) {
          let newActivity = newPack.entity as CaseActivityEntity;
          if (pack.activity.isNew && !newActivity.isNew) {
            Navigator.history.push("~/workflow/activity/" + newActivity.id);
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
      allowChangeEntity: false,
    };


    var activityPack = { entity: pack.activity, canExecute: pack.canExecuteActivity };

    return (
      <div className="normal-control">
        {this.renderTitle()}
        <CaseFromSenderInfo current={pack.activity} />
        {!pack.activity.case.isNew && <div className="inline-tags"> <InlineCaseTags case={toLite(pack.activity.case)} /></div>}
        <div className="sf-main-control" data-test-ticks={new Date().valueOf()} data-activity-entity={entityInfo(pack.activity)}>
          {this.renderMainEntity()}
        </div>
        {this.entityComponent && <CaseButtonBar frame={activityFrame} pack={activityPack} />}
      </div>
    );
  }

  renderTitle() {

    if (!this.state.pack)
      return <h3>{JavascriptMessage.loading.niceToString()}</h3>;

    const activity = this.state.pack.activity;

    return (
      <h3>
        {!activity.case.isNew && AuthClient.isPermissionAuthorized(WorkflowPermission.ViewCaseFlow) &&
          <CaseFlowButton caseActivity={this.state.pack.activity} />}
        <span className="sf-entity-title">{getToString(activity)}</span>
        <br />
        <small className="sf-type-nice-name text-muted">{Navigator.getTypeTitle(activity, undefined)}</small>
      </h3>
    );
  }

  validationErrorsTop?: ValidationErrorHandle | null;
  validationErrorsBottom?: ValidationErrorHandle | null;

  getMainTypeInfo(): TypeInfo {
    return getTypeInfo(this.state.pack!.activity.case.mainEntity.Type);
  }

  renderMainEntity() {

    var pack = this.state.pack!;
    var mainEntity = pack.activity.case.mainEntity;
    const mainFrame: EntityFrame = {
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
      allowChangeEntity: false,
    };

    var ti = this.getMainTypeInfo();

    const styleOptions: StyleOptions = {
      readOnly: Navigator.isReadOnly(ti) || Boolean(pack.activity.doneDate),
      frame: mainFrame
    };

    const ctx = new TypeContext<ICaseMainEntity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(mainEntity, "caseFrame"));

    var { activity, canExecuteActivity, canExecuteMainEntity, ...extension } = this.state.pack!;

    const wc: WidgetContext<ICaseMainEntity> = {
      ctx: ctx,
      frame: mainFrame,
    };

    return (
      <div className="sf-main-entity case-main-entity" data-main-entity={entityInfo(mainEntity)}>
        {renderWidgets(wc)}
        {this.entityComponent && !mainEntity.isNew && !pack.activity.doneBy ? <ButtonBar ref={a => this.buttonBar = a} frame={mainFrame} pack={mainFrame.pack} /> : <br />}
        <ValidationErrors entity={mainEntity} ref={ve => this.validationErrorsTop = ve} prefix="caseFrame"/>
        <ErrorBoundary>
          {this.state.getComponent && <AutoFocus>{FunctionalAdapter.withRef(this.state.getComponent(ctx), c => this.setComponent(c))}</AutoFocus>}
        </ErrorBoundary>
        <br />
        <ValidationErrors entity={mainEntity} ref={ve => this.validationErrorsBottom = ve} prefix="caseFrame" />
      </div>
    );
  }
}

