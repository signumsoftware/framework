
import * as React from 'react'
import { EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { LinkListPartEntity } from '../Signum.Dashboard'

export default function LinkListPart(p : { ctx: TypeContext<LinkListPartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div>
      <EntityTable ctx={ctx.subCtx(p => p.links)} />
    </div>
  );
}
