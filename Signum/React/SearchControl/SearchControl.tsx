import * as React from 'react'
import { Finder } from '../Finder'
import { ResultTable, ResultRow, FindOptions, FindOptionsParsed, FilterOptionParsed, FilterOption, QueryDescription, QueryRequest } from '../FindOptions'
import { Lite, Entity, ModifiableEntity, EntityPack } from '../Signum.Entities'
import { tryGetTypeInfos, getQueryKey, getTypeInfos, QueryTokenString, getQueryNiceName } from '../Reflection'
import { Navigator, ViewPromise } from '../Navigator'
import SearchControlLoaded, { OnDrilldownOptions, SearchControlMobileOptions, SearchControlViewMode, SelectionChangeReason, ShowBarExtensionOption } from './SearchControlLoaded'
import { ErrorBoundary } from '../Components';
import { Property } from 'csstype';
import "./Search.css"
import { ButtonBarElement, StyleContext } from '../TypeContext';
import { areEqualDeps, useAPI, useForceUpdate, usePrevious, useStateWithPromise } from '../Hooks'
import { RefreshMode } from '../Signum.DynamicQuery';
import { HeaderType, Title } from '../Lines/GroupHeader'

export interface SimpleFilterBuilderProps {
  findOptions: FindOptions;
}

export interface SearchControlProps {
  findOptions: FindOptions;
  formatters?: { [token: string]: Finder.CellFormatter };
  rowAttributes?: (row: ResultRow, searchControl: SearchControlLoaded) => React.HTMLAttributes<HTMLTableRowElement> | undefined;
  entityFormatter?: Finder.EntityFormatter;
  selectionFromatter?: (searchControl: SearchControlLoaded, row: ResultRow, rowIndex: number) => React.ReactElement | undefined;

  extraButtons?: (searchControl: SearchControlLoaded) => (ButtonBarElement | null | undefined | false)[];
  getViewPromise?: (e: any /*Entity*/) => undefined | string | ViewPromise<any /*Entity*/>;
  maxResultsHeight?: Property.MaxHeight<string | number> | any;
  tag?: string | {};
  searchOnLoad?: boolean;
  allowSelection?: boolean | "single";
  showContextMenu?: (fop: FindOptionsParsed) => boolean | "Basic";
  hideButtonBar?: boolean;
  hideFullScreenButton?: boolean;
  defaultIncludeDefaultFilters?: boolean;
  showHeader?: boolean | "PinnedFilters";
  avoidTableFooterContainer?: boolean;
  avoidGroupByMessage?: boolean;
  pinnedFilterVisible?: (fop: FilterOptionParsed) => boolean;
  showBarExtension?: boolean;
  showBarExtensionOption?: ShowBarExtensionOption;
  showFilters?: boolean;
  showSimpleFilterBuilder?: boolean;
  showFilterButton?: boolean;
  showSelectedButton?: boolean;
  showSystemTimeButton?: boolean;
  showGroupButton?: boolean;
  showFooter?: boolean;
  allowChangeColumns?: boolean;
  allowChangeOrder?: boolean;
  create?: boolean;
  createButtonClass?: string;
  view?: boolean | "InPlace";
  largeToolbarButtons?: boolean;
  defaultRefreshMode?: RefreshMode;
  avoidChangeUrl?: boolean;
  throwIfNotFindable?: boolean;
  deps?: React.DependencyList;
  extraOptions?: any;
  enableAutoFocus?: boolean;
  simpleFilterBuilder?: (sfbc: Finder.SimpleFilterBuilderContext) => React.ReactElement | undefined;
  onNavigated?: (lite: Lite<Entity>) => void;
  onDoubleClick?: (e: React.MouseEvent<any>, row: ResultRow) => void;
  onSelectionChanged?: (rows: ResultRow[], reason: SelectionChangeReason) => void;
  onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
  onHeighChanged?: () => void;
  onSearch?: (fo: FindOptionsParsed, dataChange: boolean, scl: SearchControlLoaded) => void;
  onResult?: (table: ResultTable, dataChange: boolean, scl: SearchControlLoaded) => void;
  //Return "no_change" to prevent refresh. Navigator.view won't be called by search control, but returning an entity allows to return it immediatly in a SearchModal in find mode.  
  onCreate?: (scl: SearchControlLoaded) => Promise<undefined | void | EntityPack<any> | ModifiableEntity | "no_change">;
  onCreateFinished?: (entity: EntityPack<Entity> | ModifiableEntity | Lite<Entity> | undefined | void, scl: SearchControlLoaded) => void;
  ctx?: StyleContext;
  customRequest?: (req: QueryRequest, fop: FindOptionsParsed) => Promise<ResultTable>;
  onPageSubTitleChanged?: () => void;
  mobileOptions?: (fop: FindOptionsParsed) => SearchControlMobileOptions;
  onDrilldown?: (scl: SearchControlLoaded, row: ResultRow, options?: OnDrilldownOptions) => Promise<boolean | undefined>;
  showTitle?: HeaderType;
  title?: React.ReactElement | string;
}

