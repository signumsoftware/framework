import * as React from 'react'
import { AutoLine, EntityLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ScheduleRuleWeekDaysEntity } from '../Signum.Scheduler'
import { useForceUpdate } from '@framework/Hooks';

export default function ScheduleRuleWeekDays(p : { ctx: TypeContext<ScheduleRuleWeekDaysEntity> }){
  const ctx4 = p.ctx.subCtx({ labelColumns: { sm: 4 } });
  const ctx2 = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  const forceUpdate = useForceUpdate();

  return (
    <div>
      <AutoLine ctx={ctx2.subCtx(f => f.startingOn)} helpText="The hour determines when each execution will occour" />
      <div className="row">
        <div className="col-sm-6">
          <AutoLine ctx={ctx4.subCtx(f => f.monday)} />
          <AutoLine ctx={ctx4.subCtx(f => f.tuesday)} />
          <AutoLine ctx={ctx4.subCtx(f => f.wednesday)} />
          <AutoLine ctx={ctx4.subCtx(f => f.thursday)} />
          <AutoLine ctx={ctx4.subCtx(f => f.friday)} />
        </div>
        <div className="col-sm-6">
          <AutoLine ctx={ctx4.subCtx(f => f.saturday)} />
          <AutoLine ctx={ctx4.subCtx(f => f.sunday)} />
          <AutoLine ctx={ctx4.subCtx(f => f.holiday)} onChange={forceUpdate} />
          {ctx4.value.holiday && < EntityLine ctx={ctx4.subCtx(f => f.calendar)} />}
        </div>
      </div>
    </div>
  );
}

