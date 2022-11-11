import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { RouteComponentProps } from 'react-router'
import * as Finder from '../Finder'
import { FindOptions, FilterOption, isFilterGroupOption, QueryDescription } from '../FindOptions'
import { getQueryNiceName } from '../Reflection'
import * as Navigator from '../Navigator'
import * as AppContext from '../AppContext';
import SearchControl, { SearchControlHandler } from './SearchControl'
import { namespace } from 'd3'
import { useTitle } from '../AppContext'
import { QueryString } from '../QueryString'
import { useAPI, useForceUpdate, useUpdatedRef } from '../Hooks'

interface SearchPageProps extends RouteComponentProps<{ queryName: string }> {

}

function SearchPage(p: SearchPageProps) {

  const fo = Finder.parseFindOptionsPath(p.match.params.queryName, QueryString.parse(p.location.search));
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

  const searchControl = React.useRef<SearchControlHandler>(null);

  var subTitle = searchControl.current?.searchControlLoaded?.pageSubTitle;

  useTitle(getQueryNiceName(p.match.params.queryName) + (subTitle ? (" - " + subTitle) : ""));

  function changeUrl() {
    const scl = searchControl.current!.searchControlLoaded!;
    const findOptions = Finder.toFindOptions(scl.props.findOptions, scl.props.queryDescription, true);
    const newPath = Finder.findOptionsPath(findOptions, scl.extraUrlParams);
    const currentLocation = AppContext.history.location;

    if (currentLocation.pathname + currentLocation.search != newPath)
      AppContext.history.replace(newPath);
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
      {qd && <SearchControl ref={searchControl}
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
        extraButtons={qs?.extraButtons}
        onPageSubTitleChanged={forceUpdate}
      />
      }
    </div>
  );
}


function anyPinned(filterOptions?: FilterOption[]): boolean {
  if (filterOptions == null)
    return false;

  return filterOptions.some(a => Boolean(a.pinned) || isFilterGroupOption(a) && anyPinned(a.filters.notNull()));
}


namespace SearchPage {
  export let marginDown = 90;
  export let minHeight = 600;
  export let showFilters = (fo: FindOptions, qd: QueryDescription, qs: Finder.QuerySettings | undefined) => {
    return false;
  }
}

export default SearchPage;
