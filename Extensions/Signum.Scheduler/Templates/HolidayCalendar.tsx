import * as React from 'react'
import { AutoLine, CheckboxLine, EntityRepeater, EntityTable, TextBoxLine } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { HolidayCalendarEntity, HolidayEmbedded } from '../Signum.Scheduler'
import { useAPI, useForceUpdate } from '../../../Signum/React/Hooks';
import { SchedulerClient } from '../SchedulerClient';
import { JavascriptMessage } from '../../../Signum/React/Signum.Entities';
import { HolidayCalendarClient } from '../HolidayCalendarClient';

export default function HolidayCalendar(p: { ctx: TypeContext<HolidayCalendarEntity> }): React.JSX.Element | string{
  const forceUpdate = useForceUpdate();
  const ctx = p.ctx.subCtx({ formGroupStyle: "Basic" });

  const countries = useAPI(() => HolidayCalendarClient.API.getCountries(), []);

  const subDivisions = useAPI(() => ctx.value.countryCode == null ? Promise.resolve([]) :
    HolidayCalendarClient.API.getSubDivisions(ctx.value.countryCode), [ctx.value.countryCode], { avoidReset: true });

  if (countries === undefined || subDivisions === undefined)
    return JavascriptMessage.loading.niceToString();

  return (
    <div>
      <CheckboxLine ctx={ctx.subCtx(f => f.isDefault)} inlineCheckbox='block' />
      <AutoLine ctx={ctx.subCtx(f => f.name)} helpText={<p>Public holidays will be imported from <a href="https://date.nager.at/">Worldwide Public Holiday</a></p>} />
      <div className="row">
        <div className="col-sm-3">
          <AutoLine ctx={ctx.subCtx(f => f.fromYear)} />
        </div>
        <div className="col-sm-3">
          <AutoLine ctx={ctx.subCtx(f => f.toYear)} />
        </div>
        <div className="col-sm-3">
          <TextBoxLine ctx={ctx.subCtx(f => f.countryCode)} onChange={v => {
            if (ctx.value.countryCode && countries?.contains(ctx.value.countryCode))
              forceUpdate();
            else {
              ctx.value.subDivisionCode = null;
              ctx.value.modified = true;
            }
          }} datalist={countries} />
        </div>
        <div className="col-sm-3">
          <TextBoxLine ctx={ctx.subCtx(f => f.subDivisionCode)} datalist={subDivisions} />
        </div>
      </div>
      <div>
        <EntityTable ctx={ctx.subCtx(f => f.holidays)} avoidFieldSet="h5" columns={[
          { property: a => a.date },
          { property: a => a.name },
        ]} />
      </div>
    </div>
  );
}

