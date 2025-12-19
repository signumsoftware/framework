import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { openModal, IModalProps, IHandleKeyboard } from '@framework/Modals'
import { TypeContext, StyleOptions, EntityFrame, FunctionalFrameComponent } from '@framework/TypeContext'
import { TypeInfo, getTypeInfo, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '@framework/Reflection'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import MessageModal from '@framework/Modals/MessageModal'
import { Lite, JavascriptMessage, entityInfo, getToString, toLite, EntityPack, ModifiableEntity, SaveChangesMessage, FrameMessage } from '@framework/Signum.Entities'
import { renderWidgets, WidgetContext } from '@framework/Frames/Widgets'
import { ValidationErrors, ValidationErrorsHandle } from '@framework/Frames/ValidationErrors'
import { ButtonBar, ButtonBarHandle } from '@framework/Frames/ButtonBar'
import { CaseActivityEntity, ICaseMainEntity, WorkflowActivityEntity, WorkflowPermission } from '../Signum.Workflow'
import CaseFromSenderInfo from './CaseFromSenderInfo'
import CaseButtonBar from './CaseButtonBar'
import CaseFlowButton from './CaseFlowButton'
import InlineCaseTags from './InlineCaseTags'
import { WorkflowClient } from '../WorkflowClient';
import { ErrorBoundary, ModalHeaderButtons } from '@framework/Components';
import { Modal } from 'react-bootstrap';
import "@framework/Frames/Frames.css"
import "./CaseAct.css"
import { AutoFocus } from '@framework/Components/AutoFocus';
import { FunctionalAdapter } from '@framework/Modals';
import { AuthClient } from '../../Signum.Authorization/AuthClient'
import { useForceUpdate, useStateWithPromise } from '@framework/Hooks'
import { LinkButton } from '@framework/Basics/LinkButton'

interface CaseFrameModalProps extends IModalProps<CaseActivityEntity | undefined> {
  title?: string;
  entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | WorkflowClient.CaseEntityPack;
  avoidPromptLooseChange?: boolean;
  readOnly?: boolean;
}

interface CaseFrameModalState {
  pack: WorkflowClient.CaseEntityPack;
  lastActivity: string;
  getComponent: (ctx: TypeContext<ICaseMainEntity>) => React.ReactElement<any>;
  refreshCount: number;
  executing?: boolean;
}

var modalCount = 0;

export const CaseFrameModal: React.ForwardRefExoticComponent<CaseFrameModalProps & React.RefAttributes<IHandleKeyboard>> =
  React.forwardRef(function CaseFrameModal(p: CaseFrameModalProps, ref: React.Ref<IHandleKeyboard>) {

  const [state, setState] = useStateWithPromise<CaseFrameModalState | undefined>(undefined);
  const [show, setShow] = React.useState(true);
  const prefix = React.useMemo(() => "caseModal" + (modalCount++), []);
  const okClicked = React.useRef(false);
  const buttonBarRef = React.useRef<ButtonBarHandle>(null);
  const entityComponentRef = React.useRef<React.Component | null>(null);
  const validationErrorsTop = React.useRef<ValidationErrorsHandle>(null);
  const validationErrorsBottom = React.useRef<ValidationErrorsHandle>(null);
  const frameRef = React.useRef<EntityFrame | undefined>(undefined);

  const forceUpdate = useForceUpdate();

  const [errorsPosition, setErrorsPosition] = React.useState<"top" | "bottom">("top");

  React.useImperativeHandle(ref, () => ({
    handleKeyDown(e: KeyboardEvent) {
      buttonBarRef.current && buttonBarRef.current.handleKeyDown(e);
    },
   
  }));



  React.useEffect(() => {
    WorkflowClient.toEntityPackWorkflow(p.entityOrPack)
      .then(pack => loadComponent(pack).then(getComponent => setPack(pack, getComponent)))

  }, [p.entityOrPack]);

  function setPack(pack: WorkflowClient.CaseEntityPack, getComponent: (ctx: TypeContext<ICaseMainEntity>) => React.ReactElement<any>, callback?: () => void) {
    setState({
      pack,
      lastActivity: JSON.stringify(pack.activity),
      getComponent,
      refreshCount: state ? state.refreshCount + 1 : 0
    }).then(callback);
  }

  function loadComponent(pack: WorkflowClient.CaseEntityPack): Promise<(ctx: TypeContext<ICaseMainEntity>) => React.ReactElement> {
    const ca = pack.activity;

    return WorkflowClient.getViewPromiseCompoment(ca);
  }

  function hasChanges() {

    var entity = state!.pack.activity;

    GraphExplorer.propagateAll(entity);

    return entity.modified;
  }

  function handleCloseClicked() {
    if (hasChanges() && !p.avoidPromptLooseChange) {
      MessageModal.show({
        title: SaveChangesMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.loseCurrentChanges.niceToString(),
        buttons: "yes_no",
        style: "warning",
        icon: "warning"
      }).then(result => {
        if (result == "yes")
          setShow(false);
      });
    } else {
      setShow(false);
    }
  }

  function handleOnExited() {
    p.onExited!(okClicked ? state?.pack?.activity : undefined);
  }

  function setComponent(c: React.Component | null) {
    if (c && entityComponentRef.current != c) {
      entityComponentRef.current = c;
      forceUpdate();
    }
  }

  function renderTitle(mainFrame: EntityFrame, pack: WorkflowClient.CaseEntityPack, ctx: TypeContext<ICaseMainEntity>) {

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
        <span className="sf-entity-title">{p.title || Navigator.renderEntity(pack.activity)}</span>&nbsp;
        {renderExpandLink(pack)}
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

  function renderExpandLink(pack: WorkflowClient.CaseEntityPack) {
    const activity = pack.activity;

    if (activity == null || activity.isNew)
      return null;

    const ti = getTypeInfo(activity.Type);

    if (!Navigator.isViewable(ti, { buttons: "close" })) //Embedded
      return null;

    return (
      <LinkButton title={undefined} className="sf-popup-fullscreen" onClick={handlePopupFullScreen} > 
        <FontAwesomeIcon icon="up-right-from-square" title={FrameMessage.Fullscreen.niceToString()}/>
      </LinkButton>
    );
  }

  function handlePopupFullScreen(e: React.MouseEvent<any>) {
    AppContext.pushOrOpenInTab("/workflow/activity/" + state?.pack!.activity.id, e);
  }


  if (state == null) {
    return (
      <Modal size="lg" show={show} onExited={handleOnExited} onHide={handleCloseClicked} className="sf-popup-control" >
        <ModalHeaderButtons
          onClose={handleCloseClicked} stickyHeader={settings?.stickyHeader}>
          <span className="sf-entity-title">{JavascriptMessage.loading.niceToString()}</span>
        </ModalHeaderButtons>
      </Modal>
    );
  }

  var pack = state.pack;
  var mainEntity = pack.activity.case.mainEntity;
  var settings = mainEntity && Navigator.getSettings(mainEntity.Type);

  var { activity, canExecuteActivity, canExecuteMainEntity, ...extension } = pack;

  var frameComponent: FunctionalFrameComponent & WorkflowClient.IHasCaseActivity = {
    forceUpdate,
    type: CaseFrameModalExt,
    getCaseActivity(): CaseActivityEntity | undefined {
      return state?.pack?.activity;
    }
  };

  var activityFrame: EntityFrame = {
    tabs: undefined,
    frameComponent: frameComponent,
    entityComponent: entityComponentRef.current,
    pack: pack && { entity: pack.activity, canExecute: pack.canExecuteActivity },
    onReload: (newPack, reloadComponent, callback) => {
      if (newPack) {
        pack!.activity = newPack.entity as CaseActivityEntity;
        pack!.canExecuteActivity = newPack.canExecute;
      }
      loadComponent(pack!)
        .then(getComponent => setPack(pack!, getComponent, callback));
    },
    onClose: (newPack?: EntityPack<ModifiableEntity>) => p.onExited!(pack!.activity),
    revalidate: () => {
      validationErrorsTop.current?.forceUpdate();
      validationErrorsBottom.current?.forceUpdate();
    },
    setError: (modelState, initialPrefix) => {
      GraphExplorer.setModelState(pack!.activity, modelState, initialPrefix || "");
      setErrorsPosition("bottom");
      forceUpdate();
    },
    refreshCount: state.refreshCount,
    allowExchangeEntity: false,
    prefix: prefix,
    isExecuting: () => state.executing == true,
    execute: async action => {
      if (state.executing)
        return;

      state.executing = true;
      forceUpdate();
      try {
        await action();

      } finally {
        state.executing = undefined;
        forceUpdate();
      }
    }
  };

  var activityPack = { entity: pack.activity, canExecute: pack.canExecuteActivity };

  const mainFrame: EntityFrame | undefined = pack && {
    tabs: undefined,
    frameComponent: frameComponent,
    entityComponent: entityComponentRef.current,
    pack: { entity: pack.activity.case.mainEntity, canExecute: pack.canExecuteMainEntity, ...extension },
    onReload: (newPack, reloadComponent, callback) => {
      if (newPack) {
        pack!.activity.case.mainEntity = newPack.entity as CaseActivityEntity;
        pack!.canExecuteMainEntity = newPack.canExecute;
      }
      loadComponent(pack!)
        .then(getComponent => setPack(pack!, getComponent, callback));
    },
    onClose: () => p.onExited!(undefined),
    revalidate: () => {
      validationErrorsTop.current?.forceUpdate();
      validationErrorsBottom.current?.forceUpdate();
    },
    setError: (ms, initialPrefix) => {
      GraphExplorer.setModelState(mainEntity, ms, initialPrefix || "");
      setErrorsPosition("top");
      forceUpdate()
    },
    refreshCount: state.refreshCount,
    allowExchangeEntity: false,
    prefix: prefix,
    isExecuting: () => state.executing == true,
    execute: async action => {
      if (state.executing)
        return;

      state.executing = true;
      forceUpdate();
      try {
        await action();

      } finally {
        state.executing = undefined;
        forceUpdate();
      }
    }
  };

  var mainEntity = pack.activity.case.mainEntity;

  var ti = getTypeInfo(pack.activity.case.mainEntity.Type);

  const styleOptions: StyleOptions = {
    readOnly: Navigator.isReadOnly(ti) || Boolean(pack.activity.doneDate),
    frame: mainFrame
  };

  const ctx = new TypeContext<ICaseMainEntity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(mainEntity, prefix));

  return (
    <Modal size="lg" show={show} onExited={handleOnExited} onHide={handleCloseClicked} className="sf-popup-control">
      <ModalHeaderButtons
        onClose={handleCloseClicked} stickyHeader={settings?.stickyHeader}>
        {renderTitle(mainFrame, pack, ctx)}
      </ModalHeaderButtons>

      <div className="case-activity-widgets mt-2 me-2">
        {!pack.activity.case.isNew && <div className="mx-2"> <InlineCaseTags case={toLite(pack.activity.case)} avoidHideIcon={true} /></div>}
        {!pack.activity.case.isNew && AppContext.isPermissionAuthorized(WorkflowPermission.ViewCaseFlow) && <CaseFlowButton caseActivity={pack.activity} />}
      </div>
      <CaseFromSenderInfo current={pack.activity} />
      <div className="modal-body">
        <div className="sf-main-control" data-refresh-count={state.refreshCount} data-activity-entity={entityInfo(pack.activity)}>
          <div className="sf-main-entity case-main-entity" style={state.executing ? {opacity: ".7" } : undefined} data-main-entity={entityInfo(mainEntity)}>
            <div className="sf-button-widget-container">
              {entityComponentRef.current && !mainEntity.isNew && !pack.activity.doneBy ? <ButtonBar ref={buttonBarRef} frame={mainFrame} pack={mainFrame.pack} /> : <br />}
            </div>
            {errorsPosition == "top" && <ValidationErrors entity={mainEntity} ref={validationErrorsTop} prefix="caseFrame" />}
            <ErrorBoundary>
              {state.getComponent && <AutoFocus>{FunctionalAdapter.withRef(state.getComponent(ctx), c => setComponent(c))}</AutoFocus>}
            </ErrorBoundary>
            <br />
            {errorsPosition == "bottom" && <ValidationErrors entity={mainEntity} ref={validationErrorsBottom} prefix="caseFrame" />}
          </div>
        </div>
        {entityComponentRef.current && <CaseButtonBar frame={activityFrame} pack={activityPack} />}
      </div>

    </Modal>
  );
});

const CaseFrameModalExt = CaseFrameModal;

export namespace CaseFrameModalManager {
  export function openView(entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | WorkflowClient.CaseEntityPack, options?: { readOnly?: boolean }): Promise<CaseActivityEntity | undefined> {

    return openModal<CaseActivityEntity>(<CaseFrameModal
      entityOrPack={entityOrPack}
      readOnly={options?.readOnly ?? false}
    />);
  }
}


