import * as React from 'react'
import { ValueLine, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarMenuEntity } from '../Signum.Entities.Toolbar'

export default class ToolbarMenu extends React.Component<{ ctx: TypeContext<ToolbarMenuEntity> }> {
  render() {
    const ctx = this.props.ctx;

    return (
      <div>
        <ValueLine ctx={ctx.subCtx(f => f.name)} />
        <EntityRepeater ctx={ctx.subCtx(f => f.elements)} />
      </div>
    );
  }
}
