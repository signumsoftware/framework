import * as React from 'react'
import { Dic } from '@framework/Globals';
import { ajaxPost, ajaxGet } from '@framework/Services';
import * as AppContext from '@framework/AppContext'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import DynamicQueryOmniboxProvider from './DynamicQueryOmniboxProvider';
import EntityOmniboxProvider from './EntityOmniboxProvider';
import { OmniboxProvider } from './OmniboxProvider';
import SpecialOmniboxProvider from './SpecialOmniboxProvider';
import { ChangeLogClient } from '@framework/Basics/ChangeLogClient';

export namespace OmniboxClient {
  
  export function start(): void {
  
    ChangeLogClient.registerChangeLogModule("Signum.Omnibox", () => import("./Changelog"));
  
    registerProvider(new EntityOmniboxProvider());
    registerProvider(new DynamicQueryOmniboxProvider());
    registerProvider(new SpecialOmniboxProvider());
  
    AppContext.clearSettingsActions.push(clearProviders);
  }
  
  export const providers: { [resultTypeName: string]: OmniboxProvider<OmniboxResult> } = {};
  
  export function clearProviders(): void {
    Dic.clear(providers);
  }
  
  export function registerProvider(prov: OmniboxProvider<OmniboxResult>): void {
    if (providers[prov.getProviderName()])
      throw new Error(`Provider '${prov.getProviderName()}' already registered`);
  
    providers[prov.getProviderName()] = prov;
  }
  
  
  
  
  
  export interface HelpOmniboxResult extends OmniboxResult {
    text: string;
    referencedTypeName: string;
    isMainTitle?: boolean;
  }
  
  
  export function renderItem(result: OmniboxResult): React.ReactNode {
    const items = result.resultTypeName == "HelpOmniboxResult" ?
      renderHelpItem(result as HelpOmniboxResult) :
      getProvider(result.resultTypeName).renderItem(result);
    return React.createElement("span", undefined, ...items);
  }
  
  function renderHelpItem(help: HelpOmniboxResult): React.ReactNode[] {
  
    const result: React.ReactNode[] = [];
  
    if (help.referencedTypeName)
      result.push(getProvider(help.referencedTypeName).icon());
  
    const str = help.text
      .replaceAll("(", "<strong>")
      .replaceAll(")", "</strong>");
  
    result.push(<span style={help.isMainTitle ? { fontWeight: "bold" } : { fontStyle: "italic" }} dangerouslySetInnerHTML = {{ __html: str }} />);
  
    return result;
  }
  
  export function navigateTo(result: OmniboxResult): Promise<string | undefined> | undefined {
  
    if (result.resultTypeName == "HelpOmniboxResult")
      return undefined;
  
    return getProvider(result.resultTypeName).navigateTo(result);
  }
  
  export function toString(result: OmniboxResult): string {
    return getProvider(result.resultTypeName).toString(result);
  }
  
  function getProvider(resultTypeName: string) {
    const prov = providers[resultTypeName];
  
    if (!prov)
      throw new Error(`No provider for '${resultTypeName}'`);
  
    return prov;
  }
  
  export namespace API {
  
    export function getResults(query: string, signal: AbortSignal): Promise<OmniboxResult[]> {
      var specialActions = OmniboxSpecialAction.specialActions; 
      return ajaxPost({ url: "/api/omnibox", signal }, {
        query: query ?? "",
        specialActions: Dic.getKeys(specialActions)
          .filter(a => specialActions[a].allowed == null || specialActions[a].allowed()) 
      })
    }
  }
}

export interface OmniboxResult {
  resultTypeName: string;
  distance: number;
}

export interface OmniboxMatch {
  distance: number;
  text: string;
  boldMask: string;
}

