import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { API, TreeNode, TreeNodeState, fixState } from './TreeClient'
import { classes } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import ContextMenu from '@framework/SearchControl/ContextMenu'
import { ContextMenuPosition } from '@framework/SearchControl/ContextMenu'
import * as Operations from '@framework/Operations'
import { SearchMessage, JavascriptMessage, EntityControlMessage, toLite, liteKey, getToString } from '@framework/Signum.Entities'
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
import * as Hooks from '../../Signum.React/Scripts/Hooks'
import SearchPage from '../../Signum.React/Scripts/SearchControl/SearchPage'

interface TreeViewerProps {
  typeName: string;
  showContextMenu?: boolean | "Basic";
  allowMove?: boolean;
  avoidChangeUrl?: boolean;
  onDoubleClick?: (selectedNode: TreeNode, e: React.MouseEvent<any>) => void;
  onSelectedNode?: (selectedNode: TreeNode | undefined) => void;
  onSearch?: () => void;
  filterOptions?: (FilterOption | null | undefined)[];
  initialShowFilters?: boolean;
  showToolbar?: boolean;
  deps?: React.DependencyList;
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



export class TreeViewer extends React.Component<TreeViewerProps, TreeViewerState>{

  constructor(props: TreeViewerProps) {
    super(props);
    this.state = {
      filterOptions: [],
      showFilters: props.initialShowFilters,
      isSelectOpen: false
    };
  }

  selectNode(node: TreeNode | undefined) {

    this.setState({ selectedNode: node });
    if (this.props.onSelectedNode)
      this.props.onSelectedNode(node);
  }

  componentWillMount() {
    this.initilize(this.props.typeName, this.props.filterOptions ?? []);
  }

  componentWillReceiveProps(newProps: TreeViewerProps) {
    var path = TreeClient.treePath(newProps.typeName, newProps.filterOptions);
    if (path == TreeClient.treePath(this.props.typeName, this.props.filterOptions)) {
      this.searchIfDeps(newProps);
      return;
    }

    if (this.state.filterOptions && this.state.queryDescription) {
      if (path == TreeClient.treePath(this.props.typeName, Finder.toFilterOptions(this.state.filterOptions))) {
        this.searchIfDeps(newProps);
        return;
      }
    }

    this.state = {
      filterOptions: [],
      showFilters: newProps.initialShowFilters,
      isSelectOpen: false,
    };
    this.forceUpdate();

    this.initilize(newProps.typeName, newProps.filterOptions ?? []);
  }

  searchIfDeps(newProps: TreeViewerProps) {
    if (Hooks.areEqual(this.props.deps ?? [], newProps.deps ?? [])) {
      this.search(false);
    }
  }

  initilize(typeName: string, filterOptions: (FilterOption | null | undefined)[]) {

    Finder.getQueryDescription(typeName)
      .then(qd => {
        Finder.parseFilterOptions(filterOptions, false, qd).then(fop => {
          this.setState({ filterOptions: fop }, () => {
            const qs = Finder.getSettings(typeName);
            const sfb = qs?.simpleFilterBuilder && qs.simpleFilterBuilder({ queryDescription: qd, initialFilterOptions: this.state.filterOptions, search: () => this.search(true)});
            this.setState({ queryDescription: qd, simpleFilterBuilder: sfb });
            if (sfb)
              this.setState({ showFilters: false });

            this.search(true);
          });
        });
      });
  }

  handleFullScreenClick = (ev: React.MouseEvent<any>) => {

    ev.preventDefault();

    const path = this.getCurrentUrl();

    if (ev.ctrlKey || ev.button == 1)
      window.open(path);
    else
      AppContext.history.push(path);
  };

  getCurrentUrl() {
    return TreeClient.treePath(this.props.typeName, Finder.toFilterOptions(this.state.filterOptions));
  }



  handleNodeIconClick = (n: TreeNode) => {
    if (n.nodeState == "Collapsed" || n.nodeState == "Filtered") {
      n.nodeState = "Expanded";
      this.search(false);
    }
    else if (n.nodeState == "Expanded") {
      n.nodeState = "Collapsed";
      this.forceUpdate();
    }
  }

  handleNodeTextClick = (n: TreeNode) => {
    this.selectNode(n);
  }

