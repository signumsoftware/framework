import * as React from 'react'
import { useLocation, useParams } from 'react-router'
import { Finder } from '../Finder'
import { FindOptions, QueryDescription } from '../FindOptions'
import { getQueryNiceName } from '../Reflection'
import * as AppContext from '../AppContext';
import SearchControl, { SearchControlHandler } from './SearchControl'
import SearchControlLoaded from './SearchControlLoaded'
import { useTitle } from '../AppContext'
import { QueryString } from '../QueryString'
import { useAPI, useForceUpdate } from '../Hooks'
import { usePageUIState } from '../Modals'


function SearchPage(): React.ReactElement {

  const params = useParams<{ queryName: string }>();
  const location = useLocation();
  const fo = Finder.parseFindOptionsPath(params.queryName!, QueryString.parse(location.search));
  const qd = useAPI(() => Finder.getQueryDescription(fo.queryName), [fo.queryName]);
  const forceUpdate = useForceUpdate();

  usePageUIState(() => {
    const scl = searchControl.current?.searchControlLoaded;
    return { name: "SearchPage", context: scl && Finder.toFindOptions(scl.props.findOptions, scl.props.queryDescription, scl.props.defaultIncudeDefaultFilters) };
  });
  

  React.useEffect(() => {
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  function onResize() {
    const sc = searchControl.current;
    const scl = sc?.searchControlLoaded;
    const containerDiv = scl?.containerDiv;
    if (containerDiv) {
      const marginTop = containerDiv.offsetTop;
      const maxHeight = (window.innerHeight - (marginTop + SearchPage.Options.marginDown));
      containerDiv.style.maxHeight = Math.max(maxHeight, SearchPage.Options.minHeight) + "px";
    }
  }

  const searchControl = React.useRef<SearchControlHandler | null | undefined>(undefined);

  useTitle(getQueryNiceName(params.queryName!));

  function changeUrl() {
    const scl = searchControl.current!.searchControlLoaded!;
    const findOptions = Finder.toFindOptions(scl.props.findOptions, scl.props.queryDescription, true);
    const newPath = Finder.findOptionsPath(findOptions, scl.extraUrlParams);

    if (location.pathname + location.search != newPath)
      AppContext.navigate(newPath, { replace: true });
  }

  if (!Finder.isFindable(fo.queryName, true))
    return (
      <div id="divSearchPage">
        <h3>
          <span className="display-6 sf-query-title">{getQueryNiceName(fo.queryName)}</span>
          <small>Error: Query not allowed {Finder.isFindable(fo.queryName, false) ? "in full screen" : ""}</small>
        </h3>
      </div>
    );

  const setSearchControl = React.useCallback(function(sc: SearchControlHandler | null) {
    searchControl.current = sc;
    onResize();
  }, []);

  var qs = Finder.getSettings(fo.queryName);
  return (
    <div id="divSearchPage" className="sf-search-page">
      <h1 tabIndex={0} className="display-6 sf-query-title h3 d-flex align-items-center">
        {SearchPage.renderTitle(searchControl.current?.searchControlLoaded, <span>{getQueryNiceName(fo.queryName)}</span>)}
        {searchControl.current?.searchControlLoaded && SearchPage.renderTitleElements(searchControl.current.searchControlLoaded)}
      </h1>
      {qd && <SearchControl ref={setSearchControl}
        defaultIncludeDefaultFilters={true}
        findOptions={fo}
        tag="SearchPage"
        throwIfNotFindable={true}
        showBarExtension={true}
        allowSelection={qs && qs.allowSelection}
        hideFullScreenButton={true}
        largeToolbarButtons={true}
        showFilters={SearchPage.Options.showFilters(fo, qd, qs)}
        showGroupButton={true}
        showSystemTimeButton={true}
        showFooter={true}
        avoidChangeUrl={false}
        view={qs?.inPlaceNavigation ? "InPlace" : undefined}
        maxResultsHeight={"none"}
        enableAutoFocus={true}
        onHeighChanged={onResize}
        onSearch={result => changeUrl()}
        onPageTitleChanged={forceUpdate}
      />
      }
    </div>
  );
}

namespace SearchPage {
  export const Options: {
    marginDown: number;
    minHeight: number;
    showFilters: (fo: FindOptions, qd: QueryDescription, qs: Finder.QuerySettings | undefined) => boolean;
  } = {
    marginDown: 70,
    minHeight: 600,
    showFilters: () => false
  };



  export function renderTitle(scl: SearchControlLoaded | null | undefined, defaultTitle: React.ReactNode): React.ReactNode {
    if (scl != null) {
      for (const f of Finder.Options.onSearchPageRenderTitle) {
        const node = f(scl, defaultTitle);
        if (node != null)
          return node;
      }
    }

    return defaultTitle;
  }

  export function renderTitleElements(scl: SearchControlLoaded): React.ReactNode {
    const elements = Finder.Options.onSearchPageTitleElements.map(f => f(scl)).filter(e => e != null);
    if (elements.length == 0)
      return null;

    return (
      <span className="ms-auto d-inline-flex align-items-center fs-6 fw-normal">
        {elements.map((e, i) => <React.Fragment key={i}>{e}</React.Fragment>)}
      </span>
    );
  }
}

export default SearchPage;