function is_touch_device(): boolean {
  return 'ontouchstart' in window        // works on most browsers 
    || Boolean(navigator.maxTouchPoints);       // works on IE10/11 and Surface
}

export interface SearchControlHandler {
  findOptions: FindOptions;
  doSearch(opts: { dataChanged?: boolean, force?: boolean }): void;
  doSearchPage1(force?: boolean): void;
  searchControlLoaded: SearchControlLoaded | null;
}

export namespace SearchControlOptions {
  export let showSelectedButton = (sc: SearchControlHandler, p: SearchControlProps): boolean => (p.showSelectedButton ?? true) && is_touch_device();
  export let showSystemTimeButton = (sc: SearchControlHandler, p: SearchControlProps): boolean => (p.showSystemTimeButton ?? false);
  export let showGroupButton = (sc: SearchControlHandler, p: SearchControlProps): boolean => (p.showGroupButton ?? false);
  export let showFilterButton = (sc: SearchControlHandler, p: SearchControlProps): boolean => (p.showFilterButton ?? true);
  export let allowChangeColumns = (sc: SearchControlHandler, p: SearchControlProps): boolean => (p.allowChangeColumns ?? true);
  export let allowOrderColumns = (sc: SearchControlHandler, p: SearchControlProps): boolean => (p.allowChangeOrder ?? true);
  export let showFooter = (sc: SearchControlHandler, p: SearchControlProps): boolean | undefined => p.showFooter;
}

