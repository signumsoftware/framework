import * as React from 'react';
import { DateTime } from 'luxon';
import { CalendarProps } from 'react-widgets/cjs/Calendar'
import { DatePicker } from 'react-widgets';
import { addClass, classes } from '../Globals';
import { toLuxonFormat, toFormatWithFixes, dateTimePlaceholder } from '../Reflection';
import { LineBaseController, useController } from '../Lines/LineBase';
import { FormGroup } from '../Lines/FormGroup';
import { FormControlReadonly } from '../Lines/FormControlReadonly';
import { JavascriptMessage } from '../Signum.Entities';
import { ValueBaseProps, ValueBaseController } from './ValueBase';
import { TypeContext } from '../Lines';
import Exception from '../Exceptions/Exception';

export interface DateTimeLineProps extends ValueBaseProps<string | null> {
  showTimeBox?: boolean;
  minDate?: Date;
  maxDate?: Date;
  calendarProps?: Partial<CalendarProps>;
  calendarAlignEnd?: boolean;
}

export class DateTimeLineController extends ValueBaseController<DateTimeLineProps, string | null>{
  init(p: DateTimeLineProps) {
    super.init(p);
    this.assertType("DateTimeLine", ["DateOnly", "DateTime"]);
  }
}

export const DateTimeLine = React.memo(React.forwardRef(function DateTimeLine(props: DateTimeLineProps, ref: React.Ref<DateTimeLineController>) {

    const c = useController(DateTimeLineController, props, ref);

    if (c.isHidden)
        return null;

    const s = c.props;
    const type = c.props.type!.name as "DateOnly" | "DateTime";
    const luxonFormat = toLuxonFormat(s.format, type);

    const m = s.ctx.value ? DateTime.fromISO(s.ctx.value) : undefined;
    const showTime = s.showTimeBox != null ? s.showTimeBox : type != "DateOnly" && luxonFormat != "D" && luxonFormat != "DD" && luxonFormat != "DDD";
    const monthOnly = luxonFormat == "LLLL yyyy";

    if (s.ctx.readOnly)
        return (
            <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...c.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
                {inputId => c.withItemGroup(<FormControlReadonly id={inputId} htmlAttributes={c.props.valueHtmlAttributes} className={addClass(c.props.valueHtmlAttributes, "sf-readonly-date")} ctx={s.ctx} innerRef={c.setRefs}>
                    {m && toFormatWithFixes(m, luxonFormat)}
                </FormControlReadonly>)}
            </FormGroup>
        );

    const handleDatePickerOnChange = (date: Date | null | undefined, str: string) => {

        var m = date && DateTime.fromJSDate(date);

        if (m)
            m = trimDateToFormat(m, type, s.format);

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
        <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...c.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
            {inputId => c.withItemGroup(
                <div className={classes(s.ctx.rwWidgetClass, c.mandatoryClass ? c.mandatoryClass + "-widget" : undefined, s.calendarAlignEnd && "sf-calendar-end")}>
                    <DatePicker
                        id={inputId}
                        value={m?.toJSDate()} onChange={handleDatePickerOnChange} autoFocus={Boolean(c.props.initiallyFocused)}
                        valueEditFormat={luxonFormat}
                        valueDisplayFormat={luxonFormat}
                        includeTime={showTime}
                        inputProps={htmlAttributes as any}
                        placeholder={htmlAttributes.placeholder}
                        messages={{ dateButton: JavascriptMessage.Date.niceToString() }}
                        min={s.minDate}
                        max={s.maxDate}
                        calendarProps={{
                            renderDay: defaultRenderDay,
                            views: monthOnly ? ["year", "decade", "century"] : undefined,
                            ...s.calendarProps
                        }} />
                </div>
            )}
        </FormGroup>
    );
}), (prev, next) => {
    if (next.extraButtons || prev.extraButtons)
        return false;

    return LineBaseController.propEquals(prev, next);
});

export function defaultRenderDay({ date, label }: { date: Date; label: string }) {
  var dateStr = DateTime.fromJSDate(date).toISODate();

  var today = dateStr == DateTime.local().toISODate();

  return <span className={today ? "sf-today" : undefined}>{label}</span>;
}

export function trimDateToFormat(date: DateTime, type: "DateOnly" | "DateTime", format: string | undefined): DateTime {

  const luxonFormat = toLuxonFormat(format, type);

  if (!luxonFormat)
    return date;

  // bug fix with farsi locale : luxon cannot parse Jalaali dates so we force using en-GB for parsing and formatting
  const formatted = date.toFormat(luxonFormat, { locale: 'en-GB' });
  return DateTime.fromFormat(formatted, luxonFormat, { locale: 'en-GB' });
}
