import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailAddressEmbedded, EmailFromEmbedded } from '../Signum.Mailing'

export default function EmailFrom(p: { ctx: TypeContext<EmailFromEmbedded> }) {
  const sc = p.ctx.subCtx({ placeholderLabels: true, formGroupStyle: "SrOnly" });

  return (
    <div className="row">
      <div className="col-sm-11 offset-sm-1">
        <EntityLine ctx={sc.subCtx(ea => ea.emailOwner)} />
      </div>
      <div className="col-sm-5 offset-sm-1">
        <ValueLine ctx={sc.subCtx(c => c.emailAddress)} />
        <ValueLine ctx={sc.subCtx(c => c.azureUserId)} />
      </div>
      <div className="col-sm-6">
        <ValueLine ctx={sc.subCtx(c => c.displayName)} />
      </div>
    </div>
  );
}

