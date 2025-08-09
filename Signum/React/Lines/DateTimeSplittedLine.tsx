import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { CalendarProps } from 'react-widgets-up/Calendar'
import { DatePicker, DropdownList, Combobox } from 'react-widgets-up'
import { classes } from '../Globals'
import { MemberInfo, TypeReference, toLuxonFormat, toNumberFormat, isTypeEnum, tryGetTypeInfo, toFormatWithFixes, splitLuxonFormat, dateTimePlaceholder, timePlaceholder } from '../Reflection'
import { genericMemo, LineBaseController, LineBaseProps, tasks, useController } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum, JavascriptMessage } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import { ValueBaseController, ValueBaseProps } from './ValueBase'
import { defaultRenderDay, trimDateToFormat } from './DateTimeLine'
import { TimeTextBox, isDurationKey } from './TimeLine'
import { TypeContext } from '../TypeContext'

export interface DateTimeSplittedLineProps extends ValueBaseProps<string /*Date or DateTime*/ | null> {
  minDate?: Date;
  maxDate?: Date;
  calendarProps?: Partial<CalendarProps>;
  initiallyShowOnly?: "Date" | "Time";
  ref?: React.Ref<DateTimeSplittedLineController>;
}

export class DateTimeSplittedLineController extends ValueBaseController<DateTimeSplittedLineProps, string /*Date or DateTime*/ | null >{
  init(p: DateTimeSplittedLineProps): void {
    super.init(p);
    this.assertType("DateTimeSplittedLine", ["DateOnly", "DateTime"]);
  }
}


export const DateTimeSplittedLine: (props: DateTimeSplittedLineProps) => React.ReactNode | null =
  genericMemo(function DateTimeSplittedLine(props: DateTimeSplittedLineProps) {

  const c = useController(DateTimeSplittedLineController, props);

  if (c.isHidden)
    return null;

  const p = c.props;
  const type = c.props.type!.name as "DateOnly" | "DateTime";
  const luxonFormat = toLuxonFormat(p.format, type);

  const dt = p.ctx.value ? DateTime.fromISO(p.ctx.value) : undefined;

  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

  if (p.ctx.readOnly)
    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes}>
        {inputId => c.withItemGroup(<FormControlReadonly id={inputId} htmlAttributes={c.props.valueHtmlAttributes} className={classes(c.props.valueHtmlAttributes?.className, "sf-readonly-date")} ctx={p.ctx} innerRef={c.setRefs}>
          {dt && toFormatWithFixes(dt, luxonFormat)}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleDatePickerOnChange = (date: Date | null | undefined) => {

    var newDT = date && DateTime.fromJSDate(date);

    if (newDT)
      newDT = trimDateToFormat(newDT, type, p.format);

    // bug fix with farsi locale : luxon cannot parse Jalaali dates so we force using en-GB for parsing and formatting
    c.setValue(newDT == null || !newDT.isValid ? null : newDT.toISO()!);
  };

  return (
    <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes}>
      {inputId => c.withItemGroup(
        <DateTimePickerSplitted value={dt?.toJSDate()} onChange={handleDatePickerOnChange}
          id={inputId}
          initiallyFocused={Boolean(c.props.initiallyFocused)}
          initiallyShowOnly={c.props.initiallyShowOnly}
          luxonFormat={luxonFormat}
          minDate={p.minDate}
          maxDate={p.maxDate}
          mandatoryClass={c.mandatoryClass}
          timeTextBoxClass={p.ctx.formControlClass}
          htmlAttributes={p.valueHtmlAttributes}
          widgetClass={p.ctx.rwWidgetClass}
          calendarProps={{
            renderDay: defaultRenderDay,
            ...p.calendarProps
          }}
        />
      )}
    </FormGroup>
  );
}, (prev, next) => {
  return LineBaseController.propEquals(prev, next);
});

