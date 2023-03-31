import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PackageEntity, PackageLineEntity, PackageQuery } from '../Signum.Processes'

export default function Package(p : { ctx: TypeContext<PackageEntity> }){
  const e = p.ctx;

  return (
    <div>
      <ValueLine ctx={e.subCtx(f => f.name)} />
      <fieldset>
        <legend>{PackageLineEntity.nicePluralName()}</legend>
        <SearchControl findOptions={{ queryName: PackageQuery.PackageLineLastProcess, filterOptions: [{ token: PackageLineEntity.token(e => e.package), value: e.value }]}} />
      </fieldset>
    </div>
  );
}

