import * as React from 'react'
import { ValueLine, EntityLine, EntityCombo } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { NoteEntity } from '../Signum.Entities.Notes'

export default class Note extends React.Component<{ ctx: TypeContext<NoteEntity> }> {
  render() {
    const e = this.props.ctx;

    const ec = e.subCtx({ labelColumns: { sm: 2 } });
    const sc = ec.subCtx({ formGroupStyle: "Basic" });

    return (
      <div>
        {!ec.value.isNew &&
          <div>
            <EntityLine ctx={ec.subCtx(e => e.createdBy)} readOnly={true} />
            <ValueLine ctx={ec.subCtx(e => e.creationDate)} readOnly={true} />
          </div>
        }
        <EntityLine ctx={ec.subCtx(n => n.target)} readOnly={true} />
        <hr />
        <ValueLine ctx={ec.subCtx(n => n.title)} />
        <EntityCombo ctx={ec.subCtx(n => n.noteType)} remove={true} />
        <ValueLine ctx={ec.subCtx(n => n.text)} valueLineType="TextArea" valueHtmlAttributes={{ style: { height: "180px" } }} />
      </div>
    );
  }
}
