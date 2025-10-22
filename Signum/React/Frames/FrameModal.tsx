
import * as React from 'react'
import { openModal, IModalProps, IHandleKeyboard, FunctionalAdapter } from '../Modals'
import MessageModal from '../Modals/MessageModal'
import { Navigator, ViewPromise } from '../Navigator'
import * as AppContext from '../AppContext';
import { ButtonBar, ButtonBarHandle } from './ButtonBar'
import { ValidationError } from '../Services'
import { classes, ifError } from '../Globals'
import { TypeContext, StyleOptions, EntityFrame, IHasChanges, ButtonsContext } from '../TypeContext'
import { Entity, Lite, ModifiableEntity, JavascriptMessage, FrameMessage, getToString, EntityPack, entityInfo, isEntityPack, isLite, is, isEntity, SaveChangesMessage, ModelEntity } from '../Signum.Entities'
import { getTypeInfo, PropertyRoute, ReadonlyBinding, GraphExplorer, isTypeModel, tryGetTypeInfo } from '../Reflection'
import { ValidationErrors, ValidationErrorsHandle } from './ValidationErrors'
import { renderWidgets, WidgetContext } from './Widgets'
import { Operations, EntityOperationContext } from '../Operations'
import { BsSize, ErrorBoundary } from '../Components';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import "./Frames.css"
import { AutoFocus } from '../Components/AutoFocus';
import { useStateWithPromise, useForceUpdate } from '../Hooks'
import { Modal } from 'react-bootstrap'
import { ModalFooterButtons, ModalHeaderButtons } from '../Components/ModalHeaderButtons'
import WidgetEmbedded from './WidgetEmbedded'
import SaveChangesModal from '../Modals/SaveChangesModal';

interface FrameModalProps<T extends ModifiableEntity> extends IModalProps<T | undefined> {
  title?: React.ReactNode | null;
  subTitle?: React.ReactNode | null;
  entityOrPack: Lite<T & Entity> | T | EntityPack<T>;
  propertyRoute?: PropertyRoute;
  isOperationVisible?: (eoc: EntityOperationContext<T & Entity>) => boolean;
  validate?: boolean;
  requiresSaveOperation?: boolean;
  avoidPromptLoseChange?: boolean;
  extraProps?: {}
  getViewPromise?: (e: T) => (undefined | string | ViewPromise<T>);
  buttons?: Navigator.ViewButtons;
  allowExchangeEntity?: boolean;
  readOnly?: boolean;
  modalSize?: BsSize;
  createNew?: () => Promise<EntityPack<T> | undefined>;
  ref?: React.Ref<IHandleKeyboard>
}

let modalCount = 0;

interface FrameModalState<T extends ModifiableEntity> {
  pack: EntityPack<T>;
  lastEntity: string;
  refreshCount: number;
  getComponent: (ctx: TypeContext<T>) => React.ReactElement;
  executing?: boolean;
}

