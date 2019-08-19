import * as React from 'react'
import * as moment from 'moment'
import * as numbro from 'numbro'
import * as DateTimePicker from 'react-widgets/lib/DateTimePicker'
import { Dic, addClass, classes } from '../Globals'
import { MemberInfo, getTypeInfo, TypeReference, toMomentFormat, toDurationFormat, toNumbroFormat, isTypeEnum, durationToString } from '../Reflection'
import { LineBase, LineBaseProps } from '../Lines/LineBase'
import { FormGroup } from '../Lines/FormGroup'
import { FormControlReadonly } from '../Lines/FormControlReadonly'
import { BooleanEnum } from '../Signum.Entities'
import TextArea from '../Components/TextArea';
import 'react-widgets/dist/css/react-widgets.css';
import { KeyCodes } from '../Components/Basic';
import { format } from 'd3';

export interface ValueLineProps extends LineBaseProps, React.Props<ValueLine> {
  valueLineType?: ValueLineType;
  unitText?: React.ReactChild;
  formatText?: string;
  autoTrimString?: boolean;
  autoFixString?: boolean;
  inlineCheckbox?: boolean | "block";
  comboBoxItems?: (OptionItem | MemberInfo | string)[];
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  extraButtons?: (vl: ValueLine) => React.ReactNode;
  initiallyFocused?: boolean;
  incrementWithArrow?: boolean | number;
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
  "TimeSpan";

export class ValueLine extends LineBase<ValueLineProps, ValueLineProps> {
  static autoFixString(str: string, autoTrim: boolean): string {

    if (autoTrim)
      return str && str.trim();

    return str;
  }

  calculateDefaultState(state: ValueLineProps) {
    state.valueLineType = ValueLine.getValueLineType(state.type!);

    if (state.valueLineType == undefined)
      throw new Error(`No ValueLineType found for type '${state.type!.name}' (property route = ${state.ctx.propertyRoute ? state.ctx.propertyRoute.propertyPath() : "??"})`);
  }

  inputElement?: HTMLElement | null;

  componentDidMount() {
    if(this.props.initiallyFocused)
    setTimeout(() => {
      let element = this.inputElement;
      if (element) {
        if (element instanceof HTMLInputElement)
          element.setSelectionRange(0, element.value.length);
        else if (element instanceof HTMLTextAreaElement)
          element.setSelectionRange(0, element.value.length);
        element.focus();
      }
    }, 0);
  }

  static getValueLineType(t: TypeReference): ValueLineType | undefined {

    if (t.isCollection || t.isLite)
      return undefined;

    if (isTypeEnum(t.name) || t.name == "boolean" && !t.isNotNullable)
      return "ComboBox";

    if (t.name == "boolean")
      return "Checkbox";

    if (t.name == "datetime" || t.name == "DateTimeOffset")
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

  overrideProps(state: ValueLineProps, overridenProps: ValueLineProps) {

    const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
    super.overrideProps(state, overridenProps);
    state.valueHtmlAttributes = valueHtmlAttributes;
  }

  static renderers: {
    [valueLineType: string]: (vl: ValueLine) => JSX.Element;
  } = {};


  renderInternal() {

    if (this.state.visible == false || this.state.hideIfNull && this.state.ctx.value == undefined)
      return null;

    return ValueLine.renderers[this.state.valueLineType!](this);

  }

  static withItemGroup(vl: ValueLine, input: JSX.Element): JSX.Element {
    if (!vl.state.unitText && !vl.state.extraButtons)
      return input;

    return (
      <div className={vl.state.ctx.inputGroupClass}>
        {input}
        {
          (vl.state.unitText != null || vl.state.extraButtons != null) &&
          <div className="input-group-append">
            {vl.state.unitText && <span className={vl.state.ctx.readonlyAsPlainText ? undefined : "input-group-text"}>{vl.state.unitText}</span>}
            {vl.state.extraButtons && vl.state.extraButtons(vl)}
          </div>
        }
      </div>
    );
  }

  static isNumber(e: React.KeyboardEvent<any>) {
    const c = e.keyCode;
    return ((c >= 48 && c <= 57) /*0-9*/ ||
      (c >= 96 && c <= 105) /*NumPad 0-9*/ ||
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

  static isDecimal(e: React.KeyboardEvent<any>): boolean {
    const c = e.keyCode;
    return (ValueLine.isNumber(e) ||
      (c == 110) /*NumPad Decimal*/ ||
      (c == 190) /*.*/ ||
      (c == 188) /*,*/);
  }

  static isDuration(e: React.KeyboardEvent<any>): boolean {
    const c = e.keyCode;
    return (ValueLine.isNumber(e) ||
      (c == 190) /*. Colon*/);
  }
}

ValueLine.renderers["Checkbox" as ValueLineType] = (vl) => {
  const s = vl.state;

  const handleCheckboxOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    vl.setValue(input.checked);
  };

  if (s.inlineCheckbox) {
    return (
      <label className={vl.state.ctx.error} style={{ display: s.inlineCheckbox == "block" ? "block" : undefined }} {...vl.baseHtmlAttributes()} {...s.formGroupHtmlAttributes}>
        <input type="checkbox" {...vl.state.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange} disabled={s.ctx.readOnly} />
        {" " + s.labelText}
      </label>
    );
  }
  else {
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }}>
        <input type="checkbox" {...vl.state.valueHtmlAttributes} checked={s.ctx.value || false} onChange={handleCheckboxOnChange}
          className={addClass(vl.state.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))} disabled={s.ctx.readOnly} />
      </FormGroup>
    );
  }
};

