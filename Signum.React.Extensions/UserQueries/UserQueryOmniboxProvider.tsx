import * as React from 'react'
import { Lite, Entity, liteKey } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getQueryKey, getQueryNiceName } from '../../../Framework/Signum.React/Scripts/Reflection'
import { OmniboxMessage } from '../Omnibox/Signum.Entities.Omnibox'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Omnibox/OmniboxClient'
import { QueryToken, FilterOperation, FindOptions, FilterOption } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import * as UserQueryClient from './UserQueryClient'
import { UserQueryEntity } from './Signum.Entities.UserQueries'



export default class UserQueryOmniboxProvider extends OmniboxProvider<UserQueryOmniboxResult>
{
    getProviderName() {
        return "UserQueryOmniboxResult";
    }

    icon() {
        return this.coloredIcon("fa fa-list-alt", "dodgerblue");
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

        return Navigator.API.fetchAndForget(result.userQuery)
            .then(uq => UserQueryClient.Converter.toFindOptions(uq, undefined))
            .then(fo => Finder.findOptionsPath(fo, { userQuery: liteKey(result.userQuery) }));
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
