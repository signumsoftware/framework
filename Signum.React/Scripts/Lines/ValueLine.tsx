import * as React from 'react'
import { DateTime, Duration, DurationObjectUnits } from 'luxon'
import { DateTimePicker, DatePicker } from 'react-widgets'
import { Dic, addClass, classes } from '../Globals'
import { MemberInfo, getTypeInfo, TypeReference, toLuxonFormat, toDurationFormat, toNumberFormat, isTypeEnum, durationToString, TypeInfo, parseDuration } from '../Reflection'
import { LineBaseController, LineBaseProps, useController } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum, JavascriptMessage } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import 'react-widgets/dist/css/react-widgets.css';
import { KeyCodes } from '../Components/Basic';
import { format } from 'd3';
import { isPrefix } from '../FindOptions'

export interface ValueLineProps extends LineBaseProps {
  valueLineType?: ValueLineType;
  unitText?: React.ReactChild;
  formatText?: string;
  autoTrimString?: boolean;
  autoFixString?: boolean;
  inlineCheckbox?: boolean | "block";
  comboBoxItems?: (OptionItem | MemberInfo | string)[];
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  extraButtons?: (vl: ValueLineController) => React.ReactNode;
  initiallyFocused?: boolean;
  incrementWithArrow?: boolean | number;
  columnCount?: number;
  columnWidth?: number;
  showTimeBox?: boolean;
}

export interface OptionItem {
  value: any;
  label: string;
}

export type ValueLineType =
  "Checkbox" |
  "ComboBox" |
  "DateTime" |
  "TextBox" |
  "TextArea" |
  "Number" |
  "Decimal" |
  "Color" |
  "TimeSpan" |
  "RadioGroup" |
  "Password";

export class ValueLineController extends LineBaseController<ValueLineProps>{

  inputElement!: React.RefObject<HTMLElement>;
  init(p: ValueLineProps) {
    super.init(p);

    this.inputElement = React.useRef<HTMLElement>(null);

    React.useEffect(() => {
      if (this.props.initiallyFocused) {
        setTimeout(() => {
          let element = this.inputElement.current;
          if (element) {
            if (element instanceof HTMLInputElement)
              element.setSelectionRange(0, element.value.length);
            else if (element instanceof HTMLTextAreaElement)
              element.setSelectionRange(0, element.value.length);
            element.focus();
          }
        }, 0);
      }

    }, []);
  }

  static autoFixString(str: string, autoTrim: boolean): string {

    if (autoTrim)
      return str?.trim();

    return str;
  }

  getDefaultProps(state: ValueLineProps) {
    super.getDefaultProps(state);
    if (state.type) {
      state.valueLineType = ValueLineController.getValueLineType(state.type);

      if (state.valueLineType == undefined)
        throw new Error(`No ValueLineType found for type '${state.type!.name}' (property route = ${state.ctx.propertyRoute ? state.ctx.propertyRoute.propertyPath() : "??"})`);
    }
  }

  overrideProps(state: ValueLineProps, overridenProps: ValueLineProps) {

    const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
    super.overrideProps(state, overridenProps);
    state.valueHtmlAttributes = valueHtmlAttributes;
  }

  static getValueLineType(t: TypeReference): ValueLineType | undefined {

    if (t.isCollection || t.isLite)
      return undefined;

    if (isTypeEnum(t.name) || t.name == "boolean" && !t.isNotNullable)
      return "ComboBox";

    if (t.name == "boolean")
      return "Checkbox";

    if (t.name == "datetime" || t.name == "DateTimeOffset" || t.name == "Date")
      return "DateTime";

    if (t.name == "string" || t.name == "Guid")
      return "TextBox";

    if (t.name == "number")
      return "Number";

    if (t.name == "decimal")
      return "Decimal";

    if (t.name == "TimeSpan")
      return "TimeSpan";

    return undefined;
  }

  withItemGroup(input: JSX.Element): JSX.Element {
    if (!this.props.unitText && !this.props.extraButtons)
      return input;

    return (
      <div className={this.props.ctx.inputGroupClass}>
        {input}
        {
          (this.props.unitText != null || this.props.extraButtons != null) &&
          <div className="input-group-append">
            {this.props.unitText && <span className={this.props.ctx.readonlyAsPlainText ? undefined : "input-group-text"}>{this.props.unitText}</span>}
            {this.props.extraButtons && this.props.extraButtons(this)}
          </div>
        }
      </div>
    );
  }

  getPlaceholder(): string | undefined {
    const p = this.props;
    return p.ctx.placeholderLabels ? asString(p.labelText) :
      p.valueHtmlAttributes && p.valueHtmlAttributes!.placeholder;
  }
}

