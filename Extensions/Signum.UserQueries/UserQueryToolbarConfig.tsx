import * as React from 'react'
import { Location } from 'react-router'
import { Navigator } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { Entity, Lite, liteKey, parseLite } from '@framework/Signum.Entities'
import { IconColor, ToolbarConfig } from '../Signum.Toolbar/ToolbarConfig'
import { UserQueryClient } from './UserQueryClient'
import { UserQueryEntity } from './Signum.UserQueries'
import { useAPI } from '@framework/Hooks';
import { SearchToolbarCount, ToolbarCount } from '../Signum.Toolbar/QueryToolbarConfig';
import { ShowCount } from '../Signum.Toolbar/Signum.Toolbar'
import { ToolbarResponse } from '../Signum.Toolbar/ToolbarClient'
import { IconProp } from '@fortawesome/fontawesome-svg-core';


export default class UserQueryToolbarConfig extends ToolbarConfig<UserQueryEntity> {
  constructor() {
    var type = UserQueryEntity;
    super(type);
  }


  getCounter(element: ToolbarResponse<UserQueryEntity>, entity: Lite<Entity> | null): React.ReactElement | undefined {

    if (element.showCount != null) {
      return <SearchUserQueryCount userQuery={element.content!}
        entity={entity}
        color={element.iconColor}
        autoRefreshPeriod={element.autoRefreshPeriod}
        showCount={element.showCount} />
    }

    return undefined;
  }

  getDefaultIcon(): IconProp {
    return "rectangle-list";
  }

  handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<UserQueryEntity>, selectedEntity: Lite<Entity> | null): void {
    if (!res.openInPopup)
      super.handleNavigateClick(e, res, selectedEntity);
    else {
      Navigator.API.fetch(res.content!)
        .then(uq => UserQueryClient.Converter.toFindOptions(uq, undefined)
          .then(fo => Finder.explore(fo, { searchControlProps: { extraOptions: { userQuery: res.content, entity: selectedEntity && liteKey(selectedEntity) } } })));
    }
  }

  navigateTo(res: ToolbarResponse<UserQueryEntity>, selectedEntity: Lite<Entity>| null): Promise<string> {
    return Navigator.API.fetch(res.content!)
      .then(uq => UserQueryClient.getUserQueryUrl(uq, selectedEntity ?? undefined));
  }

  isCompatibleWithUrlPrio(res: ToolbarResponse<UserQueryEntity>, location: Location, query: any, entityType?: string): { prio: number, inferredEntity?: Lite<Entity> } | null {
    if (query["userQuery"] == liteKey(res.content!)) {
      return { prio: 2, inferredEntity: query["entity"] && parseLite(query["entity"]) }
    }
    return null;
  }
}


interface CountUserQueryIconProps {
  userQuery: Lite<UserQueryEntity>;
  entity: Lite<Entity> | null;
  color?: string;
  autoRefreshPeriod?: number;
  showCount: ShowCount;
}


export function SearchUserQueryCount(p: CountUserQueryIconProps): React.JSX.Element {

  var userQuery = Navigator.useFetchInState(p.userQuery)
  var findOptions = useAPI(signal => userQuery && UserQueryClient.Converter.toFindOptions(userQuery, p.entity ?? undefined), [userQuery, p.entity]);

  if (findOptions == null)
    return <ToolbarCount num={undefined} showCount={p.showCount} />;

  return <SearchToolbarCount findOptions={findOptions} autoRefreshPeriod={p.autoRefreshPeriod} color={p.color} showCount={p.showCount} />
}
