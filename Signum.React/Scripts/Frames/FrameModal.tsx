
import * as React from 'react'
import { openModal, IModalProps, IHandleKeyboard } from '../Modals'
import MessageModal from '../Modals/MessageModal'
import * as Navigator from '../Navigator'
import { ButtonBar, ButtonBarHandle } from './ButtonBar'
import { ValidationError } from '../Services'
import { ifError } from '../Globals'
import { TypeContext, StyleOptions, EntityFrame, IHasChanges } from '../TypeContext'
import { Entity, Lite, ModifiableEntity, JavascriptMessage, NormalWindowMessage, getToString, EntityPack, entityInfo, isEntityPack, isLite, is, isEntity } from '../Signum.Entities'
import { getTypeInfo, PropertyRoute, ReadonlyBinding, GraphExplorer, isTypeModel } from '../Reflection'
import { ValidationErrors, ValidationErrorHandle } from './ValidationErrors'
import { renderWidgets, WidgetContext, renderEmbeddedWidgets } from './Widgets'
import { EntityOperationContext } from '../Operations'
import { ViewPromise } from "../Navigator";
import { BsSize, ErrorBoundary } from '../Components';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import "./Frames.css"
import { AutoFocus } from '../Components/AutoFocus';
import { instanceOf } from 'prop-types';
import { useStateWithPromise, useForceUpdate } from '../Hooks'
import { Modal } from 'react-bootstrap'
import { ModalHeaderButtons } from '../Components/ModalHeaderButtons'

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
  isNavigate?: boolean;
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
  const validationErrors = React.useRef<ValidationErrorHandle>(null);

  const forceUpdate = useForceUpdate();

  React.useImperativeHandle(ref, () => ({
  handleKeyDown(e: KeyboardEvent) {
      buttonBar.current && buttonBar.current.handleKeyDown(e);
  }
  }));

  const typeName = getTypeName(p.entityOrPack);
  const typeInfo = getTypeInfo(typeName);


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

  function handleOkClicked() {
    const pack = packComponent?.pack;
    if (hasChanges() &&
      (p.requiresSaveOperation != undefined ? p.requiresSaveOperation : Navigator.typeRequiresSaveOperation(pack!.entity.Type))) {
      MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
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
      MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
        message: NormalWindowMessage.LoseChanges.niceToString(),
        buttons: "yes_no",
        style: "warning",
        icon: "warning"
      }).then(result => {
        if (result == "yes") {
          setShow(false);
        }
      }).done();
    }
    else {
      setShow(false);
    }
  }

  function handleOnExited() {
    p.onExited!(okClicked.current ? packComponent!.pack.entity : undefined);
  }

  var settings = packComponent && Navigator.getSettings(packComponent.pack.entity.Type);

    return (
    <Modal size={p.modalSize ?? settings?.modalSize ?? "lg" as any} show={show} onExited={handleOnExited} onHide={handleCancelClicked} className="sf-frame-modal" >
        <ModalHeaderButtons
        onClose={p.isNavigate ? handleCancelClicked : undefined}
        onOk={!p.isNavigate ? handleOkClicked : undefined}
        onCancel={!p.isNavigate ? handleCancelClicked : undefined}
        okDisabled={!packComponent}>
        <FrameModalTitle pack={packComponent?.pack} pr={p.propertyRoute} title={p.title} getViewPromise={p.getViewPromise} />
        </ModalHeaderButtons>
      {packComponent && renderBody(packComponent)}
      </Modal>
    );

  function renderBody(pc: PackAndComponent) {

    const frame: EntityFrame = {
      frameComponent: { forceUpdate },
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
      allowChangeEntity: p.isNavigate || false,
    };

    const styleOptions: StyleOptions = {
      readOnly: p.readOnly != undefined ? p.readOnly : Navigator.isReadOnly(pc.pack),
      frame: frame,
    };

    const pr = typeInfo ? PropertyRoute.root(typeInfo) : p.propertyRoute;
    if (!pr)
      throw new Error("propertyRoute is mandatory for embeddedEntities");

    const ctx = new TypeContext(undefined, styleOptions, pr, new ReadonlyBinding(pc.pack.entity, ""), prefix!);

    const wc: WidgetContext<ModifiableEntity> = { ctx: ctx, frame: frame };

    const embeddedWidgets = renderEmbeddedWidgets(wc);

    return (
      <div className="modal-body">
        {renderWidgets(wc)}
        {entityComponent.current && <ButtonBar ref={buttonBar} frame={frame} pack={pc.pack} isOperationVisible={p.isOperationVisible} />}
        <ValidationErrors ref={validationErrors} entity={pc.pack.entity} prefix={prefix} />
        {embeddedWidgets.top}
        <div className="sf-main-control" data-test-ticks={new Date().valueOf()} data-main-entity={entityInfo(ctx.value)}>
          <ErrorBoundary>
            {pc.getComponent && <AutoFocus>{FunctionalAdapter.withRef(pc.getComponent(ctx), c => setComponent(c))}</AutoFocus>}
          </ErrorBoundary>
        </div>
        {embeddedWidgets.bottom}
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
      isNavigate={false} />);
  }

  export function openNavigate(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, options: Navigator.NavigateOptions): Promise<void> {

    return openModal<void>(<FrameModal
      entityOrPack={entityOrPack}
      readOnly={options.readOnly}
      modalSize={options.modalSize}
      propertyRoute={undefined}
      getViewPromise={options.getViewPromise}
      requiresSaveOperation={undefined}
      avoidPromptLoseChange={options.avoidPromptLooseChange}
      extraProps={options.extraProps}
      createNew={options.createNew}
      isNavigate={true}
    />);
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

    const ti = getTypeInfo(entity.Type);

    if (ti == undefined || !Navigator.isNavigable(ti, false)) //Embedded
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
    Navigator.pushOrOpenInTab(Navigator.navigateRoute(entity as Entity, typeof vp == "string" ? vp : undefined), e);
  }
}



export class FunctionalAdapter extends React.Component {

  innerRef?: any | null;

  render() {
    var only = React.Children.only(this.props.children);
    if (!React.isValidElement(only))
      throw new Error("Not a valid react element: " + only);

    if (isForwardRef(only.type)) {
      return React.cloneElement(only, { ref: (a: any) => { this.innerRef = a; } });
}

    return this.props.children;
  }
  
  static withRef(element: React.ReactElement<any>, ref: React.Ref<React.Component>) {
    var type = element.type as React.ComponentClass | React.FunctionComponent | string;
    if (typeof type == "string" || type.prototype?.render) {
      return React.cloneElement(element, { ref: ref });
    } else {
      return <FunctionalAdapter ref={ref}>{element}</FunctionalAdapter>
    }
  }

  static isInstanceOf(component: React.Component | null | undefined, type: React.ComponentType) {

    if (component instanceof type)
      return true;

    if (component instanceof FunctionalAdapter) {
      var only = React.Children.only(component.props.children);
      return React.isValidElement(only) && only.type == type;
    }

    return false
  }

  static innerRef(component: React.Component | null | undefined) {

    if (component instanceof FunctionalAdapter) {
      return component.innerRef;
    }
    return component;
  }
}

function isForwardRef(type: any) {
  return type.$$typeof == Symbol.for("react.forward_ref");
}
