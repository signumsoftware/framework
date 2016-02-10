/// <reference path="../globals.d.ts" />

import * as React from 'react'
import { DropdownButton, MenuItem } from 'react-bootstrap'
import { Dic, DomUtils } from '../Globals'
import * as Finder from '../Finder'
import { ResultTable, ResultRow, FindOptions, FilterOption, QueryDescription, ColumnOption, ColumnOptionsMode, ColumnDescription,
toQueryToken, Pagination, PaginationMode, OrderType, OrderOption, SubTokensOptions, filterOperations, QueryToken } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, IEntity, liteKey, is } from '../Signum.Entities'
import { getTypeInfos, IsByAll, getQueryKey, TypeInfo, EntityData} from '../Reflection'
import * as Navigator from '../Navigator'
import PaginationSelector from './PaginationSelector'
import FilterBuilder from './FilterBuilder'
import ColumnEditor from './ColumnEditor'
import MultipliedMessage from './MultipliedMessage'
import { getContextualItems, ContextMenu } from './ContextualItems'


require("!style!css!./Search.css");

export interface SimpleFilterBuilderProps {
    findOptions: FindOptions;
}

export interface ExternalFullScreenButton {
    onClick?: React.EventHandler<React.MouseEvent>;
}

export interface SearchControlProps extends React.Props<SearchControl> {
    allowSelection?: boolean
    findOptions: FindOptions;
    simpleFilterBuilder?: React.ComponentClass<SimpleFilterBuilderProps>;
    externalFullScreenButton?: ExternalFullScreenButton;
    showContextMenu?: boolean;
    onSelectionChanged?: (entity: Lite<IEntity>[]) => void
}

export interface SearchControlState {
    resultTable?: ResultTable;
    findOptions?: FindOptions;
    querySettings?: Finder.QuerySettings;
    queryDescription?: QueryDescription;
    loading?: boolean;
    selectedRows?: ResultRow[];
    usedRows?: ResultRow[];

    dragColumnIndex?: number,
    dropBorderIndex?: number,

    selectedMenuItems?: React.ReactElement<any>[];

    contextualMenu?: {
        position: { pageX: number, pageY: number };
        columnIndex: number;
        columnOffset?: number
        rowIndex?: number;
    };

    editingColumn?: ColumnOption;
    lastToken?: QueryToken;
}


export default class SearchControl extends React.Component<SearchControlProps, SearchControlState> {

    static defaultProps = {
        allowSelection: true,
        avoidFullScreenButton: false
    };

    // INIT

    constructor(props: SearchControlProps) {
        super(props);
        this.state = {
            resultTable: null,
            findOptions: null,
            querySettings: Finder.getQuerySettings(props.findOptions.queryName),
            queryDescription: null,
            loading: false,
            selectedRows: [],
            selectedMenuItems: null,
            usedRows: [],
        };

        if (props.externalFullScreenButton) {
            props.externalFullScreenButton.onClick = this.handleFullScreenClick;
        }

        this.initialLoad(this.props.findOptions);
    }

    componentWillReceiveProps(newProps: SearchControlProps) {
        if (JSON.stringify(this.props.findOptions) == JSON.stringify(newProps.findOptions))
            return;

        if (newProps.externalFullScreenButton) {
            newProps.externalFullScreenButton.onClick = this.handleFullScreenClick;
        }

        if (this.props.findOptions.queryName != newProps.findOptions.queryName)
            this.initialLoad(newProps.findOptions);
        else
            this.resetFindOptions(newProps.findOptions);
    }

    initialLoad(propsFindOptions: FindOptions) {

        

        Finder.API.getQueryDescription(propsFindOptions.queryName).then(qd=> {

            this.setState({
                queryDescription: qd,
            });

            this.resetFindOptions(propsFindOptions);
        });
    }

