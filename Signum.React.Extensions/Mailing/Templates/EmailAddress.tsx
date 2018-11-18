import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailAddressEmbedded } from '../Signum.Entities.Mailing'

export default class EmailAddress extends React.Component<{ ctx: TypeContext<EmailAddressEmbedded> }> {
  render() {
    const sc = this.props.ctx.subCtx({ placeholderLabels: true, formGroupStyle: "SrOnly" });

    return (
      <div className="row">
        <div className="col-sm-4 col-sm-offset-2">
          <EntityLine ctx={sc.subCtx(ea => ea.emailOwner)} />
        </div>
        <div className="col-sm-3">
          <ValueLine ctx={sc.subCtx(c => c.emailAddress)} />
        </div>
        <div className="col-sm-3">
          <ValueLine ctx={sc.subCtx(c => c.displayName)} />
        </div>
      </div>
    );
  }
}

