import * as React from 'react';
import { DateTime } from 'luxon';
import { CalendarProps } from 'react-widgets/cjs/Calendar'
import { DatePicker } from 'react-widgets';
import { classes } from '../Globals';
import { toLuxonFormat, toFormatWithFixes, dateTimePlaceholder } from '../Reflection';
import { LineBaseController, useController } from '../Lines/LineBase';
import { FormGroup } from '../Lines/FormGroup';
import { FormControlReadonly } from '../Lines/FormControlReadonly';
import { JavascriptMessage } from '../Signum.Entities';
import { ValueBaseProps, ValueBaseController } from './ValueBase';
import { TypeContext } from '../Lines';
import Exception from '../Exceptions/Exception';
import { RenderDayProp } from 'react-widgets/cjs/Month';

export interface DateTimeLineProps extends ValueBaseProps<string | null> {
  showTimeBox?: boolean;
  minDate?: Date;
  maxDate?: Date;
  calendarProps?: Partial<CalendarProps>;
  calendarAlignEnd?: boolean;
}

export class DateTimeLineController extends ValueBaseController<DateTimeLineProps, string | null>{
  init(p: DateTimeLineProps): void {
    super.init(p);
    this.assertType("DateTimeLine", ["DateOnly", "DateTime","DateTimeOffset"]);
  }
}

export const DateTimeLine: React.MemoExoticComponent<React.ForwardRefExoticComponent<DateTimeLineProps & React.RefAttributes<DateTimeLineController>>> =
  React.memo(React.forwardRef(function DateTimeLine(props: DateTimeLineProps, ref: React.Ref<DateTimeLineController>) {

    const rdat = DateTimeLineOptions.useRenderDay();

    const c = useController(DateTimeLineController, props, ref);

    if (c.isHidden)
        return null;

    const p = c.props;
    const type = c.props.type!.name as "DateOnly" | "DateTime";
    const luxonFormat = toLuxonFormat(p.format, type);

    const dt = p.ctx.value ? DateTime.fromISO(p.ctx.value) : undefined;
    const showTime = p.showTimeBox != null ? p.showTimeBox : type != "DateOnly" && luxonFormat != "D" && luxonFormat != "DD" && luxonFormat != "DDD";
    const monthOnly = luxonFormat == "LLLL yyyy";

    const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
    const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);


    if (p.ctx.readOnly)
      return (
        <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes}>
          {inputId => c.withItemGroup(<FormControlReadonly id={inputId} htmlAttributes={c.props.valueHtmlAttributes} className={classes(c.props.valueHtmlAttributes?.className, "sf-readonly-date", c.mandatoryClass)} ctx={p.ctx} innerRef={c.setRefs}>
            {dt && toFormatWithFixes(dt, luxonFormat)}
          </FormControlReadonly>)}
        </FormGroup>
      );

    const handleDatePickerOnChange = (date: Date | null | undefined, str: string) => {

        var m = date && DateTime.fromJSDate(date);

        if (m)
            m = trimDateToFormat(m, type, p.format);

        // bug fix with farsi locale : luxon cannot parse Jalaali dates so we force using en-GB for parsing and formatting
        c.setValue(m == null || !m.isValid ? null :
            type == "DateOnly" ? m.toISODate() :
                !showTime ? m.startOf("day").toFormat("yyyy-MM-dd'T'HH:mm:ss", { locale: 'en-GB' } /*No Z*/) :
                    m.toISO()!);
    };

    const htmlAttributes = {
        placeholder: c.getPlaceholder(),
        ...c.props.valueHtmlAttributes,
    } as React.AllHTMLAttributes<any>;

    if (htmlAttributes.placeholder === undefined)
        htmlAttributes.placeholder = dateTimePlaceholder(luxonFormat);

    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes}>
        {inputId => c.withItemGroup(
          <div className={classes(p.ctx.rwWidgetClass, c.mandatoryClass ? c.mandatoryClass + "-widget" : undefined, p.calendarAlignEnd && "sf-calendar-end")}>
            <DatePicker
              id={inputId}
              value={dt?.toJSDate()} onChange={handleDatePickerOnChange} autoFocus={Boolean(c.props.initiallyFocused)}
              valueEditFormat={luxonFormat}
              valueDisplayFormat={luxonFormat}
              includeTime={showTime}
              inputProps={htmlAttributes as any}
              placeholder={htmlAttributes.placeholder}
              messages={{ dateButton: JavascriptMessage.Date.niceToString() }}
              min={p.minDate}
              max={p.maxDate}
              calendarProps={{
                renderDay: rdat.renderDay,
                views: monthOnly ? ["year", "decade", "century"] : undefined,
                ...p.calendarProps
              }} />
          </div>
        )}
        </FormGroup>
    );
}), (prev, next) => {
    return LineBaseController.propEquals(prev, next);
  });

export interface RenderDayAndTitle {
  renderDay: RenderDayProp,
  getHolidayTitle: (date: DateTime) => string | "weekend" | null | undefined;
};

export namespace DateTimeLineOptions {


  export let useRenderDay: () => RenderDayAndTitle = () =>
  ({
    renderDay: defaultRenderDay,
    getHolidayTitle: (d) => isWeekend(d) ? d.weekdayLong : undefined,
  });

  export function isWeekend(date: DateTime): boolean {
    return date.weekday == 6 || date.weekday == 7;
  }
}

export function defaultRenderDay({ date, label }: { date: Date; label: string }): React.JSX.Element {

  var dt = DateTime.fromJSDate(date);

  var today = dt.toISODate() == DateTime.local().toISODate();

  return <span className={today ? "sf-today" : DateTimeLineOptions.isWeekend(dt) ? "sf-weekend" : undefined}> {label}</span >;
}

export function trimDateToFormat(date: DateTime, type: "DateOnly" | "DateTime", format: string | undefined): DateTime {

  const luxonFormat = toLuxonFormat(format, type);

  if (!luxonFormat)
    return date;

  // bug fix with farsi locale : luxon cannot parse Jalaali dates so we force using en-GB for parsing and formatting
  const formatted = date.toFormat(luxonFormat, { locale: 'en-GB' });
  return DateTime.fromFormat(formatted, luxonFormat, { locale: 'en-GB' });
}