function asString(reactChild: React.ReactNode | undefined): string | undefined {
  if (typeof reactChild == "string")
    return reactChild as string;

  return undefined;
}

export const ValueLine = React.memo(React.forwardRef(function ValueLine(props: ValueLineProps, ref: React.Ref<ValueLineController>) {

  const c = useController(ValueLineController, props, ref);

  if (c.isHidden)
    return null;

  return ValueLineRenderers.renderers[c.props.valueLineType!](c);
}), (prev, next) => {
  if (
    next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export namespace ValueLineRenderers {
  export const renderers: {
    [valueLineType: string]: (vl: ValueLineController) => JSX.Element;
  } = {};
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

function isDecimal(e: React.KeyboardEvent<any>): boolean {
  const c = e.keyCode;
  return (isNumber(e) ||
    (c == 110) /*NumPad Decimal*/ ||
    (c == 190) /*.*/ ||
    (c == 188) /*,*/);
}

function isDuration(e: React.KeyboardEvent<any>): boolean {
  const c = e.keyCode;
  return isNumber(e) || e.key == ":";
}

ValueLineRenderers.renderers["Checkbox" as ValueLineType] = (vl) => {
  const s = vl.props;

  const handleCheckboxOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    vl.setValue(input.checked);
  };

  if (s.inlineCheckbox) {
    return (
      <label className={vl.props.ctx.error} style={{ display: s.inlineCheckbox == "block" ? "block" : undefined }} {...vl.baseHtmlAttributes()} {...s.formGroupHtmlAttributes}>
        <input type="checkbox" {...vl.props.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange} disabled={s.ctx.readOnly} />
        {" " + s.labelText}
        {s.helpText && <small className="form-text text-muted">{s.helpText}</small>}
      </label>
    );
  }
  else {
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }}>
        <input type="checkbox" {...vl.props.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange}
          className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))} disabled={s.ctx.readOnly} />
      </FormGroup>
    );
  }
};

ValueLineRenderers.renderers["ComboBox"] = (vl) => {
  return internalComboBox(vl);
};

function getOptionsItems(vl: ValueLineController): OptionItem[] {
  var ti: TypeInfo;
  function getTi() {
    return ti ?? (ti = getTypeInfo(vl.props.type!.name));
  }

  if (vl.props.comboBoxItems)
    return vl.props.comboBoxItems.map(a =>
      typeof a == "string" ? getTi().members[a] && toOptionItem(getTi().members[a]) :
        toOptionItem(a)).filter(a => !!a);

  if (vl.props.type!.name == "boolean")
    return ([
      { label: BooleanEnum.niceToString("False")!, value: false },
      { label: BooleanEnum.niceToString("True")!, value: true }
    ]);

  return Dic.getValues(getTi().members).map(m => toOptionItem(m));
}

function toOptionItem(m: MemberInfo | OptionItem): OptionItem {

  if ((m as MemberInfo).name)
    return {
      value: (m as MemberInfo).name,
      label: (m as MemberInfo).niceName,
    };

  return m as OptionItem;
}

function internalComboBox(vl: ValueLineController) {

  var optionItems = getOptionsItems(vl);

  const s = vl.props;
  if (!s.type!.isNotNullable || s.ctx.value == undefined)
    optionItems = [{ value: null, label: " - " }].concat(optionItems);

  if (s.ctx.readOnly) {

    var label = null;
    if (s.ctx.value != undefined) {

      var item = optionItems.filter(a => a.value == s.ctx.value).singleOrNull();

      label = item ? item.label : s.ctx.value.toString();
    }

    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(
          <FormControlReadonly htmlAttributes={{
            ...vl.props.valueHtmlAttributes,
            ...({ 'data-value': s.ctx.value } as any) /*Testing*/
          }} ctx={s.ctx} innerRef={vl.inputElement}>
            {label}
          </FormControlReadonly>)}
      </FormGroup>
    );
  }

  function toStr(val: any) {
    return val == null ? "" :
      val === true ? "True" :
        val === false ? "False" :
          val.toString();
  }

  const handleEnumOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    const option = optionItems.filter(a => toStr(a.value) == input.value).single();
    vl.setValue(option.value);
  };

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <select {...vl.props.valueHtmlAttributes} value={toStr(s.ctx.value)} className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))} onChange={handleEnumOnChange} >
          {optionItems.map((oi, i) => <option key={i} value={toStr(oi.value)}>{oi.label}</option>)}
        </select>)
      }
    </FormGroup>
  );

}

ValueLineRenderers.renderers["TextBox" as ValueLineType] = (vl) => {
  return internalTextBox(vl, false);
};

