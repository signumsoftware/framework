
import * as React from 'react'
import { openModal, IModalProps, IHandleKeyboard, FunctionalAdapter } from '../Modals'
import MessageModal from '../Modals/MessageModal'
import * as Navigator from '../Navigator'
import * as AppContext from '../AppContext';
import { ButtonBar, ButtonBarHandle } from './ButtonBar'
import { ValidationError } from '../Services'
import { ifError } from '../Globals'
import { TypeContext, StyleOptions, EntityFrame, IHasChanges, ButtonsContext } from '../TypeContext'
import { Entity, Lite, ModifiableEntity, JavascriptMessage, NormalWindowMessage, getToString, EntityPack, entityInfo, isEntityPack, isLite, is, isEntity, SaveChangesMessage } from '../Signum.Entities'
import { getTypeInfo, PropertyRoute, ReadonlyBinding, GraphExplorer, isTypeModel, tryGetTypeInfo } from '../Reflection'
import { ValidationErrors, ValidationErrorsHandle } from './ValidationErrors'
import { renderWidgets, WidgetContext } from './Widgets'
import { EntityOperationContext, notifySuccess, operationInfos } from '../Operations'
import { ViewPromise } from "../Navigator";
import { BsSize, ErrorBoundary } from '../Components';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import "./Frames.css"
import { AutoFocus } from '../Components/AutoFocus';
import { useStateWithPromise, useForceUpdate } from '../Hooks'
import { Modal } from 'react-bootstrap'
import { ModalHeaderButtons } from '../Components/ModalHeaderButtons'
import WidgetEmbedded from './WidgetEmbedded'
import SaveChangesModal from '../Modals/SaveChangesModal';

interface FrameModalProps extends IModalProps<ModifiableEntity | undefined> {
  title?: string;
  entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>;
  propertyRoute?: PropertyRoute;
  isOperationVisible?: (eoc: EntityOperationContext<any /*Entity*/>) => boolean;
  validate?: boolean;
  requiresSaveOperation?: boolean;
  avoidPromptLoseChange?: boolean;
  extraProps?: {}
  getViewPromise?: (e: ModifiableEntity) => (undefined | string | Navigator.ViewPromise<ModifiableEntity>);
  buttons?: Navigator.ViewButtons;
  allowExchangeEntity?: boolean;
  readOnly?: boolean;
  modalSize?: BsSize;
  createNew?: () => Promise<EntityPack<ModifiableEntity> | undefined>;
}

let modalCount = 0;

interface PackAndComponent {
  pack: EntityPack<ModifiableEntity>;
  lastEntity: string;
  refreshCount: number;
  getComponent: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
}

