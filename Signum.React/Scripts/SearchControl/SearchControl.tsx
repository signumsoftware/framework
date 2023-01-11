import * as React from 'react'
import * as Finder from '../Finder'
import { CellFormatter, EntityFormatter } from '../Finder'
import { ResultTable, ResultRow, FindOptions, FindOptionsParsed, FilterOptionParsed, FilterOption, QueryDescription, QueryRequest } from '../FindOptions'
import { Lite, Entity, ModifiableEntity, EntityPack } from '../Signum.Entities'
import { tryGetTypeInfos, getQueryKey, getTypeInfos } from '../Reflection'
import * as Navigator from '../Navigator'
import SearchControlLoaded, { SearchControlMobileOptions, SearchControlViewMode, ShowBarExtensionOption } from './SearchControlLoaded'
import { ErrorBoundary } from '../Components';
import { Property } from 'csstype';
import "./Search.css"
import { ButtonBarElement, StyleContext } from '../TypeContext';
import { useForceUpdate, usePrevious, useStateWithPromise } from '../Hooks'
import { RefreshMode } from '../Signum.Entities.DynamicQuery';

export interface SimpleFilterBuilderProps {
  findOptions: FindOptions;
}

export interface SearchControlProps {
  findOptions: FindOptions;
  formatters?: { [token: string]: CellFormatter };
  rowAttributes?: (row: ResultRow, columns: string[]) => React.HTMLAttributes<HTMLTableRowElement> | undefined;
  entityFormatter?: EntityFormatter;
  extraButtons?: (searchControl: SearchControlLoaded) => (ButtonBarElement | null | undefined | false)[];
  getViewPromise?: (e: any /*Entity*/) => undefined | string | Navigator.ViewPromise<any /*Entity*/>;
  maxResultsHeight?: Property.MaxHeight<string | number> | any;
  tag?: string | {};
  searchOnLoad?: boolean;
  allowSelection?: boolean | "single";
  showContextMenu?: (fop: FindOptionsParsed) => boolean | "Basic";
  hideButtonBar?: boolean;
  hideFullScreenButton?: boolean;
  defaultIncludeDefaultFilters?: boolean;
  showHeader?: boolean | "PinnedFilters";
  pinnedFilterVisible?: (fop: FilterOptionParsed) => boolean;
  showBarExtension?: boolean;
  showBarExtensionOption?: ShowBarExtensionOption;
  showFilters?: boolean;
  showSimpleFilterBuilder?: boolean;
  showFilterButton?: boolean;
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
  simpleFilterBuilder?: (sfbc: Finder.SimpleFilterBuilderContext) => React.ReactElement<any> | undefined;
  onNavigated?: (lite: Lite<Entity>) => void;
  onDoubleClick?: (e: React.MouseEvent<any>, row: ResultRow) => void;
  onSelectionChanged?: (rows: ResultRow[]) => void;
  onFiltersChanged?: (filters: FilterOptionParsed[]) => void;
  onHeighChanged?: () => void;
  onSearch?: (fo: FindOptionsParsed, dataChange: boolean, scl: SearchControlLoaded) => void;
  onResult?: (table: ResultTable, dataChange: boolean, scl: SearchControlLoaded) => void;
  //Return "no_change" to prevent refresh. Navigator.view won't be called by search control, but returning an entity allows to return it immediatly in a SearchModal in find mode.  
  onCreate?: (scl: SearchControlLoaded) => Promise<undefined | void | EntityPack<any> | ModifiableEntity | "no_change">;
  onCreateFinished?: (entity: EntityPack<Entity> | ModifiableEntity | Lite<Entity> | undefined | void, scl: SearchControlLoaded) => void;
  styleContext?: StyleContext;
  customRequest?: (req: QueryRequest, fop: FindOptionsParsed) => Promise<ResultTable>;
  onPageSubTitleChanged?: () => void;
  mobileOptions?: (fop: FindOptionsParsed) => SearchControlMobileOptions;
}

export interface SearchControlState {
  queryDescription: QueryDescription;
  findOptions?: FindOptionsParsed;
  message?: string;
}

function is_touch_device(): boolean {
  return 'ontouchstart' in window        // works on most browsers 
    || Boolean(navigator.maxTouchPoints);       // works on IE10/11 and Surface
}

export interface SearchControlHandler {
  findOptions: FindOptions;
  state?: SearchControlState;
  doSearch(opts: { dataChanged?: boolean, force?: boolean }): void;
  doSearchPage1(force?: boolean): void;
  searchControlLoaded: SearchControlLoaded | null;
}

export namespace SearchControlOptions {
  export let showSelectedButton = (sc: SearchControlHandler) => is_touch_device();
  export let showSystemTimeButton = (sc: SearchControlHandler) => true;
  export let showGroupButton = (sc: SearchControlHandler) => true;
}