    resetFindOptions(propsFindOptions: FindOptions) {

        const qd = this.state.queryDescription;

        const ti = getTypeInfos(qd.columns["Entity"].type)

        const findOptions = Dic.extend({
            searchOnLoad: true,
            showHeader: true,
            showFilters: false,
            showFilterButton: true,
            showFooter: true,
            allowChangeColumn: true,
            create: ti.some(ti=> Navigator.isCreable(ti, true)),
            navigate: ti.some(ti=> Navigator.isNavigable(ti, null, true)),
            pagination: (this.state.querySettings && this.state.querySettings.pagination) || Finder.defaultPagination,
            columnOptionsMode: ColumnOptionsMode.Add,
            columnOptions: [],
            orderOptions: [],
            filterOptions: []
        }, propsFindOptions);

        findOptions.columnOptions = Finder.mergeColumns(Dic.getValues(qd.columns), findOptions.columnOptionsMode, findOptions.columnOptions)
        if (!findOptions.orderOptions.length) {

            const defaultOrder = this.state.querySettings && this.state.querySettings.defaultOrderColumn || Finder.defaultOrderColumn;

            const info = this.entityColumnTypeInfos().firstOrNull()

            findOptions.orderOptions = [{
                columnName: defaultOrder,
                orderType: info.entityData == EntityData.Transactional ? OrderType.Descending : OrderType.Ascending
            }];
        }

        Finder.parseTokens(findOptions).then(fo=> {
            this.setState({
                findOptions: fo,
            });

            if (this.state.findOptions.searchOnLoad)
                this.handleSearch();
        });
    }

    entityColumn(): ColumnDescription {
        return this.state.queryDescription.columns["Entity"];
    }

    entityColumnTypeInfos(): TypeInfo[] {
        return getTypeInfos(this.entityColumn().type);
    }

    canFilter() {
        const fo = this.state.findOptions;
        return fo.showHeader && (fo.showFilterButton || fo.showFilters)
    }


    
    // MAIN

    handleSearch = () => {
        const fo = this.state.findOptions;
        this.setState({ loading: false, editingColumn: null });
        Finder.API.search({
            queryKey: getQueryKey(fo.queryName),
            filters: fo.filterOptions.filter(a=> a.token != null && a.operation != null).map(fo=> ({ token: fo.token.fullKey, operation: fo.operation, value: fo.value })),
            columns: fo.columnOptions.filter(a=> a.token != null).map(co=> ({ token: co.token.fullKey, displayName: co.displayName })),
            orders: fo.orderOptions.filter(a=> a.token != null).map(oo=> ({ token: oo.token.fullKey, orderType: oo.orderType })),
            pagination: fo.pagination,
        }).then(rt=> {
            this.setState({ resultTable: rt, selectedRows: [], selectedMenuItems: null, usedRows: [], loading: false });
            this.notifySelectedRowsChanged();
            this.forceUpdate();
        });
    }

    handlePagination = (p: Pagination) => {
        this.state.findOptions.pagination = p;
        this.setState({ resultTable: null });

        if (this.state.findOptions.pagination.mode != PaginationMode.All)
            this.handleSearch();
    }

    handleOnContextMenu = (event: React.MouseEvent) => {

        event.preventDefault();
        event.stopPropagation();

        const td = DomUtils.closest(event.target as HTMLElement, "td, th");
        const columnIndex = td.getAttribute("data-column-index") && parseInt(td.getAttribute("data-column-index"));


        const tr = td.parentNode as HTMLElement;
        const rowIndex = tr.getAttribute("data-row-index") && parseInt(tr.getAttribute("data-row-index"));


        var op = DomUtils.offsetParent(this.refs["container"] as HTMLElement);

        this.state.contextualMenu = {
            position: {
                pageX: event.pageX - (op ? op.getBoundingClientRect().left : 0),
                pageY: event.pageY - (op ? op.getBoundingClientRect().top : 0)
            },
            columnIndex,
            rowIndex,
            columnOffset: td.tagName == "TH" ? this.getOffset(event.pageX, td.getBoundingClientRect(), Number.MAX_VALUE) : null
        };

        if (rowIndex != null) {
            const row = this.state.resultTable.rows[rowIndex];
            if (!this.state.selectedRows.contains(row)) {
                this.state.selectedRows = [row];
                this.state.selectedMenuItems = null;              
            }

            if (this.state.selectedMenuItems = null)
                this.loadMenuItems();
        }


        this.forceUpdate();
    }

    handleColumnChanged = (token: QueryToken) => {
        if (token)
            this.state.lastToken = token;

        this.forceUpdate();
    }

    handleColumnClose = () => {
        this.setState({ editingColumn: null });
    }

    handleFilterTokenChanged = (token: QueryToken) => {
        this.setState({ lastToken: token });
    }

