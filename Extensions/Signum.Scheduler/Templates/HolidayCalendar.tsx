import * as React from 'react'
import { AutoLine, EntityRepeater, EntityTable } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { HolidayCalendarEntity, HolidayEmbedded } from '../Signum.Scheduler'

export default function HolidayCalendar(p : { ctx: TypeContext<HolidayCalendarEntity> }): React.JSX.Element {
  const e = p.ctx;

  return (
    <div>
      <AutoLine ctx={e.subCtx(f => f.name)} />
      <div>
        <EntityTable ctx={e.subCtx(f => f.holidays)} columns={[
          { property: a => a.date },
          { property: a => a.name },
        ]} />
      </div>
    </div>
  );
}

