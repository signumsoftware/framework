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
import { DynamicViewInspector, CollapsableTypeHelp } from './Designer'
import { NodeConstructor, BaseNode } from './Nodes'
import { DesignerNode, DesignerContext } from './NodeUtils'
import * as NodeUtils from './NodeUtils'

require("!style!css!./DynamicView.css");

interface DynamicViewEntityComponentProps {
    ctx: TypeContext<DynamicViewEntity>;
}

interface DynamicViewEntityComponentState {
    exampleEntity?: Entity;
    rootNode?: BaseNode;
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
        ctx.value.viewContent = JSON.stringify(this.state.rootNode!);
        ctx.value.modified = true;
    }

    updateRoot() {

        const ctx = this.props.ctx;

        if (ctx.value.viewContent == null) {
            this.changeState(s => { s.rootNode = undefined; s.selectedNode = undefined; });
            return;
        }

        const rootNode = JSON.parse(ctx.value.viewContent) as BaseNode;

        this.changeState(s => {
            s.rootNode = rootNode;
            s.selectedNode = this.getZeroNode().createChild(rootNode);
        });
    }

    getZeroNode() {
        const context = {
            refreshView: () => this.updateStateSelectedNode(this.state.selectedNode!.reCreateNode()),
            getSelectedNode: () => this.state.selectedNode,
            setSelectedNode: (newNode) => this.updateStateSelectedNode(newNode)
        } as DesignerContext;

        return DesignerNode.zero(context, this.props.ctx.value.entityType!.cleanName);
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
        if (this.props.ctx.value.modified || this.props.ctx.value.viewContent != JSON.stringify(this.state.rootNode!))
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
        const root = this.getZeroNode().createChild(this.state.rootNode!);


        const ctx = this.props.ctx;

        const exampleCtx = new TypeContext<Entity | undefined>(undefined, { frame: ctx.frame }, root.route!, Binding.create(this.state, s => s.exampleEntity));

        return (
            <div className="design-main" style={{ marginTop: "10px" }}>
                <div className="design-left open">
                    <div className="form-vertical code-container">
                        <EntityLine ctx={exampleCtx} create={true} find={true} remove={true} viewOnCreate={false} view={false} onChange={() => this.forceUpdate()} formGroupStyle="Basic"
                            type={{ name: this.props.ctx.value.entityType!.cleanName }} labelText={DynamicViewMessage.ExampleEntity.niceToString()} />
                        <DynamicViewTree rootNode={root} />
                        <DynamicViewInspector selectedNode={root.context.getSelectedNode()} />
                        <CollapsableTypeHelp initialTypeName={ctx.value.entityType!.cleanName} />
                    </div>
                </div>
                <div className="design-content open">
                    {this.state.exampleEntity && NodeUtils.renderWithViewOverrides(root, exampleCtx as TypeContext<Entity>)}
                </div>
            </div>);
    }
}
