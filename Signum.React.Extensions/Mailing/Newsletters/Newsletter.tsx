import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { NewsletterEntity } from '../Signum.Entities.Mailing'

export default function Newsletter(p : { ctx: TypeContext<NewsletterEntity> }){
  const nc = p.ctx;

  return (
    <div>
      <ValueLine ctx={nc.subCtx(n => n.name)} />
      <ValueLine ctx={nc.subCtx(n => n.state)} readOnly={true} />
      <ValueLine ctx={nc.subCtx(n => n.from)} />
      <ValueLine ctx={nc.subCtx(n => n.displayFrom)} />
      <EntityLine ctx={nc.subCtx(e => e.query)} />
      <ValueLine ctx={nc.subCtx(n => n.subject)} />
      <ValueLine ctx={nc.subCtx(n => n.text)} valueLineType="TextArea" valueHtmlAttributes={{ style: { width: "100%", height: "180px" } }} />
    </div>
  );
}

