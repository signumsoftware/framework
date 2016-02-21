
import * as React from 'react'
import { Route } from 'react-router'

import { Dic } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { OmniboxMessage } from './Signum.Entities.Omnibox'




export function getResults(query: string): Promise<OmniboxResult[]> {
    return ajaxGet<OmniboxResult[]>({ url: "/api/omnibox?query=" + encodeURI(query) })
}

//export var providers: { [providerName: string]: OmniboxRenderer<any> } = {}; 

export function renderItem(result: OmniboxResult): React.ReactNode {
    return null;
}



export interface OmniboxResult {
    providerName?: string;
    icon?: string;
    label?: React.ReactNode;
}


