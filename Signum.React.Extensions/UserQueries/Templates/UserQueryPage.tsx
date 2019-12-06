import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Dic } from '@framework/Globals'
import * as Finder from '@framework/Finder'
import * as Navigator from '@framework/Navigator'
import { ResultTable, FindOptions, FilterOption, QueryDescription } from '@framework/FindOptions'
import { SearchMessage, JavascriptMessage, parseLite } from '@framework/Signum.Entities'
import { getQueryNiceName } from '@framework/Reflection'
import SearchControl, { SearchControlHandler } from '@framework/SearchControl/SearchControl'
import { UserQueryEntity } from '../Signum.Entities.UserQueries'
import * as UserQueryClient from '../UserQueryClient'
import { RouteComponentProps } from "react-router";
import { useAPI } from '../../../../Framework/Signum.React/Scripts/Hooks'

interface UserQueryPageProps extends RouteComponentProps<{ userQueryId: string; entity?: string }> {

}

export default function UserQueryPage(p: UserQueryPageProps) {

  const { userQueryId, entity } = p.match.params;

  const fo = useAPI(() => {
    const lite = entity == undefined ? undefined : parseLite(entity);
    return Navigator.API.fillToStrings(lite)
      .then(() => Navigator.API.fetchEntity(UserQueryEntity, userQueryId))
      .then(uc => UserQueryClient.Converter.toFindOptions(uc, lite))
  }, [userQueryId, entity]);


  const searchControl = React.useRef<SearchControlHandler>(null);


  if (fo == undefined)
    return null;

  return (
    <div id="divSearchPage">
      <h2>
        <span className="sf-entity-title">{getQueryNiceName(fo.queryName)}</span>&nbsp;
        <a className="sf-popup-fullscreen" href="#" onClick={(e) => searchControl.current!.searchControlLoaded!.handleFullScreenClick(e)}>
          <FontAwesomeIcon icon="external-link-alt" />
        </a>
      </h2>
      <SearchControl ref={searchControl}
        showFilters={UserQueryPage.showFilters}
        hideFullScreenButton={true}
        showBarExtension={true}
        findOptions={fo} />
    </div>
  );
}



  UserQueryPage.showFilters = true;


