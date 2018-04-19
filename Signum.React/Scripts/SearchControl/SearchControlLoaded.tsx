import * as React from 'react'
import * as moment from 'moment'
import { Dropdown, DropdownItem, UncontrolledTooltip, DropdownMenu, DropdownToggle } from '../Components'
import { Dic, DomUtils, classes } from '../Globals'
import * as Finder from '../Finder'
import { CellFormatter, EntityFormatter } from '../Finder'
import * as OrderUtils from '../Frames/OrderUtils'
import {
    ResultTable, ResultRow, FindOptionsParsed, FindOptions, FilterOption, FilterOptionParsed, QueryDescription, ColumnOption, ColumnOptionParsed, ColumnOptionsMode, ColumnDescription,
    toQueryToken, Pagination, PaginationMode, OrderType, OrderOption, OrderOptionParsed, SubTokensOptions, filterOperations, QueryToken, QueryRequest
} from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey, Entity, is, isEntity, isLite, toLite, ModifiableEntity } from '../Signum.Entities'
import { getTypeInfos, getTypeInfo, TypeReference, IsByAll, getQueryKey, TypeInfo, EntityData, QueryKey, PseudoType, isTypeModel } from '../Reflection'
import * as Navigator from '../Navigator'
import { AbortableRequest } from '../Services'
import * as Constructor from '../Constructor'
import PaginationSelector from './PaginationSelector'
import FilterBuilder from './FilterBuilder'
import ColumnEditor from './ColumnEditor'
import MultipliedMessage from './MultipliedMessage'
import GroupByMessage from './GroupByMessage'
import { renderContextualItems, ContextualItemsContext, MarkedRowsDictionary, MarkedRow } from './ContextualItems'
import ContextMenu from './ContextMenu'
import { ContextMenuPosition } from './ContextMenu'
import SelectorModal from '../SelectorModal'
import { ISimpleFilterBuilder } from './SearchControl'

import "./Search.css"
import { FilterOperation } from '../Signum.Entities.DynamicQuery';
import SystemTimeEditor from './SystemTimeEditor';
import { MaxHeightProperty } from 'csstype';

export interface ShowBarExtensionOption { }

export interface SearchControlLoadedProps {
    findOptions: FindOptionsParsed;
    queryDescription: QueryDescription;
    querySettings: Finder.QuerySettings;

    formatters?: { [columnName: string]: CellFormatter };
    rowAttributes?: (row: ResultRow, columns: string[]) => React.HTMLAttributes<HTMLTableRowElement> | undefined;
    entityFormatter?: EntityFormatter;
    extraButtons?: (searchControl: SearchControlLoaded) => (React.ReactElement<any> | null | undefined | false)[];
    getViewPromise?: (e: ModifiableEntity) => undefined | string | Navigator.ViewPromise<any>;
    maxResultsHeight?: MaxHeightProperty<string | number> | any;
    tag?: string | {};

    searchOnLoad: boolean;
    allowSelection: boolean;
    showContextMenu: boolean | "Basic";
    hideButtonBar: boolean;
    hideFullScreenButton: boolean;
    showHeader: boolean;
    showBarExtension: boolean;
    showBarExtensionOption?: ShowBarExtensionOption;
    showFilters: boolean;
    showSimpleFilterBuilder: boolean;
    showFilterButton: boolean;
    showSystemTimeButton: boolean;
    showGroupButton: boolean;
    showFooter: boolean;
    allowChangeColumns: boolean;
    allowChangeOrder: boolean;
    create: boolean;
    navigate: boolean;
    largeToolbarButtons: boolean;
    avoidAutoRefresh: boolean;
    avoidChangeUrl: boolean;
    refreshKey: string | number | undefined;

    simpleFilterBuilder?: (qd: QueryDescription, initialFilterOptions: FilterOptionParsed[]) => React.ReactElement<any> | undefined;
    onCreate?: () => void;
    onDoubleClick?: (e: React.MouseEvent<any>, row: ResultRow) => void;
    onNavigated?: (lite: Lite<Entity>) => void;
    onSelectionChanged?: (rows: ResultRow[]) => void;
    onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
    onHeighChanged?: () => void;
    onSearch?: (fo: FindOptionsParsed, dataChange: boolean) => void;
    onResult?: (table: ResultTable, dataChange: boolean) => void;
}

export interface SearchControlLoadedState {
    resultTable?: ResultTable;
    simpleFilterBuilder?: React.ReactElement<any>;
    selectedRows?: ResultRow[];
    markedRows?: MarkedRowsDictionary;
    isSelectOpen: boolean;
    resultFindOptions?: FindOptionsParsed;
    searchCount?: number;
    dragColumnIndex?: number,
    dropBorderIndex?: number,

    currentMenuItems?: React.ReactElement<any>[];

    contextualMenu?: {
        position: ContextMenuPosition;
        columnIndex: number | null;
        columnOffset?: number;
        rowIndex: number | null;
    };

    showFilters: boolean;
    editingColumn?: ColumnOptionParsed;
    lastToken?: QueryToken;
}


export default class SearchControlLoaded extends React.Component<SearchControlLoadedProps, SearchControlLoadedState>{

    constructor(props: SearchControlLoadedProps) {
        super(props);
        this.state = {
            isSelectOpen: false,
            showFilters: props.showFilters
        };
    }

