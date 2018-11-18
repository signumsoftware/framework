import * as React from 'react'
import { ValueLine, EntityLine, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarEntity } from '../Signum.Entities.Toolbar'

export default class Toolbar extends React.Component<{ ctx: TypeContext<ToolbarEntity> }> {
  render() {
    const ctx = this.props.ctx;
    const ctx3 = ctx.subCtx({ labelColumns: 3 });
    return (
      <div>
        <div className="row">
          <div className="col-sm-7">
            <ValueLine ctx={ctx3.subCtx(f => f.name)} />
            <EntityLine ctx={ctx3.subCtx(e => e.owner)} />
          </div>

          <div className="col-sm-5">
            <ValueLine ctx={ctx3.subCtx(f => f.location)} />
            <ValueLine ctx={ctx3.subCtx(e => e.priority)} />
          </div>
        </div>
        <EntityRepeater ctx={ctx.subCtx(f => f.elements)} />
      </div>
    );
  }
}
