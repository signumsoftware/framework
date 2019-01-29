import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { DynamicViewOverrideEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { EntityLine, TypeContext, FormGroup } from '@framework/Lines'
import { Entity, JavascriptMessage, NormalWindowMessage, is } from '@framework/Signum.Entities'
import { Binding, PropertyRoute, ReadonlyBinding } from '@framework/Reflection'
import JavascriptCodeMirror from '../../Codemirror/JavascriptCodeMirror'
import * as DynamicViewClient from '../DynamicViewClient'
import * as Navigator from '@framework/Navigator'
import { ViewReplacer } from '@framework/Frames/ReactVisitor';
import * as TypeHelpClient from '../../TypeHelp/TypeHelpClient'
import TypeHelpComponent from '../../TypeHelp/TypeHelpComponent'
import TypeHelpButtonBarComponent from '../../TypeHelp/TypeHelpButtonBarComponent'
import ValueLineModal from '@framework/ValueLineModal'
import MessageModal from '@framework/Modals/MessageModal'
import * as Nodes from '../../Dynamic/View/Nodes';
import { DropdownItem, DropdownMenu, DropdownToggle, UncontrolledDropdown } from '@framework/Components';

interface DynamicViewOverrideComponentProps {
  ctx: TypeContext<DynamicViewOverrideEntity>;
}

interface DynamicViewOverrideComponentState {
  exampleEntity?: Entity;
  componentClass?: React.ComponentClass<{ ctx: TypeContext<Entity> }> | null;
  syntaxError?: string;
  viewOverride?: (vr: ViewReplacer<Entity>) => void;
  scriptChanged?: boolean;
  viewNames?: string[];
  typeHelp?: TypeHelpClient.TypeHelp;
}

export default class DynamicViewOverrideComponent extends React.Component<DynamicViewOverrideComponentProps, DynamicViewOverrideComponentState> {

  constructor(props: DynamicViewOverrideComponentProps) {
    super(props);

    this.state = {};
  }


  componentWillMount() {
    this.updateViewNames(this.props);
    this.updateTypeHelp(this.props);
  }

  componentWillReceiveProps(newProps: DynamicViewOverrideComponentProps) {
    if (newProps.ctx.value.entityType && this.props.ctx.value.entityType && !is(this.props.ctx.value.entityType, newProps.ctx.value.entityType)) {
      this.updateViewNames(newProps);
      this.updateTypeHelp(newProps);
    }
  }

  updateViewNames(props: DynamicViewOverrideComponentProps) {
    this.setState({ viewNames: undefined });
    if (props.ctx.value.entityType) {
      const typeName = props.ctx.value.entityType.cleanName;

      Navigator.viewDispatcher.getViewNames(typeName)
        .then(vn => this.setState({ viewNames: vn }))
        .done();
    }
  }

  updateTypeHelp(props: DynamicViewOverrideComponentProps) {
    this.setState({ typeHelp: undefined });
    if (props.ctx.value.entityType)
      TypeHelpClient.API.typeHelp(props.ctx.value.entityType!.cleanName, "CSharp")
        .then(th => this.setState({ typeHelp: th }))
        .done();
  }

  handleTypeChange = () => {
    this.updateViewNames(this.props);
    this.updateTypeHelp(this.props);
  }

  handleTypeRemove = () => {
    if (this.state.scriptChanged == true)
      return MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.loseCurrentChanges.niceToString(),
        buttons: "yes_no",
        icon: "warning",
        style: "warning"
      }).then(result => { return result == "yes" });

    return Promise.resolve(true);
  }

  handleRemoveClick = (lambda: string) => {
    setTimeout(() => this.showPropmt("Remove", `vr.removeLine(${lambda})`), 0);
  }

  handleInsertBeforeClick = (lambda: string) => {
    setTimeout(() => this.showPropmt("InsertBefore", `vr.insertBeforeLine(${lambda}, ctx => [yourElement]);`), 0);
  }

  handleInsertAfterClick = (lambda: string) => {
    setTimeout(() => this.showPropmt("InsertAfter", `vr.insertAfterLine(${lambda}, ctx => [yourElement]);`), 0);
  }

  handleRenderContextualMenu = (pr: PropertyRoute) => {
    const lambda = "e => " + TypeHelpComponent.getExpression("e", pr, "TypeScript");
    return (
      <DropdownItem>
        <DropdownItem header>{pr.propertyPath()}</DropdownItem>
        <DropdownItem divider />
        <DropdownItem onClick={() => this.handleRemoveClick(lambda)}><FontAwesomeIcon icon="trash" />&nbsp; Remove</DropdownItem>
        <DropdownItem onClick={() => this.handleInsertBeforeClick(lambda)}><FontAwesomeIcon icon="arrow-up" />&nbsp; Insert Before</DropdownItem>
        <DropdownItem onClick={() => this.handleInsertAfterClick(lambda)}><FontAwesomeIcon icon="arrow-down" />&nbsp; Insert After</DropdownItem>
      </DropdownItem>
    );
  }

  handleTypeHelpClick = (pr: PropertyRoute | undefined) => {
    if (!pr || !pr.member || !pr.parent || pr.parent.propertyRouteType != "Mixin")
      return;

    var node = Nodes.NodeConstructor.appropiateComponent(pr.member, pr.propertyPath());
    if (!node)
      return;

    const expression = TypeHelpComponent.getExpression("o", pr, "TypeScript");
    const text = `modules.React.createElement(${node.kind}, { ctx: ctx.subCtx(o => ${expression}) })`;

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: text,
      valueLineType: "TextArea",
      title: "Mixin Template",
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    }).done();
  }

  render() {
    const ctx = this.props.ctx;

    return (
      <div>
        <EntityLine ctx={ctx.subCtx(a => a.entityType)} onChange={this.handleTypeChange} onRemove={this.handleTypeRemove} />
        {
          ctx.value.entityType && this.state.viewNames &&
          <FormGroup ctx={ctx.subCtx(d => d.viewName)} labelText={ctx.niceName(d => d.viewName)}>
            {
              <select value={ctx.value.viewName ? ctx.value.viewName : ""} className="form-control" onChange={this.handleViewNameChange}>
                <option value="">{" - "}</option>
                {(this.state.viewNames || []).map((v, i) => <option key={i} value={v}>{v}</option>)}
              </select>
            }
          </FormGroup>
        }

        {ctx.value.entityType &&
          <div>
            <br />
            <div className="row">
              <div className="col-sm-7">
                {this.renderExampleEntity(ctx.value.entityType!.cleanName)}
                {this.renderEditor()}
              </div>
              <div className="col-sm-5">
                <TypeHelpComponent
                  initialType={ctx.value.entityType.cleanName}
                  mode="TypeScript"
                  renderContextMenu={this.handleRenderContextualMenu}
                  onMemberClick={this.handleTypeHelpClick} />
                <br />
              </div>
            </div>
            <hr />
            {this.renderTest()}
          </div>
        }
      </div>
    );
  }

  handleViewNameChange = (e: React.SyntheticEvent<HTMLSelectElement>) => {
    this.props.ctx.value.viewName = (e.currentTarget as HTMLSelectElement).value;
    this.props.ctx.value.modified = true;
    this.forceUpdate();
  };

  renderTest() {
    const ctx = this.props.ctx;
    return (
      <div>
        {this.state.exampleEntity && this.state.componentClass &&
          <RenderWithReplacements entity={this.state.exampleEntity}
            componentClass={this.state.componentClass}
            viewOverride={this.state.viewOverride} />}
      </div>
    );
  }

  renderExampleEntity(typeName: string) {
    const exampleCtx = new TypeContext<Entity | undefined>(undefined, undefined, PropertyRoute.root(typeName), Binding.create(this.state, s => s.exampleEntity));

    return (
      <div className="code-container">
        <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} view={true} onView={this.handleOnView} onChange={this.handleEntityChange} formGroupStyle="Basic"
          type={{ name: typeName }} labelText={DynamicViewMessage.ExampleEntity.niceToString()} />
      </div>
    );
  }

  handleOnView = (exampleEntity: Entity) => {
    return Navigator.view(exampleEntity, { requiresSaveOperation: false, isOperationVisible: eoc => false });
  }

  handleCodeChange = (newCode: string) => {
    var dvo = this.props.ctx.value;

    if (dvo.script != newCode) {
      dvo.script = newCode;
      dvo.modified = true;
      this.setState({ scriptChanged: true });
      this.compileFunction();
    };
  }

  handleEntityChange = () => {

    if (!this.state.exampleEntity)
      this.setState({ componentClass: undefined });
    else {

      const entity = this.state.exampleEntity;
      const settings = Navigator.getSettings(entity.Type);

      if (!settings)
        this.setState({ componentClass: null });

      else {
        const ctx = this.props.ctx;
        return Navigator.viewDispatcher.getViewPromise(entity, ctx.value.viewName || undefined).promise.then(func => {
          var tempCtx = new TypeContext(undefined, undefined, PropertyRoute.root(entity.Type), new ReadonlyBinding(entity, "example"));
          var re = func(tempCtx);
          this.setState({ componentClass: re.type as React.ComponentClass<{ ctx: TypeContext<Entity> }> });
          this.compileFunction();
        });
      }
    }
  }

  compileFunction() {

    this.setState({
      syntaxError: undefined,
      viewOverride: undefined,
    });

    const dvo = this.props.ctx.value;
    let func: (rep: ViewReplacer<Entity>) => void;
    try {
      func = DynamicViewClient.asOverrideFunction(dvo);
      this.setState({
        viewOverride: func
      });
    } catch (e) {
      this.setState({
        syntaxError: (e as Error).message
      });
      return;
    }
  }

  renderEditor() {

    const ctx = this.props.ctx;
    return (
      <div className="code-container">
        <div className="btn-toolbar btn-toolbar-small">
          {this.state.viewNames && this.renderViewNameButtons()}
          {this.allExpressions().length > 0 && this.renderExpressionsButtons()}
          <TypeHelpButtonBarComponent typeName={ctx.value.entityType!.cleanName} mode="TypeScript" ctx={ctx} />
        </div>
        <pre style={{ border: "0px", margin: "0px" }}>{`(vr: ViewReplacer<${ctx.value.entityType!.className}>, modules) =>`}</pre>
        <JavascriptCodeMirror code={ctx.value.script || ""} onChange={this.handleCodeChange} />
        {this.state.syntaxError && <div className="alert alert-danger">{this.state.syntaxError}</div>}
      </div>
    );
  }

  handleViewNameClick = (viewName: string) => {
    this.showPropmt("View", `modules.React.createElement(RenderEntity, {ctx: ctx, getViewPromise: ctx => "${viewName}"})`);
  }

  renderViewNameButtons() {
    return (
      <UncontrolledDropdown>
        <DropdownToggle color="success" caret>View Names</DropdownToggle>
        <DropdownMenu>
          {this.state.viewNames!.map((vn, i) =>
            <DropdownItem key={i} onClick={() => this.handleViewNameClick(vn)}>{vn}</DropdownItem>)}
        </DropdownMenu>
      </UncontrolledDropdown>
    );
  }

  allExpressions() {
    var typeHelp = this.state.typeHelp;
    if (!typeHelp)
      return [];

    return typeHelp.members.filter(m => m.name && m.isExpression == true);
  }

  handleExpressionClick = (member: TypeHelpClient.TypeMemberHelp) => {
    var paramValue = member.cleanTypeName ? `queryName : "${member.cleanTypeName}Entity"` : `valueToken: "Entity.${member.name}"`;
    this.showPropmt("Expression", `modules.React.createElement(ValueSearchControlLine, {ctx: ctx, ${paramValue}})`);
  }

  renderExpressionsButtons() {
    return (
      <UncontrolledDropdown>
        <DropdownToggle color="warning" caret>Expressions</DropdownToggle>
        <DropdownMenu>
          {this.allExpressions().map((m, i) =>
            <DropdownItem key={i} onClick={() => this.handleExpressionClick(m)}>{m.name}</DropdownItem>)}
        </DropdownMenu>
      </UncontrolledDropdown>
    );
  }

  showPropmt(title: string, text: string) {

    ValueLineModal.show({
      type: { name: "string" },
      initialValue: text,
      valueLineType: "TextArea",
      title: title,
      message: "Copy to clipboard: Ctrl+C, ESC",
      initiallyFocused: true,
    }).done();
  }
}