    componentWillMount() {

        const fo = this.props.findOptions;
        const qs = Finder.getSettings(fo.queryKey);
        const qd = this.props.queryDescription;

        const sfb = this.props.showSimpleFilterBuilder == false || fo.groupResults ? undefined :
            this.props.simpleFilterBuilder ? this.props.simpleFilterBuilder(qd, fo.filterOptions) :
                qs && qs.simpleFilterBuilder ? qs.simpleFilterBuilder(qd, fo.filterOptions) :
                    undefined;

        if (sfb) {
            this.setState({
                showFilters: false,
                simpleFilterBuilder: sfb
            });
        }

        if (this.props.searchOnLoad)
            this.doSearch().done();
    }

    componentWillReceiveProps(props: SearchControlLoadedProps) {
        if (this.props.refreshKey != props.refreshKey) {
            this.doSearchPage1();
        }
    }

    isUnmounted = false;
    componentWillUnmount() {
        this.isUnmounted = true;
        this.abortableSearch.abort();
    }


    entityColumn(): ColumnDescription {
        return this.props.queryDescription.columns["Entity"];
    }

    entityColumnTypeInfos(): TypeInfo[] {
        return getTypeInfos(this.entityColumn().type);
    }

    canFilter() {
        const p = this.props;
        return p.showHeader && (p.showFilterButton || p.showFilters)
    }


    getQueryRequest(): QueryRequest {
        const fo = this.props.findOptions;
        const qs = this.props.querySettings;

        return {
            queryKey: fo.queryKey,
            groupResults: fo.groupResults,
            filters: fo.filterOptions.filter(a => a.token != undefined && a.token.filterType != undefined && a.operation != undefined).map(fo => ({ token: fo.token!.fullKey, operation: fo.operation!, value: fo.value })),
            columns: fo.columnOptions.filter(a => a.token != undefined).map(co => ({ token: co.token!.fullKey, displayName: co.displayName! }))
                .concat((!fo.groupResults && qs && qs.hiddenColumns || []).map(co => ({ token: co.columnName, displayName: "" }))),
            orders: fo.orderOptions.filter(a => a.token != undefined).map(oo => ({ token: oo.token.fullKey, orderType: oo.orderType })),
            pagination: fo.pagination,
            systemTime: fo.systemTime,
        };
    }

    // MAIN
    doSearchPage1() {
        const fo = this.props.findOptions;

        if (fo.pagination.mode == "Paginate")
            fo.pagination.currentPage = 1;

        this.doSearch().done();
    };

    resetResults(continuation: () => void) {
        this.setState({
            resultTable: undefined,
            resultFindOptions: undefined,
            selectedRows: [],
            currentMenuItems: undefined,
            markedRows: undefined,
        }, continuation);
    }

    abortableSearch = new AbortableRequest((abortController, request: QueryRequest) => Finder.API.executeQuery(request, abortController));

