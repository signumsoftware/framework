/// <reference path="../globals.d.ts" />

import * as React from 'react'
import { DropdownButton, MenuItem, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { Dic, DomUtils, classes } from '../Globals'
import * as Finder from '../Finder'
import { ResultTable, ResultRow, FindOptions, FilterOption, QueryDescription, ColumnOption, ColumnOptionsMode, ColumnDescription,
    toQueryToken, Pagination, PaginationMode, OrderType, OrderOption, SubTokensOptions, filterOperations, QueryToken, expandParentColumn, QueryRequest } from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey, Entity, is } from '../Signum.Entities'
import { getTypeInfos, IsByAll, getQueryKey, TypeInfo, EntityData, QueryKey, PseudoType} from '../Reflection'
import * as Navigator from '../Navigator'
import * as Constructor from '../Constructor'
import PaginationSelector from './PaginationSelector'
import FilterBuilder from './FilterBuilder'
import ColumnEditor from './ColumnEditor'
import MultipliedMessage from './MultipliedMessage'
import { renderContextualItems, ContextualItemsContext, MarkedRowsDictionary, MarkedRow } from './ContextualItems'
import ContextMenu from './ContextMenu'
import SelectorModal from '../SelectorModal'

require("!style!css!./Search.css");

export interface SimpleFilterBuilderProps {
    findOptions: FindOptions;
}


export interface SearchControlProps extends React.Props<SearchControl> {
    allowSelection?: boolean
    findOptions: FindOptions;
    onDoubleClick?: (e: React.MouseEvent, row: ResultRow) => void;
    showContextMenu?: boolean;
    onSelectionChanged?: (entity: Lite<Entity>[]) => void;
    onFiltersChanged?: (filters: FilterOption[]) => void;
    onResult?: (table: ResultTable) => void;
    hideExternalButton?: boolean;
}

export interface SearchControlState {
    resultTable?: ResultTable;
    findOptions?: FindOptions;
    simpleFilterBuilder?: React.ReactElement<any>;
    querySettings?: Finder.QuerySettings;
    queryDescription?: QueryDescription;
    loading?: boolean;
    selectedRows?: ResultRow[];
    markedRows?: MarkedRowsDictionary;
    searchCount?: number;
    dragColumnIndex?: number,
    dropBorderIndex?: number,

    currentMenuItems?: React.ReactElement<any>[];

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

    constructor(props: SearchControlProps) {
        super(props);
        this.state = this.initialState(props.findOptions.queryName);
    }



    initialState(queryName: PseudoType | QueryKey): SearchControlState {
        return {
            querySettings: Finder.getQuerySettings(queryName),
            loading: false,
            selectedRows: [],
        };
    }

    componentWillMount() {
        this.initialLoad(this.props.findOptions);
    }

    componentWillReceiveProps(newProps: SearchControlProps) {
        if (Finder.findOptionsPath(this.props.findOptions) == Finder.findOptionsPath(newProps.findOptions))
            return;

        this.state = this.initialState(newProps.findOptions.queryName);
        this.forceUpdate();

        this.initialLoad(newProps.findOptions);
    }

    stringify(findOptions: FindOptions) {
        return Finder.findOptionsPath((findOptions));
    }

    initialLoad(propsFindOptions: FindOptions) {

        Finder.getQueryDescription(propsFindOptions.queryName).then(qd => {

            this.setState({
                queryDescription: qd,
            });

            this.resetFindOptions(propsFindOptions);
        }).done();
    }

