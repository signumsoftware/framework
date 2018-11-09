import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailRecipientEntity } from '../Signum.Entities.Mailing'

export default class EmailRecipient extends React.Component<{ ctx: TypeContext<EmailRecipientEntity> }> {
  render() {
    const sc = this.props.ctx.subCtx({ placeholderLabels: true, formGroupStyle: "SrOnly" });

    return (
      <div className="row">
        <div className="col-sm-1">
          <ValueLine ctx={sc.subCtx(c => c.kind)} />
        </div>
        <div className="col-sm-4">
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

