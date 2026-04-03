import * as React from 'react'
import { OmniboxClient, OmniboxResult, OmniboxMatch } from './OmniboxClient'
import { OmniboxProvider } from "./OmniboxProvider";
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'

export default class SpecialOmniboxProvider extends OmniboxProvider<SpecialOmniboxResult>
{
  getProviderName() {
    return "SpecialOmniboxResult";
  }

  icon(): React.ReactElement<any, string | React.JSXElementConstructor<any>> {
    return this.coloredIcon("cog", "limegreen");
  }

  renderItem(result: SpecialOmniboxResult): React.ReactNode[] {

    const array: React.ReactNode[] = [];

    array.push(this.icon());

    array.push("!");

    this.renderMatch(result.match, array)

    return array;
  }

  navigateTo(result: SpecialOmniboxResult): Promise<string | undefined> {
    return OmniboxSpecialAction.specialActions[result.key].onClick();
  }

  toString(result: SpecialOmniboxResult): string {
    return "!" + result.key;
  }
}

interface SpecialOmniboxResult extends OmniboxResult {
  match: OmniboxMatch;
  key: string;
}
