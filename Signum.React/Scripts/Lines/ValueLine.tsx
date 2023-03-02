import * as React from 'react'
import { DateTime, Duration, DurationObjectUnits } from 'luxon'
import { DatePicker, DropdownList, Combobox } from 'react-widgets'
import { CalendarProps } from 'react-widgets/cjs/Calendar'
import { Dic, addClass, classes, softCast } from '../Globals'
import { MemberInfo, getTypeInfo, TypeReference, toLuxonFormat, toNumberFormat, isTypeEnum, timeToString, TypeInfo, tryGetTypeInfo, toFormatWithFixes, Type, splitLuxonFormat, dateTimePlaceholder, timePlaceholder, toLuxonDurationFormat } from '../Reflection'
import { LineBaseController, LineBaseProps, tasks, useController } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum, JavascriptMessage } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import { KeyCodes } from '../Components/Basic';
import { format, html } from 'd3';
import { isPrefix, QueryToken } from '../FindOptions'
import { useState } from 'react'
import { validateNewEntities } from '../Finder'

export interface ValueLineProps extends LineBaseProps {
  valueLineType?: ValueLineType;
  unit?: React.ReactChild;
  format?: string;
  autoTrimString?: boolean;
  autoFixString?: boolean;
  inlineCheckbox?: boolean | "block";
  optionItems?: (OptionItem | MemberInfo | string)[];
  datalist?: string[];
  onRenderDropDownListItem?: (oi: OptionItem) => React.ReactNode;
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  extraButtons?: (vl: ValueLineController) => React.ReactNode;
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

export type ValueLineType =
  "Checkbox" |
  "DropDownList" | /*For Enums! (only values in optionItems can be selected)*/
  "ComboBoxText" | /*For Text! (with freedom to choose a different value not in optionItems)*/
  "DateTime" |
  "DateTimeSplitted" |
  "TextBox" |
  "TextArea" |
  "Number" |
  "Decimal" |
  "Color" |
  "Time" |
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
      return "DropDownList";

    if (t.name == "boolean")
      return "Checkbox";

    if (t.name == "DateTime" || t.name == "DateTimeOffset" || t.name == "DateOnly")
      return "DateTime";

    if (t.name == "string" || t.name == "Guid")
      return "TextBox";

    if (t.name == "number")
      return "Number";

    if (t.name == "decimal")
      return "Decimal";

    if (t.name == "TimeSpan" || t.name == "TimeOnly")
      return "Time";

