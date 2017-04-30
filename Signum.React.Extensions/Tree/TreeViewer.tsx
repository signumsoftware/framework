import * as React from 'react'
import { MenuItem } from 'react-bootstrap'
import { API, TreeNode, TreeNodeState, fixState } from './TreeClient'
import { Dic, classes, DomUtils } from '../../../Framework/Signum.React/Scripts/Globals'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import ContextMenu from '../../../Framework/Signum.React/Scripts/SearchControl/ContextMenu'
import { ContextMenuPosition } from '../../../Framework/Signum.React/Scripts/SearchControl/ContextMenu'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import * as EntityOperations from '../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import { SearchMessage, JavascriptMessage, toLite, ExecuteSymbol, ConstructSymbol_From, ConstructSymbol_Simple, DeleteSymbol, OperationMessage } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TreeViewerMessage, TreeEntity, TreeOperation } from './Signum.Entities.Tree'
import * as TreeClient from './TreeClient'
import { FilterOptionParsed, QueryDescription, FilterRequest, SubTokensOptions } from "../../../Framework/Signum.React/Scripts/FindOptions";
import FilterBuilder from "../../../Framework/Signum.React/Scripts/SearchControl/FilterBuilder";
import { ISimpleFilterBuilder } from "../../../Framework/Signum.React/Scripts/Search";

require("./TreeViewer.css");

interface TreeViewerProps {
    typeName: string;
    onDoubleClick?: (selectedNode: TreeNode, e: React.MouseEvent<any>) => void;
    onSelectedNode?: (selectedNode: TreeNode | undefined) => void;
}

interface TreeViewerState {
    treeNodes?: Array<TreeNode>;
    selectedNode?: TreeNode;
    filterOptions: FilterOptionParsed[];
    queryDescription?: QueryDescription;
    simpleFilterBuilder?: React.ReactElement<any>;
    showFilters?: boolean;

    contextualMenu?: {
        position: ContextMenuPosition;
    };
}



export class TreeViewer extends React.Component<TreeViewerProps, TreeViewerState>{

    constructor(props: TreeViewerProps) {
        super(props);
        this.state = {
            queryDescription: undefined,
            filterOptions: [],
            selectedNode: undefined,
        };
    }

    selectNode(node: TreeNode | undefined) {

        this.setState({ selectedNode: node });
        if (this.props.onSelectedNode)
            this.props.onSelectedNode(node);
    }

    componentWillMount() {
        Finder.getQueryDescription(this.props.typeName)
            .then(qd => {
                const qs = Finder.getSettings(this.props.typeName);
                const sfb = qs && qs.simpleFilterBuilder && qs.simpleFilterBuilder(qd, this.state.filterOptions);
                this.setState({ queryDescription: qd, simpleFilterBuilder: sfb, showFilters: false });
            })
            .done();
            
        API.getRoots(this.props.typeName)
            .then(t => this.setState({ treeNodes: t }))
            .done();
    }

