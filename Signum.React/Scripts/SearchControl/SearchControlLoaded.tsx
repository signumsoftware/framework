import * as React from 'react'
import { DateTime } from 'luxon'
import { DomUtils, classes, Dic, softCast } from '../Globals'
import * as Finder from '../Finder'
import { CellFormatter, EntityFormatter, toFilterRequests, toFilterOptions, isAggregate } from '../Finder'
import {
  ResultTable, ResultRow, FindOptionsParsed, FilterOption, FilterOptionParsed, QueryDescription, ColumnOption, ColumnOptionParsed, ColumnDescription,
  toQueryToken, Pagination, OrderOptionParsed, SubTokensOptions, filterOperations, QueryToken, QueryRequest, isActive, isFilterGroupOptionParsed, hasOperation, hasToArray, hasElement, getTokenParents, FindOptions
} from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey, Entity, ModifiableEntity, EntityPack, FrameMessage } from '../Signum.Entities'
import { tryGetTypeInfos, TypeInfo, isTypeModel, getTypeInfos, QueryTokenString } from '../Reflection'
import * as Navigator from '../Navigator'
import * as AppContext from '../AppContext';
import { AbortableRequest } from '../Services'
import * as Constructor from '../Constructor'
import * as Hooks from '../Hooks'
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
import { FilterOperation, RefreshMode } from '../Signum.Entities.DynamicQuery';
import SystemTimeEditor from './SystemTimeEditor';
import { Property } from 'csstype';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import "./Search.css"
import "./SearchMobile.css"
import PinnedFilterBuilder from './PinnedFilterBuilder';
import { AutoFocus } from '../Components/AutoFocus';
import { ButtonBarElement, StyleContext } from '../TypeContext';
import { Dropdown, DropdownButton, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { getBreakpoint, Breakpoints } from '../Hooks'
import { IconProp } from '@fortawesome/fontawesome-svg-core'

interface ColumnParsed {
  column: ColumnOptionParsed;
  hasToArray?: QueryToken;
  cellFormatter?: Finder.CellFormatter;
  resultIndex: number;
}

export type SearchControlViewMode = "Mobile" | "Standard";

export interface SearchControlMobileOptions {
  showSwitchViewModesButton: boolean;
  defaultViewMode: SearchControlViewMode;
}

export interface ShowBarExtensionOption { }

export interface CustomDrilldownOptions {
  openInNewTab?: boolean;
  showInPlace?: boolean;
  fo?: FindOptions;
  entity?: Lite<Entity>;
  onReload?: () => void;
}

export interface SearchControlLoadedProps {
  findOptions: FindOptionsParsed;
  queryDescription: QueryDescription;
  querySettings: Finder.QuerySettings | undefined;

  formatters?: { [token: string]: CellFormatter };
  rowAttributes?: (row: ResultRow, columns: string[]) => React.HTMLAttributes<HTMLTableRowElement> | undefined;
  entityFormatter?: EntityFormatter;
  extraButtons?: (searchControl: SearchControlLoaded) => (ButtonBarElement | null | undefined | false)[];
  getViewPromise?: (e: ModifiableEntity | null) => (undefined | string | Navigator.ViewPromise<ModifiableEntity>);
  maxResultsHeight?: Property.MaxHeight<string | number> | any;
  tag?: string | {};

  defaultIncudeDefaultFilters: boolean;
  searchOnLoad: boolean;
  allowSelection: boolean | "single";
  showContextMenu: (fop: FindOptionsParsed) => boolean | "Basic";
  showSelectedButton: boolean;
  hideButtonBar: boolean;
  hideFullScreenButton: boolean;
  showHeader: boolean | "PinnedFilters";
  pinnedFilterVisible?: (fop: FilterOptionParsed) => boolean;
  showBarExtension: boolean;
  showBarExtensionOption?: ShowBarExtensionOption;
  showFilters: boolean;
  showSimpleFilterBuilder: boolean;
  showFilterButton: boolean;
  showSystemTimeButton: boolean;
  showGroupButton: boolean;
  showFooter?: boolean;
  allowChangeColumns: boolean;
  allowChangeOrder: boolean;
  create: boolean;
  createButtonClass?: string;
  view: boolean | "InPlace";
  largeToolbarButtons: boolean;
  defaultRefreshMode?: RefreshMode;
  avoidChangeUrl: boolean;
  deps?: React.DependencyList;
  extraOptions: any;


  simpleFilterBuilder?: (sfbc: Finder.SimpleFilterBuilderContext) => React.ReactElement<any> | undefined;
  enableAutoFocus: boolean;
  //Return "no_change" to prevent refresh. Navigator.view won't be called by search control, but returning an entity allows to return it immediatly in a SearchModal in find mode.  
  onCreate?: (scl: SearchControlLoaded) => Promise<undefined | void | EntityPack<any> | ModifiableEntity | "no_change">;
  onCreateFinished?: (entity: EntityPack<Entity> | ModifiableEntity | Lite<Entity> | undefined | void, scl: SearchControlLoaded) => void;
  onDoubleClick?: (e: React.MouseEvent<any>, row: ResultRow, sc?: SearchControlLoaded) => void;
  onNavigated?: (lite: Lite<Entity>) => void;
  onSelectionChanged?: (rows: ResultRow[]) => void;
  onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
  onHeighChanged?: () => void;
  onSearch?: (fo: FindOptionsParsed, dataChange: boolean, sc: SearchControlLoaded) => void;
  onResult?: (table: ResultTable, dataChange: boolean, sc: SearchControlLoaded) => void;
  styleContext?: StyleContext;
  customRequest?: (req: QueryRequest, fop: FindOptionsParsed) => Promise<ResultTable>,
  onPageTitleChanged?: () => void;
  mobileOptions?: (fop: FindOptionsParsed) => SearchControlMobileOptions;
}

export interface SearchControlLoadedState {
  resultTable?: ResultTable;
  summaryResultTable?: ResultTable;
  simpleFilterBuilder?: React.ReactElement<any>;
  selectedRows?: ResultRow[];
  markedRows?: MarkedRowsDictionary;
  isSelectOpen: boolean;
  resultFindOptions?: FindOptionsParsed;
  searchCount?: number;
  dragColumnIndex?: number,
  dropBorderIndex?: number,
  showHiddenColumns?: boolean,
  currentMenuItems?: React.ReactElement<any>[];
  dataChanged?: boolean;

  contextualMenu?: {
    position: ContextMenuPosition;
    columnIndex: number | null;
    columnOffset?: number;
    rowIndex: number | null;
  };

  showFilters: boolean;
  refreshMode?: RefreshMode;
  editingColumn?: ColumnOptionParsed;
  lastToken?: QueryToken;
  isMobile?: boolean;
  viewMode?: SearchControlViewMode;
}

export default class SearchControlLoaded extends React.Component<SearchControlLoadedProps, SearchControlLoadedState>{

  constructor(props: SearchControlLoadedProps) {
    super(props);
    this.state = {
      isSelectOpen: false,
      showFilters: props.showFilters,
      refreshMode: props.defaultRefreshMode,
    };
  }

  static maxToArrayElements = 100;
  static mobileOptions: ((fop: FindOptionsParsed) => SearchControlMobileOptions) | null = null;
  static onCustomDrilldown: ((items: Lite<Entity>[], options?: CustomDrilldownOptions) => Promise<void>) | null = null;

  pageSubTitle?: string;
  extraUrlParams: { [key: string]: string | undefined } = {};
  customDrilldowns: Lite<Entity>[] = [];

  getMobileOptions(fop: FindOptionsParsed) {
    const fo = this.props.findOptions;
    const qs = Finder.getSettings(fo.queryKey);

    return this.props.mobileOptions?.(fop) ?? qs?.mobileOptions?.(fop) ?? SearchControlLoaded.mobileOptions?.(fop) ??
      {
        showSwitchViewModesButton: true,
        defaultViewMode: "Mobile"
      };
  }

  onResize = () => {
    const isMobile = (getBreakpoint() <= Breakpoints.sm);
    if (isMobile != this.state.isMobile)
      this.setState({
        isMobile: isMobile,
        viewMode: isMobile ? this.getMobileOptions(this.props.findOptions).defaultViewMode : "Standard",
      }, () => this.fixScroll());
  }

  componentDidMount() {
    window.addEventListener('resize', this.onResize);
    this.onResize();

    const fo = this.props.findOptions;
    const qs = Finder.getSettings(fo.queryKey);
    const qd = this.props.queryDescription;

    const sfb = this.props.showSimpleFilterBuilder == false ? undefined :
      this.props.simpleFilterBuilder ? this.props.simpleFilterBuilder({ queryDescription: qd, initialFilterOptions: fo.filterOptions, search: () => this.doSearchPage1(), searchControl: this }) :
        qs?.simpleFilterBuilder ? qs.simpleFilterBuilder({ queryDescription: qd, initialFilterOptions: fo.filterOptions, search: () => this.doSearchPage1(), searchControl: this }) :
          undefined;

    if (sfb) {
      this.setState({
        showFilters: false,
        simpleFilterBuilder: sfb
      });
    }

    if (this.props.searchOnLoad)
      this.doSearch({ force: true });
  }

  componentDidUpdate(props: SearchControlLoadedProps) {
    if (!Hooks.areEqual(this.props.deps ?? [], props.deps ?? [])) {
      this.doSearchPage1();
    }
  }

  isUnmounted = false;
  componentWillUnmount() {
    this.isUnmounted = true;
    window.removeEventListener('resize', this.onResize);
    this.abortableSearch.abort();
    this.abortableSearchSummary.abort();
  }

  entityColumn(): ColumnDescription {
    return this.props.queryDescription.columns["Entity"];
  }

  entityColumnTypeInfos(): TypeInfo[] {
    return getTypeInfos(this.entityColumn().type);
  }

  canFilter() {
    const p = this.props;
    return p.showHeader == true && (p.showFilterButton || p.showFilters);
  }

  getQueryRequest(avoidHiddenColumns?: boolean): QueryRequest {
    const fo = this.props.findOptions;
    const qs = this.props.querySettings;

    return Finder.getQueryRequest(fo, qs, avoidHiddenColumns);
  }

  getSummaryQueryRequest(): QueryRequest | null {
    const fo = this.props.findOptions;

    return Finder.getSummaryQueryRequest(fo);
  }

  // MAIN

  isManualRefreshOrAllPagination() {
    return this.state.refreshMode == "Manual" || this.state.refreshMode == undefined && this.props.findOptions.pagination.mode == "All";
  }

  doSearchPage1(force: boolean = false) {

    const fo = this.props.findOptions;

    if (fo.pagination.mode == "Paginate")
      fo.pagination.currentPage = 1;

    if (this.containerDiv)
      this.containerDiv.scrollTop = 0;

    this.doSearch({ force });
  };

  resetResults(continuation: () => void) {
    this.setState({
      resultTable: undefined,
      summaryResultTable: undefined,
      resultFindOptions: undefined,
      selectedRows: [],
      currentMenuItems: undefined,
      markedRows: undefined,
      dataChanged: undefined,
    }, continuation);
  }

  abortableSearch = new AbortableRequest((signal, a: {
    request: QueryRequest;
    fop: FindOptionsParsed,
    customRequest?: (req: QueryRequest, fop: FindOptionsParsed) => Promise<ResultTable>
  }) => a.customRequest ? a.customRequest(a.request, a.fop) : Finder.API.executeQuery(a.request, signal));

  abortableSearchSummary = new AbortableRequest((signal, a: {
    request: QueryRequest;
    fop: FindOptionsParsed,
    customRequest?: (req: QueryRequest, fop: FindOptionsParsed) => Promise<ResultTable>
  }) => a.customRequest ? a.customRequest(a.request, a.fop) : Finder.API.executeQuery(a.request, signal));

  dataChanged(): Promise<void> {
    if (this.isManualRefreshOrAllPagination()) {
      this.setState({ dataChanged: true });
      return Promise.resolve();
    }
    else {
      return this.doSearch({dataChanged: true});
    }
  }

  doSearch(opts : { dataChanged?: boolean, force?: boolean}): Promise<void> {

    if (this.isUnmounted || (this.isManualRefreshOrAllPagination() && !opts.force))
      return Promise.resolve();

    var dataChanged = opts.dataChanged ?? this.state.dataChanged;

    return this.getFindOptionsWithSFB().then(fop => {
      if (this.props.onSearch)
        this.props.onSearch(fop, dataChanged ?? false, this);

      if (this.simpleFilterBuilderInstance && this.simpleFilterBuilderInstance.onDataChanged)
        this.simpleFilterBuilderInstance.onDataChanged();

      this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());
      var resultFindOptions = JSON.parse(JSON.stringify(fop));

      const qr = this.getQueryRequest();
      const qrSummary = this.getSummaryQueryRequest();

      const customRequest = this.props.customRequest;

      return Promise.all([this.abortableSearch.getData({ request: qr, fop, customRequest }),
        qrSummary ? this.abortableSearchSummary.getData({ request: qrSummary, fop, customRequest }) : Promise.resolve<ResultTable | undefined>(undefined) 
      ]).then(([rt, summaryRt]) => {
        this.setState({
          resultTable: rt,
          dataChanged: undefined,
          summaryResultTable: summaryRt,
          resultFindOptions: resultFindOptions,
          selectedRows: [],
          currentMenuItems: undefined,
          markedRows: undefined,
          searchCount: (this.state.searchCount ?? 0) + 1
        }, () => {
          this.fixScroll();
          if (this.props.onResult)
            this.props.onResult(rt, dataChanged ?? false, this);
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
    this.setState({ resultTable: undefined, resultFindOptions: undefined, dataChanged: false });

    if (this.containerDiv)
      this.containerDiv.scrollTop = 0;

    this.doSearch({});
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
        position: ContextMenu.getPositionEvent(event),
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
    if (this.props.findOptions.groupResults) {
      var allKeys = this.props.findOptions.columnOptions.filter(a => a.token && a.token.queryTokenType != "Aggregate").map(a => a.token!.fullKey);
      this.props.findOptions.orderOptions = this.props.findOptions.orderOptions.filter(o => allKeys.contains(o.token.fullKey));
    }
    this.setState({ lastToken: token });
  }

  handleColumnClose = () => {
    this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());
  }

  handleFilterTokenChanged = (token: QueryToken | undefined) => {
    this.setState({ lastToken: token });
  }


  handleFiltersChanged = () => {

    if (this.isManualRefreshOrAllPagination())
      this.forceUpdate();

    if (this.props.onFiltersChanged)
      this.props.onFiltersChanged(this.props.findOptions.filterOptions);
  }


  handlePinnedFilterChanged = () => {

    this.handleFiltersChanged();

      this.doSearchPage1();
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
        this.doSearchPage1(true);
      }, 200);
    }
  }


  fixScroll() {
    if (this.containerDiv) {
      var table = this.containerDiv.firstChild! as HTMLElement;
      if (this.containerDiv.scrollTop > table.clientHeight) {
        //var translate = "translate(0,0)";
        //this.thead!.style.transform = translate;
        this.containerDiv.scrollTop = 0;
        this.containerDiv.style.overflowY = "hidden";
        setTimeout(() => {
          this.containerDiv!.style.overflowY = "";
        }, 10);

      }
    }
  }


  containerDiv?: HTMLDivElement | null;

  render() {
    const p = this.props;
    const fo = this.props.findOptions;
    const qd = this.props.queryDescription;

    const sfb = this.state.simpleFilterBuilder &&
      React.cloneElement(this.state.simpleFilterBuilder, { ref: (e: ISimpleFilterBuilder) => { this.simpleFilterBuilderInstance = e } });

    const canAggregate = (fo.groupResults ? SubTokensOptions.CanAggregate : 0);
    const canAggregateXorOperation = (canAggregate != 0 ? canAggregate : SubTokensOptions.CanOperation);

    return (
      <div className={classes("sf-search-control sf-control-container", this.state.isMobile == true && this.state.viewMode == "Mobile" && "mobile")}
        data-search-count={this.state.searchCount}
        data-query-key={fo.queryKey}>
        {p.showHeader == true &&
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
              showPinnedFiltersOptions={false}
              showPinnedFiltersOptionsButton={true}
              /> :
              sfb && <div className="simple-filter-builder">{sfb}</div>}
          </div>
        }
        {p.showHeader == true && !this.state.showFilters && !sfb && this.renderPinnedFilters(true)}
        {p.showHeader == "PinnedFilters" && (sfb ?? this.renderPinnedFilters(true))}
        {p.showHeader == true && this.renderToolBar()}
        {p.showHeader == true && <MultipliedMessage findOptions={fo} mainType={this.entityColumn().type} />}
        {p.showHeader == true && fo.groupResults && <GroupByMessage findOptions={fo} mainType={this.entityColumn().type} />}
        {p.showHeader == true && fo.systemTime && <SystemTimeEditor findOptions={fo} queryDescription={qd} onChanged={() => this.forceUpdate()} />}
        {this.state.isMobile == true && this.state.viewMode == "Mobile" ? this.renderMobile() :
          <>
            {
              this.state.editingColumn && <ColumnEditor
                columnOption={this.state.editingColumn}
                onChange={this.handleColumnChanged}
                queryDescription={qd}
                subTokensOptions={SubTokensOptions.CanElement | SubTokensOptions.CanToArray | canAggregateXorOperation}
                close={this.handleColumnClose} />
            }
            <div ref={d => this.containerDiv = d}
              className="sf-scroll-table-container table-responsive"
              style={{ maxHeight: this.props.maxResultsHeight }}>
              <table className="sf-search-results table table-hover table-sm" onContextMenu={this.props.showContextMenu(this.props.findOptions) != false ? this.handleOnContextMenu : undefined}>
                <thead>
                  {this.renderHeaders()}
                </thead>
                <tbody>
                  {this.renderRows()}
                </tbody>
              </table>
            </div>
          </>}
        {(p.showFooter ?? (this.state.resultTable != null && (this.state.resultTable.totalElements == null || this.state.resultTable.totalElements > this.state.resultTable.rows.length))) &&
          <PaginationSelector pagination={fo.pagination} onPagination={this.handlePagination} resultTable={this.state.resultTable} />}
        {this.state.contextualMenu && this.renderContextualMenu()}
      </div>
    );
  }

  renderMobile() {
    return (
      <div ref={d => this.containerDiv = d} 
        className="sf-scroll-table-container"
        style={{ maxHeight: this.props.maxResultsHeight }}>
        <div className="sf-search-results mobile">
          {this.renderRowsMobile()}
        </div>
      </div>
    );
  }

  // TOOLBAR


  handleSearchClick = (ev: React.MouseEvent<any>) => {

    ev.preventDefault();

    this.doSearchPage1(true);

  };

  handleToggleFilters = () => {
    this.getFindOptionsWithSFB().then(() => {
      this.simpleFilterBuilderInstance = undefined;
      this.setState({
        simpleFilterBuilder: undefined,
        showFilters: !this.state.showFilters
      }, () => this.handleHeightChanged());
    });
  }

  handleSystemTimeClick = () => {
    var fo = this.props.findOptions;

    if (fo.systemTime == null)
      fo.systemTime = { mode: "AsOf", startDate: DateTime.local().toISO() };
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

      this.customDrilldowns = [];
    });
  }

  renderToolBar() {

    const p = this.props;
    const s = this.state;



    function toFindOptionsPath(fop: FindOptionsParsed) {
      var fo = Finder.toFindOptions(fop, p.queryDescription, p.defaultIncudeDefaultFilters);
      return Finder.findOptionsPath(fo);
    }

    const isManualOrAll = this.isManualRefreshOrAllPagination();
    var changesExpected = s.dataChanged || s.resultFindOptions == null || toFindOptionsPath(s.resultFindOptions) != toFindOptionsPath(p.findOptions);



    var buttonBarElements = Finder.ButtonBarQuery.getButtonBarElements({ findOptions: p.findOptions, searchControl: this });
    var leftButtonBarElements = buttonBarElements.extract(a => a.order != null && a.order < 0);


    const titleLabels = StyleContext.default.titleLabels;

    var leftButtons = ([

      p.showFilterButton && {
        order: -5,
        button: <button
          className={classes("sf-query-button sf-filters-header btn", s.showFilters && "active", "btn-light")}
          style={!s.showFilters && p.findOptions.filterOptions.filter(a => !a.pinned).length > 0 ? { border: "1px solid #6c757d" } : undefined}
          onClick={this.handleToggleFilters}
          title={titleLabels ? s.showFilters ? JavascriptMessage.hideFilters.niceToString() : JavascriptMessage.showFilters.niceToString() : undefined}>
          <FontAwesomeIcon icon="filter" />
        </button>
      },

      p.showGroupButton && {
        order: -4,
        button: < button
          className={"sf-query-button btn " + (p.findOptions.groupResults ? "btn-info active" : "btn-light")}
          onClick={this.handleToggleGroupBy}
          title={titleLabels ? p.findOptions.groupResults ? JavascriptMessage.ungroupResults.niceToString() : JavascriptMessage.groupResults.niceToString() : undefined}>
          Ʃ
            </button>
      },

      p.showSystemTimeButton && {
        order: -3.5,
        button: < button
          className={"sf-query-button btn " + (p.findOptions.systemTime ? "alert-primary" : "btn-light")}
          onClick={this.handleSystemTimeClick}
          title={titleLabels ? p.findOptions.systemTime ? JavascriptMessage.deactivateTimeMachine.niceToString() : JavascriptMessage.activateTimeMachine.niceToString() : undefined}>
          <FontAwesomeIcon icon="clock-rotate-left" />
        </button>
      },

      {
        order: -3,
        button: < button className={classes("sf-query-button sf-search btn ms-2", changesExpected ? (isManualOrAll ? "btn-danger" : "btn-primary") : (isManualOrAll ? "border-danger text-danger btn-light" : "border-primary text-primary btn-light"))} onClick={this.handleSearchClick} >
          <FontAwesomeIcon icon={"magnifying-glass"} /><span className="d-none d-sm-inline">&nbsp;{changesExpected ? SearchMessage.Search.niceToString() : SearchMessage.Refresh.niceToString()}</span>
        </button>
      },

      this.props.showContextMenu(this.props.findOptions) != false && this.props.showSelectedButton && this.renderSelectedButton(),

      p.create && {
        order: -2,
        button: <button className={classes("sf-query-button btn ", p.createButtonClass ?? "btn-light", "sf-create ms-2")} title = { titleLabels? this.createTitle() : undefined } onClick = { this.handleCreate }>
          <FontAwesomeIcon icon="plus" className="sf-create" /><span className="d-none d-sm-inline">&nbsp;{SearchMessage.Create.niceToString()}</span>
        </button>
      },

      ...(this.props.extraButtons ? this.props.extraButtons(this) : []),
      ...leftButtonBarElements
    ] as (ButtonBarElement | null | false | undefined)[])
      .filter(a => a)
      .map(a => a as ButtonBarElement);

    var rightButtons = ([
      ...(this.props.hideButtonBar ? [] : buttonBarElements),

      !this.props.hideFullScreenButton && Finder.isFindable(p.findOptions.queryKey, true) && {
        button: <button className="sf-query-button btn btn-light" onClick={this.handleFullScreenClick} title={FrameMessage.Fullscreen.niceToString()}>
          <FontAwesomeIcon icon="up-right-from-square" />
        </button>
      },

      this.state.isMobile == true && this.getMobileOptions(this.props.findOptions).showSwitchViewModesButton && {
        button: <button className="sf-query-button btn btn-light" onClick={this.handleViewModeClick}>
          {this.state.viewMode == "Mobile" ? <FontAwesomeIcon icon="desktop" /> :
            <FontAwesomeIcon icon="mobile-alt" />}
        </button>
      }
    ] as (ButtonBarElement | null | false | undefined)[])
      .filter(a => a)
      .map(a => a as ButtonBarElement);

    return (
      <div className={classes("sf-query-button-bar d-flex justify-content-between", !this.props.largeToolbarButtons && "btn-toolbar-small")}>
        {React.createElement("div", { className: "btn-toolbar" }, ...leftButtons.map(a => a.button))}
        {React.createElement("div", { className: "btn-toolbar", style: { justifyContent: "flex-end" } }, ...rightButtons.map(a => a.button))}
      </div>
    );
  }


  chooseType(): Promise<string | undefined> {

    const tis = getTypeInfos(this.props.queryDescription.columns["Entity"].type)
      .filter(ti => Navigator.isCreable(ti, { isSearch: true }));

    return SelectorModal.chooseType(tis)
      .then(ti => ti ? ti.name : undefined);
  }

  handleCreated = (entity: EntityPack<Entity> | ModifiableEntity | Lite<Entity> | undefined | void) => {
    if (this.props.onCreateFinished) {
      this.props.onCreateFinished(entity, this);
    } else {
      this.dataChanged();
    }
  }
 
  handleCreate = (ev: React.MouseEvent<any>) => {

    if (!this.props.create)
      return;

    const onCreate = this.props.onCreate;

    if (onCreate) {
      onCreate(this)
        .then(val => {
          if (val != "no_change")
            this.handleCreated(val);
        });
    }
    else {
      const isWindowsOpen = ev.button == 1 || ev.ctrlKey;

      this.chooseType().then(tn => {
        if (tn == undefined)
          return;

        var s = Navigator.getSettings(tn);

        var qs = this.props.querySettings;

        var getViewPromise = this.props.getViewPromise ?? qs?.getViewPromise;

        if (isWindowsOpen || s?.avoidPopup  || this.props.view == "InPlace")
        {
          Finder.getPropsFromFilters(tn, this.props.findOptions.filterOptions)
            .then(props => Constructor.constructPack(tn, props))
            .then(pack => {
              if (pack) {
                var vp = getViewPromise && getViewPromise(null);

                var viewName = typeof vp == "string" ? vp : undefined;

                if (this.props.view == "InPlace" && !isWindowsOpen)
                  Navigator.createInCurrentTab(pack, viewName);
                else
                  Navigator.createInNewTab(pack, viewName);
              }
            });
        } else {

          Finder.getPropsFromFilters(tn, this.props.findOptions.filterOptions)
            .then(props => Constructor.constructPack(tn, props))
            .then(pack => pack && Navigator.view(pack!, {
              getViewPromise: getViewPromise as any,
              buttons: "close",
              createNew: () => Finder.getPropsFromFilters(tn, this.props.findOptions.filterOptions)
                .then(props => Constructor.constructPack(tn, props)!),
            }))
            .then(entity => this.handleCreated(entity));
        }
        
      });
    }
  }

  handleFullScreenClick = (ev: React.MouseEvent<any>) => {

    ev.preventDefault();

    var findOptions = Finder.toFindOptions(this.props.findOptions, this.props.queryDescription, this.props.defaultIncudeDefaultFilters);

    const path = Finder.findOptionsPath(findOptions, this.extraUrlParams);

    if (ev.ctrlKey || ev.button == 1 || this.props.avoidChangeUrl)
      window.open(path);
    else
      AppContext.history.push(path);
  };

  handleViewModeClick = (ev: React.MouseEvent<any>) => {
    this.setState({ viewMode: (this.state.viewMode == "Mobile" ? "Standard" : "Mobile") }, () => this.fixScroll());
  }

  createTitle() {

    const tis = this.entityColumnTypeInfos();

    const types = tis.map(ti => ti.niceName).join(", ");
    const gender = tis.first().gender;

    return SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(gender).formatWith(types);
  }

  getSelectedEntities(): Lite<Entity>[] {

    if (this.props.findOptions.groupResults)
      throw new Error("Results are grouped")

    if (this.state.selectedRows == null)
      return [];

    return this.state.selectedRows.map(a => a.entity).notNull();
  }

  getGroupedSelectedEntities(): Promise<Lite<Entity>[]> {

    if (!this.props.findOptions.groupResults)
      throw new Error("Results are not grouped")

    if (this.state.selectedRows == null || this.state.resultFindOptions == null)
      return Promise.resolve([]);

    var resFO = this.state.resultFindOptions;
    var filters = this.state.selectedRows.map(row => SearchControlLoaded.getGroupFilters(row, this.state.resultTable!, resFO));
    return Promise.all(filters.map(fs => Finder.fetchLites({ queryName: resFO.queryKey, filterOptions: fs, orderOptions: [], count: null })))
      .then(fss => fss.flatMap(fs => fs));
  }

  // SELECT BUTTON

  handleSelectedToggle = (isOpen: boolean) => {
    this.setState({ isSelectOpen: isOpen }, () => {
      if (this.state.isSelectOpen && this.state.currentMenuItems == undefined)
        this.loadMenuItems();
    });
  }

  loadMenuItems() {
    var cm = this.props.showContextMenu(this.state.resultFindOptions ?? this.props.findOptions);
    if (cm == "Basic")
      this.setState({ currentMenuItems: [] });
    else {

      var litesPromise = !this.props.findOptions.groupResults ? Promise.resolve(this.getSelectedEntities()) : this.getGroupedSelectedEntities();

      litesPromise
        .then(lites => renderContextualItems({
          lites: lites,
          queryDescription: this.props.queryDescription,
          markRows: this.markRows,
          container: this,
          styleContext: this.props.styleContext,
        }))
        .then(menuItems => this.setState({ currentMenuItems: menuItems }));
    }
  }

  markRows = (dic: MarkedRowsDictionary) => {
    this.dataChanged()
      .then(() => this.setMarkedRows(dic));
  }

  setMarkedRows(dic: MarkedRowsDictionary){
    this.setState({ markedRows: { ...this.state.markedRows, ...dic } as MarkedRowsDictionary })
  }

  renderSelectedButton(): ButtonBarElement | null {

    if (this.state.selectedRows == undefined)
      return null;

    const title = <>
      <span className="d-none d-sm-inline">{JavascriptMessage.Selected.niceToString()}</span>
      {" (" + this.state.selectedRows!.length + ")"}
    </>;

    return {
      order: -1,
      button:
        <Dropdown
          show={this.state.isSelectOpen}
          onToggle={this.handleSelectedToggle}>
          <Dropdown.Toggle id="selectedButton" variant="light" className="sf-query-button sf-tm-selected ms-2" disabled={this.state.selectedRows!.length == 0}>
            {title}
          </Dropdown.Toggle>
          <Dropdown.Menu>
            {this.state.currentMenuItems == undefined ? <Dropdown.Item className="sf-tm-selected-loading">{JavascriptMessage.loading.niceToString()}</Dropdown.Item> :
              this.state.currentMenuItems.length == 0 ? <Dropdown.Item className="sf-search-ctxitem-no-results">{JavascriptMessage.noActionsFound.niceToString()}</Dropdown.Item> :
                this.state.currentMenuItems.map((e, i) => React.cloneElement(e, { key: i }))}
          </Dropdown.Menu>
        </Dropdown>
    };
  }

  // CONTEXT MENU

  handleContextOnHide = () => {
    this.setState({ contextualMenu: undefined });
  }


  handleQuickFilter = async () => {
    const cm = this.state.contextualMenu!;
    const fo = this.props.findOptions;

    const token = fo.columnOptions[cm.columnIndex!].token;
    let newToken = token;
    let op: FilterOperation | undefined;

    var toArray = hasToArray(token);
    if (toArray) {
      newToken = await Finder.parseSingleToken(fo.queryKey, token!.fullKey.split(".").map(p => p == toArray!.key ? "Any" : p).join("."), SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement);
      op = "IsIn";
    }
    else {
      op = token?.preferEquals || cm.rowIndex != null ? "EqualTo" as FilterOperation | undefined :
        token ? (filterOperations[token.filterType as any] || []).firstOrNull() as FilterOperation | undefined :
          undefined as FilterOperation | undefined;
    }

    const rt = this.state.resultTable;

    let value = cm.rowIndex == undefined || rt == null || token == null ? (op == "IsIn" ? [] : undefined) : rt.rows[cm.rowIndex].columns[rt.columns.indexOf(token.fullKey)]

    if (token?.filterType == "Embedded" && value != null) {
      value = null;
      op = "DistinctTo";
    }

    fo.filterOptions.push({
      token: newToken!,
      operation: op,
      value: value,
      frozen: false
    });

    if (!this.state.showFilters)
      this.setState({ showFilters: true });

    this.handleFiltersChanged();

    this.forceUpdate(() => this.handleHeightChanged());
  }

  handleInsertColumn = () => {

    const token = withoutAllAny(this.state.lastToken);

    const newColumn: ColumnOptionParsed = {
      token: token,
      displayName: token?.niceName,
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
    const cm = this.state.contextualMenu!;
    const fo = this.props.findOptions;
    const col = fo.columnOptions[cm.columnIndex!];
    fo.columnOptions.removeAt(cm.columnIndex!);
    if (fo.groupResults && col.token) {
      fo.orderOptions.extract(a => a.token.fullKey == col.token!.fullKey);
    }

    this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());
  }

  handleGroupByThisColumn = async () => {
    const cm = this.state.contextualMenu!;
    const fo = this.props.findOptions;

    const col = fo.columnOptions[cm.columnIndex!];

    fo.columnOptions.clear();

    var defAggregate = this.props.querySettings?.defaultAggregates;

    var parsedTokens: QueryToken[] = [];
    if (defAggregate) {
      parsedTokens = await Finder.API.parseTokens(fo.queryKey, [
        ...defAggregate.map(a => ({ token: a.token.toString(), options: SubTokensOptions.CanAggregate })),
        ...defAggregate.filter(a => a.summaryToken != null).map(a => ({ token: a.summaryToken!.toString(), options: SubTokensOptions.CanAggregate }))
      ].distinctBy(a => a.token));

      var ptDic = parsedTokens.toObject(a => a.fullKey);

      fo.columnOptions.push(...defAggregate.map(t => ({
        token: ptDic[t.token.toString()],
        summaryToken: t.summaryToken != null ? ptDic[t.summaryToken!.toString()] : undefined,
        displayName: (typeof t.displayName == "function" ? t.displayName() : t.displayName) ?? ptDic[t.token.toString()].niceName,
        hiddenColumn: t.hiddenColumn,
      })));
    }
    else {
      parsedTokens = await Finder.API.parseTokens(fo.queryKey, [
        { token: "Count", options: SubTokensOptions.CanAggregate }
      ]);
      fo.columnOptions.push({ token: parsedTokens[0], displayName: parsedTokens[0].niceName });
    }

    fo.columnOptions.push(col);
    fo.groupResults = true;
    fo.orderOptions.clear();
    fo.orderOptions.push(...parsedTokens.map(t => softCast<OrderOptionParsed>({ token: t, orderType: "Descending" })));

    this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());

    if (this.props.searchOnLoad)
      this.doSearchPage1();
  }

  handleRestoreDefaultColumn = () => {
    const cm = this.state.contextualMenu!;
    const fo = this.props.findOptions;
    
    const col = fo.columnOptions[cm.columnIndex!];
    fo.columnOptions.clear();
    fo.columnOptions.push(...Dic.getValues(this.props.queryDescription.columns).filter(a => a.name != "Entity").map(cd => softCast<ColumnOptionParsed>({ displayName: cd.displayName, token: toQueryToken(cd) })));
    if (fo.groupResults) {
      fo.orderOptions.clear();
    }
    fo.groupResults = false;

    this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());

    if (this.props.searchOnLoad)
      this.doSearchPage1();
  }

  renderContextualMenu() {

    const cm = this.state.contextualMenu!;
    const p = this.props;

    var fo = this.props.findOptions;
    function isColumnFilterable(columnIndex: number) {
      var token = fo.columnOptions[columnIndex].token;
      return token && token.filterType != undefined && token.format != "Password";
    }

    function isColumnGroupable(columnIndex: number) {
      var token = fo.columnOptions[columnIndex].token;
      return token && !hasOperation(token) && !hasToArray(token);
    }

    const menuItems: React.ReactElement<any>[] = [];
    if (this.canFilter() && cm.columnIndex != null && isColumnFilterable(cm.columnIndex))
      menuItems.push(<Dropdown.Item className="sf-quickfilter-header" onClick={this.handleQuickFilter}>
        <FontAwesomeIcon icon="filter" className="icon" />&nbsp;{JavascriptMessage.addFilter.niceToString()}
      </Dropdown.Item>);

    if (cm.rowIndex == undefined && p.allowChangeColumns) {

      if (menuItems.length)
        menuItems.push(<Dropdown.Divider />);

      if (cm.columnIndex != null) {
        menuItems.push(<Dropdown.Item className="sf-insert-column" onClick={this.handleInsertColumn}>
          <span className="fa-layers fa-fw icon">
            <FontAwesomeIcon icon="table-columns" transform="left-2" color="gray" />
            <FontAwesomeIcon icon="square-plus" transform="shrink-4 up-8 right-8" color="#008400" />
          </span>&nbsp;{JavascriptMessage.insertColumn.niceToString()}
        </Dropdown.Item>);

        menuItems.push(<Dropdown.Item className="sf-edit-column" onClick={this.handleEditColumn}><span className="fa-layers fa-fw icon">
          <FontAwesomeIcon icon="table-columns" transform="left-2" color="gray" />
          <FontAwesomeIcon icon="square-pen" transform="shrink-4 up-8 right-8" color="orange" />
        </span>&nbsp;{JavascriptMessage.editColumn.niceToString()}
        </Dropdown.Item>);

        menuItems.push(<Dropdown.Item className="sf-remove-column" onClick={this.handleRemoveColumn}><span className="fa-layers fa-fw icon">
          <FontAwesomeIcon icon="table-columns" transform="left-2" color="gray" />
          <FontAwesomeIcon icon="square-minus" transform="shrink-4 up-8 right-9" color="#ca0000" />
        </span>&nbsp;{JavascriptMessage.removeColumn.niceToString()}
        </Dropdown.Item>);

        menuItems.push(<Dropdown.Divider />);

        if (p.showGroupButton && isColumnGroupable(cm.columnIndex))
          menuItems.push(<Dropdown.Item className="sf-group-by-column" onClick={this.handleGroupByThisColumn}><span className="fa-layers fa-fw icon">
            <FontAwesomeIcon icon="table-columns" transform="left-2" color="gray" />
            <FontAwesomeIcon icon="layer-group" transform="shrink-4 up-8 right-8" color="#21618C" />
          </span>&nbsp;{JavascriptMessage.groupByThisColumn.niceToString()}
          </Dropdown.Item>);
      }

      menuItems.push(<Dropdown.Item className="sf-restore-default-columns" onClick={this.handleRestoreDefaultColumn}><span className="fa-layers fa-fw icon">
        <FontAwesomeIcon icon="table-columns" transform="left-2" color="gray" />
        <FontAwesomeIcon icon="rotate-left" transform="shrink-4 up-8 right-8" color="black" />
      </span>&nbsp;{JavascriptMessage.restoreDefaultColumns.niceToString()}
      </Dropdown.Item>);

      if (fo.columnOptions.some(a => a.hiddenColumn == true)) {
        menuItems.push(<Dropdown.Divider />);



        if (this.state.showHiddenColumns) {
          menuItems.push(<Dropdown.Item className="sf-hide-hidden-columns" onClick={() => this.setState({ showHiddenColumns : undefined })}>
            <FontAwesomeIcon icon="eye-slash" color="#21618C" />&nbsp;{SearchMessage.HideHiddenColumns.niceToString()}
          </Dropdown.Item>);
        } else {
          menuItems.push(<Dropdown.Item className="sf-show-hidden-columns" onClick={() => this.setState({ showHiddenColumns: true })}>
            <FontAwesomeIcon icon="eye" color="#21618C" />&nbsp;{SearchMessage.ShowHiddenColumns.niceToString()}
          </Dropdown.Item>);
        }
    }
    }

    if (cm.rowIndex != undefined) {

      menuItems.push(<Dropdown.Item className="sf-paste-menu-item" onClick={() => this.handleCopyClick()}>
        <FontAwesomeIcon icon="copy" className="icon" color="#21618C" />&nbsp;{SearchMessage.Copy.niceToString()}
      </Dropdown.Item>);

      if (this.state.currentMenuItems == undefined) {
        menuItems.push(<Dropdown.Header>{JavascriptMessage.loading.niceToString()}</Dropdown.Header>);
      } else {
        if (menuItems.length && this.state.currentMenuItems.length)
          menuItems.push(<Dropdown.Divider />);

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

  handleCopyClick() {
    const supportsClipboard = (navigator.clipboard && window.isSecureContext);
    if (!supportsClipboard)
      return;

    const text = this.state.selectedRows!.filter(r => !!r.entity)
      .map(r => liteKey(r.entity!))
      .join("|");

    navigator.clipboard.writeText(text);
  }
  
  //SELECTED ROWS

  allSelected() {
    return this.state.resultTable != undefined && this.state.resultTable.rows.length != 0 && this.state.resultTable.rows.length == this.state.selectedRows!.length;
  }

  handleToggleAll = () => {

    if (!this.state.resultTable)
      return;

    this.setState({
      selectedRows: !this.allSelected() ? this.state.resultTable!.rows.clone() : [],
      currentMenuItems: undefined,
    }, () => {
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

    var rt = this.state.summaryResultTable;
    var scl = this;

    function getSummary(summaryToken: QueryToken | undefined ) {

      if (rt == null || summaryToken == undefined)
        return null;

      var colIndex = rt.columns.indexOf(summaryToken.fullKey);

      if (colIndex == -1)
        return null;

      const val = rt.rows[0].columns[colIndex];

      var formatter = Finder.getCellFormatter(scl.props.querySettings, summaryToken, scl);

      var prefix =
        summaryToken.key == "Sum" ? "Σ" :
          summaryToken.toStr;

      return (
        <div className={formatter.cellClass}>
          <span className="text-muted me-1">{prefix}</span>

          {formatter.formatter(val, {
          columns: rt.columns,
          row: rt.rows[0],
          rowIndex: 0,
          refresh: () => scl.dataChanged(),
          systemTime: scl.props.findOptions.systemTime
        }, summaryToken)}</div>
      );
    }

    var rootKeys = !this.props.findOptions.groupResults ? [] : getRootKeyColumn(this.props.findOptions.columnOptions.filter(co => co.token && co.token.queryTokenType != "Aggregate" && !hasToArray(co.token)));

    function isDerivedKey(token: QueryToken | undefined) : boolean {
      if (token == null)
        return false;

      if (rootKeys.some(cop => cop.token!.fullKey == token!.fullKey))
        return true;

      return isDerivedKey(token!.parent);
    }

    function isNotDerivedToArray(token: QueryToken) {
      var toArray = hasToArray(token);

      return toArray != null && !isDerivedKey(toArray.parent);
    }

    var qs = this.props.querySettings;
    var fillWidths = this.props.findOptions.columnOptions.map(co => (co.token != null && ((this.props.formatters && this.props.formatters[co.token.fullKey]) || Finder.getCellFormatter(qs, co.token, this)).fillWidth));

    var allSmall = fillWidths.every(a => a == false);

    return (
      <tr>
        {this.props.allowSelection && <th className="sf-small-column sf-th-selection">
          {this.props.allowSelection == true &&
            <input type="checkbox" className="form-check-input" id="cbSelectAll" onChange={this.handleToggleAll} checked={this.allSelected()} />
          }
        </th>
        }
        {(this.props.view || this.props.findOptions.groupResults) && <th className="sf-small-column sf-th-entity" data-column-name="Entity">{Finder.Options.entityColumnHeader()}</th>}
        {this.props.findOptions.columnOptions.filter(co => !co.hiddenColumn || this.state.showHiddenColumns).map((co, i) =>
          <th key={i}
            draggable={true}
            className={classes(
              fillWidths[i] ? undefined : "sf-small-column",
              i == this.state.dragColumnIndex && "sf-draggin",
              co == this.state.editingColumn && "sf-current-column",
              co.hiddenColumn && "sf-hidden-column",
              !this.canOrder(co) && "noOrder",
              co.token && co.token.type.isCollection && "error",
              co.token && this.props.findOptions.groupResults && isNotDerivedToArray(co.token)  && "error", 
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
            {getSummary(co.summaryToken)}
            <div className="d-flex" style={{ alignItems: "center" }}>
              {this.orderIcon(co)}
              {this.props.findOptions.groupResults && co.token && co.token.queryTokenType != "Aggregate" && <span>
                <FontAwesomeIcon icon="key" className="me-1"
                  color={rootKeys.contains(co) ? "gray" : "lightgray"}
                  title={rootKeys.contains(co) ? SearchMessage.GroupKey.niceToString() : SearchMessage.DerivedGroupKey.niceToString()  } /></span>}
              {co.displayName}
            </div>
          </th>
        )}
        {allSmall && <th></th>}
      </tr>
    );
  }

  canOrder(column: ColumnOptionParsed) {
    if (!column.token || !this.props.allowChangeOrder)
      return false;

    const t = column.token;

    if (t.type.isCollection)
      return false;

    if (hasToArray(t))
      return false;

    if (t.type.isEmbedded || isTypeModel(t.type.name) || t.type.name == "CellOperationDTO")
      return t.hasOrderAdapter == true;

    return true;
  }

  orderIcon(column: ColumnOptionParsed) {

    if (column.token == undefined)
      return "";

    const orders = this.props.findOptions.orderOptions;

    const o = orders.filter(a => a.token.fullKey == column.token!.fullKey).firstOrNull();
    if (o == undefined)
      return "";


    let asc = (o.orderType == "Ascending" ? "asc" : "desc");

    if (orders.indexOf(o))
      asc += " l" + orders.indexOf(o);

    return <span className={"me-1 sf-header-sort " + asc} />;
  }

  //ROWS

  handleChecked = (event: React.ChangeEvent<HTMLInputElement>) => {

    const cb = event.currentTarget;

    const index = parseInt(cb.getAttribute("data-index")!);

    const row = this.state.resultTable!.rows[index];

    var selectedRows = this.state.selectedRows!;

    if (cb.checked) {
      if (this.props.allowSelection == "single") {
        selectedRows.clear();
        selectedRows.push(row);
      } else {
        if (!selectedRows.contains(row))
          selectedRows.push(row);
      }
    } else {
      selectedRows.remove(row);
    }

    this.notifySelectedRowsChanged();

    this.setState({ currentMenuItems: undefined });
  }

  static getGroupFilters(row: ResultRow, resTable: ResultTable, resFo: FindOptionsParsed): FilterOption[] {

    var rootKeys = getRootKeyColumn(resFo.columnOptions.filter(co => co.token && co.token.queryTokenType != "Aggregate"));

    var keyFilters = resFo.columnOptions
      .map(col => ({ col, value: row.columns[resTable.columns.indexOf(col.token!.fullKey)] }))
      .filter(a => rootKeys.contains(a.col))
      .map(a => ({ token: a.col.token!.fullKey, operation: "EqualTo", value: a.value }) as FilterOption);

    var originalFilters = toFilterOptions(resFo.filterOptions.filter(f => !isAggregate(f)));

    return [...originalFilters, ...keyFilters];
  }

  openRowGroup(row: ResultRow) {

    var resFo = this.state.resultFindOptions!;

    var extraColumns = resFo.columnOptions.map(a =>
      a.token == null ? null :
        a.token.queryTokenType == "Aggregate" ? (!a.token.parent ? null : ({ token: a.token.parent.fullKey, summaryToken: a.token.fullKey }) as ColumnOption) :
          ({ token: a.token.fullKey }) as ColumnOption)
      .notNull();

    var filters = SearchControlLoaded.getGroupFilters(row, this.state.resultTable!, resFo);

    const fo = ({
      queryName: resFo.queryKey,
      filterOptions: filters,
      columnOptions: extraColumns,
      columnOptionsMode: "ReplaceOrAdd",
      systemTime: resFo.systemTime && { ...resFo.systemTime },
      includeDefaultFilters: false,
    } as FindOptions);

    if (this.customDrilldowns.length > 0 && SearchControlLoaded.onCustomDrilldown)
      return SearchControlLoaded.onCustomDrilldown(this.customDrilldowns, { fo, onReload: () => this.dataChanged() });

    return Finder.explore(fo).then(() => {
      this.dataChanged();
    });
  }

  handleDoubleClick = (e: React.MouseEvent<any>, row: ResultRow, columns: string[]) => {

    //if ((e.target as HTMLElement).parentElement != e.currentTarget) //directly in the td
    //  return;

    if (this.props.onDoubleClick) {
      e.preventDefault();
      this.props.onDoubleClick(e, row, this);
      return;
    }

    var qs = this.props.querySettings;
    if (qs?.onDoubleClick) {
      e.preventDefault();
      qs.onDoubleClick(e, row, columns, this);
      return;
    }

    var resFo = this.state.resultFindOptions;
    if (resFo?.groupResults) {

      this.openRowGroup(row);

      return;
    }

    if (this.props.view) {
      var lite = row.entity!;

      if (!Navigator.isViewable(lite.EntityType, { isSearch: true }))
        return;

      e.preventDefault();

      const s = Navigator.getSettings(lite.EntityType);

      const qs = this.props.querySettings;

      const getViewPromise = this.props.getViewPromise ?? qs?.getViewPromise;

      const isWindowsOpen = e.button == 1 || e.ctrlKey;

      if (this.customDrilldowns.length > 0 && SearchControlLoaded.onCustomDrilldown)
        return SearchControlLoaded.onCustomDrilldown(this.customDrilldowns, { openInNewTab: isWindowsOpen || s?.avoidPopup, showInPlace: this.props.view == "InPlace", entity: lite, onReload: () => this.doSearch({}) });

      if (isWindowsOpen || s?.avoidPopup || this.props.view == "InPlace") {
        var vp = getViewPromise && getViewPromise(null);
        var url = Navigator.navigateRoute(lite, vp && typeof vp == "string" ? vp : undefined);
        if (this.props.view == "InPlace" && !isWindowsOpen)
          AppContext.history.push(url);
        else
          window.open(url);
      }
      else {
        Navigator.view(lite, { getViewPromise: getViewPromise, buttons: "close" })
          .then(() => {
            this.handleOnNavigated(lite);
          });
      }
    }
  }

  joinNodes(values: (React.ReactChild | undefined)[], separator: React.ReactChild) {

    if (values.length > (SearchControlLoaded.maxToArrayElements - 1))
      values = [...values.filter((a, i) => i < SearchControlLoaded.maxToArrayElements - 1), "…"];

    return React.createElement(React.Fragment, undefined,
      ...values.flatMap((v, i) => i == values.length - 1 ? [v] : [v, separator])
    );
  }

  getColumnOptions() {
    return this.props.findOptions.columnOptions.filter(co => !co.hiddenColumn || this.state.showHiddenColumns);
  }

  getColumnOptionsParsed() {
    const columnOptions = this.getColumnOptions();
    const resultColumns = this.state.resultTable!.columns;
    const qs = this.props.querySettings;

    return columnOptions.map(co => ({
      column: co,
      hasToArray: hasToArray(co.token),
      cellFormatter: (co.token && ((this.props.formatters && this.props.formatters[co.token.fullKey]) || Finder.getCellFormatter(qs, co.token, this))),
      resultIndex: co.token == undefined ? -1 : resultColumns.indexOf(co.token.fullKey)
    }));
  }

  getNoResultsElement() {
    const resultTable = this.state.resultTable!;
    const resFO = this.state.resultFindOptions!;

    if (resultTable.rows.length == 0) {
      if (resultTable.totalElements == 0 || this.props.showFooter == false || resFO.pagination.mode != "Paginate")
        return SearchMessage.NoResultsFound.niceToString();
      else
        return SearchMessage.NoResultsFoundInPage01.niceToString().formatHtml(
          resFO.pagination.currentPage,
          <a href="#" onClick={e => {
            e.preventDefault();
            this.handlePagination({
              mode: "Paginate",
              elementsPerPage: resFO.pagination.elementsPerPage,
              currentPage: 1
            });
          }}>{SearchMessage.GoBackToPageOne.niceToString()}</a>
        );
    }

    return undefined;
  }

  getRowAttributes(resultRow: ResultRow) {
    const qs = this.props.querySettings;
    const resultColumns = this.state.resultTable!.columns;
    const rowAttributes = this.props.rowAttributes ?? qs?.rowAttributes;

    return rowAttributes ? rowAttributes(resultRow, resultColumns) : undefined;
  }

  getEntityFormatter() {
    const qs = this.props.querySettings;
    return this.props.entityFormatter ?? (qs?.entityFormatter) ?? Finder.entityFormatRules.filter(a => a.isApplicable(this)).last("EntityFormatRules").formatter;
  }

  hasEntityColumn() {
    return this.props.findOptions.groupResults || this.props.view;
  }

  getRowMarked(row: ResultRow) {
    return row.entity && this.getMarkedRow(row.entity);
  }

  getColumnElement(row: ResultRow, rowIndex: number, c: ColumnParsed) {
    const resultColumns = this.state.resultTable!.columns;

    const fctx: Finder.CellFormatterContext = {
      refresh: () => this.dataChanged(),
      systemTime: this.props.findOptions.systemTime,
      columns: resultColumns,
      row: row,
      rowIndex: rowIndex,
    };

    return c.resultIndex == -1 || c.cellFormatter == undefined ? undefined :
      c.hasToArray != null ? this.joinNodes((row.columns[c.resultIndex] as unknown[]).map(v => c.cellFormatter!.formatter(v, fctx, c.column.token!)),
        c.hasToArray.key == "SeparatedByComma" || c.hasToArray.key == "SeparatedByCommaDistict" ? <span className="text-muted">, </span> : <br />) :
        c.cellFormatter.formatter(row.columns[c.resultIndex], fctx, c.column.token!);
  }

  renderRows(): React.ReactNode {
    const columnOptions = this.getColumnOptions();
    const columnsCount = columnOptions.length +
      (this.props.allowSelection ? 1 : 0) +
      (this.props.view ? 1 : 0);

    const resultTable = this.state.resultTable;
    if (!resultTable) {
      if (this.props.findOptions.pagination.mode == "All" && this.props.showFooter)
        return <tr><td colSpan={columnsCount} className="text-danger">{SearchMessage.ToPreventPerformanceIssuesAutomaticSearchIsDisabledCheckYourFiltersAndThenClickSearchButton.niceToString()}</td></tr>;

      return <tr><td colSpan={columnsCount}>{JavascriptMessage.searchForResults.niceToString()}</td></tr>;
    }

    var noResultsElement = this.getNoResultsElement();
    if (noResultsElement != null) 
      return <tr><td colSpan={columnsCount}>{noResultsElement}</td></tr>;

    const entityFormatter = this.getEntityFormatter();
    const columns = this.getColumnOptionsParsed();

    return resultTable.rows.map((row, i) => {
      const mark = this.getRowMarked(row);
      const markClassName = mark?.status == "Success" ? "sf-entity-ctxmenu-success" :
        mark?.status == "Warning" ? "table-warning" :
          mark?.status == "Error" ? "table-danger" :
          mark?.status == "Muted" ? "text-muted" :
            undefined;

      var ra = this.getRowAttributes(row);

      var tr = (
        <tr key={i} data-row-index={i} data-entity={row.entity && liteKey(row.entity)}
          onDoubleClick={e => this.handleDoubleClick(e, row, resultTable.columns)}
          {...ra}
          className={classes(markClassName, ra?.className)}>
          {this.props.allowSelection &&
            <td className="centered-cell">
              <input type="checkbox" className="sf-td-selection form-check-input" checked={this.state.selectedRows!.contains(row)} onChange={this.handleChecked} data-index={i} />
            </td>
          }

          {this.hasEntityColumn() &&
            <td className={entityFormatter.cellClass}>
              {entityFormatter.formatter(row, resultTable.columns, this)}
            </td>
          }

          {
            columns.map((c, j) =>
              <td key={j} data-column-index={j} className={c.cellFormatter && c.cellFormatter.cellClass}>
                {this.getColumnElement(row, i, c)}
              </td>
            )
          }
        </tr>
      );

      const message = mark?.message;
      if (!message)
        return tr;

      return (
        <OverlayTrigger
          overlay={<Tooltip placement="bottom" id={"result_row_" + i + "_tooltip"} style={{ "--bs-tooltip-max-width": "100%" } as any}>
            {message.split("\n").map((s, i) => <p key={i}>{s}</p>)}
          </Tooltip>}>
          {tr}
        </OverlayTrigger>
      );
    });
  }

  getRowMarketIcon(row: ResultRow, rowIndex: number) {
    const mark = this.getRowMarked(row);
    if (!mark)
      return undefined;

    const markIcon: IconProp =
      mark.status == "Success" ? "check-circle" :
        mark.status == "Warning" ? "exclamation-circle" :
          mark.status == "Error" ? "times-circle" :
            mark.status == "Muted" ? "xmark" : null!;

    const markIconColor: string =
      mark.status == "Success" ? "green" :
        mark.status == "Warning" ? "orange" :
          mark.status == "Error" ? "red" : 
            mark.status == "Muted" ? "gray" : null!;
        
    const icon = <span><FontAwesomeIcon icon={markIcon} color={markIconColor} /></span>;

    return (
      <span className="row-mark-icon">
        {mark.message ?
          <OverlayTrigger
            trigger="click"
            overlay={<Tooltip placement="bottom" id={"result_row_" + rowIndex + "_tooltip"}>{mark.message.split("\n").map((s, i) => <p key={i}>{s}</p>)}</Tooltip>}>
            {icon}
          </OverlayTrigger> : icon}
      </span>
    );
  }

  renderRowsMobile() {
    const resultTable = this.state.resultTable;
    if (!resultTable) {
      if (this.props.findOptions.pagination.mode == "All" && this.props.showFooter)
        return <div className="text-danger">{SearchMessage.ToPreventPerformanceIssuesAutomaticSearchIsDisabledCheckYourFiltersAndThenClickSearchButton.niceToString()}</div>;

      return <div>{JavascriptMessage.searchForResults.niceToString()}</div>;
    }

    var noResultsElement = this.getNoResultsElement();
    if (noResultsElement != null)
      return <div>{noResultsElement}</div>;

    const entityFormatter = this.getEntityFormatter();
    const columns = this.getColumnOptionsParsed();

    return resultTable.rows.map((row, i) => {
      const markIcon = this.getRowMarketIcon(row, i);
      var ra = this.getRowAttributes(row);

      var div = (
        <div key={i} data-row-index={i} data-entity={row.entity && liteKey(row.entity)}
          onDoubleClick={e => this.handleDoubleClick(e, row, resultTable.columns)}
          {...ra}
          className={classes("row-container", ra?.className)}>
          {(this.props.allowSelection || this.hasEntityColumn()) &&
            <div className="row-data row-header">
              {this.props.allowSelection &&
                <span className="row-selection">
                  <input type="checkbox" className="sf-td-selection form-check-input" checked={this.state.selectedRows!.contains(row)} onChange={this.handleChecked} data-index={i} />
                </span>
              }

              {this.hasEntityColumn() &&
                <span className={classes("row-entity", entityFormatter.cellClass)}>
                  {entityFormatter.formatter(row, resultTable.columns, this)}
                </span>
              }

              {markIcon}
            </div>}

          {
            columns.map((c, j) => {
              const isHeader = !(this.props.allowSelection || this.hasEntityColumn()) && j == 0;
              return (
                <div key={j} className={classes("row-data", isHeader && "row-header")}>
                  {<span className="row-title">{c.column.displayName}</span>}
                  <span data-column-index={j} className={classes("row-value", c.cellFormatter && c.cellFormatter.cellClass)}>
                    {this.getColumnElement(row, i, c)}
                  </span>
                  {isHeader && markIcon}
                </div>
              );
            })
          }
        </div>
      );

      return div;
    });
  }

  renderPinnedFilters(extraSmall: boolean = false): React.ReactNode {

    const fo = this.props.findOptions;

    return (
      <AutoFocus disabled={!this.props.enableAutoFocus}>
        <PinnedFilterBuilder
          filterOptions={fo.filterOptions}
          pinnedFilterVisible={this.props.pinnedFilterVisible}
          onFiltersChanged={this.handlePinnedFilterChanged}
          onSearch={() => this.doSearchPage1(true)}
          showSearchButton={this.state.refreshMode == "Manual" && this.props.showHeader != true}
          extraSmall={extraSmall}
        />
      </AutoFocus>
    );
  }
  
  handleOnNavigated = (lite: Lite<Entity>) => {

    if (this.props.onNavigated)
      this.props.onNavigated(lite);

    this.dataChanged();
  }

  getMarkedRow(entity: Lite<Entity>): MarkedRow | undefined {

    if (!entity || !this.state.markedRows)
      return undefined;

    const m = this.state.markedRows[liteKey(entity)];

    if (typeof m === "string") {
      if (m == "")
        return { status: "Success", message: undefined };
      else
        return { status: "Error", message: m };
    }
    else {
      return m;
    }
  }

  getSelectedValue<T = unknown>(token: QueryTokenString<T> | string, automaticEntityPrefix = true): Finder.AddToLite<T> | undefined {

    var result = this.tryGetSelectedValue(token, automaticEntityPrefix, true);

    return result!.value;
  }

  tryGetSelectedValue<T = unknown>(token: QueryTokenString<T> | string, automaticEntityPrefix = true, throwError = false): { value: Finder.AddToLite<T> | undefined } | undefined {

    const tokenName = token.toString();

    const sc = this;
    const colIndex = sc.state.resultTable!.columns.indexOf(tokenName);
    if (colIndex != -1 && sc.state.selectedRows && sc.state.selectedRows?.length > 0) {
      if (sc.state.selectedRows!.length == 1) {

        const row = sc.state.selectedRows!.first();

        const val = row.columns[colIndex];
        return { value: val };
      } else {

        var distinctValues = sc.state.selectedRows!.map(r => r.columns[colIndex]).distinctBy(s => Finder.Encoder.stringValue(s));

        if (distinctValues.length > 1) {
          if (throwError) {
            const co = sc.state.resultFindOptions!.columnOptions.single(co => co.token?.fullKey == tokenName);
            throw new Error(SearchMessage.MoreThanOne0Selected.niceToString(co.token?.niceName));
          }
          else {
            return undefined;
          }
        }

        return { value: distinctValues[0] };
      }
    }

    var filter = sc.props.findOptions.filterOptions.firstOrNull(a => !isFilterGroupOptionParsed(a) && isActive(a) && a.token?.fullKey == tokenName && a.operation == "EqualTo");
    if (filter != null)
      return { value: filter?.value };

    if (automaticEntityPrefix) {
      var result = this.tryGetSelectedValue(tokenName.startsWith("Entity.") ? tokenName.after("Entity.") : "Entity." + tokenName, false, false);
      if (result != null)
        return result as any;
    }

    if (throwError)
      throw new Error(`No column '${token}' found`);

    return undefined;
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

      if (a.displayName && a.token)
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


function getRootKeyColumn(columnOptions: ColumnOptionParsed[]): ColumnOptionParsed[] {
  return columnOptions.filter(t => t.token != null && !columnOptions.some(root => root.token != null && dominates(root.token, t.token!)));
}


function dominates(root: QueryToken, big: QueryToken) {

  if (hasElement(big)) {

    if (!hasElement(root))
      return false;

    var elemBig = getTokenParents(big).last(a => a.queryTokenType == "Element");
    var elemRoot = getTokenParents(root).last(a => a.queryTokenType == "Element");

    if (elemBig.fullKey != elemRoot.fullKey)
      return false;
  }

  return big.fullKey.startsWith(root.fullKey + ".")

}
