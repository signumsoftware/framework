import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ScheduleRuleMinutelyEntity } from '../Signum.Scheduler'

export default function ScheduleRuleMinutely(p : { ctx: TypeContext<ScheduleRuleMinutelyEntity> }){
  const ctx4 = p.ctx.subCtx({ labelColumns: { sm: 2 } });

  return (
    <div>
      <ValueLine ctx={ctx4.subCtx(f => f.startingOn)} helpText="The hour determines when each execution will occour"/>
      <ValueLine ctx={ctx4.subCtx(f => f.eachMinutes)} />
    </div>
  );
}

