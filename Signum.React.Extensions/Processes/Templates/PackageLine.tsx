import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { SearchControl, ValueSearchControlLine } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PackageLineEntity, ProcessExceptionLineEntity } from '../Signum.Entities.Processes'

export default function Package(p : { ctx: TypeContext<PackageLineEntity> }){
  const ctx = p.ctx.subCtx({ readOnly: true });

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(f => f.package)} />
      <EntityLine ctx={ctx.subCtx(f => f.target)} />
      <EntityLine ctx={ctx.subCtx(f => f.result)} />
      <ValueLine ctx={ctx.subCtx(f => f.finishTime)} />
      <ValueSearchControlLine ctx={ctx}
        badgeColor="danger"
        findOptions={{
          queryName: ProcessExceptionLineEntity,
          parentToken: ProcessExceptionLineEntity.token(e => e.line),
          parentValue: ctx.value
        }} />
    </div>
  );
}