ValueLineRenderers.renderers["Password" as ValueLineType] = (vl) => {
  return internalTextBox(vl, true);
}

function internalTextBox(vl: ValueLineController, password: boolean) {

  const s = vl.props;

  var htmlAtts = vl.props.valueHtmlAttributes;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(<FormControlReadonly htmlAttributes={htmlAtts} ctx={s.ctx} innerRef={vl.inputElement}>
          {s.ctx.value}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleTextOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    vl.setValue(input.value);
  };

  let handleBlur: ((e: React.FocusEvent<any>) => void) | undefined = undefined;
  if (s.autoFixString != false) {
    handleBlur = (e: React.FocusEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      var fixed = ValueLineController.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : true);
      if (fixed != input.value)
        vl.setValue(fixed);

      if (htmlAtts?.onBlur)
        htmlAtts.onBlur(e);
    };
  }

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <input type={password ? "password" : "text"}
          autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
          {...vl.props.valueHtmlAttributes}
          className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))}
          value={s.ctx.value ?? ""}
          onBlur={handleBlur || htmlAtts?.onBlur}
          onChange={isIE11() ? undefined : handleTextOnChange} //https://github.com/facebook/react/issues/7211
          onInput={isIE11() ? handleTextOnChange : undefined}
          placeholder={vl.getPlaceholder()}
          ref={vl.inputElement as React.RefObject<HTMLInputElement>} />)
      }
    </FormGroup>
  );
}

function isIE11(): boolean {
  return (!!(window as any).MSInputMethodContext && !!(document as any).documentMode);
}

ValueLineRenderers.renderers["TextArea" as ValueLineType] = (vl) => {

  const s = vl.props;

  var htmlAtts = vl.props.valueHtmlAttributes;
  var autoResize = htmlAtts?.style?.height == null && htmlAtts?.rows == null;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        <TextArea {...htmlAtts} autoResize={autoResize} className={addClass(htmlAtts, classes(s.ctx.formControlClass, vl.mandatoryClass))} value={s.ctx.value || ""}
          disabled />
      </FormGroup>
    );

  const handleTextOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    vl.setValue(input.value);
  };

  let handleBlur: ((e: React.FocusEvent<any>) => void) | undefined = undefined;
  if (s.autoFixString != false) {
    handleBlur = (e: React.FocusEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      var fixed = ValueLineController.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : false);
      if (fixed != input.value)
        vl.setValue(fixed);

      if (htmlAtts?.onBlur)
        htmlAtts.onBlur(e);
    };
  }

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <TextArea {...vl.props.valueHtmlAttributes} autoResize={autoResize} className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))} value={s.ctx.value || ""}
          onChange={isIE11() ? undefined : handleTextOnChange} //https://github.com/facebook/react/issues/7211 && https://github.com/omcljs/om/issues/704
          onInput={isIE11() ? handleTextOnChange : undefined}
          onBlur={handleBlur ?? htmlAtts?.onBlur}
          placeholder={vl.getPlaceholder()}
          innerRef={vl.inputElement as any} />
      )}
    </FormGroup>
  );
};

ValueLineRenderers.renderers["Number" as ValueLineType] = (vl) => {
  return numericTextBox(vl, isNumber);
};

ValueLineRenderers.renderers["Decimal" as ValueLineType] = (vl) => {
  return numericTextBox(vl, isDecimal);
};

