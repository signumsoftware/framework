import * as React from 'react'
import { AutoLine, EntityLine } from '@framework/Lines'
import { SearchControl, SearchValueLine } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PackageLineEntity, ProcessExceptionLineEntity } from '../Signum.Processes'

export default function Package(p : { ctx: TypeContext<PackageLineEntity> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ readOnly: true });

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(f => f.package)} />
      <EntityLine ctx={ctx.subCtx(f => f.target)} />
      <EntityLine ctx={ctx.subCtx(f => f.result)} />
      <AutoLine ctx={ctx.subCtx(f => f.finishTime)} />
      <SearchValueLine ctx={ctx}
        badgeColor="danger"
        findOptions={{
          queryName: ProcessExceptionLineEntity,
          filterOptions: [{ token: ProcessExceptionLineEntity.token(e => e.line), value: ctx.value}]
        }} />
    </div>
  );
}

