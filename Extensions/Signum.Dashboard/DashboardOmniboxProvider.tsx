import * as React from 'react'
import { Lite } from '@framework/Signum.Entities'
import { OmniboxClient, OmniboxResult, OmniboxMatch } from '../Signum.Omnibox/OmniboxClient'
import { OmniboxProvider } from '../Signum.Omnibox/OmniboxProvider'
import { DashboardClient } from './DashboardClient'
import { DashboardEntity } from './Signum.Dashboard'

export default class DashboardOmniboxProvider extends OmniboxProvider<DashboardOmniboxResult>
{
  getProviderName() {
    return "DashboardOmniboxResult";
  }

  icon(): React.ReactElement<any, string | React.JSXElementConstructor<any>> {
    return this.coloredIcon("tachometer-alt", "darkslateblue");
  }

  renderItem(result: DashboardOmniboxResult): React.ReactNode[] {

    const array: React.ReactNode[] = [];

    array.push(this.icon());

    this.renderMatch(result.toStrMatch, array);

    return array;
  }

  navigateTo(result: DashboardOmniboxResult): Promise<string> | undefined {

    if (result.dashboard == undefined)
      return undefined;

    return Promise.resolve(DashboardClient.dashboardUrl(result.dashboard));
  }

  toString(result: DashboardOmniboxResult): string {
    return "\"{0}\"".formatWith(result.toStrMatch.text);
  }
}

interface DashboardOmniboxResult extends OmniboxResult {
  toStr: string;
  toStrMatch: OmniboxMatch;

  dashboard: Lite<DashboardEntity>;
}