function numericTextBox(vl: ValueLineController, validateKey: (e: React.KeyboardEvent<any>) => boolean) {
  const s = vl.props

  const numberFormat = toNumberFormat(s.formatText);

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(
          <FormControlReadonly htmlAttributes={vl.props.valueHtmlAttributes} ctx={s.ctx} className="numeric" innerRef={vl.inputElement}>
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
      vl.setValue((s.ctx.value ?? 0) - incNumber);
    } else if (e.keyCode == KeyCodes.up) {
      e.preventDefault();
      vl.setValue((s.ctx.value ?? 0) + incNumber);
    }
  }

  const htmlAttributes = {
    placeholder: vl.getPlaceholder(),
    onKeyDown: (vl.props.incrementWithArrow || vl.props.incrementWithArrow == undefined && vl.props.valueLineType == "Number") ? handleKeyDown : undefined,
    ...vl.props.valueHtmlAttributes
  } as React.AllHTMLAttributes<any>;

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <NumericTextBox
          htmlAttributes={htmlAttributes}
          value={s.ctx.value}
          onChange={handleOnChange}
          formControlClass={classes(s.ctx.formControlClass, vl.mandatoryClass)}
          validateKey={validateKey}
          format={numberFormat}
          innerRef={vl.inputElement as React.RefObject<HTMLInputElement>}
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
    readOnly={p.readonly}
    type="text"
    autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
    className={addClass(p.htmlAttributes, classes(p.formControlClass, "numeric"))} value={value}
    onBlur={handleOnBlur}
    onChange={isIE11() ? undefined : handleOnChange} //https://github.com/facebook/react/issues/7211
    onInput={isIE11() ? handleOnChange : undefined}
    onKeyDown={handleKeyDown}
    onFocus={handleOnFocus}/>


  function handleOnFocus(e: React.FocusEvent<any>)
  {
    const input = e.currentTarget as HTMLInputElement;

    input.setSelectionRange(0, input.value != null ? input.value.length : 0);

    if (p.htmlAttributes && p.htmlAttributes.onFocus)
      p.htmlAttributes.onFocus(e);
  };


  function handleOnBlur(e: React.FocusEvent<any>) {
    if (!p.readonly) {
      const input = e.currentTarget as HTMLInputElement;
      let value = ValueLineController.autoFixString(input.value, false);

      //if (numbro.languageData().delimiters.decimal == ',' && !value.contains(",") && value.trim().length > 0) //Numbro transforms 1.000 to 1,0 in spanish or german
      //  value = value + ",00";

      const result = value == undefined || value.length == 0 ? null : unformat(p.format, value);
      setText(undefined);
      if (result != p.value)
        p.onChange(result);
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

    var result =  parseFloat(str);

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

ValueLineRenderers.renderers["DateTime" as ValueLineType] = (vl) => {

  const s = vl.props;

  const luxonFormat = toLuxonFormat(s.formatText);

  const m = s.ctx.value ? DateTime.fromISO(s.ctx.value) : undefined;
  const showTime = s.showTimeBox != null ? s.showTimeBox : luxonFormat != "D" && luxonFormat != "DD" && luxonFormat != "DDD";
  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(<FormControlReadonly htmlAttributes={vl.props.valueHtmlAttributes} className={addClass(vl.props.valueHtmlAttributes, "sf-readonly-date")} ctx={s.ctx} innerRef={vl.inputElement}>
          {m?.toFormatFixed(luxonFormat)}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleDatePickerOnChange = (date: Date | null | undefined, str: string) => {
    const m = date && DateTime.fromJSDate(date);
    vl.setValue(m == null || !m.isValid ? null :
      vl.props.type!.name == "Date" ? m.toISODate() :
        !showTime ? m.startOf("day").toFormat("yyyy-MM-dd'T'HH:mm:ss" /*No Z*/) :
          m.toISO());
  };

  const htmlAttributes = {
    placeholder: vl.getPlaceholder(),
    ...vl.props.valueHtmlAttributes,
  } as React.AllHTMLAttributes<any>;

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <div className={classes(s.ctx.rwWidgetClass, vl.mandatoryClass ? vl.mandatoryClass + "-widget" : undefined)}>
          <DateTimePicker value={m?.toJSDate()} onChange={handleDatePickerOnChange} autoFocus={vl.props.initiallyFocused}
            valueEditFormat={luxonFormat}
            valueDisplayFormat={luxonFormat}
            includeTime={showTime}
            inputProps={htmlAttributes as any} placeholder={htmlAttributes.placeholder}
            messages={{ dateButton: JavascriptMessage.Date.niceToString(), timeButton: JavascriptMessage.Time.niceToString() }}
          />
        </div>
      )}
    </FormGroup>
  );
}

ValueLineRenderers.renderers["TimeSpan" as ValueLineType] = (vl) => {
  return durationTextBox(vl, isDuration);
};

function durationTextBox(vl: ValueLineController, validateKey: (e: React.KeyboardEvent<any>) => boolean) {

  const s = vl.props;

  const durationFormat = toDurationFormat(s.formatText);

  if (s.ctx.readOnly) {
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(
          <FormControlReadonly htmlAttributes={vl.props.valueHtmlAttributes} ctx={s.ctx} className={addClass(vl.props.valueHtmlAttributes, "numeric")} innerRef={vl.inputElement}>
            {durationToString(s.ctx.value, durationFormat)}
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

  if (htmlAttributes.placeholder == undefined)
    htmlAttributes.placeholder = durationFormat;

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <DurationTextBox htmlAttributes={htmlAttributes}
          value={s.ctx.value}
          onChange={handleOnChange}
          validateKey={validateKey}
          formControlClass={classes(s.ctx.formControlClass, vl.mandatoryClass)}
          format={durationFormat}
          innerRef={vl.inputElement as React.RefObject<HTMLInputElement>} />
      )}
    </FormGroup>
  );
}

