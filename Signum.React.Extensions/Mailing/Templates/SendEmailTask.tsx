import * as React from 'react'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { ValueLine, EntityLine } from '@framework/Lines'
import { Lite, is } from '@framework/Signum.Entities'
import { TypeContext } from '@framework/TypeContext'
import { SendEmailTaskEntity, EmailTemplateEntity } from '../Signum.Entities.Mailing'
import { useAPI, useForceUpdate } from '../../../../Framework/Signum.React/Scripts/Hooks'

export default function SendEmailTask(p: { ctx: TypeContext<SendEmailTaskEntity> }) {

  const forceUpdate = useForceUpdate();

  const type = useAPI(() =>
    p.ctx.value.emailTemplate == null ? Promise.resolve(undefined) :
    Navigator.API.fetchAndForget(p.ctx.value.emailTemplate)
      .then(et => Finder.getQueryDescription(et.query!.key))
      .then(qd => qd.columns["Entity"].type.name),
    [p.ctx.value.emailTemplate])

  const sc = p.ctx;
  const ac = p.ctx.subCtx({ formGroupStyle: "Basic" });

  return (
    <div>
      <ValueLine ctx={sc.subCtx(s => s.name)} />
      <EntityLine ctx={sc.subCtx(s => s.emailTemplate)} onChange={forceUpdate} />
      {type && <EntityLine ctx={sc.subCtx(s => s.targetsFromUserQuery)} />}
      {type && <EntityLine ctx={sc.subCtx(s => s.uniqueTarget)} type={{ isLite: true, name: type }} />}
    </div>
  );
}

