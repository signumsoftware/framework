import * as React from 'react'
import { ValueLine, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailReceptionConfigurationEntity } from '../../Signum.MailingReception';

export default function EmailReceptionConfiguration(p: { ctx: TypeContext<EmailReceptionConfigurationEntity> }) {
  const sc = p.ctx.subCtx({ formGroupStyle: "Basic" });

  return (
    <div>
      <ValueLine ctx={sc.subCtx(s => s.active)} />
      <ValueLine ctx={sc.subCtx(s => s.emailAddress)} />
      <ValueLine ctx={sc.subCtx(s => s.deleteMessagesAfter)} />
      <ValueLine ctx={sc.subCtx(s => s.compareInbox)} />
      <ValueLine ctx={sc.subCtx(s => s.service)} />
    </div>

  );
}