export interface DurationTextBoxProps {
  value: string | null;
  onChange: (newValue: string | null) => void;
  validateKey: (e: React.KeyboardEvent<any>) => boolean;
  formControlClass?: string;
  format?: string;
  htmlAttributes: React.HTMLAttributes<HTMLInputElement>;
  innerRef?: React.RefObject<HTMLInputElement>;
}

export function DurationTextBox(p: DurationTextBoxProps) {

  const [text, setText] = React.useState<string | undefined>(undefined);

  const value = text != undefined ? text :
    p.value != undefined ? durationToString(p.value, p.format) :
      "";

  return <input ref={p.innerRef}
    {...p.htmlAttributes}
    type="text"
    autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
    className={addClass(p.htmlAttributes, classes(p.formControlClass, "numeric"))}
    value={value}
    onBlur={handleOnBlur}
    onChange={isIE11() ? undefined : handleOnChange} //https://github.com/facebook/react/issues/7211
    onInput={isIE11() ? handleOnChange : undefined}
    onKeyDown={handleKeyDown}
    onFocus={handleOnFocus} />


  function handleOnFocus(e: React.FocusEvent<any>) {
    const input = e.currentTarget as HTMLInputElement;

    input.setSelectionRange(0, input.value != null ? input.value.length : 0);

    if (p.htmlAttributes && p.htmlAttributes.onFocus)
      p.htmlAttributes.onFocus(e);
  };


  function handleOnBlur(e: React.FocusEvent<any>) {

    var format = p.format!;

    const input = e.currentTarget as HTMLInputElement;
    const result = input.value == undefined || input.value.length == 0 ? null : parseDurationRelaxed(input.value, format)?.toFormat(format) ?? null;
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
}

export function parseDurationRelaxed(timeStampOrHumanStr: string, format: string = "hh:mm:ss"): Duration | null {
  var valParts = timeStampOrHumanStr.split(":");
  var formatParts = format.split(":");
  if (valParts.length == 1 && formatParts.length > 1) {
    const validFormats = Array.range(0, formatParts.length).map(i => Array.range(0, i + 1).map(j => formatParts[j]).join("")); //hh:mm:ss -> "" "hh" "hhmm" "hhmmss"

    var inferedFormat = validFormats.firstOrNull(f => f.length >= timeStampOrHumanStr.length);
    if (inferedFormat == null)
      return null;

    var fixedVal = timeStampOrHumanStr.padStart(inferedFormat.length, '0');

    const getPart = (part: string) => {
      var index = inferedFormat!.indexOf(part);
      if (index == -1)
        return 0;

      return parseInt(fixedVal.substr(index, part.length));
    }

    return Duration.fromObject({
      hour: getPart("hh"),
      minute: getPart("mm"),
      second: getPart("ss"),
    });

  } else {
    return parseDuration(timeStampOrHumanStr, format);
  }
}

DurationTextBox.defaultProps = {
  format: "hh:mm:ss"
};

ValueLineRenderers.renderers["RadioGroup" as ValueLineType] = (vl) => {
  return internalRadioGroup(vl);
};

function internalRadioGroup(vl: ValueLineController) {

  var optionItems = getOptionsItems(vl);

  const s = vl.props;

  const handleEnumOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    const option = optionItems.filter(a => a.value == input.value).single();
    vl.setValue(option.value);
  };

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      <div style={getColumnStyle()}>
        {optionItems.map((oi, i) =>
          <label {...vl.props.valueHtmlAttributes} className={classes("sf-radio-element", vl.props.ctx.error)}>
            <input type="radio" key={i} value={oi.value} checked={s.ctx.value == oi.value} onChange={handleEnumOnChange} disabled={s.ctx.readOnly} />
            {" " + oi.label}
          </label>)}
      </div>
    </FormGroup>
  );

  function getColumnStyle(): React.CSSProperties | undefined {

    const p = vl.props;

    if (p.columnCount && p.columnWidth)
      return {
        columns: `${p.columnCount} ${p.columnWidth}px`,
        MozColumns: `${p.columnCount} ${p.columnWidth}px`,
        WebkitColumns: `${p.columnCount} ${p.columnWidth}px`,
      };

    if (p.columnCount)
      return {
        columnCount: p.columnCount,
        MozColumnCount: p.columnCount,
        WebkitColumnCount: p.columnCount,
      };

    if (p.columnWidth)
      return {
        columnWidth: p.columnWidth,
        MozColumnWidth: p.columnWidth,
        WebkitColumnWidth: p.columnWidth,
      };

    return undefined;
  }
}

