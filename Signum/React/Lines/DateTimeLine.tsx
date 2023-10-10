import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { DatePicker, DropdownList, Combobox } from 'react-widgets'
import { CalendarProps } from 'react-widgets/cjs/Calendar'
import { Dic, addClass, classes } from '../Globals'
import { MemberInfo, TypeReference, toLuxonFormat, toNumberFormat, isTypeEnum, timeToString, tryGetTypeInfo, toFormatWithFixes, splitLuxonFormat, dateTimePlaceholder, timePlaceholder, toLuxonDurationFormat } from '../Reflection'
import { LineBaseController, LineBaseProps, tasks, useController } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum, JavascriptMessage } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import { KeyCodes } from '../Components/Basic';
import { getTimeMachineIcon } from './TimeMachineIcon'

export interface DateTimeLineProps extends LineBaseProps {
  format?: string;
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  extraButtons?: (vl: DateTimeLineController) => React.ReactNode;
  initiallyFocused?: boolean | number;

  incrementWithArrow?: boolean | number;

  columnCount?: number;
  columnWidth?: number;

  showTimeBox?: boolean;
  minDate?: Date;
  maxDate?: Date;
  calendarProps?: Partial<CalendarProps>;
  calendarAlignEnd?: boolean;
  initiallyShowOnly?: "Date" | "Time";
  valueRef?: React.Ref<HTMLElement>;
}

export interface OptionItem {
  value: any;
  label: string;
}

export class DateTimeLineController extends LineBaseController<DateTimeLineProps>{

  inputElement!: React.RefObject<HTMLElement>;
  init(p: DateTimeLineProps) {
    super.init(p);

    this.inputElement = React.useRef<HTMLElement>(null);

    React.useEffect(() => {
      if (this.props.initiallyFocused) {
        window.setTimeout(() => {
          let element = this.inputElement.current;
          if (element) {
            if (element instanceof HTMLInputElement)
              element.setSelectionRange(0, element.value.length);
            else if (element instanceof HTMLTextAreaElement)
              element.setSelectionRange(0, element.value.length);
            element.focus();
          }
        }, this.props.initiallyFocused == true ? 0 : this.props.initiallyFocused as number);
      }

    }, []);
  }

  setRefs = (node: HTMLElement | null) => {
    if (this.props?.valueRef) {
      if (typeof this.props.valueRef == "function")
        this.props.valueRef(node);
      else
        (this.props.valueRef as React.MutableRefObject<HTMLElement | null>).current = node;
    }
    (this.inputElement as React.MutableRefObject<HTMLElement | null>).current = node;
  }

  getDefaultProps(state: DateTimeLineProps) {
    super.getDefaultProps(state);
  }

  overrideProps(state: DateTimeLineProps, overridenProps: DateTimeLineProps) {

    const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
    super.overrideProps(state, overridenProps);
    state.valueHtmlAttributes = valueHtmlAttributes;
  }


  withItemGroup(input: JSX.Element, preExtraButton?: JSX.Element): JSX.Element {

    if (!this.props.extraButtons && !preExtraButton) {
      return <>
        {getTimeMachineIcon({ ctx: this.props.ctx })}
        {input}
      </>;
    }

    return (
      <div className={this.props.ctx.inputGroupClass}>
        {getTimeMachineIcon({ ctx: this.props.ctx })}
        {input}
        {preExtraButton}
        {this.props.extraButtons && this.props.extraButtons(this)}
      </div>
    );
  }

  getPlaceholder(): string | undefined {
    const p = this.props;
    return p.valueHtmlAttributes?.placeholder ??
      ((p.ctx.placeholderLabels || p.ctx.formGroupStyle == "FloatingLabel") ? asString(p.label) :
      undefined);
  }
}

function asString(reactChild: React.ReactNode | undefined): string | undefined {
  if (typeof reactChild == "string")
    return reactChild as string;

  return undefined;
}

