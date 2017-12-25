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

        this.renderMatch(result.ToStrMatch, array);

        return array;
    }

    navigateTo(result: UserChartOmniboxResult) {

        if (result.UserChart == undefined)
            return undefined;

        return Navigator.API.fetchAndForget(result.UserChart)
            .then(a => UserChartClient.Converter.toChartRequest(a, undefined))
            .then(cr => ChartClient.Encoder.chartPath(cr, result.UserChart));
    }

    toString(result: UserChartOmniboxResult) {
        return "\"{0}\"".formatWith(result.ToStrMatch.Text);
    }
}

interface UserChartOmniboxResult extends OmniboxResult {
    ToStr: string;
    ToStrMatch: OmniboxMatch;
    
    UserChart: Lite<UserChartEntity>;
}
