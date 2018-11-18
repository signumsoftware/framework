import * as React from 'react'
import { Lite, Entity } from '@framework/Signum.Entities'
import { getQueryKey, getQueryNiceName } from '@framework/Reflection'
import { OmniboxMessage } from '../Omnibox/Signum.Entities.Omnibox'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Omnibox/OmniboxClient'
import { QueryToken, FilterOperation, FindOptions, FilterOption } from '@framework/FindOptions'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import * as ChartClient from './ChartClient'
import { ChartRequestModel} from './Signum.Entities.Chart'



export default class ChartOmniboxProvider extends OmniboxProvider<ChartOmniboxResult>
{
    getProviderName() {
        return "ChartOmniboxResult";
    }

    icon() {
        return this.coloredIcon("chart-bar", "violet");
    }

    renderItem(result: ChartOmniboxResult): React.ReactChild[] {

        const array: React.ReactChild[] = [];

        array.push(this.icon());

        this.renderMatch(result.keywordMatch, array);
        array.push("\u0020");

        if (result.queryNameMatch != undefined)
            this.renderMatch(result.queryNameMatch, array);
        else
            array.push(this.coloredSpan(OmniboxMessage.Omnibox_Query.niceToString() + "...", "lightgray"));

        return array;
    }

    navigateTo(result: ChartOmniboxResult) {

        if (result.queryNameMatch == undefined)
            return undefined;

        var cr = ChartRequestModel.New({
            queryKey: getQueryKey(result.queryName),
            orderOptions: [],
            filterOptions: [],
        });

        const path = ChartClient.Encoder.chartPath(cr);

        return Promise.resolve(path);
    }

    toString(result: ChartOmniboxResult) {
        if (result.queryNameMatch == undefined)
            return result.keywordMatch.text;

        return "{0} {1}".formatWith(result.keywordMatch.text, result.queryNameMatch.text);
    }
}

interface ChartOmniboxResult extends OmniboxResult {
    keywordMatch: OmniboxMatch;

    queryName: string;
    queryNameMatch: OmniboxMatch;
}