interface RenderWithReplacementsProps {
  entity: Entity;
  componentClass: React.ComponentClass<{ ctx: TypeContext<Entity> }>;
  viewOverride?: (vr: ViewReplacer<Entity>) => void;
}

export class RenderWithReplacements extends React.Component<RenderWithReplacementsProps> {


  originalRender: any;
  componentWillMount() {

    this.originalRender = this.props.componentClass.prototype.render;

    DynamicViewClient.unPatchComponent(this.props.componentClass);

    if (this.props.viewOverride)
      DynamicViewClient.patchComponent(this.props.componentClass, this.props.viewOverride);
  }

  componentWillReceiveProps(newProps: RenderWithReplacementsProps) {
    if (newProps.componentClass != this.props.componentClass)
      throw new Error("not implemented");

    if (newProps.viewOverride != this.props.viewOverride) {
      DynamicViewClient.unPatchComponent(this.props.componentClass);
      if (newProps.viewOverride)
        DynamicViewClient.patchComponent(this.props.componentClass, newProps.viewOverride);
    }
  }

  componentWillUnmount() {
    this.props.componentClass.prototype.render = this.originalRender;
  }

  render() {

    var ctx = new TypeContext(undefined, undefined, PropertyRoute.root(this.props.entity.Type), new ReadonlyBinding(this.props.entity, "example"));

    return React.createElement(this.props.componentClass, { ctx: ctx });
  }
}

