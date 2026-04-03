import * as React from 'react'
import { OmniboxClient, OmniboxResult, OmniboxMatch } from '../Signum.Omnibox/OmniboxClient'
import { OmniboxProvider } from '../Signum.Omnibox/OmniboxProvider'
import { HelpClient } from './HelpClient';

export default class HelpOmniboxProvider extends OmniboxProvider<HelpModuleOmniboxResult>
{
  getProviderName() {
    return "HelpModuleOmniboxResult";
  }

   icon(): React.ReactElement<any, string | React.JSXElementConstructor<any>> {
    return this.coloredIcon("book", "darkviolet");
  }

  renderItem(result: HelpModuleOmniboxResult): React.ReactNode[] {

    const array: React.ReactNode[] = [];

    array.push(this.icon());

    this.renderMatch(result.keywordMatch, array);
    array.push("\u0020");

    if (result.secondMatch != undefined)
      this.renderMatch(result.secondMatch, array);

    if (result.searchString)
      array.push(`'${result.searchString}'`);

    return array;
  }

  navigateTo(result: HelpModuleOmniboxResult): Promise<string> | undefined {

    if (result.typeName != null)
      return Promise.resolve(HelpClient.Urls.typeUrl(result.typeName));

    if (result.searchString != null)
      return Promise.resolve(HelpClient.Urls.searchUrl(result.searchString));

    if (result.keywordMatch == undefined)
      return undefined;

    return Promise.resolve(HelpClient.Urls.indexUrl());
  }

  toString(result: HelpModuleOmniboxResult): string {
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
