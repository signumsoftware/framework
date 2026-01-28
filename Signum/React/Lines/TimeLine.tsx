import * as React from 'react';
import { classes } from '../Globals';
import { timeToString, timePlaceholder, toLuxonDurationFormat } from '../Reflection';
import { genericMemo, LineBaseController, useController } from '../Lines/LineBase';
import { FormGroup } from '../Lines/FormGroup';
import { FormControlReadonly } from '../Lines/FormControlReadonly';
import { ValueBaseController, ValueBaseProps } from './ValueBase';
import { Duration } from 'luxon';
import { isNumberKey } from './NumberLine';

export interface TimeLineProps extends ValueBaseProps<string | null> {
  ref?: React.Ref<TimeLineController>
}

export class TimeLineController extends ValueBaseController<TimeLineProps, string | null> {
  override init(p: TimeLineProps): void {
    super.init(p);
    this.assertType("TimeLine", ["TimeOnly", "TimeSpan"]);
  }
}


export const TimeLine: (props: TimeLineProps) => React.ReactNode | null =
  genericMemo(function TimeLine(props: TimeLineProps) {

    const c = useController(TimeLineController, props);

    if (c.isHidden)
      return null;

    const p = c.props;

    const isLabelVisible = !(p.ctx.formGroupStyle === "SrOnly" || "visually-hidden");
    var ariaAtts = p.ctx.readOnly ? c.baseAriaAttributes() : c.extendedAriaAttributes();
    if (!isLabelVisible && p.label) {
      ariaAtts = { ...ariaAtts, "aria-label": typeof p.label === "string" ? p.label : String(p.label) };
    }

    var htmlAtts = c.props.valueHtmlAttributes;
    var mergedHtmlReadOnly = { ...htmlAtts, ...ariaAtts };

    const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
    const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

    if (p.ctx.readOnly) {
      return (
        <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
          {inputId => c.withItemGroup(
            <FormControlReadonly id={inputId} htmlAttributes={mergedHtmlReadOnly} ctx={p.ctx} className={classes(c.props.valueHtmlAttributes?.className, "numeric")} innerRef={c.setRefs}>
              {timeToString(p.ctx.value, p.format)}
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
    var mergedHtml = { ...htmlAttributes, ...ariaAtts };

    const durationFormat = toLuxonDurationFormat(p.format) ?? "hh:mm:ss";

    if (htmlAttributes.placeholder == undefined)
      htmlAttributes.placeholder = timePlaceholder(durationFormat);

    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes} ariaAttributes={ariaAtts}>
        {inputId => c.withItemGroup(
          <TimeTextBox htmlAttributes={mergedHtml}
            id={inputId}
            value={p.ctx.value}
            onChange={handleOnChange}
            validateKey={isDurationKey}
            formControlClass={classes(p.ctx.formControlClass, c.mandatoryClass)}
            durationFormat={durationFormat}
            innerRef={c.setRefs} />
        )}
      </FormGroup>
    );
  }, (prev, next) => {
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

export function TimeTextBox(p: TimeTextBoxProps): React.ReactElement {

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

export namespace TimeTextBox {
  export const defaultProps = {
    durationFormat: "hh:mm:ss"
  };
}

export function isDurationKey(e: React.KeyboardEvent<any>): boolean {
  return isNumberKey(e) || e.key == ":";
}