    return undefined;
  }

  withItemGroup(input: JSX.Element): JSX.Element {
    if (!this.props.unit && !this.props.extraButtons)
      return input;

    return (
      <div className={this.props.ctx.inputGroupClass}>
        {input}
        {this.props.unit && <span className={this.props.ctx.readonlyAsPlainText ? undefined : "input-group-text"}>{this.props.unit}</span>}
        {this.props.extraButtons && this.props.extraButtons(this)}
      </div>
    );
  }

  getPlaceholder(): string | undefined {
    const p = this.props;
    return p.valueHtmlAttributes?.placeholder ??
      (p.ctx.placeholderLabels || p.ctx.formGroupStyle == "FloatingLabel") ? asString(p.label) :
      undefined;
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

  return ValueLineRenderers.renderers.get(c.props.valueLineType!)!(c);
}), (prev, next) => {
  if (
    next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export namespace ValueLineRenderers {
  export const renderers: Map<ValueLineType, (vl: ValueLineController) => JSX.Element> = new Map();
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

export function isDecimal(e: React.KeyboardEvent<any>): boolean {
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

ValueLineRenderers.renderers.set("Checkbox", (vl) => {
  const s = vl.props;

  const handleCheckboxOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    vl.setValue(input.checked, e);
  };

  if (s.inlineCheckbox) {

    var atts = { ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes, ...s.labelHtmlAttributes };
    return (
      <label style={{ display: s.inlineCheckbox == "block" ? "block" : undefined }} {...atts} className={classes(s.ctx.labelClass, vl.props.ctx.errorClass, atts.className)}>
        <input type="checkbox" {...vl.props.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange} disabled={s.ctx.readOnly}
          className={addClass(vl.props.valueHtmlAttributes, classes("form-check-input"))}
        />
        {" "}{s.label}
        {s.helpText && <small className="form-text text-muted">{s.helpText}</small>}
      </label>
    );
  }
  else {
    return (
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }}>
        <input type="checkbox" {...vl.props.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange}
          className={addClass(vl.props.valueHtmlAttributes, classes("form-check-input"))} disabled={s.ctx.readOnly} />
      </FormGroup>
    );
  }
});


function getOptionsItems(vl: ValueLineController): OptionItem[] {

  var ti = tryGetTypeInfo(vl.props.type!.name);

  if (vl.props.optionItems) {
    return vl.props.optionItems
      .map(a => typeof a == "string" && ti != null && ti.kind == "Enum" ? toOptionItem(ti.members[a]) : toOptionItem(a))
      .filter(a => !!a);
  }

  if (vl.props.type!.name == "boolean")
    return ([
      { label: BooleanEnum.niceToString("False")!, value: false },
      { label: BooleanEnum.niceToString("True")!, value: true }
    ]);

  if (ti != null && ti.kind == "Enum")
    return Dic.getValues(ti.members).map(m => toOptionItem(m));

  throw new Error("Unable to get Options from " + vl.props.type!.name);
}

function toOptionItem(m: MemberInfo | OptionItem | string): OptionItem {

  if (typeof m == "string")
    return {
      value: m,
      label: m,
    }

  if ((m as MemberInfo).name)
    return {
      value: (m as MemberInfo).name,
      label: (m as MemberInfo).niceName,
    };

  return m as OptionItem;
}

ValueLineRenderers.renderers.set("DropDownList", (vl) => {
  return internalDropDownList(vl);
});


function internalDropDownList(vl: ValueLineController) {

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
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(
          <FormControlReadonly htmlAttributes={{
            ...vl.props.valueHtmlAttributes,
            ...({ 'data-value': s.ctx.value } as any) /*Testing*/
          }} ctx={s.ctx} innerRef={vl.setRefs}>
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

  if (vl.props.onRenderDropDownListItem) {

    var oi = optionItems.singleOrNull(a => a.value == s.ctx.value) ?? {
      value: s.ctx.value,
      label: s.ctx.value,
    };

    return (
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(
          <DropdownList<OptionItem> className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass, "p-0"))} data={optionItems}
            onChange={(oe, md) => vl.setValue(oe.value, md.originalEvent)}
            value={oi}
            filter={false}
            autoComplete="off"
            dataKey="value"
            textField="label"
            renderValue={a => vl.props.onRenderDropDownListItem!(a.item)}
            renderListItem={a => vl.props.onRenderDropDownListItem!(a.item)}
            {...(s.valueHtmlAttributes as any)}
          />)
        }
      </FormGroup>
    );
  } else {

    const handleEnumOnChange = (e: React.SyntheticEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      const option = optionItems.filter(a => toStr(a.value) == input.value).single();
      vl.setValue(option.value, e);
    };

    return (
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(
          <select {...vl.props.valueHtmlAttributes} value={toStr(s.ctx.value)} className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formSelectClass, vl.mandatoryClass))} onChange={handleEnumOnChange} >
            {optionItems.map((oi, i) => <option key={i} value={toStr(oi.value)}>{oi.label}</option>)}
          </select>)
        }
      </FormGroup>
    );
  }
}


ValueLineRenderers.renderers.set("ComboBoxText", (vl) => {
  return internalComboBoxText(vl);
});


function internalComboBoxText(vl: ValueLineController) {

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
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(
          <FormControlReadonly htmlAttributes={{
            ...vl.props.valueHtmlAttributes,
            ...({ 'data-value': s.ctx.value } as any) /*Testing*/
          }} ctx={s.ctx} innerRef={vl.setRefs}>
            {label}
          </FormControlReadonly>)}
      </FormGroup>
    );
  }


  var renderItem = vl.props.onRenderDropDownListItem ? (a: any) => vl.props.onRenderDropDownListItem!(a.item) : undefined;

  return (
    <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <Combobox<OptionItem> className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))} data={optionItems} onChange={(e: string | OptionItem, md) => {
          vl.setValue(e == null ? null : typeof e == "string" ? e : e.value, md.originalEvent);
        }} value={s.ctx.value}
          dataKey="value"
          textField="label"
          focusFirstItem
          autoSelectMatches
          renderListItem={renderItem}
          {...(s.valueHtmlAttributes as any)}
        />)
      }
    </FormGroup>
  );
}

