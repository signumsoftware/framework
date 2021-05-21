import * as React from 'react'
import { ValueLine, EntityLine, EntityDetail } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ScheduledTaskEntity } from '../Signum.Entities.Scheduler'

export default function ScheduledTask(p : { ctx: TypeContext<ScheduledTaskEntity> }){
  const ctx = p.ctx;

  return (
    <div>
      <EntityLine ctx={ctx.subCtx(f => f.task)} create={false} />
      <EntityDetail ctx={ctx.subCtx(f => f.rule)} />
      <EntityLine ctx={ctx.subCtx(f => f.user)} />
      {!ctx.value.isNew && <ValueLine ctx={ctx.subCtx(f => f.machineName)} />}
      {!ctx.value.isNew && <ValueLine ctx={ctx.subCtx(f => f.applicationName)} />}
      <ValueLine ctx={ctx.subCtx(f => f.suspended)} />
    </div>
  );
}

