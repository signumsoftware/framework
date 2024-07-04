import * as React from 'react'
import { AutoLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { EmailMessageEntity } from '../../Signum.Mailing/Signum.Mailing';
import { EmailMessagePackageMixin, EmailPackageEntity } from './Signum.Mailing.Package';

export default function EmailPackage(p : { ctx: TypeContext<EmailPackageEntity> }): React.JSX.Element {
  const e = p.ctx;

  return (
    <div>
      <AutoLine ctx={e.subCtx(f => f.name)} readOnly={true} />
      <fieldset>
        <legend>{EmailMessageEntity.nicePluralName()}</legend>
        <SearchControl findOptions={{
          queryName: EmailMessageEntity,
          filterOptions: [
            {
              token: EmailMessageEntity.token(e => e.entity).mixin(EmailMessagePackageMixin).append(e => e.package),
              value: e.value
            }
          ]
        }} />
      </fieldset>
    </div>
  );
}

