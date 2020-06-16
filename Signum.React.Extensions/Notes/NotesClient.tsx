import { EntitySettings } from '@framework/Navigator'
import * as Navigator from '@framework/Navigator'
import { EntityOperationSettings } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { NoteEntity, NoteTypeEntity, NoteOperation } from './Signum.Entities.Notes'
import * as QuickLinks from '@framework/QuickLinks'
import * as AuthClient from '../Authorization/AuthClient'

export function start(options: { routes: JSX.Element[], couldHaveNotes?: (typeName: string) => boolean }) {
  Navigator.addSettings(new EntitySettings(NoteEntity, e => import('./Templates/Note')));

  const couldHaveNotes = options.couldHaveNotes ?? (typeName => true);

  Operations.addSettings(new EntityOperationSettings(NoteOperation.CreateNoteFromEntity, {
    isVisible: eoc => couldHaveNotes!(eoc.entity.Type),
    contextual: { icon: "sticky-note", iconColor: "#0e4f8c", color: "info", isVisible: ctx => couldHaveNotes(ctx.context.lites[0].EntityType), }
  }));

  QuickLinks.registerGlobalQuickLink(ctx => new QuickLinks.QuickLinkExplore({
    queryName: NoteEntity,
    parentToken: NoteEntity.token(e => e.target),
    parentValue: ctx.lite
  }, {
      isVisible: AuthClient.navigatorIsViewable(NoteEntity) && couldHaveNotes(ctx.lite.EntityType),
      icon: "sticky-note",
      iconColor: "#337ab7",
      isShy: true
    }));
}
