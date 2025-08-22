import * as React from 'react'
import { Navigator, ViewOverride } from '@framework/Navigator'
import { AutoLine, EntityLine, TypeContext } from '@framework/Lines'
import { ModifiableEntity, Entity, JavascriptMessage, SaveChangesMessage } from '@framework/Signum.Entities'
import { getTypeInfo, Binding } from '@framework/Reflection'
import MessageModal from '@framework/Modals/MessageModal'
import { DynamicViewTabs } from './DynamicViewTabs'
import { CollapsableTypeHelp } from './Designer'
import { NodeConstructor, BaseNode } from './Nodes'
import { DesignerNode, DesignerContext, RenderWithViewOverrides } from './NodeUtils'
import ShowCodeModal from './ShowCodeModal'
import { ButtonsContext, IRenderButtons, ButtonBarElement } from '@framework/TypeContext'
import "./DynamicView.css"
import { DynamicViewEntity, DynamicViewMessage } from '../Signum.Dynamic.Views'
import { JSX } from 'react'

interface DynamicViewEntityComponentProps {
  ctx: TypeContext<DynamicViewEntity>;
}

interface DynamicViewEntityComponentState {
  exampleEntity: Entity | null;
  rootNode?: BaseNode;
  selectedNode?: DesignerNode<BaseNode>;
  viewOverrides?: ViewOverride<ModifiableEntity>[];
}

export default class DynamicViewEntityComponent extends React.Component<DynamicViewEntityComponentProps, DynamicViewEntityComponentState> implements IRenderButtons {

  constructor(props: DynamicViewEntityComponentProps) {
    super(props);
    this.state = { exampleEntity : null };
  }

  handleShowCode = (): undefined => {
    ShowCodeModal.showCode(this.props.ctx.value.entityType!.cleanName, this.state.rootNode!);
  }

  renderButtons(bc: ButtonsContext): ButtonBarElement[] {
    return [
      { button: <button key="showCode" type="button" className="btn btn-success float-end" disabled={!this.state.rootNode} onClick={this.handleShowCode}>Show code</button> }
    ];
  }

  componentWillMount(): undefined {
    this.updateRoot();
  }

  updateStateSelectedNode(newNode: DesignerNode<BaseNode>): undefined {
    this.setState({ selectedNode: newNode });
  }

  beforeSave(): undefined{
    const ctx = this.props.ctx;
    ctx.value.viewContent = JSON.stringify(this.state.rootNode!);
    ctx.value.modified = true;
  }

  updateRoot(): undefined {

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
        .then(vos => this.setState({ viewOverrides: vos }));
    else
      this.setState({ viewOverrides: undefined });

    ctx.frame!.frameComponent.forceUpdate();
  }

  getZeroNode(): DesignerNode<BaseNode>{

    var { ctx, ...extraProps } = this.props;

    const context: DesignerContext = {
      refreshView: () => {
        this.updateStateSelectedNode(this.state.selectedNode!.reCreateNode());
        this.props.ctx.value.modified = true;
      },
      getSelectedNode: () => this.state.selectedNode,
      setSelectedNode: (newNode) => {
        this.updateStateSelectedNode(newNode);
        this.props.ctx.value.modified = true;
      },
      onClose: () => { },
      props: extraProps,
      propTypes: this.props.ctx.value.props.toObject(a => a.element.name, a => a.element.type),
      locals: {},
      localsCode: this.props.ctx.value.locals,
    };

    return DesignerNode.zero(context, this.props.ctx.value.entityType!.cleanName);
  }

  handleTypeChange = () : void => {

    this.state = { exampleEntity : null };

    var dve = this.props.ctx.value;

    if (dve.entityType == null) {
      dve.viewContent = null!;
      this.setState({ exampleEntity: null });
    } else {
      dve.viewContent = JSON.stringify(NodeConstructor.createDefaultNode(getTypeInfo(dve.entityType.cleanName)));
    }

    this.updateRoot();
  }

  handleTypeRemove = (): Promise<boolean> => {
    if (this.props.ctx.value.modified || this.props.ctx.value.viewContent != JSON.stringify(this.state.rootNode!))
      return MessageModal.show({
        title: SaveChangesMessage.ThereAreChanges.niceToString(),
        message: JavascriptMessage.loseCurrentChanges.niceToString(),
        buttons: "yes_no",
        icon: "warning",
        style: "warning"
      }).then(result => { return result == "yes"; });

    return Promise.resolve(true);
  }

  render(): JSX.Element {
    const ctx = this.props.ctx;

    return (
      <div>
        <AutoLine ctx={ctx.subCtx(dv => dv.viewName)} />
        <EntityLine ctx={ctx.subCtx(dv => dv.entityType)} onChange={this.handleTypeChange} onRemove={this.handleTypeRemove} />

        {this.state.rootNode && this.renderDesigner()}
      </div>
    );
  }

  renderDesigner(): JSX.Element{
    const root = this.getZeroNode().createChild(this.state.rootNode!);

    const ctx = this.props.ctx;

    const exampleCtx = new TypeContext<Entity | null>(undefined, { frame: ctx.frame }, root.route!, Binding.create(this.state, s => s.exampleEntity));

    return (
      <div className="design-main" style={{ marginTop: "10px" }}>
        <div className="design-left open">
          <div className="code-container">
            <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} viewOnCreate={false} view={false} onChange={() => this.forceUpdate()} formGroupStyle="Basic"
              type={{ name: this.props.ctx.value.entityType!.cleanName }} label={DynamicViewMessage.ExampleEntity.niceToString()} />
            <DynamicViewTabs ctx={this.props.ctx} rootNode={root} />
            <CollapsableTypeHelp initialTypeName={ctx.value.entityType!.cleanName} />
          </div>
        </div>
        <div className="design-content open">
          {this.state.exampleEntity && this.state.viewOverrides && <RenderWithViewOverrides dn={root} parentCtx={exampleCtx as TypeContext<Entity>} vos={this.state.viewOverrides.filter(a => a.viewName == ctx.value.viewName)} />}
        </div>
      </div>
    );
  }
}
