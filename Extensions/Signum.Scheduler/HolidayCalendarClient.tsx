import { RouteObject } from 'react-router'
import { ajaxGet } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { toFormatWithFixes } from '@framework/Reflection'
import { Finder } from '@framework/Finder'
import { HolidayCalendarEntity, SchedulerMessage } from './Signum.Scheduler'
import { Lite } from '../../Signum/React/Signum.Entities';
import { RenderDayProp } from 'react-widgets-up/Month';
import { DateTime } from 'luxon';
import { DateTimeLineOptions, RenderDayAndTitle } from '../../Signum/React/Lines/DateTimeLine';
import React from 'react';
import { useAPI } from '../../Signum/React/Hooks';

export namespace HolidayCalendarClient {

  export function start(options: { routes: RouteObject[] }): void {

    Navigator.addSettings(new EntitySettings(HolidayCalendarEntity, e => import('./Templates/HolidayCalendar')));

    if (Navigator.isViewable(HolidayCalendarEntity))
      DateTimeLineOptions.useRenderDay = HolidayCalendarClient.useRenderHoliday;
  }

  export type CalendarDictionary = { [key: string]: string };

  let holidayCalendars: { [id: string]: Promise<CalendarDictionary> } = {};
  export function getHolidayCalendar(lite: Lite<HolidayCalendarEntity>): Promise<CalendarDictionary> {

    var id = lite.id!.toString()!;
    return holidayCalendars[id] ??= Navigator.API.fetchEntity(HolidayCalendarEntity, id)
      .then(hc => hc.holidays.toObject(d => d.element.date, d => d.element.name ?? SchedulerMessage.Holiday.niceToString()))
      .catch(e => { delete holidayCalendars[id]; throw e; });
  }

  let defaultHolidayCalendar: Promise<CalendarDictionary | null> | undefined = undefined;
  export function getDefaultHolidayCalendar(): Promise<CalendarDictionary | null> | undefined {

    return defaultHolidayCalendar ??= Finder.fetchEntities({
      queryName: HolidayCalendarEntity,
      filterOptions: [{ token: HolidayCalendarEntity.token(hc => hc.isDefault), value: true }]
    }).then(list => list.singleOrNull()?.holidays.toObject(d => d.element.date, d => d.element.name ?? SchedulerMessage.Holiday.niceToString()) ?? null)
      .catch(e => { defaultHolidayCalendar = undefined; throw e; });
  }

  export function useRenderHoliday(): RenderDayAndTitle {
    const calendar = useAPI(() => getDefaultHolidayCalendar(), []);
    return getRenderDayAndTitle(calendar)
  }

  export function getRenderDayAndTitle(calendar: CalendarDictionary | null | undefined): RenderDayAndTitle {
    return {
      getHolidayTitle: (d) => calendar?.[d.toISODate()!] ? { type: "holiday", text: calendar[d.toISODate()!] } :
        DateTimeLineOptions.isWeekend(d) ? { type: "weekend", text: d.weekdayLong! } : undefined,

      renderDay: ({ date, label }: { date: Date; label: string; }) => {
        var dt = DateTime.fromJSDate(date);
        var today = dt.toISODate() == DateTime.local().toISODate();
        var holiday = calendar?.[dt.toISODate()!];
        return <span className={today ? "sf-today" : DateTimeLineOptions.isWeekend(dt) ? "sf-weekend" : holiday ? "sf-holiday" : undefined}
          title={holiday ? `${toFormatWithFixes(DateTime.fromJSDate(date), "D")} (${holiday})` : undefined}
        > {label}</span>;
      },
    };
  }

  export namespace API {
    export function getCountries(): Promise<string[]> {
      return ajaxGet({ url: "/api/holidaycalendar/countries" });
    }

    export function getSubDivisions(country: string): Promise<string[]> {
      return ajaxGet({ url: "/api/holidaycalendar/subDivisions/" + country });
    }
  }
}
