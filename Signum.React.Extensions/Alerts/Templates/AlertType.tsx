import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { AlertTypeEntity } from '../Signum.Entities.Alerts'

export default class AlertType extends React.Component<{ ctx: TypeContext<AlertTypeEntity> }> {

  render() {
    const ctx = this.props.ctx;
    const ctx4 = ctx.subCtx({ labelColumns: 2 });
    return (
      <div>
        <ValueLine ctx={ctx4.subCtx(n => n.name)} />
      </div>
    );
  }
}
