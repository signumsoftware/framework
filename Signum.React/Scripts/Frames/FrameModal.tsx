
import * as React from 'react'
import { openModal, IModalProps, IHandleKeyboard } from '../Modals'
import MessageModal from '../Modals/MessageModal'
import * as Navigator from '../Navigator'
import ButtonBar from './ButtonBar'
import { ValidationError } from '../Services'
import { ifError } from '../Globals'
import { TypeContext, StyleOptions, EntityFrame, IHasChanges } from '../TypeContext'
import { Entity, Lite, ModifiableEntity, JavascriptMessage, NormalWindowMessage, getToString, EntityPack, entityInfo, isEntityPack, isLite, is, isEntity } from '../Signum.Entities'
import { getTypeInfo, PropertyRoute, ReadonlyBinding, GraphExplorer, isTypeModel } from '../Reflection'
import ValidationErrors from './ValidationErrors'
import { renderWidgets, WidgetContext, renderEmbeddedWidgets } from './Widgets'
import { EntityOperationContext } from '../Operations'
import { ViewPromise } from "../Navigator";
import { BsSize, Modal, ErrorBoundary } from '../Components';
import { ModalHeaderButtons } from '../Components/Modal';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import "./Frames.css"
import { AutoFocus } from '../Components/AutoFocus';
import { instanceOf } from 'prop-types';


interface FrameModalProps extends React.Props<FrameModal>, IModalProps {
  title?: string;
  entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>;
  propertyRoute?: PropertyRoute;
  isOperationVisible?: (eoc: EntityOperationContext<any /*Entity*/>) => boolean;
  validate?: boolean;
  requiresSaveOperation?: boolean;
  avoidPromptLooseChange?: boolean;
  extraComponentProps?: {}
  getViewPromise?: (e: ModifiableEntity) => (undefined | string | Navigator.ViewPromise<ModifiableEntity>);
  isNavigate?: boolean;
  readOnly?: boolean;
  modalSize?: BsSize;
  createNew?: () => Promise<EntityPack<ModifiableEntity> | undefined>;
}

interface FrameModalState {
  pack?: EntityPack<ModifiableEntity>;
  lastEntity?: string;
  getComponent?: (ctx: TypeContext<ModifiableEntity>) => React.ReactElement<any>;
  propertyRoute?: PropertyRoute;
  show: boolean;
  refreshCount: number;
}

let modalCount = 0;

export default class FrameModal extends React.Component<FrameModalProps, FrameModalState> implements IHandleKeyboard  {
  prefix = "modal" + (modalCount++);

  static defaultProps: FrameModalProps = {
    isOperationVisible: undefined,
    entityOrPack: null as any
  }

  constructor(props: FrameModalProps) {
    super(props);
    this.state = this.calculateState(props);
  }

  componentWillMount() {
    Navigator.toEntityPack(this.props.entityOrPack)
      .then(ep => this.setPack(ep))
      .then(pack => this.loadComponent(pack))
      .done();
  }

  componentWillReceiveProps(props: FrameModalProps) {
    this.setState(this.calculateState(props));

    Navigator.toEntityPack(props.entityOrPack)
      .then(ep => this.setPack(ep))
      .then(pack => this.loadComponent(pack))
      .done();
  }

  handleKeyDown(e: KeyboardEvent) {
    this.buttonBar && this.buttonBar.hanldleKeyDown(e);
  }

  getTypeName() {
    return (this.props.entityOrPack as Lite<Entity>).EntityType ||
      (this.props.entityOrPack as ModifiableEntity).Type ||
      (this.props.entityOrPack as EntityPack<ModifiableEntity>).entity.Type;
  }

  getTypeInfo() {
    const typeName = this.getTypeName();

    return getTypeInfo(typeName);
  }

  calculateState(props: FrameModalProps): FrameModalState {

    const typeInfo = this.getTypeInfo();

    const pr = typeInfo ? PropertyRoute.root(typeInfo) : this.props.propertyRoute;

    if (!pr)
      throw new Error("propertyRoute is mandatory for embeddedEntities");

    return {
      propertyRoute: pr,
      show: true,
      refreshCount: 0,
    };
  }
  
  setPack(pack: EntityPack<ModifiableEntity>): EntityPack<ModifiableEntity> {
    this.setState({
      pack: pack,
      refreshCount: this.state.refreshCount + 1,
      lastEntity: JSON.stringify(pack.entity)
    });

    return pack;
  }

