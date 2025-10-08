
import * as React from 'react'
import { EntityLine, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ToolbarPartEntity } from '../Signum.Dashboard'
import ToolbarRenderer from '../../Signum.Toolbar/Renderers/ToolbarRenderer';
import ToolbarMenu from '../../Signum.Toolbar/Templates/ToolbarMenu';

export default function ToolbarPart(p: { ctx: TypeContext<ToolbarPartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(p => p.toolbarMenu)} />
    </div>
  );
}
