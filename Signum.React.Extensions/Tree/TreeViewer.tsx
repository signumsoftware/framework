import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { API, TreeNode, TreeNodeState, fixState } from './TreeClient'
import { classes } from '@framework/Globals'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import ContextMenu from '@framework/SearchControl/ContextMenu'
import { ContextMenuPosition } from '@framework/SearchControl/ContextMenu'
import * as Operations from '@framework/Operations'
import { SearchMessage, JavascriptMessage, EntityControlMessage, toLite } from '@framework/Signum.Entities'
import { TreeViewerMessage, TreeEntity, TreeOperation, MoveTreeModel } from './Signum.Entities.Tree'
import * as TreeClient from './TreeClient'
import { FilterOptionParsed, QueryDescription, SubTokensOptions, FilterOption } from "@framework/FindOptions";
import FilterBuilder from "@framework/SearchControl/FilterBuilder";
import { ISimpleFilterBuilder } from "@framework/Search";
import { is } from "@framework/Signum.Entities";
import { ContextualItemsContext, renderContextualItems } from "@framework/SearchControl/ContextualItems";
import { Entity } from "@framework/Signum.Entities";
import { DisabledMixin } from "../Basics/Signum.Entities.Basics";
import { tryGetMixin } from "@framework/Signum.Entities";
import { Dropdown, DropdownButton } from 'react-bootstrap';
import { toFilterRequests } from '@framework/Finder';
import "./TreeViewer.css"
import { QueryTokenString } from '@framework/Reflection';
import { useForceUpdate, useStateWithPromise, useAPI } from '@framework/Hooks'

interface TreeViewerProps {
  typeName: string;
  showContextMenu?: boolean | "Basic";
  allowMove?: boolean;
  avoidChangeUrl?: boolean;
  onDoubleClick?: (selectedNode: TreeNode, e: React.MouseEvent<any>) => void;
  onSelectedNode?: (selectedNode: TreeNode | undefined) => void;
  onSearch?: () => void;
  filterOptions: FilterOption[];
  initialShowFilters?: boolean;
}

export type DraggedPosition = "Top" | "Bottom" | "Middle";

export interface DraggedOver {
  node: TreeNode;
  position: DraggedPosition;
}

interface TreeViewerState {
  treeNodes?: Array<TreeNode>;
  selectedNode?: TreeNode;
  filterOptions: FilterOptionParsed[];
  queryDescription?: QueryDescription;
  simpleFilterBuilder?: React.ReactElement<any>;
  showFilters?: boolean;

  isSelectOpen: boolean;

  draggedNode?: TreeNode;
  draggedKind?: "Move" | "Copy";
  draggedOver?: DraggedOver;

  currentMenuItems?: React.ReactElement<any>[];
  contextualMenu?: {
    position: ContextMenuPosition;
  };
}



