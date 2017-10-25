import * as React from 'react'
import { Lite, Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getQueryKey, getQueryNiceName } from '../../../Framework/Signum.React/Scripts/Reflection'
import { OmniboxMessage } from '../Omnibox/Signum.Entities.Omnibox'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Omnibox/OmniboxClient'
import { QueryToken, FilterOperation, FindOptions, FilterOption } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import * as TreeClient from './TreeClient'


export default class TreeOmniboxProvider extends OmniboxProvider<TreeOmniboxResult>
{
    getProviderName() {
        return "TreeOmniboxResult";
    }

    icon() {
        return this.coloredIcon("fa fa-sitemap", "gold");
    }

    renderItem(result: TreeOmniboxResult): React.ReactChild[] {

        var array: React.ReactChild[] = [];

        array.push(this.icon());

        this.renderMatch(result.TypeMatch, array);
     
        return array;
    }

    navigateTo(result: TreeOmniboxResult) {
        return Promise.resolve("~/tree/" + result.Type);
    }

    toString(result: TreeOmniboxResult) {
        return result.TypeMatch.Text;
    }
}

interface TreeOmniboxResult extends OmniboxResult {
    Type: string;
    TypeMatch: OmniboxMatch;
}
