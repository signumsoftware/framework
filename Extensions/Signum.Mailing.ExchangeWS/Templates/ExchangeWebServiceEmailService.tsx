import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ExchangeWebServiceEmailServiceEntity } from '../Signum.Mailing.ExchangeWS';

export default function ExchangeWebServiceEmailService(p: { ctx: TypeContext<ExchangeWebServiceEmailServiceEntity> }) {
  const sc = p.ctx;

  return (
    <div>
      <ValueLine ctx={sc.subCtx(s => s.exchangeVersion)} />
      <ValueLine ctx={sc.subCtx(s => s.url)} />
      <ValueLine ctx={sc.subCtx(s => s.useDefaultCredentials)} />
      <ValueLine ctx={sc.subCtx(s => s.username)} />
      <ValueLine ctx={sc.subCtx(s => s.password)} valueLineType="Password" />
    </div>
  );
}

