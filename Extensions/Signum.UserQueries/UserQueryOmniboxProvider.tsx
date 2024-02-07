import * as React from 'react'
import { Lite, liteKey } from '@framework/Signum.Entities'
import { OmniboxResult, OmniboxMatch } from '../Signum.Omnibox/OmniboxClient'
import { OmniboxProvider } from '../Signum.Omnibox/OmniboxProvider'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import * as UserQueryClient from './UserQueryClient'
import { UserQueryEntity } from './Signum.UserQueries'

export default class UserQueryOmniboxProvider extends OmniboxProvider<UserQueryOmniboxResult>
{
  getProviderName() {
    return "UserQueryOmniboxResult";
  }

  icon() {
    return this.coloredIcon( "list-alt", "dodgerblue");
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
        .then(fo => Finder.findOptionsPath(fo, { userQuery: liteKey(result.userQuery) })));
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
