import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PackageOperationEntity, PackageLineEntity, PackageQuery } from '../Signum.Entities.Processes'

export default class PackageOperation extends React.Component<{ ctx: TypeContext<PackageOperationEntity> }> {
  render() {
    const e = this.props.ctx;

    return (
      <div>
        <ValueLine ctx={e.subCtx(f => f.name)} />
        <EntityLine ctx={e.subCtx(f => f.operation)} readOnly={true} />
        <fieldset>
          <legend>{PackageLineEntity.nicePluralName()}</legend>
          <SearchControl findOptions={{ queryName: PackageQuery.PackageLineLastProcess, parentToken: PackageLineEntity.token(e => e.package), parentValue: e.value }} />
        </fieldset>
      </div>
    );
  }
}