  handleNodeTextDoubleClick = (n: TreeNode, e: React.MouseEvent<any>) => {
    if (this.props.onDoubleClick)
      this.props.onDoubleClick(n, e);
    else
      this.handleView();
  }

  handleView = () => {
    const node = this.state.selectedNode!;
    Navigator.view(node.lite)
      .then(() => this.search(false));
  }


  render() {
    return (
      <div>
        {this.renderSearch()}
        {this.props.showToolbar && <>
          <br />
          {this.renderToolbar()}
          <br />
        </>}
        
        <div className="tree-container">
          <ul>
            {!this.state.treeNodes ? JavascriptMessage.loading.niceToString() :
              this.state.treeNodes.map((node, i) =>
                <TreeNodeControl key={i} treeViewer={this} treeNode={node} dropDisabled={node == this.state.draggedNode} />)}
          </ul>
        </div>
        {this.state.contextualMenu && this.renderContextualMenu()}
      </div>
    );
  }

  handleNodeTextContextMenu = (n: TreeNode, e: React.MouseEvent<any>) => {
    e.preventDefault();
    e.stopPropagation();

    this.setState({
      selectedNode: n,
      contextualMenu: {
        position: ContextMenu.getPositionEvent(e)
      }
    }, () => this.loadMenuItems());
  }

  loadMenuItems() {
    if (this.props.showContextMenu == "Basic")
      this.setState({ currentMenuItems: [] });
    else {
      const options: ContextualItemsContext<Entity> = {
        lites: [this.state.selectedNode!.lite],
        queryDescription: this.state.queryDescription!,
        markRows: () => { this.search(false); },
        container: this,
      };

      renderContextualItems(options)
        .then(menuItems => this.setState({ currentMenuItems: menuItems }));
    }
  }

  handleContextOnHide = () => {
    this.setState({ contextualMenu: undefined, currentMenuItems: undefined });
  }

  renderContextualMenu() {
    const cm = this.state.contextualMenu!;
    if (!this.state.selectedNode)
      return null;

    return (
      <ContextMenu position={cm.position} onHide={this.handleContextOnHide}>
        {this.renderMenuItems().map((e, i) => React.cloneElement(e, { key: i }))}
      </ContextMenu>
    );
  }

  renderMenuItems(): React.ReactElement<any>[] {

    let type = this.props.typeName;

    var menuItems = [
      Navigator.isViewable(type, { isSearch: true }) && <Dropdown.Item onClick={this.handleView} className="btn-danger"><FontAwesomeIcon icon="arrow-right" />&nbsp;{EntityControlMessage.View.niceToString()}</Dropdown.Item >,
      Operations.tryGetOperationInfo(TreeOperation.CreateChild, type) && <Dropdown.Item onClick={this.handleAddChildren}><FontAwesomeIcon icon="square-caret-right" />&nbsp;{TreeViewerMessage.AddChild.niceToString()}</Dropdown.Item>,
      Operations.tryGetOperationInfo(TreeOperation.CreateNextSibling, type) && <Dropdown.Item onClick={this.handleAddSibling}><FontAwesomeIcon icon="square-caret-down" />&nbsp;{TreeViewerMessage.AddSibling.niceToString()}</Dropdown.Item>,
      <Dropdown.Item onClick={this.handleCopyClick}><FontAwesomeIcon icon="copy" />&nbsp;{SearchMessage.Copy.niceToString()}</Dropdown.Item>,
    ].filter(a => a != false) as React.ReactElement<any>[];

    if (this.state.currentMenuItems == undefined) {
      menuItems.push(<Dropdown.Header>{JavascriptMessage.loading.niceToString()}</Dropdown.Header>);
    } else {
      if (menuItems.length && this.state.currentMenuItems.length)
        menuItems.push(<Dropdown.Divider />);

      menuItems.splice(menuItems.length, 0, ...this.state.currentMenuItems);
    }

    return menuItems;
  }

  handleSearchSubmit = (e: React.FormEvent<any>) => {
    e.preventDefault();
    e.stopPropagation();

    this.search(true);
  }