    resetFindOptions(propsFindOptions: FindOptions) {

        this.simpleFilterBuilderInstance = null;

        const qd = this.state.queryDescription;

        const ti = getTypeInfos(qd.columns["Entity"].type);

        const findOptions = Dic.extend({
            searchOnLoad: true,
            showHeader: true,
            showFilters: false,
            showFilterButton: true,
            showFooter: true,
            allowChangeColumn: true,
            create: ti.some(ti => Navigator.isCreable(ti, false, true)),
            navigate: ti.some(ti => Navigator.isNavigable(ti, null, true)),
            pagination: this.defaultPagination(),
            columnOptionsMode: "Add",
            columnOptions: [],
            orderOptions: [],
            filterOptions: []
        }, expandParentColumn(propsFindOptions));

        findOptions.columnOptions = Finder.mergeColumns(Dic.getValues(qd.columns), findOptions.columnOptionsMode, findOptions.columnOptions)
        if (!findOptions.orderOptions.length) {

            const defaultOrder = this.state.querySettings && this.state.querySettings.defaultOrderColumn || Finder.defaultOrderColumn;

            const info = this.entityColumnTypeInfos().firstOrNull()

            if (qd.columns[defaultOrder]) {
                findOptions.orderOptions = [{
                    columnName: defaultOrder,
                    orderType: info.entityData == EntityData.Transactional ? "Descending" as OrderType : "Ascending" as OrderType
                }];
            }
        }

        Finder.parseTokens(findOptions)
            .then(fo => {
                var qs = this.state.querySettings;

                var sfb = qs && qs.simpleFilterBuilder && qs.simpleFilterBuilder(qd, fo);

                if (sfb)
                    fo.showFilters = false;

                this.setState({
                    findOptions: fo,
                    simpleFilterBuilder: sfb,
                });

                if (this.state.findOptions.searchOnLoad)
                    this.doSearch();
            }).done();
    }

