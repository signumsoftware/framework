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

export interface TextAreaLineProps extends LineBaseProps {
  unit?: React.ReactChild;
  format?: string;
  autoTrimString?: boolean;
  autoFixString?: boolean;
  optionItems?: (OptionItem | MemberInfo | string)[];
  datalist?: string[];
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  extraButtons?: (vl: TextAreaLineController) => React.ReactNode;
  initiallyFocused?: boolean | number;

  incrementWithArrow?: boolean | number;

  columnCount?: number;
  columnWidth?: number;

  valueRef?: React.Ref<HTMLElement>;
}

export interface OptionItem {
  value: any;
  label: string;
}

export class TextAreaLineController extends LineBaseController<TextAreaLineProps>{

  inputElement!: React.RefObject<HTMLElement>;
  init(p: TextAreaLineProps) {
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

  static autoFixString(str: string | null | undefined, autoTrim: boolean, autoNull : boolean): string | null | undefined {

    if (autoTrim)
      str = str?.trim();

    return str == "" && autoNull ? null : str;
  }

  getDefaultProps(state: TextAreaLineProps) {
    super.getDefaultProps(state);
    if (state.type) {
      state.valueLineType = TextAreaLineController.getValueLineType(state.type);

      if (state.valueLineType == undefined)
        throw new Error(`No ValueLineType found for type '${state.type!.name}' (property route = ${state.ctx.propertyRoute ? state.ctx.propertyRoute.propertyPath() : "??"})`);
    }
  }

  overrideProps(state: TextAreaLineProps, overridenProps: TextAreaLineProps) {

    const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
    super.overrideProps(state, overridenProps);
    state.valueHtmlAttributes = valueHtmlAttributes;
  }

  static getValueLineType(t: TypeReference): ValueLineType | undefined {

    if (t.isCollection || t.isLite)
      return undefined;

    //if (isTypeEnum(t.name) || t.name == "boolean" && !t.isNotNullable)
    //  return "DropDownList";

    //if (t.name == "boolean")
    //  return "Checkbox";

    //if (t.name == "DateTime" || t.name == "DateTimeOffset" || t.name == "DateOnly")
    //  return "DateTime";

    //if (t.name == "string" || t.name == "Guid")
    //  return "TextBox";

    if (t.name == "number")
      return "Number";

    if (t.name == "decimal")
      return "Decimal";

    //if (t.name == "TimeSpan" || t.name == "TimeOnly")
    //  return "Time";

    return undefined;
  }

  withItemGroup(input: JSX.Element, preExtraButton?: JSX.Element): JSX.Element {

    if (!this.props.unit && !this.props.extraButtons && !preExtraButton) {
      return <>
        {getTimeMachineIcon({ ctx: this.props.ctx })}
        {input}
      </>;
    }

    return (
      <div className={this.props.ctx.inputGroupClass}>
        {getTimeMachineIcon({ ctx: this.props.ctx })}
        {input}
        {this.props.unit && <span className={this.props.ctx.readonlyAsPlainText ? undefined : "input-group-text"}>{this.props.unit}</span>}
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

export const ValueLine = React.memo(React.forwardRef(function ValueLine(props: TextAreaLineProps, ref: React.Ref<TextAreaLineController>) {

  const c = useController(TextAreaLineController, props, ref);

  if (c.isHidden)
    return null;

  return ValueLineRenderers.renderers.get(c.props.valueLineType!)!(c);
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export namespace ValueLineRenderers {
  export const renderers: Map<ValueLineType, (vl: TextAreaLineController) => JSX.Element> = new Map();
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

function getOptionsItems(vl: TextAreaLineController): OptionItem[] {

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

ValueLineRenderers.renderers.set("TextArea", (vl) => {

  const s = vl.props;

  var htmlAtts = vl.props.valueHtmlAttributes;
  var autoResize = htmlAtts?.style?.height == null && htmlAtts?.rows == null;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => <>
          {getTimeMachineIcon({ ctx: s.ctx })}
          <TextArea id={inputId} {...htmlAtts} autoResize={autoResize} className={addClass(htmlAtts, classes(s.ctx.formControlClass, vl.mandatoryClass))} value={s.ctx.value || ""}
            disabled />
        </>}
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
      var fixed = TextAreaLineController.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : false, false);
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
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId =>  vl.withItemGroup(
        <TextArea {...vl.props.valueHtmlAttributes} autoResize={autoResize} className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))} value={s.ctx.value || ""}
          id={inputId}
          minHeight={vl.props.valueHtmlAttributes?.style?.minHeight?.toString()}
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

function numericTextBox(vl: TextAreaLineController, validateKey: (e: React.KeyboardEvent<any>) => boolean) {
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
    onKeyDown: (vl.props.incrementWithArrow || vl.props.incrementWithArrow == undefined && vl.props.valueLineType == "Number") ? handleKeyDown : undefined,
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
        let value = TextAreaLineController.autoFixString(text, false, false);

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

export interface ColorTextBoxProps {
  value: string | null;
  onChange: (newValue: string | null) => void;
  formControlClass?: string;
  groupClass?: string;
  textValueHtmlAttributes?: React.HTMLAttributes<HTMLInputElement>;
  groupHtmlAttributes?: React.HTMLAttributes<HTMLInputElement>;
  innerRef?: React.Ref<HTMLInputElement>;
}

export function ColorTextBox(p: ColorTextBoxProps) {

  const [text, setText] = React.useState<string | undefined>(undefined);

  const value = text != undefined ? text : p.value != undefined ? p.value : "";

  return (
    <span {...p.groupHtmlAttributes} className={addClass(p.groupHtmlAttributes, classes(p.groupClass))}>
      <input type="text"
        autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
        {...p.textValueHtmlAttributes}
        className={addClass(p.textValueHtmlAttributes, classes(p.formControlClass))}
        value={value}
        onBlur={handleOnBlur}
        onChange={handleOnChange}
        onFocus={handleOnFocus}
        ref={p.innerRef} />
      <input type="color"
        className={classes(p.formControlClass, "sf-color")}
        value={value}
        onBlur={handleOnBlur}
        onChange={handleOnChange}
      />
    </span>);

  function handleOnFocus(e: React.FocusEvent<any>) {
    const input = e.currentTarget as HTMLInputElement;

    input.setSelectionRange(0, input.value != null ? input.value.length : 0);

    if (p.textValueHtmlAttributes?.onFocus)
      p.textValueHtmlAttributes.onFocus(e);
  };

  function handleOnBlur(e: React.FocusEvent<any>) {

    const input = e.currentTarget as HTMLInputElement;

    var result = input.value == undefined || input.value.length == 0 ? null : input.value;

    setText(undefined);
    if (p.value != result)
      p.onChange(result);
    if (p.textValueHtmlAttributes?.onBlur)
      p.textValueHtmlAttributes.onBlur(e);
  }

  function handleOnChange(e: React.SyntheticEvent<any>) {
    const input = e.currentTarget as HTMLInputElement;
    setText(input.value);
    if (p.onChange)
      p.onChange(input.value);
  }
}

ValueLineRenderers.renderers.set("RadioGroup", (vl) => {
  return internalRadioGroup(vl);
});

function internalRadioGroup(vl: TextAreaLineController) {

  var optionItems = getOptionsItems(vl);

  const s = vl.props;

  const handleEnumOnChange = (e: React.SyntheticEvent<any>) => {
    const input = e.currentTarget as HTMLInputElement;
    const option = optionItems.filter(a => a.value == input.value).single();
    vl.setValue(option.value, e);
  };

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => <>        
        {getTimeMachineIcon({ ctx: s.ctx })}
        <div style={getColumnStyle()}>
          {optionItems.map((oi, i) =>
            <label {...vl.props.valueHtmlAttributes} className={classes("sf-radio-element", vl.props.ctx.errorClass)}>
              <input type="radio" key={i} value={oi.value} checked={s.ctx.value == oi.value} onChange={handleEnumOnChange} disabled={s.ctx.readOnly} />
              {" " + oi.label}
            </label>)}
        </div>
        </> }
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
  if (lineBase instanceof TextAreaLineController) {
    const vProps = state as TextAreaLineProps;

    if (vProps.unit === undefined &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.unit = state.ctx.propertyRoute.member!.unit;
    }
  }
}

tasks.push(taskSetFormat);
export function taskSetFormat(lineBase: LineBaseController<any>, state: LineBaseProps) {
  if (lineBase instanceof TextAreaLineController) {
    const vProps = state as TextAreaLineProps;

    if (!vProps.format &&
      state.ctx.propertyRoute &&
      state.ctx.propertyRoute.propertyRouteType == "Field") {
      vProps.format = state.ctx.propertyRoute.member!.format;
      if (vProps.valueLineType == "TextBox" && state.ctx.propertyRoute.member!.format == "Password")
        vProps.valueLineType = "Password";
      else if (vProps.valueLineType == "TextBox" && state.ctx.propertyRoute.member!.format == "Color")
        vProps.valueLineType = "Color";
    }
  }
}


export let maxValueLineSize = 100;

tasks.push(taskSetHtmlProperties);
export function taskSetHtmlProperties(lineBase: LineBaseController<any>, state: LineBaseProps) {
  const vl = lineBase instanceof TextAreaLineController ? lineBase : undefined;
  const pr = state.ctx.propertyRoute;
  const s = state as TextAreaLineProps;
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