function DateTimePickerSplitted(p: {
  value: Date | null | undefined;
  onChange: (newDateTime: Date | null | undefined) => void,
  luxonFormat: string,
  htmlAttributes?: React.AllHTMLAttributes<HTMLInputElement>,
  mandatoryClass?: string | null,
  widgetClass?: string
  timeTextBoxClass?: string;
  minDate?: Date,
  maxDate?: Date,
  initiallyFocused?: boolean,
  calendarProps?: Partial<CalendarProps>;
  initiallyShowOnly?: "Date" | "Time";
  id: string;
}) {

  const [dateFormat, timeFormat] = splitLuxonFormat(p.luxonFormat);

  const [temp, setTemp] = React.useState<{ type: "Date", date: string } | { type: "Time", time: string } | null>(() => {
    if (p.initiallyShowOnly == null || p.value == null)
      return null;

    if (p.initiallyShowOnly == "Date")
      return ({ type: "Date", date: DateTime.fromJSDate(p.value).toISODate()! });

    if (p.initiallyShowOnly == "Time")
      return ({ type: "Time", time: getTimeOfDay(DateTime.fromJSDate(p.value)).toISOTime()! });

    return null;
  });

  function handleTimeChange(time: string | null) {
    if (time == null) {
      if (p.value != null && temp == null) {
        setTemp({ type: "Date", date: DateTime.fromJSDate(p.value).startOf("day").toISODate()! });
      } else if (temp?.type == "Time") {
        setTemp(null);
      }
    } else {
      if (p.value != null) {
        p.onChange(DateTime.fromJSDate(p.value).startOf("day").plus(Duration.fromISOTime(time)).toJSDate());
        setTemp(null);
      } else if (temp?.type == "Date") {
        p.onChange(DateTime.fromISO(temp.date).plus(Duration.fromISOTime(time)).toJSDate());
        setTemp(null);
      } else {
        setTemp({ type: "Time", time: time });
      }
    }
  }

  function handleDateChange(date: Date | null | undefined) {
    if (date == null) {
      if (p.value != null && temp == null) {
        p.onChange(null);
        setTemp({ type: "Time", time: getTimeOfDay(DateTime.fromJSDate(p.value)).toISOTime()! });
      } else if (temp?.type == "Date") {
        p.onChange(null);
        setTemp(null);
      }
    } else {
      if (p.value != null) {
        p.onChange(DateTime.fromJSDate(date).startOf("day").plus(getTimeOfDay(DateTime.fromJSDate(p.value))).toJSDate());
        setTemp(null);
      } else if (temp?.type == "Time") {
        p.onChange(DateTime.fromJSDate(date).startOf("day").plus(Duration.fromISOTime(temp.time)).toJSDate());
        setTemp(null);
      } else {
        setTemp({ type: "Date", date: DateTime.fromJSDate(date).toISODate()! });
      }
    }
  }

  function getTimeOfDay(dt: DateTime): Duration {
    return dt.diff(dt.startOf("day"));
  }

  return (
    <div className="d-flex">
      <div style={{ flex: 2 }} className={classes(p.widgetClass, temp?.type == "Time" ? "sf-mandatory-widget" : p.mandatoryClass ? p.mandatoryClass + "-widget" : null, "pe-1")}>
        <DatePicker
          value={temp == null ? (p.value ? DateTime.fromJSDate(p.value).startOf("day").toJSDate() : null) :
            (temp?.type == "Date" ? DateTime.fromISO(temp.date).toJSDate() : null)}
          onChange={handleDateChange}
          autoFocus={Boolean(p.initiallyFocused)}
          valueEditFormat={dateFormat}
          valueDisplayFormat={dateFormat}
          includeTime={false}
          inputProps={p.htmlAttributes as any}
          placeholder={(p.htmlAttributes?.placeholder ?? dateTimePlaceholder(dateFormat))}
          messages={{ dateButton: JavascriptMessage.Date.niceToString() }}
          min={p.minDate}
          max={p.maxDate}
          calendarProps={{
            renderDay: defaultRenderDay,
            ...p.calendarProps
          }}
        />
      </div>
      <div style={{ flex: 1 }}>
        {timeFormat == null ?
          <span className="text-danger">Error: No timeFormat in {p.luxonFormat}</span> :
          <TimeTextBox
            value={temp == null ?
              (p.value ? getTimeOfDay(DateTime.fromJSDate(p.value))?.toISOTime() : null) :
              (temp.type == "Time" ? temp.time : null)}
            onChange={handleTimeChange}
            validateKey={isDurationKey}
            htmlAttributes={{
              ...p.htmlAttributes,
              placeholder: timePlaceholder(timeFormat),
            }}
            formControlClass={classes(p.timeTextBoxClass, temp?.type == "Date" ? "sf-mandatory" : p.mandatoryClass)}
            durationFormat={timeFormat!} />
        }
      </div>
    </div>
  );
}



