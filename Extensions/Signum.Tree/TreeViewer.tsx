import * as React from 'react'
import { OverlayTrigger, Tooltip } from "react-bootstrap";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { TreeClient, TreeNode, TreeNodeState, TreeOptions, TreeOptionsParsed } from './TreeClient'
import { classes } from '@framework/Globals'
import * as AppContext from '@framework/AppContext'
import { Navigator } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { Operations } from '@framework/Operations'
import { SearchMessage, JavascriptMessage, EntityControlMessage, toLite, liteKey, getToString, Lite } from '@framework/Signum.Entities'
import { TreeViewerMessage, TreeEntity, TreeOperation, MoveTreeModel, TreeMessage } from './Signum.Tree'
import { FilterOptionParsed, ColumnOptionParsed, QueryDescription, SubTokensOptions, FilterOption, ColumnOption, QueryRequest, hasToArray, ResultRow } from "@framework/FindOptions";
import FilterBuilder from "@framework/SearchControl/FilterBuilder";
import { ISimpleFilterBuilder } from "@framework/Search";
import { is } from "@framework/Signum.Entities";
import ContextMenu, { ContextMenuPosition, getMouseEventPosition } from '@framework/SearchControl/ContextMenu'
import { ContextualItemsContext, ContextualMenuItem, renderContextualItems, SearchableMenuItem } from "@framework/SearchControl/ContextualItems";
import { Entity } from "@framework/Signum.Entities";
import { tryGetMixin } from "@framework/Signum.Entities";
import { Dropdown, DropdownButton } from 'react-bootstrap';
import "./TreeViewer.css"
import { QueryTokenString, getTypeInfo, tryGetOperationInfo } from '@framework/Reflection';
import * as Hooks from '@framework/Hooks'
import { DisabledMixin } from '@framework/Signum.Basics'
import SearchControlLoaded, { ColumnParsed } from '@framework/SearchControl/SearchControlLoaded';
import { AutoFocus } from '../../Signum/React/Components/AutoFocus';
import { KeyNames } from '../../Signum/React/Components/Basic';

interface TreeViewerProps {
  treeOptions: TreeOptions;
  defaultSelectedLite?: Lite<TreeEntity>;
  showContextMenu?: boolean | "Basic";
  allowMove?: boolean;
  avoidChangeUrl?: boolean;
  onDoubleClick?: (selectedNode: TreeNode, e: React.MouseEvent<any>) => void;
  onSelectedNode?: (selectedNode: TreeNode | undefined) => void;
  onSearch?: (top: TreeOptionsParsed) => void;
  initialShowFilters?: boolean;
  showToolbar?: boolean;
  showExpandCollapseButtons?: boolean;
  deps?: React.DependencyList;
}

export type DraggedPosition = "Top" | "Bottom" | "Middle";

export interface DraggedOver {
  node: TreeNode;
  position: DraggedPosition;
}

interface VisibleColumnsWithFormatter extends ColumnParsed {
  columnIndex: number;
}

interface TreeViewerState {
  treeNodes?: Array<TreeNode>;
  resultColumns?: string[];
  selectedNode?: TreeNode;
  treeOptionsParsed?: TreeOptionsParsed;
  queryDescription?: QueryDescription;
  simpleFilterBuilder?: React.ReactElement<any>;
  showFilters?: boolean;

  isSelectOpen: boolean;

  draggedNode?: TreeNode;
  draggedKind?: "Move" | "Copy";
  draggedOver?: DraggedOver;

  currentMenuItems?: ContextualMenuItem[];
  contextualMenu?: {
    position: ContextMenuPosition;
    showSearchBox?: boolean;
    filter?: string;
  };
}



export class TreeViewer extends React.Component<TreeViewerProps, TreeViewerState>{

  static maxToArrayElements = 100;

  constructor(props: TreeViewerProps) {
    super(props);
    this.state = {
      showFilters: props.initialShowFilters,
      isSelectOpen: false
    };
  }

