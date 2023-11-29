import * as React from 'react'
import { AutoLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ScheduleRuleMonthsEntity } from '../Signum.Scheduler'

export default function ScheduleRuleMonths(p : { ctx: TypeContext<ScheduleRuleMonthsEntity> }){
  const ctx4 = p.ctx.subCtx({ labelColumns: { sm: 4 } });
  const ctx2 = p.ctx.subCtx({ labelColumns: { sm: 2 } });

  return (
    <div>
      <AutoLine ctx={ctx2.subCtx(f => f.startingOn)} helpText="The hour determines when each execution will occour" />
      <div className="row">
        <div className="col-sm-3">
          <AutoLine ctx={ctx4.subCtx(f => f.january)} />
          <AutoLine ctx={ctx4.subCtx(f => f.february)} />
          <AutoLine ctx={ctx4.subCtx(f => f.march)} />
        </div>
        <div className="col-sm-3">
          <AutoLine ctx={ctx4.subCtx(f => f.april)} />
          <AutoLine ctx={ctx4.subCtx(f => f.may)} />
          <AutoLine ctx={ctx4.subCtx(f => f.june)} />
        </div>
        <div className="col-sm-3">
          <AutoLine ctx={ctx4.subCtx(f => f.july)} />
          <AutoLine ctx={ctx4.subCtx(f => f.august)} />
          <AutoLine ctx={ctx4.subCtx(f => f.september)} />
        </div>
        <div className="col-sm-3">
          <AutoLine ctx={ctx4.subCtx(f => f.october)} />
          <AutoLine ctx={ctx4.subCtx(f => f.november)} />
          <AutoLine ctx={ctx4.subCtx(f => f.december)} />
        </div>
      </div>
    </div>
  );
}

