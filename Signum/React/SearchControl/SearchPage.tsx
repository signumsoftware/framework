import * as React from 'react'
import { useLocation, useParams } from 'react-router'
import { Finder } from '../Finder'
import { FindOptions, QueryDescription } from '../FindOptions'
import { getQueryNiceName } from '../Reflection'
import * as AppContext from '../AppContext';
import SearchControl, { SearchControlHandler } from './SearchControl'
import { useTitle } from '../AppContext'
import { QueryString } from '../QueryString'
import { useAPI, useForceUpdate } from '../Hooks'


function SearchPage(): React.ReactElement {

  const params = useParams<{ queryName: string }>();
  const location = useLocation();
  const fo = Finder.parseFindOptionsPath(params.queryName!, QueryString.parse(location.search));
  const qd = useAPI(() => Finder.getQueryDescription(fo.queryName), [fo.queryName]);
  const forceUpdate = useForceUpdate();
  

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
      const maxHeight = (window.innerHeight - (marginTop + SearchPage.marginDown));
      containerDiv.style.maxHeight = Math.max(maxHeight, SearchPage.minHeight) + "px";
    }
  }

  const searchControl = React.useRef<SearchControlHandler | null | undefined>(undefined);

  const subTitle = searchControl.current?.searchControlLoaded?.pageSubTitle;

  useTitle(getQueryNiceName(params.queryName!) + (subTitle ? (" - " + subTitle) : ""));

  function changeUrl() {
    const scl = searchControl.current!.searchControlLoaded!;
    const findOptions = Finder.toFindOptions(scl.props.findOptions, scl.props.queryDescription, true);
    const newPath = Finder.findOptionsPath(findOptions, scl.extraUrlParams);

    if (location.pathname + location.search != newPath)
      AppContext.navigate(newPath, { replace : true });
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

  const setSearchControl = React.useCallback(function (sc: SearchControlHandler | null) {
    searchControl.current = sc;
    onResize();
  }, []);

  var qs = Finder.getSettings(fo.queryName);
  return (
    <div id="divSearchPage" className="sf-search-page">
      <h3 className="display-6 sf-query-title">
        <span>{getQueryNiceName(fo.queryName)}</span>
        {searchControl.current?.searchControlLoaded?.pageSubTitle && <>
          <small className="sf-type-nice-name text-muted"> - {searchControl.current?.searchControlLoaded?.pageSubTitle}</small>
        </>
        }
      </h3>
      {qd && <SearchControl ref={setSearchControl}
        defaultIncludeDefaultFilters={true}
        findOptions={fo}
        tag="SearchPage"
        throwIfNotFindable={true}
        showBarExtension={true}
        allowSelection={qs && qs.allowSelection}
        hideFullScreenButton={true}
        largeToolbarButtons={true}
        showFilters={SearchPage.showFilters(fo, qd, qs)}
        showGroupButton={true}
        showSystemTimeButton={true}
        showFooter={true}
        avoidChangeUrl={false}
        view={qs?.inPlaceNavigation ? "InPlace" : undefined}
        maxResultsHeight={"none"}
        enableAutoFocus={true}
        onHeighChanged={onResize}
        onSearch={result => changeUrl()}
        onPageSubTitleChanged={forceUpdate}
      />
      }
    </div>
  );
}

namespace SearchPage {
  export let marginDown = 70;
  export let minHeight = 600;
  export let showFilters = (fo: FindOptions, qd: QueryDescription, qs: Finder.QuerySettings | undefined) => {
    return false;
  }
}

export default SearchPage;