    doSearch(dataChanged?: boolean): Promise<void> {

        if (this.isUnmounted)
            return Promise.resolve();

        return this.getFindOptionsWithSFB().then(fop => {
            if (this.props.onSearch)
                this.props.onSearch(fop, dataChanged || false);

            this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());
            var resultFindOptions = JSON.parse(JSON.stringify(this.props.findOptions));
            return this.abortableSearch.getData(this.getQueryRequest()).then(rt => {
                this.setState({
                    resultTable: rt,
                    resultFindOptions: resultFindOptions,
                    selectedRows: [],
                    currentMenuItems: undefined,
                    markedRows: undefined,
                    searchCount: (this.state.searchCount || 0) + 1
                }, () => {
                    if (this.props.onResult)
                        this.props.onResult(rt, dataChanged || false);
                    this.notifySelectedRowsChanged();
                });
            });
        });
    }

    notifySelectedRowsChanged() {
        if (this.props.onSelectionChanged)
            this.props.onSelectionChanged(this.state.selectedRows!);
    }


    simpleFilterBuilderInstance?: ISimpleFilterBuilder;

    getFindOptionsWithSFB(): Promise<FindOptionsParsed> {

        const fo = this.props.findOptions;
        const qd = this.props.queryDescription;

        if (this.simpleFilterBuilderInstance == undefined)
            return Promise.resolve(fo);

        if (!this.simpleFilterBuilderInstance.getFilters)
            throw new Error("The simple filter builder should have a method with signature: 'getFilters(): FilterOption[]'");

        var filters = this.simpleFilterBuilderInstance.getFilters();

        return Finder.parseFilterOptions(filters, false, qd).then(fos => {
            fo.filterOptions = fos;

            return fo;
        });
    }


    handlePagination = (p: Pagination) => {
        this.props.findOptions.pagination = p;
        this.setState({ resultTable: undefined, resultFindOptions: undefined });

        if (this.props.findOptions.pagination.mode != "All")
            this.doSearch().done();
    }


    handleOnContextMenu = (event: React.MouseEvent<any>) => {

        event.preventDefault();
        event.stopPropagation();

        const td = DomUtils.closest(event.target as HTMLElement, "td, th")!;
        const columnIndex = td.getAttribute("data-column-index") ? parseInt(td.getAttribute("data-column-index")!) : null;


        const tr = td.parentNode as HTMLElement;
        const rowIndex = tr.getAttribute("data-row-index") ? parseInt(tr.getAttribute("data-row-index")!) : null;

        this.setState({
            contextualMenu: {
                position: ContextMenu.getPosition(event, this.refs["container"] as HTMLElement),
                columnIndex,
                rowIndex,
                columnOffset: td.tagName == "TH" ? this.getOffset(event.pageX, td.getBoundingClientRect(), Number.MAX_VALUE) : undefined
            }
        });

        if (rowIndex != undefined) {
            const row = this.state.resultTable!.rows[rowIndex];
            if (!this.state.selectedRows!.contains(row)) {
                this.setState({
                    selectedRows: [row],
                    currentMenuItems: undefined
                }, () => {
                    this.loadMenuItems();
                    this.notifySelectedRowsChanged();
                });
            }

            if (this.state.currentMenuItems == undefined)
                this.loadMenuItems();
        }
    }


    handleColumnChanged = (token: QueryToken | undefined) => {
        this.setState({ lastToken: token });
    }

    handleColumnClose = () => {
        this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());
    }

    handleFilterTokenChanged = (token: QueryToken | undefined) => {
        this.setState({ lastToken: token });
    }


    handleFiltersChanged = () => {
        if (this.props.onFiltersChanged)
            this.props.onFiltersChanged(this.props.findOptions.filterOptions);

    }

    handleHeightChanged = () => {
        if (this.props.onHeighChanged)
            this.props.onHeighChanged();
    }

    handleFiltersKeyUp = (e: React.KeyboardEvent<HTMLDivElement>) => {
        if (e.keyCode == 13) {
            setTimeout(() => {
                var input = (document.activeElement as HTMLInputElement);
                input.blur();
                this.doSearchPage1();
            }, 200);
        }
    }

    componentDidMount() {
        this.containerDiv!.addEventListener("scroll", (e) => {
            var translate = "translate(0," + this.containerDiv!.scrollTop + "px)";
            this.thead!.style.transform = translate;
        });
    }


    containerDiv?: HTMLDivElement | null;
    thead?: HTMLTableSectionElement | null;

    render() {
        const p = this.props;
        const fo = this.props.findOptions;
        const qd = this.props.queryDescription;

        const sfb = this.state.simpleFilterBuilder &&
            React.cloneElement(this.state.simpleFilterBuilder, { ref: (e: ISimpleFilterBuilder) => { this.simpleFilterBuilderInstance = e } });

        const canAggregate = (fo.groupResults ? SubTokensOptions.CanAggregate : 0);

        return (
            <div className="sf-search-control SF-control-container" ref="container"
                data-search-count={this.state.searchCount}
                data-query-key={fo.queryKey}>
                {p.showHeader &&
                    <div onKeyUp={this.handleFiltersKeyUp}>
                        {
                            this.state.showFilters ? <FilterBuilder
                                queryDescription={qd}
                                filterOptions={fo.filterOptions}
                                lastToken={this.state.lastToken}
                                subTokensOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate}
                                onTokenChanged={this.handleFilterTokenChanged}
                                onFiltersChanged={this.handleFiltersChanged}
                                onHeightChanged={this.handleHeightChanged}
                            /> :
                                sfb && <div className="simple-filter-builder">{sfb}</div>
                        }
                    </div>
                }
                {p.showHeader && this.renderToolBar()}
                {<MultipliedMessage findOptions={fo} mainType={this.entityColumn().type} />}
                {fo.groupResults && <GroupByMessage findOptions={fo} mainType={this.entityColumn().type} />}
                {fo.systemTime && <SystemTimeEditor findOptions={fo} queryDescription={qd} onChanged={() => this.forceUpdate()} />}
                {this.state.editingColumn && <ColumnEditor
                    columnOption={this.state.editingColumn}
                    onChange={this.handleColumnChanged}
                    queryDescription={qd}
                    subTokensOptions={SubTokensOptions.CanElement | canAggregate}
                    close={this.handleColumnClose} />}
                <div ref={d => this.containerDiv = d}
                    className="sf-search-results-container table-responsive"
                    style={{ maxHeight: this.props.maxResultsHeight }}>
                    <table className="sf-search-results table table-hover table-sm" onContextMenu={this.props.showContextMenu != false ? this.handleOnContextMenu : undefined} >
                        <thead ref={th => this.thead = th}>
                            {this.renderHeaders()}
                        </thead>
                        <tbody>
                            {this.renderRows()}
                        </tbody>
                    </table>
                </div>
                {p.showFooter && <PaginationSelector pagination={fo.pagination} onPagination={this.handlePagination} resultTable={this.state.resultTable} />}
                {this.state.contextualMenu && this.renderContextualMenu()}
            </div>
        );
    }


    // TOOLBAR


    handleSearchClick = (ev: React.MouseEvent<any>) => {
        ev.preventDefault();

        this.doSearchPage1();

    };

    handleToggleFilters = () => {
        this.getFindOptionsWithSFB().then(() => {
            this.simpleFilterBuilderInstance = undefined;
            this.setState({
                simpleFilterBuilder: undefined,
                showFilters: !this.state.showFilters
            }, () => this.handleHeightChanged());
        }).done();
    }

    handleSystemTimeClick = () => {
        var fo = this.props.findOptions;

        if (fo.systemTime == null)
            fo.systemTime = { mode: "AsOf", startDate: moment().format() };
        else
            fo.systemTime = undefined;

        this.forceUpdate();
    }

    handleToggleGroupBy = () => {
        var fo = this.props.findOptions;
        var qd = this.props.queryDescription;
        this.resetResults(() => {
            if (fo.groupResults) {

                fo.groupResults = false;
                removeAggregates(fo.filterOptions, qd);
                removeAggregates(fo.orderOptions, qd);
                removeAggregates(fo.columnOptions, qd);
                this.forceUpdate();

                this.doSearchPage1();

            } else {
                fo.groupResults = true;
                if (this.state.simpleFilterBuilder) {
                    this.simpleFilterBuilderInstance = undefined;
                    this.setState({ simpleFilterBuilder: undefined, showFilters: true });
                }

                var tc = new Finder.TokenCompleter(qd);
                //addAggregates(fo.filterOptions, qd, "request");
                withAggregates(fo.orderOptions, tc, "request");
                withAggregates(fo.columnOptions, tc, "request");

                tc.finished().then(() => {
                    //addAggregates(fo.filterOptions, qd, "get");
                    withAggregates(fo.orderOptions, tc, "get");
                    withAggregates(fo.columnOptions, tc, "get");
                    this.forceUpdate();
                    this.doSearchPage1();
                });
            }
        });
    }

    renderToolBar() {

        const p = this.props;
        const s = this.state;

        var buttons = [

            p.showFilterButton && OrderUtils.setOrder(-5, <button
                className={"sf-query-button sf-filters-header btn btn-light" + (s.showFilters ? " active" : "")}
                onClick={this.handleToggleFilters}
                title={s.showFilters ? JavascriptMessage.hideFilters.niceToString() : JavascriptMessage.showFilters.niceToString()}>
                <span className="fa fa-filter"/>
            </button>),

            p.showGroupButton && OrderUtils.setOrder(-4, <button
                className={"sf-query-button btn " + (p.findOptions.groupResults ? "alert-info" : "btn-light")}
                onClick={this.handleToggleGroupBy}
                title={p.findOptions.groupResults ? JavascriptMessage.ungroupResults.niceToString() : JavascriptMessage.groupResults.niceToString()}>
                Ʃ
            </button>),

            p.showSystemTimeButton && OrderUtils.setOrder(-3.5, <button
                className={"sf-query-button btn " + (p.findOptions.systemTime ? "alert-primary" : "btn-light")}
                onClick={this.handleSystemTimeClick}
                title={p.findOptions.systemTime ? JavascriptMessage.deactivateTimeMachine.niceToString() : JavascriptMessage.activateTimeMachine.niceToString()}>
                <i className="fa fa-history" />
            </button>),

            OrderUtils.setOrder(-3, <button className={classes("sf-query-button sf-search btn", p.findOptions.pagination.mode == "All" ? "btn-danger" : "btn-primary")} onClick={this.handleSearchClick}>{SearchMessage.Search.niceToString()} </button>),

            p.create && OrderUtils.setOrder(-2, <button className="sf-query-button btn btn-light sf-search-button sf-create" title={this.createTitle()} onClick={this.handleCreate}>
                <span className="fa fa-plus sf-create" />
            </button>),

            this.props.showContextMenu != false && this.renderSelectedButton(),

            ...(this.props.hideButtonBar ? [] : Finder.ButtonBarQuery.getButtonBarElements({ findOptions: p.findOptions, searchControl: this })),

            ...(this.props.extraButtons ? this.props.extraButtons(this) : []),

            !this.props.hideFullScreenButton && Finder.isFindable(p.findOptions.queryKey, true) &&
            <button className="sf-query-button btn btn-light" onClick={this.handleFullScreenClick} >
                <span className="fa fa-external-link" />
            </button>
        ]
            .filter(a => a)
            .map(a => a as React.ReactElement<any>)
            .orderBy(a => OrderUtils.getOrder(a))
            .map(a => OrderUtils.cloneElementWithoutOrder(a!));

        return React.cloneElement(<div className={classes("sf-query-button-bar btn-toolbar", !this.props.largeToolbarButtons && "btn-toolbar-small")} />, undefined, ...buttons);
    }


    chooseType(): Promise<string | undefined> {

        const tis = getTypeInfos(this.props.queryDescription.columns["Entity"].type)
            .filter(ti => Navigator.isCreable(ti, false, true));

        return SelectorModal.chooseType(tis)
            .then(ti => ti ? ti.name : undefined);
    }

    handleCreate = (ev: React.MouseEvent<any>) => {

        if (!this.props.create)
            return;

        const onCreate = this.props.onCreate;

        if (onCreate)
            onCreate();
        else {
            const isWindowsOpen = ev.button == 1 || ev.ctrlKey;

            this.chooseType().then(tn => {
                if (tn == undefined)
                    return;

                var s = Navigator.getSettings(tn);

                if (isWindowsOpen || (s != null && s.avoidPopup)) {
                    window.open(Navigator.createRoute(tn));
                } else {
                    Constructor.construct(tn).then(e => {
                        if (e == undefined)
                            return;

                        Finder.setFilters(e.entity as Entity, this.props.findOptions.filterOptions)
                            .then(() => Navigator.navigate(e!, { getViewPromise: this.props.getViewPromise }))
                            .then(() => this.props.avoidAutoRefresh ? undefined : this.doSearch(true))
                            .done();
                    }).done();
                }
            }).done();
        }
    }

    handleFullScreenClick = (ev: React.MouseEvent<any>) => {

        ev.preventDefault();

        var findOptions = Finder.toFindOptions(this.props.findOptions, this.props.queryDescription);

        const path = Finder.findOptionsPath(findOptions);

        if (ev.ctrlKey || ev.button == 1 || this.props.avoidChangeUrl)
            window.open(path);
        else
            Navigator.history.push(path);
    };

    createTitle() {

        const tis = this.entityColumnTypeInfos();

        const types = tis.map(ti => ti.niceName).join(", ");
        const gender = tis.first().gender;

        return SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(gender).formatWith(types);
    }

    getSelectedEntities(): Lite<Entity>[] {

        if (this.props.findOptions.groupResults)
            throw new Error("Results are grouped")

        return this.state.selectedRows!.map(a => a.entity!);
    }

    // SELECT BUTTON

    handleSelectedToggle = () => {
        this.setState({ isSelectOpen: !this.state.isSelectOpen }, () => {
            if (this.state.isSelectOpen && this.state.currentMenuItems == undefined)
                this.loadMenuItems();
        });
    }

    loadMenuItems() {
        if (this.props.showContextMenu == "Basic" || this.props.findOptions.groupResults)
            this.setState({ currentMenuItems: [] });
        else {
            const options: ContextualItemsContext<Entity> = {
                lites: this.getSelectedEntities(),
                queryDescription: this.props.queryDescription,
                markRows: this.markRows,
                container: this,
            };

            renderContextualItems(options)
                .then(menuItems => this.setState({ currentMenuItems: menuItems }))
                .done();
        }
    }

    markRows = (dic: MarkedRowsDictionary) => {
        var promise = this.props.avoidAutoRefresh ? Promise.resolve(undefined) :
            this.doSearch(true);

        promise.then(() => this.setState({ markedRows: { ...this.state.markedRows, ...dic } as MarkedRowsDictionary })).done();

    }

    renderSelectedButton() {

        if (this.state.selectedRows == undefined)
            return null;

        const title = JavascriptMessage.Selected.niceToString() + " (" + this.state.selectedRows!.length + ")";

        return OrderUtils.setOrder(-1,
            <Dropdown id="selectedButton" className="sf-query-button sf-tm-selected"
                isOpen={this.state.isSelectOpen}
                toggle={this.handleSelectedToggle}
                disabled={this.state.selectedRows!.length == 0}>
                <DropdownToggle color="light" caret disabled={this.state.selectedRows!.length == 0}>{title}</DropdownToggle>
                <DropdownMenu>
                    {this.state.currentMenuItems == undefined ? <DropdownItem className="sf-tm-selected-loading">{JavascriptMessage.loading.niceToString()}</DropdownItem> :
                        this.state.currentMenuItems.length == 0 ? <DropdownItem className="sf-search-ctxitem-no-results">{JavascriptMessage.noActionsFound.niceToString()}</DropdownItem> :
                            this.state.currentMenuItems.map((e, i) => React.cloneElement(e, { key: i }))}
                </DropdownMenu>
            </Dropdown>
        );
    }

    // CONTEXT MENU

    handleContextOnHide = () => {
        this.setState({ contextualMenu: undefined });
    }


    handleQuickFilter = () => {
        const cm = this.state.contextualMenu!;
        const fo = this.props.findOptions;

        const token = fo.columnOptions[cm.columnIndex!].token;

        const op: FilterOperation | undefined =
            token && token.preferEquals || cm.rowIndex != null ? "EqualTo" as FilterOperation | undefined :
                token ? (filterOperations[token.filterType as any] || []).firstOrNull() as FilterOperation | undefined :
                    undefined as FilterOperation | undefined;

        const rt = this.state.resultTable;

        fo.filterOptions.push({
            token: token,
            operation: op,
            value: cm.rowIndex == undefined || rt == null || token == null ? undefined : rt.rows[cm.rowIndex].columns[rt.columns.indexOf(token.fullKey)],
            frozen: false
        });

        if (!this.state.showFilters)
            this.state.showFilters = true;

        this.handleFiltersChanged();

        this.forceUpdate(() => this.handleHeightChanged());
    }

    handleInsertColumn = () => {

        const token = withoutAllAny(this.state.lastToken);

        const newColumn: ColumnOptionParsed = {
            token: token,
            displayName: token && token.niceName,
        };

        const cm = this.state.contextualMenu!;
        this.setState({ editingColumn: newColumn }, () => this.handleHeightChanged());
        this.props.findOptions.columnOptions.insertAt(cm.columnIndex! + cm.columnOffset!, newColumn);

        this.forceUpdate();
    }

    handleEditColumn = () => {

        const cm = this.state.contextualMenu!;
        const fo = this.props.findOptions;
        this.setState({ editingColumn: fo.columnOptions[cm.columnIndex!] }, () => this.handleHeightChanged());

        this.forceUpdate();
    }

    handleRemoveColumn = () => {
        const s = this.state;
        const cm = this.state.contextualMenu!;
        const fo = this.props.findOptions;
        const col = fo.columnOptions[cm.columnIndex!];
        fo.columnOptions.removeAt(cm.columnIndex!);
        if (fo.groupResults && col.token) {
            fo.orderOptions.extract(a => a.token.fullKey == col.token!.fullKey);
        }

        this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());
    }

    renderContextualMenu() {

        const cm = this.state.contextualMenu!;
        const p = this.props;

        const menuItems: React.ReactElement<any>[] = [];
        if (this.canFilter() && cm.columnIndex != undefined)
            menuItems.push(<DropdownItem className="sf-quickfilter-header" onClick={this.handleQuickFilter}><span className="icon fa fa-filter"/>&nbsp;{JavascriptMessage.addFilter.niceToString()}</DropdownItem>);

        if (cm.rowIndex == undefined && p.allowChangeColumns) {

            if (menuItems.length)
                menuItems.push(<DropdownItem divider />);

            menuItems.push(<DropdownItem className="sf-insert-header" onClick={this.handleInsertColumn}><span className="icon fa fa-plus-circle" />&nbsp;{JavascriptMessage.insertColumn.niceToString()}</DropdownItem>);
            menuItems.push(<DropdownItem className="sf-edit-header" onClick={this.handleEditColumn}><span className="icon fa fa-pencil" />&nbsp;{JavascriptMessage.editColumn.niceToString()}</DropdownItem>);
            menuItems.push(<DropdownItem className="sf-remove-header" onClick={this.handleRemoveColumn}><span className="icon fa fa-minus-circle" />&nbsp;{JavascriptMessage.removeColumn.niceToString()}</DropdownItem>);
        }

        if (cm.rowIndex != undefined) {

            if (this.state.currentMenuItems == undefined) {
                menuItems.push(<DropdownItem header>{JavascriptMessage.loading.niceToString()}</DropdownItem>);
            } else {
                if (menuItems.length && this.state.currentMenuItems.length)
                    menuItems.push(<DropdownItem divider />);

                menuItems.splice(menuItems.length, 0, ...this.state.currentMenuItems);
            }
        }

        if (menuItems.length == 0)
            return null;

        return (
            <ContextMenu position={cm.position} onHide={this.handleContextOnHide}>
                {menuItems.map((e, i) => React.cloneElement(e, { key: i }))}
            </ContextMenu>
        );
    }

    //SELECTED ROWS

    allSelected() {
        return this.state.resultTable != undefined && this.state.resultTable.rows.length != 0 && this.state.resultTable.rows.length == this.state.selectedRows!.length;
    }

    handleToggleAll = () => {

        if (!this.state.resultTable)
            return;

        this.setState({ selectedRows: !this.allSelected() ? this.state.resultTable!.rows.clone() : [] }, () => {
            this.notifySelectedRowsChanged()
        });
    }

    handleHeaderClick = (e: React.MouseEvent<any>) => {

        const token = (e.currentTarget as HTMLElement).getAttribute("data-column-name");
        const fo = this.props.findOptions;
        const prev = fo.orderOptions.filter(a => a.token.fullKey == token).firstOrNull();

        if (prev != undefined) {
            prev.orderType = prev.orderType == "Ascending" ? "Descending" : "Ascending";
            if (!e.shiftKey)
                fo.orderOptions = [prev];

        } else {

            const column = fo.columnOptions.filter(a => a.token && a.token.fullKey == token).first("Column");

            const newOrder: OrderOptionParsed = { token: column.token!, orderType: "Ascending" };

            if (e.shiftKey)
                fo.orderOptions.push(newOrder);
            else
                fo.orderOptions = [newOrder];
        }

        this.forceUpdate();

        if (fo.pagination.mode != "All")
            this.doSearchPage1();
    }

    //HEADER DRAG AND DROP

    handleHeaderDragStart = (de: React.DragEvent<any>, dragIndex: number) => {
        de.dataTransfer.setData('text', "start"); //cannot be empty string
        de.dataTransfer.effectAllowed = "move";
        this.setState({ dragColumnIndex: dragIndex });
    }

    handleHeaderDragEnd = (de: React.DragEvent<any>) => {
        this.setState({ dragColumnIndex: undefined, dropBorderIndex: undefined });
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

        return undefined;
    }

    handlerHeaderDragOver = (de: React.DragEvent<any>, columnIndex: number) => {
        de.preventDefault();

        const th = de.currentTarget as HTMLElement;

        const size = th.scrollWidth;

        const offset = this.getOffset((de.nativeEvent as DragEvent).pageX, th.getBoundingClientRect(), 50);

        let dropBorderIndex = offset == undefined ? undefined : columnIndex + offset;

        if (dropBorderIndex == this.state.dragColumnIndex || dropBorderIndex == this.state.dragColumnIndex! + 1)
            dropBorderIndex = undefined;

        //de.dataTransfer.dropEffect = dropBorderIndex == undefined ? "none" : "move";

        if (this.state.dropBorderIndex != dropBorderIndex)
            this.setState({ dropBorderIndex: dropBorderIndex });
    }

    handleHeaderDrop = (de: React.DragEvent<any>) => {
        de.preventDefault();

        const dropBorderIndex = this.state.dropBorderIndex!;
        if (dropBorderIndex == null)
            return;

        const columns = this.props.findOptions.columnOptions;
        const dragColumnIndex = this.state.dragColumnIndex!;

        const temp = columns[dragColumnIndex!];
        columns.removeAt(dragColumnIndex!);
        const rebasedDropIndex = dropBorderIndex > dragColumnIndex ? dropBorderIndex - 1 : dropBorderIndex;
        columns.insertAt(rebasedDropIndex, temp);

        this.setState({
            dropBorderIndex: undefined,
            dragColumnIndex: undefined
        });
    }

    renderHeaders(): React.ReactNode {

        return (
            <tr>
                {this.props.allowSelection && <th className="sf-th-selection">
                    <input type="checkbox" id="cbSelectAll" onClick={this.handleToggleAll} checked={this.allSelected()} />
                </th>
                }
                {this.props.navigate && !this.props.findOptions.groupResults && <th className="sf-th-entity" data-column-name="Entity"></th>}
                {this.props.findOptions.columnOptions.map((co, i) =>
                    <th key={i}
                        draggable={true}
                        className={classes(
                            i == this.state.dragColumnIndex && "sf-draggin",
                            co == this.state.editingColumn && "sf-current-column",
                            !this.canOrder(co) && "noOrder",
                            co == this.state.editingColumn && co.token && co.token.type.isCollection && "error",
                            this.state.dropBorderIndex != null && i == this.state.dropBorderIndex ? "drag-left " :
                                this.state.dropBorderIndex != null && i == this.state.dropBorderIndex - 1 ? "drag-right " : undefined)}
                        data-column-name={co.token && co.token.fullKey}
                        data-column-index={i}
                        onClick={this.canOrder(co) ? this.handleHeaderClick : undefined}
                        onDragStart={e => this.handleHeaderDragStart(e, i)}
                        onDragEnd={this.handleHeaderDragEnd}
                        onDragOver={e => this.handlerHeaderDragOver(e, i)}
                        onDragEnter={e => this.handlerHeaderDragOver(e, i)}
                        onDrop={this.handleHeaderDrop}>
                        <span className={"sf-header-sort " + this.orderClassName(co)} />
                        {this.props.findOptions.groupResults && co.token && co.token.queryTokenType != "Aggregate" && <span> <i className="fa fa-key" /></span>}
                        <span> {co.displayName}</span></th>
                )}
            </tr>
        );
    }

    canOrder(column: ColumnOptionParsed) {
        if (!column.token || !this.props.allowChangeOrder)
            return false;

        const t = column.token;

        if (t.type.isCollection)
            return false;

        if (t.type.isEmbedded || isTypeModel(t.type.name))
            return t.hasOrderAdapter == true;

        return true;
    }

    orderClassName(column: ColumnOptionParsed) {

        if (column.token == undefined)
            return "";

        const orders = this.props.findOptions.orderOptions;

        const o = orders.filter(a => a.token.fullKey == column.token!.fullKey).firstOrNull();
        if (o == undefined)
            return "";


        let asc = (o.orderType == "Ascending" ? "asc" : "desc");

        if (orders.indexOf(o))
            asc += " l" + orders.indexOf(o);

        return asc;
    }

    //ROWS

    handleChecked = (event: React.ChangeEvent<HTMLInputElement>) => {

        const cb = event.currentTarget;

        const index = parseInt(cb.getAttribute("data-index")!);

        const row = this.state.resultTable!.rows[index];

        var selectedRows = this.state.selectedRows!;

        if (cb.checked) {
            if (!selectedRows.contains(row))
                selectedRows.push(row);
        } else {
            selectedRows.remove(row);
        }

        this.notifySelectedRowsChanged();

        this.setState({ currentMenuItems: undefined });
    }

    handleDoubleClick = (e: React.MouseEvent<any>, row: ResultRow) => {

        if ((e.target as HTMLElement).parentElement != e.currentTarget) //directly in the td
            return;

        if (this.props.onDoubleClick) {
            e.preventDefault();
            this.props.onDoubleClick(e, row);
            return;
        }

        var qs = this.props.querySettings;
        if (qs && qs.onDoubleClick) {
            e.preventDefault();
            qs.onDoubleClick(e, row);
            return;
        }

        var resFo = this.state.resultFindOptions;
        if (resFo && resFo.groupResults) {

            var keyFilters = resFo.columnOptions
                .map((col, i) => ({ col, value: row.columns[i] }))
                .filter(a => a.col.token && a.col.token.queryTokenType != "Aggregate")
                .map(a => ({ columnName: a.col.token!.fullKey, operation: "EqualTo", value: a.value }) as FilterOption);
            
            var nonAggregateFilters = resFo.filterOptions.filter(fo => fo.token != null && fo.token.queryTokenType != "Aggregate")
                .map(fo => ({ columnName: fo.token!.fullKey, operation: fo.operation, value: fo.value }) as FilterOption);

            var extraColumns = resFo.columnOptions.filter(a => a.token && a.token.queryTokenType == "Aggregate" && a.token.parent)
                .map(a => ({ columnName: a.token!.parent!.fullKey }) as ColumnOption);

            Finder.explore({
                queryName: resFo.queryKey,
                filterOptions: nonAggregateFilters.concat(keyFilters),
                columnOptions: extraColumns,
                columnOptionsMode: "Add",
                systemTime: resFo.systemTime && { ...resFo.systemTime },
            }).done();

            return;
        }

        var lite = row.entity!;

        if (!Navigator.isNavigable(lite.EntityType, undefined, true))
            return;

        e.preventDefault();

        const s = Navigator.getSettings(lite.EntityType)

        const avoidPopup = s != undefined && s.avoidPopup;

        if (avoidPopup || e.ctrlKey || e.button == 1) {
            window.open(Navigator.navigateRoute(lite));
        }
        else {
            Navigator.navigate(lite)
                .then(() => {
                    this.handleOnNavigated(lite);
                }).done();
        }

    }

    renderRows(): React.ReactNode {

        const columnsCount = this.props.findOptions.columnOptions.length +
            (this.props.allowSelection ? 1 : 0) +
            (this.props.navigate ? 1 : 0);


        if (!this.state.resultTable) {
            return <tr><td colSpan={columnsCount}>{JavascriptMessage.searchForResults.niceToString()}</td></tr>;
        }

        var resultTable = this.state.resultTable;

        if (resultTable.rows.length == 0) {
            return <tr><td colSpan={columnsCount}>{SearchMessage.NoResultsFound.niceToString()}</td></tr>;
        }

        const qs = this.props.querySettings;

        const columns = this.props.findOptions.columnOptions.map(co => ({
            columnOption: co,
            cellFormatter: (co.token && this.props.formatters && this.props.formatters[co.token.fullKey]) || Finder.getCellFormatter(qs, co, this),
            resultIndex: co.token == undefined ? -1 : resultTable.columns.indexOf(co.token.fullKey)
        }));

        const ctx: Finder.CellFormatterContext = {
            refresh: () => this.doSearch(true).done()
        };

        const rowAttributes = this.props.rowAttributes || qs && qs.rowAttributes;

        return this.state.resultTable.rows.map((row, i) => {

            const mark = row.entity && this.getMarkedRow(row.entity);

            var ra = rowAttributes ? rowAttributes(row, resultTable.columns) : undefined;

            const message = mark && mark.message;

            const id = message && "result_row_" + i;

            return (
                <tr key={i} data-row-index={i} data-entity={row.entity && liteKey(row.entity)}
                    onDoubleClick={e => this.handleDoubleClick(e, row)}
                    id={id}
                    {...ra}
                    className={classes(mark && mark.className, ra && ra.className)}>
                    {this.props.allowSelection &&
                        <td style={{ textAlign: "center" }}>
                            <input type="checkbox" className="sf-td-selection" checked={this.state.selectedRows!.contains(row)} onChange={this.handleChecked} data-index={i} />
                        </td>
                    }

                    {this.props.navigate && !this.props.findOptions.groupResults &&
                        <td>
                            {(this.props.entityFormatter || (qs && qs.entityFormatter) || Finder.entityFormatRules.filter(a => a.isApplicable(row, this)).last("EntityFormatRules").formatter)(row, resultTable.columns, this)}
                        </td>
                    }

                    {
                        columns.map((c, j) =>
                            <td key={j} data-column-index={j} className={c.cellFormatter && c.cellFormatter.cellClass}>
                                {c.resultIndex == -1 || c.cellFormatter == undefined ? undefined : c.cellFormatter.formatter(row.columns[c.resultIndex], ctx)}
                            </td>)
                    }

                    {message && id && <UncontrolledTooltip placement="bottom" target={id}>
                        {message.split("\n").map((s, i) => <p key={i}>{s}</p>)}
                    </UncontrolledTooltip>}
                </tr>
            );
        });
    }

    handleOnNavigated = (lite: Lite<Entity>) => {

        if (this.props.onNavigated)
            this.props.onNavigated(lite);

        if (this.props.avoidAutoRefresh)
            return;

        this.doSearch(true);
    }

    getMarkedRow(entity: Lite<Entity>): MarkedRow | undefined {

        if (!entity || !this.state.markedRows)
            return undefined;

        const m = this.state.markedRows[liteKey(entity)];

        if (typeof m === "string") {
            if (m == "")
                return { className: "sf-entity-ctxmenu-success", message: undefined };
            else
                return { className: "danger", message: m };
        }
        else {
            return m;
        }
    }
}