export const DateTimeLine = React.memo(React.forwardRef(function DateTimeLine(props: DateTimeLineProps, ref: React.Ref<DateTimeLineController>) {

  const c = useController(DateTimeLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalDateTime(c);
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

function internalDateTime(dtlc: DateTimeLineController) {

  const s = dtlc.props;
  const type = dtlc.props.type!.name as "DateOnly" | "DateTime";
  const luxonFormat = toLuxonFormat(s.format, type);

  const m = s.ctx.value ? DateTime.fromISO(s.ctx.value) : undefined;
  const showTime = s.showTimeBox != null ? s.showTimeBox : type != "DateOnly" && luxonFormat != "D" && luxonFormat != "DD" && luxonFormat != "DDD";
  const monthOnly = luxonFormat == "LLLL yyyy";

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...dtlc.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => dtlc.withItemGroup(<FormControlReadonly id={inputId} htmlAttributes={dtlc.props.valueHtmlAttributes} className={addClass(dtlc.props.valueHtmlAttributes, "sf-readonly-date")} ctx={s.ctx} innerRef={dtlc.setRefs}>
          {m && toFormatWithFixes(m, luxonFormat)}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleDatePickerOnChange = (date: Date | null | undefined, str: string) => {

    var m = date && DateTime.fromJSDate(date);

    if (m)
      m = trimDateToFormat(m, type, s.format);

    // bug fix with farsi locale : luxon cannot parse Jalaali dates so we force using en-GB for parsing and formatting
    dtlc.setValue(m == null || !m.isValid ? null :
      type == "DateOnly" ? m.toISODate() :
        !showTime ? m.startOf("day").toFormat("yyyy-MM-dd'T'HH:mm:ss", { locale: 'en-GB' }/*No Z*/) :
          m.toISO()!);
  };

  const htmlAttributes = {
    placeholder: dtlc.getPlaceholder(),
    ...dtlc.props.valueHtmlAttributes,
  } as React.AllHTMLAttributes<any>;

  if (htmlAttributes.placeholder === undefined)
    htmlAttributes.placeholder = dateTimePlaceholder(luxonFormat);

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...dtlc.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => dtlc.withItemGroup(
        <div className={classes(s.ctx.rwWidgetClass, dtlc.mandatoryClass ? dtlc.mandatoryClass + "-widget" : undefined, s.calendarAlignEnd && "sf-calendar-end")}>
          <DatePicker
            id={inputId}
            value={m?.toJSDate()} onChange={handleDatePickerOnChange} autoFocus={Boolean(dtlc.props.initiallyFocused)}
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
            }}
          />
        </div>
      )}
    </FormGroup>
  );
}

function defaultRenderDay({ date, label }: { date: Date; label: string }) {
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


function internalDateTimeSplitted(dtlc: DateTimeLineController) {

  const s = dtlc.props;
  const type = dtlc.props.type!.name as "DateOnly" | "DateTime";
  const luxonFormat = toLuxonFormat(s.format, type);

  const dt = s.ctx.value ? DateTime.fromISO(s.ctx.value) : undefined;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...dtlc.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => dtlc.withItemGroup(<FormControlReadonly id={inputId} htmlAttributes={dtlc.props.valueHtmlAttributes} className={addClass(dtlc.props.valueHtmlAttributes, "sf-readonly-date")} ctx={s.ctx} innerRef={dtlc.setRefs}>
          {dt && toFormatWithFixes(dt, luxonFormat)}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleDatePickerOnChange = (date: Date | null | undefined) => {

    var newDT = date && DateTime.fromJSDate(date);

    if (newDT)
      newDT = trimDateToFormat(newDT, type, s.format);

    // bug fix with farsi locale : luxon cannot parse Jalaali dates so we force using en-GB for parsing and formatting
    dtlc.setValue(newDT == null || !newDT.isValid ? null : newDT.toISO()!);
  };

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...dtlc.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => dtlc.withItemGroup(
        <DateTimePickerSplitted value={dt?.toJSDate()} onChange={handleDatePickerOnChange}
          id={inputId}
          initiallyFocused={Boolean(dtlc.props.initiallyFocused)}
          initiallyShowOnly={dtlc.props.initiallyShowOnly}
          luxonFormat={luxonFormat}
          minDate={s.minDate}
          maxDate={s.maxDate}
          mandatoryClass={dtlc.mandatoryClass}
          timeTextBoxClass={s.ctx.formControlClass}
          htmlAttributes={s.valueHtmlAttributes}
          widgetClass={s.ctx.rwWidgetClass}
          calendarProps={{
            renderDay: defaultRenderDay,
            ...s.calendarProps
          }}
        />
      )}
    </FormGroup>
  );
}

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
            validateKey={isDuration}
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

export function isNumber(e: React.KeyboardEvent<any>) {
  const c = e.keyCode;
  return ((c >= 48 && c <= 57 && !e.shiftKey) /*0-9*/ ||
    (c >= 96 && c <= 105) /*NumPad 0-9*/ ||
    (c == KeyCodes.enter) ||
    (c == KeyCodes.backspace) ||
    (c == KeyCodes.tab) ||
    (c == KeyCodes.clear) ||
    (c == KeyCodes.esc) ||
    (c == KeyCodes.left) ||
    (c == KeyCodes.right) ||
    (c == KeyCodes.up) ||
    (c == KeyCodes.down) ||
    (c == KeyCodes.delete) ||
    (c == KeyCodes.home) ||
    (c == KeyCodes.end) ||
    (c == KeyCodes.numpadMinus) /*NumPad -*/ ||
    (c == KeyCodes.minus) /*-*/ ||
    (e.ctrlKey && c == 86) /*Ctrl + v*/ ||
    (e.ctrlKey && c == 88) /*Ctrl + x*/ ||
    (e.ctrlKey && c == 67) /*Ctrl + c*/);
}

function isDuration(e: React.KeyboardEvent<any>): boolean {
  const c = e.keyCode;
  return isNumber(e) || e.key == ":";
}

function timeTextBox(vl: DateTimeLineController, validateKey: (e: React.KeyboardEvent<any>) => boolean) {

  const s = vl.props;

  if (s.ctx.readOnly) {
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => vl.withItemGroup(
          <FormControlReadonly id={inputId} htmlAttributes={vl.props.valueHtmlAttributes} ctx={s.ctx} className={addClass(vl.props.valueHtmlAttributes, "numeric")} innerRef={vl.setRefs}>
            {timeToString(s.ctx.value, s.format)}
          </FormControlReadonly>
        )}
      </FormGroup>
    );
  }

  const handleOnChange = (newValue: string | null) => {
    vl.setValue(newValue);
  };

  const htmlAttributes = {
    placeholder: vl.getPlaceholder(),
    ...vl.props.valueHtmlAttributes
  } as React.AllHTMLAttributes<any>;

  const durationFormat = toLuxonDurationFormat(s.format) ?? "hh:mm:ss"

  if (htmlAttributes.placeholder == undefined)
    htmlAttributes.placeholder = timePlaceholder(durationFormat);

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => vl.withItemGroup(
        <TimeTextBox htmlAttributes={htmlAttributes}
          id={inputId}
          value={s.ctx.value}
          onChange={handleOnChange}
          validateKey={validateKey}
          formControlClass={classes(s.ctx.formControlClass, vl.mandatoryClass)}
          durationFormat={durationFormat}
          innerRef={vl.setRefs} />
      )}
    </FormGroup>
  );
}

