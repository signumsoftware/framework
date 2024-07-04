import * as React from 'react'
import { AutoLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PrintPackageEntity, PrintLineEntity } from '../Signum.Printing'

export default function PrintPackage(p : { ctx: TypeContext<PrintPackageEntity> }): React.JSX.Element {
  const e = p.ctx;

  return (
    <div>
      <AutoLine ctx={e.subCtx(f => f.name)} />
      <fieldset>
        <legend>{PrintLineEntity.nicePluralName()}</legend>
        <SearchControl findOptions={{ queryName: PrintLineEntity, filterOptions: [{ token: PrintLineEntity.token(e => e.package), value: e.value }]}} />
      </fieldset>
    </div>
  );
}