function withoutAllAny(qt: QueryToken | undefined): QueryToken | undefined {
    if (qt == undefined)
        return undefined;

    if (qt.queryTokenType == "AnyOrAll")
        return withoutAllAny(qt.parent);

    var par = withoutAllAny(qt.parent);

    if (par == qt.parent)
        return qt;

    return par;
}

function removeAggregates(array: { token?: QueryToken, displayName?: string }[], qd: QueryDescription) {
    array.filter(a => a.token != null && a.token.queryTokenType == "Aggregate").forEach(a => {
        if (a.token) {
            if (a.token.parent) {
                a.token = a.token.parent;
            } else {
                a.token = qd.columns["Id"] ? toQueryToken(qd.columns["Id"]) : undefined;
            }

            if (a.displayName)
                a.displayName = a.token!.niceName;
        }
    });

    array.extract(a => a.token == null);
}

function withAggregates(array: { token?: QueryToken, displayName?: string }[], tc: Finder.TokenCompleter, mode: "request" | "get"): void {
    array.forEach(a => {
        if (a.token) {
            if (canHaveMin(a.token.type.name)) {

                var tokenName = a.token.fullKey == "Id" ? "Count" : a.token.fullKey + ".Min";

                if (mode == "request")
                    tc.request(tokenName, SubTokensOptions.CanAggregate);
                else {
                    a.token = tc.get(tokenName);
                    if (a.displayName)
                        a.displayName = a.token.niceName;
                }
            } else if (a.token.isGroupable) {
                //Nothing, will be group key
            } else {
                a.token = undefined;
            }
        }
    });

    array.extract(a => a.token == undefined);
}

function canHaveMin(typeName: string): boolean {
    return typeName == "number" || typeName == "decimal" || typeName == "TimeSpan";
}