    handleNodeIconClick = (n: TreeNode) => {
        if (n.nodeState == "Collapsed" || n.nodeState == "Filtered") {
            API.getChildren(this.props.typeName, n.lite.id!.toString())
                .then(t => {
                    var oldNodes = n.loadedChildren.toObject(a => a.lite.id!.toString());
                    n.loadedChildren = t.map(n => oldNodes[n.lite.id!.toString()] || n);
                    n.nodeState = "Expanded";
                    this.forceUpdate();
                })
                .done();
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
            this.handleNavigate();
    }

    handleNavigate = () => {
        Navigator.navigate(this.state.selectedNode!.lite).done();
    }

    treeContainer: HTMLElement;

    render() {
        return (
            <div>
                {this.renderSearch()}
                <br />
                {this.renderToolbar()}
                <br />
                <div className="tree-container" ref={(t) => { this.treeContainer = t }} >
                    <ul>
                        {!this.state.treeNodes ? JavascriptMessage.loading.niceToString() :
                            this.state.treeNodes.map((t, i) =>
                                <TreeNodeControl
                                    selectedNode={this.state.selectedNode}
                                    onNodeIconClick={this.handleNodeIconClick}
                                    onNodeTextClick={this.handleNodeTextClick}
                                    onNodeTextDoubleClick={this.handleNodeTextDoubleClick}
                                    onNodeTextContextMenu={this.handleNodeTextContextMenu}
                                    key={i} treeNode={t} />)}
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
                position: ContextMenu.getPosition(e, this.treeContainer)
            }
        });
    }

    handleContextOnHide = () => {
        this.setState({ contextualMenu: undefined });
    }

    renderContextualMenu() {
        const cm = this.state.contextualMenu!;
        if (!this.state.selectedNode)
            return null;

        return (
            <ContextMenu position={cm.position} onHide={this.handleContextOnHide}>
                <MenuItem onClick={this.handleNavigate} bsClass="danger"><i className="fa fa-arrow-down" aria-hidden="true"></i>&nbsp; {TreeViewerMessage.View.niceToString()}</MenuItem>
                <MenuItem onClick={this.handleAddChildren}><i className="fa fa-arrow-right" aria-hidden="true"></i>&nbsp; {TreeViewerMessage.AddChild.niceToString()}</MenuItem>
                <MenuItem onClick={this.handleAddSibling}><i className="fa fa-arrow-down" aria-hidden="true"></i>&nbsp; {TreeViewerMessage.AddSibling.niceToString()}</MenuItem>
                <MenuItem onClick={this.handleRemove} bsClass="danger"><i className="fa fa-trash" aria-hidden="true"></i>&nbsp; {TreeViewerMessage.Remove.niceToString()}</MenuItem>
            </ContextMenu>
        );
    }

    handleSearchSubmit = (e: React.FormEvent<any>) => {
        e.preventDefault();
        e.stopPropagation();

        this.getFilterOptionsWithSFB().then(fos => {
            const filters = fos
                .filter(fo => fo.token != undefined && fo.operation != undefined)
                .map(fo => ({ token: fo.token!.fullKey, operation: fo.operation!, value: fo.value }) as FilterRequest);

            return API.findNodes(this.props.typeName, filters);
        })
            .then(nodes => this.setState({ treeNodes: nodes }))
            .done();
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
            .then(ep => Navigator.view(ep, { requiresSaveOperation: true }))
            .then(te => {
                if (!te)
                    return;
                this.state.treeNodes!.push(toTreeNode(te));
                this.forceUpdate();
            })
            .done();
    }

    handleAddChildren = () => {
        var parent = this.state.selectedNode!;
        Operations.API.constructFromLite(parent.lite, TreeOperation.CreateChild)
            .then(ep => Navigator.view(ep, { requiresSaveOperation: true }))
            .then(te => {
                if (!te)
                    return;
                var newNode = toTreeNode(te);
                parent.loadedChildren.push(newNode);
                parent.childrenCount++;
                fixState(parent);
                this.selectNode(newNode);
            })
            .done();
    }

    handleAddSibling = () => {

        var sibling = this.state.selectedNode!;

        Operations.API.constructFromLite(sibling.lite, TreeOperation.CreateNextSibling)
            .then(ep => Navigator.view(ep, { requiresSaveOperation: true }))
            .then(te => {
                if (!te)
                    return;
                const newNode = toTreeNode(te);
                const parent = this.findParent(sibling);
                const array = parent ? parent.loadedChildren : this.state.treeNodes!;
                array.insertAt(array.indexOf(sibling) + 1, newNode);
                this.selectNode(newNode);
            })
            .done();
    }

    findParent(childNode: TreeNode) {
        return this.state.treeNodes!.flatMap(allNodes).filter(n => n.loadedChildren.contains(childNode)).singleOrNull();
    }

    handleRemove = () => {

        var node = this.state.selectedNode!;
        if (!confirm(OperationMessage.PleaseConfirmYouDLikeToDelete0FromTheSystem.niceToString(node.lite.toStr)))
            return;

        Operations.API.deleteLite(node.lite, TreeOperation.Delete)
            .then(() => {
                var parent = this.findParent(node);
                (parent ? parent.loadedChildren : this.state.treeNodes!).remove(node);
                this.selectNode(parent || undefined);
            })
            .done();
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

        return Finder.parseFilterOptions(filters, qd).then(newFos => {
            this.setState({ filterOptions: newFos });

            return newFos;
        });
    }

    renderToolbar() {
        const s = this.state;

        return (
            <div className="btn-toolbar">
                <a className={"sf-query-button sf-filters-header btn btn-default" + (s.showFilters ? " active" : "")}
                    onClick={this.handleToggleFilters}
                    title={s.showFilters ? JavascriptMessage.hideFilters.niceToString() : JavascriptMessage.showFilters.niceToString()}><span className="glyphicon glyphicon glyphicon-filter"></span></a>
                <button className="btn btn-primary" onClick={this.handleSearchSubmit}><i className="glyphicon glyphicon-search"></i> &nbsp; {JavascriptMessage.search.niceToString()}</button>
                <div className="btn-group">
                    <button className="btn btn-default" onClick={this.handleAddRoot} disabled={s.treeNodes == null}><i className="fa fa-star" aria-hidden="true"></i>&nbsp; {TreeViewerMessage.AddRoot.niceToString()}</button>
                    <button className="btn btn-default" onClick={this.handleAddChildren} disabled={s.selectedNode == null}><i className="fa fa-arrow-right" aria-hidden="true"></i>&nbsp; {TreeViewerMessage.AddChild.niceToString()}</button>
                    <button className="btn btn-default" onClick={this.handleAddSibling} disabled={s.selectedNode == null}><i className="fa fa-arrow-down" aria-hidden="true"></i>&nbsp; {TreeViewerMessage.AddSibling.niceToString()}</button>
                </div>
                <button className="btn btn-info" onClick={this.handleNavigate} disabled={s.selectedNode == null}><i className="fa fa-arrow-down" aria-hidden="true"></i>&nbsp; {TreeViewerMessage.View.niceToString()}</button>
                <button className="btn btn-danger" onClick={this.handleRemove} disabled={s.selectedNode == null}><i className="fa fa-trash" aria-hidden="true"></i>&nbsp; {TreeViewerMessage.Remove.niceToString()}</button>
            </div>
        );
    }

    handleToggleFilters = () => {
    
        this.getFilterOptionsWithSFB().then(() => {
            this.simpleFilterBuilderInstance = undefined;
            this.setState({ simpleFilterBuilder: undefined, showFilters: !this.state.showFilters });
        }).done();
    }
}

