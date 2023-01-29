import { Location } from 'history'
import * as React from 'react'
import * as Navigator from '@framework/Navigator'
import * as AppContext from '@framework/AppContext'
import * as Finder from '@framework/Finder'
import { FindOptions, SearchValue } from '@framework/Search'
import { Lite, liteKey } from '@framework/Signum.Entities'
import { IconColor, ToolbarConfig, ToolbarResponse } from '../Toolbar/ToolbarClient'
import * as UserQueryClient from './UserQueryClient'
import { UserQueryEntity } from './Signum.Entities.UserQueries'
import { useAPI } from '@framework/Hooks';
import { SearchToolbarCount, ToolbarCount } from '../Toolbar/QueryToolbarConfig';
import { useFetchInState } from '@framework/Navigator'
import { parseIcon } from '../Basics/Templates/IconTypeahead'

export default class UserQueryToolbarConfig extends ToolbarConfig<UserQueryEntity> {
  constructor() {
    var type = UserQueryEntity;
    super(type);
  }

  getIcon(element: ToolbarResponse<UserQueryEntity>) {

    if (element.showCount != null) {
      return (
        <>
          {super.getIcon(element)}
          <SearchUserQueryCount userQuery={element.content!} color={element.iconColor} autoRefreshPeriod={element.autoRefreshPeriod} />
        </>
      );
    }

    return super.getIcon(element);
  }

  getDefaultIcon(): IconColor {
    return ({
      icon: ["far", "rectangle-list"],
      iconColor: "dodgerblue",
    });
  }

  handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<UserQueryEntity>) {
    if (!res.openInPopup)
      super.handleNavigateClick(e, res);
    else {
      Navigator.API.fetch(res.content!)
        .then(uq => UserQueryClient.Converter.toFindOptions(uq, undefined)
          .then(fo => Finder.explore(fo, { searchControlProps: { extraOptions: { userQuery: res.content, customDrilldowns: uq.customDrilldowns } } })));
    }
  }

  navigateTo(res: ToolbarResponse<UserQueryEntity>): Promise<string> {
    return Navigator.API.fetch(res.content!)
      .then(uq => {
        if (uq.refreshMode == "Manual")
          return Promise.resolve(UserQueryClient.userQueryUrl(res.content!));

        return UserQueryClient.Converter.toFindOptions(uq, undefined)
          .then(fo => Finder.findOptionsPath(fo, { userQuery: liteKey(res.content!), customDrilldowns: uq.customDrilldowns }));
      });
  }

  isCompatibleWithUrlPrio(res: ToolbarResponse<UserQueryEntity>, location: Location, query: any): number {
    return query["userQuery"] == liteKey(res.content!) ||
      location.pathname == AppContext.toAbsoluteUrl(UserQueryClient.userQueryUrl(res.content!)) ? 2 : 0;
  }
}


interface CountUserQueryIconProps {
  userQuery: Lite<UserQueryEntity>;
  color?: string;
  autoRefreshPeriod?: number;
}


export function SearchUserQueryCount(p: CountUserQueryIconProps) {

  var userQuery = useFetchInState(p.userQuery)
  var findOptions = useAPI(signal => userQuery ? UserQueryClient.Converter.toFindOptions(userQuery, undefined) : Promise.resolve(undefined), [userQuery]);

  if (findOptions == null)
    return <ToolbarCount num={ undefined} />;

  return <SearchToolbarCount findOptions={findOptions} autoRefreshPeriod={p.autoRefreshPeriod} color={p.color} />
}
