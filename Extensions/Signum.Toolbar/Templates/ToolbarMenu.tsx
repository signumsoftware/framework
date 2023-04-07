import * as React from 'react'
import { ValueLine, EntityRepeater, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarMenuEntity } from '../Signum.Toolbar'
import { ToolbarElementTable } from './Toolbar';

export default function ToolbarMenu(p : { ctx: TypeContext<ToolbarMenuEntity> }){
  const ctx = p.ctx;

  return (
    <div>
      <ValueLine ctx={ctx.subCtx(f => f.name)} />
      <EntityLine ctx={ctx.subCtx(f => f.owner)} />
      <ToolbarElementTable ctx={ctx.subCtx(m => m.elements)} />
    </div>
  );
}
