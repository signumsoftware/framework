import * as React from 'react'
import { AutoLine, EntityRepeater, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarMenuEntity } from '../Signum.Toolbar'
import { ToolbarElementTable } from './Toolbar';

export default function ToolbarMenu(p : { ctx: TypeContext<ToolbarMenuEntity> }): React.JSX.Element {
  const ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(f => f.name)} />
      <EntityLine ctx={ctx.subCtx(f => f.owner)} />
      <EntityLine ctx={ctx.subCtx(f => f.entityType)}/>
      <ToolbarElementTable ctx={ctx.subCtx(m => m.elements)} />
    </div>
  );
}
