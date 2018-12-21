import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PrintPackageEntity, PrintLineEntity } from '../Signum.Entities.Printing'

export default class PrintPackage extends React.Component<{ ctx: TypeContext<PrintPackageEntity> }> {
  render() {
    const e = this.props.ctx;

    return (
      <div>
        <ValueLine ctx={e.subCtx(f => f.name)} />
        <fieldset>
          <legend>{PrintLineEntity.nicePluralName()}</legend>
          <SearchControl findOptions={{ queryName: PrintLineEntity, parentToken: PrintLineEntity.token(e => e.package), parentValue: e.value }} />
        </fieldset>
      </div>
    );
  }
}

