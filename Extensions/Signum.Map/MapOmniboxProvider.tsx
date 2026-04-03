import * as React from 'react'
import { OmniboxClient, OmniboxResult, OmniboxMatch } from '../Signum.Omnibox/OmniboxClient'
import { OmniboxProvider } from '../Signum.Omnibox/OmniboxProvider'

export default class MapOmniboxProvider extends OmniboxProvider<MapOmniboxResult>
{
  getProviderName() {
    return "MapOmniboxResult";
  }

  icon(): React.ReactElement {
    return this.coloredIcon("map", "green");
  }

  renderItem(result: MapOmniboxResult): React.ReactNode[] {

    const array: React.ReactNode[] = [];

    array.push(this.icon());

    this.renderMatch(result.keywordMatch, array);
    array.push("\u0020");

    if (result.typeMatch != undefined)
      this.renderMatch(result.typeMatch, array);

    return array;
  }

  navigateTo(result: MapOmniboxResult): Promise<string> | undefined {

    if (result.keywordMatch == undefined)
      return undefined;

    return Promise.resolve("/Map" + (result.typeName ? "/" + result.typeName : ""));
  }

  toString(result: MapOmniboxResult): string {
    if (result.typeMatch == undefined)
      return result.keywordMatch.text;

    return "{0} {1}".formatWith(result.keywordMatch.text, result.typeMatch.text);
  }
}

interface MapOmniboxResult extends OmniboxResult {
  keywordMatch: OmniboxMatch;

  typeName: string;
  typeMatch: OmniboxMatch;
}