function allNodes(node: TreeNode): TreeNode[] {
    return [node].concat(node.loadedChildren ? node.loadedChildren.flatMap(allNodes) : []);
}

function toTreeNode(treeEntity: TreeEntity): TreeNode {
    return {
        lite: toLite(treeEntity),
        childrenCount: 0,
        level: 0,
        loadedChildren: [],
        nodeState: "Leaf"
    };
}

interface TreeNodeControlProps {
    treeNode: TreeNode;
    selectedNode?: TreeNode;
    onNodeIconClick: (n: TreeNode, e: React.MouseEvent<HTMLSpanElement>) => void;
    onNodeTextClick: (n: TreeNode, e: React.MouseEvent<HTMLSpanElement>) => void;
    onNodeTextDoubleClick: (n: TreeNode, e: React.MouseEvent<HTMLSpanElement>) => void;
    onNodeTextContextMenu: (n: TreeNode, e: React.MouseEvent<HTMLSpanElement>) => void;
}

class TreeNodeControl extends React.Component<TreeNodeControlProps, void> {

    renderIcon(nodeState: TreeNodeState) {

        var node = this.props.treeNode;
        switch (nodeState) {
            case "Collapsed": return <span onClick={e => this.props.onNodeIconClick(node, e)} className="tree-icon fa fa-plus-square-o" />;
            case "Expanded": return <span onClick={e => this.props.onNodeIconClick(node, e)} className="tree-icon fa fa-minus-square-o" />;
            case "Filtered": return (
                <span onClick={e => this.props.onNodeIconClick(node, e)} className="tree-icon fa-stack fa-sm">
                    <i className="fa fa-square-o fa-stack-2x"></i>
                    <i className="fa fa-filter fa-stack-1x"></i>
                </span>);
            default: return <span className="place-holder" />;
        }
    }

    render(): React.ReactElement<any> {

        var node = this.props.treeNode;
        return (
            <li>
                {this.renderIcon(node.nodeState)}

                <span className={classes("tree-label", node == this.props.selectedNode && "tree-selected")}
                    onDoubleClick={e => this.props.onNodeTextDoubleClick(node, e)}
                    onClick={e => this.props.onNodeTextClick(node, e)}
                    onContextMenu={e => this.props.onNodeTextContextMenu(node, e)}
                >
                    {node.lite.toStr}
                </span>

                {node.loadedChildren.length > 0 && (node.nodeState == "Expanded" || node.nodeState == "Filtered") &&
                    <ul>
                        {node.loadedChildren.map((c, i) =>
                            <TreeNodeControl
                                selectedNode={this.props.selectedNode}
                                onNodeIconClick={this.props.onNodeIconClick}
                                onNodeTextClick={this.props.onNodeTextClick}
                                onNodeTextDoubleClick={this.props.onNodeTextDoubleClick}
                                onNodeTextContextMenu={this.props.onNodeTextContextMenu}
                                key={i} treeNode={c} />)}
                    </ul>
                }
            </li>
        );
    }

}
