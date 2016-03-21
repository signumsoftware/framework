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
        return this.coloredGlyphicon("glyphicon-list-alt", "dodgerblue");
    }

    renderItem(result: UserQueryOmniboxResult): React.ReactChild[] {

        var array: React.ReactChild[] = [];

        array.push(this.icon());

        this.renderMatch(result.ToStrMatch, array);

        return array;
    }

    navigateTo(result: UserQueryOmniboxResult) {

        if (result.UserQuery == null)
            return null;

        return Navigator.API.fetchAndForget(result.UserQuery)
            .then(uq => UserQueryClient.Converter.toFindOptions(uq, null))
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
