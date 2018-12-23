import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PackageEntity, PackageLineEntity, PackageQuery } from '../Signum.Entities.Processes'

export default class Package extends React.Component<{ ctx: TypeContext<PackageEntity> }> {
  render() {
    const e = this.props.ctx;

    return (
      <div>
        <ValueLine ctx={e.subCtx(f => f.name)} />
        <fieldset>
          <legend>{PackageLineEntity.nicePluralName()}</legend>
          <SearchControl findOptions={{ queryName: PackageQuery.PackageLineLastProcess, parentToken: PackageLineEntity.token(e => e.package), parentValue: e.value }} />
        </fieldset>
      </div>
    );
  }
}

