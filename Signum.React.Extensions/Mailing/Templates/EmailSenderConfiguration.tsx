import * as React from 'react'
import { ValueLine, EntityRepeater, EntityDetail, EntityAccordion } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { EmailSenderConfigurationEntity, SmtpNetworkDeliveryEmbedded, ClientCertificationFileEmbedded, SmtpEmbedded, ExchangeWebServiceEmbedded, MicrosoftGraphEmbedded, EmailRecipientEmbedded, EmailTemplateAddressEmbedded } from '../Signum.Entities.Mailing'
import { Binding } from '@framework/Reflection'
import { DoublePassword } from '../../Authorization/Templates/DoublePassword'

export default function EmailSenderConfiguration(p: { ctx: TypeContext<EmailSenderConfigurationEntity> }) {
  const sc = p.ctx;

  return (
    <div>
      <ValueLine ctx={sc.subCtx(s => s.name)} />
      <EntityDetail ctx={sc.subCtx(s => s.defaultFrom)} />
      <EntityAccordion ctx={sc.subCtx(s => s.additionalRecipients)}
        getTitle={(ctx: TypeContext<EmailRecipientEmbedded>) => <span>
          {ctx.value.kind && <strong className="me-1">{ctx.value.kind}:</strong>}
          {ctx.value.displayName && <span className="me-1">{ctx.value.displayName}</span>}
          {ctx.value.emailAddress && <span>{"<"}{ctx.value.emailAddress}{">"}</span>}
        </span>
        }/>
      <EntityDetail ctx={sc.subCtx(s => s.service)} />
    </div >
  );
}

