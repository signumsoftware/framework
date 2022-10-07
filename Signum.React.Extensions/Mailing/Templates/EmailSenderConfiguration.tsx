import * as React from 'react'
import { ValueLine, EntityRepeater, EntityDetail, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailSenderConfigurationEntity } from '../Signum.Entities.Mailing'

export default function EmailSenderConfiguration(p: { ctx: TypeContext<EmailSenderConfigurationEntity> }) {
  const sc = p.ctx;

  return (
    <div>
      <ValueLine ctx={sc.subCtx(s => s.name)} />
      <EntityDetail ctx={sc.subCtx(s => s.defaultFrom)} />
      <EntityRepeater ctx={sc.subCtx(s => s.additionalRecipients)} />
      <EntityLine ctx={sc.subCtx(s => s.service)} find={true} />
    </div >
  );
}

