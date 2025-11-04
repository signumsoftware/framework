import * as React from 'react'
import { DateTime } from 'luxon'
import { DomUtils, classes, Dic, softCast, isNumber } from '../Globals'
import { Finder } from '../Finder'
import {
  ResultTable, ResultRow, FindOptionsParsed, FilterOption, FilterOptionParsed, QueryDescription, ColumnOption, ColumnOptionParsed,
  Pagination, OrderOptionParsed, SubTokensOptions, filterOperations, QueryToken, QueryRequest, isActive,
  hasOperation, hasToArray, hasElement, getTokenParents, FindOptions, isFilterCondition, hasManual,
  withoutPinned
} from '../FindOptions'
import { SearchMessage, JavascriptMessage, Lite, liteKey, Entity, ModifiableEntity, EntityPack, FrameMessage, is } from '../Signum.Entities'
import { tryGetTypeInfos, TypeInfo, isTypeModel, getTypeInfos, QueryTokenString, getQueryNiceName, isNumberType, getTypeInfo } from '../Reflection'
import { Navigator, ViewPromise } from '../Navigator'
import * as AppContext from '../AppContext';
import { AbortableRequest } from '../Services'
import { Constructor } from '../Constructor'
import * as Hooks from '../Hooks'
import PaginationSelector from './PaginationSelector'
import FilterBuilder from './FilterBuilder'
import ColumnEditor, { columnError, columnSummaryError } from './ColumnEditor'
import MultipliedMessage, { multiplyResultTokens } from './MultipliedMessage'
import GroupByMessage from './GroupByMessage'
import { renderContextualItems, ContextualItemsContext, ContextualMenuItem, MarkedRowsDictionary, MarkedRow, SearchableMenuItem, ContextMenuPack } from './ContextualItems'
import ContextMenu, { ContextMenuPosition, getMouseEventPosition } from './ContextMenu'
import SelectorModal from '../SelectorModal'
import { ISimpleFilterBuilder } from './SearchControl'
import { FilterOperation, PaginationMode, RefreshMode, SystemTimeMode } from '../Signum.DynamicQuery';
import SystemTimeEditor from './SystemTimeEditor';
import { Property } from 'csstype';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import "./Search.css"
import "./SearchMobile.css"
import PinnedFilterBuilder from './PinnedFilterBuilder';
import { AutoFocus } from '../Components/AutoFocus';
import { ButtonBarElement, StyleContext } from '../TypeContext';
import { Button, ButtonGroup, Dropdown, DropdownButton, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { getBreakpoint, Breakpoints, useForceUpdate, useAPI } from '../Hooks'
import { IconDefinition, IconProp } from '@fortawesome/fontawesome-svg-core'
import { similarToken } from '../Search'
import { SearchHelp } from './SearchControlVisualTips'
import { VisualTipIcon } from '../Basics/VisualTipIcon'
import { SearchVisualTip, TypeEntity } from '../Signum.Basics'
import { KeyNames } from '../Components'
import { CollectionMessage } from '../Signum.External'
import { LinkButton } from '../Basics/LinkButton'
import { AccessibleTable } from '../Basics/AccessibleTable'

export interface ColumnParsed {
  column: ColumnOptionParsed;
  columnIndex: number;
  hasToArray?: QueryToken;
  cellFormatter?: Finder.CellFormatter;
  resultIndex: number | "Entity";
}

export type SearchControlViewMode = "Mobile" | "Standard";

export interface SearchControlMobileOptions {
  showSwitchViewModesButton: boolean;
  defaultViewMode: SearchControlViewMode;
}

export interface ShowBarExtensionOption { }

export interface OnDrilldownOptions {
  openInNewTab?: boolean;
  showInPlace?: boolean;
  onReload?: () => void;
}

export interface SearchControlLoadedProps {
  findOptions: FindOptionsParsed;
  queryDescription: QueryDescription;
  querySettings: Finder.QuerySettings | undefined;

  formatters?: { [token: string]: Finder.CellFormatter };
  rowAttributes?: (row: ResultRow, searchControl: SearchControlLoaded) => React.HTMLAttributes<HTMLTableRowElement> | undefined;
  entityFormatter?: Finder.EntityFormatter;
  selectionFormatter?: (searchControl: SearchControlLoaded, row: ResultRow, rowIndex: number) => React.ReactElement | undefined;
  extraButtons?: (searchControl: SearchControlLoaded) => (ButtonBarElement | null | undefined | false)[];
  getViewPromise?: (e: ModifiableEntity | null) => (undefined | string | ViewPromise<ModifiableEntity>);
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
  avoidTableFooterContainer: boolean;
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


  simpleFilterBuilder?: (sfbc: Finder.SimpleFilterBuilderContext) => React.ReactElement | undefined;
  enableAutoFocus: boolean;
  //Return "no_change" to prevent refresh. Navigator.view won't be called by search control, but returning an entity allows to return it immediatly in a SearchModal in find mode.  
  onCreate?: (scl: SearchControlLoaded) => Promise<undefined | void | EntityPack<any> | ModifiableEntity | "no_change">;
  onCreateFinished?: (entity: EntityPack<Entity> | ModifiableEntity | Lite<Entity> | undefined | void, scl: SearchControlLoaded) => void;
  onDoubleClick?: (e: React.MouseEvent<any>, row: ResultRow, sc?: SearchControlLoaded) => void;
  onNavigated?: (lite: Lite<Entity>) => void;
  onSelectionChanged?: (rows: ResultRow[], reason: SelectionChangeReason) => void;
  onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
  onHeighChanged?: () => void;
  onSearch?: (fo: FindOptionsParsed, dataChange: boolean, sc: SearchControlLoaded) => void;
  onResult?: (table: ResultTable, dataChange: boolean, sc: SearchControlLoaded) => void;
  ctx?: StyleContext;
  customRequest?: (req: QueryRequest, fop: FindOptionsParsed) => Promise<ResultTable>,
  onPageTitleChanged?: () => void;
  mobileOptions?: (fop: FindOptionsParsed) => SearchControlMobileOptions;
  onDrilldown?: (scl: SearchControlLoaded, row: ResultRow, options?: OnDrilldownOptions) => Promise<boolean | undefined>;
}

export type SelectionChangeReason = "toggle" | "toggleAll" | "newResult" | "contextMenu";

export interface SearchControlLoadedState {
  resultTable?: ResultTable;
  summaryResultTable?: ResultTable;
  simpleFilterBuilder?: React.ReactElement;
  selectedRows?: ResultRow[];
  markedRows?: MarkedRowsDictionary;
  isSelectOpen: boolean;
  resultFindOptions?: FindOptionsParsed;
  searchCount?: number;
  dragColumnIndex?: number,
  dropBorderIndex?: number,
  showHiddenColumns?: boolean,
  currentMenuPack?: ContextMenuPack;
  dataChanged?: boolean;

  contextualMenu?: {
    position: ContextMenuPosition;
    columnIndex: number | null;
    columnOffset?: number;
    rowIndex: number | null;
    filter?: string;
  };

  refreshMode?: RefreshMode;
  editingColumn?: ColumnOptionParsed;
  lastToken?: QueryToken;
  isMobile?: boolean;
  viewMode?: SearchControlViewMode;
  filterMode: SearchControlFilterMode;
}

type SearchControlFilterMode = "Simple" | "Advanced" | "Pinned";

export class SearchControlLoaded extends React.Component<SearchControlLoadedProps, SearchControlLoadedState> {

  constructor(props: SearchControlLoadedProps) {
    super(props);
    this.state = {
      isSelectOpen: false,
      refreshMode: props.defaultRefreshMode,
      filterMode: props.showFilters ? "Advanced" : "Simple",
    };
  }

  static maxToArrayElements = 100;
  static mobileOptions: ((fop: FindOptionsParsed) => SearchControlMobileOptions) | null = null;
  static onDrilldown: ((scl: SearchControlLoaded, row: ResultRow, options?: OnDrilldownOptions) => Promise<boolean | undefined>) | null = null;

  pageSubTitle?: string;
  extraUrlParams: { [key: string]: string | undefined } = {};

  getMobileOptions(fop: FindOptionsParsed): SearchControlMobileOptions {
    const fo = this.props.findOptions;
    const qs = Finder.getSettings(fo.queryKey);

    return this.props.mobileOptions?.(fop) ?? qs?.mobileOptions?.(fop) ?? SearchControlLoaded.mobileOptions?.(fop) ??
    {
      showSwitchViewModesButton: true,
      defaultViewMode: "Mobile"
    };
  }

  onResize = (): void => {
    const isMobile = (getBreakpoint() <= Breakpoints.sm);
    if (isMobile != this.state.isMobile)
      this.setState({
        isMobile: isMobile,
        viewMode: isMobile ? this.getMobileOptions(this.props.findOptions).defaultViewMode : "Standard",
      }, () => this.fixScroll());
  }

  getSimpleFilterBuilderElement(): React.ReactElement<any, string | React.JSXElementConstructor<any>> | undefined {
    const fo = this.props.findOptions;
    const qd = this.props.queryDescription;
    var qs = this.props.querySettings;
    return this.props.showSimpleFilterBuilder == false ? undefined :
      this.props.simpleFilterBuilder ? this.props.simpleFilterBuilder({ queryDescription: qd, initialFilterOptions: fo.filterOptions, search: () => this.doSearchPage1(), searchControl: this }) :
        qs?.simpleFilterBuilder ? qs.simpleFilterBuilder({ queryDescription: qd, initialFilterOptions: fo.filterOptions, search: () => this.doSearchPage1(), searchControl: this }) :
          undefined;
  }

  componentDidMount(): void {
    window.addEventListener('resize', this.onResize);
    this.onResize();

    const sfb = this.getSimpleFilterBuilderElement();

    if (sfb) {
      this.setState({
        filterMode: "Simple",
        simpleFilterBuilder: sfb
      });
    }

    if (this.props.searchOnLoad)
      this.doSearch({ force: true });
  }

  componentDidUpdate(props: SearchControlLoadedProps): void {
    if (!Hooks.areEqualDeps(this.props.deps ?? [], props.deps ?? [])) {
      this.doSearchPage1();
    }
  }

  isUnmounted = false;
  componentWillUnmount(): void {
    this.isUnmounted = true;
    window.removeEventListener('resize', this.onResize);
    this.abortableSearch.abort();
    this.abortableSearchSummary.abort();
  }

  entityColumn(): QueryToken {
    return this.props.queryDescription.columns["Entity"];
  }

  entityColumnTypeInfos(): TypeInfo[] {
    return getTypeInfos(this.entityColumn().type);
  }

  canFilter(): boolean {
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

  isManualRefreshOrAllPagination(): boolean {
    return this.state.refreshMode == "Manual" ||
      this.state.refreshMode == undefined && this.props.findOptions.pagination.mode == "All";
  }

  doSearchPage1(force: boolean = false): void {

    const fo = this.props.findOptions;

    if (fo.pagination.mode == "Paginate")
      fo.pagination.currentPage = 1;

    if (this.containerDiv)
      this.containerDiv.scrollTop = 0;

    this.doSearch({ force });
  };

  resetResults(continuation: () => void): void {
    this.setState({
      resultTable: undefined,
      summaryResultTable: undefined,
      resultFindOptions: undefined,
      selectedRows: [],
      currentMenuPack: undefined,
      markedRows: undefined,
      dataChanged: undefined,
    }, continuation);
  }

  abortableSearch: AbortableRequest<{
    request: QueryRequest
    fop: FindOptionsParsed
    customRequest?: (req: QueryRequest, fop: FindOptionsParsed) => Promise<ResultTable>
  }, ResultTable> = new AbortableRequest((signal, a: {
    request: QueryRequest;
    fop: FindOptionsParsed,
    customRequest?: (req: QueryRequest, fop: FindOptionsParsed) => Promise<ResultTable>
  }) => a.customRequest ? a.customRequest(a.request, a.fop) : Finder.API.executeQuery(a.request, signal));

  abortableSearchSummary: AbortableRequest<{
    request: QueryRequest
    fop: FindOptionsParsed
    customRequest?: (req: QueryRequest, fop: FindOptionsParsed) => Promise<ResultTable>
  }, ResultTable> = new AbortableRequest((signal, a: {
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
      return this.doSearch({ dataChanged: true });
    }
  }

  doSearch(opts: { dataChanged?: boolean, force?: boolean, keepSelected?: boolean }): Promise<void> {

    if (this.isUnmounted || (this.isManualRefreshOrAllPagination() && !opts.force))
      return Promise.resolve();

    var dataChanged = opts.dataChanged ?? this.state.dataChanged;


    const selectedLites = opts.keepSelected ? this.state.selectedRows?.map(a => a.entity!) : null;

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
          selectedRows: selectedLites?.map(l => rt.rows.firstOrNull(a => is(a.entity, l))).notNull() ?? [],
          currentMenuPack: undefined,
          markedRows: undefined,
          searchCount: (this.state.searchCount ?? 0) + 1
        }, () => {
          this.fixScroll();
          if (this.props.onResult)
            this.props.onResult(rt, dataChanged ?? false, this);
          this.notifySelectedRowsChanged("newResult");
        });
      });
    });
  }

  notifySelectedRowsChanged(reason: SelectionChangeReason): void {
    if (this.props.onSelectionChanged)
      this.props.onSelectionChanged(this.state.selectedRows!, reason);
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


  handlePagination = (p: Pagination): void => {
    this.props.findOptions.pagination = p;
    this.setState({ resultTable: undefined, resultFindOptions: undefined, dataChanged: false });

    if (this.containerDiv)
      this.containerDiv.scrollTop = 0;

    this.doSearch({});
  }


  handleOnContextMenu = (event: React.MouseEvent<any>): void => {

    event.preventDefault();
    event.stopPropagation();

    const td = DomUtils.closest(event.target as HTMLElement, "td, th")!;
    const columnIndex = td.getAttribute("data-column-index") ? parseInt(td.getAttribute("data-column-index")!) : null;


    const tr = td.parentNode as HTMLElement;
    const rowIndex = tr.getAttribute("data-row-index") ? parseInt(tr.getAttribute("data-row-index")!) : null;

    this.setState({
      contextualMenu: {
        position: getMouseEventPosition(event, event.currentTarget.querySelector('tbody')),
        columnIndex,
        rowIndex,
        columnOffset: td.tagName == "TH" ? this.getOffset(event.pageX, td.getBoundingClientRect(), Number.MAX_VALUE) : undefined,
      }
    });

    if (rowIndex != undefined) {
      const row = this.state.resultTable!.rows[rowIndex];
      if (!this.state.selectedRows!.contains(row)) {
        this.setState({
          selectedRows: [row],
          currentMenuPack: undefined
        }, () => {
          this.loadMenuPack();
          this.notifySelectedRowsChanged("contextMenu");
        });
      }

      if (this.state.currentMenuPack == undefined)
        this.loadMenuPack();
    }
  }


  handleColumnChanged = (token: QueryToken | undefined): void => {
    if (this.props.findOptions.groupResults) {
      var allKeys = this.props.findOptions.columnOptions.filter(a => a.token && a.token.queryTokenType != "Aggregate").map(a => a.token!.fullKey);
      this.props.findOptions.orderOptions = this.props.findOptions.orderOptions.filter(o => allKeys.contains(o.token.fullKey));
    }
    this.setState({ lastToken: token });
  }

  handleColumnClose = (): void => {
    this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());
  }

  handleFilterTokenChanged = (token: QueryToken | undefined): void => {
    this.setState({ lastToken: token });
  }


  handleFiltersChanged = (avoidSearch?: boolean): void => {

    //if (this.isManualRefreshOrAllPagination() || avoidSearch)
    this.forceUpdate();

    if (this.props.onFiltersChanged)
      this.props.onFiltersChanged(this.props.findOptions.filterOptions);
  }


  handlePinnedFilterChanged = (fop: FilterOptionParsed[], avoidSearch?: boolean): void => {

    this.handleFiltersChanged(avoidSearch);

    if (!avoidSearch)
      this.doSearchPage1();
  }

  handleHeightChanged = (): void => {
    if (this.props.onHeighChanged)
      this.props.onHeighChanged();
  }

  handleFiltersKeyUp = (e: React.KeyboardEvent<HTMLDivElement>): void => {
    if (e.key == KeyNames.enter) {
      e.stopPropagation();
      window.setTimeout(() => {
        var input = (document.activeElement as HTMLInputElement);
        input.blur();
        this.doSearchPage1(true);
      }, 200);
    }
  }


  fixScroll(): void {
    if (this.containerDiv) {
      var table = this.containerDiv.firstChild! as HTMLElement;
      if (this.containerDiv.scrollTop > table.clientHeight) {
        //var translate = "translate(0,0)";
        //this.thead!.style.transform = translate;
        this.containerDiv.scrollTop = 0;
        this.containerDiv.style.overflowY = "hidden";
        window.setTimeout(() => {
          this.containerDiv!.style.overflowY = "";
        }, 10);

      }
    }
  }


  containerDiv?: HTMLDivElement | null;

  render(): React.ReactElement {
    const p = this.props;
    const fo = this.props.findOptions;
    const qd = this.props.queryDescription;

    const sfb = this.state.simpleFilterBuilder &&
      React.cloneElement(this.state.simpleFilterBuilder, { ref: (e: ISimpleFilterBuilder) => { this.simpleFilterBuilderInstance = e; } } as any);

    const canAggregate = (fo.groupResults ? SubTokensOptions.CanAggregate : 0);
    const canAggregateXorOperationOrManual = (canAggregate != 0 ? canAggregate : SubTokensOptions.CanOperation | SubTokensOptions.CanManual);
    const canTimeSeries = (fo.systemTime?.mode == QueryTokenString.timeSeries.token ? SubTokensOptions.CanTimeSeries : 0)

    return (
      <div className={classes("sf-search-control sf-control-container", this.state.isMobile == true && this.state.viewMode == "Mobile" && "mobile")}
        data-search-count={this.state.searchCount}
        data-query-key={fo.queryKey}>
        {p.showHeader == true &&
          <div onKeyUp={this.handleFiltersKeyUp}>
            {
              this.state.filterMode != 'Simple' ? <FilterBuilder
                title={this.state.filterMode == "Pinned" ? SearchMessage.FilterDesigner.niceToString() : SearchMessage.AdvancedFilters.niceToString()}
                queryDescription={qd}
                filterOptions={fo.filterOptions}
                lastToken={this.state.lastToken}
                subTokensOptions={SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate | canTimeSeries}
                onTokenChanged={this.handleFilterTokenChanged}
                onFiltersChanged={() => this.handleFiltersChanged()}
                onHeightChanged={this.handleHeightChanged}
                showPinnedFiltersOptions={this.state.filterMode == 'Pinned'}
                showPinnedFiltersOptionsButton={false}
                showDashboardBehaviour={false}
              /> :
                sfb && <div className="simple-filter-builder">{sfb}</div>}
          </div>
        }
        {p.showHeader == true && this.state.filterMode == "Simple" && !sfb && this.renderPinnedFilters()}
        {p.showHeader == "PinnedFilters" && (sfb ?? this.renderPinnedFilters())}
        {p.showHeader == true && p.largeToolbarButtons && this.renderToolBar()}
        {p.showHeader == true && <MultipliedMessage findOptions={fo} mainType={this.entityColumn().type} />}
        {p.showHeader == true && fo.groupResults && <GroupByMessage findOptions={fo} mainType={this.entityColumn().type} />}
        {p.showHeader == true && fo.systemTime && <SystemTimeEditor findOptions={fo} queryDescription={qd} onChanged={() => this.forceUpdate()} />}

        <div className={p.avoidTableFooterContainer ? undefined : "sf-table-footer-container my-3 p-3 pb-1 bg-body rounded shadow-sm"}>
          {p.showHeader == true && !p.largeToolbarButtons && this.renderToolBar()}
          {this.state.isMobile == true && this.state.viewMode == "Mobile" ? this.renderMobile() :
            <>
              {
                this.state.editingColumn && <ColumnEditor
                  columnOption={this.state.editingColumn}
                  onChange={this.handleColumnChanged}
                  queryDescription={qd}
                  subTokensOptions={SubTokensOptions.CanElement | SubTokensOptions.CanToArray | SubTokensOptions.CanSnippet | canAggregateXorOperationOrManual | canTimeSeries}
                  close={this.handleColumnClose} />
              }
              <div ref={d => { this.containerDiv = d; }}
                className="sf-scroll-table-container table-responsive"
                style={{ maxHeight: this.props.maxResultsHeight }}>
                <table aria-multiselectable="true" role="grid"
                  aria-label={this.createCaption()}
                  className={classes("sf-search-results table table-hover table-sm", this.props.view && "sf-row-view")} onContextMenu={this.props.showContextMenu(this.props.findOptions) != false ? this.handleOnContextMenu : undefined}>
                  {AccessibleTable.ariaLabelAsCaption && <caption>{this.createCaption()}</caption>}
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
      </div>
    );
  }

  renderMobile(): React.ReactElement {
    return (
      <div ref={d => { this.containerDiv = d; }}
        className="sf-scroll-table-container"
        style={{ maxHeight: this.props.maxResultsHeight }}>
        <div className={classes("sf-search-results mobile", this.props.view && "sf-row-view")}>
          {this.renderRowsMobile()}
        </div>
      </div>
    );
  }

  // TOOLBAR


  handleSearchClick = (ev: React.MouseEvent<any>): void => {

    ev.preventDefault();

    this.doSearchPage1(true);

  };

  handleChangeFiltermode = async (mode: SearchControlFilterMode, refreshFilters = true, force = false): Promise<void> => {
    if (this.state.filterMode == mode && !force)
      return;

    if (refreshFilters)
      await this.getFindOptionsWithSFB();

    this.simpleFilterBuilderInstance = undefined;
    this.setState({
      simpleFilterBuilder: mode == "Simple" ? this.getSimpleFilterBuilderElement() : undefined,
      filterMode: mode
    }, () => this.handleHeightChanged());

  }

  handleSystemTimeClick = (): void => {
    var fo = this.props.findOptions;

    if (fo.systemTime == null)
      fo.systemTime = { mode: "AsOf", startDate: DateTime.local().toISO()! };
    else
      fo.systemTime = undefined;

    this.forceUpdate();
  }

  renderToolBar(): React.ReactElement {

    const p = this.props;
    const s = this.state;



    function toFindOptionsPath(fop: FindOptionsParsed) {
      var fo = Finder.toFindOptions(fop, p.queryDescription, p.defaultIncudeDefaultFilters);
      return Finder.findOptionsPath(fo);
    }

    const isManualOrAll = this.isManualRefreshOrAllPagination();
    const changesExpected = s.dataChanged || s.resultFindOptions == null || toFindOptionsPath(s.resultFindOptions) != toFindOptionsPath(p.findOptions);

    const buttonBarElements = [
      ...Finder.ButtonBarQuery.getButtonBarElements({ findOptions: p.findOptions, searchControl: this }),
      ...this.props.querySettings?.extraButtons?.(this) ?? [],
      ...this.props.extraButtons?.(this) ?? [],
    ].filter(a => Boolean(a)) as ButtonBarElement[];

    const leftButtonBarElements = buttonBarElements.filter(a => a.order != null && a.order < 0).orderBy(a => a.order ?? 0);
    const rightButtonBarElements = buttonBarElements.filter(a => a.order == null || a.order > 0).orderBy(a => a.order!);

    const titleLabels = StyleContext.default.titleLabels;

    const leftButtons = ([
      p.showFilterButton && {
        order: -5,
        button: <SearchControlEllipsisMenu sc={this} isHidden={!p.showFilterButton} />
      },

      {
        order: -3,
        button: <button type="button" className={classes("sf-query-button sf-search btn ms-2", changesExpected ? (isManualOrAll ? "btn-danger" : "btn-primary") : (isManualOrAll ? "border-danger text-danger" : "border-primary text-primary"))}
          onClick={this.handleSearchClick} title={changesExpected ? SearchMessage.Search.niceToString() : SearchMessage.Refresh.niceToString()} >
          <FontAwesomeIcon aria-hidden={true} icon={changesExpected ? "magnifying-glass" : "refresh"} />{changesExpected && <span className="d-none d-sm-inline ms-1">{SearchMessage.Search.niceToString()}</span>}
        </button>
      },

      this.props.showContextMenu(this.props.findOptions) != false && this.props.showSelectedButton && this.renderSelectedButton(),

      p.create && !this.props.ctx?.frame?.currentDate && {
        order: -2,
        button: <button className={classes("btn ", p.createButtonClass ?? "btn-tertiary")} title={titleLabels ? this.createTitle() : undefined} onClick={this.handleCreate}>
          <FontAwesomeIcon aria-hidden={true} icon="plus" className="sf-create" /><span className="d-none d-sm-inline ms-1">{this.createTitle()}</span>
        </button>
      },

      {
        order: -1,
        button: <VisualTipIcon visualTip={SearchVisualTip.SearchHelp} className="mx-2" content={props => <SearchHelp sc={this} injected={props} />} />
      },
      ...leftButtonBarElements
    ] as (ButtonBarElement | null | false | undefined)[])
      .filter(a => a)
      .map(a => a as ButtonBarElement);

    var rightButtons = ([
      ...(this.props.hideButtonBar ? [] : rightButtonBarElements),

      !this.props.hideFullScreenButton && Finder.isFindable(p.findOptions.queryKey, true) && {
        button: <button type="button" className="btn btn-tertiary" onClick={this.handleFullScreenClick} title={FrameMessage.Fullscreen.niceToString()}>
          <FontAwesomeIcon aria-hidden={true} icon="up-right-from-square" />
        </button>
      },

      this.state.isMobile == true && this.getMobileOptions(this.props.findOptions).showSwitchViewModesButton && {
        button: <button type="button" className="btn btn-tertiary" onClick={this.handleViewModeClick} title={SearchMessage.SwitchViewMode.niceToString()}>
          <FontAwesomeIcon aria-hidden={true} icon={this.state.viewMode == "Mobile" ? "desktop" : "mobile-alt"} />
        </button>
      }
    ] as (ButtonBarElement | null | false | undefined)[])
      .filter(a => a)
      .map(a => a as ButtonBarElement);

    return (
      <div className={classes("sf-query-button-bar d-flex justify-content-between", !this.props.largeToolbarButtons ? "btn-toolbar-small pb-2" : "my-3 py-2 px-3 bg-body rounded shadow-sm")}>
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

  handleCreated = (entity: EntityPack<Entity> | ModifiableEntity | Lite<Entity> | undefined | void): void => {
    if (this.props.onCreateFinished) {
      this.props.onCreateFinished(entity, this);
    } else {
      this.dataChanged();
    }
  }

  handleCreate = (ev: React.MouseEvent<any>): void => {

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

        if (isWindowsOpen || s?.avoidPopup || this.props.view == "InPlace") {
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

  handleFullScreenClick = (ev: React.MouseEvent<any>): void => {

    ev.preventDefault();

    const findOptions = Finder.toFindOptions(this.props.findOptions, this.props.queryDescription, this.props.defaultIncudeDefaultFilters || this.props.findOptions.filterOptions.some(a => a.pinned != null));

    const path = Finder.findOptionsPath(findOptions, this.extraUrlParams);

    if (ev.ctrlKey || ev.button == 1 || this.props.avoidChangeUrl)
      window.open(AppContext.toAbsoluteUrl(path));
    else
      AppContext.navigate(path);
  };

  handleViewModeClick = (ev: React.MouseEvent<any>): void => {
    this.setState({ viewMode: (this.state.viewMode == "Mobile" ? "Standard" : "Mobile") }, () => this.fixScroll());
  }

  createTitle(): string {

    const tis = this.entityColumnTypeInfos();

    const types = tis.map(ti => ti.niceName).join(", ");
    const gender = tis.first().gender;

    return SearchMessage.CreateNew0_G.niceToString().forGenderAndNumber(gender).formatWith(types);
  }

  createCaption(): string {

    const tis = this.entityColumnTypeInfos();

    const types = tis.map(ti => ti.niceName).join(", ");
    const gender = tis.first().gender;

    return SearchMessage._0ResultTable.niceToString().forGenderAndNumber(gender).formatWith(types);
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

  handleSelectedToggle = (isOpen: boolean): void => {
    this.setState({ isSelectOpen: isOpen }, () => {
      if (this.state.isSelectOpen && this.state.currentMenuPack == undefined)
        this.loadMenuPack();
    });
  }

  loadMenuPack(): void {
    var cm = this.props.showContextMenu(this.state.resultFindOptions ?? this.props.findOptions);
    if (cm == "Basic")
      this.setState({ currentMenuPack: { items: [], showSearch: false } });
    else {

      var litesPromise = !this.props.findOptions.groupResults ? Promise.resolve(this.getSelectedEntities()) : this.getGroupedSelectedEntities();

      const options = {
        lites: [],
        queryDescription: this.props.queryDescription,
        markRows: this.markRows,
        container: this,
        styleContext: this.props.ctx,
      } as ContextualItemsContext<Entity>;

      litesPromise
        .then(lites => {
          options.lites = lites;
          return renderContextualItems(options);
        })
        .then(menuPack => this.setState({ currentMenuPack: menuPack }));
    }
  }

  markRows = (dic: MarkedRowsDictionary): void => {
    this.dataChanged()
      .then(() => this.setMarkedRows(dic));
  }

  setMarkedRows(dic: MarkedRowsDictionary): void {
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
          <Dropdown.Toggle id="selectedButton" title={SearchMessage.OperationsForSelectedElements.niceToString()} variant="light" className="sf-query-button sf-tm-selected ms-2" disabled={this.state.selectedRows!.length == 0}>
            {title}
          </Dropdown.Toggle>
          <Dropdown.Menu>
            {this.state.currentMenuPack == undefined ? <Dropdown.Item className="sf-tm-selected-loading">{JavascriptMessage.loading.niceToString()}</Dropdown.Item> :
              this.state.currentMenuPack.items.length == 0 ? <Dropdown.Item className="sf-search-ctxitem-no-results">{JavascriptMessage.noActionsFound.niceToString()}</Dropdown.Item> :
                this.state.currentMenuPack.items.map((e, i) => React.cloneElement((e as SearchableMenuItem).menu ?? e, { key: i }))}
          </Dropdown.Menu>
        </Dropdown>
    };
  }

  // CONTEXT MENU

  handleContextOnHide = (): void => {
    this.setState({ contextualMenu: undefined });
  }


  handleQuickFilter = async (): Promise<void> => {
    const cm = this.state.contextualMenu!;
    const fo = this.props.findOptions;

    const token = fo.columnOptions[cm.columnIndex!].token!;
    const rt = this.state.resultTable;
    let value = cm.rowIndex == undefined || rt == null ? undefined : rt.rows[cm.rowIndex].columns[rt.columns.indexOf(token.fullKey)];

    var rule = Finder.quickFilterRules.filter(a => a.applicable(token, value, this)).last("Finder.QuickFilterRule");

    var showFilter = await rule.execute(token, value, this);

    if (this.state.filterMode == "Simple" && showFilter) {
      await this.handleChangeFiltermode("Advanced", false);
    }

    if (rt && cm.rowIndex != null)
      this.doSearchPage1();

    this.handleFiltersChanged();

    this.forceUpdate(() => this.handleHeightChanged());
  }

  addQuickFilter(token: QueryToken, operation: FilterOperation, value: unknown): boolean {
    const filterOptions = this.props.findOptions;
    var alreadyPinned = value != null && value != "" && filterOptions.filterOptions
      .firstOrNull(f => isFilterCondition(f) &&
        similarToken(f.token?.fullKey, token?.fullKey) &&
        f.operation == operation &&
        (f.value == null || f.value == "") &&
        f.pinned != null && f.pinned?.active == "WhenHasValue"
      );

    if (alreadyPinned) {
      alreadyPinned.value = value;
      return false;
    }
    else {
      filterOptions.filterOptions.push({ token, operation, value, frozen: false });
      return true;
    }

  }

  parseSingleFilterToken(token: string): Promise<QueryToken> {
    return Finder.parseSingleToken(this.props.findOptions.queryKey, token, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement);
  }

  handleInsertColumn = (): void => {

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

  handleEditColumn = (): void => {

    const cm = this.state.contextualMenu!;
    const fo = this.props.findOptions;
    this.setState({ editingColumn: fo.columnOptions[cm.columnIndex!] }, () => this.handleHeightChanged());

    this.forceUpdate();
  }

  handleRemoveColumn = (): void => {
    const cm = this.state.contextualMenu!;
    const fo = this.props.findOptions;
    const col = fo.columnOptions[cm.columnIndex!];
    fo.columnOptions.removeAt(cm.columnIndex!);
    if (fo.groupResults && col.token) {
      fo.orderOptions.extract(a => a.token.fullKey == col.token!.fullKey);
    }

    this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());
  }

  handleGroupByThisColumn = async (): Promise<void> => {
    const cm = this.state.contextualMenu!;
    const fo = this.props.findOptions;

    const col = fo.columnOptions[cm.columnIndex!];

    var timeSeriesColumn = fo.columnOptions.firstOrNull(c => c.token?.fullKey == QueryTokenString.timeSeries.token);

    fo.columnOptions.clear();

    var defAggregate = this.props.querySettings?.defaultAggregates;

    var sto = SubTokensOptions.CanAggregate | SubTokensOptions.CanElement;
    var parsedTokens: QueryToken[] = [];
    if (defAggregate) {

      var tokenParser = new Finder.TokenCompleter(this.props.queryDescription);

      defAggregate.forEach(a => tokenParser.request(a.token.toString()));
      defAggregate.filter(a => a.summaryToken != null).forEach(a => tokenParser.request(a.summaryToken!.toString()));

      await tokenParser.finished();

      fo.columnOptions.push(...defAggregate.map(t => {
        var token = tokenParser.get(t.token.toString(), sto);

        return ({
          token: token,
          summaryToken: t.summaryToken != null ? tokenParser.get(t.summaryToken!.toString(), sto) : undefined,
          displayName: (typeof t.displayName == "function" ? t.displayName() : t.displayName) ?? token.niceName,
          hiddenColumn: t.hiddenColumn,
          combineRows: t.combineRows,
        });
      }));
    }
    else {
      var tokenParser = new Finder.TokenCompleter(this.props.queryDescription);
      tokenParser.request("Count");
      await tokenParser.finished();
      var count = tokenParser.get("Count", sto);
      fo.columnOptions.push({ token: count, displayName: count.niceName });
    }

    if (timeSeriesColumn)
      fo.columnOptions.push(timeSeriesColumn);
    fo.columnOptions.push(col);
    fo.groupResults = true;
    fo.orderOptions.clear();
    fo.orderOptions.push(...parsedTokens.map(t => softCast<OrderOptionParsed>({ token: t, orderType: "Descending" })));

    this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());

    if (this.props.searchOnLoad)
      this.doSearchPage1();
  }

  handleRemoveOtherColumns = (): void => {
    const cm = this.state.contextualMenu!;
    const fo = this.props.findOptions;

    const col = fo.columnOptions[cm.columnIndex!];

    fo.columnOptions.clear();
    fo.columnOptions.push(col);

    this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());

    if (this.props.searchOnLoad)
      this.doSearchPage1();
  }

  handleRestoreDefaultColumn = (): void => {
    const cm = this.state.contextualMenu!;
    const fo = this.props.findOptions;

    const col = fo.columnOptions[cm.columnIndex!];
    var timeSeriesColumn = fo.columnOptions.firstOrNull(c => c.token?.fullKey == QueryTokenString.timeSeries.token);
    fo.columnOptions.clear();
    if (timeSeriesColumn)
      fo.columnOptions.push(timeSeriesColumn);
    fo.columnOptions.push(...Finder.getDefaultColumns(this.props.queryDescription)
      .map(token => softCast<ColumnOptionParsed>({ displayName: token.niceName, token: token })));

    if (fo.groupResults) {
      fo.orderOptions.clear();
    }
    fo.groupResults = false;

    this.setState({ editingColumn: undefined }, () => this.handleHeightChanged());

    if (this.props.searchOnLoad)
      this.doSearchPage1();
  }

  renderContextualMenu(): React.ReactElement | null {

    var showCM = this.props.showContextMenu(this.state.resultFindOptions ?? this.props.findOptions);

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

    const menuPack = this.state.currentMenuPack;
    if (cm.rowIndex != undefined && menuPack == null)
      return null; //avoid flickering

    const menuItems: React.ReactElement[] = [];
    if (this.canFilter() && cm.columnIndex != null && isColumnFilterable(cm.columnIndex)) {
      menuItems.push(<Dropdown.Header>{SearchMessage.Filters.niceToString()}</Dropdown.Header>);
      menuItems.push(<Dropdown.Item className="sf-quickfilter-header" onClick={this.handleQuickFilter}>
        {getAddFilterIcon()}&nbsp;{JavascriptMessage.addFilter.niceToString()}
      </Dropdown.Item>);
    }

    if (cm.rowIndex == undefined && p.allowChangeColumns) {

      if (menuItems.length)
        menuItems.push(<Dropdown.Divider />);

      menuItems.push(<Dropdown.Header>{SearchMessage.Columns.niceToString()}</Dropdown.Header>);

      if (cm.columnIndex != null) {
        menuItems.push(<Dropdown.Item className="sf-insert-column" onClick={this.handleInsertColumn}>
          {getInsertColumnIcon()}&nbsp;{JavascriptMessage.insertColumn.niceToString()}
        </Dropdown.Item>);

        menuItems.push(<Dropdown.Item className="sf-edit-column" onClick={this.handleEditColumn}>
          {getEditColumnIcon()}&nbsp;{JavascriptMessage.editColumn.niceToString()}
        </Dropdown.Item>);

        menuItems.push(<Dropdown.Item className="sf-remove-column" onClick={this.handleRemoveColumn}>
          {getRemoveColumnIcon()}&nbsp;{JavascriptMessage.removeColumn.niceToString()}
        </Dropdown.Item>);

        menuItems.push(<Dropdown.Divider />);

        if (p.showGroupButton && isColumnGroupable(cm.columnIndex))
          menuItems.push(<Dropdown.Item className="sf-group-by-column" onClick={this.handleGroupByThisColumn}>
            {getGroupByThisColumnIcon()}&nbsp;{JavascriptMessage.groupByThisColumn.niceToString()}
          </Dropdown.Item>);

        menuItems.push(<Dropdown.Item className="sf-remove-other-columns" onClick={this.handleRemoveOtherColumns}>
          {getRemoveOtherColumns()}&nbsp;{JavascriptMessage.removeOtherColumns.niceToString()}
        </Dropdown.Item>);
      }

      menuItems.push(<Dropdown.Item className="sf-restore-default-columns" onClick={this.handleRestoreDefaultColumn}>
        {getResotreDefaultColumnsIcon()}&nbsp;{JavascriptMessage.restoreDefaultColumns.niceToString()}
      </Dropdown.Item>);

      if (fo.columnOptions.some(a => a.hiddenColumn == true)) {
        menuItems.push(<Dropdown.Divider />);

        if (this.state.showHiddenColumns) {
          menuItems.push(<Dropdown.Item className="sf-hide-hidden-columns" onClick={() => this.setState({ showHiddenColumns: undefined })}>
            <FontAwesomeIcon aria-hidden={true} icon="eye-slash" color="#21618C" />&nbsp;{SearchMessage.HideHiddenColumns.niceToString()}
          </Dropdown.Item>);
        } else {
          menuItems.push(<Dropdown.Item className="sf-show-hidden-columns" onClick={() => this.setState({ showHiddenColumns: true })}>
            <FontAwesomeIcon aria-hidden={true} icon="eye" color="#21618C" />&nbsp;{SearchMessage.ShowHiddenColumns.niceToString()}
          </Dropdown.Item>);
        }
      }
    }

    const renderEntityMenuItems = cm.rowIndex != undefined && showCM != "Basic";

    if (renderEntityMenuItems) {

      menuItems.push(<Dropdown.Item className="sf-paste-menu-item" onClick={() => this.handleCopyClick()}>
        <FontAwesomeIcon aria-hidden={true} icon="copy" className="icon" color="#21618C" />&nbsp;{SearchMessage.Copy.niceToString()}
      </Dropdown.Item>);

      if (menuPack == undefined) {
        menuItems.push(<Dropdown.Header>{JavascriptMessage.loading.niceToString()}</Dropdown.Header>);
      } else {
        if (menuItems.length && menuPack.items.length)
          menuItems.push(<Dropdown.Divider />);

        const filter = this.state.contextualMenu?.filter;
        const filtered = filter ? menuPack.items.filter(mi => !(mi as SearchableMenuItem).fullText || (mi as SearchableMenuItem).fullText.toLowerCase().contains(filter.toLowerCase())) : menuPack.items;

        menuItems.splice(menuItems.length, 0, ...filtered.map(mi => (mi as SearchableMenuItem).menu ?? mi));
      }
    }

    if (menuItems.length == 0)
      return null;

    return (
      <ContextMenu id="table-context-menu" position={cm.position} onHide={this.handleContextOnHide}>
        {renderEntityMenuItems && menuPack && menuPack.showSearch &&
          <AutoFocus>
            <input
              type="search"
              className="form-control form-control-sm dropdown-item"
              value={this.state?.contextualMenu?.filter}
              placeholder={SearchMessage.Search.niceToString()}
              onKeyDown={this.handleMenuFilterKeyDown}
              onChange={this.handleMenuFilterChange} />
          </AutoFocus>}
        <div style={{ position: "relative", maxHeight: "calc(100vh - 400px)", overflow: "auto" }}>
          {menuItems.map((e, i) => React.cloneElement(e, { key: i }))}
        </div>
      </ContextMenu>
    );
  }

  handleMenuFilterChange = (e: React.ChangeEvent<HTMLInputElement>): void => {
    this.setState({ contextualMenu: this.state.contextualMenu && Object.assign(this.state.contextualMenu, { filter: e.currentTarget.value }) })
  }

  handleMenuFilterKeyDown = (e: React.KeyboardEvent<any>): void => {
    if (!e.shiftKey && e.key == KeyNames.arrowDown) {

      e.preventDefault();
      e.stopPropagation();

      var firstItem = document.querySelector("#table-context-menu a.dropdown-item:not(:has(input), .disabled)") as HTMLAnchorElement
      if (firstItem && typeof firstItem.focus === 'function')
        firstItem.focus();
    }
  }

  handleCopyClick(): void {
    const supportsClipboard = (navigator.clipboard && window.isSecureContext);
    if (!supportsClipboard)
      return;

    const text = this.state.selectedRows!.filter(r => !!r.entity)
      .map(r => liteKey(r.entity!))
      .join("|");

    navigator.clipboard.writeText(text);
  }

  //SELECTED ROWS

  async askAllLites(cic: ContextualItemsContext<Entity>, action: string): Promise<Lite<Entity>[] | undefined> {
    const rt = this.state.resultTable;
    const fo = this.state.resultFindOptions;
    if (rt == null || fo == null)
      return undefined;

    if (!this.allSelected())
      return cic.lites;

    if (fo.pagination.mode != "Paginate")
      return cic.lites;

    if (rt.totalElements != null && rt.totalElements == this.state.selectedRows!.length)
      return cic.lites;

    const tis = this.entityColumnTypeInfos();

    const selected = cic.lites.groupBy(a => a.EntityType)
      .map(g => niceCount(g.elements.length, getTypeInfo(g.key)))
      .joinCommaHtml(CollectionMessage.And.niceToString());

    const all = tis.length == 1 && !fo.groupResults && multiplyResultTokens(fo).length == 0 ?
      niceCount(rt.totalElements!, tis.single()) :
      <CountEntities fop={fo} tis={tis} />;


    const pm = await SelectorModal.chooseElement<PaginationMode>([fo.pagination.mode, "All"], {
      title: action,
      message: SearchMessage.YouHaveSelectedAllRowsOnThisPageDoYouWantTo0OnlyTheseRowsOrToAllRowsAcrossAllPages.niceToString().formatHtml(<strong>{action}</strong>),
      buttonDisplay: a =>
        a == "All" ?
          <span>
            {SearchMessage.AllPages.niceToString()}{" "}
            ({fo.groupResults ? SearchMessage._0GroupWith1_N.niceToString().forGenderAndNumber(rt.totalElements).formatHtml(<strong>{rt.totalElements}</strong>, all) : all})
          </span> :
          <span>
            {SearchMessage.CurrentPage.niceToString()}{" "}
            ({fo.groupResults ? SearchMessage._0GroupWith1_N.niceToString().forGenderAndNumber(this.state.selectedRows!.length).formatHtml(<strong>{this.state.selectedRows!.length}</strong>, selected) : selected})
          </span>,
      buttonName: a => a,
      size: "md",
    });


    if (pm == null)
      return undefined;

    if (pm != "All")
      return cic.lites;

    const allLites = await Finder.fetchLites({
      queryName: fo!.queryKey,
      filterOptions: Finder.toFilterOptions(fo!.filterOptions),
      count: null,
    });

    return allLites;
  }

  allSelected(): boolean {
    return this.state.resultTable != undefined && this.state.resultTable.rows.length != 0 && this.state.resultTable.rows.length == this.state.selectedRows!.length;
  }

  handleToggleAll = (): void => {

    if (!this.state.resultTable)
      return;

    this.setState({
      selectedRows: !this.allSelected() ? this.state.resultTable!.rows.clone() : [],
      currentMenuPack: undefined,
    }, () => {
      this.notifySelectedRowsChanged("toggleAll")
    });
  }

  handleHeaderClick = (e: React.MouseEvent<any>): void => {

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

  handleHeaderDragStart = (de: React.DragEvent<any>, dragIndex: number): void => {
    de.dataTransfer.setData('text', "start"); //cannot be empty string
    de.dataTransfer.effectAllowed = "move";
    this.setState({ dragColumnIndex: dragIndex });
  }

  handleHeaderDragEnd = (de: React.DragEvent<any>): void => {
    this.setState({ dragColumnIndex: undefined, dropBorderIndex: undefined });
  }


  getOffset(pageX: number, rect: DOMRect, margin: number): 1 | 0 | undefined {

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

  handlerHeaderDragOver = (de: React.DragEvent<any>, columnIndex: number): void => {
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

  handleHeaderDrop = (de: React.DragEvent<any>): void => {
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

    function getSummary(summaryToken: QueryToken | undefined) {

      if (rt == null || summaryToken == undefined)
        return null;

      var colIndex = rt.columns.indexOf(summaryToken.fullKey);

      if (colIndex == -1)
        return null;

      const val = rt.rows[0]?.columns[colIndex];

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
            systemTime: scl.props.findOptions.systemTime,
            searchControl: scl,
          }, { column: { token: summaryToken }, resultIndex: colIndex, columnIndex: colIndex, cellFormatter: formatter })}
        </div>
      );
    }

    var rootKeys = !this.props.findOptions.groupResults ? [] : getRootKeyColumn(this.props.findOptions.columnOptions.filter(co => co.token && co.token.queryTokenType != "Aggregate" && !hasToArray(co.token)));

    function isDerivedKey(token: QueryToken | undefined): boolean {
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
    var visibleColumns = this.getVisibleColumnsWithFormatter();
    var allSmall = visibleColumns.every(c => c.cellFormatter?.fillWidth == false);

    return (
      <tr>
        {this.props.allowSelection && <th scope="col" className="sf-small-column sf-th-selection">
          {this.props.allowSelection == true &&
            <input type="checkbox" aria-label={SearchMessage.SelectAllResults.niceToString()} className="form-check-input" id="cbSelectAll" onChange={this.handleToggleAll} checked={this.allSelected()} />
          }
        </th>
        }
        {(this.props.view || this.props.findOptions.groupResults) && <th className="sf-small-column sf-th-entity" data-column-name="Entity">{Finder.Options.entityColumnHeader()}</th>}
        {visibleColumns.map(({ column: co, cellFormatter, columnIndex: i }) =>
          <th key={i}
            scope="col"
            draggable={true}
            className={classes(
              cellFormatter?.fillWidth == false ? "sf-small-column" : undefined,
              i == this.state.dragColumnIndex && "sf-draggin",
              co == this.state.editingColumn && "sf-current-column",
              co.hiddenColumn && "sf-hidden-column",
              !this.canOrder(co) && "noOrder",
              (columnError(co.token) || columnSummaryError(co.summaryToken)) && "error",
              co.token && this.props.findOptions.groupResults && isNotDerivedToArray(co.token) && "error",
              this.state.dropBorderIndex != null && i == this.state.dropBorderIndex ? "drag-left " :
                this.state.dropBorderIndex != null && i == this.state.dropBorderIndex - 1 ? "drag-right " : undefined)}
            title={columnError(co.token) ?? columnSummaryError(co.summaryToken)}
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
              {
                co.token?.fullKey == QueryTokenString.timeSeries.token ? <span>
                  <FontAwesomeIcon icon="clock" className="me-1"
                    color={"gray"}
                    role="img"
                    aria-label={SystemTimeMode.niceToString("TimeSeries")}
                    title={SystemTimeMode.niceToString("TimeSeries")} />
                </span> :
                  this.props.findOptions.groupResults && co.token && co.token.queryTokenType != "Aggregate" ? <span>
                    <FontAwesomeIcon icon="key" className="me-1"
                      color={rootKeys.contains(co) ? "gray" : "lightgray"}
                      role="img"
                      aria-label={rootKeys.contains(co) ? SearchMessage.GroupKey.niceToString() : SearchMessage.DerivedGroupKey.niceToString()}
                      title={rootKeys.contains(co) ? SearchMessage.GroupKey.niceToString() : SearchMessage.DerivedGroupKey.niceToString()} />
                  </span> : null
              }
              {co.displayName}
            </div>
          </th>
        )}
        {allSmall && <th></th>}
      </tr>
    );
  }

  canOrder(column: ColumnOptionParsed): boolean {
    if (!column.token || !this.props.allowChangeOrder)
      return false;

    const t = column.token;

    if (t.type.isCollection)
      return false;

    if (hasToArray(t))
      return false;

    if (t.type.isEmbedded || isTypeModel(t.type.name) || t.type.name == "CellOperationDTO" || t.type.name == "ManualCellDTO")
      return t.hasOrderAdapter == true;

    return true;
  }

  orderIcon(column: ColumnOptionParsed): React.ReactElement | "" {

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

  handleChecked = (event: React.ChangeEvent<HTMLInputElement>, index: number): void => {

    const cb = event.currentTarget;

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

    this.notifySelectedRowsChanged("toggle");

    this.setState({ currentMenuPack: undefined });
  }

  static getGroupFilters(row: ResultRow, resTable: ResultTable, resFo: FindOptionsParsed): FilterOption[] {

    var rootKeys = getRootKeyColumn(resFo.columnOptions.filter(co => co.token && co.token.queryTokenType != "Aggregate" && !hasOperation(co.token) && !hasManual(co.token) && co.token.fullKey != QueryTokenString.timeSeries.token));

    var keyFilters = resFo.columnOptions
      .filter(col => col.token != null)
      .map(col => ({ col, value: row.columns[resTable.columns.indexOf(col.token!.fullKey)] }))
      .filter(a => rootKeys.contains(a.col))
      .map(a => ({ token: a.col.token!.fullKey, operation: "EqualTo", value: a.value }) as FilterOption);

    var originalFilters = Finder.toFilterOptions(resFo.filterOptions.map(a => withoutPinned(a)).notNull().filter(f => !Finder.isAggregate(f)));

    return [...originalFilters, ...keyFilters];
  }

  openRowGroup(row: ResultRow, e: React.MouseEvent): void {

    var resFo = this.state.resultFindOptions!;

    var extraColumns = resFo.columnOptions.map(a =>
      a.token == null ? null :
        a.token.fullKey == QueryTokenString.timeSeries.token ? null :
          a.token.queryTokenType == "Aggregate" ? (!a.token.parent ? null : ({ token: a.token.parent.fullKey, summaryToken: a.token.fullKey }) as ColumnOption) :
            ({ token: a.token.fullKey }) as ColumnOption)
      .notNull();

    var filters = SearchControlLoaded.getGroupFilters(row, this.state.resultTable!, resFo);

    const fo = ({
      queryName: resFo.queryKey,
      filterOptions: filters,
      columnOptions: extraColumns,
      columnOptionsMode: "ReplaceOrAdd",
      systemTime: resFo.systemTime &&
        (resFo.systemTime.mode == "TimeSeries" ? { mode: "AsOf", startDate: this.getRowValue(row, QueryTokenString.timeSeries) } :
          { ...resFo.systemTime }),
      includeDefaultFilters: false,
    } as FindOptions);

    const isWindowsOpen = e.button == 1 || e.ctrlKey;

    const onDrilldown = this.props.onDrilldown ?? SearchControlLoaded.onDrilldown;
    const promise = onDrilldown ? onDrilldown(this, row, { openInNewTab: isWindowsOpen, onReload: () => this.dataChanged() }) : Promise.resolve(false);
    promise.then(done => {
      if (done == false) {
        if (isWindowsOpen) {
          window.open(AppContext.toAbsoluteUrl(Finder.findOptionsPath(fo)));
        } else {

          return Finder.explore(fo).then(() => {
            this.dataChanged();
          });
        }
      }

    });
  }

  handleDoubleClick = (e: React.MouseEvent<any>, row: ResultRow, columns: string[]): void => {

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
      this.openRowGroup(row, e);
      return;
    }

    if (this.props.view) {
      var lite = row.entity!;

      if (!lite || !Navigator.isViewable(lite.EntityType, { isSearch: "main" }))
        return;

      e.preventDefault();

      const s = Navigator.getSettings(lite.EntityType);

      const qs = this.props.querySettings;

      const getViewPromise = this.props.getViewPromise ?? qs?.getViewPromise;

      const isWindowsOpen = e.button == 1 || e.ctrlKey;

      const onDrilldown = this.props.onDrilldown ?? SearchControlLoaded.onDrilldown;
      const promise = onDrilldown ? onDrilldown(this, row, { openInNewTab: isWindowsOpen || s?.avoidPopup, showInPlace: this.props.view == "InPlace", onReload: () => this.handleOnNavigated(lite) }) : Promise.resolve(false);
      promise.then(done => {
        if (done == false) {
          if (isWindowsOpen || s?.avoidPopup || this.props.view == "InPlace") {
            var vp = getViewPromise && getViewPromise(null);
            var url = Navigator.navigateRoute(lite, vp && typeof vp == "string" ? vp : undefined);
            if (this.props.view == "InPlace" && !isWindowsOpen)
              AppContext.navigate(url);
            else
              window.open(AppContext.toAbsoluteUrl(url));
          }
          else {
            Navigator.view(lite, { getViewPromise: getViewPromise, buttons: "close" })
              .then(() => {
                this.handleOnNavigated(lite);
              });
          }
        }
      });
    }
  }

  static joinNodes(values: (React.ReactElement | string | null | undefined)[], separator: React.ReactElement | string, maxToArrayElements: number): React.FunctionComponentElement<{
    children?: React.ReactNode | undefined
  }> {

    if (values.length > (maxToArrayElements - 1))
      values = [...values.filter((a, i) => i < maxToArrayElements - 1), "…"];

    return React.createElement(React.Fragment, undefined,
      ...values.flatMap((v, i) => i == values.length - 1 ? [v] : [v, separator])
    );
  }

  getVisibleColumn(): {
    co: ColumnOptionParsed
    i: number
  }[] {
    return this.props.findOptions.columnOptions
      .map((co, i) => ({ co, i }))
      .filter(({ co, i }) => !co.hiddenColumn || this.state.showHiddenColumns);
  }

  getVisibleColumnsWithFormatter(): ColumnParsed[] {
    const columnOptions = this.getVisibleColumn();
    const resultColumns = this.state.resultTable?.columns;
    const qs = this.props.querySettings;

    return columnOptions.map<ColumnParsed>(({ co, i }) => ({
      column: co,
      columnIndex: i,
      hasToArray: hasToArray(co.token),
      cellFormatter: (co.token && ((this.props.formatters && this.props.formatters[co.token.fullKey]) || Finder.getCellFormatter(qs, co.token, this))),
      resultIndex: co.token == undefined || resultColumns == null ? -1 :
        co.token.fullKey == "Entity" && !this.state.resultTable?.columns.contains("Entity") ? "Entity" :
          resultColumns.indexOf(co.token.fullKey)
    }));
  }

  getNoResultsElement(): React.ReactElement | string | undefined {
    const resultTable = this.state.resultTable!;
    const resFO = this.state.resultFindOptions!;

    if (resultTable.rows.length == 0) {

      if (resultTable.totalElements == 0 || this.props.showFooter == false || resFO.pagination.mode != "Paginate") {

        if (this.props.querySettings?.noResultMessage) {
          var node = this.props.querySettings?.noResultMessage(this);
          if (node !== undefined)
            return node;
        }

        return SearchMessage.NoResultsFound.niceToString();
      }
      else
        return SearchMessage.NoResultsFoundInPage01.niceToString().formatHtml(
          resFO.pagination.currentPage,
          <LinkButton title={undefined} onClick={e => {
            this.handlePagination({
              mode: "Paginate",
              elementsPerPage: resFO.pagination.elementsPerPage,
              currentPage: 1
            });
          }}>
            {SearchMessage.GoBackToPageOne.niceToString()}
          </LinkButton>
        );
    }

    return undefined;
  }

  getRowAttributes(resultRow: ResultRow): React.HTMLAttributes<HTMLTableRowElement> | undefined {
    const qs = this.props.querySettings;
    const rowAttributes = this.props.rowAttributes ?? qs?.rowAttributes;

    return rowAttributes ? rowAttributes(resultRow, this) : undefined;
  }

  getEntityFormatter(): Finder.EntityFormatter {
    const qs = this.props.querySettings;
    return this.props.entityFormatter ?? (qs?.entityFormatter) ?? Finder.entityFormatRules.filter(a => a.isApplicable(this)).last("EntityFormatRules").formatter;
  }

  hasEntityColumn(): boolean | "InPlace" {
    return this.props.findOptions.groupResults || this.props.view;
  }

  getColumnElement(fctx: Finder.CellFormatterContext, c: ColumnParsed): string | React.ReactElement<any, string | React.JSXElementConstructor<any>> | React.FunctionComponentElement<{
    children?: React.ReactNode | undefined
  }> | null | undefined {

    return c.resultIndex == -1 || c.cellFormatter == undefined ? undefined :
      c.hasToArray != null ? SearchControlLoaded.joinNodes((getRowValue(fctx.row, c.resultIndex) as unknown[]).map(v => c.cellFormatter!.formatter(v, fctx, c)),
        c.hasToArray.key == "SeparatedByComma" || c.hasToArray.key == "SeparatedByCommaDistinct" ? <span className="text-muted">, </span> : <br />, SearchControlLoaded.maxToArrayElements) :
        c.cellFormatter.formatter(getRowValue(fctx.row, c.resultIndex), fctx, c);
  }

  rowRefs: React.RefObject<HTMLTableRowElement | null>[] = [];

  renderRows(): React.ReactNode {
    const columnOptions = this.getVisibleColumn();
    const columnsCount = columnOptions.length +
      (this.props.allowSelection ? 1 : 0) +
      (this.props.view ? 1 : 0);

    const resultTable = this.state.resultTable;
    if (!resultTable) {
      if (this.props.findOptions.pagination.mode === "All" && this.props.showFooter)
        return <tr tabIndex={0}><td colSpan={columnsCount} className="text-danger">{SearchMessage.ToPreventPerformanceIssuesAutomaticSearchIsDisabledCheckYourFiltersAndThenClickSearchButton.niceToString()}</td></tr>;

      return <tr tabIndex={0}><td colSpan={columnsCount}>{JavascriptMessage.searchForResults.niceToString()}</td></tr>;
    }

    var noResultsElement = this.getNoResultsElement();
    if (noResultsElement != null)
      return <tr tabIndex={0}><td colSpan={columnsCount}>{noResultsElement}</td></tr>;

    const entityFormatter = this.getEntityFormatter();
    const columns = this.getVisibleColumnsWithFormatter();
    var anyCombineEquals = columns.some(a => a.column.combineRows != null);

    // Row refs für Fokus-Handling erstellen
    this.rowRefs = resultTable.rows.map(() => React.createRef<HTMLTableRowElement>());

    return resultTable.rows.map((row, i, rows) => {
      const mark = this.getMarkedRow(row);
      const markClassName = mark?.status === "Success" ? "sf-entity-ctxmenu-success" :
        mark?.status === "Warning" ? "sf-row-warning" :
          mark?.status === "Error" ? "sf-row-danger" :
            mark?.status === "Muted" ? "text-muted" :
              undefined;

      const selected = this.state.selectedRows?.contains(row);
      var ra = this.getRowAttributes(row);

      function equals(a: unknown, b: unknown) {
        return a === b || is(a as any, b as any, false, false);
      }

      function calculateRowSpan(getVal: (row: ResultRow) => unknown): number | undefined {
        const value = getVal(row);
        let rowSpan = 1;
        while (i + rowSpan < rows.length && equals(getVal(rows[rowSpan + i]), value))
          rowSpan++;
        return rowSpan === 1 ? undefined : rowSpan;
      }

      var fctx: Finder.CellFormatterContext = {
        refresh: () => this.dataChanged(),
        systemTime: this.props.findOptions.systemTime,
        columns: this.state.resultTable!.columns,
        row: row,
        rowIndex: i,
        searchControl: this,
      };

      var tr = (
        <tr
          key={i}
          aria-describedby={`result_row_${i}_tooltip`}
          aria-selected={selected}
          ref={this.rowRefs[i]}
          data-row-index={i}
          data-entity={row.entity && liteKey(row.entity)}
          onDoubleClick={e => this.handleDoubleClick(e, row, resultTable.columns)}
          onKeyDown={e => {
            if (e.key === "ArrowDown") {
              e.preventDefault();
              this.rowRefs[i + 1]?.current?.focus();
            } else if (e.key === "ArrowUp") {
              e.preventDefault();
              this.rowRefs[i - 1]?.current?.focus();
            }
          }}
          {...ra}
          className={classes(markClassName, ra?.className, selected && "sf-row-selected")}
        >
          {this.props.allowSelection &&
            <td className="centered-cell">
              {this.props.selectionFormatter ? this.props.selectionFormatter(this, row, i) :
                <input type="checkbox"
                  className="sf-td-selection form-check-input"
                  checked={this.state.selectedRows!.contains(row)}
                  onChange={e => this.handleChecked(e, i)}
                  aria-label={`Select row ${i + 1}`}
                  data-index={i} />}
            </td>
          }

          {this.hasEntityColumn() &&
            (anyCombineEquals && i !== 0 && equals(resultTable.rows[i - 1].entity, row.entity) ? null :
              <td className={entityFormatter.cellClass} rowSpan={anyCombineEquals ? calculateRowSpan(row => row.entity) : undefined}>
                {entityFormatter.formatter(fctx)}
              </td>
            )
          }

          {columns.map((c, j) =>
            i !== 0 && c.column.combineRows === "EqualValue" && equals(getRowValue(resultTable.rows[i - 1], c.resultIndex), getRowValue(row, c.resultIndex)) ? null :
              i !== 0 && c.column.combineRows === "EqualEntity" && equals(resultTable.rows[i - 1].entity, row.entity) ? null :
                <td key={j} data-column-index={j} className={c.cellFormatter && c.cellFormatter.cellClass}
                  rowSpan={
                    c.column.combineRows === "EqualValue" ? calculateRowSpan(row => getRowValue(row, c.resultIndex)) :
                      c.column.combineRows === "EqualEntity" ? calculateRowSpan(row => row.entity) :
                        undefined}>
                  {this.getColumnElement(fctx, c)}
                </td>
          )}
        </tr>
      );

      const message = mark?.message;
      if (!message)
        return tr;

      return (
        <OverlayTrigger
          overlay={
            <Tooltip role="tooltip" placement="bottom" id={"result_row_" + i + "_tooltip"} style={{ "--bs-tooltip-max-width": "100%" } as any}>
              {message.split("\n").map((s, i) => <p key={i}>{s}</p>)}
            </Tooltip>
          }
        >
          {tr}
        </OverlayTrigger>
      );
    });
  }


  getRowMarketIcon(row: ResultRow, rowIndex: number): React.ReactElement | undefined {
    const mark = this.getMarkedRow(row);
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

  renderRowsMobile(): React.ReactElement | React.ReactElement[] {
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
    const columns = this.getVisibleColumnsWithFormatter();

    return resultTable.rows.map((row, i) => {
      const markIcon = this.getRowMarketIcon(row, i);
      const ra = this.getRowAttributes(row);

      var fctx: Finder.CellFormatterContext = {
        refresh: () => this.dataChanged(),
        systemTime: this.props.findOptions.systemTime,
        columns: this.state.resultTable!.columns,
        row: row,
        rowIndex: i,
        searchControl: this,
      };

      var div = (
        <div key={i} data-row-index={i} data-entity={row.entity && liteKey(row.entity)}
          onDoubleClick={e => this.handleDoubleClick(e, row, resultTable.columns)}
          {...ra}
          className={classes("row-container", ra?.className)}>
          {(this.props.allowSelection || this.hasEntityColumn()) &&
            <div className="row-data row-header">
              {this.props.allowSelection &&
                <span className="row-selection">
                  {this.props.selectionFormatter ? this.props.selectionFormatter(this, row, i) :
                    <input type="checkbox" className="sf-td-selection form-check-input" checked={this.state.selectedRows!.contains(row)} onChange={e => this.handleChecked(e, i)} data-index={i} />}
                </span>
              }

              {this.hasEntityColumn() &&
                <span className={classes("row-entity", entityFormatter.cellClass)}>
                  {entityFormatter.formatter(fctx)}
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
                    {this.getColumnElement(fctx, c)}
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

  renderPinnedFilters(): React.ReactNode {

    const fo = this.props.findOptions;

    return (
      <AutoFocus disabled={!this.props.enableAutoFocus}>
        <PinnedFilterBuilder
          filterOptions={fo.filterOptions}
          pinnedFilterVisible={this.props.pinnedFilterVisible}
          onFiltersChanged={this.handlePinnedFilterChanged}
          onSearch={() => this.doSearchPage1(true)}
          showSearchButton={this.state.refreshMode == "Manual" && this.props.showHeader != true}

        />
      </AutoFocus>
    );
  }

  handleOnNavigated = (lite: Lite<Entity>): void => {

    if (this.props.onNavigated)
      this.props.onNavigated(lite);

    this.dataChanged();
  }

  getMarkedRow(row: ResultRow): MarkedRow | undefined {

    if (!this.state.markedRows)
      return undefined;

    var key = this.props.querySettings?.markRowsColumn ? row.columns[this.state.resultTable!.columns.indexOf(this.props.querySettings?.markRowsColumn)] : row.entity && liteKey(row.entity);

    if (key == null)
      return;

    const m = this.state.markedRows[key];
    if (m === null)
      return { status: "Success", message: undefined };

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

  getRowValue<T = unknown>(row: ResultRow, token: QueryTokenString<T> | string, automaticEntityPrefix = true): Finder.AddToLite<T> | undefined {

    var result = this.tryGetRowValue(row, token, automaticEntityPrefix, true);

    return result!.value;
  }

  tryGetRowValue<T = unknown>(row: ResultRow, token: QueryTokenString<T> | string, automaticEntityPrefix = true, throwError = false): { value: Finder.AddToLite<T> | undefined } | undefined {

    const tokenName = token.toString();

    if (tokenName == "Entity")
      return { value: row.entity as Finder.AddToLite<T> | undefined };

    const colIndex = this.state.resultTable!.columns.indexOf(tokenName);
    if (colIndex != -1)
      return { value: row.columns[colIndex] };

    var filter = this.props.findOptions.filterOptions.firstOrNull(a => isFilterCondition(a) && isActive(a) && a.token?.fullKey == tokenName && a.operation == "EqualTo");
    if (filter != null)
      return { value: filter?.value };

    if (automaticEntityPrefix) {
      var result = this.tryGetRowValue(row, tokenName.startsWith("Entity.") ? tokenName.after("Entity.") : "Entity." + tokenName, false, false);
      if (result != null)
        return result as any;
    }

    if (throwError)
      throw new Error(`No column '${token}' found`);

    return undefined;
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

    var filter = sc.props.findOptions.filterOptions.firstOrNull(a => isFilterCondition(a) && isActive(a) && a.token?.fullKey == tokenName && a.operation == "EqualTo");
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

export function getResotreDefaultColumnsIcon(): React.ReactElement {
  return <span className="fa-layers fa-fw icon">
    <FontAwesomeIcon aria-hidden={true} icon="table-columns" transform="left-2" color="var(--bs-secondary-color)" />
    <FontAwesomeIcon aria-hidden={true} icon="rotate-left" transform="shrink-4 up-8 right-8" color="var(--bs-body-color)" />
  </span>
}

export function getGroupByThisColumnIcon(): React.ReactElement {
  return <span className="fa-layers fa-fw icon">
    <FontAwesomeIcon aria-hidden={true} icon="table-columns" transform="left-2" color="var(--bs-secondary-color)" />
    <FontAwesomeIcon aria-hidden={true} icon={["fas", "layer-group"]} transform="shrink-3 up-8 right-8" color="var(--bs-cyan)" />
  </span>
}

export function getRemoveOtherColumns(): React.ReactElement {
  return <span className="fa-layers fa-fw icon">
    <FontAwesomeIcon aria-hidden={true} icon="table-columns" transform="left-2" color="var(--bs-secondary-color)" />
    <FontAwesomeIcon aria-hidden={true} icon="remove" transform="shrink-4 up-8 right-8" color="var(--bs-body-color)" />
  </span>
}

export function getRemoveColumnIcon(): React.ReactElement {
  return <span className="fa-layers fa-fw icon">
    <FontAwesomeIcon aria-hidden={true} icon="table-columns" transform="left-2" color="var(--bs-secondary-color)" />
    <FontAwesomeIcon aria-hidden={true} icon={["fas", "square-xmark"]} transform="shrink-3 up-8 right-8" color="var(--bs-danger)" />
  </span>
}

export function getEditColumnIcon(): React.ReactElement {
  return <span className="fa-layers fa-fw icon">
    <FontAwesomeIcon aria-hidden={true} icon="table-columns" transform="left-2" color="var(--bs-secondary-color)" />
    <FontAwesomeIcon aria-hidden={true} icon={["fas", "square-pen"]} transform="shrink-3 up-8 right-8" color="var(--bs-orange)" />
  </span>
}

export function getInsertColumnIcon(): React.ReactElement {
  return <span className="fa-layers fa-fw icon">
    <FontAwesomeIcon aria-hidden={true} icon="table-columns" transform="left-2" color="var(--bs-secondary-color)" />
    <FontAwesomeIcon aria-hidden={true} icon={["fas", "square-plus"]} transform="shrink-3 up-8 right-8" color="var(--bs-success)" />
  </span>
}

export function getAddFilterIcon(): React.ReactElement {
  return <span className="fa-layers fa-fw icon">
    <FontAwesomeIcon aria-hidden={true} icon="filter" transform="left-2" color="var(--bs-secondary-color)" />
    <FontAwesomeIcon aria-hidden={true} icon={["fas", "square-plus"]} transform="shrink-3 up-8 right-8" color="var(--bs-blue)" />
  </span>
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

function getRowValue(row: ResultRow, resultIndex: number | "Entity") {
  if (resultIndex == "Entity")
    return row.entity;

  return row.columns[resultIndex];
}

function SearchControlEllipsisMenu(p: { sc: SearchControlLoaded, isHidden: boolean }) {

  if (p.isHidden)
    return null;

  var props = p.sc.props;
  var filterMode = p.sc.state.filterMode;
  const active = filterMode == "Advanced" || filterMode == "Pinned";

  const activeFilters = p.sc.props.findOptions.filterOptions.filter(f => isActive(f)).length ?? 0;

  return (
    <Dropdown as={ButtonGroup} title={SearchMessage.Filters.niceToString()}>
      <Button type="button" variant="tertiary" className="sf-filter-button" aria-label={SearchMessage.Filters.niceToString()} active={active} onClick={e => p.sc.handleChangeFiltermode(active ? 'Simple' : 'Advanced')}>
        <FontAwesomeIcon aria-hidden={true} icon="filter" /> {activeFilters == 0 ? null : activeFilters}
      </Button>
      <Dropdown.Toggle variant="tertiary" split className="px-2" aria-label={SearchMessage.FilterTypeSelection.niceToString()}></Dropdown.Toggle>
      <Dropdown.Menu aria-label={SearchMessage.FilterMenu.niceToString()}>
        <Dropdown.Item data-key={("Simple" satisfies SearchControlFilterMode)} active={filterMode == 'Simple'} onClick={e => p.sc.handleChangeFiltermode('Simple')} ><span className="me-2" style={{ visibility: filterMode != 'Simple' ? 'hidden' : undefined }} > <FontAwesomeIcon aria-hidden={true} icon="check" color="navy" /></span>{SearchMessage.SimpleFilters.niceToString()}</Dropdown.Item>
        <Dropdown.Item data-key={("Advanced" satisfies SearchControlFilterMode)} active={filterMode == 'Advanced'} onClick={e => p.sc.handleChangeFiltermode('Advanced')} ><span className="me-2" style={{ visibility: filterMode != 'Advanced' ? 'hidden' : undefined }} > <FontAwesomeIcon aria-hidden={true} icon="check" color="navy" /></span>{SearchMessage.AdvancedFilters.niceToString()}</Dropdown.Item>
        <Dropdown.Item data-key={("Pinned" satisfies SearchControlFilterMode)} active={filterMode == 'Pinned'} onClick={e => p.sc.handleChangeFiltermode('Pinned')} ><span className="me-2" style={{ visibility: filterMode != 'Pinned' ? 'hidden' : undefined }} > <FontAwesomeIcon aria-hidden={true} icon="check" color="navy" /></span>{SearchMessage.FilterDesigner.niceToString()}</Dropdown.Item>
        {props.showSystemTimeButton && <Dropdown.Divider />}
        {props.showSystemTimeButton && <Dropdown.Item onClick={p.sc.handleSystemTimeClick} ><span className="me-2" style={{ visibility: p.sc.props.findOptions.systemTime == null ? 'hidden' : undefined }} > <FontAwesomeIcon aria-hidden={true} icon="check" color="navy" /></span>{SearchMessage.TimeMachine.niceToString()}</Dropdown.Item>}
      </Dropdown.Menu>
    </Dropdown>
  );
}

function niceCount(count: number, ti: TypeInfo) {
  return <span><strong>{count}</strong> {count == 1 ? ti.niceName : ti.nicePluralName}</span>;
}

function CountEntities(p: { fop: FindOptionsParsed, tis: TypeInfo[] }): React.ReactElement {

  var counts = useAPI<number | ResultTable>(() => p.tis.length == 1 ?
    Finder.getQueryValue(p.fop.queryKey,
      Finder.toFilterOptions(p.fop.filterOptions)) :
    Finder.getResultTable({
      queryName: p.fop.queryKey,
      filterOptions: Finder.toFilterOptions(p.fop.filterOptions),
      groupResults: true,
      columnOptions: [
        { token: "Count" },
        { token: "Entity.Type" },
      ]
    }), []);

  return counts == undefined ? <span>…</span> :
    typeof counts == "number" ? niceCount(counts, p.tis.single()) :
      counts.rows.map(a => niceCount(a.columns[0], getTypeInfo(a.columns[1])))
        .joinCommaHtml(CollectionMessage.And.niceToString())
}

export default SearchControlLoaded;