export const FrameModal = React.forwardRef(function FrameModal(p: FrameModalProps, ref: React.Ref<IHandleKeyboard>) {

  const [packComponent, setPackComponent] = useStateWithPromise<PackAndComponent | undefined>(undefined);
  const [show, setShow] = React.useState(true);
  const prefix = React.useMemo(() => "modal" + (modalCount++), []);

  const okClicked = React.useRef(false);
  const buttonBar = React.useRef<ButtonBarHandle>(null);
  const entityComponent = React.useRef<React.Component>(null);
  const validationErrors = React.useRef<ValidationErrorsHandle>(null);
  const frameRef = React.useRef<EntityFrame | undefined>(undefined);

  const forceUpdate = useForceUpdate();

  React.useImperativeHandle(ref, () => ({
    handleKeyDown(e: KeyboardEvent) {
      buttonBar.current && buttonBar.current.handleKeyDown(e);
    }
  }));

  const typeName = getTypeName(p.entityOrPack);
  const typeInfo = tryGetTypeInfo(typeName);


  React.useEffect(() => {
    Navigator.toEntityPack(p.entityOrPack)
      .then(pack => loadComponent(pack).promise.then(getComponent => setPack(pack, getComponent)))
      .done();
  }, [p.entityOrPack]);

  function loadComponent(pack: EntityPack<ModifiableEntity>, callback?: () => void) {

    const result = p.getViewPromise && p.getViewPromise(pack.entity);

    var viewPromise = result instanceof ViewPromise ? result : Navigator.getViewPromise(pack.entity, result);

    if (p.extraProps)
      viewPromise = viewPromise.withProps(p.extraProps);

    return viewPromise;
  }
  
  function setPack(pack: EntityPack<ModifiableEntity>, getComponent: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>, callback?: () => void) {
    setPackComponent({
      pack,
      lastEntity: JSON.stringify(pack.entity),
      getComponent,
      refreshCount: packComponent ? packComponent.refreshCount + 1 : 0
    }).then(callback).done();
  }

  function getSaveChangesOperations() {

    const frame = frameRef.current;

    if (frame == null)
      return [];

    const ti = tryGetTypeInfo(frame.pack.entity.Type)

    const pack = frame.pack;

    const buttonContext: ButtonsContext = {
      frame: frame,
      pack: pack,
      isOperationVisible: p.isOperationVisible,
      tag: "SaveChangesModal"
    };

    return ti == null ? [] : operationInfos(ti)
      .filter(oi => oi.canBeNew || !pack.entity.isNew)
      .filter(oi => oi.operationType == "Execute" && oi.canBeModified)
      .map(oi => EntityOperationContext.fromEntityPack(frame, pack as EntityPack<Entity>, oi.key)!)
      .filter(eoc => eoc.isVisibleInButtonBar(buttonContext));
  }

  function handleOkClicked() {
    const pack = packComponent?.pack;
    if (hasChanges() &&
      (p.requiresSaveOperation != undefined ? p.requiresSaveOperation : Navigator.typeRequiresSaveOperation(pack!.entity.Type))) {
      MessageModal.show({
        title: SaveChangesMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.saveChangesBeforeOrPressCancel.niceToString(),
        buttons: "ok",
        style: "warning",
        icon: "warning"
      }).done();
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
          GraphExplorer.setModelState(pack!.entity, e.modelState, "entity");
          forceUpdate();
        })).done();
    }
  }

  function hasChanges() {

    const hc = FunctionalAdapter.innerRef(entityComponent.current) as IHasChanges | null;
    if (hc?.entityHasChanges)
      return hc.entityHasChanges();

    if (packComponent == null)
      return false;

    const entity = packComponent.pack.entity;

    const ge = GraphExplorer.propagateAll(entity);

    return entity.modified && JSON.stringify(entity) != packComponent.lastEntity;
  }

  function handleCancelClicked() {

    if (hasChanges() && !p.avoidPromptLoseChange) {
      SaveChangesModal.show({ eocs: getSaveChangesOperations() })
        .then(result => {
          if (result == "loseChanges")
            setShow(false);

          if (result instanceof EntityOperationContext) {

            result.onExecuteSuccess = pack => {
              notifySuccess();
              frameRef.current!.onClose(pack);
            };

            result.defaultClick();
          }
        }).done();
    }
    else {
      setShow(false);
    }
  }

  function handleOnExited() {
    if (okClicked.current)
      p.onExited!(packComponent!.pack.entity);
    else if (packComponent == null)
      p.onExited!(undefined);
    else {
      var oldEntity = JSON.parse(packComponent.lastEntity) as ModifiableEntity;
      GraphExplorer.propagateAll(oldEntity);
      p.onExited!(oldEntity.modified ? undefined : oldEntity);
    }
  }

  var settings = packComponent && Navigator.getSettings(packComponent.pack.entity.Type);

    return (
    <Modal size={p.modalSize ?? settings?.modalSize ?? "lg" as any} show={show} onExited={handleOnExited} onHide={handleCancelClicked} className="sf-frame-modal" >
        <ModalHeaderButtons
          onClose={p.buttons == "close" ? handleCancelClicked : undefined}
          onOk={p.buttons == "ok_cancel" ? handleOkClicked : undefined}
          onCancel={p.buttons == "ok_cancel" ? handleCancelClicked : undefined}
        okDisabled={!packComponent}>
        <FrameModalTitle pack={packComponent?.pack} pr={p.propertyRoute} title={p.title} getViewPromise={p.getViewPromise} />
        </ModalHeaderButtons>
      {packComponent && renderBody(packComponent)}
      </Modal>
    );

  function renderBody(pc: PackAndComponent) {

    const frame: EntityFrame = {
      tabs: undefined,
      frameComponent: { forceUpdate, type: FrameModal as any },
      entityComponent: entityComponent.current,
      onReload: (pack, reloadComponent, callback) => {
        const newPack = pack || packComponent!.pack;
        if (reloadComponent) {
          setPackComponent(undefined)
            .then(() => loadComponent(newPack).promise)
            .then(getComponent => setPack(newPack, getComponent, callback))
            .done();
        }
        else {
          setPack(newPack, packComponent!.getComponent, callback);
        }
      },
      pack: pc.pack,
      onClose: (newPack?: EntityPack<ModifiableEntity>) => p.onExited!(newPack?.entity),
      revalidate: () => validationErrors.current && validationErrors.current.forceUpdate(),
      setError: (modelState, initialPrefix = "") => {
        GraphExplorer.setModelState(pc.pack.entity, modelState, initialPrefix!);
        forceUpdate();
      },
      refreshCount: pc.refreshCount,
      createNew: p.createNew,
      allowExchangeEntity: p.buttons == "close" && (p.allowExchangeEntity ?? true),
      prefix: prefix,
    };

    frameRef.current = frame;

    const styleOptions: StyleOptions = {
      readOnly: p.readOnly != undefined ? p.readOnly : Navigator.isReadOnly(pc.pack, { isEmbedded: p.propertyRoute?.typeReference().isEmbedded }),
      frame: frame,
    };

    const pr = typeInfo ? PropertyRoute.root(typeInfo) : p.propertyRoute;
    if (!pr)
      throw new Error("propertyRoute is mandatory for embeddedEntities");

    const ctx = new TypeContext(undefined, styleOptions, pr, new ReadonlyBinding(pc.pack.entity, ""), prefix!);

    const wc: WidgetContext<ModifiableEntity> = { ctx: ctx, frame: frame };

    return (
      <div className="modal-body">
        {renderWidgets(wc)}
        <WidgetEmbedded widgetContext={wc} >
        {entityComponent.current && <ButtonBar ref={buttonBar} frame={frame} pack={pc.pack} isOperationVisible={p.isOperationVisible} />}
        <ValidationErrors ref={validationErrors} entity={pc.pack.entity} prefix={prefix} />
        <div className="sf-main-control" data-test-ticks={new Date().valueOf()} data-main-entity={entityInfo(ctx.value)}>
          <ErrorBoundary>
            {pc.getComponent && <AutoFocus>{FunctionalAdapter.withRef(pc.getComponent(ctx), c => setComponent(c))}</AutoFocus>}
          </ErrorBoundary>
        </div>
        </WidgetEmbedded>
      </div>
    );
  }

  function setComponent(c: React.Component | null) {
    if (c && entityComponent.current != c) {
      (entityComponent as React.MutableRefObject<React.Component>).current = c;
      forceUpdate();
    }
  }
});