export interface TimeTextBoxProps {
  value: string | null;
  onChange: (newValue: string | null) => void;
  validateKey: (e: React.KeyboardEvent<any>) => boolean;
  formControlClass?: string;
  durationFormat?: string;
  htmlAttributes?: React.HTMLAttributes<HTMLInputElement>;
  innerRef?: React.Ref<HTMLInputElement>;
  id?: string;
}

export function TimeTextBox(p: TimeTextBoxProps) {

  const [text, setText] = React.useState<string | undefined>(undefined);

  const value = text != undefined ? text :
    p.value != undefined ? Duration.fromISOTime(p.value).toFormat(p.durationFormat!) :
      "";

  return <input
    id={p.id}
    ref={p.innerRef}
    {...p.htmlAttributes}
    type="text"
    autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
    className={addClass(p.htmlAttributes, classes(p.formControlClass, "numeric"))}
    value={value}
    onBlur={handleOnBlur}
    onChange={handleOnChange}
    onKeyDown={handleKeyDown}
    onFocus={handleOnFocus} />


  function handleOnFocus(e: React.FocusEvent<any>) {
    const input = e.currentTarget as HTMLInputElement;

    input.setSelectionRange(0, input.value != null ? input.value.length : 0);

    if (p.htmlAttributes && p.htmlAttributes.onFocus)
      p.htmlAttributes.onFocus(e);
  };


  function handleOnBlur(e: React.FocusEvent<any>) {

    const input = e.currentTarget as HTMLInputElement;

    var duration = input.value == undefined || input.value.length == 0 ? null : Duration.fromISOTime(fixCasual(input.value));

    const result = duration && duration.toISOTime();
    setText(undefined);
    if (p.value != result)
      p.onChange(result);
    if (p.htmlAttributes && p.htmlAttributes.onBlur)
      p.htmlAttributes.onBlur(e);
  }

  function handleOnChange(e: React.SyntheticEvent<any>) {
    const input = e.currentTarget as HTMLInputElement;
    setText(input.value);
  }

  function handleKeyDown(e: React.KeyboardEvent<any>) {
    if (!p.validateKey(e))
      e.preventDefault();
  }

  function fixCasual(val: string) {

    if (val.contains(":"))
      return val.split(":").map(a => a.padStart(2, "0")).join(":");

    if (val.length == 1)
      return "0" + val + "00";

    if (val.length == 2)
      return val + "00";

    if (val.length == 3)
      return "0" + val;

    return val;
  }

}

TimeTextBox.defaultProps = {
  durationFormat: "hh:mm:ss"
};
