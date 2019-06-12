import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SMSMessageEntity } from '../Signum.Entities.SMS'

export default function SMSMessage(p: { ctx: TypeContext<SMSMessageEntity> }) {

  return (
    <div>
      <ValueLine ctx={p.ctx.subCtx(a => a.messageID)} readOnly={true} />
      <EntityLine ctx={p.ctx.subCtx(a => a.template)} create={false} remove={false} />
      <ValueLine ctx={p.ctx.subCtx(a => a.certified)} readOnly={p.ctx.value.state == "Created"} />
      <ValueLine ctx={p.ctx.subCtx(a => a.destinationNumber)} readOnly={p.ctx.value.isNew} />
      <ValueLine ctx={p.ctx.subCtx(a => a.message)} formGroupHtmlAttributes={{ className: "sf-sms-msg-text" }} readOnly={p.ctx.value.editableMessage || p.ctx.value.state != "Created"} />

      {p.ctx.value.state == "Created" && p.ctx.value.editableMessage &&
        <div />}

      <ValueLine ctx={p.ctx.subCtx(a => a.from)} readOnly={p.ctx.value.state != "Created"} />
      {p.ctx.value.state != "Created" &&
        <div>
          <ValueLine ctx={p.ctx.subCtx(a => a.sendDate)} readOnly={true} />
          <ValueLine ctx={p.ctx.subCtx(a => a.state)} readOnly={true} />
        </div>}
    </div>);
}
