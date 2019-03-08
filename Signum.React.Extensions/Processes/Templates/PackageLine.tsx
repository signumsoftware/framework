import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PackageLineEntity, ProcessExceptionLineEntity } from '../Signum.Entities.Processes'

export default function Package(p : { ctx: TypeContext<PackageLineEntity> }){
  const e = p.ctx.subCtx({ readOnly: true });

  return (
    <div>
      <EntityLine ctx={e.subCtx(f => f.package)} />
      <EntityLine ctx={e.subCtx(f => f.target)} />
      <EntityLine ctx={e.subCtx(f => f.result)} />
      <ValueLine ctx={e.subCtx(f => f.finishTime)} />
      <fieldset>
        <legend>{PackageLineEntity.nicePluralName()}</legend>
        <SearchControl findOptions={{
          queryName: ProcessExceptionLineEntity,
          parentToken: ProcessExceptionLineEntity.token(e => e.line),
          parentValue: e.value
        }} />
      </fieldset>
    </div>
  );
}