    defaultPagination() {
        return (this.state.querySettings && this.state.querySettings.pagination) || Finder.defaultPagination
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

    getQueryKey(): string {
        return getQueryKey(this.state.findOptions.queryName);
    }

    getQueryRequest(): QueryRequest {
        var fo = this.state.findOptions;

        return {
            queryKey: getQueryKey(fo.queryName),
            filters: fo.filterOptions.filter(a => a.token != null && a.operation != null).map(fo => ({ token: fo.token.fullKey, operation: fo.operation, value: fo.value })),
            columns: fo.columnOptions.filter(a => a.token != null).map(co => ({ token: co.token.fullKey, displayName: co.displayName }))
                .concat((this.state.querySettings && this.state.querySettings.hiddenColumns || []).map(co => ({ token: co.columnName, displayName: "" }))),
            orders: fo.orderOptions.filter(a => a.token != null).map(oo => ({ token: oo.token.fullKey, orderType: oo.orderType })),
            pagination: fo.pagination,
        };
    }

    // MAIN
    doSearchPage1() {
        var fo = this.state.findOptions;

        if (fo.pagination.mode == "Paginate")
            fo.pagination.currentPage = 1;

        this.doSearch();
    };

    doSearch() {
        this.getFindOptionsWithSFB().then(fo => {
            this.setState({ loading: false, editingColumn: null });
            Finder.API.search(this.getQueryRequest()).then(rt => {
                this.setState({
                    resultTable: rt,
                    selectedRows: [],
                    currentMenuItems: null,
                    markedRows: null,
                    loading: false,
                    searchCount: (this.state.searchCount || 0) + 1
                });
                if (this.props.onResult)
                    this.props.onResult(rt);

                this.notifySelectedRowsChanged();
                this.forceUpdate();
            }).done();
        });
    }

    handlePagination = (p: Pagination) => {
        this.state.findOptions.pagination = p;
        this.setState({ resultTable: null });

        if (this.state.findOptions.pagination.mode != "All")
            this.doSearch();
    }

    handleOnContextMenu = (event: React.MouseEvent) => {

        event.preventDefault();
        event.stopPropagation();

        const td = DomUtils.closest(event.target as HTMLElement, "td, th");
        const columnIndex = td.getAttribute("data-column-index") && parseInt(td.getAttribute("data-column-index"));


        const tr = td.parentNode as HTMLElement;
        const rowIndex = tr.getAttribute("data-row-index") && parseInt(tr.getAttribute("data-row-index"));


        const op = DomUtils.offsetParent(this.refs["container"] as HTMLElement);

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
                this.state.currentMenuItems = null;
            }

            if (this.state.currentMenuItems == null)
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

    simpleFilterBuilderInstance: ISimpleFilterBuilder;

    getFindOptionsWithSFB(): Promise<FindOptions> {

        var fo = this.state.findOptions;

        if (this.simpleFilterBuilderInstance == null)
            return Promise.resolve(fo);

        if (!this.simpleFilterBuilderInstance.getFilters)
            throw new Error("The simple filter builder should have a method with signature: 'getFilters(): FilterOption[]'");

        fo.filterOptions = this.simpleFilterBuilderInstance.getFilters();

        return Finder.parseTokens(fo);
    }

    handleFiltersChanged = () => {
        if (this.props.onFiltersChanged)
            this.props.onFiltersChanged(this.state.findOptions.filterOptions);
    }

    render() {

        const fo = this.state.findOptions;
        if (!fo)
            return null;

        var sfb = this.state.simpleFilterBuilder && React.cloneElement(this.state.simpleFilterBuilder, { ref: (e) => { this.simpleFilterBuilderInstance = e } });

        return (
            <div className="sf-search-control SF-control-container" ref="container" data-search-count={this.state.searchCount} data-query-key={getQueryKey(fo.queryName)}>
                {fo.showHeader && (fo.showFilters ? <FilterBuilder
                    queryDescription={this.state.queryDescription}
                    filterOptions={fo.filterOptions}
                    lastToken ={this.state.lastToken}
                    subTokensOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement}
                    onTokenChanged= {this.handleFilterTokenChanged}
                    onFiltersChanged={this.handleFiltersChanged}/> : (sfb && <div className="simple-filter-builder">{sfb}</div>)) }
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

        if (!this.state.simpleFilterBuilder)
            this.forceUpdate();

        this.getFindOptionsWithSFB().then(() => {
            this.simpleFilterBuilderInstance = null;
            this.setState({ simpleFilterBuilder: null });
        }).done();
    }

    handleSearchClick = (ev: React.MouseEvent) => {
        ev.preventDefault();

        this.doSearchPage1();

    };

    renderToolBar() {

        const fo = this.state.findOptions;
        return (
            <div className="sf-query-button-bar btn-toolbar">
                { fo.showFilterButton && <a
                    className={"sf-query-button sf-filters-header btn btn-default" + (fo.showFilters ? " active" : "") }
                    onClick={this.handleToggleFilters}
                    title={ fo.showFilters ? JavascriptMessage.hideFilters.niceToString() : JavascriptMessage.showFilters.niceToString() }><span className="glyphicon glyphicon glyphicon-filter"></span></a >}
                <button className={"sf-query-button sf-search btn btn-primary" + (this.state.loading ? " disabled" : "") } onClick={this.handleSearchClick}>{SearchMessage.Search.niceToString() } </button>
                {fo.create && <a className="sf-query-button btn btn-default sf-line-button sf-create" title={this.createTitle() } onClick={this.handleCreate}>
                    <span className="glyphicon glyphicon-plus"></span>
                </a>}
                {this.props.showContextMenu != false && this.renderSelecterButton() }
                {Finder.ButtonBarQuery.getButtonBarElements({ findOptions: fo, searchControl: this }).map((a, i) => React.cloneElement(a, { key: i })) }
                {!this.props.hideExternalButton &&
                    <a className="sf-query-button btn btn-default" href="#" onClick={this.handleFullScreenClick} >
                        <span className="glyphicon glyphicon-new-window"></span>
                    </a> }
            </div>
        );
    }


    chooseType(): Promise<string> {

        const tis = getTypeInfos(this.state.queryDescription.columns["Entity"].type)
            .filter(ti => Navigator.isCreable(ti, false, true));

        return SelectorModal.chooseType(tis)
            .then(ti => ti ? ti.name : null);
    }

    handleCreate = (ev: React.MouseEvent) => {

        if (!this.state.findOptions.create)
            return;

        var isPopup = ev.button == 2 || ev.ctrlKey;

        this.chooseType().then(tn => {
            if (tn == null)
                return;

            if (isPopup) {
                window.open(Navigator.createRoute(tn));
            } else {
                Constructor.construct(tn).then(e => {
                    if (e == null)
                        return;

                    Navigator.navigate(e);
                }).done();
            }
        }).done();
    }

    handleFullScreenClick = (ev: React.MouseEvent) => {

        ev.preventDefault();

        const fo = this.state.findOptions;


        const pair = Finder.smartColumns(fo.columnOptions, Dic.getValues(this.state.queryDescription.columns));

        const path = Finder.findOptionsPath({
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

        if (isOpen && this.state.currentMenuItems == null)
            this.loadMenuItems();
    }

    loadMenuItems() {
        const options: ContextualItemsContext<Entity> = {
            lites: this.state.selectedRows.map(a => a.entity),
            queryDescription: this.state.queryDescription,
            markRows: this.markRows
        };

        renderContextualItems(options)
            .then(menuItems => this.setState({ currentMenuItems: menuItems }))
            .done();
    }

    markRows = (dic: MarkedRowsDictionary) => {
        this.setState({ markedRows: Dic.extend(this.state.markedRows, dic) });
    }

    renderSelecterButton() {

        const title = JavascriptMessage.Selected.niceToString() + " (" + this.state.selectedRows.length + ")";

        return (
            <DropdownButton id="selectedButton" className="sf-query-button sf-tm-selected" title={title}
                onToggle={this.handleSelectedToggle}
                disabled={this.state.selectedRows.length == 0}>
                {this.state.currentMenuItems == null ? <MenuItem className="sf-tm-selected-loading">{JavascriptMessage.loading.niceToString() }</MenuItem> :
                    this.state.currentMenuItems.length == 0 ? <MenuItem className="sf-search-ctxitem-no-results">{JavascriptMessage.noActionsFound.niceToString() }</MenuItem> :
                        this.state.currentMenuItems.map((e, i) => React.cloneElement(e, { key: i })) }
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

        this.handleFiltersChanged();

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
        var s = this.state;
        const cm = s.contextualMenu;
        var col = s.findOptions.columnOptions[cm.columnIndex];
        s.findOptions.columnOptions.removeAt(cm.columnIndex);

        if (s.editingColumn == col)
            s.editingColumn = null;

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

        if (cm.rowIndex != null) {

            if (this.state.currentMenuItems == null) {
                menuItems.push(<MenuItem header>{JavascriptMessage.loading.niceToString() }</MenuItem>);
            } else {
                if (menuItems.length && this.state.currentMenuItems.length)
                    menuItems.push(<MenuItem divider/>);

                menuItems.splice(menuItems.length, 0, ...this.state.currentMenuItems);
            }
        }

        return (
            <ContextMenu position={cm.position} onHide={this.handleContextOnHide}>
                {menuItems.map((e, i) => React.cloneElement(e, { key: i })) }
            </ContextMenu>
        );
    }

    //SELECTED ROWS

    allSelected() {
        return this.state.resultTable != null && this.state.resultTable.rows.length != 0 && this.state.resultTable.rows.length == this.state.selectedRows.length;
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
            this.props.onSelectionChanged(this.state.selectedRows.map(a => a.entity));
    }


    handleHeaderClick = (e: React.MouseEvent) => {

        const token = (e.currentTarget as HTMLElement).getAttribute("data-column-name");
        var fo = this.state.findOptions;
        const prev = fo.orderOptions.filter(a => a.token.fullKey == token).firstOrNull();

        if (prev != null) {
            prev.orderType = prev.orderType == "Ascending" ? "Descending" : "Ascending";
            if (!e.shiftKey)
                fo.orderOptions = [prev];

        } else {

            const column = fo.columnOptions.filter(a => a.token.fullKey == token).first("Column");

            const newOrder: OrderOption = { token: column.token, orderType: "Ascending", columnName: column.token.fullKey };

            if (e.shiftKey)
                fo.orderOptions.push(newOrder);
            else
                fo.orderOptions = [newOrder];
        }


        if (fo.pagination.mode != "All")
            this.doSearchPage1();
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

        //console.log(JSON.stringify({
        //    dragIndex: this.state.dragColumnIndex,
        //    dropIndex: this.state.dropBorderIndex
        //}));

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
                { this.state.findOptions.navigate && <th className="sf-th-entity" data-column-name="Entity"></th> }
                { this.state.findOptions.columnOptions.map((co, i) =>
                    <th draggable={true}
                        style={i == this.state.dragColumnIndex ? { opacity: 0.5 } : null }
                        className={classes(co == this.state.editingColumn ? "sf-current-column" : null,
                            co == this.state.editingColumn && co.token && co.token.type.isCollection ? "error" : null,
                            i == this.state.dropBorderIndex ? "drag-left " : i == this.state.dropBorderIndex - 1 ? "drag-right " : null) }
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

        const o = orders.filter(a => a.token.fullKey == column.token.fullKey).firstOrNull();
        if (o == null)
            return "";


        let asc = (o.orderType == "Ascending" ? "asc" : "desc");

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

        this.state.currentMenuItems = null;

        this.notifySelectedRowsChanged();
        this.forceUpdate();
    }

    handleDoubleClick = (e: React.MouseEvent, row: ResultRow) => {

        if (this.props.onDoubleClick) {
            e.preventDefault();
            this.props.onDoubleClick(e, row);
            return;
        }

        if (!Navigator.isNavigable(row.entity.EntityType))
            return;

        e.preventDefault();

        var s = Navigator.getSettings(row.entity.EntityType)

        var avoidPopup = s != null && s.avoidPopup;

        if (avoidPopup || e.ctrlKey || e.button == 1) {
            window.open(Navigator.navigateRoute(row.entity));
        }
        else {
            Navigator.navigate(row.entity).done();
        }

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

        const columns = this.state.findOptions.columnOptions.map(co => ({
            columnOption: co,
            cellFormatter: co.token == null ? null : (qs && qs.formatters && qs.formatters[co.token.fullKey]) || Finder.formatRules.filter(a => a.isApplicable(co)).last("FormatRules").formatter(co),
            resultIndex: co.token == null ? null : this.state.resultTable.columns.indexOf(co.token.fullKey)
        }));


        const rowAttributes = qs && qs.rowAttributes;

        return this.state.resultTable.rows.map((row, i) => {
            
            const mark = this.getMarkedRow(row.entity);

            const tr = (
                <tr key={i} data-row-index={i} data-entity={liteKey(row.entity) }  onDoubleClick={e => this.handleDoubleClick(e, row) }
                    className={mark && mark.style} 
                    {...rowAttributes ? rowAttributes(row, this.state.resultTable.columns) : null}>                    
                    {this.props.allowSelection &&
                        <td style={{ textAlign: "center" }}>
                            <input type="checkbox" className="sf-td-selection" checked={this.state.selectedRows.contains(row) } onChange={this.handleChecked} data-index={i}/>
                        </td>
                    }

                    {this.state.findOptions.navigate &&
                        <td>
                            {this.wrapError(mark, i, ((qs && qs.entityFormatter) || Finder.entityFormatRules.filter(a => a.isApplicable(row)).last("EntityFormatRules").formatter)(row, this.state.resultTable.columns)) }
                        </td>
                    }

                    {columns.map((c, j) =>
                        <td key={j} data-column-index={j} style={{ textAlign: c.cellFormatter && c.cellFormatter.textAllign }}>
                            {c.resultIndex == -1 || c.cellFormatter == null ? null : c.cellFormatter.formatter(row.columns[c.resultIndex]) }
                        </td>) }
                </tr>
            );

            return tr;
        });
    }

    getMarkedRow(entity: Lite<Entity>): MarkedRow {

        if (!entity || !this.state.markedRows)
            return null; 

        var m = this.state.markedRows[liteKey(entity)];

        if (typeof m === "string") {
            if (m)
                return { style: "error", message: m };
            else
                return { style: "sf-entity-ctxmenu-success", message: null };
        }
        else {
            return m;
        }
    }

    wrapError(mark: MarkedRow, index: number, child: React.ReactChild) {
        if (!mark || mark.message == "")
            return child;

        const tooltip = <Tooltip id={"mark_" + index } >{mark.message}</Tooltip>;

        return <OverlayTrigger placement="bottom" overlay={tooltip}>{child}</OverlayTrigger>;
    }
}

export interface ISimpleFilterBuilder {
    getFilters(): FilterOption[];
}