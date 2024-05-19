import * as React from 'react';
import { classes } from '../Globals';
import { timeToString, timePlaceholder, toLuxonDurationFormat } from '../Reflection';
import { LineBaseController, genericForwardRef, useController } from '../Lines/LineBase';
import { FormGroup } from '../Lines/FormGroup';
import { FormControlReadonly } from '../Lines/FormControlReadonly';
import { ValueBaseController, ValueBaseProps } from './ValueBase';
import { Duration } from 'luxon';
import { isNumberKey } from './NumberLine';

export interface TimeLineProps extends ValueBaseProps<string | null> {

}

export class TimeLineController extends ValueBaseController<TimeLineProps, string | null>{
  init(p: TimeLineProps) {
    super.init(p);
    this.assertType("TimeLine", ["TimeOnly", "TimeSpan"]);
  }
}


export const TimeLine = React.memo(React.forwardRef(function TimeLine(props: TimeLineProps, ref: React.Ref<TimeLineController>) {

  const c = useController(TimeLineController, props, ref);

  if (c.isHidden)
    return null;

  const s = c.props;

  if (s.ctx.readOnly) {
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...c.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => c.withItemGroup(
          <FormControlReadonly id={inputId} htmlAttributes={c.props.valueHtmlAttributes} ctx={s.ctx} className={classes(c.props.valueHtmlAttributes?.className, "numeric")} innerRef={c.setRefs}>
            {timeToString(s.ctx.value, s.format)}
          </FormControlReadonly>
        )}
      </FormGroup>
    );
  }

  const handleOnChange = (newValue: string | null) => {
    c.setValue(newValue);
  };

  const htmlAttributes = {
    placeholder: c.getPlaceholder(),
    ...c.props.valueHtmlAttributes
  } as React.AllHTMLAttributes<any>;

  const durationFormat = toLuxonDurationFormat(s.format) ?? "hh:mm:ss";

  if (htmlAttributes.placeholder == undefined)
    htmlAttributes.placeholder = timePlaceholder(durationFormat);

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...c.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => c.withItemGroup(
        <TimeTextBox htmlAttributes={htmlAttributes}
          id={inputId}
          value={s.ctx.value}
          onChange={handleOnChange}
          validateKey={isDurationKey}
          formControlClass={classes(s.ctx.formControlClass, c.mandatoryClass)}
          durationFormat={durationFormat}
          innerRef={c.setRefs} />
      )}
    </FormGroup>
  );
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});



export interface TimeTextBoxProps {
  value: string | null;
  onChange: (newValue: string | null) => void;
  validateKey: (e: React.KeyboardEvent<any>) => boolean;
  formControlClass?: string;
  durationFormat?: string;
  htmlAttributes?: React.InputHTMLAttributes<HTMLInputElement>;
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
    autoComplete="off"
    {...p.htmlAttributes}
    type="text"
    className={classes(p.htmlAttributes?.className, p.formControlClass, "numeric")}
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

export function isDurationKey(e: React.KeyboardEvent<any>): boolean {
  return isNumberKey(e) || e.key == ":";
}
