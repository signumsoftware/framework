import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ExchangeWebServiceEmailServiceEntity } from '../../Signum.Mailing'
import { Binding } from '@framework/Reflection'
import { DoublePassword } from '../../../Signum.Authorization/Templates/DoublePassword'

export default function ExchangeWebServiceEmailService(p: { ctx: TypeContext<ExchangeWebServiceEmailServiceEntity> }) {
  const sc = p.ctx;

  return (
    <div>
      <ValueLine ctx={sc.subCtx(s => s.exchangeVersion)} />
      <ValueLine ctx={sc.subCtx(s => s.url)} />
      <ValueLine ctx={sc.subCtx(s => s.useDefaultCredentials)} />
      <ValueLine ctx={sc.subCtx(s => s.username)} />
      {!sc.readOnly &&
        <DoublePassword ctx={new TypeContext<string>(sc, undefined, undefined as any, Binding.create(sc.value, v => v.newPassword))} initialOpen={sc.value.isNew ?? false} mandatory={false} />}
    </div>
  );
}

