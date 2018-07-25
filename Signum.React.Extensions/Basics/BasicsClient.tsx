import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Constructor from '@framework/Constructor'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { TimeSpanEmbedded, DateSpanEmbedded } from './Signum.Entities.Basics'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '@framework/QuickLinks'

export function start(options: { routes: JSX.Element[] }) {
    Navigator.addSettings(new EntitySettings(TimeSpanEmbedded, e => import('./Templates/TimeSpan')));
    Navigator.addSettings(new EntitySettings(DateSpanEmbedded, e => import('./Templates/DateSpan')));
    Constructor.registerConstructor(TimeSpanEmbedded, () => TimeSpanEmbedded.New({ days: 0, hours: 0, minutes: 0, seconds: 0 }));
    Constructor.registerConstructor(DateSpanEmbedded, () => DateSpanEmbedded.New({ years: 0, months: 0, days: 0 }));
}