  selectNode(node: TreeNode | undefined) : void {

    this.setState({ selectedNode: node });
    if (this.props.onSelectedNode)
      this.props.onSelectedNode(node);
  }

  componentWillMount() : void {
    this.initialize(this.props.treeOptions);
  }

  componentWillReceiveProps(newProps: TreeViewerProps) : void {
    var path = TreeClient.treePath(newProps.treeOptions);
    if (path == TreeClient.treePath(this.props.treeOptions)) {
      this.searchIfDeps(newProps);
      return;
    }

    if (this.state.treeOptionsParsed && this.state.queryDescription) {
      if (path == TreeClient.treePath(TreeClient.toTreeOptions(this.state.treeOptionsParsed, this.state.queryDescription))) {
        this.searchIfDeps(newProps);
        return;
      }
    }

    this.state = {
      showFilters: newProps.initialShowFilters,
      isSelectOpen: false,
    };
    this.forceUpdate();

    this.initialize(newProps.treeOptions);
  }

  searchIfDeps(newProps: TreeViewerProps): void {
    if (Hooks.areEqualDeps(this.props.deps ?? [], newProps.deps ?? [])) {
      this.search(false);
    }
  }

  initialize(to: TreeOptions): void {

    Finder.getQueryDescription(to.typeName)
      .then(qd => {
        this.setState({ queryDescription: qd }, () =>
        TreeClient.parseTreeOptions(to, qd)
          .then((top) => {
            this.setState({ treeOptionsParsed: top }, () => {
              const qs = Finder.getSettings(to.typeName);
              const sfb = qs?.simpleFilterBuilder && qs.simpleFilterBuilder({ queryDescription: this.state.queryDescription!, initialFilterOptions: this.state.treeOptionsParsed!.filterOptions, search: () => this.search(true) });
              this.setState({ simpleFilterBuilder: sfb });
              if (sfb)
                this.setState({ showFilters: false });

              this.search(true, false, true);
            });
          }));
      });
  }

  handleFullScreenClick = (ev: React.MouseEvent<any>): void => {


    const path = this.getCurrentUrl();

    if (ev.ctrlKey || ev.button == 1)
      window.open(AppContext.toAbsoluteUrl(path));
    else
      AppContext.navigate(path);
  };

  getCurrentUrl() : string {
    return TreeClient.treePath(TreeClient.toTreeOptions(this.state.treeOptionsParsed!, this.state.queryDescription!));
  }

  handleNodeIconClick = (n: TreeNode): void => {
    if (n.nodeState == "Collapsed" || n.nodeState == "Filtered") {
      n.nodeState = "Expanded";
      this.search(false);
    }
    else if (n.nodeState == "Expanded") {
      n.nodeState = "Collapsed";
      allNodes(n).forEach(n => n.nodeState = "Collapsed");
      this.forceUpdate();
    }
  }

  handleNodeTextClick = (n: TreeNode): void => {
    this.selectNode(n);
  }

  handleNodeTextDoubleClick = (n: TreeNode, e: React.MouseEvent<any>): void => {
    if (this.props.onDoubleClick)
      this.props.onDoubleClick(n, e);
    else
      this.handleView();
  }

  handleView = (): void => {
    const node = this.state.selectedNode!;
    Navigator.view(node.lite)
      .then(() => this.search(false));
  }


  render(): React.ReactElement {
    return (
      <div>
        {this.renderSearch()}
        {this.props.showToolbar && <>
          <br />
          {this.renderToolbar()}
          <br />
        </>}
        
        {!this.props.showToolbar && this.props.showExpandCollapseButtons && this.renderExpandCollapseButtons()}

        <div className="tree-container sf-scroll-table-container table-responsive">
          <table className="sf-search-results table table-hover table-sm">
            <thead>
              {this.renderHeaders()}
            </thead>
            <tbody>
              {this.renderRows()}
            </tbody>
          </table>
        </div>
        {this.state.contextualMenu && this.renderContextualMenu()}
      </div>
    );
  }

