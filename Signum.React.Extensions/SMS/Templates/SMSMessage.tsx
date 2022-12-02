import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SMSMessageEntity } from '../Signum.Entities.SMS'

export default function SMSMessage(p: { ctx: TypeContext<SMSMessageEntity> }) {

  var ctx4 = p.ctx.subCtx({ labelColumns: 4, formSize: "xs" });

  return (
    <div>

      <div className="row">
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(a => a.from)} readOnly={p.ctx.value.state != "Created"} />
          <ValueLine ctx={ctx4.subCtx(a => a.destinationNumber)} readOnly={p.ctx.value.state != "Created"} />
          <ValueLine ctx={ctx4.subCtx(a => a.certified)} readOnly={p.ctx.value.state != "Created"} />
          <EntityLine ctx={ctx4.subCtx(a => a.referred)} readOnly={true} />
        </div>
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(a => a.messageID)} readOnly={true} />
          <EntityLine ctx={ctx4.subCtx(a => a.template)} readOnly={true} />
          {p.ctx.value.state != "Created" &&
            <div>
              <ValueLine ctx={ctx4.subCtx(a => a.sendDate)} readOnly={true} />
              <ValueLine ctx={ctx4.subCtx(a => a.state)} readOnly={true} />
            </div>
          }
        </div>
      </div>

      <ValueLine ctx={p.ctx.subCtx(a => a.message)} readOnly={!(p.ctx.value.editableMessage || p.ctx.value.state == "Created")}
        formGroupHtmlAttributes={{ className: "sf-sms-msg-text" }} />


    </div>);
}
