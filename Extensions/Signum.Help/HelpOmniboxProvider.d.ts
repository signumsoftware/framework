import * as React from 'react';
import { OmniboxResult, OmniboxMatch } from '../Signum.Omnibox/OmniboxClient';
import { OmniboxProvider } from '../Signum.Omnibox/OmniboxProvider';
export default class HelpOmniboxProvider extends OmniboxProvider<HelpModuleOmniboxResult> {
    getProviderName(): string;
    icon(): React.ReactElement<any, string | React.JSXElementConstructor<any>>;
    renderItem(result: HelpModuleOmniboxResult): React.ReactChild[];
    navigateTo(result: HelpModuleOmniboxResult): Promise<string> | undefined;
    toString(result: HelpModuleOmniboxResult): string;
}
interface HelpModuleOmniboxResult extends OmniboxResult {
    keywordMatch: OmniboxMatch;
    typeName?: string;
    secondMatch?: OmniboxMatch;
    searchString?: string;
}
export {};