ValueLine.renderers["ComboBox"] = (vl) => {
  return internalComboBox(vl);
};

function getPlaceholder(vl: ValueLine): string | undefined {

  const s = vl.state;
  return s.ctx.placeholderLabels ? asString(s.labelText) :
    s.valueHtmlAttributes && s.valueHtmlAttributes!.placeholder;
}

function getOptionsItems(vl: ValueLine): OptionItem[] {
  var ti = getTypeInfo(vl.state.type!.name);
  if (vl.state.comboBoxItems)
    return vl.state.comboBoxItems.map(a =>
      typeof a == "string" ? ti.members[a] && toOptionItem(ti.members[a]) :
        toOptionItem(a)).filter(a => !!a);

  if (vl.state.type!.name == "boolean")
    return ([
      { label: BooleanEnum.niceToString("False")!, value: false },
      { label: BooleanEnum.niceToString("True")!, value: true }
    ]);

  return Dic.getValues(ti.members).map(m => toOptionItem(m));
}

function toOptionItem(m: MemberInfo | OptionItem): OptionItem {

  if ((m as MemberInfo).name)
    return {
      value: (m as MemberInfo).name,
      label: (m as MemberInfo).niceName,
    };

  return m as OptionItem;
}

function internalComboBox(vl: ValueLine) {

  var optionItems = getOptionsItems(vl);

  const s = vl.state;
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
        {ValueLine.withItemGroup(vl,
          <FormControlReadonly htmlAttributes={{
            ...vl.state.valueHtmlAttributes,
            ...({ 'data-value': s.ctx.value } as any) /*Testing*/
          }} ctx={s.ctx} innerRef={elment => vl.inputElement = elment}>
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
      {ValueLine.withItemGroup(vl,
        <select {...vl.state.valueHtmlAttributes} value={toStr(s.ctx.value)} className={addClass(vl.state.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))} onChange={handleEnumOnChange} >
          {optionItems.map((oi, i) => <option key={i} value={toStr(oi.value)}>{oi.label}</option>)}
        </select>)
      }
    </FormGroup>
  );

}

