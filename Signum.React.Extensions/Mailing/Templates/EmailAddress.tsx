import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailAddressEmbedded } from '../Signum.Entities.Mailing'

export default function EmailAddress(p : { ctx: TypeContext<EmailAddressEmbedded> }){
  const sc = p.ctx.subCtx({ placeholderLabels: true, formGroupStyle: "SrOnly" });

  return (
    <div className="row">
      <div className="col-sm-11 offset-sm-1">
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

