import * as React from 'react'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Omnibox/OmniboxClient'


export default class TreeOmniboxProvider extends OmniboxProvider<TreeOmniboxResult>
{
  getProviderName() {
    return "TreeOmniboxResult";
  }

  icon() {
    return this.coloredIcon("sitemap", "gold");
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
    return result.TypeMatch.text;
  }
}

interface TreeOmniboxResult extends OmniboxResult {
  Type: string;
  TypeMatch: OmniboxMatch;
}
