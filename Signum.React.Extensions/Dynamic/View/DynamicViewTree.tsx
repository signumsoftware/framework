import * as React from 'react'
import { FormGroup, FormControlStatic, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '../../../../Framework/Signum.React/Scripts/Lines'
import { ModifiableEntity } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { classes, DomUtils, Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder'
import { FindOptions } from '../../../../Framework/Signum.React/Scripts/FindOptions'
import { getQueryNiceName } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import { TypeContext, FormGroupStyle } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { DesignerContext, BaseNode, ContainerNode } from './Nodes'
import ContextMenu from '../../../../Framework/Signum.React/Scripts/SearchControl/ContextMenu'
import SelectorModal from '../../../../Framework/Signum.React/Scripts/SelectorModal'
import { MenuItem } from 'react-bootstrap'

import * as Nodes from './Nodes'
import { DynamicViewEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'

export interface DynamicViewTreeProps {
    rootNode: BaseNode;
    selectedNode?: BaseNode;
    dc: DesignerContext,
    onSelected: (newNode: BaseNode) => void 
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

    handleNodeTextContextMenu = (n: BaseNode, e: React.MouseEvent) => {
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
        return (
            <div>
                <div className="dynamic-view-tree" ref={(t) => { this.treeContainer = t } } >
                    <DynamicViewNode
                        node={this.props.rootNode}
                        selectedNode={this.props.selectedNode}
                        onContextMenu={this.handleNodeTextContextMenu}
                        onSelected={this.props.onSelected}
                        />
                </div>
                {this.state.contextualMenu && this.renderContextualMenu()}
            </div>
        );
    }

    renderContextualMenu() {
        const cm = this.state.contextualMenu!;
        if (!this.props.selectedNode)
            return null;

        const node = this.props.selectedNode;

        const isContainer = Nodes.registeredNodes[node.kind].isContainer;
        const isRoot = (node == this.props.rootNode);
        
        return (
            <ContextMenu position={cm.position} onHide={this.handleContextOnHide}>
                {isContainer && <MenuItem onClick={this.handleAddChildren}><i className="fa fa-arrow-down" aria-hidden="true"></i>&nbsp; {DynamicViewMessage.AddChild.niceToString()}</MenuItem>}
                {isContainer && !isRoot && <MenuItem onClick={this.handleAddSibling}><i className="fa fa-arrow-down" aria-hidden="true"></i>&nbsp; {DynamicViewMessage.AddSibling.niceToString()}</MenuItem>}
                {!isRoot && <MenuItem onClick={this.handleRemove} bsClass="danger"><i className="fa fa-trash" aria-hidden="true"></i>&nbsp; {DynamicViewMessage.Remove.niceToString()}</MenuItem>}
            </ContextMenu>
        );
    }

    handleContextOnHide = () => {
        this.changeState(s => s.contextualMenu = undefined);
    }

    handleAddChildren = () => {
        var parent = this.props.selectedNode! as ContainerNode;

        this.newNode().then(n => {
            if (!n)
                return;

            parent.children.push(n);
            this.props.onSelected(n);
        });
    }

    findParent(childNode: BaseNode): ContainerNode {
        return allNodes(this.props.rootNode)
            .filter(n => (n as ContainerNode).children && (n as ContainerNode).children.contains(childNode))
            .single() as ContainerNode;
    }
    
    handleAddSibling = () => {
        var sibling = this.props.selectedNode!;
        var parent = this.findParent(sibling);
        this.newNode().then(n => {
            if (!n)
                return;

            parent.children.insertAt(parent.children.indexOf(sibling) + 1, n);
            this.props.onSelected(n);
        });
    }

    handleRemove = () => {

        var node = this.props.selectedNode!;
        var parent = this.findParent(node);
        var nodeIndex = parent.children.indexOf(node);

        parent.children.remove(node);
        this.props.onSelected(nodeIndex < parent.children.length ? parent.children[nodeIndex] : parent);
    }

    newNode(): Promise<BaseNode | undefined>{
        return SelectorModal.chooseElement(Dic.getValues(Nodes.registeredNodes),
            { message: DynamicViewMessage.SelectATypeOfComponent.niceToString(), display: a => a.kind }).then(t => {

                if (!t)
                    return undefined;

                var newElement: BaseNode = { kind: t.kind };

                if (t.isContainer)
                    (newElement as ContainerNode).children = [];

                return newElement;
            });
    }
}

function allNodes(node: BaseNode): BaseNode[] {
    return [node].concat((node as ContainerNode).children ? (node as ContainerNode).children.flatMap(allNodes) : []);
}


export interface DynamicViewNodeProps {
    node: BaseNode;
    selectedNode?: BaseNode;
    onSelected: (newNode: BaseNode) => void,
    onContextMenu: (n: BaseNode, e: React.MouseEvent) => void,
}

export class DynamicViewNode extends React.Component<DynamicViewNodeProps, { isOpened: boolean }>{

    constructor(props: DynamicViewNodeProps) {
        super(props);
        this.state = { isOpened: false };
    }

    handleIconClick = () => {
        this.changeState(s => s.isOpened = !s.isOpened);
    }

    renderIcon() {
        if (!Nodes.registeredNodes[this.props.node.kind].isContainer)
            return <span className="place-holder" />;

        if (this.state.isOpened) 
            return <span onClick={this.handleIconClick} className="tree-icon fa fa-minus-square-o" />;
         else 
            return <span onClick={this.handleIconClick} className="tree-icon fa fa-plus-square-o" />;
    }


    render(): React.ReactElement<any> {
        var node = this.props.node;

        var title = node.kind;

        if ((node as any).route)
            title += ": " + (node as any).route;  

        var container = node as ContainerNode;

        return (
            <li>
                {this.renderIcon()}

                <span className={classes("tree-label", node == this.props.selectedNode && "tree-selected")}
                    onClick={e => this.props.onSelected(node)}
                    onContextMenu={e => this.props.onContextMenu(node, e)}
                    >
                    {title}
                </span>

                {container.children && container.children.length > 0 && (this.state.isOpened) &&
                    <ul>
                        {container.children.map((c, i) =>
                        <DynamicViewNode
                            selectedNode={this.props.selectedNode}
                            onContextMenu={this.props.onContextMenu}
                            onSelected={this.props.onSelected}
                            key={i} node={c} />)}
                    </ul>
                }
            </li>
        );
    }
}
