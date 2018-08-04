import * as React from 'react'
import { Lite, Entity, liteKey } from '@framework/Signum.Entities'
import { getQueryKey, getQueryNiceName } from '@framework/Reflection'
import { OmniboxMessage } from '../Omnibox/Signum.Entities.Omnibox'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Omnibox/OmniboxClient'
import { QueryToken, FilterOperation, FindOptions, FilterOption } from '@framework/FindOptions'
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

        this.renderMatch(result.ToStrMatch, array);

        return array;
    }

    navigateTo(result: UserQueryOmniboxResult) {

        if (result.UserQuery == undefined)
            return undefined;

        return Navigator.API.fetchAndForget(result.UserQuery)
            .then(uq => UserQueryClient.Converter.toFindOptions(uq, undefined))
            .then(fo => Finder.findOptionsPath(fo, { userQuery: liteKey(result.UserQuery) }));
    }

    toString(result: UserQueryOmniboxResult) {
        return "\"{0}\"".formatWith(result.ToStrMatch.Text);
    }
}

interface UserQueryOmniboxResult extends OmniboxResult {
    ToStr: string;
    ToStrMatch: OmniboxMatch;

    UserQuery: Lite<UserQueryEntity>;
}
