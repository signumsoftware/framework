import * as React from 'react'
import { OmniboxResult, OmniboxMatch, OmniboxProvider } from '../Omnibox/OmniboxClient'
import { Urls } from './HelpClient';

export default class HelpOmniboxProvider extends OmniboxProvider<HelpModuleOmniboxResult>
{
  getProviderName() {
    return "HelpModuleOmniboxResult";
  }

  icon() {
    return this.coloredIcon("book", "darkviolet");
  }

  renderItem(result: HelpModuleOmniboxResult): React.ReactChild[] {

    const array: React.ReactChild[] = [];

    array.push(this.icon());

    this.renderMatch(result.keywordMatch, array);
    array.push("\u0020");

    if (result.secondMatch != undefined)
      this.renderMatch(result.secondMatch, array);

    if (result.searchString)
      array.push(`'${result.searchString}'`);

    return array;
  }

  navigateTo(result: HelpModuleOmniboxResult) {

    if (result.typeName != null)
      return Promise.resolve(Urls.typeUrl(result.typeName));

    if (result.searchString != null)
      return Promise.resolve(Urls.searchUrl(result.searchString));

    if (result.keywordMatch == undefined)
      return undefined;

    return Promise.resolve(Urls.indexUrl());
  }

  toString(result: HelpModuleOmniboxResult) {
    if (result.secondMatch)
      return "{0} {1}".formatWith(result.keywordMatch.text, result.secondMatch.text);

    if (result.searchString)
      return "{0} \"{1}\"".formatWith(result.keywordMatch.text, result.searchString);

    return "{0}".formatWith(result.keywordMatch.text);
  }
}

interface HelpModuleOmniboxResult extends OmniboxResult {
  keywordMatch: OmniboxMatch;

  typeName?: string;
  secondMatch?: OmniboxMatch;

  searchString?: string;
}
