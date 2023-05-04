import { RouteObject } from 'react-router'
import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { NoteEntity, NoteOperation } from './Signum.Notes'
import * as QuickLinks from '@framework/QuickLinks'

export function start(options: { routes: RouteObject[], couldHaveNotes?: (typeName: string) => boolean }) {
  Navigator.addSettings(new EntitySettings(NoteEntity, e => import('./Templates/Note')));

  const couldHaveNotes = options.couldHaveNotes ?? (typeName => true);

  Operations.addSettings(new EntityOperationSettings(NoteOperation.CreateNoteFromEntity, {
    isVisible: eoc => couldHaveNotes!(eoc.entity.Type),
    icon: "note-sticky",
    iconColor: "#0e4f8c",
    color: "info",
    contextual: { isVisible: ctx => couldHaveNotes(ctx.context.lites[0].EntityType), }
  }));

  QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkExplore({
    queryName: NoteEntity,
    filterOptions: [{ token: NoteEntity.token(e => e.target), value: ctx.lite}]
  }, {
    isVisible: Navigator.isViewable(NoteEntity) && couldHaveNotes(ctx.lite.EntityType),
    icon: "note-sticky",
    iconColor: "#337ab7",
  }));
}