  renderHeaders = (): React.ReactElement => {
    const visibleColumns = this.getVisibleColumnsWithFormatter();
    return (
      <tr>
        <th
          className="noOrder"
          data-column-name="Name">
          {getTypeInfo(this.props.treeOptions.typeName).members["Name"].niceName}
        </th>
        {visibleColumns.map(({ column: co, columnIndex: ci }, i) =>
          <th key={i}
            className={classes(co.hiddenColumn && "sf-hidden-column", "noOrder")}
            data-column-name={co.token && co.token.fullKey}
            data-column-index={ci}>
            {co.displayName}
          </th>)}
      </tr>
    );
  }

  renderRows = (): React.ReactNode | string => {
    if (!this.state.treeNodes)
      return JavascriptMessage.loading.niceToString();

    const visibleColumns = this.getVisibleColumnsWithFormatter();
    return this.state.treeNodes.map((node, i) =>
      <TreeNodeControl key={i} treeViewer={this} treeNode={node} columns={visibleColumns} dropDisabled={node == this.state.draggedNode} />);
  }

  getVisibleColumnsWithFormatter = (): VisibleColumnsWithFormatter[] => {
    if (!this.state.resultColumns)
      return [];

    const qs = Finder.getSettings(this.props.treeOptions.typeName);
    const resultColumns = this.state.resultColumns;
    const columnOptions = this.state.treeOptionsParsed!.columnOptions
      .map((co, i) => ({ co, i }))
      .filter(({ co, i }) => !co.hiddenColumn &&
        co.token?.fullKey != "Id" &&
        co.token?.fullKey != "Name" &&
        co.token?.fullKey != "FullName" &&
        resultColumns.some(rc => rc == co.token?.fullKey));

    return columnOptions.map(({ co, i }) => ({
      column: co,
      columnIndex: i,
      hasToArray: hasToArray(co.token),
      cellFormatter: (co.token && Finder.getCellFormatter(qs, co.token, undefined)),
      resultIndex: co.token == undefined || resultColumns == null ? -1 : resultColumns.indexOf(co.token.fullKey)
    } as VisibleColumnsWithFormatter));
  }

  handleNodeTextContextMenu = (n: TreeNode, e: React.MouseEvent<any>) : void => {
    e.preventDefault();
    e.stopPropagation();

    this.setState({
      selectedNode: n,
      contextualMenu: {
        position: getMouseEventPosition(e, document.querySelector('.tree-container tbody')),
      }
    }, () => this.loadMenuItems());
  }

