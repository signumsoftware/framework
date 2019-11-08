import * as React from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { Dic } from '@framework/Globals';
import { ajaxPost, ajaxGet } from '@framework/Services';
import * as Navigator from '@framework/Navigator'

export function start(...params: OmniboxProvider<OmniboxResult>[]) {
  params.forEach(op => registerProvider(op));

  Navigator.clearSettingsActions.push(clearProviders);
  Navigator.clearSettingsActions.push(clearSpecialActions);
}

export const providers: { [resultTypeName: string]: OmniboxProvider<OmniboxResult> } = {};

export function clearProviders() {
  Dic.clear(providers);
}

export function registerProvider(prov: OmniboxProvider<OmniboxResult>) {
  if (providers[prov.getProviderName()])
    throw new Error(`Provider '${prov.getProviderName()}' already registered`);

  providers[prov.getProviderName()] = prov;
}



export const specialActions: { [resultTypeName: string]: SpecialOmniboxAction } = {};

export function clearSpecialActions() {
  Dic.clear(specialActions);
}

export function registerSpecialAction(action: SpecialOmniboxAction) {
  if (specialActions[action.key])
    throw new Error(`Action '${action.key}' already registered`);

  specialActions[action.key] = action;
}

export interface SpecialOmniboxAction {
  key: string;
  allowed: () => boolean;
  onClick: () => Promise<string | undefined>;
}


export interface HelpOmniboxResult extends OmniboxResult {
  text: string;
  referencedTypeName: string;
}


export function renderItem(result: OmniboxResult): React.ReactChild {
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

  result.push(<span style={{ fontStyle: "italic" }} dangerouslySetInnerHTML={{ __html: str }} />);

  return result;
}

export function navigateTo(result: OmniboxResult) {

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
    return ajaxPost({ url: "~/api/omnibox", signal }, {
      query: query ?? "",
      specialActions: Dic.getKeys(specialActions).filter(a => specialActions[a].allowed == null || specialActions[a].allowed())
    })
  }
}

export abstract class OmniboxProvider<T extends OmniboxResult> {
  abstract getProviderName(): string;
  abstract renderItem(result: T): React.ReactNode[];
  abstract navigateTo(result: T): Promise<string | undefined> | undefined;
  abstract toString(result: T): string;
  abstract icon(): React.ReactNode;

  renderMatch(match: OmniboxMatch, array: React.ReactNode[]) {

    const regex = /#+/g;

    let last = 0;
    let m: RegExpExecArray;
    while (m = regex.exec(match.boldMask)!) {
      if (m.index > last)
        array.push(<span>{match.text.substr(last, m.index - last)}</span>);

      array.push(<strong>{match.text.substr(m.index, m[0].length)}</strong>)

      last = m.index + m[0].length;
    }

    if (last < match.text.length)
      array.push(<span>{match.text.substr(last)}</span>);
  }

  coloredSpan(text: string, colorName: string): React.ReactChild {
    return <span style={{ color: colorName, lineHeight: "1.6em" }}>{text}</span>;
  }

  coloredIcon(icon: IconProp, color: string): React.ReactChild {
    return <FontAwesomeIcon icon={icon} color={color} className="icon" />;
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


