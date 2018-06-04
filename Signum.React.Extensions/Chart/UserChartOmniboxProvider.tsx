import * as React from 'react'
import { Lite, Entity, liteKey } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { getQueryKey, getQueryNiceName } from '../../../Framework/Signum.React/Scripts/Reflection'
import { OmniboxMessage } from '../Omnibox/Signum.Entities.Omnibox'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Omnibox/OmniboxClient'
import { QueryToken, FilterOperation, FindOptions, FilterOption } from '../../../Framework/Signum.React/Scripts/FindOptions'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import * as ChartClient from './ChartClient'
import * as UserChartClient from './UserChart/UserChartClient'
import { UserChartEntity } from './Signum.Entities.Chart'



export default class UserChartOmniboxProvider extends OmniboxProvider<UserChartOmniboxResult>
{
    getProviderName() {
        return "UserChartOmniboxResult";
    }

    icon() {
        return this.coloredIcon("fa fa-bar-chart", "darkviolet");
    }

    renderItem(result: UserChartOmniboxResult): React.ReactChild[] {

        const array: React.ReactChild[] = [];

        array.push(this.icon());

        this.renderMatch(result.toStrMatch, array);

        return array;
    }

    navigateTo(result: UserChartOmniboxResult) {

        if (result.userChart == undefined)
            return undefined;

        return Navigator.API.fetchAndForget(result.userChart)
            .then(a => UserChartClient.Converter.toChartRequest(a, undefined))
            .then(cr => ChartClient.Encoder.chartPath(cr, result.userChart));
    }

    toString(result: UserChartOmniboxResult) {
        return "\"{0}\"".formatWith(result.toStrMatch.text);
    }
}

interface UserChartOmniboxResult extends OmniboxResult {
    toStr: string;
    toStrMatch: OmniboxMatch;
    
    userChart: Lite<UserChartEntity>;
}