ValueLineRenderers.renderers.set("TextBox", (vl) => {
  return internalTextBox(vl, false);
});

ValueLineRenderers.renderers.set("Password", (vl) => {
  return internalTextBox(vl, true);
});

function internalTextBox(vl: ValueLineController, password: boolean) {

  const s = vl.props;

  var htmlAtts = vl.props.valueHtmlAttributes;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(<FormControlReadonly htmlAttributes={htmlAtts} ctx={s.ctx} innerRef={vl.setRefs}>
          {s.ctx.value}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleTextOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    vl.setValue(input.value, e);
  };

  let handleBlur: ((e: React.FocusEvent<any>) => void) | undefined = undefined;
  if (s.autoFixString != false) {
    handleBlur = (e: React.FocusEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      var fixed = ValueLineController.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : true);
      if (fixed != input.value)
        vl.setValue(fixed, e);

      if (htmlAtts?.onBlur)
        htmlAtts.onBlur(e);
    };
  }

  return (
    <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <input type={password ? "password" : "text"}
          autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
          {...vl.props.valueHtmlAttributes}
          className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))}
          value={s.ctx.value ?? ""}
          onBlur={handleBlur || htmlAtts?.onBlur}
          onChange={handleTextOnChange} //https://github.com/facebook/react/issues/7211
          placeholder={vl.getPlaceholder()}
          list={s.datalist ? s.ctx.getUniqueId("dataList") : undefined}
          ref={vl.setRefs} />)
      }
      {s.datalist &&
        <datalist id={s.ctx.getUniqueId("dataList")}>
          {s.datalist.map(item => <option value={item} />)}
        </datalist>
      }
    </FormGroup>
  );
}

ValueLineRenderers.renderers.set("TextArea", (vl) => {

  const s = vl.props;

  var htmlAtts = vl.props.valueHtmlAttributes;
  var autoResize = htmlAtts?.style?.height == null && htmlAtts?.rows == null;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        <TextArea {...htmlAtts} autoResize={autoResize} className={addClass(htmlAtts, classes(s.ctx.formControlClass, vl.mandatoryClass))} value={s.ctx.value || ""}
          disabled />
      </FormGroup>
    );

  const handleTextOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    vl.setValue(input.value, e);
  };

  let handleBlur: ((e: React.FocusEvent<any>) => void) | undefined = undefined;
  if (s.autoFixString != false) {
    handleBlur = (e: React.FocusEvent<any>) => {
      const input = e.currentTarget as HTMLInputElement;
      var fixed = ValueLineController.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : false);
      if (fixed != input.value)
        vl.setValue(fixed, e);

      if (htmlAtts?.onBlur)
        htmlAtts.onBlur(e);
    };
  }

  const handleOnFocus = (e: React.FocusEvent<any>) => {
    console.log("onFocus handler called");
    if (htmlAtts?.onFocus) {
        console.log("passed onFocus called");

        htmlAtts?.onFocus(e);
    }
  }
    


  return (
    <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <TextArea {...vl.props.valueHtmlAttributes} autoResize={autoResize} className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))} value={s.ctx.value || ""}
          onChange={handleTextOnChange}
          onBlur={handleBlur ?? htmlAtts?.onBlur}
          onFocus={handleOnFocus}
          placeholder={vl.getPlaceholder()}
          innerRef={vl.setRefs} />
      )}
    </FormGroup>
  );
});

ValueLineRenderers.renderers.set("Number", (vl) => {
  return numericTextBox(vl, isNumber);
});

ValueLineRenderers.renderers.set("Decimal", (vl) => {
  return numericTextBox(vl, isDecimal);
});

