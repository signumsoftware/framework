import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { FormGroup, FormControlReadonly, ValueLine, ValueLineType, EntityLine, EntityCombo, EntityList, EntityRepeater } from '@framework/Lines'
import { ModifiableEntity } from '@framework/Signum.Entities'
import { classes, DomUtils, Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import { FindOptions } from '@framework/FindOptions'
import { getQueryNiceName } from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import { TypeContext, FormGroupStyle } from '@framework/TypeContext'
import ContextMenu from '@framework/SearchControl/ContextMenu'
import { ContextMenuPosition } from '@framework/SearchControl/ContextMenu'
import SelectorModal from '@framework/SelectorModal'

import * as NodeUtils from './NodeUtils'
import NodeSelectorModal from './NodeSelectorModal'
import { DesignerContext, DesignerNode } from './NodeUtils'
import { BaseNode, ContainerNode, LineBaseNode, NodeConstructor, EntityTableNode, EntityTableColumnNode } from './Nodes'
import { DynamicViewEntity, DynamicViewMessage } from '../Signum.Entities.Dynamic'

import "./DynamicViewTree.css"
import { DropdownItem } from '@framework/Components';

export interface DynamicViewTreeProps {
    rootNode: DesignerNode<BaseNode>;
}

export type DraggedPosition = "Top" | "Bottom" | "Middle";
export type DraggedError = "Error" | "Warning" | "Ok";

export interface DnamicViewTreeState {
    draggedNode?: DesignerNode<BaseNode>;
    draggedOver?: {
        dn: DesignerNode<BaseNode>;
        position: DraggedPosition;
        error: DraggedError
    }
    contextualMenu?: {
        position: ContextMenuPosition;
    };
}

export class DynamicViewTree extends React.Component<DynamicViewTreeProps, DnamicViewTreeState>{
    constructor(props: DynamicViewTreeProps) {
        super(props);
        this.state = {};
    }

    handleNodeTextContextMenu = (n: DesignerNode<BaseNode>, e: React.MouseEvent<any>) => {
        e.preventDefault();
        e.stopPropagation();

        this.props.rootNode.context.setSelectedNode(n);
        this.setState({
            contextualMenu : {
                position: ContextMenu.getPosition(e, this.treeContainer)
            }
        });
    }

    treeContainer!: HTMLElement;

    render() {
        return (
            <div>
                <div className="dynamic-view-tree" ref={(t) => this.treeContainer = t!} >
                    <ul>
                        <DynamicViewNode
                            node={this.props.rootNode}
                            dynamicTreeView={this} />
                    </ul>
                </div>
                {this.state.contextualMenu && this.renderContextualMenu()}
            </div>
        );
    }

    renderContextualMenu() {
        const cm = this.state.contextualMenu!;
        const dn = this.props.rootNode.context.getSelectedNode();
        if (!dn)
            return null;

        const no = NodeUtils.registeredNodes[dn.node.kind];

        const cn = dn.node as ContainerNode;

        const isRoot = (dn.node == this.props.rootNode.node);

        return (
            <ContextMenu position={cm.position} onHide={this.handleContextOnHide}>
                {no.isContainer && <DropdownItem onClick={this.handleAddChildren}><FontAwesomeIcon icon="arrow-right" />&nbsp; {DynamicViewMessage.AddChild.niceToString()}</DropdownItem>}
                {!isRoot && <DropdownItem onClick={this.handleAddSibling}><FontAwesomeIcon icon="arrow-down" />&nbsp; {DynamicViewMessage.AddSibling.niceToString()}</DropdownItem>}

                {no.isContainer && <DropdownItem divider={true} />}

                {no.isContainer && cn.children.length == 0 && dn.route && <DropdownItem onClick={this.handleGenerateChildren}><FontAwesomeIcon icon="bolt" />&nbsp; {DynamicViewMessage.GenerateChildren.niceToString()}</DropdownItem>}
                {no.isContainer && cn.children.length > 0 && <DropdownItem onClick={this.handleClearChildren} color="danger"><FontAwesomeIcon icon="trash" />&nbsp; {DynamicViewMessage.ClearChildren.niceToString()}</DropdownItem>}

                {!isRoot && <DropdownItem divider={true} />}
                {!isRoot && <DropdownItem onClick={this.handleRemove} color="danger"><FontAwesomeIcon icon="times" />&nbsp; {DynamicViewMessage.Remove.niceToString()}</DropdownItem>}
            </ContextMenu>
        );
    }

    handleContextOnHide = () => {
        this.setState({ contextualMenu: undefined });
    }

    handleAddChildren = () => {
        const parent = this.props.rootNode.context.getSelectedNode()! as DesignerNode<ContainerNode>;

        this.newNode(parent).then(n => {
            if (!n)
                return;

            parent.node.children.push(n);
            this.props.rootNode.context.setSelectedNode(parent.createChild(n));
            parent.context.refreshView();
        });
    }

    handleAddSibling = () => {
        var sibling = this.props.rootNode.context.getSelectedNode()!;
        var parent = sibling.parent! as DesignerNode<ContainerNode>;
        this.newNode(parent).then(n => {
            if (!n)
                return;

            parent.node.children.insertAt(parent.node.children.indexOf(sibling.node) + 1, n);
            this.props.rootNode.context.setSelectedNode(parent.createChild(n));
            parent.context.refreshView();
        });
    }

    handleRemove = () => {

        var selected = this.props.rootNode.context.getSelectedNode()!;
        var parent = selected.parent as DesignerNode<ContainerNode>;
        var nodeIndex = parent.node.children.indexOf(selected.node);

        parent.node.children.remove(selected.node);
        this.props.rootNode.context.setSelectedNode(parent);
        parent.context.refreshView();
    }

    handleClearChildren = () => {

        var selected = this.props.rootNode.context.getSelectedNode() ! as DesignerNode<ContainerNode>;
        selected.node.children.clear();
        selected.context.refreshView();
    }

    handleGenerateChildren = () => {

        var selected = this.props.rootNode.context.getSelectedNode()! as DesignerNode<ContainerNode>;
        if (selected.node.kind == "EntityTable")
            selected.node.children.push(...NodeConstructor.createEntityTableSubChildren(selected.fixRoute()!));
        else
            selected.node.children.push(...NodeConstructor.createSubChildren(selected.fixRoute()!));
        selected.context.refreshView();
    }

    newNode(parent: DesignerNode<ContainerNode>): Promise<BaseNode | undefined>{
        return NodeSelectorModal.chooseElement(parent.node.kind).then(t => {

                if (!t)
                    return undefined;

                var node: BaseNode = { kind: t.kind };

                if (t.isContainer)
                    (node as ContainerNode).children = [];

                if (t.initialize)
                    t.initialize(node, parent);  

                return node;
            });
    }
}

function allNodes(node: BaseNode): BaseNode[] {
    return [node].concat((node as ContainerNode).children ? (node as ContainerNode).children.flatMap(allNodes) : []);
}


export interface DynamicViewNodeProps {
    node: DesignerNode<BaseNode>;
    dynamicTreeView: DynamicViewTree;
}

export class DynamicViewNode extends React.Component<DynamicViewNodeProps, { isOpened: boolean }>{

    constructor(props: DynamicViewNodeProps) {
        super(props);
        this.state = { isOpened: true };
    }

    handleIconClick = () => {
        this.setState({ isOpened: !this.state.isOpened });
    }

    renderIcon() {
        var c = this.props.node.node as ContainerNode;

        if (!c.children || c.children.length == 0)
            return <span className="place-holder" />;

        if (this.state.isOpened) {
            return (
                <span onClick={this.handleIconClick} className="tree-icon">
                    <FontAwesomeIcon icon={["far", "minus-square"]} />
                </span>);
        }
        else {
            return (
                <span onClick={this.handleIconClick} className="tree-icon">
                    <FontAwesomeIcon icon={["far", "plus-square"]} />
                </span>);
        }
    }
        
    handleDragStart = (e: React.DragEvent<any>) => {
        e.dataTransfer.setData('text', "start"); //cannot be empty string
        e.dataTransfer.effectAllowed = "move";
        this.props.dynamicTreeView.setState({ draggedNode: this.props.node });
    }

    handleDragOver = (e: React.DragEvent<any>) => {
        e.preventDefault();
        const de = e.nativeEvent as DragEvent;
        const dn = this.props.node;
        const span = e.currentTarget as HTMLElement;
        const newPosition = this.getOffset(de.pageY, span.getBoundingClientRect(), 7);
        const newError = this.getError(newPosition);
        //de.dataTransfer.dropEffect = newError == "Error" ? "none" : "move";

        const s = this.props.dynamicTreeView.state;

        if (s.draggedOver == null ||
            s.draggedOver.dn.node != dn.node ||
            s.draggedOver.position != newPosition ||
            s.draggedOver.error != newError) {

            this.props.dynamicTreeView.setState({
                draggedOver : {
                    dn: dn,
                    position: newPosition,
                    error: newError
                }
            });
        }
    }

    getOffset(pageY: number, rect: ClientRect, margin: number): DraggedPosition {

        const height = Math.round(rect.height / 5) * 5;
        const offsetY = pageY - rect.top;

        if (offsetY < margin)
            return "Top";

        if (offsetY > (height - margin))
            return "Bottom";

        return "Middle";
    }

    getError(position: DraggedPosition): DraggedError {
        const parent = position == "Middle" ? this.props.node : this.props.node.parent;

        if (!parent || !parent.node)
            return "Error";

        const parentOptions = NodeUtils.registeredNodes[parent.node.kind];
        if (!parentOptions.isContainer)
            return "Error";

        const dragged = this.props.dynamicTreeView.state.draggedNode!;
        const draggedOptions = NodeUtils.registeredNodes[dragged.node.kind];
        if (parentOptions.validChild && parentOptions.validChild != dragged.node.kind ||
            draggedOptions.validParent && draggedOptions.validParent != parent.node.kind)
            return "Error";

        const draggedField = (dragged.node as LineBaseNode).field;
        if (draggedField && (parent.route == undefined || parent.route.subMembers()[draggedField] === undefined))
            return "Warning";

        return "Ok";
    }

    handleDragEnd = (e: React.DragEvent<any>) => {
        this.props.dynamicTreeView.setState({ draggedNode: undefined, draggedOver: undefined });
    }
    
    handleDrop = (e: React.DragEvent<any>) => {
        const dragged = this.props.dynamicTreeView.state.draggedNode!;
        const over = this.props.dynamicTreeView.state.draggedOver!;

        this.props.dynamicTreeView.setState({ draggedNode: undefined, draggedOver: undefined });

        if (over.error == "Error")
            return;

        const cn = dragged.parent!.node as ContainerNode;
        cn.children.remove(dragged.node);

        if (over.position == "Middle") {
            (over.dn.node as ContainerNode).children.push(dragged.node);
            this.props.node.context.setSelectedNode(over.dn.createChild(dragged.node));
        } else {
            const parent = over.dn.parent!.node as ContainerNode;
            const index = parent.children.indexOf(over.dn.node);
            parent.children.insertAt(index + (over.position == "Top" ? 0 : 1), dragged.node);
            this.props.node.context.setSelectedNode(over.dn.parent!.createChild(dragged.node));
        }
    }

    render(): React.ReactElement<any> {
        var dn = this.props.node;

        var container = dn.node as ContainerNode;

        const error = NodeUtils.validate(dn, undefined);

        const tree = this.props.dynamicTreeView;

        const sn = dn.context.getSelectedNode();

        const className = classes("tree-label", sn && dn.node == sn.node && "tree-selected", error && "tree-error"); 

        return (
            <li >
                <div draggable={dn.parent != null}
                    onDragStart={this.handleDragStart}
                    onDragEnter={this.handleDragOver}
                    onDragOver={this.handleDragOver}
                    onDragEnd={this.handleDragEnd}
                    onDrop={this.handleDrop}
                    style={this.getDragAndDropStyle()}>

                    {this.renderIcon()}
                    <span
                        className={className}
                        title={error || undefined}
                        onClick={e => dn.context.setSelectedNode(dn)}
                        onContextMenu={e => tree.handleNodeTextContextMenu(dn, e)}>
                        {NodeUtils.registeredNodes[dn.node.kind].renderTreeNode(dn)}
                    </span>
                </div>

                {container.children && container.children.length > 0 && (this.state.isOpened) &&
                    <ul>
                    {container.children.map((c, i) =>
                        <DynamicViewNode
                            dynamicTreeView={tree}
                            key={i} node={dn.createChild(c)} />)}
                    </ul>
                }
            </li>
        );
    }

    getDragAndDropStyle(): React.CSSProperties | undefined {
        const ts = this.props.dynamicTreeView.state;

        const dn = this.props.node;

        if (ts.draggedNode == undefined)
            return undefined;

        if (dn.node == ts.draggedNode.node)
            return { opacity: 0.5 };

        const over = ts.draggedOver;

        if (over && dn.node == over.dn.node) {

            const color =
                over.error == "Error" ? "rgb(193, 0, 0)" :
                    over.error == "Warning" ? "rgb(255, 153, 0)" :
                        over.error == "Ok" ? "rgb(10, 162, 0)" : "";
            

            if (over.position == "Top")
                return { borderTop: "2px dashed " + color };
            if (over.position == "Bottom")
                return { borderBottom: "2px solid " + color };
            else
                return { backgroundColor: color.replace("(", "a(").replace(")", ", 0.2)") };
        }

        return undefined;
    }
}
