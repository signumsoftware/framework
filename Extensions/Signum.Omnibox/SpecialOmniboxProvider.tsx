import * as React from 'react'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from './OmniboxClient'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'

export default class SpecialOmniboxProvider extends OmniboxProvider<SpecialOmniboxResult>
{
  getProviderName() {
    return "SpecialOmniboxResult";
  }

  icon() {
    return this.coloredIcon("cog", "limegreen");
  }

  renderItem(result: SpecialOmniboxResult): React.ReactChild[] {

    const array: React.ReactChild[] = [];

    array.push(this.icon());

    array.push("!");

    this.renderMatch(result.match, array)

    return array;
  }

  navigateTo(result: SpecialOmniboxResult) {
    return OmniboxSpecialAction.specialActions[result.key].onClick();
  }

  toString(result: SpecialOmniboxResult) {
    return "!" + result.key;
  }
}

interface SpecialOmniboxResult extends OmniboxResult {
  match: OmniboxMatch;
  key: string;
}
