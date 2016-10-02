import * as React from 'react'
import { Button } from 'react-bootstrap'
import { Dic, classes } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Constructor from '../../../../Framework/Signum.React/Scripts/Constructor'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { DynamicViewEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { ValueLine, EntityLine, TypeContext } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity, Entity, Lite, JavascriptMessage } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getTypeInfo, Binding, PropertyRoute } from '../../../../Framework/Signum.React/Scripts/Reflection'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal'
import { DynamicViewTree } from './DynamicViewTree'
import { DynamicViewInspector } from './Designer'
import { NodeConstructor, BaseNode } from './Nodes'
import { DesignerNode, DesignerContext } from './NodeUtils'
import * as NodeUtils from './NodeUtils'

require("!style!css!./DynamicView.css");

interface DynamicViewEntityComponentProps {
    ctx: TypeContext<DynamicViewEntity>;
}

interface DynamicViewEntityComponentState {
    exampleEntity?: Entity;
    rootNode?: DesignerNode<BaseNode>;
    selectedNode?: DesignerNode<BaseNode>;
}


export default class DynamicViewEntityComponent extends React.Component<DynamicViewEntityComponentProps, DynamicViewEntityComponentState> {

    constructor(props: DynamicViewEntityComponentProps) {
        super(props);

        this.state = {};
    }

    componentWillMount() {
        this.updateRoot();
    }

    updateStateSelectedNode(newNode: DesignerNode<BaseNode>) {
        this.changeState(s => s.selectedNode = newNode);
    }

    beforeSave() {
        const ctx = this.props.ctx;
        ctx.value.viewContent = JSON.stringify(this.state.rootNode!.node);
        ctx.value.modified = true;
    }

    updateRoot() {

        const ctx = this.props.ctx;

        if (ctx.value.viewContent == null) {
            this.changeState(s => { s.rootNode = undefined; s.selectedNode = undefined; });
            return;
        }

        const context = {
            refreshView: () => this.updateStateSelectedNode(this.state.selectedNode!.reCreateNode()),
            getSelectedNode: () => this.state.selectedNode,
            setSelectedNode: (newNode) => this.updateStateSelectedNode(newNode)
        } as DesignerContext;
        

        const baseNode = JSON.parse(ctx.value.viewContent) as BaseNode;

        const root = DesignerNode.root(baseNode, context, ctx.value.entityType!.cleanName);

        this.changeState(s => { s.rootNode = root; s.selectedNode = root; });
    }


    handleTypeChange = () => {
        this.state = {};
        var dve = this.props.ctx.value;
        if (dve.entityType == null) {
            dve.viewContent = null;
            this.changeState(s => s.exampleEntity = undefined);
        } else {
            dve.viewContent = JSON.stringify(NodeConstructor.createDefaultNode(getTypeInfo(dve.entityType.cleanName)));
        }
        this.updateRoot();
    }

    handleTypeRemove = () => {
        if (this.props.ctx.value.modified || this.props.ctx.value.viewContent != JSON.stringify(this.state.rootNode!.node))
            return Promise.resolve(confirm(JavascriptMessage.loseCurrentChanges.niceToString()));

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
        const root = this.state.rootNode!;

        const exampleCtx = new TypeContext<Entity | undefined>(undefined, undefined, root.route!, Binding.create(this.state, s => s.exampleEntity));

        return (
            <div className="design-main" style={{ marginTop: "10px" }}>
                <div className="design-left open">
                    <div className="form-vertical">
                        <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} viewOnCreate={false} view={false} onChange={() => this.forceUpdate()} formGroupStyle="Basic"
                            type={{ name: root.route!.rootType!.name }} labelText={DynamicViewMessage.ExampleEntity.niceToString()} />
                        <DynamicViewTree rootNode={root} />
                        <DynamicViewInspector selectedNode={root.context.getSelectedNode()} />
                    </div>
                </div>
                <div className="design-content open">
                    {this.state.exampleEntity && NodeUtils.render(root, exampleCtx as TypeContext<Entity>)}
                </div>
            </div>);
    }
}
