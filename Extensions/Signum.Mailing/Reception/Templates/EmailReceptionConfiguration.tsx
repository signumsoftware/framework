import * as React from 'react'
import { AutoLine, EntityRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailReceptionConfigurationEntity } from '../Signum.Mailing.Reception';

export default function EmailReceptionConfiguration(p: { ctx: TypeContext<EmailReceptionConfigurationEntity> }): React.JSX.Element {
  const sc = p.ctx.subCtx({ formGroupStyle: "Basic" });

  return (
    <div>
      <AutoLine ctx={sc.subCtx(s => s.active)} />
      <AutoLine ctx={sc.subCtx(s => s.emailAddress)} />
      <AutoLine ctx={sc.subCtx(s => s.deleteMessagesAfter)} />
      <AutoLine ctx={sc.subCtx(s => s.compareInbox)} />
      <AutoLine ctx={sc.subCtx(s => s.service)} />
    </div>

  );
}
