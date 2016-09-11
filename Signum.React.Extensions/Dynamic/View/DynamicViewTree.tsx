import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, DomUtils, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import ContextMenu from '../../../../Framework/Signum.React/Scripts/SearchControl/ContextMenu'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal'
import { MenuItem } from 'react-bootstrap'

import * as NodeUtils from './NodeUtils'
import NodeSelectorModal from './NodeSelectorModal'
import { DesignerContext, DesignerNode } from './NodeUtils'
import { BaseNode, ContainerNode, LineBaseNode } from './Nodes'
import { DynamicViewEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'

require("!style!css!./DynamicViewTree.css");

export interface DynamicViewTreeProps {
    rootNode: DesignerNode<BaseNode>;
    selectedNode?: DesignerNode<BaseNode>;
    onSelected: (newNode: DesignerNode<BaseNode>) => void 
}

export interface DnamicViewTreeState {
    contextualMenu?: {
        position: { pageX: number, pageY: number };
    };
}

export class DynamicViewTree extends React.Component<DynamicViewTreeProps, DnamicViewTreeState>{

    constructor(props: DynamicViewTreeProps) {
        super(props);
        this.state = {};
    }

    handleNodeTextContextMenu = (n: DesignerNode<BaseNode>, e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();

        const op = DomUtils.offsetParent(this.treeContainer);

        this.props.onSelected(n);
        this.changeState(s => {
            s.contextualMenu = {
                position: {
                    pageX: e.pageX - (op ? op.getBoundingClientRect().left : 0),
                    pageY: e.pageY - (op ? op.getBoundingClientRect().top : 0)
                }
            };
        });
    }

    treeContainer: HTMLElement;

    render() {

        const sn = this.props.selectedNode;

        return (
            <div>
                <div className="dynamic-view-tree" ref={(t) => { this.treeContainer = t } } >
                    <ul>
                        <DynamicViewNode
                            node={this.props.rootNode}
                            selectedNode={sn && sn.node}
                            onContextMenu={this.handleNodeTextContextMenu}
                            onSelected={this.props.onSelected}
                            />
                    </ul>
                </div>
                {this.state.contextualMenu && this.renderContextualMenu()}
            </div>
        );
    }

    renderContextualMenu() {
        const cm = this.state.contextualMenu!;
        if (!this.props.selectedNode)
            return null;

        const dn = this.props.selectedNode;

        const isContainer = NodeUtils.registeredNodes[dn.node.kind].isContainer;
        const isRoot = (dn == this.props.rootNode);
        
        return (
            <ContextMenu position={cm.position} onHide={this.handleContextOnHide}>
                {isContainer && <MenuItem onClick={this.handleAddChildren}><i className="fa fa-arrow-down" aria-hidden="true"></i>&nbsp; {DynamicViewMessage.AddChild.niceToString()}</MenuItem>}
                {!isRoot && <MenuItem onClick={this.handleAddSibling}><i className="fa fa-arrow-down" aria-hidden="true"></i>&nbsp; {DynamicViewMessage.AddSibling.niceToString()}</MenuItem>}
                {!isRoot && <MenuItem onClick={this.handleRemove} bsClass="danger"><i className="fa fa-trash" aria-hidden="true"></i>&nbsp; {DynamicViewMessage.Remove.niceToString()}</MenuItem>}
            </ContextMenu>
        );
    }

    handleContextOnHide = () => {
        this.changeState(s => s.contextualMenu = undefined);
    }

    handleAddChildren = () => {
        const parent = this.props.selectedNode! as DesignerNode<ContainerNode>;

        this.newNode(parent.node.kind).then(n => {
            if (!n)
                return;

            parent.node.children.push(n);
            this.props.onSelected(parent.createChild(n));
            parent.context.refreshView();
        });
    }

    handleAddSibling = () => {
        var sibling = this.props.selectedNode!;
        var parent = sibling.parent! as DesignerNode<ContainerNode>;
        this.newNode(parent.node.kind).then(n => {
            if (!n)
                return;

            parent.node.children.insertAt(parent.node.children.indexOf(sibling.node) + 1, n);
            this.props.onSelected(parent.createChild(n));
            parent.context.refreshView();
        });
    }

    handleRemove = () => {

        var selected = this.props.selectedNode!;
        var parent = selected.parent as DesignerNode<ContainerNode>;
        var nodeIndex = parent.node.children.indexOf(selected.node);

        parent.node.children.remove(selected.node);
        this.props.onSelected(parent);
        parent.context.refreshView();
    }

    newNode(parentType: string): Promise<BaseNode | undefined>{
        return NodeSelectorModal.chooseElement(parentType).then(t => {

                if (!t)
                    return undefined;

                var node: BaseNode = { kind: t.kind };

                if (t.isContainer)
                    (node as ContainerNode).children = [];

                if (t.initialize)
                    t.initialize(node);  

                return node;
            });
    }
}

function allNodes(node: BaseNode): BaseNode[] {
    return [node].concat((node as ContainerNode).children ? (node as ContainerNode).children.flatMap(allNodes) : []);
}


export interface DynamicViewNodeProps {
    node: DesignerNode<BaseNode>;
    selectedNode?: BaseNode;
    onSelected: (newNode: DesignerNode<BaseNode>) => void;
    onContextMenu: (n: DesignerNode<BaseNode>, e: React.MouseEvent) => void;
}

export class DynamicViewNode extends React.Component<DynamicViewNodeProps, { isOpened: boolean }>{

    constructor(props: DynamicViewNodeProps) {
        super(props);
        this.state = { isOpened: true };
    }

    handleIconClick = () => {
        this.changeState(s => s.isOpened = !s.isOpened);
    }

    renderIcon() {
        var c = this.props.node.node as ContainerNode;

        if (!c.children || c.children.length == 0)
            return <span className="place-holder" />;

        if (this.state.isOpened) 
            return <span onClick={this.handleIconClick} className="tree-icon fa fa-minus-square-o" />;
         else 
            return <span onClick={this.handleIconClick} className="tree-icon fa fa-plus-square-o" />;
    }

    render(): React.ReactElement<any> {
        var dn = this.props.node;

        var container = dn.node as ContainerNode;

        const error = NodeUtils.validate(dn);



        return (
            <li>
                {this.renderIcon()}

                <span className={classes("tree-label", dn.node == this.props.selectedNode && "tree-selected", error && "tree-error")}
                    title={error || undefined}
                    onClick={e => this.props.onSelected(dn)}
                    onContextMenu={e => this.props.onContextMenu(dn, e)}
                    >
                    {NodeUtils.registeredNodes[dn.node.kind].renderTreeNode(dn)}
                </span>

                {container.children && container.children.length > 0 && (this.state.isOpened) &&
                    <ul>
                        {container.children.map((c, i) =>
                        <DynamicViewNode
                            selectedNode={this.props.selectedNode}
                            onContextMenu={this.props.onContextMenu}
                            onSelected={this.props.onSelected}
                            key={i} node={dn.createChild(c)} />)}
                    </ul>
                }
            </li>
        );
    }
}
