import * as React from 'react'
import * as Navigator from '@framework/Navigator'
import { DynamicViewEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, TypeContext } from '@framework/Lines'
import { ModifiableEntity, Entity, JavascriptMessage, NormalWindowMessage } from '@framework/Signum.Entities'
import { getTypeInfo, Binding } from '@framework/Reflection'
import MessageModal from '@framework/Modals/MessageModal'
import { DynamicViewTree } from './DynamicViewTree'
import { DynamicViewInspector, CollapsableTypeHelp } from './Designer'
import { NodeConstructor, BaseNode } from './Nodes'
import { DesignerNode, DesignerContext } from './NodeUtils'
import * as NodeUtils from './NodeUtils'
import ShowCodeModal from './ShowCodeModal'
import { ButtonsContext, IRenderButtons } from '@framework/TypeContext'
import "./DynamicView.css"

interface DynamicViewEntityComponentProps {
  ctx: TypeContext<DynamicViewEntity>;
}

interface DynamicViewEntityComponentState {
  exampleEntity?: Entity;
  rootNode?: BaseNode;
  selectedNode?: DesignerNode<BaseNode>;
  viewOverrides?: Navigator.ViewOverride<ModifiableEntity>[];
}

export default class DynamicViewEntityComponent extends React.Component<DynamicViewEntityComponentProps, DynamicViewEntityComponentState> implements IRenderButtons {

  constructor(props: DynamicViewEntityComponentProps) {
    super(props);
    this.state = {};
  }

  handleShowCode = () => {
    ShowCodeModal.showCode(this.props.ctx.value.entityType!.cleanName, this.state.rootNode!);
  }

  renderButtons(bc: ButtonsContext) {
    return [
      <button key="showCode" type="button" className="btn btn-success float-right" disabled={!this.state.rootNode} onClick={this.handleShowCode}>Show code</button>
    ];
  }

  componentWillMount() {
    this.updateRoot();
  }

  updateStateSelectedNode(newNode: DesignerNode<BaseNode>) {
    this.setState({ selectedNode: newNode });
  }

  beforeSave() {
    const ctx = this.props.ctx;
    ctx.value.viewContent = JSON.stringify(this.state.rootNode!);
    ctx.value.modified = true;
  }

  updateRoot() {

    const ctx = this.props.ctx;

    if (ctx.value.viewContent == null) {
      this.setState({
        rootNode: undefined,
        selectedNode: undefined
      });

    } else {
      const rootNode = JSON.parse(ctx.value.viewContent) as BaseNode;

      this.setState({
        rootNode: rootNode,
        selectedNode: this.getZeroNode().createChild(rootNode)
      });
    }

    if (ctx.value.entityType)
      Navigator.viewDispatcher.getViewOverrides(ctx.value.entityType.cleanName)
        .then(vos => this.setState({ viewOverrides: vos }))
        .done();
    else
      this.setState({ viewOverrides: undefined });

    ctx.frame!.frameComponent.forceUpdate();
  }

  getZeroNode() {
    const context = {
      refreshView: () => {
        this.updateStateSelectedNode(this.state.selectedNode!.reCreateNode());
        this.props.ctx.value.modified = true;
      },
      getSelectedNode: () => this.state.selectedNode,
      setSelectedNode: (newNode) => {
        this.updateStateSelectedNode(newNode);
        this.props.ctx.value.modified = true;
      }
    } as DesignerContext;

    return DesignerNode.zero(context, this.props.ctx.value.entityType!.cleanName);
  }

  handleTypeChange = () => {

    this.state = {};

    var dve = this.props.ctx.value;

    if (dve.entityType == null) {
      dve.viewContent = null;
      this.setState({ exampleEntity: undefined });
    } else {
      dve.viewContent = JSON.stringify(NodeConstructor.createDefaultNode(getTypeInfo(dve.entityType.cleanName)));
    }

    this.updateRoot();
  }

  handleTypeRemove = () => {
    if (this.props.ctx.value.modified || this.props.ctx.value.viewContent != JSON.stringify(this.state.rootNode!))
      return MessageModal.show({
        title: NormalWindowMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.loseCurrentChanges.niceToString(),
        buttons: "yes_no",
        icon: "warning",
        style: "warning"
      }).then(result => { return result == "yes"; });

    return Promise.resolve(true);
  }

  render() {
    const ctx = this.props.ctx;

    return (
      <div>
        <ValueLine ctx={ctx.subCtx(dv => dv.viewName)} />
        <EntityLine ctx={ctx.subCtx(dv => dv.entityType)} onChange={this.handleTypeChange} onRemove={this.handleTypeRemove} />

        {this.state.rootNode && this.renderDesigner()}
      </div>
    );
  }

  renderDesigner() {
    const root = this.getZeroNode().createChild(this.state.rootNode!);

    const ctx = this.props.ctx;

    const exampleCtx = new TypeContext<Entity | undefined>(undefined, { frame: ctx.frame }, root.route!, Binding.create(this.state, s => s.exampleEntity));

    return (
      <div className="design-main" style={{ marginTop: "10px" }}>
        <div className="design-left open">
          <div className="code-container">
            <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} viewOnCreate={false} view={false} onChange={() => this.forceUpdate()} formGroupStyle="Basic"
              type={{ name: this.props.ctx.value.entityType!.cleanName }} labelText={DynamicViewMessage.ExampleEntity.niceToString()} />
            <DynamicViewTree rootNode={root} />
            <DynamicViewInspector selectedNode={root.context.getSelectedNode()} />
            <CollapsableTypeHelp initialTypeName={ctx.value.entityType!.cleanName} />
          </div>
        </div>
        <div className="design-content open">
          {this.state.exampleEntity && this.state.viewOverrides && NodeUtils.renderWithViewOverrides(root, exampleCtx as TypeContext<Entity>, this.state.viewOverrides.filter(a => a.viewName == ctx.value.viewName))}
        </div>
      </div>);
  }
}
