
import * as React from 'react'
import { EntityLine, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarMenuPartEntity } from '../Signum.Dashboard'

export default function ToolbarMenuPart(p: { ctx: TypeContext<ToolbarMenuPartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(p => p.toolbarMenu)} />
    </div>
  );
}
