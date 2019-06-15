import * as React from 'react'
import { ValueLine, EntityTabRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SMSTemplateEntity, SMSCharactersMessage } from '../Signum.Entities.SMS'

export default function SMSTemplate(p: { ctx: TypeContext<SMSTemplateEntity> }) {

  var ctx = p.ctx.subCtx({ labelColumns: 4 });
  return (
    <div>
      <ValueLine ctx={p.ctx.subCtx(a => a.name)} />
      <div className="row">
        <div className="col-sm-6">
          <ValueLine ctx={ctx.subCtx(a => a.active)} />
          <ValueLine ctx={ctx.subCtx(a => a.startDate)} />
          <ValueLine ctx={ctx.subCtx(a => a.endDate)} />
          <ValueLine ctx={ctx.subCtx(a => a.from)} />
        </div>
        <div className="col-sm-6">
          <ValueLine ctx={ctx.subCtx(a => a.certified)} />
          <ValueLine ctx={ctx.subCtx(a => a.editableMessage)} />
          <ValueLine ctx={ctx.subCtx(a => a.removeNoSMSCharacters)} />
          <ValueLine ctx={ctx.subCtx(a => a.messageLengthExceeded)} />
        </div>
      </div>


      <EntityTabRepeater ctx={p.ctx.subCtx(a => a.messages)} />
    </div>
  );
}
