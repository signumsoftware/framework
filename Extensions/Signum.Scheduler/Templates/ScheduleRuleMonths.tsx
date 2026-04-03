import * as React from 'react'
import { AutoLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { ScheduleRuleMonthsEntity } from '../Signum.Scheduler'
import { SelectAllCheckbox } from './ScheduleRuleWeekDays';
import { useForceUpdate } from '@framework/Hooks';

export default function ScheduleRuleMonths(p : { ctx: TypeContext<ScheduleRuleMonthsEntity> }): React.JSX.Element {
  const ctx4 = p.ctx.subCtx({ labelColumns: { sm: 4 } });
  const ctx2 = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  const forceUpdate = useForceUpdate();

  return (
    <div>
      <AutoLine ctx={ctx2.subCtx(f => f.startingOn)} helpText="The hour determines when each execution will occour" />
    

      <div className="row">
        <div className="col-sm-6">
          <SelectAllCheckbox ctx={ctx2} properties={[
            'january', 'february', 'march',
            'april', 'may', 'june',
            'july', 'august', 'september',
            'october', 'november', 'december'
          ]} onChange={forceUpdate} />
        </div>
      </div>

      <div className="row">
        <div className="col-sm-3">
          <AutoLine ctx={ctx4.subCtx(f => f.january)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.february)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.march)} onChange={forceUpdate} />
        </div>
        <div className="col-sm-3">
          <AutoLine ctx={ctx4.subCtx(f => f.april)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.may)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.june)} onChange={forceUpdate} />
        </div>
        <div className="col-sm-3">
          <AutoLine ctx={ctx4.subCtx(f => f.july)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.august)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.september)} onChange={forceUpdate} />
        </div>
        <div className="col-sm-3">
          <AutoLine ctx={ctx4.subCtx(f => f.october)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.november)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.december)} onChange={forceUpdate} />
        </div>
      </div>
    </div>
  );
}

