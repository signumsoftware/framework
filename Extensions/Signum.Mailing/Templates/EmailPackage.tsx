import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { EmailPackageEntity, EmailMessageEntity } from '../Signum.Mailing'

export default function EmailPackage(p : { ctx: TypeContext<EmailPackageEntity> }){
  const e = p.ctx;

  return (
    <div>
      <ValueLine ctx={e.subCtx(f => f.name)} readOnly={true} />
      <fieldset>
        <legend>{EmailMessageEntity.nicePluralName()}</legend>
        <SearchControl findOptions={{
          queryName: EmailMessageEntity,
          filterOptions: [{ token: EmailMessageEntity.token(e => e.package), value: e.value}]
        }} />
      </fieldset>
    </div>
  );
}

