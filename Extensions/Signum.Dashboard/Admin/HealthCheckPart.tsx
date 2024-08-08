
import * as React from 'react'
import { EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { HealthCheckPartEntity } from '../Signum.Dashboard'

export default function HealthCheckPart(p: { ctx: TypeContext<HealthCheckPartEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: "SrOnly", placeholderLabels: true });

  return (
    <div>
      <EntityTable ctx={ctx.subCtx(p => p.items)} />
    </div>
  );
}
