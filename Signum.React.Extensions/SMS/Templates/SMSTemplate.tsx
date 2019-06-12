import * as React from 'react'
import { ValueLine, EntityTabRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { SMSTemplateEntity, SMSCharactersMessage } from '../Signum.Entities.SMS'

export default function SMSTemplate(p: { ctx: TypeContext<SMSTemplateEntity> }) {

  return (
    <div>
      <ValueLine ctx={p.ctx.subCtx(a => a.name)} />
      <div className="Row">
        <div className="col-sm-8">
          <ValueLine ctx={p.ctx.subCtx(a => a.active)} />
          <ValueLine ctx={p.ctx.subCtx(a => a.startDate)} />
          <ValueLine ctx={p.ctx.subCtx(a => a.endDate)} />
        </div>
        <div className="col-sm-4">
          <ValueLine ctx={p.ctx.subCtx(a => a.certified)} />
          <ValueLine ctx={p.ctx.subCtx(a => a.editableMessage)} />
          <ValueLine ctx={p.ctx.subCtx(a => a.removeNoSMSCharacters)} />
        </div>
      </div>

      <ValueLine ctx={p.ctx.subCtx(a => a.from)} />
      <ValueLine ctx={p.ctx.subCtx(a => a.messageLengthExceeded)} />

      <div className="Row">
        <div className="col-sm-7">
          <EntityTabRepeater ctx={p.ctx.subCtx(a => a.messages)} />
        </div>
        <div className="col-sm-5">
          <fieldset>
            <legend>{SMSCharactersMessage.Replacements.niceToString()}</legend>
          </fieldset>
        </div>
      </div>
    </div>);
}
