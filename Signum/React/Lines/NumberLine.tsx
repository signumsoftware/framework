import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { CalendarProps } from 'react-widgets/cjs/Calendar'
import { Dic, classes } from '../Globals'
import { MemberInfo, TypeReference, toLuxonFormat, toNumberFormat, isTypeEnum, timeToString, tryGetTypeInfo, toFormatWithFixes, splitLuxonFormat, dateTimePlaceholder, timePlaceholder, toLuxonDurationFormat, numberLimits } from '../Reflection'
import { LineBaseController, LineBaseProps, setRefProp, tasks, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum, JavascriptMessage } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import { KeyNames } from '../Components/Basic';
import { ValueBaseController, ValueBaseProps } from './ValueBase'
import { TypeContext } from '../Lines'
import { ValidationMessage } from '../Signum.Entities.Validation'

export interface NumberLineProps extends ValueBaseProps<number | null> {
  incrementWithArrow?: boolean | number;
  minValue?: number | null;
  maxValue?: number | null;
  ref?: React.Ref<NumberLineController>;
}

export class NumberLineController extends ValueBaseController<NumberLineProps, number | null>{
}

export const NumberLine: React.NamedExoticComponent<NumberLineProps> = React.memo(function NumberLine(props: NumberLineProps) {

  const c = useController(NumberLineController, props, props.ref);

  if (c.isHidden)
    return null;

  return numericTextBox(c, c.props.type!.name == "decimal" ? isDecimalKey :  isNumberKey);
}, (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});


function numericTextBox(c: NumberLineController, validateKey: (e: React.KeyboardEvent<any>) => boolean) {
  const p = c.props

  const numberFormat = toNumberFormat(p.format);
  const helpText = p.helpText && (typeof p.helpText == "function" ? p.helpText(c) : p.helpText);
  const helpTextOnTop = p.helpTextOnTop && (typeof p.helpTextOnTop == "function" ? p.helpTextOnTop(c) : p.helpTextOnTop);

  if (p.ctx.readOnly)
    return (
      <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes}>
        {inputId => c.withItemGroup(
          <FormControlReadonly id={inputId} htmlAttributes={c.props.valueHtmlAttributes} ctx={p.ctx} className="numeric" innerRef={c.setRefs}>
            {p.ctx.value == null ? "" : numberFormat.format(p.ctx.value)}
          </FormControlReadonly>)}
      </FormGroup>
    );

  const handleOnChange = (newValue: number | null) => {
    c.setValue(newValue);
  };

  var incNumber = typeof c.props.incrementWithArrow == "number" ? c.props.incrementWithArrow : 1;

  const handleKeyDown = (e: React.KeyboardEvent<any>) => {
    if (e.key == KeyNames.arrowDown) {
      e.preventDefault();
      c.setValue((p.ctx.value ?? 0) - incNumber, e);
    } else if (e.key == KeyNames.arrowUp) {
      e.preventDefault();
      c.setValue((p.ctx.value ?? 0) + incNumber, e);
    }
  }

  const htmlAttributes = {
    placeholder: c.getPlaceholder(),
    onKeyDown: (c.props.incrementWithArrow || c.props.incrementWithArrow == undefined ) ? handleKeyDown : undefined,
    ...c.props.valueHtmlAttributes
  } as React.AllHTMLAttributes<any>;

  const limits = numberLimits[p.type?.name!];

  return (
    <FormGroup ctx={p.ctx} error={p.error} label={p.label} labelIcon={p.labelIcon} helpText={helpText} helpTextOnTop={helpTextOnTop} htmlAttributes={{ ...c.baseHtmlAttributes(), ...p.formGroupHtmlAttributes }} labelHtmlAttributes={p.labelHtmlAttributes}>
      {inputId => c.withItemGroup(
        <NumberBox
          id={inputId}
          minValue={p.minValue != undefined ? p.minValue : limits?.min}
          maxValue={p.maxValue != undefined ? p.maxValue : limits?.max}
          htmlAttributes={htmlAttributes}
          value={p.ctx.value}
          onChange={handleOnChange}
          formControlClass={classes(p.ctx.formControlClass, c.mandatoryClass)}
          validateKey={validateKey}
          format={numberFormat}
          innerRef={c.setRefs}
        />
      )}
    </FormGroup>
  );
}

export interface NumberBoxProps {
  value: number | null | undefined;
  readonly?: boolean;
  onChange: (newValue: number | null) => void;
  validateKey: (e: React.KeyboardEvent<any>) => boolean;
  minValue?: number | null;
  maxValue?: number | null;
  format: Intl.NumberFormat;
  formControlClass?: string;
  htmlAttributes?: React.InputHTMLAttributes<HTMLInputElement>;
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


export function NumberBox(p: NumberBoxProps): React.ReactElement {

  const [text, setText] = React.useState<string | undefined>(undefined);


  const value = text != undefined ? text :
    p.value != undefined ? p.format?.format(p.value) :
      "";

  const warning =
    p.value != null && p.minValue != null && p.value < p.minValue ? ValidationMessage.NumberIsTooSmall.niceToString() :
      p.value != null && p.maxValue != null && p.maxValue < p.value ? ValidationMessage.NumberIsTooBig.niceToString() :
        undefined;

  return <input ref={p.innerRef}
    autoComplete="off"
    {...p.htmlAttributes}
    id={p.id}
    readOnly={p.readonly}
    type="text"
    className={classes(p.htmlAttributes?.className, p.formControlClass, "numeric", warning && "border-warning")} value={value}
    title={warning}
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

  function triggetOnBlur() {
    if (text != null) {
      let value = NumberLineController.autoFixString(text, false, false);

      const result = value == undefined || value.length == 0 ? null : unformat(p.format, value);
      setText(undefined);
      if (result != p.value)
        p.onChange(result);
    }
  }


  function handleOnBlur(e: React.FocusEvent<any>) {
    if (!p.readonly) {
      triggetOnBlur();
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

    if (!p.validateKey(e)) {
      if (e.ctrlKey || e.altKey) //possible shortcut
        triggetOnBlur();
      e.preventDefault();
    }
    else {
      var atts = p.htmlAttributes;
      atts?.onKeyDown && atts.onKeyDown(e);
    }
  }
}

export function isNumberKey(e: React.KeyboardEvent<any>): boolean {
  const c = e.key;
  return ((c >= '0' && c <= '9' && !e.shiftKey) /*0-9*/ ||
    (c == KeyNames.enter) ||
    (c == KeyNames.backspace) ||
    (c == KeyNames.tab) ||
    (c == KeyNames.esc) ||
    (c == KeyNames.arrowLeft) ||
    (c == KeyNames.arrowRight) ||
    (c == KeyNames.arrowUp) ||
    (c == KeyNames.arrowDown) ||
    (c == KeyNames.delete) ||
    (c == KeyNames.home) ||
    (c == KeyNames.end) ||
    (c == KeyNames.numpadMinus) /*NumPad -*/ ||
    (c == KeyNames.minus) /*-*/ ||
    (e.ctrlKey && c == 'v') /*Ctrl + v*/ ||
    (e.ctrlKey && c == 'x') /*Ctrl + x*/ ||
    (e.ctrlKey && c == 'c') /*Ctrl + c*/);
}

export function isDecimalKey(e: React.KeyboardEvent<any>): boolean {
  return (isNumberKey(e) ||
    (e.key == "Separator") /*NumPad Decimal*/ ||
    (e.key == ".") /*.*/ ||
    (e.key == ",") /*,*/);
}
