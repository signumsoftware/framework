import * as React from 'react'
import { AutoLine, PasswordLine, TextBoxLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ExchangeWebServiceEmailServiceEntity } from '../Signum.Mailing.ExchangeWS';

export default function ExchangeWebServiceEmailService(p: { ctx: TypeContext<ExchangeWebServiceEmailServiceEntity> }) {
  const sc = p.ctx;

  return (
    <div>
      <AutoLine ctx={sc.subCtx(s => s.exchangeVersion)} />
      <AutoLine ctx={sc.subCtx(s => s.url)} />
      <AutoLine ctx={sc.subCtx(s => s.useDefaultCredentials)} />
      <AutoLine ctx={sc.subCtx(s => s.username)} />
      <AutoLine ctx={sc.subCtx(s => s.password)} />
    </div>
  );
}