function getTypeName(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>) {
  return (entityOrPack as Lite<Entity>).EntityType ??
    (entityOrPack as ModifiableEntity).Type ??
    (entityOrPack as EntityPack<ModifiableEntity>).entity.Type;
}

export namespace FrameModalManager {
  export function openView(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, options: Navigator.ViewOptions): Promise<Entity | undefined> {

    return openModal<Entity>(<FrameModal
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
      createNew={options.createNew}
      allowExchangeEntity={options.allowExchangeEntity}
      buttons={options.buttons ?? Navigator.typeDefaultButtons(getTypeName(entityOrPack), options.propertyRoute?.typeReference().isEmbedded)} />);
  }
}



export function FrameModalTitle({ pack, pr, title, getViewPromise }: { pack?: EntityPack<ModifiableEntity>, pr?: PropertyRoute, title: React.ReactNode, getViewPromise?: (e: ModifiableEntity) => (undefined | string | Navigator.ViewPromise<ModifiableEntity>); }) {

  if (!pack)
      return <span className="sf-entity-title">{JavascriptMessage.loading.niceToString()}</span>;

  const entity = pack.entity;

    return (
      <span>
      <span className="sf-entity-title">{title || getToString(entity)}</span>&nbsp;
        {renderExpandLink(pack.entity)}
        <br />
        <small className="sf-type-nice-name text-muted"> {pr?.member && pr.member.typeNiceName || Navigator.getTypeTitle(entity, pr)}</small>
      </span>
    );

  function renderExpandLink(entity: ModifiableEntity) {

    if (entity == undefined || entity.isNew)
      return undefined;

    const ti = tryGetTypeInfo(entity.Type);

    if (ti == undefined || !Navigator.isViewable(ti, { buttons: "close" })) //Embedded
      return undefined;

    return (
      <a className="sf-popup-fullscreen sf-pointer" href="#" onClick={handlePopupFullScreen}>
        <FontAwesomeIcon icon="external-link-alt" />
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