ValueLine.renderers["TextBox" as ValueLineType] = (vl) => {

  const s = vl.state;

  var htmlAtts = vl.state.valueHtmlAttributes;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {ValueLine.withItemGroup(vl, <FormControlReadonly htmlAttributes={htmlAtts} ctx={s.ctx} innerRef={elment => vl.inputElement = elment}>
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
      var fixed = ValueLine.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : true);
      if (fixed != input.value)
        vl.setValue(fixed);

      if (htmlAtts && htmlAtts.onBlur)
        htmlAtts.onBlur(e);
    };
  }


  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {ValueLine.withItemGroup(vl,
        <input type="text"
          autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
          {...vl.state.valueHtmlAttributes}
          className={addClass(vl.state.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))}
          value={s.ctx.value || ""}
          onBlur={handleBlur || htmlAtts && htmlAtts.onBlur}
          onChange={isIE11() ? undefined : handleTextOnChange} //https://github.com/facebook/react/issues/7211
          onInput={isIE11() ? handleTextOnChange : undefined}
          placeholder={getPlaceholder(vl)}
          ref={elment => vl.inputElement = elment} />)
      }
    </FormGroup>
  );
};

function asString(reactChild: React.ReactChild | undefined): string | undefined {
  if (typeof reactChild == "string")
    return reactChild as string;

  return undefined;
}

function isIE11(): boolean {
  return (!!(window as any).MSInputMethodContext && !!(document as any).documentMode);
}

ValueLine.renderers["TextArea" as ValueLineType] = (vl) => {

  const s = vl.state;

  var htmlAtts = vl.state.valueHtmlAttributes;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        <TextArea {...htmlAtts} className={addClass(htmlAtts, classes(s.ctx.formControlClass, vl.mandatoryClass))} value={s.ctx.value || ""}
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
      var fixed = ValueLine.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : false);
      if (fixed != input.value)
        vl.setValue(fixed);

      if (htmlAtts && htmlAtts.onBlur)
        htmlAtts.onBlur(e);
    };
  }

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {ValueLine.withItemGroup(vl,
        <TextArea {...vl.state.valueHtmlAttributes} className={addClass(vl.state.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))} value={s.ctx.value || ""}
          onChange={isIE11() ? undefined : handleTextOnChange} //https://github.com/facebook/react/issues/7211 && https://github.com/omcljs/om/issues/704
          onInput={isIE11() ? handleTextOnChange : undefined}
          onBlur={handleBlur || htmlAtts && htmlAtts.onBlur}
          placeholder={getPlaceholder(vl)}
          innerRef={elment => vl.inputElement = elment} />
      )}
    </FormGroup>
  );
};

ValueLine.renderers["Number" as ValueLineType] = (vl) => {
  return numericTextBox(vl, ValueLine.isNumber);
};

ValueLine.renderers["Decimal" as ValueLineType] = (vl) => {
  return numericTextBox(vl, ValueLine.isDecimal);
};

function numericTextBox(vl: ValueLine, validateKey: (e: React.KeyboardEvent<any>) => boolean) {
  const s = vl.state

  const numbroFormat = toNumbroFormat(s.formatText);

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {ValueLine.withItemGroup(vl,
          <FormControlReadonly htmlAttributes={vl.state.valueHtmlAttributes} ctx={s.ctx} className="numeric" innerRef={elment => vl.inputElement = elment}>
            {s.ctx.value == null ? "" : numbro(s.ctx.value).format(numbroFormat)}
          </FormControlReadonly>)}
      </FormGroup>
    );

  const handleOnChange = (newValue: number | null) => {
    vl.setValue(newValue);
  };
  
  var incNumber = typeof vl.state.incrementWithArrow == "number" ? vl.state.incrementWithArrow : 1;

  const handleKeyDown = (e: React.KeyboardEvent<any>) => {
    if (e.keyCode == KeyCodes.down) {
      e.preventDefault();
      vl.setValue((s.ctx.value || 0) - incNumber);
    } else if (e.keyCode == KeyCodes.up) {
      e.preventDefault();
      vl.setValue((s.ctx.value || 0) + incNumber);
    }
  }

  const htmlAttributes = {
    placeholder: getPlaceholder(vl),
    onKeyDown: vl.state.incrementWithArrow || vl.state.incrementWithArrow == undefined && vl.state.valueLineType == "Number" ? handleKeyDown : undefined, 
    ...vl.props.valueHtmlAttributes
  } as React.AllHTMLAttributes<any>;

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {ValueLine.withItemGroup(vl,
        <NumericTextBox
          htmlAttributes={htmlAttributes}
          value={s.ctx.value}
          onChange={handleOnChange}
          formControlClass={classes(s.ctx.formControlClass, vl.mandatoryClass)}
          validateKey={validateKey}
          format={numbroFormat}
          innerRef={elment => vl.inputElement = elment}
        />
      )}
    </FormGroup>
  );
}

