import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { classes } from '@framework/Globals'
import ContextMenu from '@framework/SearchControl/ContextMenu'
import { ContextMenuPosition } from '@framework/SearchControl/ContextMenu'
import * as NodeUtils from './NodeUtils'
import NodeSelectorModal from './NodeSelectorModal'
import { DesignerNode } from './NodeUtils'
import { BaseNode, ContainerNode, LineBaseNode, NodeConstructor } from './Nodes'
import { DynamicViewMessage } from '../Signum.Entities.Dynamic'
import { Dropdown, DropdownButton } from 'react-bootstrap';
import "./DynamicViewTree.css"

export interface DynamicViewTreeProps {
  rootNode: DesignerNode<BaseNode>;
}

export type DraggedPosition = "Top" | "Bottom" | "Middle";
export type DraggedError = "Error" | "Warning" | "Ok";

//export interface DnamicViewTreeState {
//  draggedNode?: DesignerNode<BaseNode>;
//  draggedOver?: {
//    dn: DesignerNode<BaseNode>;
//    position: DraggedPosition;
//    error: DraggedError
//  }
//  contextualMenu?: {
//    position: ContextMenuPosition;
//  };
//}

interface DraggedOverInfo {
  dn: DesignerNode<BaseNode>;
  position: DraggedPosition;
  error: DraggedError
}

