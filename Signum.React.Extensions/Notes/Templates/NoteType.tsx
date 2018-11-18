import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { NoteTypeEntity } from '../Signum.Entities.Notes'

export default class NoteType extends React.Component<{ ctx: TypeContext<NoteTypeEntity> }> {
  render() {
    const e = this.props.ctx;

    const ec = e.subCtx({ labelColumns: { sm: 2 } });
    const sc = ec.subCtx({ formGroupStyle: "Basic" });

    return (
      <div>
        <ValueLine ctx={ec.subCtx(n => n.name)} />
      </div>
    );
  }
}
