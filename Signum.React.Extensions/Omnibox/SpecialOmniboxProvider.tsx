import * as React from 'react'
import { Lite, Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { OmniboxMessage } from './Signum.Entities.Omnibox'
import { OmniboxResult, OmniboxMatch, OmniboxProvider, specialActions } from './OmniboxClient'
import { QueryToken, FilterOperation, FindOptions, FilterOption } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'



export default class SpecialOmniboxProvider extends OmniboxProvider<SpecialOmniboxResult>
{
    getProviderName() {
        return "SpecialOmniboxResult";
    }

    icon() {
        return this.coloredIcon("fa fa-cog", "limegreen");
    }

    renderItem(result: SpecialOmniboxResult): React.ReactChild[] {

        const array: React.ReactChild[] = [];

        array.push(this.icon());

        array.push("!");

        this.renderMatch(result.Match, array)
     
        return array;
    }

    navigateTo(result: SpecialOmniboxResult) {
        return specialActions[result.Key].onClick();
    }

    toString(result: SpecialOmniboxResult) {
        return "!" + result.Key;
    }
}

interface SpecialOmniboxResult extends OmniboxResult {
    Match: OmniboxMatch;
    Key: string;
}
