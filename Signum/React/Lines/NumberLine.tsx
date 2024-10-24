import * as React from 'react'
import { DateTime, Duration } from 'luxon'
import { CalendarProps } from 'react-widgets/cjs/Calendar'
import { Dic, classes } from '../Globals'
import { MemberInfo, TypeReference, toLuxonFormat, toNumberFormat, isTypeEnum, timeToString, tryGetTypeInfo, toFormatWithFixes, splitLuxonFormat, dateTimePlaceholder, timePlaceholder, toLuxonDurationFormat } from '../Reflection'
import { LineBaseController, LineBaseProps, setRefProp, tasks, useController, useInitiallyFocused } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum, JavascriptMessage } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import { KeyNames } from '../Components/Basic';
import { ValueBaseController, ValueBaseProps } from './ValueBase'
import { TypeContext } from '../Lines'

export interface NumberLineProps extends ValueBaseProps<number | null> {
  incrementWithArrow?: boolean | number;
}

export class NumberLineController extends ValueBaseController<NumberLineProps, number | null>{
}

export const NumberLine = React.memo(React.forwardRef(function NumberLine(props: NumberLineProps, ref: React.Ref<NumberLineController>) {

  const c = useController(NumberLineController, props, ref);

  if (c.isHidden)
    return null;

  return numericTextBox(c, c.props.type!.name == "decimal" ? isDecimalKey :  isNumberKey);
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
    if (e.key == KeyNames.arrowDown) {
      e.preventDefault();
      vl.setValue((s.ctx.value ?? 0) - incNumber, e);
    } else if (e.key == KeyNames.arrowUp) {
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
        <NumberBox
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

export interface NumberBoxProps {
  value: number | null | undefined;
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


export function NumberBox(p: NumberBoxProps) {

  const [text, setText] = React.useState<string | undefined>(undefined);


  const value = text != undefined ? text :
    p.value != undefined ? p.format?.format(p.value) :
      "";

  return <input ref={p.innerRef}
    autoComplete="off" 
    {...p.htmlAttributes}
    id={p.id}
    readOnly={p.readonly}
    type="text"
    className={classes(p.htmlAttributes?.className, p.formControlClass, "numeric")} value={value}
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

export function isNumberKey(e: React.KeyboardEvent<any>) {
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
