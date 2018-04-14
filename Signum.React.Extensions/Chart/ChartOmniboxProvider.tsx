import * as React from 'react'
import { Lite, Entity } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getQueryKey, getQueryNiceName } from '../../../Framework/Signum.React/Scripts/Reflection'
import { OmniboxMessage } from '../Omnibox/Signum.Entities.Omnibox'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Omnibox/OmniboxClient'
import { QueryToken, FilterOperation, FindOptions, FilterOption } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import * as ChartClient from './ChartClient'
import { ChartRequest} from './Signum.Entities.Chart'



export default class ChartOmniboxProvider extends OmniboxProvider<ChartOmniboxResult>
{
    getProviderName() {
        return "ChartOmniboxResult";
    }

    icon() {
        return this.coloredIcon("fa fa-bar-chart", "violet");
    }

    renderItem(result: ChartOmniboxResult): React.ReactChild[] {

        const array: React.ReactChild[] = [];

        array.push(this.icon());

        this.renderMatch(result.KeywordMatch, array);
        array.push("\u0020");

        if (result.QueryNameMatch != undefined)
            this.renderMatch(result.QueryNameMatch, array);
        else
            array.push(this.coloredSpan(OmniboxMessage.Omnibox_Query.niceToString() + "...", "lightgray"));

        return array;
    }

    navigateTo(result: ChartOmniboxResult) {

        if (result.QueryNameMatch == undefined)
            return undefined;

        var cr = ChartRequest.New({
            queryKey: getQueryKey(result.QueryName),
            orderOptions: [],
            filterOptions: [],
        });

        const path = ChartClient.Encoder.chartPath(cr);

        return Promise.resolve(path);
    }

    toString(result: ChartOmniboxResult) {
        if (result.QueryNameMatch == undefined)
            return result.KeywordMatch.Text;

        return "{0} {1}".formatWith(result.KeywordMatch.Text, result.QueryNameMatch.Text);
    }
}

interface ChartOmniboxResult extends OmniboxResult {
    KeywordMatch: OmniboxMatch;

    QueryName: string;
    QueryNameMatch: OmniboxMatch;
}
