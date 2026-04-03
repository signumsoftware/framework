import * as React from 'react'
import { AutoLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PackageEntity, PackageLineEntity, PackageQuery } from '../Signum.Processes'

export default function Package(p : { ctx: TypeContext<PackageEntity> }): React.JSX.Element {
  const e = p.ctx;

  return (
    <div>
      <AutoLine ctx={e.subCtx(f => f.name)} />
      <SearchControl title={PackageLineEntity.nicePluralName()} showTitle="display-6" findOptions={{ queryName: PackageQuery.PackageLineLastProcess, filterOptions: [{ token: PackageLineEntity.token(e => e.package), value: e.value }] }} />
    </div>
  );
}