function numericTextBox(vl: ValueLineController, validateKey: (e: React.KeyboardEvent<any>) => boolean) {
  const s = vl.props

  const numberFormat = toNumberFormat(s.format);

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(
          <FormControlReadonly htmlAttributes={vl.props.valueHtmlAttributes} ctx={s.ctx} className="numeric" innerRef={vl.setRefs}>
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
    onKeyDown: (vl.props.incrementWithArrow || vl.props.incrementWithArrow == undefined && vl.props.valueLineType == "Number") ? handleKeyDown : undefined,
    ...vl.props.valueHtmlAttributes
  } as React.AllHTMLAttributes<any>;

  return (
    <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <NumericTextBox
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
    onChange={handleOnChange} //https://github.com/facebook/react/issues/7211
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
        let value = ValueLineController.autoFixString(text, false);

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

ValueLineRenderers.renderers.set("DateTime", (vl) => {

  const s = vl.props;
  const type = vl.props.type!.name as "DateOnly" | "DateTime";
  const luxonFormat = toLuxonFormat(s.format, type);

  const m = s.ctx.value ? DateTime.fromISO(s.ctx.value) : undefined;
  const showTime = s.showTimeBox != null ? s.showTimeBox : type != "DateOnly" && luxonFormat != "D" && luxonFormat != "DD" && luxonFormat != "DDD";
  const monthOnly = luxonFormat == "LLLL yyyy";

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(<FormControlReadonly htmlAttributes={vl.props.valueHtmlAttributes} className={addClass(vl.props.valueHtmlAttributes, "sf-readonly-date")} ctx={s.ctx} innerRef={vl.setRefs}>
          {m && toFormatWithFixes(m, luxonFormat)}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleDatePickerOnChange = (date: Date | null | undefined, str: string) => {

    var m = date && DateTime.fromJSDate(date);

    if (m)
      m = trimDateToFormat(m, type, s.format);

    // bug fix with farsi locale : luxon cannot parse Jalaali dates so we force using en-GB for parsing and formatting
    vl.setValue(m == null || !m.isValid ? null :
      type == "DateOnly" ? m.toISODate() :
        !showTime ? m.startOf("day").toFormat("yyyy-MM-dd'T'HH:mm:ss", { locale: 'en-GB' }/*No Z*/) :
          m.toISO());
  };

  const htmlAttributes = {
    placeholder: vl.getPlaceholder(),
    ...vl.props.valueHtmlAttributes,
  } as React.AllHTMLAttributes<any>;

  if (htmlAttributes.placeholder === undefined)
    htmlAttributes.placeholder = dateTimePlaceholder(luxonFormat);

  return (
    <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <div className={classes(s.ctx.rwWidgetClass, vl.mandatoryClass ? vl.mandatoryClass + "-widget" : undefined, s.calendarAlignEnd && "sf-calendar-end")}>
          <DatePicker
            value={m?.toJSDate()} onChange={handleDatePickerOnChange} autoFocus={Boolean(vl.props.initiallyFocused)}
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
});

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


ValueLineRenderers.renderers.set("DateTimeSplitted", (vl) => {

  const s = vl.props;
  const type = vl.props.type!.name as "DateOnly" | "DateTime";
  const luxonFormat = toLuxonFormat(s.format, type);

  const dt = s.ctx.value ? DateTime.fromISO(s.ctx.value) : undefined;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(<FormControlReadonly htmlAttributes={vl.props.valueHtmlAttributes} className={addClass(vl.props.valueHtmlAttributes, "sf-readonly-date")} ctx={s.ctx} innerRef={vl.setRefs}>
          {dt && toFormatWithFixes(dt, luxonFormat)}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleDatePickerOnChange = (date: Date | null | undefined) => {

    var newDT = date && DateTime.fromJSDate(date);

    if (newDT)
      newDT = trimDateToFormat(newDT, type, s.format);

    // bug fix with farsi locale : luxon cannot parse Jalaali dates so we force using en-GB for parsing and formatting
    vl.setValue(newDT == null || !newDT.isValid ? null : newDT.toISO());
  };

  return (
    <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <DateTimePickerSplitted value={dt?.toJSDate()} onChange={handleDatePickerOnChange}
          initiallyFocused={Boolean(vl.props.initiallyFocused)}
          initiallyShowOnly={vl.props.initiallyShowOnly}
          luxonFormat={luxonFormat}
          minDate={s.minDate}
          maxDate={s.maxDate}
          mandatoryClass={vl.mandatoryClass}
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
}) {

  const [dateFormat, timeFormat] = splitLuxonFormat(p.luxonFormat);

  const [temp, setTemp] = React.useState<{ type: "Date", date: string } | { type: "Time", time: string } | null>(() => {
    if (p.initiallyShowOnly == null || p.value == null)
      return null;

    if (p.initiallyShowOnly == "Date")
      return ({ type: "Date", date: DateTime.fromJSDate(p.value).toISODate() });

    if (p.initiallyShowOnly == "Time")
      return ({ type: "Time", time: getTimeOfDay(DateTime.fromJSDate(p.value)).toISOTime() });

    return null;
  });

  function handleTimeChange(time: string | null) {
    if (time == null) {
      if (p.value != null && temp == null) {
        setTemp({ type: "Date", date: DateTime.fromJSDate(p.value).startOf("day").toISODate() });
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
        setTemp({ type: "Time", time: getTimeOfDay(DateTime.fromJSDate(p.value)).toISOTime() });
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
        setTemp({ type: "Date", date: DateTime.fromJSDate(date).toISODate() });
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

ValueLineRenderers.renderers.set("Time", (vl) => {
  return timeTextBox(vl, isDuration);
});

function timeTextBox(vl: ValueLineController, validateKey: (e: React.KeyboardEvent<any>) => boolean) {

  const s = vl.props;

  if (s.ctx.readOnly) {
    return (
      <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {vl.withItemGroup(
          <FormControlReadonly htmlAttributes={vl.props.valueHtmlAttributes} ctx={s.ctx} className={addClass(vl.props.valueHtmlAttributes, "numeric")} innerRef={vl.setRefs}>
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
    <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {vl.withItemGroup(
        <TimeTextBox htmlAttributes={htmlAttributes}
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
}

export function TimeTextBox(p: TimeTextBoxProps) {

  const [text, setText] = React.useState<string | undefined>(undefined);

  const value = text != undefined ? text :
    p.value != undefined ? Duration.fromISOTime(p.value).toFormat(p.durationFormat!) :
      "";

  return <input ref={p.innerRef}
    {...p.htmlAttributes}
    type="text"
    autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
    className={addClass(p.htmlAttributes, classes(p.formControlClass, "numeric"))}
    value={value}
    onBlur={handleOnBlur}
    onChange={handleOnChange} //https://github.com/facebook/react/issues/7211
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

ValueLineRenderers.renderers.set("RadioGroup", (vl) => {
  return internalRadioGroup(vl);
});

function internalRadioGroup(vl: ValueLineController) {

  var optionItems = getOptionsItems(vl);

  const s = vl.props;

  const handleEnumOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    const option = optionItems.filter(a => a.value == input.value).single();
    vl.setValue(option.value, e);
  };

  return (
    <FormGroup ctx={s.ctx} label={s.label} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      <div style={getColumnStyle()}>
        {optionItems.map((oi, i) =>
          <label {...vl.props.valueHtmlAttributes} className={classes("sf-radio-element", vl.props.ctx.errorClass)}>
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
      };

    if (p.columnCount)
      return {
        columnCount: p.columnCount,
      };

    if (p.columnWidth)
      return {
        columnWidth: p.columnWidth,
      };

    return undefined;
  }
}

tasks.push(taskSetUnit);
export function taskSetUnit(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof ValueLineController) {
    const vProps = state as ValueLineProps;

    if (vProps.unit === undefined &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.unit = state.ctx.propertyRoute.member!.unit;
    }
  }
}

tasks.push(taskSetFormat);
export function taskSetFormat(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof ValueLineController) {
    const vProps = state as ValueLineProps;

    if (!vProps.format &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.format = state.ctx.propertyRoute.member!.format;
      if (vProps.valueLineType == "TextBox" && state.ctx.propertyRoute.member!.format == "Password")
        vProps.valueLineType = "Password";
    }
  }
}


export let maxValueLineSize = 100;

tasks.push(taskSetHtmlProperties);
export function taskSetHtmlProperties(lineBase: LineBaseController<any>, state: LineBaseProps) {
  const vl = lineBase instanceof ValueLineController ? lineBase : undefined;
  const pr = state.ctx.propertyRoute;
  const s = state as ValueLineProps;
  if (vl && pr?.propertyRouteType == "Field" && (s.valueLineType == "TextBox" || s.valueLineType == "TextArea")) {

    var member = pr.member!;

    if (member.maxLength != undefined && !s.ctx.readOnly) {

      if (!s.valueHtmlAttributes)
        s.valueHtmlAttributes = {};

      if (s.valueHtmlAttributes.maxLength == undefined)
        s.valueHtmlAttributes.maxLength = member.maxLength;

      if (s.valueHtmlAttributes.size == undefined)
        s.valueHtmlAttributes.size = maxValueLineSize == undefined ? member.maxLength : Math.min(maxValueLineSize, member.maxLength);
    }

    if (member.isMultiline)
      s.valueLineType = "TextArea";
  }
}
