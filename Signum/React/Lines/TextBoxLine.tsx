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

export interface TextBoxLineProps extends LineBaseProps {
  unit?: React.ReactChild;
  format?: string;
  autoTrimString?: boolean;
  autoFixString?: boolean;
  valueHtmlAttributes?: React.AllHTMLAttributes<any>;
  extraButtons?: (vl: TextBoxLineController) => React.ReactNode;
  initiallyFocused?: boolean | number;
  datalist?: string[];

  columnCount?: number;
  columnWidth?: number;

  valueRef?: React.Ref<HTMLElement>;
}

export interface OptionItem {
  value: any;
  label: string;
}

export class TextBoxLineController extends LineBaseController<TextBoxLineProps>{

  inputElement!: React.RefObject<HTMLElement>;
  init(p: TextBoxLineProps) {
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

  getDefaultProps(state: TextBoxLineProps) {
    super.getDefaultProps(state);
  }

  overrideProps(state: TextBoxLineProps, overridenProps: TextBoxLineProps) {

    const valueHtmlAttributes = { ...state.valueHtmlAttributes, ...Dic.simplify(overridenProps.valueHtmlAttributes) };
    super.overrideProps(state, overridenProps);
    state.valueHtmlAttributes = valueHtmlAttributes;
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

export const TextBoxLine = React.memo(React.forwardRef(function TextBoxLine(props: TextBoxLineProps, ref: React.Ref<TextBoxLineController>) {

  const c = useController(TextBoxLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "text");
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export const PasswordLine = React.memo(React.forwardRef(function PasswordLine(props: TextBoxLineProps, ref: React.Ref<TextBoxLineController>) {

  const c = useController(TextBoxLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "password");
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export const GuidLine = React.memo(React.forwardRef(function GuidLine(props: TextBoxLineProps, ref: React.Ref<TextBoxLineController>) {

  const c = useController(TextBoxLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "guid");
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});

export const ColorLine = React.memo(React.forwardRef(function ColorLine(props: TextBoxLineProps, ref: React.Ref<TextBoxLineController>) {

  const c = useController(TextBoxLineController, props, ref);

  if (c.isHidden)
    return null;

  return internalTextBox(c, "color");
}), (prev, next) => {
  if (next.extraButtons || prev.extraButtons)
    return false;

  return LineBaseController.propEquals(prev, next);
});


function internalTextBox(vl: TextBoxLineController, type: "password" | "color" | "text" | "guid") {

  const s = vl.props;

  var htmlAtts = vl.props.valueHtmlAttributes;

  if (s.ctx.readOnly)
    return (
      <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
        {inputId => vl.withItemGroup(<FormControlReadonly id={inputId} htmlAttributes={htmlAtts} ctx={s.ctx} innerRef={vl.setRefs}>
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
      var fixed = TextBoxLineController.autoFixString(input.value, s.autoTrimString != null ? s.autoTrimString : true, type == "guid");
      if (fixed != input.value)
        vl.setValue(fixed, e);

      if (htmlAtts?.onBlur)
        htmlAtts.onBlur(e);
    };
  }

  return (
    <FormGroup ctx={s.ctx} label={s.label} labelIcon={s.labelIcon} helpText={s.helpText} htmlAttributes={{ ...vl.baseHtmlAttributes(), ...s.formGroupHtmlAttributes }} labelHtmlAttributes={s.labelHtmlAttributes}>
      {inputId => <>
        {vl.withItemGroup(
          <input type={type == "color" || type == "guid" ? "text" : type}
            id={inputId}
            autoComplete="asdfasf" /*Not in https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill*/
            {...vl.props.valueHtmlAttributes}
            className={addClass(vl.props.valueHtmlAttributes, classes(s.ctx.formControlClass, vl.mandatoryClass))}
            value={s.ctx.value ?? ""}
            onBlur={handleBlur || htmlAtts?.onBlur}
            onChange={handleTextOnChange}
            placeholder={vl.getPlaceholder()}
            list={s.datalist ? s.ctx.getUniqueId("dataList") : undefined}
            ref={vl.setRefs} />,
          type == "color" ? <input type="color"
            className={classes(s.ctx.formControlClass, "sf-color")}
            value={s.ctx.value ?? ""}
            onBlur={handleBlur || htmlAtts?.onBlur}
            onChange={handleTextOnChange}
          /> : undefined

        )
        }
        {s.datalist &&
          <datalist id={s.ctx.getUniqueId("dataList")}>
            {s.datalist.map((item, i) => <option key={i} value={item} />)}
          </datalist>
        }
      </>}
    </FormGroup>
  );
}