const SearchControl: React.ForwardRefExoticComponent<SearchControlProps & React.RefAttributes<SearchControlHandler>> = React.forwardRef(function SearchControl(p: SearchControlProps, ref: React.Ref<SearchControlHandler>) {

  const searchControlLoaded = React.useRef<SearchControlLoaded>(null);
  const lastDeps = usePrevious(p.deps);
  //const lastFO = usePrevious(p.findOptions);
  //const lastFrame = usePrevious({ currentDate: p.ctx?.frame?.currentDate, previousDateda: p.ctx?.frame?.previousDate });

  const handler: SearchControlHandler = {
    findOptions: p.findOptions,
    get searchControlLoaded() {
      return searchControlLoaded.current;
    },
    doSearch: opts => searchControlLoaded.current && searchControlLoaded.current.doSearch(opts),
    doSearchPage1: force => searchControlLoaded.current && searchControlLoaded.current.doSearchPage1(force),
  };
  React.useImperativeHandle(ref, () => handler, [p.findOptions, searchControlLoaded.current]);

  const qd = useAPI<QueryDescription | "not-allowed">(() => {

    if (!Finder.isFindable(p.findOptions.queryName, false)) {
      if (p.throwIfNotFindable)
        throw Error(`Query ${getQueryKey(p.findOptions.queryName)} not allowed`);

      return "not-allowed";
    }

    return Finder.getQueryDescription(p.findOptions.queryName);
  }, [getQueryKey(p.findOptions.queryName)]);


  const fop = useAPI<FindOptionsParsed | string | null>(async (abort, oldFop) => {
    if (qd == null || qd == "not-allowed")
      return null;

    const message = Finder.validateNewEntities(p.findOptions);
    if (message)
      return message;

    if (oldFop && typeof oldFop == "object") {
      const oldFo = Finder.toFindOptions(oldFop, qd, p.defaultIncludeDefaultFilters!);
      if (Finder.findOptionsPath(p.findOptions) == Finder.findOptionsPath(oldFo))
        return oldFop;
    }

    const fop = await Finder.parseFindOptions(p.findOptions, qd, p.defaultIncludeDefaultFilters!);

    if (fop.systemTime == undefined && p.ctx?.frame?.currentDate && p.ctx.frame!.previousDate &&
      Finder.isSystemVersioned(qd.columns["Entity"].type)) {

      fop.systemTime = {
        mode: 'Between',
        joinMode: 'FirstCompatible',
        startDate: p.ctx.frame.previousDate,
        endDate: p.ctx.frame.currentDate
      };

      const cops = await Finder.parseColumnOptions([
        { token: QueryTokenString.entity().systemValidFrom(), hiddenColumn: true },
        { token: QueryTokenString.entity().systemValidTo(), hiddenColumn: true }
      ], fop.groupResults, qd);

      fop.columnOptions = [...cops, ...fop.columnOptions];

      fop.orderOptions = [...fop.orderOptions, { token: cops[0].token!, orderType: "Descending" }];
    }

    return fop;

  }, [qd, Finder.findOptionsPath(p.findOptions), p.ctx?.frame?.currentDate, p.ctx?.frame?.previousDate], { avoidReset: true });

  if (qd == null || qd == "not-allowed")
    return null;

  if (fop == null)
    return null;

  if (typeof fop == "string") {
    return (
      <div className="alert alert-danger" role="alert">
        <strong>Error in SearchControl ({getQueryKey(p.findOptions.queryName)}): </strong>
        {fop}
      </div>
    );
  }

  if (fop.queryKey != qd.queryKey)
    return null;

  const qs = Finder.getSettings(fop.queryKey);

  const tis = getTypeInfos(qd.columns["Entity"].type);

  return (
    <ErrorBoundary>
      {p.showTitle && <Title type={p.showTitle}>{p.title ?? getQueryNiceName(qd.queryKey)}</Title>}
      <SearchControlLoaded ref={searchControlLoaded}
        findOptions={fop}
        queryDescription={qd}
        querySettings={qs}

        formatters={p.formatters}
        rowAttributes={p.rowAttributes}
        entityFormatter={p.entityFormatter}
        extraButtons={p.extraButtons}
        getViewPromise={p.getViewPromise}
        maxResultsHeight={p.maxResultsHeight === undefined ? "400px" : p.maxResultsHeight}
        tag={p.tag}

        defaultIncudeDefaultFilters={p.defaultIncludeDefaultFilters ?? false}
        searchOnLoad={p.searchOnLoad != null ? p.searchOnLoad : true}
        showHeader={p.showHeader != null ? p.showHeader : true}
        avoidTableFooterContainer={p.avoidTableFooterContainer ?? false}
        avoidGroupByMessage={p.avoidGroupByMessage ?? false}
        pinnedFilterVisible={p.pinnedFilterVisible}
        showFilters={p.showFilters != null ? p.showFilters : false}
        showSimpleFilterBuilder={p.showSimpleFilterBuilder != null ? p.showSimpleFilterBuilder : true}
        showFilterButton={SearchControlOptions.showFilterButton(handler, p)}
        showSystemTimeButton={SearchControlOptions.showSystemTimeButton(handler, p) && (qs?.allowSystemTime ?? tis.some(a => a.isSystemVersioned == true))}
        showGroupButton={SearchControlOptions.showGroupButton(handler, p)}
        showSelectedButton={SearchControlOptions.showSelectedButton(handler, p)}
        showFooter={SearchControlOptions.showFooter(handler, p)}
        allowChangeColumns={SearchControlOptions.allowChangeColumns(handler, p)}
        allowChangeOrder={SearchControlOptions.allowOrderColumns(handler, p)}
        create={p.create != null ? p.create : (qs?.allowCreate ?? true) && (fop => tis.some(ti => Navigator.isCreable(ti, {isSearch: true, fo: fop })))}
        createButtonClass={p.createButtonClass}

        view={p.view != null ? p.view : tis.some(ti => Navigator.isViewable(ti, { isSearch: "main" }))}

        allowSelection={p.allowSelection != null ? p.allowSelection : qs && qs.allowSelection != null ? qs!.allowSelection : true}
        showContextMenu={p.showContextMenu ?? qs?.showContextMenu ?? ((fo) => fo.groupResults ? "Basic" : true)}
        hideButtonBar={p.hideButtonBar != null ? p.hideButtonBar : false}
        hideFullScreenButton={p.hideFullScreenButton != null ? p.hideFullScreenButton : false}
        showBarExtension={p.showBarExtension != null ? p.showBarExtension : true}
        showBarExtensionOption={p.showBarExtensionOption}
        largeToolbarButtons={p.largeToolbarButtons != null ? p.largeToolbarButtons : false}
        defaultRefreshMode={p.defaultRefreshMode}
        avoidChangeUrl={p.avoidChangeUrl != null ? p.avoidChangeUrl : false}
        deps={[fop, ... (p.deps ?? [])]}
        extraOptions={p.extraOptions}

        enableAutoFocus={p.enableAutoFocus == null ? false : p.enableAutoFocus}
        simpleFilterBuilder={p.simpleFilterBuilder}

        onCreate={p.onCreate}
        onCreateFinished={p.onCreateFinished}
        onNavigated={p.onNavigated}
        onSearch={p.onSearch}
        onDoubleClick={p.onDoubleClick}
        onSelectionChanged={p.onSelectionChanged}
        onFiltersChanged={p.onFiltersChanged}
        onHeighChanged={p.onHeighChanged}
        onResult={p.onResult}

        ctx={p.ctx}
        customRequest={p.customRequest}
        onPageTitleChanged={p.onPageSubTitleChanged}

        selectionFormatter={p.selectionFromatter}

        mobileOptions={p.mobileOptions}
        onDrilldown={p.onDrilldown}
      />
    </ErrorBoundary>
  );
});

export default SearchControl;

export interface ISimpleFilterBuilder {
  getFilters(): FilterOption[];
  onDataChanged?(): void;
}


