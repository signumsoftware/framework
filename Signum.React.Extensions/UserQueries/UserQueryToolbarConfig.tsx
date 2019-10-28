import { Location } from 'history'
import { OutputParams } from 'query-string'
import * as React from 'react'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { FindOptions, ValueSearchControl } from '@framework/Search'
import { Lite, liteKey } from '@framework/Signum.Entities'
import { ToolbarConfig, ToolbarResponse } from '../Toolbar/ToolbarClient'
import * as UserQueryClient from './UserQueryClient'
import { UserQueryEntity } from './Signum.Entities.UserQueries'
import { parseIcon } from '../Dashboard/Admin/Dashboard';
import { coalesceIcon } from '@framework/Operations/ContextualOperations';
import { useAPI, useFetchAndForget } from '@framework/Hooks';
import { CountIcon } from '../Toolbar/QueryToolbarConfig';

export default class UserQueryToolbarConfig extends ToolbarConfig<UserQueryEntity> {
  constructor() {
    var type = UserQueryEntity;
    super(type);
  }

  countIcon?: CountIcon | null;
  getIcon(element: ToolbarResponse<UserQueryEntity>) {

    if (element.iconName == "count")
      return <CountUserQueryIcon innerRef={ci => this.countIcon = ci} userQuery={element.content!} color={element.iconColor || "red"} autoRefreshPeriod={element.autoRefreshPeriod} />;

    return ToolbarConfig.coloredIcon(coalesceIcon(parseIcon(element.iconName), ["far", "list-alt"]), element.iconColor || "dodgerblue");
  }

  handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<any>) {
    if (!res.openInPopup)
      super.handleNavigateClick(e, res);
    else {
      Navigator.API.fetchAndForget(res.content!)
        .then(uq => UserQueryClient.Converter.toFindOptions(uq, undefined))
        .then(fo => Finder.explore(fo))
        .then(() => this.countIcon && this.countIcon.refreshValue())
        .done();
    }
  }

  navigateTo(res: ToolbarResponse<UserQueryEntity>): Promise<string> {
    return Navigator.API.fetchAndForget(res.content!)
      .then(uq => UserQueryClient.Converter.toFindOptions(uq, undefined))
      .then(fo => Finder.findOptionsPath(fo, { userQuery: liteKey(res.content!) }));
  }

  isCompatibleWithUrl(res: ToolbarResponse<UserQueryEntity>, location: Location, query: OutputParams): boolean {
    return query["userQuery"] == liteKey(res.content!);
  }
}


interface CountUserQueryIconProps {
  userQuery: Lite<UserQueryEntity>;
  color?: string;
  autoRefreshPeriod?: number;
  innerRef: (e: CountIcon | null) => void;
}


export function CountUserQueryIcon(p: CountUserQueryIconProps) {

  var userQuery = useFetchAndForget(p.userQuery)
  var findOptions = useAPI(undefined, [userQuery], signal => userQuery ? UserQueryClient.Converter.toFindOptions(userQuery, undefined) : Promise.resolve(undefined));

  if (findOptions == null)
    return <span className="icon" style={{ color: p.color }}>…</span>;

  return <CountIcon ref={p.innerRef} findOptions={findOptions} autoRefreshPeriod={p.autoRefreshPeriod} color={p.color} />
}
