import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PackageOperationEntity, PackageLineEntity, PackageQuery } from '../Signum.Processes'

export default function PackageOperation(p : { ctx: TypeContext<PackageOperationEntity> }){
  const e = p.ctx;

  return (
    <div>
      <ValueLine ctx={e.subCtx(f => f.name)} />
      <EntityLine ctx={e.subCtx(f => f.operation)} readOnly={true} />
      <fieldset>
        <legend>{PackageLineEntity.nicePluralName()}</legend>
        <SearchControl findOptions={{ queryName: PackageQuery.PackageLineLastProcess, filterOptions: [{ token: PackageLineEntity.token(e => e.package), value: e.value }]}} />
      </fieldset>
    </div>
  );
}

