import * as React from 'react'
import { AutoLine, EntityLine, FormGroup } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { HolidayCalendarEntity, SchedulerMessage, ScheduleRuleWeekDaysEntity } from '../Signum.Scheduler'
import { useForceUpdate } from '@framework/Hooks';
import { Finder } from '@framework/Finder';
import { Entity } from '@framework/Signum.Entities';


export default function ScheduleRuleWeekDays(p: { ctx: TypeContext<ScheduleRuleWeekDaysEntity>; }): React.JSX.Element {
  const ctx4 = p.ctx.subCtx({ labelColumns: { sm: 4 } });
  const ctx2 = p.ctx.subCtx({ labelColumns: { sm: 2 } });
  const forceUpdate = useForceUpdate();

  return (
    <div>
      <AutoLine ctx={ctx2.subCtx(f => f.startingOn)} helpText="The hour determines when each execution will occour" />
      <SelectAllCheckbox ctx={ctx2} properties={[
        'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday', 'holiday'
      ]} onChange={forceUpdate} />
      <div className="row">
        <div className="col-sm-6">
          <AutoLine ctx={ctx4.subCtx(f => f.monday)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.tuesday)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.wednesday)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.thursday)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.friday)} onChange={forceUpdate} />
        </div>
        <div className="col-sm-6">
          <AutoLine ctx={ctx4.subCtx(f => f.saturday)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.sunday)} onChange={forceUpdate} />
          <AutoLine ctx={ctx4.subCtx(f => f.holiday)} onChange={forceUpdate} />
          <EntityLine ctx={ctx4.subCtx(f => f.calendar)} onChange={forceUpdate} />
        </div>
      </div>
    </div>
  );
}


interface MultiCheckboxLineProps<T extends Entity> {
  ctx: TypeContext<T>;
  properties: (keyof T)[];
  onChange?: (newValue: boolean) => void;
}

export function SelectAllCheckbox<T extends Entity>({ ctx, properties, onChange }: MultiCheckboxLineProps<T>): React.JSX.Element {

  const entity = ctx.value;
  const forceUpdate = useForceUpdate();
  const selectAllRef = React.useRef<HTMLInputElement>(null);

  const allChecked = properties.every(p => entity[p]);


  const handleSelectAllChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.checked;
    properties.forEach(p => {
      entity[p] = newValue as any;
    });
    onChange?.(newValue);
    forceUpdate();
  };

  return (
    <FormGroup ctx={ctx} label={<strong>{SchedulerMessage.SelectAll.niceToString()}</strong>}>
      {id => <input
        type="checkbox"
        className="form-check-input"
        ref={selectAllRef}
        checked={allChecked}
        onChange={handleSelectAllChange}
      />}
    </FormGroup>
  );
}
