import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { EntityOperationSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { NoteEntity, NoteTypeEntity, NoteOperation } from './Signum.Entities.Notes'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'

export function start(options: { routes: JSX.Element[], couldHaveNotes?: (typeName: string) => boolean }) {
    Navigator.addSettings(new EntitySettings(NoteEntity, e => import('./Templates/Note')));
    Navigator.addSettings(new EntitySettings(NoteTypeEntity, e => import('./Templates/NoteType')));

    const couldHaveNotes = options.couldHaveNotes || (typeName => true);

    Operations.addSettings(new EntityOperationSettings(NoteOperation.CreateNoteFromEntity, {
        isVisible: eoc => couldHaveNotes!(eoc.entity.Type),
        contextual: { icon: "fa fa-sticky-note", iconColor: "#0e4f8c", color: "info", isVisible: ctx => couldHaveNotes(ctx.context.lites[0].EntityType), }
    }));

    QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkExplore({
        queryName: NoteEntity,
        parentColumn: "Target",
        parentValue: ctx.lite
    }, { isVisible: couldHaveNotes(ctx.lite.EntityType), icon: "fa fa-sticky-note", iconColor: "#337ab7" }));
}