const SearchControl = React.forwardRef(function SearchControl(p: SearchControlProps, ref: React.Ref<SearchControlHandler>) {

  const [state, setState] = useStateWithPromise<SearchControlState | undefined>(undefined);
  const searchControlLoaded = React.useRef<SearchControlLoaded>(null);
  const lastProps = usePrevious(p);

  const handler: SearchControlHandler = {
    findOptions: p.findOptions,
    get searchControlLoaded() {
      return searchControlLoaded.current;
    },
    state: state,
    doSearch: opts => searchControlLoaded.current && searchControlLoaded.current.doSearch(opts),
    doSearchPage1: force => searchControlLoaded.current && searchControlLoaded.current.doSearchPage1(force),
  };
  React.useImperativeHandle(ref, () => handler, [p.findOptions, state, searchControlLoaded.current]);

  React.useEffect(() => {
    const path = Finder.findOptionsPath(p.findOptions);
    if (path == (lastProps && Finder.findOptionsPath(lastProps.findOptions)))
      return;

    if (state?.findOptions) {
      const fo = Finder.toFindOptions(state.findOptions, state.queryDescription, p.defaultIncludeDefaultFilters!);
      if (path == Finder.findOptionsPath(fo))
        return;
    }

    setState(undefined).then(() => {
      const fo = p.findOptions;
      if (!Finder.isFindable(fo.queryName, false)) {
        if (p.throwIfNotFindable)
          throw Error(`Query ${getQueryKey(fo.queryName)} not allowed`);

        return;
      }

      Finder.getQueryDescription(fo.queryName).then(qd => {
        const message = Finder.validateNewEntities(fo);

        if (message)
          setState({ queryDescription: qd, message: message });
        else
          Finder.parseFindOptions(fo, qd, p.defaultIncludeDefaultFilters!).then(fop => {
            setState({ findOptions: fop, queryDescription: qd });
          });
      });
    });
  }, [p.findOptions]);

  if (state?.message) {
    return (
      <div className="alert alert-danger" role="alert">
        <strong>Error in SearchControl ({getQueryKey(p.findOptions.queryName)}): </strong>
        {state.message}
      </div>
    );
  }

  if (!state || !state.findOptions)
    return null;

  const fop = state.findOptions;
  if (!Finder.isFindable(fop.queryKey, false))
    return null;

  const qs = Finder.getSettings(fop.queryKey);
  const qd = state!.queryDescription!;

  const tis = getTypeInfos(qd.columns["Entity"].type);

  return (
    <ErrorBoundary>
      <SearchControlLoaded ref={searchControlLoaded}
        findOptions={fop}
        queryDescription={qd}
        querySettings={qs}

        formatters={p.formatters}
        rowAttributes={p.rowAttributes}
        entityFormatter={p.entityFormatter}
        extraButtons={p.extraButtons}
        getViewPromise={p.getViewPromise}
        maxResultsHeight={p.maxResultsHeight}
        tag={p.tag}

        defaultIncudeDefaultFilters={p.defaultIncludeDefaultFilters!}
        searchOnLoad={p.searchOnLoad != null ? p.searchOnLoad : true}
        showHeader={p.showHeader != null ? p.showHeader : true}
        pinnedFilterVisible={p.pinnedFilterVisible}
        showFilters={p.showFilters != null ? p.showFilters : false}
        showSimpleFilterBuilder={p.showSimpleFilterBuilder != null ? p.showSimpleFilterBuilder : true}
        showFilterButton={p.showFilterButton != null ? p.showFilterButton : true}
        showSystemTimeButton={SearchControlOptions.showSystemTimeButton(handler) && (p.showSystemTimeButton ?? false) && (qs?.allowSystemTime ?? tis.some(a => a.isSystemVersioned == true))}
        showGroupButton={SearchControlOptions.showGroupButton(handler) && (p.showGroupButton ?? false)}
        showSelectedButton={SearchControlOptions.showSelectedButton(handler)}
        showFooter={p.showFooter}
        allowChangeColumns={p.allowChangeColumns != null ? p.allowChangeColumns : true}
        allowChangeOrder={p.allowChangeOrder != null ? p.allowChangeOrder : true}
        create={p.create != null ? p.create : tis.some(ti => Navigator.isCreable(ti, { isSearch: true }))}
        createButtonClass={p.createButtonClass}

        view={p.view != null ? p.view : tis.some(ti => Navigator.isViewable(ti, { isSearch: true }))}

        allowSelection={p.allowSelection != null ? p.allowSelection : qs && qs.allowSelection != null ? qs!.allowSelection : true}
        showContextMenu={p.showContextMenu ?? qs?.showContextMenu ?? ((fo) => fo.groupResults ? "Basic" : true)}
        hideButtonBar={p.hideButtonBar != null ? p.hideButtonBar : false}
        hideFullScreenButton={p.hideFullScreenButton != null ? p.hideFullScreenButton : false}
        showBarExtension={p.showBarExtension != null ? p.showBarExtension : true}
        showBarExtensionOption={p.showBarExtensionOption}
        largeToolbarButtons={p.largeToolbarButtons != null ? p.largeToolbarButtons : false}
        defaultRefreshMode={p.defaultRefreshMode}
        avoidChangeUrl={p.avoidChangeUrl != null ? p.avoidChangeUrl : true}
        deps={p.deps}
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

        styleContext={p.styleContext}
        customRequest={p.customRequest}
        onPageTitleChanged={p.onPageSubTitleChanged}

        mobileOptions={p.mobileOptions}
      />
    </ErrorBoundary>
  );
});

(SearchControl ).defaultProps = {
  allowSelection: true,
  maxResultsHeight: "400px",
  defaultIncludeDefaultFilters: false,
};

export default SearchControl;

export interface ISimpleFilterBuilder {
  getFilters(): FilterOption[];
  onDataChanged?(): void;
}