export function TreeViewer(p : TreeViewerProps){
  const forceUpdate = useForceUpdate();

  const [treeNodes, setTreeNodes] = React.useState<Array<TreeNode> | undefined>();
  const [selectedNode, setSelectedNode] = React.useState<TreeNode | undefined>();
  const [filterOptions, setFilterOptions] = React.useState<FilterOptionParsed[] | undefined>([]);
  const [showFilters, setShowFilters] = React.useState<boolean>(p.initialShowFilters || false);
  const [isSelectOpen, setSelectOpen] = React.useState<boolean>(false);
  const [simpleFilterBuilder, setSimpleFilterBuilder] = React.useState<React.ReactElement<any> | undefined>(undefined);
  const queryDescription = useAPI(() => Finder.getQueryDescription(p.typeName), [p.typeName]);

  React.useEffect(() => {
    if (queryDescription == null)
      return;

    if (filterOptions != null && TreeClient.treePath(p.typeName, p.filterOptions) ==
      TreeClient.treePath(p.typeName, Finder.toFilterOptions(filterOptions)))
      return;

    Finder.parseFilterOptions(p.filterOptions, false, queryDescription!).then(fop => {

      setFilterOptions(fop);

      const qs = Finder.getSettings(p.typeName);
      const sfb = qs && qs.simpleFilterBuilder && qs.simpleFilterBuilder({ queryDescription: queryDescription, initialFilterOptions: fop, search: () => search(true) });
      setSimpleFilterBuilder(sfb);
      if (sfb)
        setShowFilters(false);

      search(true);
    });
  }, [queryDescription, TreeClient.treePath(p.typeName, p.filterOptions)]);


  function selectNode(node: TreeNode | undefined) {
    setSelectedNode(node);
    if (p.onSelectedNode)
      p.onSelectedNode(node);
  }

  function handleFullScreenClick(ev: React.MouseEvent<any>) {
    ev.preventDefault();

    const path = TreeClient.treePath(p.typeName, Finder.toFilterOptions(filterOptions!));

    if (ev.ctrlKey || ev.button == 1)
      window.open(path);
    else
      Navigator.history.push(path);
  }

  function handleNodeIconClick(n: TreeNode) {
    if (n.nodeState == "Collapsed" || n.nodeState == "Filtered") {
      n.nodeState = "Expanded";
      search(false);
    }
    else if (n.nodeState == "Expanded") {
      n.nodeState = "Collapsed";
      forceUpdate();
    }
  }

  function handleNodeTextClick(n: TreeNode) {
    selectNode(n);
  }

  function handleNodeTextDoubleClick(n: TreeNode, e: React.MouseEvent<any>) {
    if (p.onDoubleClick)
      p.onDoubleClick(n, e);
    else
      handleNavigate();
  }

  function handleNavigate() {
    const node = selectedNode!;
    Navigator.navigate(node.lite)
      .then(() => search(false))
      .done();
  }

  treeContainer!: HTMLElement;


  function handleNodeTextContextMenu(n: TreeNode, e: React.MouseEvent<any>) {
    e.preventDefault();
    e.stopPropagation();

    setState({
      selectedNode: n,
      contextualMenu: {
        position: ContextMenu.getPosition(e, treeContainer)
      }
    }, () => loadMenuItems());
  }

  function loadMenuItems() {
    if (p.showContextMenu == "Basic")
      setState({ currentMenuItems: [] });
    else {
      const options: ContextualItemsContext<Entity> = {
        lites: [selectedNode!.lite],
        queryDescription: queryDescription!,
        markRows: () => { search(false); },
        container: this,
      };

      renderContextualItems(options)
        .then(menuItems => setState({ currentMenuItems: menuItems }))
        .done();
    }
  }

  function handleContextOnHide() {
    setState({ contextualMenu: undefined, currentMenuItems: undefined });
  }

  function renderContextualMenu() {
    const cm = contextualMenu!;
    if (!selectedNode)
      return null;

    return (
      <ContextMenu position={cm.position} onHide={handleContextOnHide}>
        {renderMenuItems().map((e, i) => React.cloneElement(e, { key: i }))}
      </ContextMenu>
    );
  }

  renderMenuItems(): React.ReactElement<any>[] {

    let type = p.typeName;

    var menuItems = [
      Navigator.isNavigable(type, undefined, true) && <Dropdown.Item onClick={handleNavigate} className="btn-danger"><FontAwesomeIcon icon="arrow-right" />&nbsp;{EntityControlMessage.View.niceToString()}</Dropdown.Item >,
      Operations.isOperationAllowed(TreeOperation.CreateChild, type) && <Dropdown.Item onClick={handleAddChildren}><FontAwesomeIcon icon="caret-square-right" />&nbsp;{TreeViewerMessage.AddChild.niceToString()}</Dropdown.Item>,
      Operations.isOperationAllowed(TreeOperation.CreateNextSibling, type) && <Dropdown.Item onClick={handleAddSibling}><FontAwesomeIcon icon="caret-square-down" />&nbsp;{TreeViewerMessage.AddSibling.niceToString()}</Dropdown.Item>,
    ].filter(a => a != false) as React.ReactElement<any>[];

    if (currentMenuItems == undefined) {
      menuItems.push(<Dropdown.Header>{JavascriptMessage.loading.niceToString()}</Dropdown.Header>);
    } else {
      if (menuItems.length && currentMenuItems.length)
        menuItems.push(<Dropdown.Divider />);

      menuItems.splice(menuItems.length, 0, ...currentMenuItems);
    }

    return menuItems;
  }

  function handleSearchSubmit(e: React.FormEvent<any>) {
    e.preventDefault();
    e.stopPropagation();

    search(true);
  }


  function search(clearExpanded: boolean) {
    getFilterOptionsWithSFB().then(filters => {
      let expandedNodes = clearExpanded || !treeNodes ? [] :
        treeNodes!.flatMap(allNodes).filter(a => a.nodeState == "Expanded").map(a => a.lite);


      const userFilters = toFilterRequests(filters.filter(fo => fo.frozen == false));
      const frozenFilters = toFilterRequests(filters.filter(fo => fo.frozen == true));

      if (userFilters.length == 0)
        userFilters.push({ token: QueryTokenString.entity<TreeEntity>().append(e => e.level).toString(), operation: "EqualTo", value: 1 });

      return API.findNodes(p.typeName, { userFilters, frozenFilters, expandedNodes });
    })
      .then(nodes => {
        const selectedLite = selectedNode && selectedNode.lite;
        var newSeleted = selectedLite && nodes.filter(a => is(a.lite, selectedLite)).singleOrNull();
        setState({ treeNodes: nodes, selectedNode: newSeleted || undefined });

        if (p.onSearch)
          p.onSearch();
      })
      .done();
  }

  function renderSearch() {
    const s = state;

    const sfb = simpleFilterBuilder &&
      React.cloneElement(simpleFilterBuilder, { ref: (e: ISimpleFilterBuilder) => { simpleFilterBuilderInstance = e } });

    return (
      <form onSubmit={handleSearchSubmit}>
        {s.queryDescription && (s.showFilters ?
          <FilterBuilder
            queryDescription={s.queryDescription}
            filterOptions={s.filterOptions}
            subTokensOptions={SubTokensOptions.CanAnyAll} /> :
          sfb && <div className="simple-filter-builder">{sfb}</div>)}
      </form>
    );
  }

  function handleAddRoot() {
    Operations.API.construct(p.typeName, TreeOperation.CreateRoot)
      .then(ep => Navigator.view(ep, { requiresSaveOperation: true }))
      .then(te => {
        if (!te)
          return;
        treeNodes!.push(toTreeNode(te));
        forceUpdate();
      })
      .done();
  }

  function handleAddChildren() {
    var parent = selectedNode!;
    Operations.API.constructFromLite(parent.lite, TreeOperation.CreateChild)
      .then(ep => Navigator.view(ep, { requiresSaveOperation: true }))
      .then(te => {
        if (!te)
          return;
        var newNode = toTreeNode(te);
        parent.loadedChildren.push(newNode);
        parent.childrenCount++;
        fixState(parent);
        selectNode(newNode);
      })
      .done();
  }

  function handleAddSibling() {
    var sibling = selectedNode!;

    Operations.API.constructFromLite(sibling.lite, TreeOperation.CreateNextSibling)
      .then(ep => Navigator.view(ep, { requiresSaveOperation: true }))
      .then(te => {
        if (!te)
          return;
        const newNode = toTreeNode(te);
        const parent = findParent(sibling);
        const array = parent ? parent.loadedChildren : treeNodes!;
        array.insertAt(array.indexOf(sibling) + 1, newNode);
        selectNode(newNode);
      })
      .done();
  }

  function findParent(childNode: TreeNode) {
    return treeNodes!.flatMap(allNodes).filter(n => n.loadedChildren.contains(childNode)).singleOrNull();
  }

  simpleFilterBuilderInstance?: ISimpleFilterBuilder;
  getFilterOptionsWithSFB(): Promise<FilterOptionParsed[]> {

    const fos = filterOptions;
    const qd = queryDescription!;

    if (simpleFilterBuilderInstance == undefined)
      return Promise.resolve(fos);

    if (!simpleFilterBuilderInstance.getFilters)
      throw new Error("The simple filter builder should have a method with signature: 'getFilters(): FilterOption[]'");

    var filters = simpleFilterBuilderInstance.getFilters();

    return Finder.parseFilterOptions(filters, false, qd).then(newFos => {
      setState({ filterOptions: newFos });

      return newFos;
    });
  }

  function renderToolbar() {
    const s = state;
    const selected = s.selectedNode;
    const menuItems = renderMenuItems();

    return (
      <div className="btn-toolbar">
        <a className={"sf-query-button sf-filters-header btn btn-light" + (s.showFilters ? " active" : "")}
          onClick={handleToggleFilters}
          title={s.showFilters ? JavascriptMessage.hideFilters.niceToString() : JavascriptMessage.showFilters.niceToString()}><FontAwesomeIcon icon="filter" /></a>
        <button className="btn btn-primary" onClick={handleSearchSubmit}>{JavascriptMessage.search.niceToString()}</button>
        {Operations.isOperationAllowed(TreeOperation.CreateRoot, p.typeName) && <button className="btn btn-light" onClick={handleAddRoot} disabled={s.treeNodes == null} > <FontAwesomeIcon icon="star" />&nbsp;{TreeViewerMessage.AddRoot.niceToString()}</button>}
        <Dropdown
          onToggle={handleSelectedToggle}
          show={s.isSelectOpen}>
          <Dropdown.Toggle id="selectedButton"
            className="sf-query-button sf-tm-selected" disabled={selected == undefined}
            variant="light">
            {`${JavascriptMessage.Selected.niceToString()} (${selected ? selected.lite.toStr : TreeViewerMessage.None.niceToString()})`}
          </Dropdown.Toggle>
          <Dropdown.Menu>
            {menuItems == undefined ? <Dropdown.Item className="sf-tm-selected-loading">{JavascriptMessage.loading.niceToString()}</Dropdown.Item> :
              menuItems.length == 0 ? <Dropdown.Item className="sf-search-ctxitem-no-results">{JavascriptMessage.noActionsFound.niceToString()}</Dropdown.Item> :
                menuItems.map((e, i) => React.cloneElement(e, { key: i }))}
          </Dropdown.Menu>
        </Dropdown>
        <button className="btn btn-light" onClick={handleExplore} ><FontAwesomeIcon icon="search" /> &nbsp; {SearchMessage.Explore.niceToString()}</button>
      </div>
    );
  }

  function handleSelectedToggle() {
    if (!isSelectOpen && currentMenuItems == undefined)
      loadMenuItems();

    setState({ isSelectOpen: !isSelectOpen });
  }

  function handleExplore(e: React.MouseEvent<any>) {
    var path = Finder.findOptionsPath({
      queryName: p.typeName,
      filterOptions: Finder.toFilterOptions(filterOptions),
    });

    if (p.avoidChangeUrl)
      window.open(Navigator.toAbsoluteUrl(path));
    else
      Navigator.pushOrOpenInTab(path, e);
  }

  function handleToggleFilters() {
    getFilterOptionsWithSFB().then(() => {
      simpleFilterBuilderInstance = undefined;
      setState({ simpleFilterBuilder: undefined, showFilters: !showFilters });
    }).done();
  }


  function handleDragStart(node: TreeNode, e: React.DragEvent<any>) {
    e.dataTransfer.setData('text', "start"); //cannot be empty string

    var isCopy = e.ctrlKey || e.shiftKey || e.altKey;
    e.dataTransfer.effectAllowed = isCopy ? "copy" : "move";
    setState({ draggedNode: node, draggedKind: isCopy ? "Copy" : "Move" });
  }


  function handleDragOver(node: TreeNode, e: React.DragEvent<any>) {
    e.preventDefault();
    const de = e.nativeEvent as DragEvent;
    const span = e.currentTarget as HTMLElement;
    const newPosition = getOffset(de.pageY, span.getBoundingClientRect(), 7);

    const s = state;

    if (s.draggedOver == null ||
      s.draggedOver.node != node ||
      s.draggedOver.position != newPosition) {

      setState({
        draggedOver: {
          node: node,
          position: newPosition,
        }
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

  function handleDragEnd(node: TreeNode, e: React.DragEvent<any>) {
    setState({ draggedNode: undefined, draggedOver: undefined, draggedKind: undefined });
  }

  function handleDrop(node: TreeNode, e: React.DragEvent<any>) {
    const dragged = draggedNode!;
    const over = draggedOver!;

    if (dragged == over.node)
      return;

    var nodeParent = findParent(over.node);
    const ts = TreeClient.settings[p.typeName];
    if (ts && ts.dragTargetIsValid)
      ts.dragTargetIsValid(dragged, over.position == "Middle" ? over.node : nodeParent)
        .then(valid => {
          if (!valid)
            return;

          moveOrCopyOperation(nodeParent, dragged, over);

        }).done()
    else
      moveOrCopyOperation(nodeParent, dragged, over);
  }

  function moveOrCopyOperation(nodeParent: TreeNode | null, dragged: TreeNode, over: DraggedOver) {
    var partial: Partial<MoveTreeModel> =
      over.position == "Middle" ? { newParent: over.node.lite, insertPlace: "LastNode" } :
        over.position == "Top" ? { newParent: nodeParent ? nodeParent.lite : undefined, insertPlace: "Before", sibling: over.node.lite } :
          over.position == "Bottom" ? { newParent: nodeParent ? nodeParent.lite : undefined, insertPlace: "After", sibling: over.node.lite } :
            {};

    var toExpand = over.position == "Middle" ? over.node : nodeParent;

    if (draggedKind == "Move") {
      const treeModel = MoveTreeModel.New(partial);
      Operations.API.executeLite(dragged.lite, TreeOperation.Move, treeModel).then(() =>

        setState({ draggedNode: undefined, draggedOver: undefined, draggedKind: undefined, selectedNode: dragged }, () => {
          if (toExpand)
            toExpand.nodeState = "Expanded";

          search(false);
        })
      ).done();

    } else {
      const s = TreeClient.settings[p.typeName];
      var promise = s && s.createCopyModel ? s.createCopyModel(dragged.lite, partial) : Promise.resolve(MoveTreeModel.New(partial));
      promise.then(treeModel => treeModel &&
        Operations.API.constructFromLite(dragged.lite, TreeOperation.Copy, treeModel).then(() =>
          setState({ draggedNode: undefined, draggedOver: undefined, draggedKind: undefined, selectedNode: dragged }, () => {
            if (toExpand)
              toExpand.nodeState = "Expanded";

            search(false);
          })
        ))
        .done();
    };
  }
  return (
    <div>
      {renderSearch()}
      <br />
      {renderToolbar()}
      <br />
      <div className="tree-container" ref={(t) => treeContainer = t!} >
        <ul>
          {!treeNodes ? JavascriptMessage.loading.niceToString() :
            treeNodes.map((node, i) =>
              <TreeNodeControl key={i} treeViewer={this} treeNode={node} dropDisabled={node == draggedNode} />)}
        </ul>
      </div>
      {contextualMenu && renderContextualMenu()}
    </div>
  );
}


function allNodes(node: TreeNode): TreeNode[] {
  return [node].concat(node.loadedChildren ? node.loadedChildren.flatMap(allNodes) : []);
}

function toTreeNode(treeEntity: TreeEntity): TreeNode {

  var dm = tryGetMixin(treeEntity, DisabledMixin);
  return {
    lite: toLite(treeEntity),
    name: treeEntity.name!,
    childrenCount: 0,
    disabled: dm != null && Boolean(dm.isDisabled),
    level: 0,
    loadedChildren: [],
    nodeState: "Leaf"
  };
}

interface TreeNodeControlProps {
  treeViewer: TreeViewer;
  treeNode: TreeNode;
  dropDisabled: boolean;
}

function TreeNodeControl(p : TreeNodeControlProps){
  function renderIcon(nodeState: TreeNodeState) {
    var node = p.treeNode;
    const tv = p.treeViewer;
    switch (nodeState) {
      case "Collapsed": return (
        <span onClick={() => tv.handleNodeIconClick(node)} className="tree-icon" >
          <FontAwesomeIcon icon={["far", "plus-square"]} />
        </span>);
      case "Expanded": return (
        <span onClick={() => tv.handleNodeIconClick(node)} className="tree-icon" >
          <FontAwesomeIcon icon={["far", "minus-square"]} />
        </span>);
      case "Filtered": return (
        <span onClick={() => tv.handleNodeIconClick(node)} className="tree-icon fa-layers fa-fw" >
          <FontAwesomeIcon icon="square" />
          <FontAwesomeIcon icon="filter" inverse transform="shrink-2" />
        </span>);
      default: return <span className="place-holder" />;
    }
  }


  function getDragAndDropStyle(node: TreeNode): React.CSSProperties | undefined {
    const s = p.treeViewer.state;

    if (s.draggedNode == undefined)
      return undefined;

    if (node == s.draggedNode)
      return { opacity: 0.5 };

    const over = s.draggedOver;

    if (over && node == over.node) {

      const color = p.dropDisabled ? "rgb(193, 0, 0)" :
        "rgb(10, 162, 0)";

      if (over.position == "Top")
        return { borderTop: "2px dashed " + color };
      if (over.position == "Bottom")
        return { borderBottom: "2px solid " + color };
      else
        return { backgroundColor: color.replace("(", "a(").replace(")", ", 0.2)") };
    }

    return undefined;
  }

  var node = p.treeNode;
  const tv = p.treeViewer;
  return (
    <li>
      <div draggable={tv.props.allowMove}
        onDragStart={de => tv.handleDragStart(node, de)}
        onDragEnter={de => tv.handleDragOver(node, de)}
        onDragOver={de => tv.handleDragOver(node, de)}
        onDragEnd={de => tv.handleDragEnd(node, de)}
        onDrop={p.dropDisabled ? undefined : de => tv.handleDrop(node, de)}
        style={getDragAndDropStyle(node)}>
        {renderIcon(node.nodeState)}

        <span className={classes("tree-label", node == tv.state.selectedNode && "tree-selected", node.disabled && "tree-disabled")}
          onDoubleClick={e => tv.handleNodeTextDoubleClick(node, e)}
          onClick={() => tv.handleNodeTextClick(node)}
          onContextMenu={tv.props.showContextMenu != false ? e => tv.handleNodeTextContextMenu(node, e) : undefined}>
          {node.name}
        </span>
      </div>

      {node.loadedChildren.length > 0 && (node.nodeState == "Expanded" || node.nodeState == "Filtered") &&
        <ul>
          {node.loadedChildren.map((n, i) =>
            <TreeNodeControl key={i} treeViewer={tv} treeNode={n} dropDisabled={p.dropDisabled || n == tv.state.draggedNode} />)}
        </ul>
      }
    </li>
  );
}
