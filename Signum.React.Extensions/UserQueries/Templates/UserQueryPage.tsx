import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '@framework/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite } from '@framework/Signum.Entities'
import { getQueryNiceName, newLite } from '@framework/Reflection'
import SearchControl, { SearchControlHandler } from '@framework/SearchControl/SearchControl'
import { UserQueryEntity } from '../Signum.Entities.UserQueries'
import * as UserQueryClient from '../UserQueryClient'
import { RouteComponentProps } from "react-router";
import { useAPI, useForceUpdate } from '@framework/Hooks'
import { useState } from 'react'
import { translated } from '../../Translation/TranslatedInstanceTools'
import SearchPage from '@framework/SearchControl/SearchPage'
import { useTitle } from '../../../Signum.React/Scripts/AppContext'

interface UserQueryPageProps extends RouteComponentProps<{ userQueryId: string; entity?: string }> {

}

export default function UserQueryPage(p: UserQueryPageProps) {

  const [currentUserQuery, setCurrentUserQuery] = useState<UserQueryEntity | null>(null);

  const { userQueryId, entity } = p.match.params;

  const forceUpdate = useForceUpdate();
  
  const fo = useAPI(() => {
    return Navigator.API.fetchEntity(UserQueryEntity, userQueryId)
      .then(uq => {
        setCurrentUserQuery(uq);
        const lite = entity == undefined ? undefined : parseLite(entity);
        return Navigator.API.fillLiteModels(lite)
          .then(() => UserQueryClient.Converter.toFindOptions(uq, lite))
      })
  }, [userQueryId, entity]);

  const searchControl = React.useRef<SearchControlHandler>(null);

  var subTitle = searchControl.current?.searchControlLoaded?.pageSubTitle;


  useTitle(fo == null ? JavascriptMessage.loading.niceToString() : (getQueryNiceName(fo.queryName) + (subTitle ? (" - " + subTitle) : "")));

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

  React.useEffect(() => {
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);


  if (fo == undefined || currentUserQuery == null)
    return null;

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

      {currentUserQuery && <SearchControl ref={searchControl}
        defaultIncludeDefaultFilters={true}
        findOptions={fo}
        tag="UserQueryPage"
        throwIfNotFindable={true}
        showBarExtension={true}
        allowSelection={qs && qs.allowSelection}
        hideFullScreenButton={true}
        largeToolbarButtons={true}
        showFilters={false /*consider adding uq.showFilters*/}
        showGroupButton={true}
        showSystemTimeButton={true}
        showFooter={true}
        view={qs?.inPlaceNavigation ? "InPlace" : undefined}
        extraOptions={{ userQuery: newLite(UserQueryEntity, userQueryId), customDrilldowns: currentUserQuery.customDrilldowns }}
        defaultRefreshMode={currentUserQuery.refreshMode}
        searchOnLoad={currentUserQuery.refreshMode == "Auto"}
        onHeighChanged={onResize}
        extraButtons={qs?.extraButtons}
        onPageSubTitleChanged={forceUpdate}
      />
      }
    </div>
  );
}



