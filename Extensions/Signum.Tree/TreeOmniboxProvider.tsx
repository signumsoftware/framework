import * as React from 'react'
import { OmniboxClient, OmniboxResult, OmniboxMatch } from '../Signum.Omnibox/OmniboxClient'
import { OmniboxProvider } from '../Signum.Omnibox/OmniboxProvider'


export default class TreeOmniboxProvider extends OmniboxProvider<TreeOmniboxResult>
{
  getProviderName() {
    return "TreeOmniboxResult";
  }

  icon(): React.ReactElement {
    return this.coloredIcon("sitemap", "gold");
  }

  renderItem(result: TreeOmniboxResult): React.ReactElement[] {

    var array: React.ReactElement[] = [];

    array.push(this.icon());

    this.renderMatch(result.typeMatch, array);

    return array;
  }

  navigateTo(result: TreeOmniboxResult) : Promise<string> {
    return Promise.resolve("/tree/" + result.type);
  }

  toString(result: TreeOmniboxResult) : string {
    return result.typeMatch.text;
  }
}

interface TreeOmniboxResult extends OmniboxResult {
  type: string;
  typeMatch: OmniboxMatch;
}