  search(clearExpanded: boolean) {
    this.getFilterOptionsWithSFB().then(filters => {
      let expandedNodes = clearExpanded || !this.state.treeNodes ? [] :
        this.state.treeNodes!.flatMap(allNodes).filter(a => a.nodeState == "Expanded").map(a => a.lite);


      const userFilters = toFilterRequests(filters.filter(fo => fo.frozen == false));
      const frozenFilters = toFilterRequests(filters.filter(fo => fo.frozen == true));

      if (userFilters.length == 0)
        userFilters.push({ token: QueryTokenString.entity<TreeEntity>().append(e => e.level).toString(), operation: "EqualTo", value: 1 });

      return API.findNodes(this.props.typeName, { userFilters, frozenFilters, expandedNodes });
    })
      .then(nodes => {
        const selectedLite = this.state.selectedNode && this.state.selectedNode.lite;
        var newSeleted = selectedLite && nodes.filter(a => is(a.lite, selectedLite)).singleOrNull();
        this.setState({ treeNodes: nodes, selectedNode: newSeleted || undefined });

        if (this.props.onSearch)
          this.props.onSearch();
      });
  }

  renderSearch() {
    const s = this.state;

    const sfb = this.state.simpleFilterBuilder &&
      React.cloneElement(this.state.simpleFilterBuilder, { ref: (e: ISimpleFilterBuilder) => { this.simpleFilterBuilderInstance = e } });

    return (
      <form onSubmit={this.handleSearchSubmit}>
        {s.queryDescription && (s.showFilters ?
          <FilterBuilder
            queryDescription={s.queryDescription}
            filterOptions={s.filterOptions}
            subTokensOptions={SubTokensOptions.CanAnyAll} /> :
          sfb && <div className="simple-filter-builder">{sfb}</div>)}
      </form>
    );
  }

  handleAddRoot = () => {
    Operations.API.construct(this.props.typeName, TreeOperation.CreateRoot)
      .then(ep => Navigator.view(ep!, { requiresSaveOperation: true }))
      .then(te => {
        if (!te)
          return;
        this.state.treeNodes!.push(toTreeNode(te));
        this.forceUpdate();
      });
  }

  handleAddChildren = () => {
    var parent = this.state.selectedNode!;
    Operations.API.constructFromLite(parent.lite, TreeOperation.CreateChild)
      .then(ep => Navigator.view(ep!, { requiresSaveOperation: true }))
      .then(te => {
        if (!te)
          return;
        var newNode = toTreeNode(te);
        parent.loadedChildren.push(newNode);
        parent.childrenCount++;
        fixState(parent);
        this.selectNode(newNode);
      });
  }

  handleAddSibling = () => {

    var sibling = this.state.selectedNode!;

    Operations.API.constructFromLite(sibling.lite, TreeOperation.CreateNextSibling)
      .then(ep => Navigator.view(ep!, { requiresSaveOperation: true }))
      .then(te => {
        if (!te)
          return;
        const newNode = toTreeNode(te);
        const parent = this.findParent(sibling);
        const array = parent ? parent.loadedChildren : this.state.treeNodes!;
        array.insertAt(array.indexOf(sibling) + 1, newNode);
        this.selectNode(newNode);
      });
  }

  handleCopyClick = () => {
    const supportsClipboard = (navigator.clipboard && window.isSecureContext);
    if (!supportsClipboard || !this.state.selectedNode)
      return;

    const text = liteKey(this.state.selectedNode!.lite);
    navigator.clipboard.writeText(text);
  }

  findParent(childNode: TreeNode) {
    return this.state.treeNodes!.flatMap(allNodes).filter(n => n.loadedChildren.contains(childNode)).singleOrNull();
  }

  simpleFilterBuilderInstance?: ISimpleFilterBuilder;
  getFilterOptionsWithSFB(): Promise<FilterOptionParsed[]> {

    const fos = this.state.filterOptions;
    const qd = this.state.queryDescription!;

    if (this.simpleFilterBuilderInstance == undefined)
      return Promise.resolve(fos);

    if (!this.simpleFilterBuilderInstance.getFilters)
      throw new Error("The simple filter builder should have a method with signature: 'getFilters(): FilterOption[]'");

    var filters = this.simpleFilterBuilderInstance.getFilters();

    return Finder.parseFilterOptions(filters, false, qd).then(newFos => {
      this.setState({ filterOptions: newFos });

      return newFos;
    });
  }

