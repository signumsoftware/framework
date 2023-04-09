import * as React from 'react'
import { Lite } from '@framework/Signum.Entities'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Signum.Omnibox/OmniboxClient'
import * as DashboardClient from './DashboardClient'
import { DashboardEntity } from './Signum.Dashboard'

export default class DashboardOmniboxProvider extends OmniboxProvider<DashboardOmniboxResult>
{
  getProviderName() {
    return "DashboardOmniboxResult";
  }

  icon() {
    return this.coloredIcon("tachometer-alt", "darkslateblue");
  }

  renderItem(result: DashboardOmniboxResult): React.ReactChild[] {

    const array: React.ReactChild[] = [];

    array.push(this.icon());

    this.renderMatch(result.toStrMatch, array);

    return array;
  }

  navigateTo(result: DashboardOmniboxResult) {

    if (result.dashboard == undefined)
      return undefined;

    return Promise.resolve(DashboardClient.dashboardUrl(result.dashboard));
  }

  toString(result: DashboardOmniboxResult) {
    return "\"{0}\"".formatWith(result.toStrMatch.text);
  }
}

interface DashboardOmniboxResult extends OmniboxResult {
  toStr: string;
  toStrMatch: OmniboxMatch;

  dashboard: Lite<DashboardEntity>;
}
