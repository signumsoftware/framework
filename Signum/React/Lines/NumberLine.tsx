import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { DatePicker, DropdownList, Combobox } from 'react-widgets'
import { CalendarProps } from 'react-widgets/cjs/Calendar'
import { Dic, addClass, classes, isNumber } from '../Globals'
import { MemberInfo, TypeReference, toLuxonFormat, toNumberFormat, isTypeEnum, timeToString, tryGetTypeInfo, toFormatWithFixes, splitLuxonFormat, dateTimePlaceholder, timePlaceholder, toLuxonDurationFormat } from '../Reflection'
import { LineBaseController, LineBaseProps, isDecimal, setRefProp, tasks, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum, JavascriptMessage } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import { KeyCodes } from '../Components/Basic';
import { getTimeMachineIcon } from './TimeMachineIcon'
import { ValueBaseController, ValueBaseProps } from './ValueBase'

export interface NumberLineProps extends ValueBaseProps<NumberLineController> {
  incrementWithArrow?: boolean | number;
}

export class NumberLineController extends ValueBaseController<NumberLineProps>{
}

export const NumberLine = React.memo(React.forwardRef(function NumberLine(props: NumberLineProps, ref: React.Ref<NumberLineController>) {

  const c = useController(NumberLineController, props, ref);

  if (c.isHidden)
    return null;

  return numericTextBox(c, isNumber);
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export const DecimalLine = React.memo(React.forwardRef(function DecimalLine(props: NumberLineProps, ref: React.Ref<NumberLineController>) {

  const c = useController(NumberLineController, props, ref);

  if (c.isHidden)
    return null;

  return numericTextBox(c, isDecimal);
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

function numericTextBox(vl: NumberLineController, validateKey: (e: React.KeyboardEvent<any>) => boolean) {
  const s = vl.props

  const numberFormat = toNumberFormat(s.format);

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => vl.withItemGroup(
          <FormControlReadonly id={inputId} htmlAttributes={vl.props.valueHtmlAttributes} ctx={s.ctx} className="numeric" innerRef={vl.setRefs}>
            {s.ctx.value == null ? "" : numberFormat.format(s.ctx.value)}
          </FormControlReadonly>)}
      </FormGroup>
    );

  const handleOnChange = (newValue: number | null) => {
    vl.setValue(newValue);
  };

  var incNumber = typeof vl.props.incrementWithArrow == "number" ? vl.props.incrementWithArrow : 1;

  const handleKeyDown = (e: React.KeyboardEvent<any>) => {
    if (e.keyCode == KeyCodes.down) {
      e.preventDefault();
      vl.setValue((s.ctx.value ?? 0) - incNumber, e);
    } else if (e.keyCode == KeyCodes.up) {
      e.preventDefault();
      vl.setValue((s.ctx.value ?? 0) + incNumber, e);
    }
  }

  const htmlAttributes = {
    placeholder: vl.getPlaceholder(),
    onKeyDown: (vl.props.incrementWithArrow || vl.props.incrementWithArrow == undefined ) ? handleKeyDown : undefined,
    ...vl.props.valueHtmlAttributes
  } as React.AllHTMLAttributes<any>;

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => vl.withItemGroup(
        <NumericTextBox
          id={inputId }
          htmlAttributes={htmlAttributes}
          value={s.ctx.value}
          onChange={handleOnChange}
          formControlClass={classes(s.ctx.formControlClass, vl.mandatoryClass)}
          validateKey={validateKey}
          format={numberFormat}
          innerRef={vl.setRefs}
        />
      )}
    </FormGroup>
  );
}

export interface NumericTextBoxProps {
  value: number | null;
  readonly?: boolean;
  onChange: (newValue: number | null) => void;
  validateKey: (e: React.KeyboardEvent<any>) => boolean;
  format: Intl.NumberFormat;
  formControlClass?: string;
  htmlAttributes?: React.HTMLAttributes<HTMLInputElement>;
  innerRef?: ((ta: HTMLInputElement | null) => void) | React.RefObject<HTMLInputElement>;
  id?: string;
}

const cachedLocaleSeparators: {
  [locale: string]: { group: string, decimal: string }
} = {};

function getLocaleSeparators(locale: string) {
  var result = cachedLocaleSeparators[locale];
  if (result)
    return result;

  var format = new Intl.NumberFormat(locale, { minimumFractionDigits: 0 });
  result = {
    group: format.format(1111).replace(/1/g, ''),
    decimal: format.format(1.1).replace(/1/g, ''),
  };
  return cachedLocaleSeparators[locale] = result;
}


export function NumericTextBox(p: NumericTextBoxProps) {

  const [text, setText] = React.useState<string | undefined>(undefined);


  const value = text != undefined ? text :
    p.value != undefined ? p.format?.format(p.value) :
      "";

  return <input ref={p.innerRef} {...p.htmlAttributes}
    id={p.id}
    readOnly={p.readonly}
    type="text"
    autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
    className={addClass(p.htmlAttributes, classes(p.formControlClass, "numeric"))} value={value}
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
    if (!p.readonly) {
      if (text != null) {
        let value = NumberLineController.autoFixString(text, false, false);

        const result = value == undefined || value.length == 0 ? null : unformat(p.format, value);
        setText(undefined);
        if (result != p.value)
          p.onChange(result);
      }
    }

    if (p.htmlAttributes && p.htmlAttributes.onBlur)
      p.htmlAttributes.onBlur(e);
  }


  function unformat(format: Intl.NumberFormat, str: string): number {

    var options = format.resolvedOptions();

    var isPercentage = options.style == "percent";

    var separators = getLocaleSeparators(options.locale);

    if (separators.group)
      str = str.replace(new RegExp('\\' + separators.group, 'g'), '');

    if (separators.decimal)
      str = str.replace(new RegExp('\\' + separators.decimal), '.');

    var result = parseFloat(str);

    if (isPercentage)
      return result / 100;

    return result;
  }

  function handleOnChange(e: React.SyntheticEvent<any>) {
    if (!p.readonly) {
      const input = e.currentTarget as HTMLInputElement;
      setText(input.value);
    }
  }

  function handleKeyDown(e: React.KeyboardEvent<any>) {
    if (!p.validateKey(e))
      e.preventDefault();
    else {
      var atts = p.htmlAttributes;
      atts?.onKeyDown && atts.onKeyDown(e);
    }
  }
}

tasks.push(taskSetUnit);
export function taskSetUnit(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof NumericTextBox) {
    const vProps = state as NumberLineProps;

    if (vProps.unit === undefined &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.unit = state.ctx.propertyRoute.member!.unit;
    }
  }
}
