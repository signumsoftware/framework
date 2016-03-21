
import * as React from 'react'
import { Route } from 'react-router'

import { Dic } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { OmniboxMessage } from './Signum.Entities.Omnibox'


export function start(...params: OmniboxProvider<OmniboxResult>[]) {
    params.forEach(op => register(op));
}

export function register(prov: OmniboxProvider<OmniboxResult>) {
    if (providers[prov.getProviderName()])
        throw new Error(`Provider '${prov.getProviderName()}' already registered`);

    providers[prov.getProviderName()] = prov;
}

export var providers: { [resultTypeName: string]: OmniboxProvider<OmniboxResult> } = {}; 

export function renderItem(result: OmniboxResult): React.ReactChild {
    var items = getProvider(result.ResultTypeName).renderItem(result);
    return React.createElement("span", null, ...items);
}

export function navigateTo(result: OmniboxResult): Promise<string> {
    return getProvider(result.ResultTypeName).navigateTo(result);
}

export function toString(result: OmniboxResult): string {
    return getProvider(result.ResultTypeName).toString(result);
}

function getProvider(resultTypeName: string) {
    var prov = providers[resultTypeName];

    if (!prov)
        throw new Error(`No provider for '${resultTypeName}'`);

    return prov;
}

export function getResults(query: string): Promise<OmniboxResult[]> {
    return ajaxGet<OmniboxResult[]>({ url: "/api/omnibox?query=" + encodeURI(query) })
}

export abstract class OmniboxProvider<T extends OmniboxResult> {
    abstract getProviderName(): string;
    abstract renderItem(result: T): React.ReactNode[];
    abstract navigateTo(result: T): Promise<string>;
    abstract toString(result: T): string;
    abstract icon(): React.ReactNode;
    
    renderMatch(match: OmniboxMatch, array: React.ReactNode[]) {

        var regex = /#+/g;

        var last = 0;
        var m: RegExpExecArray;
        while (m = regex.exec(match.BoldMask)) {
            if (m.index > last)
                array.push(<span>{match.Text.substr(last, m.index - last) }</span>);

            array.push(<strong>{match.Text.substr(m.index, m[0].length) }</strong>)

            last = m.index + m[0].length;
        }

        if (last < match.Text.length)
            array.push(<span>{match.Text.substr(last) }</span>);
    }

    coloredSpan(text: string, colorName: string): React.ReactChild {
        return <span style={{ color: colorName, lineHeight: "1.6em" }}>{text}</span>;
    }

    coloredGlyphicon(icon: string, colorName: string): React.ReactChild {
        return <span className={"glyphicon " + icon} style={{ color: colorName, }}/>;
    }
}


export interface OmniboxResult {
    ResultTypeName?: string;
    Distance?: number;
}

export interface OmniboxMatch {
    Distance: number;
    Text: string;
    BoldMask: string;
}