  renderToolbar() {
    const s = this.state;
    const selected = s.selectedNode;
    const menuItems = this.renderMenuItems();

    return (
      <div className="btn-toolbar">
        <a className={"sf-query-button sf-filters-header btn btn-light" + (s.showFilters ? " active" : "")}
          onClick={this.handleToggleFilters}
          title={s.showFilters ? JavascriptMessage.hideFilters.niceToString() : JavascriptMessage.showFilters.niceToString()}><FontAwesomeIcon icon="filter" /></a>
        <button className="btn btn-primary" onClick={this.handleSearchSubmit}>{JavascriptMessage.search.niceToString()}</button>
        {Operations.tryGetOperationInfo(TreeOperation.CreateRoot, this.props.typeName) && <button className="btn btn-light" onClick={this.handleAddRoot} disabled={s.treeNodes == null} > <FontAwesomeIcon icon="star" />&nbsp;{TreeViewerMessage.AddRoot.niceToString()}</button>}
        <Dropdown
          onToggle={this.handleSelectedToggle}
          show={s.isSelectOpen}>
          <Dropdown.Toggle id="selectedButton"
            className="sf-query-button sf-tm-selected" disabled={selected == undefined}
            variant="light">
            {`${JavascriptMessage.Selected.niceToString()} (${selected ? getToString(selected.lite) : TreeViewerMessage.None.niceToString()})`}
          </Dropdown.Toggle>
          <Dropdown.Menu>
            {menuItems == undefined ? <Dropdown.Item className="sf-tm-selected-loading">{JavascriptMessage.loading.niceToString()}</Dropdown.Item> :
              menuItems.length == 0 ? <Dropdown.Item className="sf-search-ctxitem-no-results">{JavascriptMessage.noActionsFound.niceToString()}</Dropdown.Item> :
                menuItems.map((e, i) => React.cloneElement(e, { key: i }))}
          </Dropdown.Menu>
        </Dropdown>
        <button className="btn btn-light" onClick={this.handleExplore} ><FontAwesomeIcon icon="magnifying-glass" /> &nbsp; {SearchMessage.Explore.niceToString()}</button>
      </div>
    );
  }

  handleSelectedToggle = () => {

    if (!this.state.isSelectOpen && this.state.currentMenuItems == undefined)
      this.loadMenuItems();

    this.setState({ isSelectOpen: !this.state.isSelectOpen });
  }

  handleExplore = (e: React.MouseEvent<any>) => {
    var path = Finder.findOptionsPath({
      queryName: this.props.typeName,
      filterOptions: Finder.toFilterOptions(this.state.filterOptions),
    });

    if (this.props.avoidChangeUrl)
      window.open(AppContext.toAbsoluteUrl(path));
    else
      AppContext.pushOrOpenInTab(path, e);
  }

  handleToggleFilters = () => {

    this.getFilterOptionsWithSFB().then(() => {
      this.simpleFilterBuilderInstance = undefined;
      this.setState({ simpleFilterBuilder: undefined, showFilters: !this.state.showFilters });
    });
  }


  handleDragStart = (node: TreeNode, e: React.DragEvent<any>) => {
    e.dataTransfer.setData('text', "start"); //cannot be empty string

    var isCopy = e.ctrlKey || e.shiftKey || e.altKey;
    e.dataTransfer.effectAllowed = isCopy ? "copy" : "move";
    this.setState({ draggedNode: node, draggedKind: isCopy ? "Copy" : "Move" });
  }


