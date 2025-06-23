import * as React from 'react'
import { AutoLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { MultipleSMSModel } from '../Signum.SMS'

export default function MultipleSMS(p: { ctx: TypeContext<MultipleSMSModel> }): React.JSX.Element {

  return (
    <div>
      <AutoLine ctx={p.ctx.subCtx(a => a.message)} formGroupHtmlAttributes={{ className: "sf-sms-msg-text" }} />
      <AutoLine ctx={p.ctx.subCtx(a => a.from)} />
    </div>);
}
