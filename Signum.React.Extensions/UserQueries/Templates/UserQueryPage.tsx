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
import { useAPI } from '@framework/Hooks'
import { useState } from 'react'
import { translated } from '../../Translation/TranslatedInstanceTools'
import SearchPage from '@framework/SearchControl/SearchPage'

interface UserQueryPageProps extends RouteComponentProps<{ userQueryId: string; entity?: string }> {

}

export default function UserQueryPage(p: UserQueryPageProps) {

  const [currentUserQuery, setCurrentUserQuery] = useState<UserQueryEntity | null>(null);

  const { userQueryId, entity } = p.match.params;

  const fo = useAPI(() => {
    return Navigator.API.fetchEntity(UserQueryEntity, userQueryId)
      .then(uq => {
        setCurrentUserQuery(uq);
        const lite = entity == undefined ? undefined : parseLite(entity);
        return Navigator.API.fillToStrings(lite)
          .then(() => UserQueryClient.Converter.toFindOptions(uq, lite))
      })
  }, [userQueryId, entity]);

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

  const searchControl = React.useRef<SearchControlHandler>(null);

  if (fo == undefined || currentUserQuery == null)
    return null;

  var qs = Finder.getSettings(fo.queryName);
  return (
    <div id="divSearchPage" className="sf-search-page">
      <h2>
        <span className="sf-entity-title">{getQueryNiceName(fo.queryName)}</span>&nbsp;
        <a className="sf-popup-fullscreen" href="#" onClick={(e) => searchControl.current!.searchControlLoaded!.handleFullScreenClick(e)}>
          <FontAwesomeIcon icon="external-link-alt" />
        </a>
        <p className="lead">
          ({translated(currentUserQuery, a => a.displayName)})
      </p>
      </h2>

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
        view={qs?.inPlaceNavigation ? "InPlace" : undefined}
        extraOptions={{ userQuery: newLite(UserQueryEntity, userQueryId) }}
        defaultRefreshMode={currentUserQuery.refreshMode}
        searchOnLoad={currentUserQuery.refreshMode == "Auto"}
        onHeighChanged={onResize}
        extraButtons={qs?.extraButtons}
      />
      }
    </div>
  );
}