  loadMenuItems() : void {
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
        .then(menuPack => this.setState({
          currentMenuItems: menuPack.items,
          contextualMenu: this.state.contextualMenu && Object.assign(this.state.contextualMenu, { showSearchBox: menuPack.showSearch })
        }));
    }
  }

  handleContextOnHide = (): void => {
    this.setState({ contextualMenu: undefined, currentMenuItems: undefined });
  }

  renderContextualMenu(): React.ReactElement | null {
    const cm = this.state.contextualMenu!;
    if (!this.state.selectedNode)
      return null;

      var menuItems = this.renderMenuItems();

    return (
      <ContextMenu id="table-context-menu" position={cm.position} onHide={this.handleContextOnHide} itemsCount={menuItems.length}>
        {this.state.contextualMenu?.showSearchBox &&
          <AutoFocus>
            <input
              type="search"
              className="form-control form-control-sm dropdown-item"
              value={this.state?.contextualMenu?.filter}
              placeholder={SearchMessage.Search.niceToString()}
              onKeyDown={this.handleMenuFilterKeyDown}
              onChange={this.handleMenuFilterChange} />
          </AutoFocus>}
          <div style={{ position:"relative", maxHeight: "calc(100vh - 400px)", overflowY: "auto" }}>
            {menuItems.map((mi, i) => React.cloneElement((mi as SearchableMenuItem).menu ?? mi, { key: i }))}
          </div>
      </ContextMenu>
    );
  }

  handleMenuFilterChange = (e: React.ChangeEvent<HTMLInputElement>) : void => {
    const cm = this.state.contextualMenu;

    cm && this.setState({ contextualMenu: Object.assign(cm, { filter: e.currentTarget.value }) })
  }

  handleMenuFilterKeyDown = (e: React.KeyboardEvent<any>) : void => {
    if (!e.shiftKey && e.key == KeyNames.arrowDown) {

      e.preventDefault();
      e.stopPropagation();

      var firstItem = document.querySelector("#table-context-menu a.dropdown-item:not(.disabled)") as HTMLAnchorElement
      if (firstItem && typeof firstItem.focus === 'function')
        firstItem.focus();
    }
  }

  renderMenuItems(): ContextualMenuItem[] {

    let type = this.props.treeOptions.typeName;

    var menuItems = [
      Navigator.isViewable(type, { isSearch: "main" }) && <Dropdown.Item onClick={this.handleView} className="btn-danger"><FontAwesomeIcon icon="arrow-right" />&nbsp;{EntityControlMessage.View.niceToString()}</Dropdown.Item >,
      tryGetOperationInfo(TreeOperation.CreateChild, type) && <Dropdown.Item onClick={this.handleAddChildren}><FontAwesomeIcon icon="square-caret-right" />&nbsp;{TreeViewerMessage.AddChild.niceToString()}</Dropdown.Item>,
      tryGetOperationInfo(TreeOperation.CreateNextSibling, type) && <Dropdown.Item onClick={this.handleAddSibling}><FontAwesomeIcon icon="square-caret-down" />&nbsp;{TreeViewerMessage.AddSibling.niceToString()}</Dropdown.Item>,
      <Dropdown.Item onClick={this.handleCopyClick}><FontAwesomeIcon icon="copy" />&nbsp;{SearchMessage.Copy.niceToString()}</Dropdown.Item>,
    ].filter(a => a != false) as ContextualMenuItem[];

    if (this.state.currentMenuItems == undefined) {
      menuItems.push(<Dropdown.Header>{JavascriptMessage.loading.niceToString()}</Dropdown.Header>);
    } else {
      if (menuItems.length && this.state.currentMenuItems.length)
        menuItems.push(<Dropdown.Divider />);

      const filter = this.state.contextualMenu?.filter;
      const filtered = filter ? this.state.currentMenuItems.filter(mi => !(mi as SearchableMenuItem).fullText || (mi as SearchableMenuItem).fullText.toLowerCase().contains(filter.toLowerCase())) : this.state.currentMenuItems;

      menuItems.splice(menuItems.length, 0, ...filtered.map(mi => (mi as SearchableMenuItem).menu ?? mi));      
    }

    return menuItems;
  }

  handleSearchSubmit = (e: React.FormEvent<any>) : void => {
    e.preventDefault();
    e.stopPropagation();

    this.search(true);
  }

  getQueryRequest(avoidHiddenColumns?: boolean): QueryRequest {
    const fo = TreeClient.toFindOptionsParsed(this.state.treeOptionsParsed!);
    const qs = Finder.getSettings(this.props.treeOptions.typeName);
    const result = Finder.getQueryRequest(fo, qs, avoidHiddenColumns);

    return result;
  }

  search(clearExpanded: boolean, loadDescendants: boolean = false, considerDefaultSelectedLite: boolean = false): undefined {

    if (!this.state.treeOptionsParsed || !this.state.queryDescription)
      return;

    const defaultSelectedLite = considerDefaultSelectedLite ? this.props.defaultSelectedLite : undefined;

    this.getFilterOptionsWithSFB().then(filters => {
      let expandedNodes = clearExpanded || !this.state.treeNodes ? [] :
        this.state.treeNodes!.flatMap(allNodes).filter(a => a.nodeState == "Expanded").map(a => a.lite);

      const userFilters = Finder.toFilterRequests(filters.filter(fo => fo.frozen == false));
      const frozenFilters = Finder.toFilterRequests(filters.filter(fo => fo.frozen == true));

      const qr = this.getQueryRequest(true);
      const columns = qr.columns;

      if (userFilters.length == 0)
        userFilters.push({ token: QueryTokenString.entity<TreeEntity>().append(e => e.level).toString(), operation: "EqualTo", value: 1 });

      return TreeClient.API.findNodes(this.props.treeOptions.typeName, { userFilters, frozenFilters, columns, expandedNodes, loadDescendants });
    })
      .then(response => {
        const nodes = response.nodes;
        const selectedLite = this.state.selectedNode?.lite;
        var newSeleted = selectedLite && nodes.flatMap(allNodes).filter(a => is(a.lite, selectedLite)).singleOrNull();

        if (newSeleted == null)
          newSeleted = defaultSelectedLite && nodes.flatMap(allNodes).filter(a => is(a.lite, defaultSelectedLite)).singleOrNull();

        this.setState({ treeNodes: nodes, resultColumns: response.columns, selectedNode: newSeleted || undefined });
        this.forceUpdate();

        if (this.props.onSearch)
          this.props.onSearch(this.state.treeOptionsParsed!);

        if (defaultSelectedLite && newSeleted && is(newSeleted?.lite, defaultSelectedLite))
          this.selectNode(newSeleted!);
      });
  }

  renderSearch(): React.ReactElement {
    const s = this.state;

    const sfb = this.state.simpleFilterBuilder &&
      React.cloneElement(this.state.simpleFilterBuilder, { ref: (e: ISimpleFilterBuilder) => { this.simpleFilterBuilderInstance = e } });

    return (
      <form onSubmit={this.handleSearchSubmit}>
        {s.treeOptionsParsed && s.queryDescription && (s.showFilters ?
          <FilterBuilder
            queryDescription={s.queryDescription}
            filterOptions={s.treeOptionsParsed.filterOptions}
            subTokensOptions={SubTokensOptions.CanAnyAll} /> :
          sfb && <div className="simple-filter-builder">{sfb}</div>)}
      </form>
    );
  }

  handleAddRoot = (): void => {
    Operations.API.construct(this.props.treeOptions.typeName, TreeOperation.CreateRoot)
      .then(ep => Navigator.view(ep!, { requiresSaveOperation: true }))
      .then(te => {
        if (!te)
          return;

        const qr = this.getQueryRequest(true);
        const columns = qr.columns;

        TreeClient.API.getNode(this.props.treeOptions.typeName, { lite: toLite(te), columns })
          .then((node) => {
            this.state.treeNodes!.push(toTreeNode(te, node));
            this.forceUpdate();
          });
      });
  }

  handleAddChildren = (): void => {
    var parent = this.state.selectedNode!;
    Operations.API.constructFromLite(parent.lite, TreeOperation.CreateChild)
      .then(ep => Navigator.view(ep!, { requiresSaveOperation: true }))
      .then(te => {
        if (!te)
          return;

        const qr = this.getQueryRequest(true);
        const columns = qr.columns;

        TreeClient.API.getNode(this.props.treeOptions.typeName, { lite: toLite(te), columns })
          .then(node => {
            var newNode = toTreeNode(te, node);
            parent.loadedChildren.push(newNode);
            parent.childrenCount++;
            TreeClient.fixState(parent);
            this.selectNode(newNode);
          })
      });
  }

  handleAddSibling = (): void => {

    var sibling = this.state.selectedNode!;

    Operations.API.constructFromLite(sibling.lite, TreeOperation.CreateNextSibling)
      .then(ep => Navigator.view(ep!, { requiresSaveOperation: true }))
      .then(te => {
        if (!te)
          return;

        const qr = this.getQueryRequest(true);
        const columns = qr.columns;

        TreeClient.API.getNode(this.props.treeOptions.typeName, { lite: toLite(te), columns })
          .then(node => {
            const newNode = toTreeNode(te, node);
            const parent = this.findParent(sibling);
            const array = parent ? parent.loadedChildren : this.state.treeNodes!;
            array.insertAt(array.indexOf(sibling) + 1, newNode);
            this.selectNode(newNode);
          });
      });
  }

  handleCopyClick = (): void => {
    const supportsClipboard = (navigator.clipboard && window.isSecureContext);
    if (!supportsClipboard || !this.state.selectedNode)
      return;

    const text = liteKey(this.state.selectedNode!.lite);
    navigator.clipboard.writeText(text);
  }

  findParent(childNode: TreeNode): TreeNode | null {
    return this.state.treeNodes!.flatMap(allNodes).filter(n => n.loadedChildren.contains(childNode)).singleOrNull();
  }

  simpleFilterBuilderInstance?: ISimpleFilterBuilder;
  getFilterOptionsWithSFB(): Promise<FilterOptionParsed[]> {

    const fos = this.state.treeOptionsParsed!.filterOptions;
    const qd = this.state.queryDescription!;

    if (this.simpleFilterBuilderInstance == undefined)
      return Promise.resolve(fos);

    if (!this.simpleFilterBuilderInstance.getFilters)
      throw new Error("The simple filter builder should have a method with signature: 'getFilters(): FilterOption[]'");

    var filters = this.simpleFilterBuilderInstance.getFilters();

    return Finder.parseFilterOptions(filters, false, qd).then(newFos => {
      var top = this.state.treeOptionsParsed!;
      top.filterOptions = newFos;
      this.setState({ treeOptionsParsed: top });

      return newFos;
    });
  }

  renderToolbar(): React.ReactElement{
    const s = this.state;
    const selected = s.selectedNode;
    const menuItems = this.renderMenuItems();

    return (
      <div className="btn-toolbar">
        <a className={"sf-query-button sf-filters-header btn btn-light" + (s.showFilters ? " active" : "")}
          onClick={this.handleToggleFilters}
          title={s.showFilters ? JavascriptMessage.hideFilters.niceToString() : JavascriptMessage.showFilters.niceToString()}><FontAwesomeIcon icon="filter" /></a>
        <button className="btn btn-primary" onClick={this.handleSearchSubmit}>{JavascriptMessage.search.niceToString()}</button>
        {this.props.showExpandCollapseButtons && <button className="btn btn-light" onClick={this.handleExpandAll} disabled={s.treeNodes == null}>{TreeViewerMessage.ExpandAll.niceToString()}</button>}
        {this.props.showExpandCollapseButtons && <button className="btn btn-light" onClick={this.handleCollapseAll} disabled={s.treeNodes == null}>{TreeViewerMessage.CollapseAll.niceToString()}</button>}
        {tryGetOperationInfo(TreeOperation.CreateRoot, this.props.treeOptions.typeName) && <button className="btn btn-light" onClick={this.handleAddRoot} disabled={s.treeNodes == null} > <FontAwesomeIcon icon="star" />&nbsp;{TreeViewerMessage.AddRoot.niceToString()}</button>}
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
                menuItems.map((mi, i) => React.cloneElement((mi as SearchableMenuItem).menu ?? mi, { key: i }))}
          </Dropdown.Menu>
        </Dropdown>
        <button className="btn btn-light" onClick={this.handleExplore} ><FontAwesomeIcon icon="magnifying-glass" /> &nbsp; {TreeMessage.ListView.niceToString()}</button>
      </div>
    );
  }

  renderExpandCollapseButtons(): React.ReactElement {
    const s = this.state;

    return (
      <div className="btn-toolbar">
        <button className="btn btn-sm btn-light" onClick={this.handleExpandAll} disabled={s.treeNodes == null} title={TreeViewerMessage.ExpandAll.niceToString()}>
          <FontAwesomeIcon icon="plus" />
        </button>

        <button className="btn btn-sm btn-light" onClick={this.handleCollapseAll} disabled={s.treeNodes == null} title={TreeViewerMessage.CollapseAll.niceToString()}>
          <FontAwesomeIcon icon="minus" />
        </button>
      </div>
    );
  }

  handleExpandAll = (): undefined => {
    this.search(true, true, true);
  }

  handleCollapseAll = (): undefined => {
    this.search(true, false, true);
  }

  handleSelectedToggle = (): undefined => {

    if (!this.state.isSelectOpen && this.state.currentMenuItems == undefined)
      this.loadMenuItems();

    this.setState({ isSelectOpen: !this.state.isSelectOpen });
  }

  handleExplore = (e: React.MouseEvent<any>) : void => {
    const fop = TreeClient.toFindOptionsParsed(this.state.treeOptionsParsed!);
    const fo = Finder.toFindOptions(fop, this.state.queryDescription!, false);

    var path = Finder.findOptionsPath(fo);

    if (this.props.avoidChangeUrl)
      window.open(AppContext.toAbsoluteUrl(path));
    else
      AppContext.pushOrOpenInTab(path, e);
  }

  handleToggleFilters = () : void => {

    this.getFilterOptionsWithSFB().then(() => {
      this.simpleFilterBuilderInstance = undefined;
      this.setState({ simpleFilterBuilder: undefined, showFilters: !this.state.showFilters });
    });
  }


  handleDragStart = (node: TreeNode, e: React.DragEvent<any>): void => {
    e.dataTransfer.setData('text', "start"); //cannot be empty string

    var isCopy = e.ctrlKey || e.shiftKey || e.altKey;
    e.dataTransfer.effectAllowed = isCopy ? "copy" : "move";
    this.setState({ draggedNode: node, draggedKind: isCopy ? "Copy" : "Move" });
  }


  handleDragOver = (node: TreeNode, e: React.DragEvent<any>): void => {
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

  handleDragEnd = (node: TreeNode, e: React.DragEvent<any>): void => {
    this.setState({ draggedNode: undefined, draggedOver: undefined, draggedKind: undefined });
  }

  handleDrop = (node: TreeNode, e: React.DragEvent<any>): void => {
    const dragged = this.state.draggedNode!;
    const over = this.state.draggedOver!;

    if (dragged == over.node)
      return;

    var nodeParent = this.findParent(over.node);
    const ts = TreeClient.settings[this.props.treeOptions.typeName];
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

  moveOrCopyOperation(nodeParent: TreeNode | null, dragged: TreeNode, over: DraggedOver): void {

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
      const s = TreeClient.settings[this.props.treeOptions.typeName];
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

function toTreeNode(treeEntity: TreeEntity, newNode: TreeNode): TreeNode {

  var dm = tryGetMixin(treeEntity, DisabledMixin);
  return {
    values: newNode.values,
    lite: toLite(treeEntity),
    name: treeEntity.name!,
    fullName: treeEntity.fullName,
    childrenCount: 0,
    disabled: dm != null && Boolean(dm.isDisabled),
    level: newNode.level,
    loadedChildren: [],
    nodeState: "Leaf"
  };
}

interface TreeNodeControlProps {
  treeViewer: TreeViewer;
  treeNode: TreeNode;
  columns: VisibleColumnsWithFormatter[];
  dropDisabled: boolean;
}

class TreeNodeControl extends React.Component<TreeNodeControlProps> {

  renderIcon(nodeState: TreeNodeState) {

    var node = this.props.treeNode;
    const tv = this.props.treeViewer;
    switch (nodeState) {
      case "Collapsed": return (
        <span onClick={() => tv.handleNodeIconClick(node)}  className="tree-icon" >
          <FontAwesomeIcon icon={["far", "square-plus"]} title={EntityControlMessage.Expand.niceToString()}/>
        </span>);
      case "Expanded": return (
        <span onClick={() => tv.handleNodeIconClick(node)}  className="tree-icon" >
          <FontAwesomeIcon icon={["far", "square-minus"]} title={EntityControlMessage.Collapse.niceToString()}/>
        </span>);
      case "Filtered": return (
        <span onClick={() => tv.handleNodeIconClick(node)}  className="tree-icon fa-layers fa-fw" >
          <FontAwesomeIcon icon="square" title={EntityControlMessage.Expand.niceToString()}/>
          <FontAwesomeIcon icon="filter" inverse transform="shrink-2" title={EntityControlMessage.Expand.niceToString()} />
        </span>);
      default: return <span className="place-holder"></span>;
    }
  }

  render(): React.ReactElement<any> {

    var node = this.props.treeNode;
    const tv = this.props.treeViewer;
    return (
      <>
        <tr>
          <td>
            <div className="try-no-wrap"
              draggable={tv.props.allowMove}
              onDragStart={de => tv.handleDragStart(node, de)}
              onDragEnter={de => tv.handleDragOver(node, de)}
              onDragOver={de => tv.handleDragOver(node, de)}
              onDragEnd={de => tv.handleDragEnd(node, de)}
              onDrop={this.props.dropDisabled ? undefined : de => tv.handleDrop(node, de)}
              style={{ marginLeft: `${(node.level - 1) * 32}px`, ...this.getDragAndDropStyle(node) }}>
              {this.renderIcon(node.nodeState)}

              <span className={classes("tree-label", node == tv.state.selectedNode && "tree-selected", node.disabled && "tree-disabled")}
                onDoubleClick={e => tv.handleNodeTextDoubleClick(node, e)}
                onClick={() => tv.handleNodeTextClick(node)}
                onContextMenu={tv.props.showContextMenu != false ? e => tv.handleNodeTextContextMenu(node, e) : undefined}>
                {node.fullName != node.name ? <OverlayTrigger
                  overlay={
                    <Tooltip>
                      <span>{node.fullName}</span>
                    </Tooltip>
                  }>
                  <span>{node.name}</span>
                </OverlayTrigger> : node.name}
              </span>
            </div>
          </td>
          {this.renderExtraColumns(node)}
        </tr>

        {
          node.loadedChildren.length > 0 && (node.nodeState == "Expanded" || node.nodeState == "Filtered") &&
          node.loadedChildren.map((n, i) =>
            <TreeNodeControl key={i} treeViewer={tv} treeNode={n} columns={this.props.columns} dropDisabled={this.props.dropDisabled || n == tv.state.draggedNode} />)
        }
      </>
    );
  }

  renderExtraColumns = (node: TreeNode) => {
    return this.props.columns.map(c =>
      <td>
        {this.getColumnElement(node, c)}
      </td>);
  }

  getColumnElement(node: TreeNode, c: ColumnParsed) {

    var fctx: Finder.CellFormatterContext = {
      refresh: undefined,
      columns: this.props.columns.map(c => c.column.token!.key),
      row: ({
        entity: node.lite,
        columns: node.values,
      }) as ResultRow,
      rowIndex: -1,
    };

    return c.resultIndex == -1 || c.cellFormatter == undefined ? undefined :
      c.hasToArray != null ? SearchControlLoaded.joinNodes((this.getRowValue(fctx.row, c.resultIndex) as unknown[]).map(v => c.cellFormatter!.formatter(v, fctx, c)),
      c.hasToArray.key == "SeparatedByComma" || c.hasToArray.key == "SeparatedByCommaDistinct" ? <span className="text-muted">, </span> : <br />, TreeViewer.maxToArrayElements) :
    c.cellFormatter.formatter(this.getRowValue(fctx.row, c.resultIndex), fctx, c);
  }

  getRowValue(row: ResultRow, resultIndex: number | "Entity") {
    if (resultIndex == "Entity")
      return row.entity;

    return row.columns[resultIndex];
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
