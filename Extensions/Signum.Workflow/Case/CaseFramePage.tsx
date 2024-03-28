import * as React from 'react'
import { useLocation, useParams } from 'react-router'
import { TypeContext, StyleOptions, EntityFrame, FunctionalFrameComponent } from '@framework/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '@framework/Reflection'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { Entity, JavascriptMessage, entityInfo, getToString, toLite, EntityPack } from '@framework/Signum.Entities'
import { renderWidgets, WidgetContext } from '@framework/Frames/Widgets'
import { ValidationErrors, ValidationErrorsHandle } from '@framework/Frames/ValidationErrors'
import { ButtonBar, ButtonBarHandle } from '@framework/Frames/ButtonBar'
import { CaseActivityEntity, WorkflowEntity, ICaseMainEntity, WorkflowMainEntityStrategy, WorkflowActivityEntity, WorkflowPermission } from '../Signum.Workflow'
import { WorkflowClient } from '../WorkflowClient'
import CaseButtonBar from './CaseButtonBar'
import CaseFlowButton from './CaseFlowButton'
import InlineCaseTags from './InlineCaseTags'
import { ErrorBoundary } from '@framework/Components';
import "@framework/Frames/Frames.css"
import "./CaseAct.css"
import { AutoFocus } from '@framework/Components/AutoFocus';
import { AuthClient } from '../../Signum.Authorization/AuthClient'
import { FunctionalAdapter } from '@framework/Modals'
import { useForceUpdate, useStateWithPromise } from '@framework/Hooks'


interface CaseFramePageState {
  pack: WorkflowClient.CaseEntityPack;
  lastActivity: string;
  getComponent: (ctx: TypeContext<Entity>) => React.ReactElement<any>;
  refreshCount: number;
  executing?: boolean;
}

