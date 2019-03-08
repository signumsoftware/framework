import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { PrintPackageEntity, PrintLineEntity } from '../Signum.Entities.Printing'

export default function PrintPackage(p : { ctx: TypeContext<PrintPackageEntity> }){
  const e = p.ctx;

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