export interface NumericTextBoxProps {
  value: number | null;
  onChange: (newValue: number | null) => void;
  validateKey: (e: React.KeyboardEvent<any>) => boolean;
  format?: string;
  formControlClass?: string;
  htmlAttributes?: React.HTMLAttributes<HTMLInputElement>;
  innerRef?: (ta: HTMLInputElement | null) => void;
}

export class NumericTextBox extends React.Component<NumericTextBoxProps, { text?: string }> {

  constructor(props: NumericTextBoxProps) {
    super(props);
    this.state = { text: undefined };
  }
  
  render() {

    const value = this.state.text != undefined ? this.state.text :
      this.props.value != undefined ? numbro(this.props.value).format(this.props.format) :
        "";

    return <input ref={this.props.innerRef} {...this.props.htmlAttributes}
      type="text"
      autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
      className={addClass(this.props.htmlAttributes, classes(this.props.formControlClass, "numeric"))} value={value}
      onBlur={this.handleOnBlur}
      onChange={isIE11() ? undefined : this.handleOnChange} //https://github.com/facebook/react/issues/7211
      onInput={isIE11() ? this.handleOnChange : undefined}
      onKeyDown={this.handleKeyDown} />

  }

  handleOnBlur = (e: React.FocusEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;

    let value = ValueLine.autoFixString(input.value, false);

    if (numbro.languageData().delimiters.decimal == ',' && !value.contains(",") && value.trim().length > 0) //Numbro transforms 1.000 to 1,0 in spanish or german
      value = value + ",00";

    if (this.props.format && this.props.format.endsWith("%")) {
      if (value && !value.endsWith("%"))
        value += "%";
    }

    const result = value == undefined || value.length == 0 ? null : numbro.unformat(value, this.props.format);
    this.setState({ text: undefined });
    if (result != this.props.value)
      this.props.onChange(result);

    if (this.props.htmlAttributes && this.props.htmlAttributes.onBlur)
      this.props.htmlAttributes.onBlur(e);
  }

  handleOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    this.setState({ text: input.value });
  }

  handleKeyDown = (e: React.KeyboardEvent<any>) => {
    if (!this.props.validateKey(e))
      e.preventDefault();
    else {
      var atts = this.props.htmlAttributes;
      atts && atts.onKeyDown && atts.onKeyDown(e);
    }
  }
}

ValueLine.renderers["DateTime" as ValueLineType] = (vl) => {

  const s = vl.state;

  const momentFormat = toMomentFormat(s.formatText);

  const m = s.ctx.value ? moment(s.ctx.value) : undefined;
  const showTime = momentFormat != "L" && momentFormat != "LL";

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {ValueLine.withItemGroup(vl, <FormControlReadonly htmlAttributes={vl.state.valueHtmlAttributes} className={addClass(vl.state.valueHtmlAttributes, "sf-readonly-date")} ctx={s.ctx} innerRef={elment => vl.inputElement = elment}>
          {m && m.format(momentFormat)}
        </FormControlReadonly>)}
      </FormGroup>
    );

  const handleDatePickerOnChange = (date?: Date, str?: string) => {
    const m = moment(date);
    vl.setValue(!m.isValid() ? null  :
      !showTime ? m.format("YYYY-MM-DDTHH:mm:ss" /*No Z*/) :
        m.format());
  };

  let currentDate = moment();
  if (!showTime)
    currentDate = currentDate.startOf("day");

  const htmlAttributes = {
    placeholder: getPlaceholder(vl),
    ...vl.state.valueHtmlAttributes,
  } as React.AllHTMLAttributes<any>;

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {ValueLine.withItemGroup(vl,
        <div className={classes(s.ctx.rwWidgetClass, vl.mandatoryClass ? vl.mandatoryClass + "-widget" : undefined)}>
          <DateTimePicker value={m && m.toDate()} onChange={handleDatePickerOnChange} autoFocus={vl.state.initiallyFocused}
            format={momentFormat} time={showTime} defaultCurrentDate={currentDate.toDate()} inputProps={htmlAttributes} placeholder={htmlAttributes.placeholder} />
        </div>
      )}
    </FormGroup>
  );
}

