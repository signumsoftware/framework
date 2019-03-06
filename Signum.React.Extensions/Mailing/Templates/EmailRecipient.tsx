import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailRecipientEmbedded } from '../Signum.Entities.Mailing'

export default function EmailRecipient(p : { ctx: TypeContext<EmailRecipientEmbedded> }){
  const sc = p.ctx.subCtx({ placeholderLabels: true, formGroupStyle: "SrOnly" });

  return (
    <div className="row">
      <div className="col-sm-1">
        <ValueLine ctx={sc.subCtx(c => c.kind)} />
      </div>
      <div className="col-sm-11">
        <EntityLine ctx={sc.subCtx(ea => ea.emailOwner)} />
      </div>
      <div className="col-sm-5 offset-sm-1">
        <ValueLine ctx={sc.subCtx(c => c.emailAddress)} />
      </div>
      <div className="col-sm-6">
        <ValueLine ctx={sc.subCtx(c => c.displayName)} />
      </div>
    </div>
  );
}

