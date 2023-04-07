import * as React from 'react'
import { ValueLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ScheduleRuleMonthsEntity } from '../Signum.Scheduler'

export default function ScheduleRuleMonths(p : { ctx: TypeContext<ScheduleRuleMonthsEntity> }){
  const ctx4 = p.ctx.subCtx({ labelColumns: { sm: 4 } });
  const ctx2 = p.ctx.subCtx({ labelColumns: { sm: 2 } });

  return (
    <div>
      <ValueLine ctx={ctx2.subCtx(f => f.startingOn)} helpText="The hour determines when each execution will occour" />
      <div className="row">
        <div className="col-sm-3">
          <ValueLine ctx={ctx4.subCtx(f => f.january)} />
          <ValueLine ctx={ctx4.subCtx(f => f.february)} />
          <ValueLine ctx={ctx4.subCtx(f => f.march)} />
        </div>
        <div className="col-sm-3">
          <ValueLine ctx={ctx4.subCtx(f => f.april)} />
          <ValueLine ctx={ctx4.subCtx(f => f.may)} />
          <ValueLine ctx={ctx4.subCtx(f => f.june)} />
        </div>
        <div className="col-sm-3">
          <ValueLine ctx={ctx4.subCtx(f => f.july)} />
          <ValueLine ctx={ctx4.subCtx(f => f.august)} />
          <ValueLine ctx={ctx4.subCtx(f => f.september)} />
        </div>
        <div className="col-sm-3">
          <ValueLine ctx={ctx4.subCtx(f => f.october)} />
          <ValueLine ctx={ctx4.subCtx(f => f.november)} />
          <ValueLine ctx={ctx4.subCtx(f => f.december)} />
        </div>
      </div>
    </div>
  );
}