ValueLine.renderers["TimeSpan" as ValueLineType] = (vl) => {
  return durationTextBox(vl, ValueLine.isDuration);
};

function durationTextBox(vl: ValueLine, validateKey: (e: React.KeyboardEvent<any>) => boolean) {

  const s = vl.state;

  const durationFormat = toDurationFormat(s.formatText);

  if (s.ctx.readOnly) {
    return (
      <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {ValueLine.withItemGroup(vl,
          <FormControlReadonly htmlAttributes={vl.state.valueHtmlAttributes} ctx={s.ctx} className={addClass(vl.state.valueHtmlAttributes, "numeric")} innerRef={elment => vl.inputElement = elment}>
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
    placeholder: getPlaceholder(vl),
    ...vl.props.valueHtmlAttributes
  } as React.AllHTMLAttributes<any>;

  if (htmlAttributes.placeholder == undefined)
    htmlAttributes.placeholder = durationFormat;

  return (
    <FormGroup ctx={s.ctx} labelText={s.labelText} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {ValueLine.withItemGroup(vl,
        <DurationTextBox htmlAttributes={htmlAttributes}
          value={s.ctx.value}
          onChange={handleOnChange}
          validateKey={validateKey}
          formControlClass={classes(s.ctx.formControlClass, vl.mandatoryClass)}
          format={durationFormat}
          innerRef={elment => vl.inputElement = elment} />
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
  innerRef?: (ta: HTMLInputElement | null) => void;
}

export class DurationTextBox extends React.Component<DurationTextBoxProps, { text?: string }> {

  static defaultProps: {
    format: "hh:mm:ss"
  };

  constructor(props: DurationTextBoxProps) {
    super(props);
    this.state = { text: undefined };
  }

  render() {
    const value = this.state.text != undefined ? this.state.text :
      this.props.value != undefined ? durationToString(this.props.value, this.props.format) :
        "";

    return <input ref={this.props.innerRef}
      {...this.props.htmlAttributes}
      type="text"
      autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
      className={addClass(this.props.htmlAttributes, classes(this.props.formControlClass, "numeric"))}
      value={value}
      onBlur={this.handleOnBlur}
      onChange={isIE11() ? undefined : this.handleOnChange} //https://github.com/facebook/react/issues/7211
      onInput={isIE11() ? this.handleOnChange : undefined}
      onKeyDown={this.handleKeyDown}/>

  }


  handleOnBlur = (e: React.FocusEvent<any>) => {

    var format = this.props.format!;

    function fixNumber(val: string) {
      if (!val.contains(":")) {
        if (format.startsWith("hh"))
          return format.replace("hh", val.toString()).replace("mm", "00").replace("ss", "00");
        if (format.startsWith("mm"))
          return format.replace("mm", val.toString()).replace("ss", "00");
        return val;
      }
      return val;
    }

    function normalize(val: string) {
      if (!"hh:mm:ss".contains(format))
        throw new Error("not implemented");

      return "hh:mm:ss".replace(format, val).replace("hh", "00").replace("mm", "00").replace("ss", "00")
    }


    const input = e.currentTarget as HTMLInputElement;
    const result = input.value == undefined || input.value.length == 0 ? null :
      normalize(fixNumber(input.value));
    this.setState({ text: undefined });
    if (this.props.value != result)
      this.props.onChange(result);;
    if (this.props.htmlAttributes && this.props.htmlAttributes.onBlur)
      this.props.htmlAttributes.onBlur(e);
  }

  handleOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    this.setState({ text: input.value });
  }

  handleKeyDown = (e: React.KeyboardEvent<any>) => {
    if (!this.props.validateKey(e))
      e.preventDefault();
  }
}
