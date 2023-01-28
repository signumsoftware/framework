import * as React from 'react'
import { Lite, liteKey } from '@framework/Signum.Entities'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Omnibox/OmniboxClient'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import * as UserQueryClient from './UserQueryClient'
import { UserQueryEntity } from './Signum.Entities.UserQueries'

export default class UserQueryOmniboxProvider extends OmniboxProvider<UserQueryOmniboxResult>
{
  getProviderName() {
    return "UserQueryOmniboxResult";
  }

  icon() {
    return this.coloredIcon(["far", "list-alt"], "dodgerblue");
  }

  renderItem(result: UserQueryOmniboxResult): React.ReactChild[] {

    const array: React.ReactChild[] = [];

    array.push(this.icon());

    this.renderMatch(result.toStrMatch, array);

    return array;
  }

  navigateTo(result: UserQueryOmniboxResult) {

    if (result.userQuery == undefined)
      return undefined;

    return Navigator.API.fetch(result.userQuery)
      .then(uq => UserQueryClient.Converter.toFindOptions(uq, undefined)
        .then(fo => Finder.findOptionsPath(fo, { userQuery: liteKey(result.userQuery), customDrilldowns: uq.customDrilldowns })));
  }

  toString(result: UserQueryOmniboxResult) {
    return "\"{0}\"".formatWith(result.toStrMatch.text);
  }
}

interface UserQueryOmniboxResult extends OmniboxResult {
  toStr: string;
  toStrMatch: OmniboxMatch;

  userQuery: Lite<UserQueryEntity>;
}
