import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { MultipleSMSModel } from '../Signum.Entities.SMS'

export default function MultipleSMS(p: { ctx: TypeContext<MultipleSMSModel> }){

  return (
    <div>
      <ValueLine ctx={p.ctx.subCtx(a => a.message)} formGroupHtmlAttributes={{ className: "sf-sms-msg-text" }} />
      <ValueLine ctx={p.ctx.subCtx(a => a.from)} />
    </div>);
}