export function DynamicViewTree(p: DynamicViewTreeProps) {

  const [draggedNode, setDraggedNode] = React.useState<DesignerNode<BaseNode> | undefined>(undefined);

  const [draggedOver, setDraggedOver] = React.useState<DraggedOverInfo | undefined>(undefined);


  const [contextualMenu, setContextualMenu] = React.useState<{
    position: ContextMenuPosition;
  } | undefined>(undefined);


  function handleNodeTextContextMenu(n: DesignerNode<BaseNode>, e: React.MouseEvent<any>) {
    e.preventDefault();
    e.stopPropagation();

    p.rootNode.context.setSelectedNode(n);
    setContextualMenu({
      position: ContextMenu.getPositionEvent(e)
    });
  }

  function renderContextualMenu() {
    const cm = contextualMenu!;
    const dn = p.rootNode.context.getSelectedNode();
    if (!dn)
      return null;

    const no = NodeUtils.registeredNodes[dn.node.kind];

    const cn = dn.node as ContainerNode;

    const isRoot = (dn.node == p.rootNode.node);

    return (
      <ContextMenu position={cm.position} onHide={() => setContextualMenu(undefined)}>
        {no.isContainer && <Dropdown.Item onClick={handleAddChildren}><FontAwesomeIcon icon="arrow-right" />&nbsp; {DynamicViewMessage.AddChild.niceToString()}</Dropdown.Item>}
        {!isRoot && <Dropdown.Item onClick={handleAddSibling}><FontAwesomeIcon icon="arrow-down" />&nbsp; {DynamicViewMessage.AddSibling.niceToString()}</Dropdown.Item>}

        {no.isContainer && <Dropdown.Divider />}

        {no.isContainer && cn.children.length == 0 && dn.route && <Dropdown.Item onClick={handleGenerateChildren}><FontAwesomeIcon icon="bolt" />&nbsp; {DynamicViewMessage.GenerateChildren.niceToString()}</Dropdown.Item>}
        {no.isContainer && cn.children.length > 0 && <Dropdown.Item onClick={handleClearChildren} color="danger"><FontAwesomeIcon icon="trash" />&nbsp; {DynamicViewMessage.ClearChildren.niceToString()}</Dropdown.Item>}

        {!isRoot && <Dropdown.Divider />}
        {!isRoot && <Dropdown.Item onClick={handleRemove} color="danger"><FontAwesomeIcon icon="xmark" />&nbsp; {DynamicViewMessage.Remove.niceToString()}</Dropdown.Item>}
      </ContextMenu>
    );
  }

  function handleAddChildren() {
    const parent = p.rootNode.context.getSelectedNode()! as DesignerNode<ContainerNode>;

    newNode(parent).then(n => {
      if (!n)
        return;

      parent.node.children.push(n);
      p.rootNode.context.setSelectedNode(parent.createChild(n));
      parent.context.refreshView();
    });
  }

  function handleAddSibling() {
    var sibling = p.rootNode.context.getSelectedNode()!;
    var parent = sibling.parent! as DesignerNode<ContainerNode>;
    newNode(parent).then(n => {
      if (!n)
        return;

      parent.node.children.insertAt(parent.node.children.indexOf(sibling.node) + 1, n);
      p.rootNode.context.setSelectedNode(parent.createChild(n));
      parent.context.refreshView();
    });
  }

  function handleRemove() {
    var selected = p.rootNode.context.getSelectedNode()!;
    var parent = selected.parent as DesignerNode<ContainerNode>;
    var nodeIndex = parent.node.children.indexOf(selected.node);

    parent.node.children.remove(selected.node);
    p.rootNode.context.setSelectedNode(parent);
    parent.context.refreshView();
  }

  function handleClearChildren() {
    var selected = p.rootNode.context.getSelectedNode()! as DesignerNode<ContainerNode>;
    selected.node.children.clear();
    selected.context.refreshView();
  }

  function handleGenerateChildren() {
    var selected = p.rootNode.context.getSelectedNode()! as DesignerNode<ContainerNode>;
    if (selected.node.kind == "EntityTable")
      selected.node.children.push(...NodeConstructor.createEntityTableSubChildren(selected.fixRoute()!));
    else
      selected.node.children.push(...NodeConstructor.createSubChildren(selected.fixRoute()!));
    selected.context.refreshView();
  }

  function newNode(parent: DesignerNode<ContainerNode>): Promise<BaseNode | undefined> {
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
  return (
    <div>
      <div className="dynamic-view-tree">
        <ul>
          <DynamicViewNode
            node={p.rootNode}
            dynamicTreeView={{
              draggedOver,
              draggedNode,
              setDraggedNode,
              setDraggedOver,
              handleNodeTextContextMenu
            }} />
        </ul>
      </div>
      {contextualMenu && renderContextualMenu()}
    </div>
  );
}

interface DynamicViewTreeHandle {
  draggedOver: DraggedOverInfo | undefined;
  draggedNode: DesignerNode<BaseNode> | undefined;
  setDraggedNode(node: DesignerNode<BaseNode> | undefined) : void;
  setDraggedOver(node: DraggedOverInfo | undefined) : void;
  handleNodeTextContextMenu(n: DesignerNode<BaseNode>, e: React.MouseEvent<any>): void;
}

function allNodes(node: BaseNode): BaseNode[] {
  return [node].concat((node as ContainerNode).children ? (node as ContainerNode).children.flatMap(allNodes) : []);
}


export interface DynamicViewNodeProps {
  node: DesignerNode<BaseNode>;
  dynamicTreeView: DynamicViewTreeHandle;
}

export function DynamicViewNode(p: DynamicViewNodeProps) {


  const [isOpened, setIsOpened] = React.useState<boolean>(true);

  function handleIconClick() {
    setIsOpened(!isOpened);
  }

  function renderIcon() {
    var c = p.node.node as ContainerNode;

    if (!c.children || c.children.length == 0)
      return <span className="place-holder" />;

    if (isOpened) {
      return (
        <span onClick={handleIconClick} className="tree-icon">
          <FontAwesomeIcon icon={["far", "square-minus"]} />
        </span>);
    }
    else {
      return (
        <span onClick={handleIconClick} className="tree-icon">
          <FontAwesomeIcon icon={["far", "square-plus"]} />
        </span>);
    }
  }

  function handleDragStart(e: React.DragEvent<any>) {
    e.dataTransfer.setData('text', "start"); //cannot be empty string
    e.dataTransfer.effectAllowed = "move";
    p.dynamicTreeView.setDraggedNode(p.node);
  }

  function handleDragOver(e: React.DragEvent<any>) {
    e.preventDefault();
    const de = e.nativeEvent as DragEvent;
    const dn = p.node;
    const span = e.currentTarget as HTMLElement;
    const newPosition = getOffset(de.pageY, span.getBoundingClientRect(), 7);
    const newError = getError(newPosition);
    //de.dataTransfer.dropEffect = newError == "Error" ? "none" : "move";

    const tree = p.dynamicTreeView;

    if (tree.draggedOver == null ||
      tree.draggedOver.dn.node != dn.node ||
      tree.draggedOver.position != newPosition ||
      tree.draggedOver.error != newError) {

      p.dynamicTreeView.setDraggedOver({
        dn: dn,
        position: newPosition,
        error: newError
      });
    }
  }

  function getOffset(pageY: number, rect: ClientRect, margin: number): DraggedPosition {

    const height = Math.round(rect.height / 5) * 5;
    const offsetY = pageY - rect.top;

    if (offsetY < margin)
      return "Top";

    if (offsetY > (height - margin))
      return "Bottom";

    return "Middle";
  }

  function getError(position: DraggedPosition): DraggedError {
    const parent = position == "Middle" ? p.node : p.node.parent;

    if (!parent || !parent.node)
      return "Error";

    const parentOptions = NodeUtils.registeredNodes[parent.node.kind];
    if (!parentOptions.isContainer)
      return "Error";

    const dragged = p.dynamicTreeView.draggedNode!;
    const draggedOptions = NodeUtils.registeredNodes[dragged.node.kind];
    if (parentOptions.validChild && parentOptions.validChild != dragged.node.kind ||
      draggedOptions.validParent && draggedOptions.validParent != parent.node.kind)
      return "Error";

    const draggedField = (dragged.node as LineBaseNode).field;
    if (draggedField && (parent.route == undefined || parent.route.subMembers()[draggedField] === undefined))
      return "Warning";

    return "Ok";
  }

  function handleDragEnd(e: React.DragEvent<any>) {
    p.dynamicTreeView.setDraggedOver(undefined);
    p.dynamicTreeView.setDraggedNode(undefined);
  }

  function handleDrop(e: React.DragEvent<any>) {
    const dragged = p.dynamicTreeView.draggedNode!;
    const over = p.dynamicTreeView.draggedOver!;

    p.dynamicTreeView.setDraggedOver(undefined);
    p.dynamicTreeView.setDraggedNode(undefined);

    if (over.error == "Error")
      return;

    const cn = dragged.parent!.node as ContainerNode;
    cn.children.remove(dragged.node);

    if (over.position == "Middle") {
      (over.dn.node as ContainerNode).children.push(dragged.node);
      p.node.context.setSelectedNode(over.dn.createChild(dragged.node));
    } else {
      const parent = over.dn.parent!.node as ContainerNode;
      const index = parent.children.indexOf(over.dn.node);
      parent.children.insertAt(index + (over.position == "Top" ? 0 : 1), dragged.node);
      p.node.context.setSelectedNode(over.dn.parent!.createChild(dragged.node));
    }
  }


  function getDragAndDropStyle(): React.CSSProperties | undefined {
    const dtv = p.dynamicTreeView;

    const dn = p.node;

    if (dtv.draggedNode == undefined)
      return undefined;

    if (dn.node == dtv.draggedNode.node)
      return { opacity: 0.5 };

    const over = dtv.draggedOver;

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
  var dn = p.node;

  var container = dn.node as ContainerNode;

  const error = NodeUtils.validate(dn, undefined);

  const tree = p.dynamicTreeView;

  const sn = dn.context.getSelectedNode();

  const className = classes("tree-label", sn && dn.node == sn.node && "tree-selected", error && "tree-error");

  return (
    <li >
      <div draggable={dn.parent != null}
        onDragStart={handleDragStart}
        onDragEnter={handleDragOver}
        onDragOver={handleDragOver}
        onDragEnd={handleDragEnd}
        onDrop={handleDrop}
        style={getDragAndDropStyle()}>

        {renderIcon()}
        <span
          className={className}
          title={error ?? undefined}
          onClick={e => dn.context.setSelectedNode(dn)}
          onContextMenu={e => tree.handleNodeTextContextMenu(dn, e)}>
          {NodeUtils.registeredNodes[dn.node.kind].renderTreeNode(dn)}
        </span>
      </div>

      {container.children && container.children.length > 0 && (isOpened) &&
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