  loadComponent(pack: EntityPack<ModifiableEntity>) {

    const result = this.props.getViewPromise && this.props.getViewPromise(pack.entity);

    var viewPromise = result instanceof ViewPromise ? result : Navigator.getViewPromise(pack.entity, result);

    if (this.props.extraComponentProps)
      viewPromise = viewPromise.withProps(this.props.extraComponentProps);

    return viewPromise.promise
      .then(c => this.setState({ getComponent: c }));
  }

  okClicked: boolean = false;
  handleOkClicked = () => {
    if (this.hasChanges() &&
      (this.props.requiresSaveOperation != undefined ? this.props.requiresSaveOperation : Navigator.typeRequiresSaveOperation(this.state.pack!.entity.Type))) {
      MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.saveChangesBeforeOrPressCancel.niceToString(),
        buttons: "ok",
        style: "warning",
        icon: "warning"
      }).done();
    }
    else {

      if (!this.props.validate) {

        this.okClicked = true;
        this.setState({ show: false });

        return;
      }

      Navigator.API.validateEntity(this.state.pack!.entity)
        .then(() => {
          this.okClicked = true;
          this.setState({ show: false });
        }, ifError(ValidationError, e => {
          GraphExplorer.setModelState(this.state.pack!.entity, e.modelState, "entity");
          this.forceUpdate();
        })).done();
    }
  }


  handleCancelClicked = () => {

    if (this.hasChanges() && !this.props.avoidPromptLooseChange) {
      MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
        message: NormalWindowMessage.LoseChanges.niceToString(),
        buttons: "yes_no",
        style: "warning",
        icon: "warning"
      }).then(result => {
        if (result == "yes") {
          this.setState({ show: false });
        }
      }).done();
    }
    else {
      this.setState({ show: false });
    }
  }

  hasChanges() {

    var hc = this.entityComponent as IHasChanges | null | undefined;
    if (hc && hc.componentHasChanges)
      return hc.componentHasChanges();

    if (this.state.pack == null)
      return false;

    const entity = this.state.pack.entity;

    var ge = GraphExplorer.propagateAll(entity);

    return entity.modified && JSON.stringify(entity) != this.state.lastEntity;
  }

  handleOnExited = () => {
    this.props.onExited!(this.okClicked ? this.state.pack!.entity : undefined);
  }

  render() {

    const pack = this.state.pack;

    return (
      <Modal size={this.props.modalSize || "lg"} show={this.state.show} onExited={this.handleOnExited} onHide={this.handleCancelClicked} className="sf-popup-control" >
        <ModalHeaderButtons
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

  entityComponent?: React.Component<any, any> | null;

  setComponent(c: React.Component<any, any> | null) {
    if (c && this.entityComponent != c) {
      this.entityComponent = c;
      this.forceUpdate();
    }
  }

  buttonBar?: ButtonBar | null;

  renderBody() {

    const frame: EntityFrame = {
      frameComponent: this,
      entityComponent: this.entityComponent,
      onReload: pack => {
        var newPack = pack || this.state.pack!;
        
        if (is(this.state.pack!.entity as Entity, newPack.entity as Entity))
          this.setPack(newPack);
        else {
          this.setPack(newPack);
          this.setState({ getComponent: undefined }, () => this.loadComponent(newPack).done()); //For AutoFocus and potentialy another view
        }
      },
      pack: this.state.pack,
      onClose: (ok?: boolean) => this.props.onExited!(ok ? this.state.pack!.entity : undefined),
      revalidate: () => this.validationErrors && this.validationErrors.forceUpdate(),
      setError: (modelState, initialPrefix = "") => {
        GraphExplorer.setModelState(this.state.pack!.entity, modelState, initialPrefix!);
        this.forceUpdate();
      },
      refreshCount: this.state.refreshCount,
      allowChangeEntity: this.props.isNavigate || false,
    };

    const pack = this.state.pack!;

    const styleOptions: StyleOptions = {
      readOnly: this.props.readOnly != undefined ? this.props.readOnly : Navigator.isReadOnly(pack),
      frame: frame,
    };

    const ctx = new TypeContext(undefined, styleOptions, this.state.propertyRoute!, new ReadonlyBinding(pack.entity, ""), this.prefix!);

    const wc: WidgetContext<ModifiableEntity> = { ctx: ctx, pack: pack };

    const embeddedWidgets = renderEmbeddedWidgets(wc);

    return (
      <div className="modal-body">
        {renderWidgets({ ctx: ctx, pack: pack })}
        {this.entityComponent && <ButtonBar ref={bb => this.buttonBar = bb} frame={frame} pack={pack} isOperationVisible={this.props.isOperationVisible} />}
        <ValidationErrors entity={pack.entity} ref={ve => this.validationErrors = ve} prefix={this.prefix} />
        {embeddedWidgets.top}
        <div className="sf-main-control" data-test-ticks={new Date().valueOf()} data-main-entity={entityInfo(ctx.value)}>
          <ErrorBoundary>
            {this.state.getComponent && <AutoFocus>{FunctionalAdapter.withRef(this.state.getComponent(ctx), c => this.setComponent(c))}</AutoFocus>}
          </ErrorBoundary>
        </div>
        {embeddedWidgets.bottom}
      </div>
    );
  }

  validationErrors?: ValidationErrors | null;

  renderTitle() {

    if (!this.state.pack)
      return <span className="sf-entity-title">{JavascriptMessage.loading.niceToString()}</span>;

    const entity = this.state.pack.entity;
    const pr = this.props.propertyRoute;

    return (
      <span>
        <span className="sf-entity-title">{this.props.title || getToString(entity)}</span>&nbsp;
        {this.renderExpandLink()}
        <br />
        <small className="sf-type-nice-name text-muted"> {pr && pr.member && pr.member.typeNiceName || Navigator.getTypeTitle(entity, pr)}</small>
      </span>
    );
  }

  renderExpandLink() {
    const entity = this.state.pack!.entity;

    if (entity == undefined || entity.isNew)
      return undefined;

    const ti = getTypeInfo(entity.Type);

    if (ti == undefined || !Navigator.isNavigable(ti, false)) //Embedded
      return undefined;

    return (
      <a className="sf-popup-fullscreen sf-pointer" href="#" onClick={this.handlePopupFullScreen}>
        <FontAwesomeIcon icon="external-link-alt" />
      </a>
    );
  }


  handlePopupFullScreen = (e: React.MouseEvent<any>) => {
    e.preventDefault();

    var entity = this.state.pack!.entity;
    var vp = this.props.getViewPromise && this.props.getViewPromise(entity);
    Navigator.pushOrOpenInTab(Navigator.navigateRoute(entity as Entity, typeof vp == "string" ? vp : undefined), e);
  }

  static openView(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, options: Navigator.ViewOptions): Promise<Entity | undefined> {

    return openModal<Entity>(<FrameModal
      entityOrPack={entityOrPack}
      readOnly={options.readOnly}
      modalSize={options.modalSize}
      propertyRoute={options.propertyRoute}
      getViewPromise={options.getViewPromise}
      isOperationVisible={options.isOperationVisible}
      requiresSaveOperation={options.requiresSaveOperation}
      avoidPromptLooseChange={options.avoidPromptLooseChange}
      extraComponentProps={options.extraComponentProps}
      validate={options.validate == undefined ? FrameModal.isModelEntity(entityOrPack) : options.validate}
      title={options.title}
      isNavigate={false} />);
  }

  static isModelEntity(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>) {
    const typeName = isEntityPack(entityOrPack) ? entityOrPack.entity.Type :
      isLite(entityOrPack) ? entityOrPack.EntityType : entityOrPack.Type;

    return isTypeModel(typeName);
  }

  static openNavigate(entityOrPack: Lite<Entity> | ModifiableEntity | EntityPack<ModifiableEntity>, options: Navigator.NavigateOptions): Promise<void> {

    return openModal<void>(<FrameModal
      entityOrPack={entityOrPack}
      readOnly={options.readOnly}
      modalSize={options.modalSize}
      propertyRoute={undefined}
      getViewPromise={options.getViewPromise}
      requiresSaveOperation={undefined}
      avoidPromptLooseChange={options.avoidPromptLooseChange}
      extraComponentProps={options.extraComponentProps}
      createNew={options.createNew}
      isNavigate={true}
    />);
  }
}

export class FunctionalAdapter extends React.Component {

  render() {
    return this.props.children;
  }
  
  static withRef(element: React.ReactElement<any>, ref: (c: React.Component | null) => void) {
    var type = element.type as React.ComponentClass | React.FunctionComponent | string;
    if (typeof type == "string" || type.prototype.render) {
      return React.cloneElement(element, { ref: ref });
    } else {
      return <FunctionalAdapter ref={ref}>{element}</FunctionalAdapter>
    }
  }
}