  handleDragOver = (node: TreeNode, e: React.DragEvent<any>) => {
    e.preventDefault();
    const de = e.nativeEvent as DragEvent;
    const span = e.currentTarget as HTMLElement;
    const newPosition = this.getOffset(de.pageY, span.getBoundingClientRect(), 7);

    const s = this.state;

    if (s.draggedOver == null ||
      s.draggedOver.node != node ||
      s.draggedOver.position != newPosition) {

      this.setState({
        draggedOver: {
          node: node,
          position: newPosition,
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

  handleDragEnd = (node: TreeNode, e: React.DragEvent<any>) => {
    this.setState({ draggedNode: undefined, draggedOver: undefined, draggedKind: undefined });
  }

  handleDrop = (node: TreeNode, e: React.DragEvent<any>) => {
    const dragged = this.state.draggedNode!;
    const over = this.state.draggedOver!;

    if (dragged == over.node)
      return;

    var nodeParent = this.findParent(over.node);
    const ts = TreeClient.settings[this.props.typeName];
    if (ts?.dragTargetIsValid)
      ts.dragTargetIsValid(dragged, over.position == "Middle" ? over.node : nodeParent)
        .then(valid => {
          if (!valid)
            return;

          this.moveOrCopyOperation(nodeParent, dragged, over);

        })
    else
      this.moveOrCopyOperation(nodeParent, dragged, over);
  }

  moveOrCopyOperation(nodeParent: TreeNode | null, dragged: TreeNode, over: DraggedOver) {

    var partial: Partial<MoveTreeModel> =
      over.position == "Middle" ? { newParent: over.node.lite, insertPlace: "LastNode" } :
        over.position == "Top" ? { newParent: nodeParent ? nodeParent.lite : undefined, insertPlace: "Before", sibling: over.node.lite } :
          over.position == "Bottom" ? { newParent: nodeParent ? nodeParent.lite : undefined, insertPlace: "After", sibling: over.node.lite } :
            {};

    var toExpand = over.position == "Middle" ? over.node : nodeParent;

    if (this.state.draggedKind == "Move") {
      const treeModel = MoveTreeModel.New(partial);
      Operations.API.executeLite(dragged.lite, TreeOperation.Move, treeModel).then(() =>

        this.setState({ draggedNode: undefined, draggedOver: undefined, draggedKind: undefined, selectedNode: dragged }, () => {
          if (toExpand)
            toExpand.nodeState = "Expanded";

          this.search(false);
        })
      );

    } else {
      const s = TreeClient.settings[this.props.typeName];
      var promise = s?.createCopyModel ? s.createCopyModel(dragged.lite, partial) : Promise.resolve(MoveTreeModel.New(partial));
      promise.then(treeModel => treeModel &&
        Operations.API.constructFromLite(dragged.lite, TreeOperation.Copy, treeModel).then(() =>
          this.setState({ draggedNode: undefined, draggedOver: undefined, draggedKind: undefined, selectedNode: dragged }, () => {
            if (toExpand)
              toExpand.nodeState = "Expanded";

            this.search(false);
          })
        ));
    };
  }
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

class TreeNodeControl extends React.Component<TreeNodeControlProps> {

  renderIcon(nodeState: TreeNodeState) {

    var node = this.props.treeNode;
    const tv = this.props.treeViewer;
    switch (nodeState) {
      case "Collapsed": return (
        <span onClick={() => tv.handleNodeIconClick(node)} className="tree-icon" >
          <FontAwesomeIcon icon={["far", "square-plus"]} />
        </span>);
      case "Expanded": return (
        <span onClick={() => tv.handleNodeIconClick(node)} className="tree-icon" >
          <FontAwesomeIcon icon={["far", "square-minus"]} />
        </span>);
      case "Filtered": return (
        <span onClick={() => tv.handleNodeIconClick(node)} className="tree-icon fa-layers fa-fw" >
          <FontAwesomeIcon icon="square" />
          <FontAwesomeIcon icon="filter" inverse transform="shrink-2" />
        </span>);
      default: return <span className="place-holder" />;
    }
  }

  render(): React.ReactElement<any> {

    var node = this.props.treeNode;
    const tv = this.props.treeViewer;
    return (
      <li>
        <div draggable={tv.props.allowMove}
          onDragStart={de => tv.handleDragStart(node, de)}
          onDragEnter={de => tv.handleDragOver(node, de)}
          onDragOver={de => tv.handleDragOver(node, de)}
          onDragEnd={de => tv.handleDragEnd(node, de)}
          onDrop={this.props.dropDisabled ? undefined : de => tv.handleDrop(node, de)}
          style={this.getDragAndDropStyle(node)}>
          {this.renderIcon(node.nodeState)}

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
              <TreeNodeControl key={i} treeViewer={tv} treeNode={n} dropDisabled={this.props.dropDisabled || n == tv.state.draggedNode} />)}
          </ul>
        }
      </li>
    );
  }

  getDragAndDropStyle(node: TreeNode): React.CSSProperties | undefined {
    const s = this.props.treeViewer.state;

    if (s.draggedNode == undefined)
      return undefined;

    if (node == s.draggedNode)
      return { opacity: 0.5 };

    const over = s.draggedOver;

    if (over && node == over.node) {

      const color = this.props.dropDisabled ? "rgb(193, 0, 0)" :
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
}
