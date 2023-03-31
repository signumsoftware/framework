import * as React from 'react'
import { ValueLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ScheduleRuleWeekDaysEntity } from '../Signum.Scheduler'
import { useForceUpdate } from '@framework/Hooks';

export default function ScheduleRuleWeekDays(p : { ctx: TypeContext<ScheduleRuleWeekDaysEntity> }){
  const ctx4 = p.ctx.subCtx({ labelColumns: { sm: 4 } });
  const ctx2 = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  const forceUpdate = useForceUpdate();

  return (
    <div>
      <ValueLine ctx={ctx2.subCtx(f => f.startingOn)} helpText="The hour determines when each execution will occour" />
      <div className="row">
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(f => f.monday)} />
          <ValueLine ctx={ctx4.subCtx(f => f.tuesday)} />
          <ValueLine ctx={ctx4.subCtx(f => f.wednesday)} />
          <ValueLine ctx={ctx4.subCtx(f => f.thursday)} />
          <ValueLine ctx={ctx4.subCtx(f => f.friday)} />
        </div>
        <div className="col-sm-6">
          <ValueLine ctx={ctx4.subCtx(f => f.saturday)} />
          <ValueLine ctx={ctx4.subCtx(f => f.sunday)} />
          <ValueLine ctx={ctx4.subCtx(f => f.holiday)} onChange={forceUpdate} />
          {ctx4.value.holiday && < EntityLine ctx={ctx4.subCtx(f => f.calendar)} />}
        </div>
      </div>
    </div>
  );
}