    render() {

        const fo = this.state.findOptions;
        if (!fo)
            return null;

        const SFB = this.props.simpleFilterBuilder;

        return (
            <div className="sf-search-control SF-control-container" ref="container">
                {SFB && <div className="simple-filter-builder"><SFB findOptions={fo}/></div> }
                {fo.showHeader && fo.showFilters && <FilterBuilder
                    queryDescription={this.state.queryDescription}
                    filterOptions={fo.filterOptions}
                    lastToken ={this.state.lastToken}
                    subTokensOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement}
                    tokenChanged= {this.handleFilterTokenChanged}/> }
                {fo.showHeader && this.renderToolBar() }
                {<MultipliedMessage findOptions={fo} mainType={this.entityColumn().type}/>}
                {this.state.editingColumn && <ColumnEditor
                    columnOption={this.state.editingColumn}
                    onChange={this.handleColumnChanged}
                    queryDescription={this.state.queryDescription}
                    subTokensOptions={SubTokensOptions.CanElement}
                    close={this.handleColumnClose}/>}
                <div className="sf-search-results-container table-responsive" >
                    <table className="sf-search-results table table-hover table-condensed" onContextMenu={this.handleOnContextMenu} >
                        <thead>
                            {this.renderHeaders() }
                        </thead>
                        <tbody>
                            {this.renderRows() }
                        </tbody>
                    </table>
                </div>
                {fo.showFooter && <PaginationSelector pagination={fo.pagination} onPagination={this.handlePagination} resultTable={this.state.resultTable}/>}
                {this.state.contextualMenu && this.renderContextualMenu() }
            </div>
        );
    }

    // TOOLBAR
    handleToggleFilters = () => {
        this.state.findOptions.showFilters = !this.state.findOptions.showFilters;
        this.forceUpdate();
    }

    renderToolBar() {

        const fo = this.state.findOptions;
        return (
            <div className="sf-query-button-bar btn-toolbar">
                { fo.showFilterButton && <a
                    className={"sf-query-button sf-filters-header btn btn-default" + (fo.showFilters ? " active" : "") }
                    onClick={this.handleToggleFilters}
                    title={ fo.showFilters ? JavascriptMessage.hideFilters.niceToString() : JavascriptMessage.showFilters.niceToString() }><span className="glyphicon glyphicon glyphicon-filter"></span></a >}
                <button className={"sf-query-button sf-search btn btn-primary" + (this.state.loading ? " disabled" : "") } onClick={this.handleSearch}>{SearchMessage.Search.niceToString() } </button>
                {fo.create && <a className="sf-query-button btn btn-default sf-line-button sf-create" title={this.createTitle() }><span className="glyphicon glyphicon-plus"></span></a>}
                {this.props.showContextMenu != false && this.renderSelecterButton() }
                {Finder.ButtonBarQuery.getContextBarElements(fo.queryName) }
                {!this.props.externalFullScreenButton &&
                    <a className="sf-query-button btn btn-default" href="#" onClick={this.handleFullScreenClick} >
                        <span className="glyphicon glyphicon-new-window"></span>
                    </a> }
            </div>
        );
    }

    handleFullScreenClick = (ev: React.MouseEvent) => {

        ev.preventDefault();

        var fo = this.state.findOptions;


        var pair = Finder.smartColumns(fo.columnOptions, Dic.getValues(this.state.queryDescription.columns));

        var path = Finder.findOptionsPath({
            queryName: fo.queryName,
            filterOptions: fo.filterOptions,
            orderOptions: fo.orderOptions,
            columnOptions: pair.columns,
            columnOptionsMode: pair.mode,
        } as FindOptions);

        if (ev.ctrlKey || ev.button == 1)
            window.open(path);
        else
            Navigator.currentHistory.push(path);
    };

    createTitle() {

        const tis = this.entityColumnTypeInfos();

        const types = tis.map(ti => ti.niceName).join(", ");
        const gender = tis.first().gender;

        return SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(gender).formatWith(types);
    }

    // SELECT BUTTON

    handleSelectedToggle = (isOpen: boolean) => {

        if (isOpen && this.state.selectedMenuItems == null)
            this.loadMenuItems();
    }

    loadMenuItems() {
        getContextualItems({ lites: this.state.selectedRows.map(a=> a.entity), queryDescription: this.state.queryDescription })
            .then(menuItems=> this.setState({ selectedMenuItems: menuItems }));
    }

    renderSelecterButton() {

        const title = JavascriptMessage.Selected.niceToString() + " (" + this.state.selectedRows.length + ")";

        return (
            <DropdownButton id="selectedButton" className="sf-query-button sf-tm-selected" title={title}
                onToggle={this.handleSelectedToggle}
                disabled={this.state.selectedRows.length == 0}>
                {this.state.selectedMenuItems == null ? <MenuItem className="sf-tm-selected-loading">{JavascriptMessage.loading.niceToString() }</MenuItem> :
                    this.state.selectedMenuItems.length == 0 ? <MenuItem className="sf-search-ctxitem-no-results">{JavascriptMessage.noActionsFound.niceToString() }</MenuItem> :
                        this.state.selectedMenuItems.map((e, i) => React.cloneElement(e, { key: i })) }
            </DropdownButton>
        );
    }

    // CONTEXT MENU

    handleContextOnHide = () => {
        this.setState({ contextualMenu: null });
    }


    handleQuickFilter = () => {
        const cm = this.state.contextualMenu;
        const fo = this.state.findOptions;

        const token = fo.columnOptions[cm.columnIndex].token;

        const fops = filterOperations[token.filterType as any];

        const resultColumnIndex = this.state.resultTable.columns.indexOf(token.fullKey);

        fo.filterOptions.push({
            token: token,
            columnName: token.fullKey,
            operation: fops && fops.firstOrNull(),
            value: cm.rowIndex == null || resultColumnIndex == -1 ? null : this.state.resultTable.rows[cm.rowIndex].columns[resultColumnIndex]
        });

        if (!fo.showFilters)
            fo.showFilters = true;

        this.forceUpdate();
    }

    handleInsertColumn = () => {
      
        const newColumn: ColumnOption = {
            token: this.state.lastToken,
            displayName: this.state.lastToken && this.state.lastToken.niceName,
            columnName: null,
        };

        const cm = this.state.contextualMenu;
        this.setState({ editingColumn: newColumn });
        this.state.findOptions.columnOptions.insertAt(cm.columnIndex + cm.columnOffset, newColumn);

        this.forceUpdate();
    }

    handleEditColumn = () => {

        const cm = this.state.contextualMenu;
        this.setState({ editingColumn: this.state.findOptions.columnOptions[cm.columnIndex] });

        this.forceUpdate();
    }

    handleRemoveColumn = () => {

        const cm = this.state.contextualMenu;
        this.state.findOptions.columnOptions.removeAt(cm.columnIndex);

        this.forceUpdate();
    }

    renderContextualMenu() {

        const cm = this.state.contextualMenu;
        const fo = this.state.findOptions;

        const menuItems: React.ReactElement<any>[] = [];
        if (this.canFilter() && cm.columnIndex != null)
            menuItems.push(<MenuItem className="sf-quickfilter-header" onClick={this.handleQuickFilter}>{JavascriptMessage.addFilter.niceToString() }</MenuItem>);

        if (cm.rowIndex == null || fo.allowChangeColumns) {

            if (menuItems.length)
                menuItems.push(<MenuItem divider/>);
                        
            menuItems.push(<MenuItem className="sf-insert-header" onClick={this.handleInsertColumn}>{ JavascriptMessage.insertColumn.niceToString() }</MenuItem>);
            menuItems.push(<MenuItem className="sf-edit-header" onClick={this.handleEditColumn}>{JavascriptMessage.editColumn.niceToString() }</MenuItem>);
            menuItems.push(<MenuItem className="sf-remove-header" onClick={this.handleRemoveColumn}>{JavascriptMessage.removeColumn.niceToString() }</MenuItem>);
        }

        if (cm.rowIndex != null && this.state.selectedMenuItems) {

            if (menuItems.length && this.state.selectedMenuItems.length)
                menuItems.push(<MenuItem divider/>);

            menuItems.splice(menuItems.length, 0, ...this.state.selectedMenuItems);
        }
        
        return (
            <ContextMenu position={cm.position} onHide={this.handleContextOnHide}>
                {menuItems.map((e, i) => React.cloneElement(e, { key: i })) }
            </ContextMenu>
        );
    }

    //SELECTED ROWS

    allSelected() {
        return this.state.resultTable && this.state.resultTable.rows.length && this.state.resultTable.rows.length == this.state.selectedRows.length;
    }


    handleToggleAll = () => {

        if (!this.state.resultTable)
            return;

        this.setState({ selectedRows: !this.allSelected() ? this.state.resultTable.rows.clone() : [] });
        this.notifySelectedRowsChanged();
        this.forceUpdate();
    }



    notifySelectedRowsChanged() {
        if (this.props.onSelectionChanged)
            this.props.onSelectionChanged(this.state.selectedRows.map(a=> a.entity));
    }


    handleHeaderClick = (e: React.MouseEvent) => {

        const token = (e.currentTarget as HTMLElement).getAttribute("data-column-name")

        const prev = this.state.findOptions.orderOptions.filter(a=> a.token.fullKey == token).firstOrNull();

        if (prev != null) {
            prev.orderType = prev.orderType == OrderType.Ascending ? OrderType.Descending : OrderType.Ascending;
            if (!e.shiftKey)
                this.state.findOptions.orderOptions = [prev];

        } else {

            const column = this.state.findOptions.columnOptions.filter(a=> a.token.fullKey == token).first("Column");

            const newOrder: OrderOption = { token: column.token, orderType: OrderType.Ascending, columnName: column.token.fullKey };

            if (e.shiftKey)
                this.state.findOptions.orderOptions.push(newOrder);
            else
                this.state.findOptions.orderOptions = [newOrder];
        }

        //this.setState({ resultTable: null });

        if (this.state.findOptions.pagination.mode != PaginationMode.All)
            this.handleSearch();
    }

    //HEADER DRAG AND DROP

    handleHeaderDragStart = (de: React.DragEvent) => {
        de.dataTransfer.effectAllowed = "move";
        const dragIndex = parseInt((de.currentTarget as HTMLElement).getAttribute("data-column-index"));
        this.setState({ dragColumnIndex: dragIndex });
    }

    handleHeaderDragEnd = (de: React.DragEvent) => {
        this.setState({ dragColumnIndex: null, dropBorderIndex: null });
    }


    getOffset(pageX: number, rect: ClientRect, margin: number) {

        if (margin > rect.width / 2)
            margin = rect.width / 2;

        const width = rect.width;
        const offsetX = pageX - rect.left;

        if (offsetX < margin)
            return 0;

        if (offsetX > (width - margin))
            return 1;

        return null;
    }

    handlerHeaderDragOver = (de: React.DragEvent) => {
        de.preventDefault();

        const th = de.currentTarget as HTMLElement;

        const size = th.scrollWidth;

        const columnIndex = parseInt(th.getAttribute("data-column-index"));

        const offset = this.getOffset((de.nativeEvent as DragEvent).pageX, th.getBoundingClientRect(), 50);

        let dropBorderIndex = offset == null ? null : columnIndex + offset;

        if (dropBorderIndex == this.state.dragColumnIndex || dropBorderIndex == this.state.dragColumnIndex + 1)
            dropBorderIndex = null;

        de.dataTransfer.dropEffect = dropBorderIndex == null ? "none" : "move";

        if (this.state.dropBorderIndex != dropBorderIndex)
            this.setState({ dropBorderIndex: dropBorderIndex });
    }

    handleHeaderDrop = (de: React.DragEvent) => {

        console.log(JSON.stringify({
            dragIndex: this.state.dragColumnIndex,
            dropIndex: this.state.dropBorderIndex
        }));

        const columns = this.state.findOptions.columnOptions;
        const temp = columns[this.state.dragColumnIndex];
        columns.removeAt(this.state.dragColumnIndex);
        const rebasedDropIndex = this.state.dropBorderIndex > this.state.dragColumnIndex ?
            this.state.dropBorderIndex - 1 :
            this.state.dropBorderIndex;
        columns.insertAt(rebasedDropIndex, temp);

        this.setState({
            dropBorderIndex: null,
            dragColumnIndex: null
        });
    }


    renderHeaders(): React.ReactNode {

        return (
            <tr>
                { this.props.allowSelection && <th className="sf-th-selection">
                    <input type="checkbox" id="cbSelectAll" onClick={this.handleToggleAll} checked={this.allSelected() }/>
                </th>
                }
                { this.state.findOptions.navigate && <th className="sf-th-entity"></th> }
                { this.state.findOptions.columnOptions.map((co, i) =>
                    <th draggable={true}
                        style={i == this.state.dragColumnIndex ? { opacity: 0.5 } : null }
                        className={(i == this.state.dropBorderIndex ? "drag-left " : i == this.state.dropBorderIndex - 1 ? "drag-right " : "") }
                        data-column-name={co.token && co.token.fullKey}
                        data-column-index={i}
                        key={i}
                        onClick={this.handleHeaderClick}
                        onDragStart={this.handleHeaderDragStart}
                        onDragEnd={this.handleHeaderDragEnd}
                        onDragOver={this.handlerHeaderDragOver}
                        onDragEnter={this.handlerHeaderDragOver}
                        onDrop={this.handleHeaderDrop}>
                        <span className={"sf-header-sort " + this.orderClassName(co) }/>
                        <span> { co.displayName }</span></th>
                ) }
            </tr>
        );
    }

    orderClassName(column: ColumnOption) {

        if (column.token == null)
            return "";

        const orders = this.state.findOptions.orderOptions;

        const o = orders.filter(a=> a.token.fullKey == column.token.fullKey).firstOrNull();
        if (o == null)
            return "";


        let asc = (o.orderType == OrderType.Ascending ? "asc" : "desc");

        if (orders.indexOf(o))
            asc += " l" + orders.indexOf(o);

        return asc;
    }

    //ROWS

    handleChecked = (event: React.MouseEvent) => {

        const cb = (event.currentTarget) as HTMLInputElement;

        const index = parseInt(cb.getAttribute("data-index"));

        const row = this.state.resultTable.rows[index];


        if (cb.checked) {
            if (!this.state.selectedRows.contains(row))
                this.state.selectedRows.push(row);
        } else {
            this.state.selectedRows.remove(row);
        }

        this.state.selectedMenuItems = null;

        this.notifySelectedRowsChanged();
        this.forceUpdate();
    }
    
    renderRows(): React.ReactNode {

        const columnsCount = this.state.findOptions.columnOptions.length +
            (this.props.allowSelection ? 1 : 0) +
            (this.state.findOptions.navigate ? 1 : 0);

        if (!this.state.resultTable) {
            return <tr><td colSpan={columnsCount}>{JavascriptMessage.searchForResults.niceToString() }</td></tr>;
        }

        if (this.state.resultTable.rows.length == 0) {
            return <tr><td colSpan={columnsCount}>{ SearchMessage.NoResultsFound.niceToString() }</td></tr>;
        }

        const qs = this.state.querySettings;
        
        const columns = this.state.findOptions.columnOptions.map(co=> ({
            columnOption: co,
            cellFormatter: co.token == null ? null : (qs && qs.formatters && qs.formatters[co.token.fullKey]) || Finder.formatRules.filter(a=> a.isApplicable(co)).last("FormatRules").formatter(co),
            resultIndex: co.token == null ? null : this.state.resultTable.columns.indexOf(co.token.fullKey)
        }));
        

        const rowAttributes = qs && qs.rowAttributes;

        return this.state.resultTable.rows.map((row, i) =>
            <tr key={i} data-row-index={i} data-entity={liteKey(row.entity) } {...rowAttributes ? rowAttributes(row, this.state.resultTable.columns) : null}
                style={{ opacity: this.state.usedRows.some(s => row === s) ? 0.5 : 1 }} >

                {this.props.allowSelection &&
                    <td style={{ textAlign: "center" }}>
                        <input type="checkbox" className="sf-td-selection" checked={this.state.selectedRows.contains(row) } onChange={this.handleChecked} data-index={i}/>
                    </td>
                }

                {this.state.findOptions.navigate &&
                    <td>
                        {((qs && qs.entityFormatter) || Finder.entityFormatRules.filter(a => a.isApplicable(row)).last("EntityFormatRules").formatter)(row) }
                    </td>
                }

                {columns.map((c, j) =>
                    <td key={j} data-column-index={j} style={{ textAlign: c.cellFormatter && c.cellFormatter.textAllign }}>
                        {c.resultIndex == -1 || c.cellFormatter == null ? null : c.cellFormatter.formatter(row.columns[c.resultIndex]) }
                    </td>) }
            </tr>);
    }

}