export default function CaseFramePage() {

  var params = useParams() as { workflowId: string; mainEntityStrategy: string; caseActivityId?: string };
  let [state, setState] = useStateWithPromise<CaseFramePageState | undefined>(undefined);

  const buttonBarRef = React.useRef<ButtonBarHandle>(null);
  const entityComponentRef = React.useRef<React.Component | null>(null);
  const validationErrorsTop = React.useRef<ValidationErrorsHandle>(null);
  const validationErrorsBottom = React.useRef<ValidationErrorsHandle>(null);
  const forceUpdate = useForceUpdate();
  
  React.useEffect(() => {

    function loadEntity(): Promise<WorkflowClient.CaseEntityPack | undefined> {

      if (params.caseActivityId) {
        return WorkflowClient.API.fetchActivityForViewing({ EntityType: CaseActivityEntity.typeName, id: params.caseActivityId })

      } else if (params.workflowId) {
        const ti = getTypeInfo(WorkflowEntity);
        return WorkflowClient.createNewCase(parseId(ti, params.workflowId), (params.mainEntityStrategy as WorkflowMainEntityStrategy));

      } else
        throw new Error("No caseActivityId or workflowId set");
    }

    loadEntity()
      .then(pack => {
        if (pack) {
          WorkflowClient.getViewPromiseCompoment(pack.activity)
            .then(c => setPack(pack, c));
        } else {
          AppContext.navigate(-1);
        }
      });

  }, [params.caseActivityId, params.workflowId, params.mainEntityStrategy]);

  function hanldleKeyDown(e: KeyboardEvent) {
    if (!e.openedModals && buttonBarRef.current)
      buttonBarRef.current.handleKeyDown(e);
  }

  React.useEffect(() => {
    window.addEventListener("keydown", hanldleKeyDown);

    return () => window.removeEventListener("keydown", hanldleKeyDown);
  }, []);

  AppContext.useTitle(state == null ? "" : getToString(state.pack.activity.case));

  function onClose() {
    AppContext.navigate(WorkflowClient.getDefaultInboxUrl());
  }

  function setComponent(c: React.Component<any, any> | null) {
    if (c && entityComponentRef.current != c) {
      entityComponentRef.current = c;
      forceUpdate();
    }
  }

  function setPack(pack: WorkflowClient.CaseEntityPack, getComponent: (ctx: TypeContext<ICaseMainEntity>) => React.ReactElement<any>, callback?: () => void) {
    setState({
      pack,
      lastActivity: JSON.stringify(pack.activity),
      getComponent,
      refreshCount: state ? state.refreshCount + 1 : 0
    }).then(callback);
  }

  if (!state) {
    return (
      <div className="normal-control">
        <h3 className="border-bottom pb-3">{JavascriptMessage.loading.niceToString()}</h3>
      </div>
    );
  }

  var pack = state.pack;

  var frameComponent: FunctionalFrameComponent & WorkflowClient.IHasCaseActivity = {
    forceUpdate,
    type: CaseFramePage,
    getCaseActivity(): CaseActivityEntity | undefined {
      return pack?.activity;
    }
  };

  const activityFrame: EntityFrame = {
    tabs: undefined,
    frameComponent: frameComponent,
    entityComponent: entityComponentRef.current,
    pack: pack && { entity: pack.activity, canExecute: pack.canExecuteActivity },
    onReload: (newPack, reloadComponent, callback) => {
      if (newPack) {
        let newActivity = newPack.entity as CaseActivityEntity;
        if (pack.activity.isNew && !newActivity.isNew) {
          AppContext.navigate("/workflow/activity/" + newActivity.id);
          return;
        }
        else {
          pack.activity = newActivity;
          pack.canExecuteActivity = newPack.canExecute;
        }
      }
      setPack(pack, state!.getComponent, callback);
    },
    onClose: () => onClose(),
    revalidate: () => { throw new Error("Not implemented"); },
    setError: (ms, initialPrefix) => {
      GraphExplorer.setModelState(pack.activity, ms, initialPrefix ?? "");
      forceUpdate()
    },
    refreshCount: state.refreshCount,
    allowExchangeEntity: false,
    prefix: "caseFrame",
    isExecuting: () => state!.executing == true,
    execute: async action => {
      if (state!.executing)
        return;

      state!.executing = true;
      forceUpdate();
      try {
        await action();
      } finally {
        state!.executing = undefined;
        forceUpdate();
      }
    }
  };

  var mainEntity = pack.activity.case.mainEntity;

  const mainFrame: EntityFrame = {
    tabs: undefined,
    frameComponent: frameComponent,
    entityComponent: entityComponentRef.current,
    pack: pack && { entity: pack.activity.case.mainEntity, canExecute: pack.canExecuteMainEntity },
    onReload: (newPack, reloadComponent, callback) => {
      if (newPack) {
        pack.activity.case.mainEntity = newPack.entity as ICaseMainEntity;
        pack.canExecuteMainEntity = newPack.canExecute;
      }
      setPack(pack, state!.getComponent, callback);
    },
    onClose: () => onClose(),
    revalidate: () => {
      validationErrorsTop && validationErrorsTop.current?.forceUpdate();
      validationErrorsBottom && validationErrorsBottom.current?.forceUpdate();
    },
    setError: (ms, initialPrefix) => {
      GraphExplorer.setModelState(mainEntity, ms, initialPrefix ?? "");
      forceUpdate()
    },
    refreshCount: state.refreshCount,
    allowExchangeEntity: false,
    prefix: "caseFrame",
    isExecuting: () => state!.executing == true,
    execute: async action => {
      if (state!.executing)
        return;

      state!.executing = true;
      forceUpdate();
      try {
        await action();

      } finally {
        state!.executing = undefined;
        forceUpdate();
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
      {renderTitle(mainFrame, pack, ctx)}
      <div className="case-activity-widgets mt-2 me-2">
        {!pack.activity.case.isNew && <div className="mx-2"> <InlineCaseTags case={toLite(pack.activity.case)} avoidHideIcon={true} /></div>}
        {!pack.activity.case.isNew && AppContext.isPermissionAuthorized(WorkflowPermission.ViewCaseFlow) && <CaseFlowButton caseActivity={pack.activity} />}
      </div>
      <div className="sf-main-control" data-refresh-count={state.refreshCount} data-activity-entity={entityInfo(pack.activity)}>
        <div className="sf-main-entity case-main-entity" style={state.executing == true ? { opacity: ".7" } : undefined} data-main-entity={entityInfo(mainEntity)}>
          <div className="sf-button-widget-container">
            {entityComponentRef.current && !mainEntity.isNew && !pack.activity.doneBy ? <ButtonBar ref={buttonBarRef} frame={mainFrame} pack={mainFrame.pack} /> : <br />}
          </div>
          <ValidationErrors entity={mainEntity} ref={validationErrorsTop} prefix="caseFrame" />
          <ErrorBoundary>
            {state.getComponent && <AutoFocus>{FunctionalAdapter.withRef(state.getComponent(ctx), c => setComponent(c))}</AutoFocus>}
          </ErrorBoundary>
          <br />
          <ValidationErrors entity={mainEntity} ref={validationErrorsBottom} prefix="caseFrame" />
        </div>
      </div>
      {entityComponentRef.current && <CaseButtonBar frame={activityFrame} pack={activityPack} />}
    </div>
  );

  function renderTitle(mainFrame: EntityFrame, pack: WorkflowClient.CaseEntityPack, ctx: TypeContext<ICaseMainEntity>) {

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
        <span className="sf-entity-title">{Navigator.renderEntity(pack.activity)}</span>
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
}

CaseFramePage.showSubTitle = true;
