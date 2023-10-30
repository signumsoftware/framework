import * as React from 'react'
import { Location } from 'react-router'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, liteKey } from '@framework/Signum.Entities'
import { ToolbarResponse } from '../Signum.Toolbar/ToolbarClient'
import { IconColor, ToolbarConfig } from '../Signum.Toolbar/ToolbarConfig'
import * as UserQueryClient from './UserQueryClient'
import { UserQueryEntity } from './Signum.UserQueries'
import { useAPI } from '@framework/Hooks';
import { SearchToolbarCount, ToolbarCount } from '../Signum.Toolbar/QueryToolbarConfig';
import { useFetchInState } from '@framework/Navigator'
import { ShowCount } from '../Signum.Toolbar/Signum.Toolbar'

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
          <SearchUserQueryCount userQuery={element.content!} color={element.iconColor} autoRefreshPeriod={element.autoRefreshPeriod} showCount={element.showCount} />
        </>
      );
    }

    return super.getIcon(element);
  }

  getDefaultIcon(): IconColor {
    return ({
      icon: "rectangle-list",
      iconColor: "dodgerblue",
    });
  }

  handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<UserQueryEntity>) {
    if (!res.openInPopup)
      super.handleNavigateClick(e, res);
    else {
      Navigator.API.fetch(res.content!)
        .then(uq => UserQueryClient.Converter.toFindOptions(uq, undefined)
          .then(fo => Finder.explore(fo, { searchControlProps: { extraOptions: { userQuery: res.content } } })));
    }
  }

  navigateTo(res: ToolbarResponse<UserQueryEntity>): Promise<string> {
    return Navigator.API.fetch(res.content!)
      .then(uq => {
        if (uq.refreshMode == "Manual")
          return Promise.resolve(UserQueryClient.userQueryUrl(res.content!));

        return UserQueryClient.Converter.toFindOptions(uq, undefined)
          .then(fo => Finder.findOptionsPath(fo, { userQuery: liteKey(res.content!) }));
      });
  }

  isCompatibleWithUrlPrio(res: ToolbarResponse<UserQueryEntity>, location: Location, query: any): number {
    return query["userQuery"] == liteKey(res.content!) ||
      location.pathname == UserQueryClient.userQueryUrl(res.content!) ? 2 : 0;
  }
}


interface CountUserQueryIconProps {
  userQuery: Lite<UserQueryEntity>;
  color?: string;
  autoRefreshPeriod?: number;
  showCount: ShowCount;
}


export function SearchUserQueryCount(p: CountUserQueryIconProps) {

  var userQuery = useFetchInState(p.userQuery)
  var findOptions = useAPI(signal => userQuery ? UserQueryClient.Converter.toFindOptions(userQuery, undefined) : Promise.resolve(undefined), [userQuery]);

  if (findOptions == null)
    return <ToolbarCount num={undefined} showCount={p.showCount} />;

  return <SearchToolbarCount findOptions={findOptions} autoRefreshPeriod={p.autoRefreshPeriod} color={p.color} showCount={p.showCount} />
}
