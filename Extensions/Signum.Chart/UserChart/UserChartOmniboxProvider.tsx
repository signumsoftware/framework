import * as React from 'react'
import { Lite } from '@framework/Signum.Entities'
import { OmniboxClient, OmniboxResult, OmniboxMatch } from '../../Signum.Omnibox/OmniboxClient'
import { OmniboxProvider } from '../../Signum.Omnibox/OmniboxProvider'
import { Navigator } from '@framework/Navigator'
import { ChartClient } from '../ChartClient'
import { UserChartClient } from '../UserChart/UserChartClient'
import { UserChartEntity } from './Signum.Chart.UserChart'

export default class UserChartOmniboxProvider extends OmniboxProvider<UserChartOmniboxResult>
{
  getProviderName() {
    return "UserChartOmniboxResult";
  }

  icon(): React.ReactElement {
    return this.coloredIcon("chart-bar", "darkviolet");
  }

  renderItem(result: UserChartOmniboxResult): React.ReactNode[] {

    const array: React.ReactNode[] = [];

    array.push(this.icon());

    this.renderMatch(result.toStrMatch, array);

    return array;
  }

  navigateTo(result: UserChartOmniboxResult): Promise<string> | undefined {

    if (result.userChart == undefined)
      return undefined;

    return Navigator.API.fetch(result.userChart)
      .then(a => UserChartClient.Converter.toChartRequest(a, undefined))
      .then(cr => ChartClient.Encoder.chartPathPromise(cr, result.userChart));
  }

  toString(result: UserChartOmniboxResult): string {
    return "\"{0}\"".formatWith(result.toStrMatch.text);
  }
}

interface UserChartOmniboxResult extends OmniboxResult {
  toStr: string;
  toStrMatch: OmniboxMatch;

  userChart: Lite<UserChartEntity>;
}
