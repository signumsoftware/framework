import * as React from 'react'
import { AutoLine, EntityLine, EntityCombo, TextAreaLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { NoteEntity } from '../Signum.Notes'

export default function Note(p : { ctx: TypeContext<NoteEntity> }): React.JSX.Element {
  const e = p.ctx;

  const ec = e.subCtx({ labelColumns: { sm: 2 } });
  const sc = ec.subCtx({ formGroupStyle: "Basic" });

  return (
    <div>
      {!ec.value.isNew &&
        <div>
          <EntityLine ctx={ec.subCtx(e => e.createdBy)} readOnly={true} />
          <AutoLine ctx={ec.subCtx(e => e.creationDate)} readOnly={true} />
        </div>
      }
      <EntityLine ctx={ec.subCtx(n => n.target)} readOnly={true} />
      <hr />
      <AutoLine ctx={ec.subCtx(n => n.title)} />
      <EntityCombo ctx={ec.subCtx(n => n.noteType)} remove={true} />
      <TextAreaLine ctx={ec.subCtx(n => n.text)} valueHtmlAttributes={{ style: { height: "180px" } }} />
    </div>
  );
}
