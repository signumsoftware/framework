import * as React from 'react'
import { AutoLine, EntityLine, TextBoxLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailRecipientEmbedded } from '../Signum.Mailing'

export default function EmailRecipient(p : { ctx: TypeContext<EmailRecipientEmbedded> }): React.JSX.Element {
  const sc = p.ctx.subCtx({ placeholderLabels: true, formGroupStyle: "SrOnly" });

  return (
    <div className="row">
      <div className="col-sm-1">
        <AutoLine ctx={sc.subCtx(c => c.kind)} onChange={e => p.ctx.frame?.frameComponent.forceUpdate()} />
      </div>
      <div className="col-sm-11">
        <EntityLine ctx={sc.subCtx(ea => ea.emailOwner)} />
      </div>
      <div className="col-sm-5 offset-sm-1">
        <TextBoxLine ctx={sc.subCtx(c => c.emailAddress)} valueHtmlAttributes={{ onBlur: e => p.ctx.frame?.frameComponent.forceUpdate() }} />
      </div>
      <div className="col-sm-6">
        <TextBoxLine ctx={sc.subCtx(c => c.displayName)} valueHtmlAttributes={{ onBlur: e => p.ctx.frame?.frameComponent.forceUpdate() }} />
      </div>
    </div>
  );
}

