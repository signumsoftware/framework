import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { TypeContext } from '@framework/TypeContext'
import { EmailPackageEntity, EmailMessageEntity } from '../Signum.Entities.Mailing'

export default class EmailPackage extends React.Component<{ ctx: TypeContext<EmailPackageEntity> }> {
  render() {
    const e = this.props.ctx;

    return (
      <div>
        <ValueLine ctx={e.subCtx(f => f.name)} readOnly={true} />
        <fieldset>
          <legend>{EmailMessageEntity.nicePluralName()}</legend>
          <SearchControl findOptions={{
            queryName: EmailMessageEntity,
            parentToken: EmailMessageEntity.token(e => e.package),
            parentValue: e.value
          }} />
        </fieldset>
      </div>
    );
  }
}

