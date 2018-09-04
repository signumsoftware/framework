import * as React from 'react'
import { Route } from 'react-router'
import { Dic, classes } from '@framework/Globals';
import { ajaxPost, ajaxPostRaw, ajaxGet, saveFile } from '@framework/Services';
import { EntitySettings, ViewPromise } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { Lite, Entity, EntityPack, ExecuteSymbol, DeleteSymbol, ConstructSymbol_From, registerToString, JavascriptMessage, toLite } from '@framework/Signum.Entities'
import { EntityOperationSettings } from '@framework/Operations'
import { PseudoType, QueryKey, GraphExplorer, OperationType, Type, getTypeName  } from '@framework/Reflection'
import * as Operations from '@framework/Operations'
import { NoteEntity, NoteTypeEntity, NoteOperation } from './Signum.Entities.Notes'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import * as AuthClient from '../Authorization/AuthClient'
import * as QuickLinks from '@framework/QuickLinks'

export function start(options: { routes: JSX.Element[], couldHaveNotes?: (typeName: string) => boolean }) {
    Navigator.addSettings(new EntitySettings(NoteEntity, e => import('./Templates/Note')));
    Navigator.addSettings(new EntitySettings(NoteTypeEntity, e => import('./Templates/NoteType')));

    const couldHaveNotes = options.couldHaveNotes || (typeName => true);

    Operations.addSettings(new EntityOperationSettings(NoteOperation.CreateNoteFromEntity, {
        isVisible: eoc => couldHaveNotes!(eoc.entity.Type),
        contextual: { icon: "sticky-note", iconColor: "#0e4f8c", color: "info", isVisible: ctx => couldHaveNotes(ctx.context.lites[0].EntityType), }
    }));

    QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkExplore({
        queryName: NoteEntity,
        parentToken: "Target",
        parentValue: ctx.lite
    }, { isVisible: couldHaveNotes(ctx.lite.EntityType), icon: "sticky-note", iconColor: "#337ab7" }));
}