export function FrameModal<T extends ModifiableEntity>(p: FrameModalProps<T>): React.JSX.Element {

  const [state, setState] = useStateWithPromise<FrameModalState<T> | undefined>(undefined);
  const [show, setShow] = React.useState(true);
  const prefix = React.useMemo(() => "modal" + (modalCount++), []);

  const okClicked = React.useRef(false);
  const buttonBar = React.useRef<ButtonBarHandle>(null);
  const entityComponent = React.useRef<React.Component>(null);
  const validationErrors = React.useRef<ValidationErrorsHandle>(null);
  const frameRef = React.useRef<EntityFrame<T> | undefined>(undefined);

  const forceUpdate = useForceUpdate();

  React.useImperativeHandle(p.ref, () => ({
    handleKeyDown(e: KeyboardEvent) {
      buttonBar.current && buttonBar.current.handleKeyDown(e);
    }
  }));

  const typeName = getTypeName(p.entityOrPack);
  const typeInfo = tryGetTypeInfo(typeName);


  React.useEffect(() => {
    Navigator.toEntityPack(p.entityOrPack)
      .then(pack => loadComponent(pack).promise.then(getComponent => setPack(pack, getComponent)));
  }, [p.entityOrPack]);

  function loadComponent(pack: EntityPack<T>, forceViewName?: string | ViewPromise<T>) {

    if (forceViewName) {
      if (forceViewName instanceof ViewPromise)
        return forceViewName;

      return Navigator.getViewPromise(pack.entity, forceViewName);
    }

    const result = p.getViewPromise && p.getViewPromise(pack.entity);

    var viewPromise = result instanceof ViewPromise ? result : Navigator.getViewPromise(pack.entity, result);

    if (p.extraProps)
      viewPromise = viewPromise.withProps(p.extraProps);

    return viewPromise;
  }

  function setPack(pack: EntityPack<T>, getComponent: (ctx: TypeContext<T>) => React.ReactElement, callback?: () => void) {
    setState({
      pack,
      lastEntity: JSON.stringify(pack.entity),
      getComponent,
      refreshCount: state ? state.refreshCount + 1 : 0
    }).then(callback);
  }

  function getSaveChangesOperations() {

    const frame = frameRef.current;

    if (frame == null)
      return [];

    const ti = tryGetTypeInfo(frame.pack.entity.Type)

    const pack = frame.pack;

    const buttonContext: ButtonsContext = {
      frame: frame as unknown as EntityFrame<ModifiableEntity>,
      pack: pack,
      isOperationVisible: p.isOperationVisible,
      tag: "SaveChangesModal"
    };

    return ti == null ? [] : Operations.operationInfos(ti)
      .filter(oi => oi.canBeNew || !pack.entity.isNew)
      .filter(oi => oi.operationType == "Execute" && oi.canBeModified)
      .map(oi => EntityOperationContext.fromEntityPack<T & Entity>(frame as unknown as EntityFrame<T & Entity>, pack as EntityPack<T & Entity>, oi.key)!)
      .filter(eoc => (eoc.settings?.showOnSaveChangesModal ?? Operations.Defaults.isSave(eoc.operationInfo)))
      .filter(eoc => eoc.isVisibleInButtonBar(buttonContext));
  }

  function handleOkClicked(): void {
    const pack = state?.pack;
    if (hasChanges() &&
      (p.requiresSaveOperation != undefined ? p.requiresSaveOperation : Navigator.typeRequiresSaveOperation(pack!.entity.Type))) {
      MessageModal.show({
        title: SaveChangesMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.saveChangesBeforeOrPressCancel.niceToString(),
        buttons: "ok",
        style: "warning",
        icon: "warning"
      });
    }
    else {

      if (!p.validate) {

        okClicked.current = true;
        setShow(false)

        return;
      }

      Navigator.API.validateEntity(pack!.entity)
        .then(() => {
          okClicked.current = true;
          setShow(false);
        }, ifError(ValidationError, e => {
          GraphExplorer.setModelState(pack!.entity, e.modelState, "");
          forceUpdate();
        }));
    }
  }

  function hasChanges() {

    const hc = FunctionalAdapter.innerRef(entityComponent.current) as IHasChanges | null;
    if (hc?.entityHasChanges) {
      var result = hc.entityHasChanges();
      if (result != null)
        return result;
    }

    if (state == null)
      return false;

    const entity = state.pack.entity;

    const ge = GraphExplorer.propagateAll(entity);

    return entity.modified && JSON.stringify(entity) != state.lastEntity;
  }

  function handleCancelClicked() {

    if (hasChanges() && !p.avoidPromptLoseChange) {
      SaveChangesModal.show({ eocs: getSaveChangesOperations() })
        .then(result => {
          if (result == "loseChanges")
            setShow(false);

          if (result instanceof EntityOperationContext) {

            result.onExecuteSuccess = pack => {
              Operations.notifySuccess();
              frameRef.current!.onClose?.(pack);
              return Promise.resolve();
            };

            result.defaultClick();
          }
        });
    }
    else {
      setShow(false);
    }
  }

  function handleOnExited() {
    if (okClicked.current)
      p.onExited!(state!.pack.entity);
    else if (state == null)
      p.onExited!(undefined);
    else {
      if (p.buttons == "close") { //Even if you cancel, maybe you have executed an operation 
        var oldEntity = JSON.parse(state.lastEntity) as T;
        GraphExplorer.propagateAll(oldEntity);
        p.onExited!(oldEntity.modified ? undefined : oldEntity);
      }
      else {
        p.onExited!(undefined);
      }
    }
  }

  var settings = state && Navigator.getSettings(state.pack.entity.Type);

  let frame: EntityFrame<T>;
  let wc: WidgetContext<ModifiableEntity> | undefined = undefined;
  let styleOptions: StyleOptions;
  let ctx: TypeContext<any>;

  const pr = typeInfo ? PropertyRoute.root(typeInfo) : p.propertyRoute;
  if (!pr)
    throw new Error(`No TypeInfo for "${typeName}" found, if is an EmbeddedEntity set the propertyRoute explicitly`);

  if (state) {
    frame = {
      tabs: undefined,
      frameComponent: { forceUpdate: forceUpdate, type: FrameModalEx },
      entityComponent: entityComponent.current,
      onReload: (pack, reloadComponent, callback) => {
        const newPack = pack || state!.pack;
        if (reloadComponent) {
          setState(undefined)
            .then(() => loadComponent(newPack, reloadComponent == true ? undefined : reloadComponent).promise)
            .then(getComponent => setPack(newPack, getComponent, callback));
        }
        else {
          setPack(newPack, state!.getComponent, callback);
        }
      },
      pack: state.pack,
      onClose: (newPack?: EntityPack<T>) => p.onExited!(newPack?.entity),
      revalidate: () => validationErrors.current && validationErrors.current.forceUpdate(),
      setError: (modelState, initialPrefix = "") => {
        GraphExplorer.setModelState(state.pack.entity, modelState, initialPrefix!);
        forceUpdate();
      },
      refreshCount: state.refreshCount,
      createNew: p.createNew,
      allowExchangeEntity: p.buttons == "close" && (p.allowExchangeEntity ?? true),
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
    styleOptions = {
      readOnly: p.readOnly != undefined ? p.readOnly : Navigator.isReadOnly(state.pack, { isEmbedded: p.propertyRoute?.typeReference().isEmbedded }),
      frame: frame as unknown as EntityFrame<ModifiableEntity>,
    };

    ctx = new TypeContext(undefined, styleOptions, pr, new ReadonlyBinding(state.pack.entity, ""), prefix!);

    wc = { ctx: ctx, frame: frame as unknown as EntityFrame<ModifiableEntity> };
  }

  return (
    <Modal
      size={p.modalSize ?? settings?.modalSize ?? "lg" as any}
      show={show}
      onExited={handleOnExited}
      onHide={handleCancelClicked}
      className="sf-frame-modal"
      dialogClassName={classes(settings?.modalDialogClass, settings?.modalMaxWidth ? "modal-max-width" : undefined)}
      enforceFocus={settings?.enforceFocusInModal ?? true}
      fullscreen={settings?.modalFullScreen ? true : undefined}
    >
      <ModalHeaderButtons onClose={p.buttons == "close" ? handleCancelClicked : undefined} stickyHeader={settings?.stickyHeader}>
        <FrameModalTitle pack={state?.pack} pr={p.propertyRoute} title={p.title} subTitle={p.subTitle} getViewPromise={p.getViewPromise as any} widgets={wc && renderWidgets(wc, settings?.stickyHeader)} />
      </ModalHeaderButtons>
      {state && renderBody(state)}
      {p.buttons == "ok_cancel" && <ModalFooterButtons
        onOk={handleOkClicked}
        onCancel={handleCancelClicked}
        okDisabled={!state}>
      </ModalFooterButtons>
      }
    </Modal>
  );

  function renderBody(pc: FrameModalState<T>) {

    frameRef.current = frame;

    return (
      <div className="modal-body" style={pc.executing == true ? { opacity: ".6" } : undefined}>
        {wc && <WidgetEmbedded widgetContext={wc} >
          <div className="sf-button-widget-container">
            {entityComponent.current && <ButtonBar ref={buttonBar} frame={frame as unknown as EntityFrame<ModifiableEntity>} pack={pc.pack} isOperationVisible={p.isOperationVisible} />}
          </div>
          <ValidationErrors ref={validationErrors} entity={pc.pack.entity} prefix={prefix} />
          <div className="sf-main-control" data-refresh-count={pc.refreshCount} data-main-entity={entityInfo(ctx.value)}>
            <ErrorBoundary>
              {pc.getComponent && <AutoFocus>{FunctionalAdapter.withRef(pc.getComponent(ctx), c => setComponent(c))}</AutoFocus>}
            </ErrorBoundary>
          </div>
        </WidgetEmbedded>}
      </div>
    );
  }

  function setComponent(c: React.Component | null) {
    if (c && entityComponent.current != c) {
      (entityComponent as React.RefObject<React.Component | null>).current = c;
      forceUpdate();
    }
  }
}

const FrameModalEx = FrameModal;

function getTypeName(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>) {
  return (entityOrPack as Lite<Entity>).EntityType ??
    (entityOrPack as ModifiableEntity).Type ??
    (entityOrPack as EntityPack<ModifiableEntity>).entity.Type;
}

export namespace FrameModalManager {
  export function openView<T extends ModifiableEntity>(entityOrPack: Lite<T & Entity> | T | EntityPack<T>, options: Navigator.ViewOptions<T>): Promise<T | undefined> {

    return openModal<T>(<FrameModal
      entityOrPack={entityOrPack}
      readOnly={options.readOnly}
      modalSize={options.modalSize}
      propertyRoute={options.propertyRoute}
      getViewPromise={options.getViewPromise}
      isOperationVisible={options.isOperationVisible}
      requiresSaveOperation={options.requiresSaveOperation}
      avoidPromptLoseChange={options.avoidPromptLoseChange}
      extraProps={options.extraProps}
      validate={options.validate == undefined ? isTypeModel(getTypeName(entityOrPack)) : options.validate}
      title={options.title}
      subTitle={options.subTitle}
      createNew={options.createNew}
      allowExchangeEntity={options.allowExchangeEntity}
      buttons={options.buttons ?? Navigator.typeDefaultButtons(getTypeName(entityOrPack), options.propertyRoute?.typeReference().isEmbedded)} />);
  }
}

export function FrameModalTitle({ pack, pr, title, subTitle, widgets, getViewPromise }: {
  pack?: EntityPack<ModifiableEntity>, pr?: PropertyRoute, title: React.ReactNode, subTitle?: React.ReactNode | null, widgets: React.ReactNode, getViewPromise?: (e: ModifiableEntity) => (undefined | string | ViewPromise<ModifiableEntity>);
}): React.ReactElement {

  if (!pack)
    return <span className="sf-entity-title">{JavascriptMessage.loading.niceToString()}</span>;

  const entity = pack.entity;

  if (title === undefined) {
    title = Navigator.renderEntity(entity) ?? "";
  }

  if (subTitle === undefined) {
    subTitle = Navigator.getTypeSubTitle(entity, pr);
  }

  return (
    <div>
      {title != null && <>
        <span className="sf-entity-title">{title}</span>&nbsp;
        {renderExpandLink(pack.entity)}
      </>
      }
      {(subTitle || widgets) &&
        <div className="sf-entity-sub-title">
          {subTitle && <small className="sf-type-nice-name text-muted"> {subTitle}</small>}
          {widgets}
        </div>
      }
    </div>
  );

  function renderExpandLink(entity: ModifiableEntity) {

    if (entity == undefined || entity.isNew)
      return undefined;

    const ti = tryGetTypeInfo(entity.Type);

    if (ti == undefined || !Navigator.isViewable(ti, { buttons: "close" })) //Embedded
      return undefined;

    return (
      <a className="sf-popup-fullscreen sf-pointer" role="button" tabIndex={0} href="#" onClick={handlePopupFullScreen} title={FrameMessage.Fullscreen.niceToString()}>
        <FontAwesomeIcon aria-hidden={true} icon="up-right-from-square" />
      </a>
    );
  }


  function handlePopupFullScreen(e: React.MouseEvent<any>) {
    e.preventDefault();

    var entity = pack!.entity;
    var vp = getViewPromise && getViewPromise(entity);
    AppContext.pushOrOpenInTab(Navigator.navigateRoute(entity as Entity, typeof vp == "string" ? vp : undefined), e);
  }
}

