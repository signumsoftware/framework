import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { TimeSpanEmbedded, DateSpanEmbedded } from './Signum.Entities.Basics'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'

export function start(options: { routes: JSX.Element[] }) {
    Navigator.addSettings(new EntitySettings(TimeSpanEmbedded, e => import('./Templates/TimeSpan')));
    Navigator.addSettings(new EntitySettings(DateSpanEmbedded, e => import('./Templates/DateSpan')));
    Constructor.registerConstructor(TimeSpanEmbedded, () => TimeSpanEmbedded.New({ days: 0, hours: 0, minutes: 0, seconds: 0 }));
    Constructor.registerConstructor(DateSpanEmbedded, () => DateSpanEmbedded.New({ years: 0, months: 0, days: 0 }));
}
