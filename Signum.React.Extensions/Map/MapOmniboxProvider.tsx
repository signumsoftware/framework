import * as React from 'react'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Omnibox/OmniboxClient'

export default class MapOmniboxProvider extends OmniboxProvider<MapOmniboxResult>
{
  getProviderName() {
    return "MapOmniboxResult";
  }

  icon() {
    return this.coloredIcon("map", "green");
  }

  renderItem(result: MapOmniboxResult): React.ReactChild[] {

    const array: React.ReactChild[] = [];

    array.push(this.icon());

    this.renderMatch(result.keywordMatch, array);
    array.push("\u0020");

    if (result.typeMatch != undefined)
      this.renderMatch(result.typeMatch, array);

    return array;
  }

  navigateTo(result: MapOmniboxResult) {

    if (result.keywordMatch == undefined)
      return undefined;

    return Promise.resolve("~/Map" + (result.typeName ? "/" + result.typeName : ""));
  }

  toString(result: MapOmniboxResult) {
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
